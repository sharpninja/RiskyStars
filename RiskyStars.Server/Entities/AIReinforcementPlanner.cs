namespace RiskyStars.Server.Entities;

public class AIReinforcementPlanner
{
    private readonly GameStateEvaluator _evaluator;
    private readonly Random _random;

    public AIReinforcementPlanner(GameStateEvaluator evaluator, Random? random = null)
    {
        _evaluator = evaluator;
        _random = random ?? new Random();
    }

    public Dictionary<string, int> AllocateReinforcements(
        Game game, 
        string playerId, 
        int availableArmies,
        DifficultyLevel difficultyLevel)
    {
        var allocations = new Dictionary<string, int>();
        
        if (availableArmies <= 0)
        {
            return allocations;
        }

        var playerRegions = game.GetAllRegions()
            .Where(r => r.OwnerId == playerId)
            .ToList();

        if (playerRegions.Count == 0)
        {
            return allocations;
        }

        var threatAssessments = AssessThreatLevels(game, playerId, playerRegions);
        
        var prioritizedRegions = PrioritizeRegionsByDifficulty(
            threatAssessments, 
            difficultyLevel);

        int remainingArmies = availableArmies;
        
        foreach (var (regionId, priority) in prioritizedRegions)
        {
            if (remainingArmies <= 0)
            {
                break;
            }

            var region = playerRegions.First(r => r.Id == regionId);
            var assessment = threatAssessments[regionId];
            
            int currentGarrison = region.Army?.UnitCount ?? 0;
            int minimumGarrison = CalculateMinimumGarrison(game, region);
            int desiredGarrison = CalculateDesiredGarrison(assessment, minimumGarrison);
            
            int armiesNeeded = Math.Max(0, desiredGarrison - currentGarrison);
            int armiesToAllocate = Math.Min(armiesNeeded, remainingArmies);
            
            if (armiesToAllocate > 0)
            {
                allocations[regionId] = armiesToAllocate;
                remainingArmies -= armiesToAllocate;
            }
        }

        if (remainingArmies > 0)
        {
            DistributeRemainingArmies(
                allocations, 
                playerRegions, 
                remainingArmies, 
                threatAssessments);
        }

        return allocations;
    }

    private Dictionary<string, ThreatAssessment> AssessThreatLevels(
        Game game, 
        string playerId, 
        List<Region> playerRegions)
    {
        var assessments = new Dictionary<string, ThreatAssessment>();
        
        foreach (var region in playerRegions)
        {
            var assessment = new ThreatAssessment
            {
                RegionId = region.Id,
                IsFrontline = IsFrontlineRegion(game, region, playerId),
                RegionValue = CalculateRegionValue(game, region),
                EnemyThreat = CalculateEnemyThreat(game, region, playerId),
                StrategicImportance = CalculateStrategicImportance(game, region, playerId)
            };
            
            assessment.ThreatScore = CalculateThreatScore(assessment);
            assessments[region.Id] = assessment;
        }

        return assessments;
    }

