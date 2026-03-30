using RiskyStars.Server.Entities;
using RiskyStars.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RiskyStars.Server.Services;

public class CombatManager
{
    private readonly CombatResolver _combatResolver;
    private readonly Dictionary<string, CombatSession> _activeCombats;

    public CombatManager(CombatResolver combatResolver)
    {
        _combatResolver = combatResolver;
        _activeCombats = new Dictionary<string, CombatSession>();
    }

    public CombatManager(int seed) : this(new CombatResolver(seed))
    {
    }

    public IEnumerable<CombatEvent> InitiateCombat(string locationId, Army attacker, Army defender)
    {
        if (_activeCombats.ContainsKey(locationId))
        {
            throw new InvalidOperationException($"Combat already active at location {locationId}");
        }

        attacker.CombatRole = CombatRole.Attacker;
        attacker.IsInCombat = true;
        defender.CombatRole = CombatRole.Defender;
        defender.IsInCombat = true;

        var session = new CombatSession
        {
            LocationId = locationId,
            AttackingArmies = new List<Army> { attacker },
            DefendingArmies = new List<Army> { defender }
        };

        _activeCombats[locationId] = session;

        var events = new List<CombatEvent>();

        // Send combat initiated event
        var initiatedEvent = new CombatEvent
        {
            EventId = Guid.NewGuid().ToString(),
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            EventType = CombatEvent.Types.CombatEventType.CombatInitiated,
            LocationId = locationId
        };
        
        initiatedEvent.ArmyStates.Add(new CombatArmyState
        {
            ArmyId = attacker.Id,
            PlayerId = attacker.OwnerId,
            CombatRole = CombatRole.Attacker.ToString(),
            UnitCount = attacker.UnitCount
        });
        
        initiatedEvent.ArmyStates.Add(new CombatArmyState
        {
            ArmyId = defender.Id,
            PlayerId = defender.OwnerId,
            CombatRole = CombatRole.Defender.ToString(),
            UnitCount = defender.UnitCount
        });

        events.Add(initiatedEvent);
        session.CombatHistory.Add(initiatedEvent);

        return events;
    }

    public IEnumerable<CombatEvent> ResolveCombatRound(string locationId)
    {
        if (!_activeCombats.TryGetValue(locationId, out var session))
        {
            throw new InvalidOperationException($"No active combat at location {locationId}");
        }

        var events = new List<CombatEvent>();

        // Resolve the combat round
        var combatEvent = _combatResolver.ResolveCombatRound(
            locationId,
            session.AttackingArmies,
            session.DefendingArmies);

        events.Add(combatEvent);
        session.CombatHistory.Add(combatEvent);
        session.RoundNumber++;

        // Remove destroyed armies
        session.RemoveDestroyedArmies();

        // Check if combat is complete
        var outcome = session.DetermineCombatOutcome();
        if (outcome.IsComplete)
        {
            var endEvent = new CombatEvent
            {
                EventId = Guid.NewGuid().ToString(),
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                EventType = CombatEvent.Types.CombatEventType.CombatEnded,
                LocationId = locationId
            };

            foreach (var army in outcome.SurvivingArmies)
            {
                endEvent.ArmyStates.Add(new CombatArmyState
                {
                    ArmyId = army.Id,
                    PlayerId = army.OwnerId,
                    CombatRole = army.CombatRole?.ToString() ?? "",
                    UnitCount = army.UnitCount
                });
                
                army.IsInCombat = false;
                army.CombatRole = null;
            }

            events.Add(endEvent);
            session.CombatHistory.Add(endEvent);
            session.IsActive = false;
            _activeCombats.Remove(locationId);
        }

        return events;
    }

    public IEnumerable<CombatEvent> AddReinforcements(string locationId, Army reinforcement, bool isAttacker)
    {
        if (!_activeCombats.TryGetValue(locationId, out var session))
        {
            throw new InvalidOperationException($"No active combat at location {locationId}");
        }

        var events = new List<CombatEvent>();

        reinforcement.IsInCombat = true;
        session.AddReinforcements(reinforcement, isAttacker);

        // Reinforcements immediately participate in combat
        if (isAttacker)
        {
            // Attacking reinforcements fight existing defenders
            var combatEvent = _combatResolver.ResolveCombatRound(
                locationId,
                new List<Army> { reinforcement },
                session.DefendingArmies,
                CombatEvent.Types.CombatEventType.ReinforcementsArrived);

            events.Add(combatEvent);
            session.CombatHistory.Add(combatEvent);
        }
        else
        {
            // Defending reinforcements - casualties applied to reinforcements first
            if (session.AttackingArmies.Any(a => a.UnitCount > 0))
            {
                var defendingReinforcements = session.DefendingArmies
                    .Where(a => a.CombatRole == CombatRole.DefendingReinforcement)
                    .ToList();
                
                var existingDefenders = session.DefendingArmies
                    .Where(a => a.CombatRole == CombatRole.Defender)
                    .ToList();

                var combatEvent = _combatResolver.ResolveReinforcementCombat(
                    locationId,
                    session.AttackingArmies,
                    defendingReinforcements,
                    existingDefenders);

                events.Add(combatEvent);
                session.CombatHistory.Add(combatEvent);
            }
        }

        session.RoundNumber++;
        session.RemoveDestroyedArmies();

        // Check if combat is complete
        var outcome = session.DetermineCombatOutcome();
        if (outcome.IsComplete)
        {
            var endEvent = new CombatEvent
            {
                EventId = Guid.NewGuid().ToString(),
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                EventType = CombatEvent.Types.CombatEventType.CombatEnded,
                LocationId = locationId
            };

            foreach (var army in outcome.SurvivingArmies)
            {
                endEvent.ArmyStates.Add(new CombatArmyState
                {
                    ArmyId = army.Id,
                    PlayerId = army.OwnerId,
                    CombatRole = army.CombatRole?.ToString() ?? "",
                    UnitCount = army.UnitCount
                });
                
                army.IsInCombat = false;
                army.CombatRole = null;
            }

            events.Add(endEvent);
            session.CombatHistory.Add(endEvent);
            session.IsActive = false;
            _activeCombats.Remove(locationId);
        }

        return events;
    }

    public bool IsCombatActive(string locationId)
    {
        return _activeCombats.ContainsKey(locationId);
    }

    public CombatSession? GetCombatSession(string locationId)
    {
        _activeCombats.TryGetValue(locationId, out var session);
        return session;
    }

    public void EndCombat(string locationId)
    {
        if (_activeCombats.TryGetValue(locationId, out var session))
        {
            foreach (var army in session.AttackingArmies.Concat(session.DefendingArmies))
            {
                army.IsInCombat = false;
                army.CombatRole = null;
            }
            
            session.IsActive = false;
            _activeCombats.Remove(locationId);
        }
    }
}
