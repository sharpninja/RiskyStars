using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Options;
using RiskyStars.Server.Entities;

namespace RiskyStars.Server.Services;

public class GameStateManager
{
    private readonly ConcurrentDictionary<string, Game> _games = new();
    private readonly ConcurrentDictionary<string, List<Channel<Shared.TurnBasedGameStateUpdate>>> _gameSubscribers = new();
    private readonly ConcurrentDictionary<string, CombatManager> _combatManagers = new();
    private readonly object _gameLock = new();
    private readonly GameRepository _gameRepository;
    private readonly ILogger<GameStateManager> _logger;
    private readonly bool _autoSaveEnabled;

    public GameStateManager(GameRepository gameRepository, ILogger<GameStateManager> logger, IOptions<GamePersistenceOptions> options)
    {
        _gameRepository = gameRepository;
        _logger = logger;
        _autoSaveEnabled = options.Value.AutoSaveEnabled;
    }

    public Game? GetGame(string gameId)
    {
        _games.TryGetValue(gameId, out var game);
        return game;
    }

    public void CreateGame(Game game)
    {
        _games[game.Id] = game;
        _gameSubscribers[game.Id] = new List<Channel<Shared.TurnBasedGameStateUpdate>>();
        _combatManagers[game.Id] = new CombatManager(new CombatResolver());
        
        InitializePlayerOwnership(game);
        BroadcastGameStateUpdate(game, "Game created");

        if (_autoSaveEnabled)
        {
            _ = Task.Run(async () => await SaveGameStateAsync(game.Id));
        }
    }

    public CombatManager? GetCombatManager(string gameId)
    {
        _combatManagers.TryGetValue(gameId, out var combatManager);
        return combatManager;
    }

    public Channel<Shared.TurnBasedGameStateUpdate> SubscribeToGameUpdates(string gameId)
    {
        var channel = Channel.CreateUnbounded<Shared.TurnBasedGameStateUpdate>();
        
        if (_gameSubscribers.TryGetValue(gameId, out var subscribers))
        {
            lock (subscribers)
            {
                subscribers.Add(channel);
            }
            
            var game = GetGame(gameId);
            if (game != null)
            {
                var initialUpdate = GenerateGameStateUpdate(game, "Player connected");
                channel.Writer.TryWrite(initialUpdate);
            }
        }
        
        return channel;
    }

    public void UnsubscribeFromGameUpdates(string gameId, Channel<Shared.TurnBasedGameStateUpdate> channel)
    {
        if (_gameSubscribers.TryGetValue(gameId, out var subscribers))
        {
            lock (subscribers)
            {
                subscribers.Remove(channel);
            }
        }
        channel.Writer.TryComplete();
    }

    public void AdvancePhase(string gameId)
    {
        var game = GetGame(gameId);
        if (game == null) return;

        lock (_gameLock)
        {
            var currentPhase = game.CurrentPhase;
            var eventMessage = "";

            switch (currentPhase)
            {
                case Entities.TurnPhase.Production:
                    ExecuteProductionPhase(game);
                    game.CurrentPhase = Entities.TurnPhase.Purchase;
                    eventMessage = $"Production complete. {game.CurrentPlayer.Name} can now purchase armies.";
                    break;

                case Entities.TurnPhase.Purchase:
                    game.CurrentPhase = Entities.TurnPhase.Reinforcement;
                    eventMessage = $"Purchase complete. {game.CurrentPlayer.Name} can now reinforce locations.";
                    break;

                case Entities.TurnPhase.Reinforcement:
                    game.CurrentPhase = Entities.TurnPhase.Movement;
                    eventMessage = $"Reinforcement complete. {game.CurrentPlayer.Name} can now move armies.";
                    break;

                case Entities.TurnPhase.Movement:
                    // Resolve all active combats before advancing to next player
                    var combatManager = GetCombatManager(gameId);
                    if (combatManager != null)
                    {
                        var combatEvents = combatManager.ResolveAllActiveCombatsForTurnEnd();
                        foreach (var combatEvent in combatEvents)
                        {
                            BroadcastCombatEvent(game, combatEvent);
                        }
                    }
                    
                    AdvanceToNextPlayer(game);
                    eventMessage = $"Turn ended. Now {game.CurrentPlayer.Name}'s turn.";
                    
                    if (_autoSaveEnabled)
                    {
                        _ = Task.Run(async () => await SaveGameStateAsync(gameId));
                    }
                    break;
            }

            BroadcastGameStateUpdate(game, eventMessage);
        }
    }

