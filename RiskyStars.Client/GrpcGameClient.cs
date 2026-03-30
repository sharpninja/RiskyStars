using Grpc.Core;
using Grpc.Net.Client;
using RiskyStars.Shared;
using System.Collections.Concurrent;

namespace RiskyStars.Client;

public class GrpcGameClient : IDisposable
{
    private readonly GrpcChannel _channel;
    private readonly GameService.GameServiceClient _client;
    private readonly ConcurrentQueue<GameUpdate> _gameStateUpdateQueue;
    private AsyncDuplexStreamingCall<GamePlayerAction, GameUpdate>? _stream;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _receiveTask;
    private bool _disposed;

    public bool IsConnected => _stream != null && !_cancellationTokenSource?.IsCancellationRequested == true;

    public int QueuedUpdateCount => _gameStateUpdateQueue.Count;

    public GrpcGameClient(string serverAddress)
    {
        _channel = GrpcChannel.ForAddress(serverAddress);
        _client = new GameService.GameServiceClient(_channel);
        _gameStateUpdateQueue = new ConcurrentQueue<GameUpdate>();
    }

    public async Task ConnectAsync(string playerId, string playerName, string sessionId)
    {
        if (_stream != null)
        {
            throw new InvalidOperationException("Already connected. Disconnect first.");
        }

        _cancellationTokenSource = new CancellationTokenSource();
        _stream = _client.PlayGame(cancellationToken: _cancellationTokenSource.Token);

        _receiveTask = Task.Run(async () => await ReceiveUpdatesAsync(), _cancellationTokenSource.Token);

        var connectAction = new GamePlayerAction
        {
            PlayerId = playerId,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Connect = new GameConnectAction
            {
                PlayerName = playerName,
                SessionId = sessionId
            }
        };

        await SendActionAsync(connectAction);
    }

    public async Task DisconnectAsync(string reason = "Client disconnect")
    {
        if (_stream == null)
        {
            return;
        }

        try
        {
            var disconnectAction = new GamePlayerAction
            {
                PlayerId = string.Empty,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Disconnect = new GameDisconnectAction
                {
                    Reason = reason
                }
            };

            await SendActionAsync(disconnectAction);
            await _stream.RequestStream.CompleteAsync();
        }
        catch (Exception)
        {
        }
        finally
        {
            _cancellationTokenSource?.Cancel();
            
            if (_receiveTask != null)
            {
                try
                {
                    await _receiveTask;
                }
                catch (OperationCanceledException)
                {
                }
            }

            _stream?.Dispose();
            _stream = null;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            _receiveTask = null;
        }
    }

    public async Task SendActionAsync(GamePlayerAction action)
    {
        if (_stream == null)
        {
            throw new InvalidOperationException("Not connected. Call ConnectAsync first.");
        }

        try
        {
            await _stream.RequestStream.WriteAsync(action);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
        {
            throw new InvalidOperationException("Connection was cancelled.", ex);
        }
    }

    public async Task SendMoveArmyAsync(string playerId, string armyId, string targetLocationId, LocationType targetLocationType)
    {
        var action = new GamePlayerAction
        {
            PlayerId = playerId,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            MoveArmy = new MoveArmyAction
            {
                ArmyId = armyId,
                TargetLocationId = targetLocationId,
                TargetLocationType = targetLocationType
            }
        };

        await SendActionAsync(action);
    }

    public async Task SendProduceResourcesAsync(string playerId, string gameId)
    {
        var action = new GamePlayerAction
        {
            PlayerId = playerId,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ProduceResources = new ProduceResourcesAction
            {
                GameId = gameId
            }
        };

        await SendActionAsync(action);
    }

    public async Task SendPurchaseArmiesAsync(string playerId, string gameId, int count)
    {
        var action = new GamePlayerAction
        {
            PlayerId = playerId,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            PurchaseArmies = new PurchaseArmiesAction
            {
                GameId = gameId,
                Count = count
            }
        };

        await SendActionAsync(action);
    }

    public async Task SendReinforceLocationAsync(string playerId, string gameId, string locationId, LocationType locationType, int unitCount)
    {
        var action = new GamePlayerAction
        {
            PlayerId = playerId,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ReinforceLocation = new ReinforceLocationAction
            {
                GameId = gameId,
                LocationId = locationId,
                LocationType = locationType,
                UnitCount = unitCount
            }
        };

        await SendActionAsync(action);
    }

    public async Task SendAdvancePhaseAsync(string playerId, string gameId)
    {
        var action = new GamePlayerAction
        {
            PlayerId = playerId,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            AdvancePhase = new AdvancePhaseAction
            {
                GameId = gameId
            }
        };

        await SendActionAsync(action);
    }

    public bool TryDequeueUpdate(out GameUpdate? update)
    {
        return _gameStateUpdateQueue.TryDequeue(out update);
    }

    public GameUpdate? DequeueUpdate()
    {
        if (_gameStateUpdateQueue.TryDequeue(out var update))
        {
            return update;
        }
        return null;
    }

    public IEnumerable<GameUpdate> DequeueAllUpdates()
    {
        var updates = new List<GameUpdate>();
        while (_gameStateUpdateQueue.TryDequeue(out var update))
        {
            updates.Add(update);
        }
        return updates;
    }

    public void ClearQueue()
    {
        while (_gameStateUpdateQueue.TryDequeue(out _))
        {
        }
    }

    private async Task ReceiveUpdatesAsync()
    {
        if (_stream == null)
        {
            return;
        }

        try
        {
            await foreach (var update in _stream.ResponseStream.ReadAllAsync(_cancellationTokenSource?.Token ?? CancellationToken.None))
            {
                _gameStateUpdateQueue.Enqueue(update);
            }
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
        {
        }
        catch (OperationCanceledException)
        {
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _cancellationTokenSource?.Cancel();

        if (_receiveTask != null)
        {
            try
            {
                _receiveTask.Wait(TimeSpan.FromSeconds(5));
            }
            catch (AggregateException)
            {
            }
        }

        _stream?.Dispose();
        _cancellationTokenSource?.Dispose();
        _channel.Dispose();

        GC.SuppressFinalize(this);
    }
}
