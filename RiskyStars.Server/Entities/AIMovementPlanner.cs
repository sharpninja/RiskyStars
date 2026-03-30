using RiskyStars.Server.Services;

namespace RiskyStars.Server.Entities;

public class AIMovementPlanner
{
    private readonly GameStateEvaluator _evaluator;
    private readonly CombatPredictor _combatPredictor;
    private readonly Random _random;

    public AIMovementPlanner(
        GameStateEvaluator evaluator, 
        CombatPredictor combatPredictor,
        Random? random = null)
    {
        _evaluator = evaluator;
        _combatPredictor = combatPredictor;
        _random = random ?? new Random();
    }

    public MovementAction? SelectBestMove(
        Game game,
        string playerId,
        DifficultyLevel difficultyLevel)
    {
        var playerArmies = game.GetAllArmies()
            .Where(a => a.OwnerId == playerId && !a.HasMovedThisTurn)
            .ToList();

        if (playerArmies.Count == 0)
            return null;

        var allMoves = new List<MovementAction>();

        allMoves.AddRange(GenerateOffensiveMoves(game, playerId, playerArmies, difficultyLevel));
        allMoves.AddRange(GenerateExpansionMoves(game, playerId, playerArmies));
        allMoves.AddRange(GenerateConsolidationMoves(game, playerId, playerArmies));
        allMoves.AddRange(GenerateDefensiveMoves(game, playerId, playerArmies));

        var legalMoves = allMoves
            .Where(m => IsLegalMove(game, m, playerId))
            .ToList();

        if (legalMoves.Count == 0)
            return null;

        foreach (var move in legalMoves)
        {
            move.Score = CalculateMoveScore(game, move, playerId, difficultyLevel);
        }

        return SelectMoveByDifficulty(legalMoves, difficultyLevel);
    }

