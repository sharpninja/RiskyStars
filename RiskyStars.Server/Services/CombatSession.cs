using RiskyStars.Server.Entities;
using RiskyStars.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RiskyStars.Server.Services;

public class CombatSession
{
    public string LocationId { get; set; } = string.Empty;
    public List<Army> AttackingArmies { get; set; } = new();
    public List<Army> DefendingArmies { get; set; } = new();
    public List<CombatEvent> CombatHistory { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public int RoundNumber { get; set; } = 0;
    public List<ReinforcementArrival> ReinforcementArrivals { get; set; } = new();
    public int NextReinforcementOrder { get; set; } = 0;

    public bool HasDefendersRemaining()
    {
        return DefendingArmies.Any(a => a.UnitCount > 0);
    }

    public bool HasAttackersRemaining()
    {
        return AttackingArmies.Any(a => a.UnitCount > 0);
    }

    public void AddReinforcements(Army army, bool isAttacker)
    {
        if (isAttacker)
        {
            army.CombatRole = CombatRole.AttackingReinforcement;
            AttackingArmies.Add(army);
        }
        else
        {
            army.CombatRole = CombatRole.DefendingReinforcement;
            DefendingArmies.Add(army);
        }

        // Track reinforcement arrival order
        ReinforcementArrivals.Add(new ReinforcementArrival
        {
            Army = army,
            IsAttacker = isAttacker,
            ArrivalOrder = NextReinforcementOrder++
        });
    }

    public void RemoveDestroyedArmies()
    {
        AttackingArmies.RemoveAll(a => a.UnitCount <= 0);
        DefendingArmies.RemoveAll(a => a.UnitCount <= 0);
    }

    public CombatOutcome DetermineCombatOutcome()
    {
        bool defendersRemain = HasDefendersRemaining();
        bool attackersRemain = HasAttackersRemaining();

        if (!defendersRemain && !attackersRemain)
        {
            // First reinforcing player with remaining units gains possession
            // Check reinforcement arrivals in order
            var firstReinforcementWithUnits = ReinforcementArrivals
                .OrderBy(r => r.ArrivalOrder)
                .FirstOrDefault(r => r.Army.UnitCount > 0);

            if (firstReinforcementWithUnits != null)
            {
                return new CombatOutcome
                {
                    IsComplete = true,
                    WinningSide = firstReinforcementWithUnits.IsAttacker ? CombatSide.Attacker : CombatSide.Defender,
                    SurvivingArmies = new List<Army> { firstReinforcementWithUnits.Army }
                };
            }

            return new CombatOutcome
            {
                IsComplete = true,
                WinningSide = CombatSide.None,
                SurvivingArmies = new List<Army>()
            };
        }
        else if (!defendersRemain && attackersRemain)
        {
            return new CombatOutcome
            {
                IsComplete = true,
                WinningSide = CombatSide.Attacker,
                SurvivingArmies = AttackingArmies.Where(a => a.UnitCount > 0).ToList()
            };
        }
        else if (defendersRemain && !attackersRemain)
        {
            return new CombatOutcome
            {
                IsComplete = true,
                WinningSide = CombatSide.Defender,
                SurvivingArmies = DefendingArmies.Where(a => a.UnitCount > 0).ToList()
            };
        }
        else
        {
            return new CombatOutcome
            {
                IsComplete = false,
                WinningSide = CombatSide.None,
                SurvivingArmies = new List<Army>()
            };
        }
    }

    public ReinforcementArrivalOrder GetCurrentReinforcementOrder(bool isAttacker)
    {
        return new ReinforcementArrivalOrder
        {
            ReinforcementArrivalIndex = NextReinforcementOrder - 1,
            IsAttackerReinforcement = isAttacker
        };
    }
}

public class CombatOutcome
{
    public bool IsComplete { get; set; }
    public CombatSide WinningSide { get; set; }
    public List<Army> SurvivingArmies { get; set; } = new();
}

public enum CombatSide
{
    None,
    Attacker,
    Defender
}
