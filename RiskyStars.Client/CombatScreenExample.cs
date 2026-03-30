using RiskyStars.Shared;

namespace RiskyStars.Client;

public static class CombatScreenExample
{
    public static CombatEvent CreateSampleCombatEvent()
    {
        var combatEvent = new CombatEvent
        {
            EventId = "combat_001",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            EventType = CombatEvent.Types.CombatEventType.CombatEnded,
            LocationId = "region_alpha_1"
        };

        var attackerArmy = new CombatArmyState
        {
            ArmyId = "army_attacker_1",
            PlayerId = "player_1",
            CombatRole = "Attacker",
            UnitCount = 3
        };

        var defenderArmy = new CombatArmyState
        {
            ArmyId = "army_defender_1",
            PlayerId = "player_2",
            CombatRole = "Defender",
            UnitCount = 2
        };

        combatEvent.ArmyStates.Add(attackerArmy);
        combatEvent.ArmyStates.Add(defenderArmy);

        var round1 = new CombatRoundResult();
        
        round1.AttackerRolls.Add(new DiceRoll { ArmyId = "army_attacker_1", Roll = 6, UnitIndex = 0 });
        round1.AttackerRolls.Add(new DiceRoll { ArmyId = "army_attacker_1", Roll = 4, UnitIndex = 1 });
        round1.AttackerRolls.Add(new DiceRoll { ArmyId = "army_attacker_1", Roll = 2, UnitIndex = 2 });
        
        round1.DefenderRolls.Add(new DiceRoll { ArmyId = "army_defender_1", Roll = 5, UnitIndex = 0 });
        round1.DefenderRolls.Add(new DiceRoll { ArmyId = "army_defender_1", Roll = 3, UnitIndex = 1 });
        
        var pairing1 = new RollPairing
        {
            AttackerRoll = round1.AttackerRolls[0],
            DefenderRoll = round1.DefenderRolls[0],
            WinnerArmyId = "army_attacker_1",
            IsDiscarded = false
        };
        
        var pairing2 = new RollPairing
        {
            AttackerRoll = round1.AttackerRolls[1],
            DefenderRoll = round1.DefenderRolls[1],
            WinnerArmyId = "army_attacker_1",
            IsDiscarded = false
        };
        
        var discardedPairing = new RollPairing
        {
            AttackerRoll = round1.AttackerRolls[2],
            DefenderRoll = null,
            WinnerArmyId = "",
            IsDiscarded = true
        };
        
        round1.Pairings.Add(pairing1);
        round1.Pairings.Add(pairing2);
        round1.Pairings.Add(discardedPairing);
        
        round1.Casualties.Add(new ArmyCasualty
        {
            ArmyId = "army_attacker_1",
            PlayerId = "player_1",
            CombatRole = "Attacker",
            Casualties = 0,
            RemainingUnits = 3
        });
        
        round1.Casualties.Add(new ArmyCasualty
        {
            ArmyId = "army_defender_1",
            PlayerId = "player_2",
            CombatRole = "Defender",
            Casualties = 2,
            RemainingUnits = 0
        });
        
        combatEvent.RoundResults.Add(round1);
        
        return combatEvent;
    }

