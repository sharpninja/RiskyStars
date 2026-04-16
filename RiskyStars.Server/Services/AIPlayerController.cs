using RiskyStars.Server.Entities;

namespace RiskyStars.Server.Services;

public class AIPlayerController
{
    private readonly GameStateManager _gameStateManager;
    private readonly AIPurchaseDecisionMaker _purchaseDecisionMaker;
    private readonly AIReinforcementPlanner _reinforcementPlanner;
    private readonly AIMovementPlanner _movementPlanner;
    private readonly AIEconomicManager _economicManager;
    private readonly ILogger<AIPlayerController> _logger;

    public AIPlayerController(
        GameStateManager gameStateManager,
        AIPurchaseDecisionMaker purchaseDecisionMaker,
        AIReinforcementPlanner reinforcementPlanner,
        AIMovementPlanner movementPlanner,
        AIEconomicManager economicManager,
        ILogger<AIPlayerController> logger)
    {
        _gameStateManager = gameStateManager;
        _purchaseDecisionMaker = purchaseDecisionMaker;
        _reinforcementPlanner = reinforcementPlanner;
        _movementPlanner = movementPlanner;
        _economicManager = economicManager;
        _logger = logger;
    }

    public async Task ExecuteAITurnAsync(string gameId, AIPlayer aiPlayer)
    {
        var game = _gameStateManager.GetGame(gameId);
        if (game == null)
        {
            _logger.LogWarning("Game {GameId} not found for AI player {PlayerId}", gameId, aiPlayer.Id);
            return;
        }

        if (game.CurrentPlayer.Id != aiPlayer.Id)
        {
            _logger.LogWarning("AI player {PlayerId} attempted to execute turn but is not current player", aiPlayer.Id);
            return;
        }

        try
        {
            _logger.LogInformation("AI player {PlayerName} ({Difficulty}) starting turn execution", 
                aiPlayer.Name, aiPlayer.DifficultyLevel);

            await ExecuteProductionPhaseAsync(gameId, aiPlayer);
            await ExecutePurchasePhaseAsync(gameId, aiPlayer);
            await ExecuteReinforcementPhaseAsync(gameId, aiPlayer);
            await ExecuteMovementPhaseAsync(gameId, aiPlayer);

            _logger.LogInformation("AI player {PlayerName} completed turn execution", aiPlayer.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing AI turn for player {PlayerId}", aiPlayer.Id);
            throw;
        }
    }

    private async Task ExecuteProductionPhaseAsync(string gameId, AIPlayer aiPlayer)
    {
        var game = _gameStateManager.GetGame(gameId);
        if (game == null || game.CurrentPhase != Entities.TurnPhase.Production)
        {
            return;
        }

        _logger.LogDebug("AI {PlayerName}: Executing production phase", aiPlayer.Name);

        await ApplyThinkingDelayAsync(aiPlayer.DifficultyLevel);

        _gameStateManager.ProduceResources(gameId);
        
        await ApplyThinkingDelayAsync(aiPlayer.DifficultyLevel);
        
        _gameStateManager.AdvancePhase(gameId);
    }

    private async Task ExecutePurchasePhaseAsync(string gameId, AIPlayer aiPlayer)
    {
        var game = _gameStateManager.GetGame(gameId);
        if (game == null || game.CurrentPhase != Entities.TurnPhase.Purchase)
        {
            return;
        }

        _logger.LogDebug("AI {PlayerName}: Executing purchase phase", aiPlayer.Name);

        await ApplyThinkingDelayAsync(aiPlayer.DifficultyLevel);

        var purchaseDecision = _purchaseDecisionMaker.MakePurchaseDecision(game, aiPlayer);
        
        if (purchaseDecision.RecommendedPurchaseCount > 0)
        {
            _logger.LogInformation("AI {PlayerName}: Purchasing {Count} armies", 
                aiPlayer.Name, purchaseDecision.RecommendedPurchaseCount);
            
            _gameStateManager.PurchaseArmies(gameId, aiPlayer.Id, purchaseDecision.RecommendedPurchaseCount);
        }
        else
        {
            _logger.LogDebug("AI {PlayerName}: Not purchasing any armies", aiPlayer.Name);
        }

        await ApplyThinkingDelayAsync(aiPlayer.DifficultyLevel);

        var economicDecision = _economicManager.MakeEconomicDecisions(game, aiPlayer);
        if (economicDecision.UpgradeDecisions.Any() || economicDecision.HeroAssignments.Any())
        {
            _logger.LogInformation("AI {PlayerName}: Executing {UpgradeCount} upgrades and {HeroCount} hero assignments",
                aiPlayer.Name, economicDecision.UpgradeDecisions.Count, economicDecision.HeroAssignments.Count);
            
            _economicManager.ExecuteEconomicDecisions(game, aiPlayer, economicDecision);
        }

        await ApplyThinkingDelayAsync(aiPlayer.DifficultyLevel);

        _gameStateManager.AdvancePhase(gameId);
    }

    private async Task ExecuteReinforcementPhaseAsync(string gameId, AIPlayer aiPlayer)
    {
        var game = _gameStateManager.GetGame(gameId);
        if (game == null || game.CurrentPhase != Entities.TurnPhase.Reinforcement)
        {
            return;
        }

        _logger.LogDebug("AI {PlayerName}: Executing reinforcement phase", aiPlayer.Name);

        await ApplyThinkingDelayAsync(aiPlayer.DifficultyLevel);

        var availableArmies = CalculateAvailableArmiesForReinforcement(game, aiPlayer.Id);
        
        if (availableArmies > 0)
        {
            var reinforcementAllocations = _reinforcementPlanner.AllocateReinforcements(
                game, 
                aiPlayer.Id, 
                availableArmies, 
                aiPlayer.DifficultyLevel);

            foreach (var allocation in reinforcementAllocations)
            {
                var region = game.GetAllRegions().FirstOrDefault(r => r.Id == allocation.Key);
                if (region != null && allocation.Value > 0)
                {
                    _logger.LogDebug("AI {PlayerName}: Reinforcing region {RegionId} with {Count} armies",
                        aiPlayer.Name, allocation.Key, allocation.Value);
                    
                    _gameStateManager.ReinforceLocation(
                        gameId, 
                        aiPlayer.Id, 
                        allocation.Key, 
                        Entities.LocationType.Region, 
                        allocation.Value);
                    
                    await Task.Delay(GetSubActionDelay(aiPlayer.DifficultyLevel));
                }
            }
        }
        else
        {
            _logger.LogDebug("AI {PlayerName}: No armies available for reinforcement", aiPlayer.Name);
        }

        await ApplyThinkingDelayAsync(aiPlayer.DifficultyLevel);

        _gameStateManager.AdvancePhase(gameId);
    }

    private async Task ExecuteMovementPhaseAsync(string gameId, AIPlayer aiPlayer)
    {
        var game = _gameStateManager.GetGame(gameId);
        if (game == null || game.CurrentPhase != Entities.TurnPhase.Movement)
        {
            return;
        }

        _logger.LogDebug("AI {PlayerName}: Executing movement phase", aiPlayer.Name);

        await ApplyThinkingDelayAsync(aiPlayer.DifficultyLevel);

        int maxMovesPerTurn = GetMaxMovesPerTurn(aiPlayer.DifficultyLevel);
        int movesExecuted = 0;

        while (movesExecuted < maxMovesPerTurn)
        {
            game = _gameStateManager.GetGame(gameId);
            if (game == null)
            {
                break;
            }

            var moveAction = _movementPlanner.SelectBestMove(game, aiPlayer.Id, aiPlayer.DifficultyLevel);
            
            if (moveAction == null)
            {
                _logger.LogDebug("AI {PlayerName}: No more valid moves available", aiPlayer.Name);
                break;
            }

            try
            {
                _logger.LogDebug("AI {PlayerName}: Moving army {ArmyId} from {Source} to {Target} ({ActionType})",
                    aiPlayer.Name, moveAction.ArmyId, moveAction.SourceLocationId, 
                    moveAction.TargetLocationId, moveAction.ActionType);

                _gameStateManager.MoveArmy(
                    gameId, 
                    moveAction.ArmyId, 
                    moveAction.TargetLocationId, 
                    moveAction.TargetLocationType);

                movesExecuted++;
                
                await Task.Delay(GetSubActionDelay(aiPlayer.DifficultyLevel));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI {PlayerName}: Failed to execute move for army {ArmyId}", 
                    aiPlayer.Name, moveAction.ArmyId);
                break;
            }
        }

        _logger.LogInformation("AI {PlayerName}: Executed {Count} movements", aiPlayer.Name, movesExecuted);

        await ApplyThinkingDelayAsync(aiPlayer.DifficultyLevel);

        _gameStateManager.AdvancePhase(gameId);
    }

