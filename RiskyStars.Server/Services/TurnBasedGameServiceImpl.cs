using Grpc.Core;
using RiskyStars.Shared;

namespace RiskyStars.Server.Services;

public class TurnBasedGameServiceImpl : TurnBasedGameService.TurnBasedGameServiceBase
{
    private readonly GameStateManager _gameStateManager;

    public TurnBasedGameServiceImpl(GameStateManager gameStateManager)
    {
        _gameStateManager = gameStateManager;
    }

    public override async Task StreamTurnBasedGameState(TurnBasedGameStateRequest request, IServerStreamWriter<TurnBasedGameStateUpdate> responseStream, ServerCallContext context)
    {
        var channel = _gameStateManager.SubscribeToGameUpdates(request.GameId);

        try
        {
            await foreach (var update in channel.Reader.ReadAllAsync(context.CancellationToken))
            {
                await responseStream.WriteAsync(update);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _gameStateManager.UnsubscribeFromGameUpdates(request.GameId, channel);
        }
    }
}
