using Grpc.Core;
using RiskyStars.Shared;

namespace RiskyStars.Server.Services;

public class GameServiceImpl : GameService.GameServiceBase
{
    public override Task<PingResponse> Ping(PingRequest request, ServerCallContext context)
    {
        return Task.FromResult(new PingResponse
        {
            Message = $"Server received: {request.Message}"
        });
    }
}
