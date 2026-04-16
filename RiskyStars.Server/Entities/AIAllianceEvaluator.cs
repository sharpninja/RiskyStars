namespace RiskyStars.Server.Entities;

public class AIAllianceEvaluator
{
    private readonly GameStateEvaluator _gameStateEvaluator;
    private readonly Random _random;

    public AIAllianceEvaluator(GameStateEvaluator gameStateEvaluator, Random? random = null)
    {
        _gameStateEvaluator = gameStateEvaluator;
        _random = random ?? new Random();
    }

    public bool ShouldProposeAlliance(AIPlayer aiPlayer, Player potentialAlly, Game game)
    {
        if (aiPlayer.Id == potentialAlly.Id)
        {
            return false;
        }

        if (aiPlayer.AllianceId != null)
        {
            return false;
        }

        if (potentialAlly.AllianceId != null)
        {
            return false;
        }

        return aiPlayer.DifficultyLevel switch
        {
            DifficultyLevel.Easy => EvaluateEasyProposal(),
            DifficultyLevel.Medium => EvaluateMediumProposal(aiPlayer, potentialAlly, game),
            DifficultyLevel.Hard => EvaluateHardProposal(aiPlayer, potentialAlly, game),
            _ => false
        };
    }

    public bool ShouldAcceptAlliance(AIPlayer aiPlayer, Player proposer, Game game)
    {
        if (aiPlayer.Id == proposer.Id)
        {
            return false;
        }

        if (aiPlayer.AllianceId != null)
        {
            return false;
        }

        return aiPlayer.DifficultyLevel switch
        {
            DifficultyLevel.Easy => EvaluateEasyAcceptance(),
            DifficultyLevel.Medium => EvaluateMediumAcceptance(aiPlayer, proposer, game),
            DifficultyLevel.Hard => EvaluateHardAcceptance(aiPlayer, proposer, game),
            _ => false
        };
    }

    private bool EvaluateEasyProposal()
    {
        return _random.NextDouble() < 0.3;
    }

    private bool EvaluateEasyAcceptance()
    {
        return _random.NextDouble() < 0.3;
    }

    private bool EvaluateMediumProposal(AIPlayer aiPlayer, Player potentialAlly, Game game)
    {
        double militaryRatio = CalculateMilitaryStrengthRatio(aiPlayer, potentialAlly, game);
        
        if (militaryRatio < 0.5 || militaryRatio > 2.0)
        {
            return false;
        }

        bool hasProximity = HasTerritorialProximity(aiPlayer, potentialAlly, game);
        if (!hasProximity)
        {
            return false;
        }

        bool hasMutualThreat = HasMutualThreat(aiPlayer, potentialAlly, game);
        bool hasComplementarity = HasResourceComplementarity(aiPlayer, potentialAlly, game);

        return hasMutualThreat || hasComplementarity;
    }

    private bool EvaluateMediumAcceptance(AIPlayer aiPlayer, Player proposer, Game game)
    {
        double militaryRatio = CalculateMilitaryStrengthRatio(aiPlayer, proposer, game);
        
        if (militaryRatio < 0.4 || militaryRatio > 2.5)
        {
            return false;
        }

        bool hasProximity = HasTerritorialProximity(aiPlayer, proposer, game);
        bool hasMutualThreat = HasMutualThreat(aiPlayer, proposer, game);
        bool hasComplementarity = HasResourceComplementarity(aiPlayer, proposer, game);

        int positiveFactors = 0;
        if (hasProximity)
        {
            positiveFactors++;
        }

        if (hasMutualThreat)
        {
            positiveFactors++;
        }

        if (hasComplementarity)
        {
            positiveFactors++;
        }

        return positiveFactors >= 2;
    }

    private bool EvaluateHardProposal(AIPlayer aiPlayer, Player potentialAlly, Game game)
    {
        var gameLeader = IdentifyGameLeader(game);
        
        if (gameLeader != null && gameLeader.Id != aiPlayer.Id && gameLeader.Id != potentialAlly.Id)
        {
            bool bothThreatenedByLeader = 
                IsThreatenedBy(aiPlayer, gameLeader, game) && 
                IsThreatenedBy(potentialAlly, gameLeader, game);
            
            if (bothThreatenedByLeader)
            {
                double militaryRatio = CalculateMilitaryStrengthRatio(aiPlayer, potentialAlly, game);
                if (militaryRatio >= 0.3 && militaryRatio <= 3.0)
                {
                    return true;
                }
            }
        }

        double combinedStrength = CalculateCombinedStrengthAdvantage(aiPlayer, potentialAlly, game);
        if (combinedStrength > 1.5)
        {
            bool hasProximity = HasTerritorialProximity(aiPlayer, potentialAlly, game);
            bool hasComplementarity = HasResourceComplementarity(aiPlayer, potentialAlly, game);
            
            if (hasProximity && hasComplementarity)
            {
                return true;
            }
        }

        return false;
    }