    private int CalculateAvailableArmiesForReinforcement(Game game, string playerId)
    {
        var playerRegions = game.GetAllRegions().Where(r => r.OwnerId == playerId).ToList();
        int totalArmies = 0;

        foreach (var region in playerRegions)
        {
            if (region.Army != null)
            {
                totalArmies += region.Army.UnitCount;
            }
        }

        return totalArmies;
    }

    private int GetMaxMovesPerTurn(DifficultyLevel difficulty)
    {
        return difficulty switch
        {
            DifficultyLevel.Easy => 3,
            DifficultyLevel.Medium => 5,
            DifficultyLevel.Hard => 10,
            _ => 5
        };
    }

    private async Task ApplyThinkingDelayAsync(DifficultyLevel difficulty)
    {
        var (minDelay, maxDelay) = GetThinkingDelayRange(difficulty);
        var random = new Random();
        var delay = random.Next(minDelay, maxDelay);
        
        _logger.LogTrace("Applying thinking delay: {Delay}ms", delay);
        await Task.Delay(delay);
    }

    private (int minDelay, int maxDelay) GetThinkingDelayRange(DifficultyLevel difficulty)
    {
        return difficulty switch
        {
            DifficultyLevel.Easy => (1000, 2000),
            DifficultyLevel.Medium => (2000, 3000),
            DifficultyLevel.Hard => (3000, 4000),
            _ => (2000, 3000)
        };
    }