    private List<MovementAction> GenerateOffensiveMoves(
        Game game,
        string playerId,
        List<Army> playerArmies,
        DifficultyLevel difficultyLevel)
    {
        var offensiveMoves = new List<MovementAction>();

        foreach (var army in playerArmies)
        {
            if (army.LocationType == LocationType.Region)
            {
                var region = game.GetAllRegions().FirstOrDefault(r => r.Id == army.LocationId);
                if (region == null) continue;

                var stellarBody = game.StarSystems
                    .SelectMany(s => s.StellarBodies)
                    .FirstOrDefault(b => b.Regions.Any(r => r.Id == region.Id));

                if (stellarBody == null) continue;

                foreach (var targetRegion in stellarBody.Regions)
                {
                    if (targetRegion.Id == region.Id) continue;
                    if (targetRegion.OwnerId == playerId) continue;

                    var targetArmy = targetRegion.Army;
                    int defenderCount = targetArmy?.UnitCount ?? 0;

                    if (targetRegion.OwnerId != null && defenderCount > 0)
                    {
                        var prediction = _combatPredictor.PredictCombat(
                            army.UnitCount,
                            defenderCount,
                            difficultyLevel);

                        if (prediction.AttackerWinProbability > 0.0)
                        {
                            offensiveMoves.Add(new MovementAction
                            {
                                ArmyId = army.Id,
                                SourceLocationId = army.LocationId,
                                SourceLocationType = army.LocationType,
                                TargetLocationId = targetRegion.Id,
                                TargetLocationType = LocationType.Region,
                                ActionType = MovementActionType.Offensive,
                                UnitCount = army.UnitCount,
                                CombatPrediction = prediction
                            });
                        }
                    }
                }

                var starSystem = game.StarSystems.FirstOrDefault(s => s.Id == stellarBody.StarSystemId);
                if (starSystem != null)
                {
                    foreach (var lane in starSystem.HyperspaceLanes)
                    {
                        var mouthId = GetLaneMouthIdForStarSystem(lane, starSystem.Id);
                        if (string.IsNullOrEmpty(mouthId)) continue;

                        var mouthOwnerId = GetLaneMouthOwnerId(lane, mouthId);
                        var mouthArmy = GetLaneMouthArmy(lane, mouthId);

                        if (mouthOwnerId != playerId && mouthOwnerId != null && mouthArmy != null)
                        {
                            var prediction = _combatPredictor.PredictCombat(
                                army.UnitCount,
                                mouthArmy.UnitCount,
                                difficultyLevel);

                            if (prediction.AttackerWinProbability > 0.0)
                            {
                                offensiveMoves.Add(new MovementAction
                                {
                                    ArmyId = army.Id,
                                    SourceLocationId = army.LocationId,
                                    SourceLocationType = army.LocationType,
                                    TargetLocationId = mouthId,
                                    TargetLocationType = LocationType.HyperspaceLaneMouth,
                                    ActionType = MovementActionType.Offensive,
                                    UnitCount = army.UnitCount,
                                    CombatPrediction = prediction
                                });
                            }
                        }
                    }
                }
            }
            else if (army.LocationType == LocationType.HyperspaceLaneMouth)
            {
                var lane = game.GetAllHyperspaceLanes()
                    .FirstOrDefault(l => l.MouthAId == army.LocationId || l.MouthBId == army.LocationId);

                if (lane == null) continue;

                var oppositeMouthId = lane.GetOppositeMouthId(army.LocationId);
                var oppositeMouthOwnerId = GetLaneMouthOwnerId(lane, oppositeMouthId);
                var oppositeMouthArmy = GetLaneMouthArmy(lane, oppositeMouthId);

                if (oppositeMouthOwnerId != playerId && oppositeMouthOwnerId != null && oppositeMouthArmy != null)
                {
                    var prediction = _combatPredictor.PredictCombat(
                        army.UnitCount,
                        oppositeMouthArmy.UnitCount,
                        difficultyLevel);

                    if (prediction.AttackerWinProbability > 0.0)
                    {
                        offensiveMoves.Add(new MovementAction
                        {
                            ArmyId = army.Id,
                            SourceLocationId = army.LocationId,
                            SourceLocationType = army.LocationType,
                            TargetLocationId = oppositeMouthId,
                            TargetLocationType = LocationType.HyperspaceLaneMouth,
                            ActionType = MovementActionType.Offensive,
                            UnitCount = army.UnitCount,
                            CombatPrediction = prediction
                        });
                    }
                }

                var oppositeSystemId = army.LocationId == lane.MouthAId 
                    ? lane.StarSystemBId 
                    : lane.StarSystemAId;
                var oppositeSystem = game.StarSystems.FirstOrDefault(s => s.Id == oppositeSystemId);

                if (oppositeSystem != null)
                {
                    foreach (var body in oppositeSystem.StellarBodies)
                    {
                        foreach (var targetRegion in body.Regions)
                        {
                            if (targetRegion.OwnerId == playerId) continue;

                            var targetArmy = targetRegion.Army;
                            int defenderCount = targetArmy?.UnitCount ?? 0;

                            if (targetRegion.OwnerId != null && defenderCount > 0)
                            {
                                var prediction = _combatPredictor.PredictCombat(
                                    army.UnitCount,
                                    defenderCount,
                                    difficultyLevel);

                                if (prediction.AttackerWinProbability > 0.0)
                                {
                                    offensiveMoves.Add(new MovementAction
                                    {
                                        ArmyId = army.Id,
                                        SourceLocationId = army.LocationId,
                                        SourceLocationType = army.LocationType,
                                        TargetLocationId = targetRegion.Id,
                                        TargetLocationType = LocationType.Region,
                                        ActionType = MovementActionType.Offensive,
                                        UnitCount = army.UnitCount,
                                        CombatPrediction = prediction
                                    });
                                }
                            }
                        }
                    }
                }
            }
        }

        return offensiveMoves;
    }

