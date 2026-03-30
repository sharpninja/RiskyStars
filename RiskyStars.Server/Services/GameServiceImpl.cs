using System.Collections.Concurrent;
using System.Threading.Channels;
using Grpc.Core;
using RiskyStars.Shared;

namespace RiskyStars.Server.Services;

public class GameServiceImpl : GameService.GameServiceBase
{
    private readonly GameSessionManager _sessionManager;
    private readonly GameStateManager _gameStateManager;
    private readonly ConcurrentDictionary<string, PlayerStreamContext> _activeStreams = new();

    public GameServiceImpl(GameSessionManager sessionManager, GameStateManager gameStateManager)
    {
        _sessionManager = sessionManager;
        _gameStateManager = gameStateManager;
    }

    public override Task<PingResponse> Ping(PingRequest request, ServerCallContext context)
    {
        return Task.FromResult(new PingResponse
        {
            Message = $"Server received: {request.Message}"
        });
    }

    public override async Task PlayGame(
        IAsyncStreamReader<GamePlayerAction> requestStream,
        IServerStreamWriter<GameUpdate> responseStream,
        ServerCallContext context)
    {
        string? playerId = null;
        string? sessionId = null;
        string? gameId = null;
        Channel<Shared.TurnBasedGameStateUpdate>? gameStateChannel = null;

        try
        {
            var firstAction = await requestStream.MoveNext(context.CancellationToken)
                ? requestStream.Current
                : null;

            if (firstAction?.Connect == null)
            {
                await SendConnectionError(responseStream, "First action must be a connect action");
                return;
            }

            playerId = firstAction.PlayerId;
            sessionId = firstAction.Connect.SessionId;

            if (string.IsNullOrEmpty(playerId))
            {
                await SendConnectionError(responseStream, "Player ID is required");
                return;
            }

            if (string.IsNullOrEmpty(sessionId))
            {
                await SendConnectionError(responseStream, "Session ID is required");
                return;
            }

            var session = _sessionManager.GetSession(sessionId);
            if (session == null)
            {
                await SendConnectionError(responseStream, "Invalid session");
                return;
            }

            if (!session.PlayerIds.Contains(playerId))
            {
                await SendConnectionError(responseStream, "Player not part of this session");
                return;
            }

            gameId = session.GameId;
            var game = _gameStateManager.GetGame(gameId);
            if (game == null)
            {
                await SendConnectionError(responseStream, "Game not found");
                return;
            }

            gameStateChannel = _gameStateManager.SubscribeToGameUpdates(gameId);

            var streamContext = new PlayerStreamContext
            {
                PlayerId = playerId,
                SessionId = sessionId,
                GameId = gameId,
                ResponseStream = responseStream,
                CancellationToken = context.CancellationToken
            };
            _activeStreams[playerId] = streamContext;

            var connectionUpdate = new GameUpdate
            {
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                GameId = gameId,
                ConnectionStatus = new GameConnectionStatus
                {
                    PlayerId = playerId,
                    Status = GameConnectionStatus.Types.ConnectionState.Connected,
                    Message = $"Connected to game {gameId}"
                }
            };
            await responseStream.WriteAsync(connectionUpdate);

            var broadcastTask = Task.Run(async () =>
            {
                try
                {
                    await foreach (var update in gameStateChannel.Reader.ReadAllAsync(context.CancellationToken))
                    {
                        var gameUpdate = ConvertToGameUpdate(update);
                        await BroadcastToAllPlayers(gameId, gameUpdate);
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception)
                {
                }
            });

            var receiveTask = Task.Run(async () =>
            {
                try
                {
                    while (await requestStream.MoveNext(context.CancellationToken))
                    {
                        var action = requestStream.Current;
                        _sessionManager.UpdatePlayerActivity(playerId);
                        await ProcessPlayerAction(action, playerId, gameId);
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (RpcException)
                {
                    throw;
                }
            });

            await Task.WhenAny(broadcastTask, receiveTask);
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new RpcException(new Status(StatusCode.Internal, $"Internal error: {ex.Message}"));
        }
        finally
        {
            if (!string.IsNullOrEmpty(playerId))
            {
                _activeStreams.TryRemove(playerId, out _);

                if (!string.IsNullOrEmpty(gameId) && gameStateChannel != null)
                {
                    _gameStateManager.UnsubscribeFromGameUpdates(gameId, gameStateChannel);
                }

                var disconnectUpdate = new GameUpdate
                {
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    GameId = gameId ?? "",
                    ConnectionStatus = new GameConnectionStatus
                    {
                        PlayerId = playerId,
                        Status = GameConnectionStatus.Types.ConnectionState.Disconnected,
                        Message = "Player disconnected"
                    }
                };

                if (!string.IsNullOrEmpty(gameId))
                {
                    await BroadcastToAllPlayers(gameId, disconnectUpdate, playerId);
                }
            }
        }
    }

    private async Task ProcessPlayerAction(GamePlayerAction action, string playerId, string gameId)
    {
        try
        {
            switch (action.ActionCase)
            {
                case GamePlayerAction.ActionOneofCase.ProduceResources:
                    _gameStateManager.ProduceResources(gameId);
                    break;

                case GamePlayerAction.ActionOneofCase.PurchaseArmies:
                    _gameStateManager.PurchaseArmies(gameId, playerId, action.PurchaseArmies.Count);
                    break;

                case GamePlayerAction.ActionOneofCase.ReinforceLocation:
                    var reinforceAction = action.ReinforceLocation;
                    var locationType = ConvertLocationType(reinforceAction.LocationType);
                    _gameStateManager.ReinforceLocation(
                        gameId,
                        playerId,
                        reinforceAction.LocationId,
                        locationType,
                        reinforceAction.UnitCount);
                    break;

                case GamePlayerAction.ActionOneofCase.MoveArmy:
                    var moveAction = action.MoveArmy;
                    var targetLocationType = ConvertLocationType(moveAction.TargetLocationType);
                    _gameStateManager.MoveArmy(
                        gameId,
                        moveAction.ArmyId,
                        moveAction.TargetLocationId,
                        targetLocationType);
                    break;

                case GamePlayerAction.ActionOneofCase.AdvancePhase:
                    _gameStateManager.AdvancePhase(gameId);
                    break;

                case GamePlayerAction.ActionOneofCase.Disconnect:
                    break;

                default:
                    break;
            }
        }
        catch (Exception ex)
        {
            var errorUpdate = new GameUpdate
            {
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                GameId = gameId,
                Error = new GameErrorMessage
                {
                    ErrorCode = "ACTION_FAILED",
                    ErrorMessage = $"Failed to process action: {ex.Message}",
                    Details = ex.GetType().Name
                }
            };

            if (_activeStreams.TryGetValue(playerId, out var streamContext))
            {
                await streamContext.ResponseStream.WriteAsync(errorUpdate);
            }
        }
    }

    private async Task BroadcastToAllPlayers(string gameId, GameUpdate update, string? excludePlayerId = null)
    {
        var playersToNotify = _activeStreams.Values
            .Where(s => s.GameId == gameId && s.PlayerId != excludePlayerId)
            .ToList();

        foreach (var streamContext in playersToNotify)
        {
            try
            {
                await streamContext.ResponseStream.WriteAsync(update);
            }
            catch (Exception)
            {
            }
        }
    }

    private GameUpdate ConvertToGameUpdate(Shared.TurnBasedGameStateUpdate turnBasedUpdate)
    {
        var update = new GameUpdate
        {
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            GameId = turnBasedUpdate.GameId,
            GameState = turnBasedUpdate
        };

        return update;
    }

    private Entities.LocationType ConvertLocationType(Shared.LocationType protoType)
    {
        return protoType switch
        {
            Shared.LocationType.Region => Entities.LocationType.Region,
            Shared.LocationType.HyperspaceLaneMouth => Entities.LocationType.HyperspaceLaneMouth,
            _ => Entities.LocationType.Region
        };
    }

    private async Task SendConnectionError(IServerStreamWriter<GameUpdate> responseStream, string message)
    {
        var errorUpdate = new GameUpdate
        {
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            GameId = "",
            ConnectionStatus = new GameConnectionStatus
            {
                PlayerId = "",
                Status = GameConnectionStatus.Types.ConnectionState.Error,
                Message = message
            }
        };

        await responseStream.WriteAsync(errorUpdate);
    }

    private class PlayerStreamContext
    {
        public string PlayerId { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public string GameId { get; set; } = string.Empty;
        public IServerStreamWriter<GameUpdate> ResponseStream { get; set; } = null!;
        public CancellationToken CancellationToken { get; set; }
    }
}