    private bool IsFrontlineRegion(Game game, Region region, string playerId)
    {
        var stellarBody = game.StarSystems
            .SelectMany(s => s.StellarBodies)
            .FirstOrDefault(b => b.Regions.Any(r => r.Id == region.Id));

        if (stellarBody == null)
        {
            return false;
        }

        foreach (var otherRegion in stellarBody.Regions)
        {
            if (otherRegion.OwnerId != playerId && otherRegion.OwnerId != null)
            {
                return true;
            }
        }

        var starSystem = game.StarSystems.FirstOrDefault(s => s.Id == stellarBody.StarSystemId);
        if (starSystem == null)
        {
            return false;
        }

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
                    return true;
                }
            }
        }

        return false;
    }

    private double CalculateRegionValue(Game game, Region region)
    {
        var stellarBody = game.StarSystems
            .SelectMany(s => s.StellarBodies)
            .FirstOrDefault(b => b.Regions.Any(r => r.Id == region.Id));

        if (stellarBody == null)
        {
            return 1.0;
        }

        double baseValue = Region.BaseProductionPerRegion;
        double upgradeMultiplier = stellarBody.GetYieldMultiplier();
        
        double resourceTypeMultiplier = stellarBody.ResourceType switch
        {
            ResourceType.Population => 1.5,
            ResourceType.Metal => 1.2,
            ResourceType.Fuel => 1.0,
            _ => 1.0
        };

        return baseValue * upgradeMultiplier * resourceTypeMultiplier;
    }

    private double CalculateEnemyThreat(Game game, Region region, string playerId)
    {
        double threat = 0.0;

        var stellarBody = game.StarSystems
            .SelectMany(s => s.StellarBodies)
            .FirstOrDefault(b => b.Regions.Any(r => r.Id == region.Id));

        if (stellarBody == null)
        {
            return threat;
        }

        foreach (var otherRegion in stellarBody.Regions)
        {
            if (otherRegion.OwnerId != playerId && otherRegion.OwnerId != null)
            {
                int enemyUnits = otherRegion.Army?.UnitCount ?? 0;
                threat += enemyUnits * 1.5;
            }
        }

        var starSystem = game.StarSystems.FirstOrDefault(s => s.Id == stellarBody.StarSystemId);
        if (starSystem == null)
        {
            return threat;
        }

        foreach (var lane in starSystem.HyperspaceLanes)
        {
            var oppositeSystemId = lane.GetOppositeStarSystemId(starSystem.Id);
            var oppositeSystem = game.StarSystems.FirstOrDefault(s => s.Id == oppositeSystemId);

            if (oppositeSystem != null)
            {
                var enemyRegions = oppositeSystem.StellarBodies
                    .SelectMany(b => b.Regions)
                    .Where(r => r.OwnerId != playerId && r.OwnerId != null);

                foreach (var enemyRegion in enemyRegions)
                {
                    int enemyUnits = enemyRegion.Army?.UnitCount ?? 0;
                    threat += enemyUnits * 1.0;
                }
            }

            var mouthAOwnerId = lane.MouthAOwnerId;
            var mouthBOwnerId = lane.MouthBOwnerId;

            if (mouthAOwnerId != playerId && mouthAOwnerId != null)
            {
                int armyUnits = lane.MouthAArmy?.UnitCount ?? 0;
                threat += armyUnits * 2.0;
            }

            if (mouthBOwnerId != playerId && mouthBOwnerId != null)
            {
                int armyUnits = lane.MouthBArmy?.UnitCount ?? 0;
                threat += armyUnits * 2.0;
            }
        }

        return threat;
    }

    private double CalculateStrategicImportance(Game game, Region region, string playerId)
    {
        double importance = 0.0;

        var stellarBody = game.StarSystems
            .SelectMany(s => s.StellarBodies)
            .FirstOrDefault(b => b.Regions.Any(r => r.Id == region.Id));

        if (stellarBody == null)
        {
            return importance;
        }

        var starSystem = game.StarSystems.FirstOrDefault(s => s.Id == stellarBody.StarSystemId);
        if (starSystem == null)
        {
            return importance;
        }

        int laneCount = starSystem.HyperspaceLanes.Count;
        importance += laneCount * 100.0;

        var ownedRegionsInSystem = stellarBody.Regions.Count(r => r.OwnerId == playerId);
        var totalRegionsInSystem = stellarBody.Regions.Count;
        
        if (ownedRegionsInSystem == totalRegionsInSystem)
        {
            importance += 200.0;
        }

        if (starSystem.Type == StarSystemType.Home)
        {
            importance += 500.0;
        }
        else if (starSystem.Type == StarSystemType.Featured)
        {
            importance += 300.0;
        }

        return importance;
    }

    private double CalculateThreatScore(ThreatAssessment assessment)
    {
        double score = 0.0;

        if (assessment.IsFrontline)
        {
            score += 1000.0;
        }

        score += assessment.EnemyThreat * 2.0;
        score += assessment.RegionValue * 0.5;
        score += assessment.StrategicImportance * 1.5;

        return score;
    }

    private List<(string RegionId, double Priority)> PrioritizeRegionsByDifficulty(
        Dictionary<string, ThreatAssessment> assessments,
        DifficultyLevel difficultyLevel)
    {
        var sortedByThreat = assessments
            .OrderByDescending(kvp => kvp.Value.ThreatScore)
            .ToList();

        return difficultyLevel switch
        {
            DifficultyLevel.Hard => sortedByThreat
                .Select((kvp, index) => (kvp.Key, Priority: kvp.Value.ThreatScore))
                .ToList(),
            
            DifficultyLevel.Medium => PrioritizeTopThreats(sortedByThreat),
            
            DifficultyLevel.Easy => PrioritizeRandomWeighted(sortedByThreat),
            
            _ => sortedByThreat
                .Select((kvp, index) => (kvp.Key, Priority: kvp.Value.ThreatScore))
                .ToList()
        };
    }

    private List<(string RegionId, double Priority)> PrioritizeTopThreats(
        List<KeyValuePair<string, ThreatAssessment>> sortedAssessments)
    {
        var result = new List<(string RegionId, double Priority)>();
        
        var topThree = sortedAssessments.Take(3).ToList();
        foreach (var kvp in topThree)
        {
            result.Add((kvp.Key, kvp.Value.ThreatScore));
        }

        var remaining = sortedAssessments.Skip(3).ToList();
        if (remaining.Count > 0)
        {
            var shuffled = remaining.OrderBy(x => _random.Next()).ToList();
            foreach (var kvp in shuffled)
            {
                result.Add((kvp.Key, kvp.Value.ThreatScore * 0.5));
            }
        }

        return result;
    }

    private List<(string RegionId, double Priority)> PrioritizeRandomWeighted(
        List<KeyValuePair<string, ThreatAssessment>> sortedAssessments)
    {
        var result = new List<(string RegionId, double Priority)>();
        
        double totalThreat = sortedAssessments.Sum(kvp => Math.Max(1.0, kvp.Value.ThreatScore));
        
        var weightedList = new List<(string RegionId, double Weight, double OriginalThreat)>();
        foreach (var kvp in sortedAssessments)
        {
            double weight = Math.Max(1.0, kvp.Value.ThreatScore) / totalThreat;
            weightedList.Add((kvp.Key, weight, kvp.Value.ThreatScore));
        }

        var remaining = new List<(string RegionId, double Weight, double OriginalThreat)>(weightedList);
        
        while (remaining.Count > 0)
        {
            double roll = _random.NextDouble();
            double cumulative = 0.0;
            
            (string RegionId, double Weight, double OriginalThreat) selected = remaining[0];
            foreach (var item in remaining)
            {
                cumulative += item.Weight;
                if (roll <= cumulative)
                {
                    selected = item;
                    break;
                }
            }

            result.Add((selected.RegionId, selected.OriginalThreat));
            remaining.Remove(selected);
            
            if (remaining.Count > 0)
            {
                double newTotal = remaining.Sum(x => x.Weight);
                remaining = remaining
                    .Select(x => (x.RegionId, Weight: x.Weight / newTotal, x.OriginalThreat))
                    .ToList();
            }
        }

        return result;
    }

    private int CalculateMinimumGarrison(Game game, Region region)
    {
        var stellarBody = game.StarSystems
            .SelectMany(s => s.StellarBodies)
            .FirstOrDefault(b => b.Regions.Any(r => r.Id == region.Id));

        if (stellarBody == null)
        {
            return 1;
        }

        int baseGarrison = stellarBody.ResourceType switch
        {
            ResourceType.Population => 2,
            ResourceType.Metal => 2,
            ResourceType.Fuel => 1,
            _ => 1
        };

        int upgradeBonus = stellarBody.UpgradeLevel;
        
        return baseGarrison + upgradeBonus;
    }

    private int CalculateDesiredGarrison(ThreatAssessment assessment, int minimumGarrison)
    {
        int desired = minimumGarrison;

        if (assessment.IsFrontline)
        {
            desired += (int)(assessment.EnemyThreat * 1.2);
        }
        else
        {
            desired += (int)(assessment.EnemyThreat * 0.5);
        }

        double strategicBonus = assessment.StrategicImportance / 200.0;
        desired += (int)Math.Ceiling(strategicBonus);

        return Math.Max(minimumGarrison, desired);
    }

    private void DistributeRemainingArmies(
        Dictionary<string, int> allocations,
        List<Region> playerRegions,
        int remainingArmies,
        Dictionary<string, ThreatAssessment> assessments)
    {
        var sortedRegions = playerRegions
            .OrderByDescending(r => assessments[r.Id].ThreatScore)
            .ToList();

        int index = 0;
        while (remainingArmies > 0 && sortedRegions.Count > 0)
        {
            var region = sortedRegions[index % sortedRegions.Count];
            
            if (!allocations.ContainsKey(region.Id))
            {
                allocations[region.Id] = 0;
            }
            
            allocations[region.Id]++;
            remainingArmies--;
            index++;
        }
    }

    private class ThreatAssessment
    {
        public string RegionId { get; set; } = string.Empty;
        public bool IsFrontline { get; set; }
        public double RegionValue { get; set; }
        public double EnemyThreat { get; set; }
        public double StrategicImportance { get; set; }
        public double ThreatScore { get; set; }
    }
}
