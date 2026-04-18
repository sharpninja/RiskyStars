using Grpc.Core;
using Grpc.Net.Client;
using RiskyStars.Shared;

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
var url = "http://localhost:54123";
using var channel = GrpcChannel.ForAddress(url);
var lobbyClient = new LobbyService.LobbyServiceClient(channel);
var auth = await lobbyClient.AuthenticateAsync(new AuthenticateRequest { PlayerName = "SmokePlayer" });
Console.WriteLine($"AUTH success={auth.Success} player={auth.PlayerId}");
if (!auth.Success || string.IsNullOrWhiteSpace(auth.AuthToken) || string.IsNullOrWhiteSpace(auth.PlayerId))
{
    Environment.Exit(2);
}
var metadata = new Metadata { { "Authorization", $"Bearer {auth.AuthToken}" } };
var start = await lobbyClient.StartSinglePlayerGameAsync(new StartSinglePlayerGameRequest
{
    PlayerName = "SmokePlayer",
    MapName = "Default",
    AiPlayers = { new SinglePlayerAISlot { PlayerName = "SmokeAI", Difficulty = "Medium" } }
}, metadata);
Console.WriteLine($"START success={start.Success} session={start.SessionId} player={start.PlayerId} message={start.Message}");
if (!start.Success || string.IsNullOrWhiteSpace(start.SessionId) || string.IsNullOrWhiteSpace(start.PlayerId))
{
    Environment.Exit(3);
}
var gameClient = new GameService.GameServiceClient(channel);
using var call = gameClient.PlayGame();
await call.RequestStream.WriteAsync(new GamePlayerAction
{
    PlayerId = start.PlayerId,
    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
    Connect = new GameConnectAction
    {
        PlayerName = "SmokePlayer",
        SessionId = start.SessionId
    }
});
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
if (!await call.ResponseStream.MoveNext(cts.Token))
{
    Console.WriteLine("NO_UPDATE");
    Environment.Exit(4);
}
var update = call.ResponseStream.Current;
Console.WriteLine($"UPDATE connection={update.ConnectionStatus?.Status} message={update.ConnectionStatus?.Message} error={update.Error?.ErrorMessage}");
await call.RequestStream.CompleteAsync();
