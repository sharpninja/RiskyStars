using System.Threading.Channels;
using Grpc.Core;
using RiskyStars.Shared;

namespace RiskyStars.Server.Services;

public class RiskyStarsGameServiceImpl : RiskyStarsGame.RiskyStarsGameBase
{
    private readonly GameSessionManager _sessionManager;
    private readonly GameStateManager _gameStateManager;

    public RiskyStarsGameServiceImpl(GameSessionManager sessionManager, GameStateManager gameStateManager)
    {
        _sessionManager = sessionManager;
        _gameStateManager = gameStateManager;
    }

    public override async Task PlayGame(
        IAsyncStreamReader<PlayerAction> requestStream,
        IServerStreamWriter<GameStateUpdate> responseStream,
        ServerCallContext context)
    {
        string? playerId = null;
        string? sessionId = null;
        Channel<GameStateUpdate>? updateChannel = null;

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

            var authToken = context.RequestHeaders.GetValue("authorization")?.Replace("Bearer ", "");
            
            if (string.IsNullOrEmpty(authToken))
            {
                authToken = _sessionManager.AuthenticatePlayer(firstAction.Connect.PlayerName);
            }

            if (!_sessionManager.ValidateAuthToken(authToken, out playerId))
            {
                await SendConnectionError(responseStream, "Invalid authentication token");
                return;
            }

            sessionId = context.RequestHeaders.GetValue("session-id");
            
            if (string.IsNullOrEmpty(sessionId))
            {
                await SendConnectionError(responseStream, "Session ID required");
                return;
            }

            var session = _sessionManager.GetSession(sessionId);
            if (session == null)
            {
                await SendConnectionError(responseStream, "Invalid session");
                return;
            }

            updateChannel = Channel.CreateUnbounded<GameStateUpdate>();
            _sessionManager.ConnectPlayer(playerId, sessionId, updateChannel);

            var connectionUpdate = new GameStateUpdate
            {
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Tick = 0,
                ConnectionStatus = new ConnectionStatusUpdate
                {
                    PlayerId = playerId,
                    Status = ConnectionStatusUpdate.Types.ConnectionStatus.Connected,
                    Message = $"Welcome {firstAction.Connect.PlayerName}"
                }
            };
            await responseStream.WriteAsync(connectionUpdate);

            var sendTask = Task.Run(async () =>
            {
                try
                {
                    await foreach (var update in updateChannel.Reader.ReadAllAsync(context.CancellationToken))
                    {
                        await responseStream.WriteAsync(update);
                    }
                }
                catch (OperationCanceledException)
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
                        await ProcessPlayerAction(action, playerId, sessionId, updateChannel);
                    }
                }
                catch (OperationCanceledException)
                {
                }
            });

            await Task.WhenAny(sendTask, receiveTask);
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
                _sessionManager.DisconnectPlayer(playerId);
                
                if (updateChannel != null)
                {
                    var disconnectUpdate = new GameStateUpdate
                    {
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        Tick = 0,
                        ConnectionStatus = new ConnectionStatusUpdate
                        {
                            PlayerId = playerId,
                            Status = ConnectionStatusUpdate.Types.ConnectionStatus.Disconnected,
                            Message = "Player disconnected"
                        }
                    };

                    if (!string.IsNullOrEmpty(sessionId))
                    {
                        _sessionManager.BroadcastToSession(sessionId, disconnectUpdate);
                    }
                }
            }
        }
    }

    private async Task ProcessPlayerAction(
        PlayerAction action,
        string playerId,
        string sessionId,
        Channel<GameStateUpdate> updateChannel)
    {
        var session = _sessionManager.GetSession(sessionId);
        if (session == null)
        {
            return;
        }

        var game = _gameStateManager.GetGame(session.GameId);
        if (game == null)
        {
            return;
        }

        try
        {
            switch (action.ActionCase)
            {
                case PlayerAction.ActionOneofCase.Move:
                    await HandleMoveAction(action.Move, playerId, session.GameId, updateChannel);
                    break;

                case PlayerAction.ActionOneofCase.Attack:
                    await HandleAttackAction(action.Attack, playerId, session.GameId, updateChannel);
                    break;

                case PlayerAction.ActionOneofCase.Build:
                    await HandleBuildAction(action.Build, playerId, session.GameId, updateChannel);
                    break;

                case PlayerAction.ActionOneofCase.Research:
                    await HandleResearchAction(action.Research, playerId, session.GameId, updateChannel);
                    break;

                case PlayerAction.ActionOneofCase.Trade:
                    await HandleTradeAction(action.Trade, playerId, session.GameId, updateChannel);
                    break;

                case PlayerAction.ActionOneofCase.Disconnect:
                    _sessionManager.DisconnectPlayer(playerId);
                    break;

                default:
                    break;
            }
        }
        catch (Exception ex)
        {
            var errorUpdate = new GameStateUpdate
            {
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Tick = 0,
                GameEvent = new GameEvent
                {
                    EventId = Guid.NewGuid().ToString(),
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    EventType = GameEvent.Types.GameEventType.UnknownGameEvent,
                    Description = $"Error processing action: {ex.Message}"
                }
            };
            errorUpdate.GameEvent.AffectedPlayers.Add(playerId);

            await updateChannel.Writer.WriteAsync(errorUpdate);
        }
    }

    private async Task HandleMoveAction(MoveAction move, string playerId, string gameId, Channel<GameStateUpdate> updateChannel)
    {
        await Task.CompletedTask;
    }

    private async Task HandleAttackAction(AttackAction attack, string playerId, string gameId, Channel<GameStateUpdate> updateChannel)
    {
        await Task.CompletedTask;
    }

    private async Task HandleBuildAction(BuildAction build, string playerId, string gameId, Channel<GameStateUpdate> updateChannel)
    {
        await Task.CompletedTask;
    }

    private async Task HandleResearchAction(ResearchAction research, string playerId, string gameId, Channel<GameStateUpdate> updateChannel)
    {
        await Task.CompletedTask;
    }

    private async Task HandleTradeAction(TradeAction trade, string playerId, string gameId, Channel<GameStateUpdate> updateChannel)
    {
        await Task.CompletedTask;
    }

    private async Task SendConnectionError(IServerStreamWriter<GameStateUpdate> responseStream, string message)
    {
        var errorUpdate = new GameStateUpdate
        {
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Tick = 0,
            ConnectionStatus = new ConnectionStatusUpdate
            {
                PlayerId = "",
                Status = ConnectionStatusUpdate.Types.ConnectionStatus.Error,
                Message = message
            }
        };

        await responseStream.WriteAsync(errorUpdate);
    }
}
