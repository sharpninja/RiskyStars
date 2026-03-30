using RiskyStars.Server.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RiskyStars.Server.Services;

public class AIPurchaseDecisionMaker
{
    private const int PopulationCostPerArmy = 10;
    private const int MetalCostPerArmy = 3;
    private const int FuelCostPerArmy = 3;

    private readonly CombatPredictor _combatPredictor;
    private readonly ResourceManager _resourceManager;

    public AIPurchaseDecisionMaker(CombatPredictor combatPredictor, ResourceManager resourceManager)
    {
        _combatPredictor = combatPredictor;
        _resourceManager = resourceManager;
    }

    public int DetermineOptimalPurchaseCount(Game game, AIPlayer aiPlayer)
    {
        var maxAffordable = CalculateMaxAffordableArmies(aiPlayer);
        
        if (maxAffordable == 0)
            return 0;

        var productionRate = CalculateProductionRate(game, aiPlayer.Id);
        var sustainableSpending = CalculateSustainableSpending(aiPlayer, productionRate);
        
        var threatLevel = AssessThreatLevel(game, aiPlayer);
        
        var spendingAggressiveness = GetSpendingAggressiveness(aiPlayer.DifficultyLevel);
        
        var optimalCount = CalculateOptimalPurchaseCount(
            maxAffordable,
            sustainableSpending,
            threatLevel,
            spendingAggressiveness,
            aiPlayer.DifficultyLevel
        );

        return Math.Max(0, Math.Min(optimalCount, maxAffordable));
    }

    private int CalculateMaxAffordableArmies(Player player)
    {
        var populationLimit = player.PopulationStockpile / PopulationCostPerArmy;
        var metalLimit = player.MetalStockpile / MetalCostPerArmy;
        var fuelLimit = player.FuelStockpile / FuelCostPerArmy;

        return Math.Min(populationLimit, Math.Min(metalLimit, fuelLimit));
    }

    private (int population, int metal, int fuel) CalculateProductionRate(Game game, string playerId)
    {
        var ownedBodies = game.GetPlayerOwnedBodies(playerId).ToList();

        int populationProduction = 0;
        int metalProduction = 0;
        int fuelProduction = 0;

        foreach (var body in ownedBodies)
        {
            int production = body.CalculateTotalProduction();
            switch (body.ResourceType)
            {
                case ResourceType.Population:
                    populationProduction += production;
                    break;
                case ResourceType.Metal:
                    metalProduction += production;
                    break;
                case ResourceType.Fuel:
                    fuelProduction += production;
                    break;
            }
        }

        return (populationProduction, metalProduction, fuelProduction);
    }

    private int CalculateSustainableSpending(Player player, (int population, int metal, int fuel) productionRate)
    {
        if (productionRate.population == 0 || productionRate.metal == 0 || productionRate.fuel == 0)
            return 0;

        var turnsOfPopulationReserve = player.PopulationStockpile / (double)productionRate.population;
        var turnsOfMetalReserve = player.MetalStockpile / (double)productionRate.metal;
        var turnsOfFuelReserve = player.FuelStockpile / (double)productionRate.fuel;

        var populationSustainable = productionRate.population / PopulationCostPerArmy;
        var metalSustainable = productionRate.metal / MetalCostPerArmy;
        var fuelSustainable = productionRate.fuel / FuelCostPerArmy;

        var sustainableArmiesPerTurn = Math.Min(populationSustainable, Math.Min(metalSustainable, fuelSustainable));

        var minReserveTurns = Math.Min(turnsOfPopulationReserve, Math.Min(turnsOfMetalReserve, turnsOfFuelReserve));

        if (minReserveTurns < 2.0)
        {
            return (int)(sustainableArmiesPerTurn * 0.5);
        }
        else if (minReserveTurns < 4.0)
        {
            return sustainableArmiesPerTurn;
        }
        else
        {
            return (int)(sustainableArmiesPerTurn * Math.Min(3.0, minReserveTurns / 2.0));
        }
    }