    private bool EvaluateHardAcceptance(AIPlayer aiPlayer, Player proposer, Game game)
    {
        var gameLeader = IdentifyGameLeader(game);
        
        if (gameLeader != null && gameLeader.Id != aiPlayer.Id && gameLeader.Id != proposer.Id)
        {
            bool bothThreatenedByLeader = 
                IsThreatenedBy(aiPlayer, gameLeader, game) && 
                IsThreatenedBy(proposer, gameLeader, game);
            
            if (bothThreatenedByLeader)
            {
                double militaryRatio = CalculateMilitaryStrengthRatio(aiPlayer, proposer, game);
                if (militaryRatio >= 0.25 && militaryRatio <= 4.0)
                {
                    return true;
                }
            }
        }

        double combinedStrength = CalculateCombinedStrengthAdvantage(aiPlayer, proposer, game);
        if (combinedStrength > 1.3)
        {
            bool hasProximity = HasTerritorialProximity(aiPlayer, proposer, game);
            bool hasComplementarity = HasResourceComplementarity(aiPlayer, proposer, game);
            bool hasMutualThreat = HasMutualThreat(aiPlayer, proposer, game);
            
            int positiveFactors = 0;
            if (hasProximity)
            {
                positiveFactors++;
            }

            if (hasComplementarity)
            {
                positiveFactors++;
            }

            if (hasMutualThreat)
            {
                positiveFactors++;
            }

            return positiveFactors >= 2;
        }

        return false;
    }

    private double CalculateMilitaryStrengthRatio(Player player1, Player player2, Game game)
    {
        double player1Strength = _gameStateEvaluator.EvaluateMilitaryStrength(game, player1.Id);
        double player2Strength = _gameStateEvaluator.EvaluateMilitaryStrength(game, player2.Id);

        if (player2Strength == 0)
        {
            return player1Strength > 0 ? double.MaxValue : 1.0;
        }

        return player1Strength / player2Strength;
    }

