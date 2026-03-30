using RiskyStars.Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddSingleton<GameStateManager>();
builder.Services.AddSingleton<GameSessionManager>();
builder.Services.AddHostedService<SessionCleanupService>();

var app = builder.Build();

app.MapGrpcService<GameServiceImpl>();
app.MapGrpcService<TurnBasedGameServiceImpl>();
app.MapGrpcService<RiskyStarsGameServiceImpl>();
app.MapGrpcService<LobbyServiceImpl>();

app.MapGet("/", () => "RiskyStars gRPC Server");

app.Run();