    private ThreatAssessment AssessThreatLevel(Game game, AIPlayer aiPlayer)
    {
        var playerArmies = game.GetAllArmies().Where(a => a.OwnerId == aiPlayer.Id).ToList();
        var totalPlayerUnits = playerArmies.Sum(a => a.UnitCount);

        var enemyArmies = game.GetAllArmies().Where(a => a.OwnerId != aiPlayer.Id).ToList();
        var totalEnemyUnits = enemyArmies.Sum(a => a.UnitCount);

        if (totalEnemyUnits == 0)
        {
            return new ThreatAssessment
            {
                Level = ThreatLevel.None,
                NearbyEnemyUnits = 0,
                TotalEnemyUnits = 0,
                PowerRatio = double.PositiveInfinity,
                RecommendedPurchaseMultiplier = 0.5
            };
        }

        var nearbyEnemyUnits = CountNearbyEnemyUnits(game, aiPlayer.Id);
        var powerRatio = totalPlayerUnits / (double)totalEnemyUnits;

        var frontlineRegions = GetFrontlineRegions(game, aiPlayer.Id);
        var frontlineArmies = playerArmies.Where(a => 
            a.LocationType == LocationType.HyperspaceLaneMouth ||
            (a.LocationType == LocationType.Region && frontlineRegions.Contains(a.LocationId))
        ).ToList();
        var frontlineUnits = frontlineArmies.Sum(a => a.UnitCount);

        ThreatLevel level;
        double purchaseMultiplier;

        if (powerRatio < 0.5)
        {
            level = ThreatLevel.Critical;
            purchaseMultiplier = 2.0;
        }
        else if (powerRatio < 0.8 || nearbyEnemyUnits > frontlineUnits)
        {
            level = ThreatLevel.High;
            purchaseMultiplier = 1.5;
        }
        else if (powerRatio < 1.2)
        {
            level = ThreatLevel.Moderate;
            purchaseMultiplier = 1.0;
        }
        else if (powerRatio < 2.0)
        {
            level = ThreatLevel.Low;
            purchaseMultiplier = 0.75;
        }
        else
        {
            level = ThreatLevel.None;
            purchaseMultiplier = 0.5;
        }

        return new ThreatAssessment
        {
            Level = level,
            NearbyEnemyUnits = nearbyEnemyUnits,
            TotalEnemyUnits = totalEnemyUnits,
            PowerRatio = powerRatio,
            RecommendedPurchaseMultiplier = purchaseMultiplier
        };
    }

    private int CountNearbyEnemyUnits(Game game, string playerId)
    {
        var nearbyEnemyUnits = 0;
        var frontlineRegions = GetFrontlineRegions(game, playerId);

        foreach (var region in game.GetAllRegions().Where(r => frontlineRegions.Contains(r.Id)))
        {
            var stellarBody = game.StarSystems
                .SelectMany(s => s.StellarBodies)
                .FirstOrDefault(b => b.Regions.Any(r => r.Id == region.Id));

            if (stellarBody == null)
                continue;

            foreach (var otherRegion in stellarBody.Regions)
            {
                if (otherRegion.OwnerId != playerId && otherRegion.OwnerId != null && otherRegion.Army != null)
                {
                    nearbyEnemyUnits += otherRegion.Army.UnitCount;
                }
            }

            var starSystem = game.StarSystems.FirstOrDefault(s => s.Id == stellarBody.StarSystemId);
            if (starSystem != null)
            {
                foreach (var lane in starSystem.HyperspaceLanes)
                {
                    var oppositeSystemId = lane.GetOppositeStarSystemId(starSystem.Id);
                    var oppositeSystem = game.StarSystems.FirstOrDefault(s => s.Id == oppositeSystemId);

                    if (oppositeSystem != null)
                    {
                        var enemyUnitsInSystem = oppositeSystem.StellarBodies
                            .SelectMany(b => b.Regions)
                            .Where(r => r.OwnerId != playerId && r.OwnerId != null && r.Army != null)
                            .Sum(r => r.Army!.UnitCount);

                        nearbyEnemyUnits += enemyUnitsInSystem;
                    }

                    if (lane.MouthAArmy != null && lane.MouthAArmy.OwnerId != playerId)
                    {
                        nearbyEnemyUnits += lane.MouthAArmy.UnitCount;
                    }

                    if (lane.MouthBArmy != null && lane.MouthBArmy.OwnerId != playerId)
                    {
                        nearbyEnemyUnits += lane.MouthBArmy.UnitCount;
                    }
                }
            }
        }

        return nearbyEnemyUnits;
    }