    public void ProduceResources(string gameId)
    {
        var game = GetGame(gameId);
        if (game == null) return;

        lock (_gameLock)
        {
            if (game.CurrentPhase != Entities.TurnPhase.Production)
                throw new InvalidOperationException("Not in production phase");

            ExecuteProductionPhase(game);
            BroadcastGameStateUpdate(game, $"{game.CurrentPlayer.Name} produced resources");
        }
    }

    public void PurchaseArmies(string gameId, string playerId, int count)
    {
        var game = GetGame(gameId);
        if (game == null) return;

        lock (_gameLock)
        {
            if (game.CurrentPhase != Entities.TurnPhase.Purchase)
                throw new InvalidOperationException("Not in purchase phase");

            if (game.CurrentPlayer.Id != playerId)
                throw new InvalidOperationException("Not the current player's turn");

            var player = game.Players.FirstOrDefault(p => p.Id == playerId);
            if (player == null)
                throw new InvalidOperationException("Player not found");

            player.PurchaseArmy(count);
            BroadcastGameStateUpdate(game, $"{player.Name} purchased {count} army unit(s)");
        }
    }

    public void ReinforceLocation(string gameId, string playerId, string locationId, Entities.LocationType locationType, int unitCount)
    {
        var game = GetGame(gameId);
        if (game == null) return;

        lock (_gameLock)
        {
            if (game.CurrentPhase != Entities.TurnPhase.Reinforcement)
                throw new InvalidOperationException("Not in reinforcement phase");

            if (game.CurrentPlayer.Id != playerId)
                throw new InvalidOperationException("Not the current player's turn");

            var player = game.Players.FirstOrDefault(p => p.Id == playerId);
            if (player == null)
                throw new InvalidOperationException("Player not found");

            var army = CreateOrGetArmy(game, playerId, locationId, locationType);
            army.UnitCount += unitCount;

            BroadcastGameStateUpdate(game, $"{player.Name} reinforced location with {unitCount} unit(s)");
        }
    }

    public void MoveArmy(string gameId, string armyId, string targetLocationId, Entities.LocationType targetLocationType)
    {
        var game = GetGame(gameId);
        if (game == null) return;

        lock (_gameLock)
        {
            if (game.CurrentPhase != Entities.TurnPhase.Movement)
                throw new InvalidOperationException("Not in movement phase");

            var army = game.GetAllArmies().FirstOrDefault(a => a.Id == armyId);
            if (army == null)
                throw new InvalidOperationException("Army not found");

            if (army.OwnerId != game.CurrentPlayer.Id)
                throw new InvalidOperationException("Not the current player's army");

            army.Move(targetLocationId, targetLocationType);
            BroadcastGameStateUpdate(game, $"{game.CurrentPlayer.Name} moved army to new location");
        }
    }

    public void UpdateOwnership(string gameId, string locationId, Entities.LocationType locationType, string newOwnerId)
    {
        var game = GetGame(gameId);
        if (game == null) return;

        lock (_gameLock)
        {
            if (locationType == Entities.LocationType.Region)
            {
                var region = game.GetAllRegions().FirstOrDefault(r => r.Id == locationId);
                if (region == null)
                    throw new InvalidOperationException("Region not found");

                var oldOwnerId = region.OwnerId;
                region.OwnerId = newOwnerId;

                UpdatePlayerOwnershipLists(game, oldOwnerId, newOwnerId, locationId, locationType);
                
                var stellarBody = game.StarSystems
                    .SelectMany(s => s.StellarBodies)
                    .FirstOrDefault(b => b.Id == region.StellarBodyId);
                
                if (stellarBody != null && oldOwnerId != null && oldOwnerId != newOwnerId)
                {
                    var allRegionsChanged = stellarBody.Regions.All(r => r.OwnerId == newOwnerId);
                    if (allRegionsChanged)
                    {
                        stellarBody.RemoveHeroes();
                        stellarBody.RemoveUpgrades();
                    }
                }
            }
            else if (locationType == Entities.LocationType.HyperspaceLaneMouth)
            {
                foreach (var lane in game.GetAllHyperspaceLanes())
                {
                    if (lane.MouthAId == locationId)
                    {
                        var oldOwnerId = lane.MouthAOwnerId;
                        lane.MouthAOwnerId = newOwnerId;
                        UpdatePlayerOwnershipLists(game, oldOwnerId, newOwnerId, locationId, locationType);
                        break;
                    }
                    else if (lane.MouthBId == locationId)
                    {
                        var oldOwnerId = lane.MouthBOwnerId;
                        lane.MouthBOwnerId = newOwnerId;
                        UpdatePlayerOwnershipLists(game, oldOwnerId, newOwnerId, locationId, locationType);
                        break;
                    }
                }
            }

            var player = game.Players.FirstOrDefault(p => p.Id == newOwnerId);
            BroadcastGameStateUpdate(game, $"{player?.Name ?? "Unknown"} captured location");
        }
    }