    private List<MovementAction> GenerateExpansionMoves(
        Game game,
        string playerId,
        List<Army> playerArmies)
    {
        var expansionMoves = new List<MovementAction>();

        foreach (var army in playerArmies)
        {
            if (army.LocationType == LocationType.Region)
            {
                var region = game.GetAllRegions().FirstOrDefault(r => r.Id == army.LocationId);
                if (region == null) continue;

                var stellarBody = game.StarSystems
                    .SelectMany(s => s.StellarBodies)
                    .FirstOrDefault(b => b.Regions.Any(r => r.Id == region.Id));

                if (stellarBody == null) continue;

                foreach (var targetRegion in stellarBody.Regions)
                {
                    if (targetRegion.Id == region.Id) continue;
                    if (targetRegion.OwnerId != null) continue;

                    expansionMoves.Add(new MovementAction
                    {
                        ArmyId = army.Id,
                        SourceLocationId = army.LocationId,
                        SourceLocationType = army.LocationType,
                        TargetLocationId = targetRegion.Id,
                        TargetLocationType = LocationType.Region,
                        ActionType = MovementActionType.Expansion,
                        UnitCount = army.UnitCount
                    });
                }

                var starSystem = game.StarSystems.FirstOrDefault(s => s.Id == stellarBody.StarSystemId);
                if (starSystem != null)
                {
                    foreach (var lane in starSystem.HyperspaceLanes)
                    {
                        var mouthId = GetLaneMouthIdForStarSystem(lane, starSystem.Id);
                        if (string.IsNullOrEmpty(mouthId)) continue;

                        var mouthOwnerId = GetLaneMouthOwnerId(lane, mouthId);

                        if (mouthOwnerId == null)
                        {
                            expansionMoves.Add(new MovementAction
                            {
                                ArmyId = army.Id,
                                SourceLocationId = army.LocationId,
                                SourceLocationType = army.LocationType,
                                TargetLocationId = mouthId,
                                TargetLocationType = LocationType.HyperspaceLaneMouth,
                                ActionType = MovementActionType.Expansion,
                                UnitCount = army.UnitCount
                            });
                        }
                    }
                }
            }
            else if (army.LocationType == LocationType.HyperspaceLaneMouth)
            {
                var lane = game.GetAllHyperspaceLanes()
                    .FirstOrDefault(l => l.MouthAId == army.LocationId || l.MouthBId == army.LocationId);

                if (lane == null) continue;

                var oppositeSystemId = army.LocationId == lane.MouthAId 
                    ? lane.StarSystemBId 
                    : lane.StarSystemAId;
                var oppositeSystem = game.StarSystems.FirstOrDefault(s => s.Id == oppositeSystemId);

                if (oppositeSystem != null)
                {
                    foreach (var body in oppositeSystem.StellarBodies)
                    {
                        foreach (var targetRegion in body.Regions)
                        {
                            if (targetRegion.OwnerId != null) continue;

                            expansionMoves.Add(new MovementAction
                            {
                                ArmyId = army.Id,
                                SourceLocationId = army.LocationId,
                                SourceLocationType = army.LocationType,
                                TargetLocationId = targetRegion.Id,
                                TargetLocationType = LocationType.Region,
                                ActionType = MovementActionType.Expansion,
                                UnitCount = army.UnitCount
                            });
                        }
                    }
                }
            }
        }

        return expansionMoves;
    }

    private List<MovementAction> GenerateConsolidationMoves(
        Game game,
        string playerId,
        List<Army> playerArmies)
    {
        var consolidationMoves = new List<MovementAction>();

        foreach (var army in playerArmies)
        {
            if (army.LocationType == LocationType.Region)
            {
                var region = game.GetAllRegions().FirstOrDefault(r => r.Id == army.LocationId);
                if (region == null) continue;

                var stellarBody = game.StarSystems
                    .SelectMany(s => s.StellarBodies)
                    .FirstOrDefault(b => b.Regions.Any(r => r.Id == region.Id));

                if (stellarBody == null) continue;

                foreach (var targetRegion in stellarBody.Regions)
                {
                    if (targetRegion.Id == region.Id) continue;
                    if (targetRegion.OwnerId != playerId) continue;

                    consolidationMoves.Add(new MovementAction
                    {
                        ArmyId = army.Id,
                        SourceLocationId = army.LocationId,
                        SourceLocationType = army.LocationType,
                        TargetLocationId = targetRegion.Id,
                        TargetLocationType = LocationType.Region,
                        ActionType = MovementActionType.Consolidation,
                        UnitCount = army.UnitCount
                    });
                }

                var starSystem = game.StarSystems.FirstOrDefault(s => s.Id == stellarBody.StarSystemId);
                if (starSystem != null)
                {
                    foreach (var lane in starSystem.HyperspaceLanes)
                    {
                        var mouthId = GetLaneMouthIdForStarSystem(lane, starSystem.Id);
                        if (string.IsNullOrEmpty(mouthId)) continue;

                        var mouthOwnerId = GetLaneMouthOwnerId(lane, mouthId);

                        if (mouthOwnerId == playerId)
                        {
                            consolidationMoves.Add(new MovementAction
                            {
                                ArmyId = army.Id,
                                SourceLocationId = army.LocationId,
                                SourceLocationType = army.LocationType,
                                TargetLocationId = mouthId,
                                TargetLocationType = LocationType.HyperspaceLaneMouth,
                                ActionType = MovementActionType.Consolidation,
                                UnitCount = army.UnitCount
                            });
                        }
                    }
                }
            }
            else if (army.LocationType == LocationType.HyperspaceLaneMouth)
            {
                var lane = game.GetAllHyperspaceLanes()
                    .FirstOrDefault(l => l.MouthAId == army.LocationId || l.MouthBId == army.LocationId);

                if (lane == null) continue;

                var oppositeMouthId = lane.GetOppositeMouthId(army.LocationId);
                var oppositeMouthOwnerId = GetLaneMouthOwnerId(lane, oppositeMouthId);

                if (oppositeMouthOwnerId == playerId)
                {
                    consolidationMoves.Add(new MovementAction
                    {
                        ArmyId = army.Id,
                        SourceLocationId = army.LocationId,
                        SourceLocationType = army.LocationType,
                        TargetLocationId = oppositeMouthId,
                        TargetLocationType = LocationType.HyperspaceLaneMouth,
                        ActionType = MovementActionType.Consolidation,
                        UnitCount = army.UnitCount
                    });
                }

                var sourceSystemId = army.LocationId == lane.MouthAId 
                    ? lane.StarSystemAId 
                    : lane.StarSystemBId;
                var sourceSystem = game.StarSystems.FirstOrDefault(s => s.Id == sourceSystemId);

                if (sourceSystem != null)
                {
                    foreach (var body in sourceSystem.StellarBodies)
                    {
                        foreach (var targetRegion in body.Regions)
                        {
                            if (targetRegion.OwnerId != playerId) continue;

                            consolidationMoves.Add(new MovementAction
                            {
                                ArmyId = army.Id,
                                SourceLocationId = army.LocationId,
                                SourceLocationType = army.LocationType,
                                TargetLocationId = targetRegion.Id,
                                TargetLocationType = LocationType.Region,
                                ActionType = MovementActionType.Consolidation,
                                UnitCount = army.UnitCount
                            });
                        }
                    }
                }
            }
        }

        return consolidationMoves;
    }

