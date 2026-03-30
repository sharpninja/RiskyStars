using RiskyStars.Server.Entities;
using RiskyStars.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RiskyStars.Server.Services;

public class CombatResolver
{
    private readonly Random _random;

    public CombatResolver(int seed)
    {
        _random = new Random(seed);
    }

    public CombatResolver() : this(Environment.TickCount)
    {
    }

    public CombatEvent ResolveCombatRound(
        string locationId,
        List<Army> attackingArmies,
        List<Army> defendingArmies,
        CombatEvent.Types.CombatEventType eventType = CombatEvent.Types.CombatEventType.CombatRoundComplete)
    {
        var combatEvent = new CombatEvent
        {
            EventId = Guid.NewGuid().ToString(),
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            EventType = eventType,
            LocationId = locationId
        };

        var roundResult = new CombatRoundResult();

        // Roll dice for all units in attacking armies
        var attackerRolls = new List<DiceRoll>();
        foreach (var army in attackingArmies)
        {
            for (int i = 0; i < army.UnitCount; i++)
            {
                var roll = new DiceRoll
                {
                    ArmyId = army.Id,
                    Roll = RollDice(),
                    UnitIndex = i
                };
                attackerRolls.Add(roll);
            }
        }

        // Roll dice for all units in defending armies
        var defenderRolls = new List<DiceRoll>();
        foreach (var army in defendingArmies)
        {
            for (int i = 0; i < army.UnitCount; i++)
            {
                var roll = new DiceRoll
                {
                    ArmyId = army.Id,
                    Roll = RollDice(),
                    UnitIndex = i
                };
                defenderRolls.Add(roll);
            }
        }

        // Sort descending
        attackerRolls = attackerRolls.OrderByDescending(r => r.Roll).ToList();
        defenderRolls = defenderRolls.OrderByDescending(r => r.Roll).ToList();

        roundResult.AttackerRolls.AddRange(attackerRolls);
        roundResult.DefenderRolls.AddRange(defenderRolls);

        // Pair rolls
        var pairings = PairRolls(attackerRolls, defenderRolls);
        roundResult.Pairings.AddRange(pairings);

        // Calculate casualties
        var casualties = CalculateCasualties(pairings, attackingArmies, defendingArmies);
        roundResult.Casualties.AddRange(casualties);

        // Apply casualties to armies
        ApplyCasualties(casualties, attackingArmies, defendingArmies);

        // Add round result to combat event
        combatEvent.RoundResults.Add(roundResult);

        // Add army states
        foreach (var army in attackingArmies)
        {
            combatEvent.ArmyStates.Add(new CombatArmyState
            {
                ArmyId = army.Id,
                PlayerId = army.OwnerId,
                CombatRole = army.CombatRole?.ToString() ?? "Attacker",
                UnitCount = army.UnitCount
            });
        }

        foreach (var army in defendingArmies)
        {
            combatEvent.ArmyStates.Add(new CombatArmyState
            {
                ArmyId = army.Id,
                PlayerId = army.OwnerId,
                CombatRole = army.CombatRole?.ToString() ?? "Defender",
                UnitCount = army.UnitCount
            });
        }

        return combatEvent;
    }

    private int RollDice()
    {
        return _random.Next(1, 11); // 1-10 inclusive
    }

    private List<RollPairing> PairRolls(List<DiceRoll> attackerRolls, List<DiceRoll> defenderRolls)
    {
        var pairings = new List<RollPairing>();
        int pairCount = Math.Min(attackerRolls.Count, defenderRolls.Count);

        // Pair the sorted rolls
        for (int i = 0; i < pairCount; i++)
        {
            var attackerRoll = attackerRolls[i];
            var defenderRoll = defenderRolls[i];

            string winnerArmyId;
            if (attackerRoll.Roll > defenderRoll.Roll)
            {
                winnerArmyId = attackerRoll.ArmyId;
            }
            else
            {
                // Defender wins ties
                winnerArmyId = defenderRoll.ArmyId;
            }

            pairings.Add(new RollPairing
            {
                AttackerRoll = attackerRoll,
                DefenderRoll = defenderRoll,
                WinnerArmyId = winnerArmyId,
                IsDiscarded = false
            });
        }

        // Mark unpaired rolls as discarded
        for (int i = pairCount; i < attackerRolls.Count; i++)
        {
            pairings.Add(new RollPairing
            {
                AttackerRoll = attackerRolls[i],
                DefenderRoll = null,
                WinnerArmyId = "",
                IsDiscarded = true
            });
        }

        for (int i = pairCount; i < defenderRolls.Count; i++)
        {
            pairings.Add(new RollPairing
            {
                AttackerRoll = null,
                DefenderRoll = defenderRolls[i],
                WinnerArmyId = "",
                IsDiscarded = true
            });
        }

        return pairings;
    }

