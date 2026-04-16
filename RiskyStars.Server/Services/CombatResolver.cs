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
                {
                    defenderCasualties[defenderArmyId] = 0;
                }

                defenderCasualties[defenderArmyId]++;
            }
            else
            {
                // Defender won, attacker loses a unit
                var attackerArmyId = pairing.AttackerRoll.ArmyId;
                if (!attackerCasualties.ContainsKey(attackerArmyId))
                {
                    attackerCasualties[attackerArmyId] = 0;
                }

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
        List<Army> attackingArmies,
        List<Army> defendingArmies,
        ReinforcementArrivalOrder arrivalOrder)
    {
        var combatEvent = new CombatEvent
        {
            EventId = Guid.NewGuid().ToString(),
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            EventType = CombatEvent.Types.CombatEventType.ReinforcementsArrived,
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

        // Calculate total casualties for each side
        int totalAttackerCasualties = 0;
        int totalDefenderCasualties = 0;

        foreach (var pairing in pairings.Where(p => !p.IsDiscarded))
        {
            if (pairing.WinnerArmyId == pairing.AttackerRoll.ArmyId)
            {
                totalDefenderCasualties++;
            }
            else
            {
                totalAttackerCasualties++;
            }
        }

        // Apply casualties based on reinforcement arrival order
        ApplyCasualtiesWithReinforcementOrder(
            attackingArmies,
            defendingArmies,
            totalAttackerCasualties,
            totalDefenderCasualties,
            arrivalOrder,
            roundResult);

        // Add round result to combat event
        combatEvent.RoundResults.Add(roundResult);

        // Add army states after casualties
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

    private void ApplyCasualtiesWithReinforcementOrder(
        List<Army> attackingArmies,
        List<Army> defendingArmies,
        int totalAttackerCasualties,
        int totalDefenderCasualties,
        ReinforcementArrivalOrder arrivalOrder,
        CombatRoundResult roundResult)
    {
        var casualties = new List<ArmyCasualty>();

        // Apply attacker casualties
        int remainingAttackerCasualties = totalAttackerCasualties;
        
        // Apply to attacking reinforcements first
        foreach (var army in attackingArmies.Where(a => a.CombatRole == CombatRole.AttackingReinforcement))
        {
            if (remainingAttackerCasualties <= 0)
            {
                break;
            }

            int armyCasualties = Math.Min(army.UnitCount, remainingAttackerCasualties);
            army.UnitCount -= armyCasualties;
            remainingAttackerCasualties -= armyCasualties;
            
            casualties.Add(new ArmyCasualty
            {
                ArmyId = army.Id,
                PlayerId = army.OwnerId,
                CombatRole = army.CombatRole.ToString(),
                Casualties = armyCasualties,
                RemainingUnits = army.UnitCount
            });
        }

        // Apply remaining to original attackers
        foreach (var army in attackingArmies.Where(a => a.CombatRole == CombatRole.Attacker))
        {
            if (remainingAttackerCasualties <= 0)
            {
                casualties.Add(new ArmyCasualty
                {
                    ArmyId = army.Id,
                    PlayerId = army.OwnerId,
                    CombatRole = army.CombatRole.ToString(),
                    Casualties = 0,
                    RemainingUnits = army.UnitCount
                });
            }
            else
            {
                int armyCasualties = Math.Min(army.UnitCount, remainingAttackerCasualties);
                army.UnitCount -= armyCasualties;
                remainingAttackerCasualties -= armyCasualties;
                
                casualties.Add(new ArmyCasualty
                {
                    ArmyId = army.Id,
                    PlayerId = army.OwnerId,
                    CombatRole = army.CombatRole.ToString(),
                    Casualties = armyCasualties,
                    RemainingUnits = army.UnitCount
                });
            }
        }

        // Apply defender casualties - always apply to defending reinforcements first
        int remainingDefenderCasualties = totalDefenderCasualties;
        
        // Apply to defending reinforcements first
        foreach (var army in defendingArmies.Where(a => a.CombatRole == CombatRole.DefendingReinforcement))
        {
            if (remainingDefenderCasualties <= 0)
            {
                break;
            }

            int armyCasualties = Math.Min(army.UnitCount, remainingDefenderCasualties);
            army.UnitCount -= armyCasualties;
            remainingDefenderCasualties -= armyCasualties;
            
            casualties.Add(new ArmyCasualty
            {
                ArmyId = army.Id,
                PlayerId = army.OwnerId,
                CombatRole = army.CombatRole.ToString(),
                Casualties = armyCasualties,
                RemainingUnits = army.UnitCount
            });
        }

        // Apply remaining to original defenders
        foreach (var army in defendingArmies.Where(a => a.CombatRole == CombatRole.Defender))
        {
            if (remainingDefenderCasualties <= 0)
            {
                casualties.Add(new ArmyCasualty
                {
                    ArmyId = army.Id,
                    PlayerId = army.OwnerId,
                    CombatRole = army.CombatRole.ToString(),
                    Casualties = 0,
                    RemainingUnits = army.UnitCount
                });
            }
            else
            {
                int armyCasualties = Math.Min(army.UnitCount, remainingDefenderCasualties);
                army.UnitCount -= armyCasualties;
                remainingDefenderCasualties -= armyCasualties;
                
                casualties.Add(new ArmyCasualty
                {
                    ArmyId = army.Id,
                    PlayerId = army.OwnerId,
                    CombatRole = army.CombatRole.ToString(),
                    Casualties = armyCasualties,
                    RemainingUnits = army.UnitCount
                });
            }
        }

        roundResult.Casualties.AddRange(casualties);
    }

    public List<CombatEvent> ResolveMultiRoundCombat(
        string locationId,
        List<Army> attackingArmies,
        List<Army> defendingArmies,
        List<ReinforcementArrival> reinforcementSchedule)
    {
        var combatEvents = new List<CombatEvent>();

        // Initial combat round between attacker and defender
        if (attackingArmies.Any(a => a.UnitCount > 0) && defendingArmies.Any(d => d.UnitCount > 0))
        {
            var initialEvent = ResolveCombatRound(
                locationId,
                attackingArmies,
                defendingArmies,
                CombatEvent.Types.CombatEventType.CombatInitiated);
            
            combatEvents.Add(initialEvent);
        }

        // Process reinforcements in arrival order
        foreach (var reinforcement in reinforcementSchedule.OrderBy(r => r.ArrivalOrder))
        {
            // Add reinforcement to appropriate side
            if (reinforcement.IsAttacker)
            {
                reinforcement.Army.CombatRole = CombatRole.AttackingReinforcement;
                reinforcement.Army.IsInCombat = true;
                attackingArmies.Add(reinforcement.Army);
            }
            else
            {
                reinforcement.Army.CombatRole = CombatRole.DefendingReinforcement;
                reinforcement.Army.IsInCombat = true;
                defendingArmies.Add(reinforcement.Army);
            }

            // Check if there are opposing forces
            bool hasAttackers = attackingArmies.Any(a => a.UnitCount > 0);
            bool hasDefenders = defendingArmies.Any(d => d.UnitCount > 0);

            if (hasAttackers && hasDefenders)
            {
                var reinforcementEvent = ResolveReinforcementCombat(
                    locationId,
                    attackingArmies,
                    defendingArmies,
                    new ReinforcementArrivalOrder
                    {
                        ReinforcementArrivalIndex = reinforcement.ArrivalOrder,
                        IsAttackerReinforcement = reinforcement.IsAttacker
                    });
                
                combatEvents.Add(reinforcementEvent);
            }

            // Remove destroyed armies after each reinforcement round
            attackingArmies.RemoveAll(a => a.UnitCount <= 0);
            defendingArmies.RemoveAll(d => d.UnitCount <= 0);
        }

        return combatEvents;
    }
}

public class ReinforcementArrivalOrder
{
    public int ReinforcementArrivalIndex { get; set; }
    public bool IsAttackerReinforcement { get; set; }
}

public class ReinforcementArrival
{
    public Army Army { get; set; } = null!;
    public bool IsAttacker { get; set; }
    public int ArrivalOrder { get; set; }
}