    private List<MovementAction> GenerateDefensiveMoves(
        Game game,
        string playerId,
        List<Army> playerArmies)
    {
        var defensiveMoves = new List<MovementAction>();

        var threatenedRegions = IdentifyThreatenedRegions(game, playerId);

        foreach (var army in playerArmies)
        {
            if (army.LocationType == LocationType.Region)
            {
                var region = game.GetAllRegions().FirstOrDefault(r => r.Id == army.LocationId);
                if (region == null) continue;

                var stellarBody = game.StarSystems
                    .SelectMany(s => s.StellarBodies)
                    .FirstOrDefault(b => b.Regions.Any(r => r.Id == region.Id));

                if (stellarBody == null) continue;

                foreach (var targetRegion in stellarBody.Regions)
                {
                    if (targetRegion.Id == region.Id) continue;
                    if (targetRegion.OwnerId != playerId) continue;

                    if (threatenedRegions.Contains(targetRegion.Id))
                    {
                        defensiveMoves.Add(new MovementAction
                        {
                            ArmyId = army.Id,
                            SourceLocationId = army.LocationId,
                            SourceLocationType = army.LocationType,
                            TargetLocationId = targetRegion.Id,
                            TargetLocationType = LocationType.Region,
                            ActionType = MovementActionType.Defensive,
                            UnitCount = army.UnitCount
                        });
                    }
                }

                var starSystem = game.StarSystems.FirstOrDefault(s => s.Id == stellarBody.StarSystemId);
                if (starSystem != null)
                {
                    foreach (var lane in starSystem.HyperspaceLanes)
                    {
                        var mouthId = GetLaneMouthIdForStarSystem(lane, starSystem.Id);
                        if (string.IsNullOrEmpty(mouthId)) continue;

                        var mouthOwnerId = GetLaneMouthOwnerId(lane, mouthId);

                        if (mouthOwnerId == playerId)
                        {
                            var oppositeMouthId = lane.GetOppositeMouthId(mouthId);
                            var oppositeMouthOwnerId = GetLaneMouthOwnerId(lane, oppositeMouthId);

                            if (oppositeMouthOwnerId != playerId && oppositeMouthOwnerId != null)
                            {
                                defensiveMoves.Add(new MovementAction
                                {
                                    ArmyId = army.Id,
                                    SourceLocationId = army.LocationId,
                                    SourceLocationType = army.LocationType,
                                    TargetLocationId = mouthId,
                                    TargetLocationType = LocationType.HyperspaceLaneMouth,
                                    ActionType = MovementActionType.Defensive,
                                    UnitCount = army.UnitCount
                                });
                            }
                        }
                    }
                }
            }
            else if (army.LocationType == LocationType.HyperspaceLaneMouth)
            {
                var lane = game.GetAllHyperspaceLanes()
                    .FirstOrDefault(l => l.MouthAId == army.LocationId || l.MouthBId == army.LocationId);

                if (lane == null) continue;

                var sourceSystemId = army.LocationId == lane.MouthAId 
                    ? lane.StarSystemAId 
                    : lane.StarSystemBId;
                var sourceSystem = game.StarSystems.FirstOrDefault(s => s.Id == sourceSystemId);

                if (sourceSystem != null)
                {
                    foreach (var body in sourceSystem.StellarBodies)
                    {
                        foreach (var targetRegion in body.Regions)
                        {
                            if (targetRegion.OwnerId != playerId) continue;

                            if (threatenedRegions.Contains(targetRegion.Id))
                            {
                                defensiveMoves.Add(new MovementAction
                                {
                                    ArmyId = army.Id,
                                    SourceLocationId = army.LocationId,
                                    SourceLocationType = army.LocationType,
                                    TargetLocationId = targetRegion.Id,
                                    TargetLocationType = LocationType.Region,
                                    ActionType = MovementActionType.Defensive,
                                    UnitCount = army.UnitCount
                                });
                            }
                        }
                    }
                }
            }
        }

        return defensiveMoves;
    }