    private HashSet<string> GetFrontlineRegions(Game game, string playerId)
    {
        var frontlineRegions = new HashSet<string>();
        var ownedRegions = game.GetAllRegions().Where(r => r.OwnerId == playerId).ToList();

        foreach (var region in ownedRegions)
        {
            var stellarBody = game.StarSystems
                .SelectMany(s => s.StellarBodies)
                .FirstOrDefault(b => b.Regions.Any(r => r.Id == region.Id));

            if (stellarBody == null)
                continue;

            bool hasEnemyNeighbor = false;

            foreach (var otherRegion in stellarBody.Regions)
            {
                if (otherRegion.OwnerId != playerId && otherRegion.OwnerId != null)
                {
                    hasEnemyNeighbor = true;
                    break;
                }
            }

            if (!hasEnemyNeighbor)
            {
                var starSystem = game.StarSystems.FirstOrDefault(s => s.Id == stellarBody.StarSystemId);
                if (starSystem != null)
                {
                    foreach (var lane in starSystem.HyperspaceLanes)
                    {
                        var oppositeSystemId = lane.GetOppositeStarSystemId(starSystem.Id);
                        var oppositeSystem = game.StarSystems.FirstOrDefault(s => s.Id == oppositeSystemId);

                        if (oppositeSystem != null)
                        {
                            var oppositeRegions = oppositeSystem.StellarBodies
                                .SelectMany(b => b.Regions)
                                .Where(r => r.OwnerId != playerId && r.OwnerId != null);

                            if (oppositeRegions.Any())
                            {
                                hasEnemyNeighbor = true;
                                break;
                            }
                        }
                    }
                }
            }

            if (hasEnemyNeighbor)
            {
                frontlineRegions.Add(region.Id);
            }
        }

        return frontlineRegions;
    }

    private double GetSpendingAggressiveness(DifficultyLevel difficulty)
    {
        return difficulty switch
        {
            DifficultyLevel.Easy => 0.5,
            DifficultyLevel.Medium => 0.7,
            DifficultyLevel.Hard => 0.9,
            _ => 0.5
        };
    }

    private int CalculateOptimalPurchaseCount(
        int maxAffordable,
        int sustainableSpending,
        ThreatAssessment threat,
        double spendingAggressiveness,
        DifficultyLevel difficulty)
    {
        var basePurchaseCount = (int)(maxAffordable * spendingAggressiveness);

        var threatAdjustedCount = (int)(basePurchaseCount * threat.RecommendedPurchaseMultiplier);

        var sustainabilityLimit = sustainableSpending * GetSustainabilityHorizon(difficulty);

        var optimalCount = Math.Min(threatAdjustedCount, sustainabilityLimit);

        if (threat.Level == ThreatLevel.Critical)
        {
            optimalCount = Math.Max(optimalCount, (int)(maxAffordable * 0.8));
        }

        if (sustainableSpending > 0 && optimalCount < sustainableSpending / 2)
        {
            optimalCount = Math.Max(optimalCount, sustainableSpending / 2);
        }

        return Math.Min(optimalCount, maxAffordable);
    }

    private int GetSustainabilityHorizon(DifficultyLevel difficulty)
    {
        return difficulty switch
        {
            DifficultyLevel.Easy => 2,
            DifficultyLevel.Medium => 3,
            DifficultyLevel.Hard => 4,
            _ => 2
        };
    }

    public PurchaseDecision MakePurchaseDecision(Game game, AIPlayer aiPlayer)
    {
        var optimalCount = DetermineOptimalPurchaseCount(game, aiPlayer);
        var threat = AssessThreatLevel(game, aiPlayer);
        var productionRate = CalculateProductionRate(game, aiPlayer.Id);
        var sustainableSpending = CalculateSustainableSpending(aiPlayer, productionRate);

        return new PurchaseDecision
        {
            RecommendedPurchaseCount = optimalCount,
            MaxAffordable = CalculateMaxAffordableArmies(aiPlayer),
            SustainableSpending = sustainableSpending,
            ThreatAssessment = threat,
            SpendingAggressiveness = GetSpendingAggressiveness(aiPlayer.DifficultyLevel)
        };
    }
}

public class ThreatAssessment
{
    public ThreatLevel Level { get; set; }
    public int NearbyEnemyUnits { get; set; }
    public int TotalEnemyUnits { get; set; }
    public double PowerRatio { get; set; }
    public double RecommendedPurchaseMultiplier { get; set; }
}

public enum ThreatLevel
{
    None,
    Low,
    Moderate,
    High,
    Critical
}

public class PurchaseDecision
{
    public int RecommendedPurchaseCount { get; set; }
    public int MaxAffordable { get; set; }
    public int SustainableSpending { get; set; }
    public ThreatAssessment ThreatAssessment { get; set; } = new();
    public double SpendingAggressiveness { get; set; }
}