    private void InitializePlayerOwnership(Game game)
    {
        foreach (var player in game.Players)
        {
            player.OwnedRegionIds.Clear();
            player.OwnedHyperspaceLaneMouthIds.Clear();
        }

        foreach (var region in game.GetAllRegions())
        {
            if (!string.IsNullOrEmpty(region.OwnerId))
            {
                var player = game.Players.FirstOrDefault(p => p.Id == region.OwnerId);
                if (player != null && !player.OwnedRegionIds.Contains(region.Id))
                {
                    player.OwnedRegionIds.Add(region.Id);
                }
            }
        }

        foreach (var lane in game.GetAllHyperspaceLanes())
        {
            if (!string.IsNullOrEmpty(lane.MouthAOwnerId))
            {
                var player = game.Players.FirstOrDefault(p => p.Id == lane.MouthAOwnerId);
                if (player != null && !player.OwnedHyperspaceLaneMouthIds.Contains(lane.MouthAId))
                {
                    player.OwnedHyperspaceLaneMouthIds.Add(lane.MouthAId);
                }
            }

            if (!string.IsNullOrEmpty(lane.MouthBOwnerId))
            {
                var player = game.Players.FirstOrDefault(p => p.Id == lane.MouthBOwnerId);
                if (player != null && !player.OwnedHyperspaceLaneMouthIds.Contains(lane.MouthBId))
                {
                    player.OwnedHyperspaceLaneMouthIds.Add(lane.MouthBId);
                }
            }
        }
    }

    private void UpdatePlayerOwnershipLists(Game game, string? oldOwnerId, string newOwnerId, string locationId, Entities.LocationType locationType)
    {
        if (!string.IsNullOrEmpty(oldOwnerId))
        {
            var oldOwner = game.Players.FirstOrDefault(p => p.Id == oldOwnerId);
            if (oldOwner != null)
            {
                if (locationType == Entities.LocationType.Region)
                {
                    oldOwner.OwnedRegionIds.Remove(locationId);
                }
                else
                {
                    oldOwner.OwnedHyperspaceLaneMouthIds.Remove(locationId);
                }
            }
        }

        var newOwner = game.Players.FirstOrDefault(p => p.Id == newOwnerId);
        if (newOwner != null)
        {
            if (locationType == Entities.LocationType.Region)
            {
                if (!newOwner.OwnedRegionIds.Contains(locationId))
                {
                    newOwner.OwnedRegionIds.Add(locationId);
                }
            }
            else
            {
                if (!newOwner.OwnedHyperspaceLaneMouthIds.Contains(locationId))
                {
                    newOwner.OwnedHyperspaceLaneMouthIds.Add(locationId);
                }
            }
        }
    }

    private void ExecuteProductionPhase(Game game)
    {
        var player = game.CurrentPlayer;
        var ownedBodies = game.GetPlayerOwnedBodies(player.Id);
        player.ProduceResources(ownedBodies);
    }

    private void AdvanceToNextPlayer(Game game)
    {
        ResetArmiesForCurrentPlayer(game);

        game.CurrentPlayerIndex++;
        if (game.CurrentPlayerIndex >= game.Players.Count)
        {
            game.CurrentPlayerIndex = 0;
            game.TurnNumber++;
            
            foreach (var player in game.Players)
            {
                if (player.AllianceId == null && player.TurnsSinceLeftAlliance > 0 && player.TurnsSinceLeftAlliance < 4)
                {
                    player.TurnsSinceLeftAlliance++;
                }
            }
        }

        game.CurrentPhase = Entities.TurnPhase.Production;
    }

    private void ResetArmiesForCurrentPlayer(Game game)
    {
        var currentPlayerId = game.CurrentPlayer.Id;
        foreach (var army in game.GetAllArmies().Where(a => a.OwnerId == currentPlayerId))
        {
            army.ResetTurn();
        }
    }