    private HashSet<string> IdentifyThreatenedRegions(Game game, string playerId)
    {
        var threatenedRegions = new HashSet<string>();
        var playerRegions = game.GetAllRegions().Where(r => r.OwnerId == playerId).ToList();

        foreach (var region in playerRegions)
        {
            var stellarBody = game.StarSystems
                .SelectMany(s => s.StellarBodies)
                .FirstOrDefault(b => b.Regions.Any(r => r.Id == region.Id));

            if (stellarBody == null) continue;

            foreach (var otherRegion in stellarBody.Regions)
            {
                if (otherRegion.OwnerId != playerId && otherRegion.OwnerId != null)
                {
                    var enemyArmy = otherRegion.Army;
                    if (enemyArmy != null && enemyArmy.UnitCount > 0)
                    {
                        threatenedRegions.Add(region.Id);
                        break;
                    }
                }
            }

            if (threatenedRegions.Contains(region.Id))
                continue;

            var starSystem = game.StarSystems.FirstOrDefault(s => s.Id == stellarBody.StarSystemId);
            if (starSystem != null)
            {
                foreach (var lane in starSystem.HyperspaceLanes)
                {
                    var mouthId = GetLaneMouthIdForStarSystem(lane, starSystem.Id);
                    var oppositeMouthId = lane.GetOppositeMouthId(mouthId);
                    var oppositeMouthOwnerId = GetLaneMouthOwnerId(lane, oppositeMouthId);
                    var oppositeMouthArmy = GetLaneMouthArmy(lane, oppositeMouthId);

                    if (oppositeMouthOwnerId != playerId && oppositeMouthOwnerId != null 
                        && oppositeMouthArmy != null && oppositeMouthArmy.UnitCount > 0)
                    {
                        threatenedRegions.Add(region.Id);
                        break;
                    }
                }
            }
        }

        return threatenedRegions;
    }

    private bool IsLegalMove(Game game, MovementAction move, string playerId)
    {
        var army = game.GetAllArmies().FirstOrDefault(a => a.Id == move.ArmyId);
        if (army == null || army.OwnerId != playerId || army.HasMovedThisTurn)
            return false;

        if (move.SourceLocationType == LocationType.Region)
        {
            var sourceRegion = game.GetAllRegions().FirstOrDefault(r => r.Id == move.SourceLocationId);
            if (sourceRegion == null || sourceRegion.OwnerId != playerId)
                return false;

            var stellarBody = game.StarSystems
                .SelectMany(s => s.StellarBodies)
                .FirstOrDefault(b => b.Regions.Any(r => r.Id == sourceRegion.Id));

            if (stellarBody == null)
                return false;

            int minimumGarrison = CalculateMinimumGarrison(stellarBody);
            int currentGarrison = sourceRegion.Army?.UnitCount ?? 0;
            int unitsMoving = move.UnitCount;

            if (currentGarrison - unitsMoving < minimumGarrison)
                return false;
        }

        if (move.TargetLocationType == LocationType.Region)
        {
            var targetRegion = game.GetAllRegions().FirstOrDefault(r => r.Id == move.TargetLocationId);
            if (targetRegion == null)
                return false;

            if (move.ActionType == MovementActionType.Consolidation && targetRegion.OwnerId != playerId)
                return false;
        }

        return true;
    }