    public static CombatEvent CreateMultiRoundCombatEvent()
    {
        var combatEvent = new CombatEvent
        {
            EventId = "combat_002",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            EventType = CombatEvent.Types.CombatEventType.CombatEnded,
            LocationId = "region_beta_5"
        };

        combatEvent.ArmyStates.Add(new CombatArmyState
        {
            ArmyId = "army_attacker_1",
            PlayerId = "player_1",
            CombatRole = "Attacker",
            UnitCount = 2
        });

        combatEvent.ArmyStates.Add(new CombatArmyState
        {
            ArmyId = "army_defender_1",
            PlayerId = "player_2",
            CombatRole = "Defender",
            UnitCount = 1
        });

        for (int roundNum = 0; roundNum < 2; roundNum++)
        {
            var round = new CombatRoundResult();
            
            int attackerCount = roundNum == 0 ? 3 : 2;
            int defenderCount = roundNum == 0 ? 3 : 2;
            
            for (int i = 0; i < attackerCount; i++)
            {
                round.AttackerRolls.Add(new DiceRoll 
                { 
                    ArmyId = "army_attacker_1", 
                    Roll = Random.Shared.Next(1, 7), 
                    UnitIndex = i 
                });
            }
            
            for (int i = 0; i < defenderCount; i++)
            {
                round.DefenderRolls.Add(new DiceRoll 
                { 
                    ArmyId = "army_defender_1", 
                    Roll = Random.Shared.Next(1, 7), 
                    UnitIndex = i 
                });
            }
            
            int pairCount = Math.Min(attackerCount, defenderCount);
            for (int i = 0; i < pairCount; i++)
            {
                var attackRoll = round.AttackerRolls[i];
                var defendRoll = round.DefenderRolls[i];
                
                round.Pairings.Add(new RollPairing
                {
                    AttackerRoll = attackRoll,
                    DefenderRoll = defendRoll,
                    WinnerArmyId = attackRoll.Roll > defendRoll.Roll ? "army_attacker_1" : "army_defender_1",
                    IsDiscarded = false
                });
            }
            
            round.Casualties.Add(new ArmyCasualty
            {
                ArmyId = "army_attacker_1",
                PlayerId = "player_1",
                CombatRole = "Attacker",
                Casualties = roundNum == 0 ? 1 : 0,
                RemainingUnits = roundNum == 0 ? 2 : 2
            });
            
            round.Casualties.Add(new ArmyCasualty
            {
                ArmyId = "army_defender_1",
                PlayerId = "player_2",
                CombatRole = "Defender",
                Casualties = roundNum == 0 ? 1 : 1,
                RemainingUnits = roundNum == 0 ? 2 : 1
            });
            
            combatEvent.RoundResults.Add(round);
        }
        
        return combatEvent;
    }

    public static CombatEvent CreateReinforcementEvent()
    {
        var combatEvent = new CombatEvent
        {
            EventId = "combat_003",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            EventType = CombatEvent.Types.CombatEventType.ReinforcementsArrived,
            LocationId = "region_gamma_7"
        };

        combatEvent.ArmyStates.Add(new CombatArmyState
        {
            ArmyId = "army_attacker_1",
            PlayerId = "player_1",
            CombatRole = "Attacker",
            UnitCount = 5
        });

        combatEvent.ArmyStates.Add(new CombatArmyState
        {
            ArmyId = "army_defender_1",
            PlayerId = "player_2",
            CombatRole = "Defender",
            UnitCount = 4
        });

        combatEvent.ArmyStates.Add(new CombatArmyState
        {
            ArmyId = "army_attacker_2",
            PlayerId = "player_1",
            CombatRole = "Attacker",
            UnitCount = 3
        });

        var round = new CombatRoundResult();
        
        round.AttackerRolls.Add(new DiceRoll { ArmyId = "army_attacker_1", Roll = 5, UnitIndex = 0 });
        round.DefenderRolls.Add(new DiceRoll { ArmyId = "army_defender_1", Roll = 4, UnitIndex = 0 });
        
        round.Pairings.Add(new RollPairing
        {
            AttackerRoll = round.AttackerRolls[0],
            DefenderRoll = round.DefenderRolls[0],
            WinnerArmyId = "army_attacker_1",
            IsDiscarded = false
        });
        
        round.Casualties.Add(new ArmyCasualty
        {
            ArmyId = "army_attacker_1",
            PlayerId = "player_1",
            CombatRole = "Attacker",
            Casualties = 0,
            RemainingUnits = 5
        });
        
        round.Casualties.Add(new ArmyCasualty
        {
            ArmyId = "army_attacker_2",
            PlayerId = "player_1",
            CombatRole = "Attacker",
            Casualties = 0,
            RemainingUnits = 3
        });
        
        round.Casualties.Add(new ArmyCasualty
        {
            ArmyId = "army_defender_1",
            PlayerId = "player_2",
            CombatRole = "Defender",
            Casualties = 1,
            RemainingUnits = 3
        });
        
        combatEvent.RoundResults.Add(round);
        
        return combatEvent;
    }
}