    private bool HasTerritorialProximity(Player player1, Player player2, Game game)
    {
        var player1Systems = GetPlayerStarSystems(player1, game);
        var player2Systems = GetPlayerStarSystems(player2, game);

        foreach (var system1 in player1Systems)
        {
            if (player2Systems.Contains(system1))
            {
                return true;
            }

            foreach (var lane in system1.HyperspaceLanes)
            {
                var neighborSystemId = lane.GetOppositeStarSystemId(system1.Id);
                if (player2Systems.Any(s => s.Id == neighborSystemId))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool HasMutualThreat(Player player1, Player player2, Game game)
    {
        var player1Neighbors = GetHostileNeighbors(player1, game);
        var player2Neighbors = GetHostileNeighbors(player2, game);

        var mutualThreats = player1Neighbors.Intersect(player2Neighbors).ToList();

        return mutualThreats.Any();
    }

    private bool HasResourceComplementarity(Player player1, Player player2, Game game)
    {
        var player1ResourceProfile = GetResourceProfile(player1, game);
        var player2ResourceProfile = GetResourceProfile(player2, game);

        int differentPrimaryResources = 0;

        if (player1ResourceProfile.PrimaryResource != player2ResourceProfile.PrimaryResource)
        {
            differentPrimaryResources++;
        }

        if (player1ResourceProfile.SecondaryResource != player2ResourceProfile.SecondaryResource &&
            player1ResourceProfile.SecondaryResource != player2ResourceProfile.PrimaryResource &&
            player1ResourceProfile.PrimaryResource != player2ResourceProfile.SecondaryResource)
        {
            differentPrimaryResources++;
        }

        return differentPrimaryResources >= 1;
    }

    private Player? IdentifyGameLeader(Game game)
    {
        if (game.Players.Count == 0)
        {
            return null;
        }

        Player? leader = null;
        double highestScore = double.MinValue;

        foreach (var player in game.Players)
        {
            double score = _gameStateEvaluator.EvaluateOverallPosition(game, player.Id);
            if (score > highestScore)
            {
                highestScore = score;
                leader = player;
            }
        }

        return leader;
    }

    private bool IsThreatenedBy(Player player, Player threat, Game game)
    {
        var playerSystems = GetPlayerStarSystems(player, game);
        var threatSystems = GetPlayerStarSystems(threat, game);

        foreach (var playerSystem in playerSystems)
        {
            if (threatSystems.Contains(playerSystem))
            {
                return true;
            }

            foreach (var lane in playerSystem.HyperspaceLanes)
            {
                var neighborSystemId = lane.GetOppositeStarSystemId(playerSystem.Id);
                if (threatSystems.Any(s => s.Id == neighborSystemId))
                {
                    return true;
                }
            }
        }

        double threatMilitaryStrength = _gameStateEvaluator.EvaluateMilitaryStrength(game, threat.Id);
        double playerMilitaryStrength = _gameStateEvaluator.EvaluateMilitaryStrength(game, player.Id);

        if (playerMilitaryStrength > 0 && threatMilitaryStrength / playerMilitaryStrength > 1.5)
        {
            return true;
        }

        return false;
    }

    private double CalculateCombinedStrengthAdvantage(Player player1, Player player2, Game game)
    {
        double player1Strength = _gameStateEvaluator.EvaluateMilitaryStrength(game, player1.Id);
        double player2Strength = _gameStateEvaluator.EvaluateMilitaryStrength(game, player2.Id);
        double combinedStrength = player1Strength + player2Strength;

        double averageOpponentStrength = 0;
        int opponentCount = 0;

        foreach (var player in game.Players)
        {
            if (player.Id != player1.Id && player.Id != player2.Id)
            {
                double strength = _gameStateEvaluator.EvaluateMilitaryStrength(game, player.Id);
                averageOpponentStrength += strength;
                opponentCount++;
            }
        }

        if (opponentCount > 0)
        {
            averageOpponentStrength /= opponentCount;
        }

        if (averageOpponentStrength == 0)
        {
            return combinedStrength > 0 ? double.MaxValue : 1.0;
        }

        return combinedStrength / averageOpponentStrength;
    }

    private List<StarSystem> GetPlayerStarSystems(Player player, Game game)
    {
        var playerRegionIds = new HashSet<string>(
            game.GetAllRegions()
                .Where(r => r.OwnerId == player.Id)
                .Select(r => r.Id)
        );

        return game.StarSystems
            .Where(system => system.StellarBodies
                .Any(body => body.Regions
                    .Any(region => playerRegionIds.Contains(region.Id))))
            .ToList();
    }

    private HashSet<string> GetHostileNeighbors(Player player, Game game)
    {
        var hostileNeighbors = new HashSet<string>();
        var playerSystems = GetPlayerStarSystems(player, game);

        foreach (var system in playerSystems)
        {
            foreach (var body in system.StellarBodies)
            {
                foreach (var region in body.Regions)
                {
                    if (region.OwnerId != null && region.OwnerId != player.Id)
                    {
                        hostileNeighbors.Add(region.OwnerId);
                    }
                }
            }

            foreach (var lane in system.HyperspaceLanes)
            {
                var neighborSystemId = lane.GetOppositeStarSystemId(system.Id);
                var neighborSystem = game.StarSystems.FirstOrDefault(s => s.Id == neighborSystemId);

                if (neighborSystem != null)
                {
                    foreach (var body in neighborSystem.StellarBodies)
                    {
                        foreach (var region in body.Regions)
                        {
                            if (region.OwnerId != null && region.OwnerId != player.Id)
                            {
                                hostileNeighbors.Add(region.OwnerId);
                            }
                        }
                    }
                }
            }
        }

        return hostileNeighbors;
    }

    private ResourceProfile GetResourceProfile(Player player, Game game)
    {
        var resourceCounts = new Dictionary<ResourceType, int>
        {
            { ResourceType.Population, 0 },
            { ResourceType.Metal, 0 },
            { ResourceType.Fuel, 0 }
        };

        var ownedBodies = game.GetPlayerOwnedBodies(player.Id);

        foreach (var body in ownedBodies)
        {
            resourceCounts[body.ResourceType]++;
        }

        var sortedResources = resourceCounts
            .OrderByDescending(kvp => kvp.Value)
            .Select(kvp => kvp.Key)
            .ToList();

        return new ResourceProfile
        {
            PrimaryResource = sortedResources.ElementAtOrDefault(0),
            SecondaryResource = sortedResources.ElementAtOrDefault(1)
        };
    }

    private class ResourceProfile
    {
        public ResourceType PrimaryResource { get; set; }
        public ResourceType SecondaryResource { get; set; }
    }
}
