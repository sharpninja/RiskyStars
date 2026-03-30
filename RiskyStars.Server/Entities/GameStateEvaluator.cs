namespace RiskyStars.Server.Entities;

public class GameStateEvaluator
{
    private const double PopulationWeight = 1.5;
    private const double MetalWeight = 1.2;
    private const double FuelWeight = 1.0;
    private const double RegionCountWeight = 100.0;

    private const double TerritoryWeight = 0.30;
    private const double StrategicWeight = 0.25;
    private const double MilitaryWeight = 0.25;
    private const double EconomicWeight = 0.20;

    public double EvaluateTerritoryValue(Game game, string playerId)
    {
        var player = game.Players.FirstOrDefault(p => p.Id == playerId);
        if (player == null)
            return 0.0;

        var ownedRegions = game.GetAllRegions().Where(r => r.OwnerId == playerId).ToList();
        var regionCount = ownedRegions.Count;

        double resourceTypeScore = 0.0;
        var regionsByResourceType = new Dictionary<ResourceType, int>
        {
            { ResourceType.Population, 0 },
            { ResourceType.Metal, 0 },
            { ResourceType.Fuel, 0 }
        };

        foreach (var region in ownedRegions)
        {
            var stellarBody = game.StarSystems
                .SelectMany(s => s.StellarBodies)
                .FirstOrDefault(b => b.Id == region.StellarBodyId);

            if (stellarBody != null)
            {
                regionsByResourceType[stellarBody.ResourceType]++;
            }
        }

        resourceTypeScore += regionsByResourceType[ResourceType.Population] * PopulationWeight;
        resourceTypeScore += regionsByResourceType[ResourceType.Metal] * MetalWeight;
        resourceTypeScore += regionsByResourceType[ResourceType.Fuel] * FuelWeight;

        double regionScore = regionCount * RegionCountWeight;

        double diversityBonus = 0.0;
        int resourceTypesPresent = regionsByResourceType.Count(kvp => kvp.Value > 0);
        if (resourceTypesPresent == 3)
        {
            diversityBonus = regionCount * 50.0;
        }
        else if (resourceTypesPresent == 2)
        {
            diversityBonus = regionCount * 20.0;
        }

        return regionScore + resourceTypeScore + diversityBonus;
    }

    public double EvaluateStrategicPosition(Game game, string playerId)
    {
        var player = game.Players.FirstOrDefault(p => p.Id == playerId);
        if (player == null)
            return 0.0;

        double strategicScore = 0.0;

        var ownedRegions = game.GetAllRegions().Where(r => r.OwnerId == playerId).ToList();
        var allRegions = game.GetAllRegions().ToList();

        int frontlineRegions = 0;
        int interiorRegions = 0;

        foreach (var region in ownedRegions)
        {
            var stellarBody = game.StarSystems
                .SelectMany(s => s.StellarBodies)
                .FirstOrDefault(b => b.Regions.Any(r => r.Id == region.Id));

            if (stellarBody == null)
                continue;

            var starSystem = game.StarSystems.FirstOrDefault(s => s.Id == stellarBody.StarSystemId);
            if (starSystem == null)
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

            if (hasEnemyNeighbor)
            {
                frontlineRegions++;
            }
            else
            {
                interiorRegions++;
            }
        }

        double frontlineExposure = frontlineRegions > 0 
            ? (double)frontlineRegions / ownedRegions.Count 
            : 0.0;
        
        double exposurePenalty = frontlineExposure * -500.0;

        double interiorBonus = interiorRegions * 75.0;

        var ownedLaneMouths = game.GetAllHyperspaceLanes()
            .SelectMany(lane => new[]
            {
                new { MouthId = lane.MouthAId, OwnerId = lane.MouthAOwnerId },
                new { MouthId = lane.MouthBId, OwnerId = lane.MouthBOwnerId }
            })
            .Where(m => m.OwnerId == playerId)
            .Count();

        double laneControlBonus = ownedLaneMouths * 200.0;

        var controlledLanes = game.GetAllHyperspaceLanes()
            .Where(lane => lane.MouthAOwnerId == playerId && lane.MouthBOwnerId == playerId)
            .Count();

        double fullLaneControlBonus = controlledLanes * 500.0;

        strategicScore = exposurePenalty + interiorBonus + laneControlBonus + fullLaneControlBonus;

        return strategicScore;
    }