    private Army CreateOrGetArmy(Game game, string playerId, string locationId, Entities.LocationType locationType)
    {
        if (locationType == Entities.LocationType.Region)
        {
            var region = game.GetAllRegions().FirstOrDefault(r => r.Id == locationId);
            if (region == null)
                throw new InvalidOperationException("Region not found");

            if (region.Army == null)
            {
                region.Army = new Army
                {
                    Id = Guid.NewGuid().ToString(),
                    OwnerId = playerId,
                    LocationId = locationId,
                    LocationType = locationType,
                    UnitCount = 0
                };
            }
            return region.Army;
        }
        else
        {
            foreach (var lane in game.GetAllHyperspaceLanes())
            {
                if (lane.MouthAId == locationId)
                {
                    if (lane.MouthAArmy == null)
                    {
                        lane.MouthAArmy = new Army
                        {
                            Id = Guid.NewGuid().ToString(),
                            OwnerId = playerId,
                            LocationId = locationId,
                            LocationType = locationType,
                            UnitCount = 0
                        };
                    }
                    return lane.MouthAArmy;
                }
                else if (lane.MouthBId == locationId)
                {
                    if (lane.MouthBArmy == null)
                    {
                        lane.MouthBArmy = new Army
                        {
                            Id = Guid.NewGuid().ToString(),
                            OwnerId = playerId,
                            LocationId = locationId,
                            LocationType = locationType,
                            UnitCount = 0
                        };
                    }
                    return lane.MouthBArmy;
                }
            }
            throw new InvalidOperationException("Hyperspace lane mouth not found");
        }
    }

    private void BroadcastGameStateUpdate(Game game, string eventMessage)
    {
        var update = GenerateGameStateUpdate(game, eventMessage);
        
        if (_gameSubscribers.TryGetValue(game.Id, out var subscribers))
        {
            lock (subscribers)
            {
                foreach (var channel in subscribers.ToList())
                {
                    channel.Writer.TryWrite(update);
                }
            }
        }
    }

    private void BroadcastCombatEvent(Game game, Shared.CombatEvent combatEvent)
    {
        var update = new Shared.TurnBasedGameStateUpdate
        {
            GameId = game.Id,
            TurnNumber = game.TurnNumber,
            CurrentPhase = ConvertTurnPhase(game.CurrentPhase),
            CurrentPlayerId = game.CurrentPlayer.Id,
            EventMessage = $"Combat event: {combatEvent.EventType}"
        };

        update.CombatEvents.Add(combatEvent);

        if (_gameSubscribers.TryGetValue(game.Id, out var subscribers))
        {
            lock (subscribers)
            {
                foreach (var channel in subscribers.ToList())
                {
                    channel.Writer.TryWrite(update);
                }
            }
        }
    }

    private Shared.TurnBasedGameStateUpdate GenerateGameStateUpdate(Game game, string eventMessage)
    {
        var update = new Shared.TurnBasedGameStateUpdate
        {
            GameId = game.Id,
            TurnNumber = game.TurnNumber,
            CurrentPhase = ConvertTurnPhase(game.CurrentPhase),
            CurrentPlayerId = game.CurrentPlayer.Id,
            EventMessage = eventMessage
        };

        for (int i = 0; i < game.Players.Count; i++)
        {
            var player = game.Players[i];
            update.PlayerStates.Add(new Shared.PlayerState
            {
                PlayerId = player.Id,
                PlayerName = player.Name,
                PopulationStockpile = player.PopulationStockpile,
                MetalStockpile = player.MetalStockpile,
                FuelStockpile = player.FuelStockpile,
                TurnOrder = i
            });
        }

        foreach (var region in game.GetAllRegions())
        {
            if (!string.IsNullOrEmpty(region.OwnerId))
            {
                update.RegionOwnerships.Add(new Shared.RegionOwnership
                {
                    RegionId = region.Id,
                    OwnerId = region.OwnerId
                });
            }
        }

        foreach (var lane in game.GetAllHyperspaceLanes())
        {
            if (!string.IsNullOrEmpty(lane.MouthAOwnerId))
            {
                update.HyperspaceLaneMouthOwnerships.Add(new Shared.HyperspaceLaneMouthOwnership
                {
                    HyperspaceLaneMouthId = lane.MouthAId,
                    OwnerId = lane.MouthAOwnerId
                });
            }

            if (!string.IsNullOrEmpty(lane.MouthBOwnerId))
            {
                update.HyperspaceLaneMouthOwnerships.Add(new Shared.HyperspaceLaneMouthOwnership
                {
                    HyperspaceLaneMouthId = lane.MouthBId,
                    OwnerId = lane.MouthBOwnerId
                });
            }
        }

        foreach (var army in game.GetAllArmies())
        {
            update.ArmyStates.Add(new Shared.ArmyState
            {
                ArmyId = army.Id,
                OwnerId = army.OwnerId,
                UnitCount = army.UnitCount,
                LocationId = army.LocationId,
                LocationType = ConvertLocationType(army.LocationType),
                HasMovedThisTurn = army.HasMovedThisTurn,
                IsInCombat = army.IsInCombat
            });
        }

        return update;
    }