    private int CalculateMinimumGarrison(StellarBody stellarBody)
    {
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

    private double CalculateMoveScore(
        Game game,
        MovementAction move,
        string playerId,
        DifficultyLevel difficultyLevel)
    {
        double score = 0.0;

        switch (move.ActionType)
        {
            case MovementActionType.Offensive:
                score = CalculateOffensiveScore(game, move, playerId, difficultyLevel);
                break;
            case MovementActionType.Expansion:
                score = CalculateExpansionScore(game, move, playerId);
                break;
            case MovementActionType.Consolidation:
                score = CalculateConsolidationScore(game, move, playerId);
                break;
            case MovementActionType.Defensive:
                score = CalculateDefensiveScore(game, move, playerId);
                break;
        }

        return score;
    }

    private double CalculateOffensiveScore(
        Game game,
        MovementAction move,
        string playerId,
        DifficultyLevel difficultyLevel)
    {
        double score = 0.0;

        if (move.CombatPrediction == null)
            return score;

        score += move.CombatPrediction.AttackerWinProbability * 1000.0;

        double expectedValue = 
            move.CombatPrediction.AttackerWinProbability * move.CombatPrediction.ExpectedAttackerRemainingUnits -
            move.CombatPrediction.DefenderWinProbability * move.CombatPrediction.ExpectedAttackerCasualties;

        score += expectedValue * 50.0;

        if (move.TargetLocationType == LocationType.Region)
        {
            var targetRegion = game.GetAllRegions().FirstOrDefault(r => r.Id == move.TargetLocationId);
            if (targetRegion != null)
            {
                var stellarBody = game.StarSystems
                    .SelectMany(s => s.StellarBodies)
                    .FirstOrDefault(b => b.Regions.Any(r => r.Id == targetRegion.Id));

                if (stellarBody != null)
                {
                    double regionValue = Region.BaseProductionPerRegion * stellarBody.GetYieldMultiplier();
                    score += regionValue * 2.0;

                    double resourceTypeMultiplier = stellarBody.ResourceType switch
                    {
                        ResourceType.Population => 1.5,
                        ResourceType.Metal => 1.2,
                        ResourceType.Fuel => 1.0,
                        _ => 1.0
                    };
                    score += regionValue * resourceTypeMultiplier * 1.5;
                }
            }
        }
        else if (move.TargetLocationType == LocationType.HyperspaceLaneMouth)
        {
            score += 500.0;
        }

        var aiConfig = GetAIConfiguration(game, playerId, difficultyLevel);
        score *= aiConfig.AggressivenessWeight;

        return score;
    }

    private double CalculateExpansionScore(Game game, MovementAction move, string playerId)
    {
        double score = 500.0;

        if (move.TargetLocationType == LocationType.Region)
        {
            var targetRegion = game.GetAllRegions().FirstOrDefault(r => r.Id == move.TargetLocationId);
            if (targetRegion != null)
            {
                var stellarBody = game.StarSystems
                    .SelectMany(s => s.StellarBodies)
                    .FirstOrDefault(b => b.Regions.Any(r => r.Id == targetRegion.Id));

                if (stellarBody != null)
                {
                    double regionValue = Region.BaseProductionPerRegion * stellarBody.GetYieldMultiplier();
                    score += regionValue * 3.0;

                    double resourceTypeMultiplier = stellarBody.ResourceType switch
                    {
                        ResourceType.Population => 2.0,
                        ResourceType.Metal => 1.5,
                        ResourceType.Fuel => 1.2,
                        _ => 1.0
                    };
                    score += regionValue * resourceTypeMultiplier;

                    var starSystem = game.StarSystems.FirstOrDefault(s => s.Id == stellarBody.StarSystemId);
                    if (starSystem != null)
                    {
                        int ownedRegionsInSystem = stellarBody.Regions.Count(r => r.OwnerId == playerId);
                        int totalRegionsInSystem = stellarBody.Regions.Count;

                        if (ownedRegionsInSystem + 1 == totalRegionsInSystem)
                        {
                            score += 800.0;
                        }

                        score += starSystem.HyperspaceLanes.Count * 100.0;
                    }
                }
            }
        }
        else if (move.TargetLocationType == LocationType.HyperspaceLaneMouth)
        {
            score += 700.0;

            var lane = game.GetAllHyperspaceLanes()
                .FirstOrDefault(l => l.MouthAId == move.TargetLocationId || l.MouthBId == move.TargetLocationId);

            if (lane != null)
            {
                var oppositeMouthId = lane.GetOppositeMouthId(move.TargetLocationId);
                var oppositeMouthOwnerId = GetLaneMouthOwnerId(lane, oppositeMouthId);

                if (oppositeMouthOwnerId == playerId)
                {
                    score += 1000.0;
                }
            }
        }

        var aiConfig = GetAIConfiguration(game, playerId, DifficultyLevel.Medium);
        score *= aiConfig.ExpansionPriority;

        return score;
    }

    private double CalculateConsolidationScore(Game game, MovementAction move, string playerId)
    {
        double score = 200.0;

        if (move.TargetLocationType == LocationType.Region)
        {
            var targetRegion = game.GetAllRegions().FirstOrDefault(r => r.Id == move.TargetLocationId);
            if (targetRegion != null)
            {
                var stellarBody = game.StarSystems
                    .SelectMany(s => s.StellarBodies)
                    .FirstOrDefault(b => b.Regions.Any(r => r.Id == targetRegion.Id));

                if (stellarBody != null)
                {
                    bool isFrontline = IsFrontlineRegion(game, targetRegion, playerId);
                    if (isFrontline)
                    {
                        score += 400.0;
                    }

                    int currentGarrison = targetRegion.Army?.UnitCount ?? 0;
                    score += currentGarrison * 10.0;

                    var starSystem = game.StarSystems.FirstOrDefault(s => s.Id == stellarBody.StarSystemId);
                    if (starSystem != null)
                    {
                        score += starSystem.HyperspaceLanes.Count * 50.0;

                        if (starSystem.Type == StarSystemType.Home)
                        {
                            score += 300.0;
                        }
                        else if (starSystem.Type == StarSystemType.Featured)
                        {
                            score += 150.0;
                        }
                    }
                }
            }
        }
        else if (move.TargetLocationType == LocationType.HyperspaceLaneMouth)
        {
            score += 300.0;

            var lane = game.GetAllHyperspaceLanes()
                .FirstOrDefault(l => l.MouthAId == move.TargetLocationId || l.MouthBId == move.TargetLocationId);

            if (lane != null)
            {
                var oppositeMouthId = lane.GetOppositeMouthId(move.TargetLocationId);
                var oppositeMouthOwnerId = GetLaneMouthOwnerId(lane, oppositeMouthId);
                var oppositeMouthArmy = GetLaneMouthArmy(lane, oppositeMouthId);

                if (oppositeMouthOwnerId != playerId && oppositeMouthOwnerId != null && oppositeMouthArmy != null)
                {
                    score += oppositeMouthArmy.UnitCount * 15.0;
                }
            }
        }

        return score;
    }

    private double CalculateDefensiveScore(Game game, MovementAction move, string playerId)
    {
        double score = 400.0;

        if (move.TargetLocationType == LocationType.Region)
        {
            var targetRegion = game.GetAllRegions().FirstOrDefault(r => r.Id == move.TargetLocationId);
            if (targetRegion != null)
            {
                var stellarBody = game.StarSystems
                    .SelectMany(s => s.StellarBodies)
                    .FirstOrDefault(b => b.Regions.Any(r => r.Id == targetRegion.Id));

                if (stellarBody != null)
                {
                    double enemyThreat = CalculateEnemyThreatToRegion(game, targetRegion, playerId);
                    score += enemyThreat * 20.0;

                    double regionValue = Region.BaseProductionPerRegion * stellarBody.GetYieldMultiplier();
                    score += regionValue * 2.5;

                    int currentGarrison = targetRegion.Army?.UnitCount ?? 0;
                    int minimumGarrison = CalculateMinimumGarrison(stellarBody);

                    if (currentGarrison < minimumGarrison)
                    {
                        score += (minimumGarrison - currentGarrison) * 100.0;
                    }

                    var starSystem = game.StarSystems.FirstOrDefault(s => s.Id == stellarBody.StarSystemId);
                    if (starSystem != null)
                    {
                        if (starSystem.Type == StarSystemType.Home)
                        {
                            score += 1000.0;
                        }
                        else if (starSystem.Type == StarSystemType.Featured)
                        {
                            score += 500.0;
                        }
                    }
                }
            }
        }
        else if (move.TargetLocationType == LocationType.HyperspaceLaneMouth)
        {
            score += 600.0;

            var lane = game.GetAllHyperspaceLanes()
                .FirstOrDefault(l => l.MouthAId == move.TargetLocationId || l.MouthBId == move.TargetLocationId);

            if (lane != null)
            {
                var oppositeMouthId = lane.GetOppositeMouthId(move.TargetLocationId);
                var oppositeMouthArmy = GetLaneMouthArmy(lane, oppositeMouthId);

                if (oppositeMouthArmy != null)
                {
                    score += oppositeMouthArmy.UnitCount * 25.0;
                }
            }
        }

        var aiConfig = GetAIConfiguration(game, playerId, DifficultyLevel.Medium);
        score *= (2.0 - aiConfig.DefenseThreshold);

        return score;
    }

    private bool IsFrontlineRegion(Game game, Region region, string playerId)
    {
        var stellarBody = game.StarSystems
            .SelectMany(s => s.StellarBodies)
            .FirstOrDefault(b => b.Regions.Any(r => r.Id == region.Id));

        if (stellarBody == null)
            return false;

        foreach (var otherRegion in stellarBody.Regions)
        {
            if (otherRegion.OwnerId != playerId && otherRegion.OwnerId != null)
                return true;
        }

        var starSystem = game.StarSystems.FirstOrDefault(s => s.Id == stellarBody.StarSystemId);
        if (starSystem == null)
            return false;

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
                    return true;
            }
        }