    public double EvaluateMilitaryStrength(Game game, string playerId)
    {
        var player = game.Players.FirstOrDefault(p => p.Id == playerId);
        if (player == null)
            return 0.0;

        var playerArmies = game.GetAllArmies().Where(a => a.OwnerId == playerId).ToList();
        var totalUnits = playerArmies.Sum(a => a.UnitCount);

        var allArmies = game.GetAllArmies().ToList();
        var totalEnemyUnits = allArmies.Where(a => a.OwnerId != playerId).Sum(a => a.UnitCount);

        double armySizeScore = totalUnits * 50.0;

        double relativePowerBonus = 0.0;
        if (totalEnemyUnits > 0)
        {
            double powerRatio = (double)totalUnits / totalEnemyUnits;
            relativePowerBonus = powerRatio * 1000.0;
        }
        else
        {
            relativePowerBonus = 2000.0;
        }

        var frontlineArmies = 0;
        var reserveArmies = 0;

        foreach (var army in playerArmies)
        {
            bool isFrontline = false;

            if (army.LocationType == LocationType.HyperspaceLaneMouth)
            {
                isFrontline = true;
            }
            else if (army.LocationType == LocationType.Region)
            {
                var region = game.GetAllRegions().FirstOrDefault(r => r.Id == army.LocationId);
                if (region != null)
                {
                    var stellarBody = game.StarSystems
                        .SelectMany(s => s.StellarBodies)
                        .FirstOrDefault(b => b.Regions.Any(r => r.Id == region.Id));

                    if (stellarBody != null)
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
                                    var hasEnemyInSystem = oppositeSystem.StellarBodies
                                        .SelectMany(b => b.Regions)
                                        .Any(r => r.OwnerId != playerId && r.OwnerId != null);

                                    if (hasEnemyInSystem)
                                    {
                                        isFrontline = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (isFrontline)
                frontlineArmies += army.UnitCount;
            else
                reserveArmies += army.UnitCount;
        }

        double positionBonus = frontlineArmies * 30.0 + reserveArmies * 15.0;

        return armySizeScore + relativePowerBonus + positionBonus;
    }

    public double EvaluateEconomicPower(Game game, string playerId)
    {
        var player = game.Players.FirstOrDefault(p => p.Id == playerId);
        if (player == null)
            return 0.0;

        int totalPopulation = player.PopulationStockpile;
        int totalMetal = player.MetalStockpile;
        int totalFuel = player.FuelStockpile;

        double stockpileScore = 
            totalPopulation * 2.0 + 
            totalMetal * 5.0 + 
            totalFuel * 3.0;

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

        double productionScore = 
            populationProduction * 3.0 + 
            metalProduction * 7.0 + 
            fuelProduction * 5.0;

        int upgradeCount = ownedBodies.Sum(b => b.UpgradeLevel);
        double upgradeBonus = upgradeCount * 500.0;

        double balanceBonus = 0.0;
        if (populationProduction > 0 && metalProduction > 0 && fuelProduction > 0)
        {
            int minProduction = Math.Min(populationProduction, Math.Min(metalProduction, fuelProduction));
            balanceBonus = minProduction * 5.0;
        }

        return stockpileScore + productionScore + upgradeBonus + balanceBonus;
    }

    public double EvaluateOverallPosition(Game game, string playerId)
    {
        double territoryScore = EvaluateTerritoryValue(game, playerId);
        double strategicScore = EvaluateStrategicPosition(game, playerId);
        double militaryScore = EvaluateMilitaryStrength(game, playerId);
        double economicScore = EvaluateEconomicPower(game, playerId);

        double weightedScore = 
            territoryScore * TerritoryWeight +
            strategicScore * StrategicWeight +
            militaryScore * MilitaryWeight +
            economicScore * EconomicWeight;

        return weightedScore;
    }
}