    private int GetSubActionDelay(DifficultyLevel difficulty)
    {
        return difficulty switch
        {
            DifficultyLevel.Easy => 300,
            DifficultyLevel.Medium => 200,
            DifficultyLevel.Hard => 100,
            _ => 200
        };
    }

    public Task<List<PlayerActionMessage>> GeneratePlayerActionsAsync(string gameId, AIPlayer aiPlayer)
    {
        var actions = new List<PlayerActionMessage>();
        var game = _gameStateManager.GetGame(gameId);
        
        if (game == null || game.CurrentPlayer.Id != aiPlayer.Id)
        {
            return Task.FromResult(actions);
        }

        try
        {
            switch (game.CurrentPhase)
            {
                case Entities.TurnPhase.Production:
                    actions.Add(new PlayerActionMessage
                    {
                        PlayerId = aiPlayer.Id,
                        GameId = gameId,
                        ActionType = PlayerActionType.ProduceResources,
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    });
                    break;

                case Entities.TurnPhase.Purchase:
                    var purchaseDecision = _purchaseDecisionMaker.MakePurchaseDecision(game, aiPlayer);
                    if (purchaseDecision.RecommendedPurchaseCount > 0)
                    {
                        actions.Add(new PlayerActionMessage
                        {
                            PlayerId = aiPlayer.Id,
                            GameId = gameId,
                            ActionType = PlayerActionType.PurchaseArmies,
                            Count = purchaseDecision.RecommendedPurchaseCount,
                            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                        });
                    }
                    break;

                case Entities.TurnPhase.Reinforcement:
                    var availableArmies = CalculateAvailableArmiesForReinforcement(game, aiPlayer.Id);
                    if (availableArmies > 0)
                    {
                        var reinforcementAllocations = _reinforcementPlanner.AllocateReinforcements(
                            game, aiPlayer.Id, availableArmies, aiPlayer.DifficultyLevel);

                        foreach (var allocation in reinforcementAllocations)
                        {
                            actions.Add(new PlayerActionMessage
                            {
                                PlayerId = aiPlayer.Id,
                                GameId = gameId,
                                ActionType = PlayerActionType.ReinforceLocation,
                                LocationId = allocation.Key,
                                LocationType = Entities.LocationType.Region,
                                Count = allocation.Value,
                                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                            });
                        }
                    }
                    break;

                case Entities.TurnPhase.Movement:
                    int maxMoves = GetMaxMovesPerTurn(aiPlayer.DifficultyLevel);
                    for (int i = 0; i < maxMoves; i++)
                    {
                        var moveAction = _movementPlanner.SelectBestMove(game, aiPlayer.Id, aiPlayer.DifficultyLevel);
                        if (moveAction == null)
                        {
                            break;
                        }

                        actions.Add(new PlayerActionMessage
                        {
                            PlayerId = aiPlayer.Id,
                            GameId = gameId,
                            ActionType = PlayerActionType.MoveArmy,
                            ArmyId = moveAction.ArmyId,
                            TargetLocationId = moveAction.TargetLocationId,
                            TargetLocationType = moveAction.TargetLocationType,
                            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                        });

                        var army = game.GetAllArmies().FirstOrDefault(a => a.Id == moveAction.ArmyId);
                        if (army != null)
                        {
                            army.HasMovedThisTurn = true;
                        }
                    }
                    break;
            }

            actions.Add(new PlayerActionMessage
            {
                PlayerId = aiPlayer.Id,
                GameId = gameId,
                ActionType = PlayerActionType.AdvancePhase,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating player actions for AI {PlayerId}", aiPlayer.Id);
        }

        return Task.FromResult(actions);
    }

    public bool ShouldExecuteAITurn(string gameId)
    {
        var game = _gameStateManager.GetGame(gameId);
        if (game == null)
        {
            return false;
        }

        return game.CurrentPlayer is AIPlayer;
    }

    public AIPlayer? GetCurrentAIPlayer(string gameId)
    {
        var game = _gameStateManager.GetGame(gameId);
        if (game == null)
        {
            return null;
        }

        return game.CurrentPlayer as AIPlayer;
    }

    public async Task ProcessAITurnIfNeededAsync(string gameId)
    {
        var aiPlayer = GetCurrentAIPlayer(gameId);
        if (aiPlayer != null)
        {
            _logger.LogDebug("Detected AI player turn for {PlayerName}, scheduling execution", aiPlayer.Name);
            
            await Task.Run(async () =>
            {
                await Task.Delay(500);
                await ExecuteAITurnAsync(gameId, aiPlayer);
            });
        }
    }
}

public class PlayerActionMessage
{
    public string PlayerId { get; set; } = string.Empty;
    public string GameId { get; set; } = string.Empty;
    public PlayerActionType ActionType { get; set; }
    public string? ArmyId { get; set; }
    public string? LocationId { get; set; }
    public Entities.LocationType? LocationType { get; set; }
    public string? TargetLocationId { get; set; }
    public Entities.LocationType? TargetLocationType { get; set; }
    public int Count { get; set; }
    public long Timestamp { get; set; }
}

public enum PlayerActionType
{
    ProduceResources,
    PurchaseArmies,
    ReinforceLocation,
    MoveArmy,
    AdvancePhase
}