        return false;
    }

    private double CalculateEnemyThreatToRegion(Game game, Region region, string playerId)
    {
        double threat = 0.0;

        var stellarBody = game.StarSystems
            .SelectMany(s => s.StellarBodies)
            .FirstOrDefault(b => b.Regions.Any(r => r.Id == region.Id));

        if (stellarBody == null)
            return threat;

        foreach (var otherRegion in stellarBody.Regions)
        {
            if (otherRegion.OwnerId != playerId && otherRegion.OwnerId != null)
            {
                int enemyUnits = otherRegion.Army?.UnitCount ?? 0;
                threat += enemyUnits * 2.0;
            }
        }

        var starSystem = game.StarSystems.FirstOrDefault(s => s.Id == stellarBody.StarSystemId);
        if (starSystem == null)
            return threat;

        foreach (var lane in starSystem.HyperspaceLanes)
        {
            var mouthId = GetLaneMouthIdForStarSystem(lane, starSystem.Id);
            var oppositeMouthId = lane.GetOppositeMouthId(mouthId);
            var oppositeMouthOwnerId = GetLaneMouthOwnerId(lane, oppositeMouthId);
            var oppositeMouthArmy = GetLaneMouthArmy(lane, oppositeMouthId);

            if (oppositeMouthOwnerId != playerId && oppositeMouthOwnerId != null && oppositeMouthArmy != null)
            {
                threat += oppositeMouthArmy.UnitCount * 1.5;
            }
        }

        return threat;
    }

    private MovementAction? SelectMoveByDifficulty(
        List<MovementAction> moves,
        DifficultyLevel difficultyLevel)
    {
        if (moves.Count == 0)
            return null;

        var sortedMoves = moves.OrderByDescending(m => m.Score).ToList();

        return difficultyLevel switch
        {
            DifficultyLevel.Hard => sortedMoves.First(),
            DifficultyLevel.Medium => SelectTopThreeWithRandomness(sortedMoves),
            DifficultyLevel.Easy => SelectWithHighRandomness(sortedMoves),
            _ => sortedMoves.First()
        };
    }

    private MovementAction SelectTopThreeWithRandomness(List<MovementAction> sortedMoves)
    {
        var topThree = sortedMoves.Take(3).ToList();
        return topThree[_random.Next(topThree.Count)];
    }

    private MovementAction SelectWithHighRandomness(List<MovementAction> sortedMoves)
    {
        double totalScore = sortedMoves.Sum(m => Math.Max(1.0, m.Score));
        double roll = _random.NextDouble() * totalScore;
        double cumulative = 0.0;

        foreach (var move in sortedMoves)
        {
            cumulative += Math.Max(1.0, move.Score);
            if (roll <= cumulative)
            {
                return move;
            }
        }

        return sortedMoves.First();
    }

    private AIConfiguration GetAIConfiguration(Game game, string playerId, DifficultyLevel difficultyLevel)
    {
        var aiPlayer = game.Players.OfType<AIPlayer>().FirstOrDefault(p => p.Id == playerId);
        if (aiPlayer != null)
        {
            return aiPlayer.AIConfiguration;
        }

        return AIConfiguration.CreateForDifficulty(difficultyLevel);
    }

    private string GetLaneMouthIdForStarSystem(HyperspaceLane lane, string starSystemId)
    {
        if (lane.StarSystemAId == starSystemId)
            return lane.MouthAId;
        if (lane.StarSystemBId == starSystemId)
            return lane.MouthBId;
        return string.Empty;
    }

    private string? GetLaneMouthOwnerId(HyperspaceLane lane, string mouthId)
    {
        if (lane.MouthAId == mouthId)
            return lane.MouthAOwnerId;
        if (lane.MouthBId == mouthId)
            return lane.MouthBOwnerId;
        return null;
    }

    private Army? GetLaneMouthArmy(HyperspaceLane lane, string mouthId)
    {
        if (lane.MouthAId == mouthId)
            return lane.MouthAArmy;
        if (lane.MouthBId == mouthId)
            return lane.MouthBArmy;
        return null;
    }
}

public class MovementAction
{
    public string ArmyId { get; set; } = string.Empty;
    public string SourceLocationId { get; set; } = string.Empty;
    public LocationType SourceLocationType { get; set; }
    public string TargetLocationId { get; set; } = string.Empty;
    public LocationType TargetLocationType { get; set; }
    public MovementActionType ActionType { get; set; }
    public int UnitCount { get; set; }
    public CombatPrediction? CombatPrediction { get; set; }
    public double Score { get; set; }
}

public enum MovementActionType
{
    Offensive,
    Expansion,
    Consolidation,
    Defensive
}