    private List<ArmyCasualty> CalculateCasualties(
        List<RollPairing> pairings,
        List<Army> attackingArmies,
        List<Army> defendingArmies)
    {
        var attackerCasualties = new Dictionary<string, int>();
        var defenderCasualties = new Dictionary<string, int>();

        // Count casualties from pairings
        foreach (var pairing in pairings.Where(p => !p.IsDiscarded))
        {
            if (pairing.WinnerArmyId == pairing.AttackerRoll.ArmyId)
            {
                // Attacker won, defender loses a unit
                var defenderArmyId = pairing.DefenderRoll.ArmyId;
                if (!defenderCasualties.ContainsKey(defenderArmyId))
                    defenderCasualties[defenderArmyId] = 0;
                defenderCasualties[defenderArmyId]++;
            }
            else
            {
                // Defender won, attacker loses a unit
                var attackerArmyId = pairing.AttackerRoll.ArmyId;
                if (!attackerCasualties.ContainsKey(attackerArmyId))
                    attackerCasualties[attackerArmyId] = 0;
                attackerCasualties[attackerArmyId]++;
            }
        }

        var casualties = new List<ArmyCasualty>();

        // Create casualty records for attacking armies
        foreach (var army in attackingArmies)
        {
            int casualtyCount = attackerCasualties.GetValueOrDefault(army.Id, 0);
            casualties.Add(new ArmyCasualty
            {
                ArmyId = army.Id,
                PlayerId = army.OwnerId,
                CombatRole = army.CombatRole?.ToString() ?? "Attacker",
                Casualties = casualtyCount,
                RemainingUnits = army.UnitCount - casualtyCount
            });
        }

        // Create casualty records for defending armies
        foreach (var army in defendingArmies)
        {
            int casualtyCount = defenderCasualties.GetValueOrDefault(army.Id, 0);
            casualties.Add(new ArmyCasualty
            {
                ArmyId = army.Id,
                PlayerId = army.OwnerId,
                CombatRole = army.CombatRole?.ToString() ?? "Defender",
                Casualties = casualtyCount,
                RemainingUnits = army.UnitCount - casualtyCount
            });
        }

        return casualties;
    }

    private void ApplyCasualties(
        List<ArmyCasualty> casualties,
        List<Army> attackingArmies,
        List<Army> defendingArmies)
    {
        var allArmies = attackingArmies.Concat(defendingArmies).ToDictionary(a => a.Id);

        foreach (var casualty in casualties)
        {
            if (allArmies.TryGetValue(casualty.ArmyId, out var army))
            {
                army.UnitCount = Math.Max(0, casualty.RemainingUnits);
            }
        }
    }

    public CombatEvent ResolveReinforcementCombat(
        string locationId,
        List<Army> reinforcingAttackers,
        List<Army> reinforcingDefenders,
        List<Army> existingDefenders)
    {
        // When reinforcements arrive, apply casualties to reinforcements first
        // According to 1.1.02: "all defending casualties are applied to the Defending Reinforcements first"
        
        var allAttackers = reinforcingAttackers.ToList();
        var allDefenders = reinforcingDefenders.Concat(existingDefenders).ToList();

        var combatEvent = ResolveCombatRound(
            locationId,
            allAttackers,
            allDefenders,
            CombatEvent.Types.CombatEventType.ReinforcementsArrived);

        // After calculating casualties, apply them to reinforcements first
        if (reinforcingDefenders.Any())
        {
            ApplyCasualtiesReinforcementsFirst(
                combatEvent.RoundResults[0],
                reinforcingDefenders,
                existingDefenders);
        }

        return combatEvent;
    }

    private void ApplyCasualtiesReinforcementsFirst(
        CombatRoundResult roundResult,
        List<Army> reinforcingDefenders,
        List<Army> existingDefenders)
    {
        // Get total defender casualties
        int totalDefenderCasualties = roundResult.Casualties
            .Where(c => c.CombatRole == "Defender" || c.CombatRole == "DefendingReinforcement")
            .Sum(c => c.Casualties);

        // Apply to reinforcements first
        int remainingCasualties = totalDefenderCasualties;
        
        foreach (var army in reinforcingDefenders)
        {
            if (remainingCasualties <= 0) break;
            
            int casualties = Math.Min(army.UnitCount, remainingCasualties);
            army.UnitCount -= casualties;
            remainingCasualties -= casualties;
        }

        // Apply remaining casualties to existing defenders
        foreach (var army in existingDefenders)
        {
            if (remainingCasualties <= 0) break;
            
            int casualties = Math.Min(army.UnitCount, remainingCasualties);
            army.UnitCount -= casualties;
            remainingCasualties -= casualties;
        }

        // Update casualty records to reflect actual distribution
        roundResult.Casualties.Clear();
        
        foreach (var army in reinforcingDefenders)
        {
            roundResult.Casualties.Add(new ArmyCasualty
            {
                ArmyId = army.Id,
                PlayerId = army.OwnerId,
                CombatRole = army.CombatRole?.ToString() ?? "DefendingReinforcement",
                Casualties = totalDefenderCasualties - remainingCasualties,
                RemainingUnits = army.UnitCount
            });
        }

        foreach (var army in existingDefenders)
        {
            roundResult.Casualties.Add(new ArmyCasualty
            {
                ArmyId = army.Id,
                PlayerId = army.OwnerId,
                CombatRole = army.CombatRole?.ToString() ?? "Defender",
                Casualties = 0,
                RemainingUnits = army.UnitCount
            });
        }
    }
}