    private Shared.TurnPhase ConvertTurnPhase(Entities.TurnPhase phase)
    {
        return phase switch
        {
            Entities.TurnPhase.Production => Shared.TurnPhase.Production,
            Entities.TurnPhase.Purchase => Shared.TurnPhase.Purchase,
            Entities.TurnPhase.Reinforcement => Shared.TurnPhase.Reinforcement,
            Entities.TurnPhase.Movement => Shared.TurnPhase.Movement,
            _ => Shared.TurnPhase.Production
        };
    }

    private Shared.LocationType ConvertLocationType(Entities.LocationType type)
    {
        return type switch
        {
            Entities.LocationType.Region => Shared.LocationType.Region,
            Entities.LocationType.HyperspaceLaneMouth => Shared.LocationType.HyperspaceLaneMouth,
            _ => Shared.LocationType.Region
        };
    }

    public async Task<bool> SaveGameStateAsync(string gameId)
    {
        var game = GetGame(gameId);
        if (game == null)
        {
            _logger.LogWarning("Cannot save game {GameId}: game not found", gameId);
            return false;
        }

        var combatManager = GetCombatManager(gameId);
        return await _gameRepository.SaveGameStateAsync(game, combatManager);
    }

    public async Task<bool> LoadGameStateAsync(string gameId)
    {
        try
        {
            var result = await _gameRepository.LoadGameStateAsync(gameId);
            if (result == null)
            {
                _logger.LogWarning("Failed to load game state for {GameId}", gameId);
                return false;
            }

            var (game, combatSnapshots) = result.Value;
            
            if (game == null)
            {
                _logger.LogError("Loaded game is null for {GameId}", gameId);
                return false;
            }
            
            _games[game.Id] = game;
            _gameSubscribers[game.Id] = new List<Channel<Shared.TurnBasedGameStateUpdate>>();
            
            var combatManager = new CombatManager(new CombatResolver());
            _combatManagers[game.Id] = combatManager;

            foreach (var combatSnapshot in combatSnapshots)
            {
                var session = combatSnapshot.ToCombatSession();
                
                var allArmies = game.GetAllArmies().ToDictionary(a => a.Id);
                
                var arrivals = new List<ReinforcementArrival>();
                foreach (var arrivalSnapshot in combatSnapshot.ReinforcementArrivals)
                {
                    if (allArmies.TryGetValue(arrivalSnapshot.ArmyId, out var army))
                    {
                        arrivals.Add(new ReinforcementArrival
                        {
                            Army = army,
                            IsAttacker = arrivalSnapshot.IsAttacker,
                            ArrivalOrder = arrivalSnapshot.ArrivalOrder
                        });
                    }
                }
                session.ReinforcementArrivals = arrivals;

                combatManager.RestoreCombatSession(session);
            }

            _logger.LogInformation("Successfully loaded game {GameId} at turn {TurnNumber}", game.Id, game.TurnNumber);
            BroadcastGameStateUpdate(game, "Game loaded from saved state");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading game state for {GameId}", gameId);
            return false;
        }
    }

    public async Task<List<GameStateMetadata>> GetSavedGamesAsync()
    {
        return await _gameRepository.GetAllGameMetadataAsync();
    }

    public async Task<bool> RecoverGameAsync(string gameId)
    {
        if (_games.ContainsKey(gameId))
        {
            _logger.LogWarning("Game {GameId} is already loaded", gameId);
            return false;
        }

        return await LoadGameStateAsync(gameId);
    }

    public async Task RecoverAllGamesAsync()
    {
        _logger.LogInformation("Starting auto-recovery of saved games...");
        
        var savedGameIds = await _gameRepository.GetAllSavedGameIdsAsync();
        var recoveredCount = 0;

        foreach (var gameId in savedGameIds)
        {
            if (!_games.ContainsKey(gameId))
            {
                var success = await LoadGameStateAsync(gameId);
                if (success)
                {
                    recoveredCount++;
                }
            }
        }

        _logger.LogInformation("Auto-recovery complete: {Count} game(s) recovered", recoveredCount);
    }

    public async Task<bool> DeleteGameAsync(string gameId)
    {
        if (_games.TryRemove(gameId, out _))
        {
            _gameSubscribers.TryRemove(gameId, out _);
            _combatManagers.TryRemove(gameId, out _);
        }

        return await _gameRepository.DeleteGameStateAsync(gameId);
    }
}
