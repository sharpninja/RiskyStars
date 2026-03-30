using RiskyStars.Server.Entities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace RiskyStars.Server.Services;

public class CombatPredictor
{
    private readonly ConcurrentDictionary<string, CombatPrediction> _predictionCache;
    private readonly object _lockObject = new object();

    public CombatPredictor()
    {
        _predictionCache = new ConcurrentDictionary<string, CombatPrediction>();
    }

    public CombatPrediction PredictCombat(
        int attackerUnitCount,
        int defenderUnitCount,
        DifficultyLevel difficulty)
    {
        var cacheKey = GetCacheKey(attackerUnitCount, defenderUnitCount, difficulty);
        
        if (_predictionCache.TryGetValue(cacheKey, out var cachedPrediction))
        {
            return cachedPrediction;
        }

        var iterations = GetIterationCount(difficulty);
        var prediction = RunMonteCarloSimulation(attackerUnitCount, defenderUnitCount, iterations);
        
        _predictionCache.TryAdd(cacheKey, prediction);
        
        return prediction;
    }

    public void ClearCache()
    {
        _predictionCache.Clear();
    }

    private string GetCacheKey(int attackerUnitCount, int defenderUnitCount, DifficultyLevel difficulty)
    {
        return $"{attackerUnitCount}:{defenderUnitCount}:{difficulty}";
    }

    private int GetIterationCount(DifficultyLevel difficulty)
    {
        return difficulty switch
        {
            DifficultyLevel.Easy => 10,
            DifficultyLevel.Medium => 100,
            DifficultyLevel.Hard => 1000,
            _ => 100
        };
    }

    private CombatPrediction RunMonteCarloSimulation(
        int attackerUnitCount,
        int defenderUnitCount,
        int iterations)
    {
        int attackerWins = 0;
        double totalAttackerCasualties = 0;
        double totalDefenderCasualties = 0;
        double totalAttackerRemainingUnits = 0;
        double totalDefenderRemainingUnits = 0;

        for (int i = 0; i < iterations; i++)
        {
            var result = SimulateSingleCombat(attackerUnitCount, defenderUnitCount);
            
            if (result.AttackerRemainingUnits > 0)
            {
                attackerWins++;
            }
            
            totalAttackerCasualties += result.AttackerCasualties;
            totalDefenderCasualties += result.DefenderCasualties;
            totalAttackerRemainingUnits += result.AttackerRemainingUnits;
            totalDefenderRemainingUnits += result.DefenderRemainingUnits;
        }

        return new CombatPrediction
        {
            AttackerWinProbability = (double)attackerWins / iterations,
            DefenderWinProbability = 1.0 - ((double)attackerWins / iterations),
            ExpectedAttackerCasualties = totalAttackerCasualties / iterations,
            ExpectedDefenderCasualties = totalDefenderCasualties / iterations,
            ExpectedAttackerRemainingUnits = totalAttackerRemainingUnits / iterations,
            ExpectedDefenderRemainingUnits = totalDefenderRemainingUnits / iterations
        };
    }

    private SimulationResult SimulateSingleCombat(int attackerUnits, int defenderUnits)
    {
        var random = new Random(Guid.NewGuid().GetHashCode());
        int initialAttackerUnits = attackerUnits;
        int initialDefenderUnits = defenderUnits;

        while (attackerUnits > 0 && defenderUnits > 0)
        {
            var attackerRolls = new List<int>();
            var defenderRolls = new List<int>();

            for (int i = 0; i < attackerUnits; i++)
            {
                attackerRolls.Add(random.Next(1, 11));
            }

            for (int i = 0; i < defenderUnits; i++)
            {
                defenderRolls.Add(random.Next(1, 11));
            }

            attackerRolls = attackerRolls.OrderByDescending(r => r).ToList();
            defenderRolls = defenderRolls.OrderByDescending(r => r).ToList();

            int pairCount = Math.Min(attackerRolls.Count, defenderRolls.Count);
            int attackerCasualtiesThisRound = 0;
            int defenderCasualtiesThisRound = 0;

            for (int i = 0; i < pairCount; i++)
            {
                if (attackerRolls[i] > defenderRolls[i])
                {
                    defenderCasualtiesThisRound++;
                }
                else
                {
                    attackerCasualtiesThisRound++;
                }
            }

            attackerUnits -= attackerCasualtiesThisRound;
            defenderUnits -= defenderCasualtiesThisRound;
        }

        return new SimulationResult
        {
            AttackerCasualties = initialAttackerUnits - attackerUnits,
            DefenderCasualties = initialDefenderUnits - defenderUnits,
            AttackerRemainingUnits = attackerUnits,
            DefenderRemainingUnits = defenderUnits
        };
    }
}

public class CombatPrediction
{
    public double AttackerWinProbability { get; set; }
    public double DefenderWinProbability { get; set; }
    public double ExpectedAttackerCasualties { get; set; }
    public double ExpectedDefenderCasualties { get; set; }
    public double ExpectedAttackerRemainingUnits { get; set; }
    public double ExpectedDefenderRemainingUnits { get; set; }
}

internal class SimulationResult
{
    public int AttackerCasualties { get; set; }
    public int DefenderCasualties { get; set; }
    public int AttackerRemainingUnits { get; set; }
    public int DefenderRemainingUnits { get; set; }
}
