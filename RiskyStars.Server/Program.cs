using RiskyStars.Server;
using RiskyStars.Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<GamePersistenceOptions>(
    builder.Configuration.GetSection("GamePersistence"));

builder.Services.AddGrpc();
builder.Services.AddSingleton<GameRepository>();
builder.Services.AddSingleton<GameStateManager>();
builder.Services.AddSingleton<GameSessionManager>();
builder.Services.AddHostedService<SessionCleanupService>();
builder.Services.AddHostedService<GameRecoveryService>();

var app = builder.Build();

app.MapGrpcService<GameServiceImpl>();
app.MapGrpcService<TurnBasedGameServiceImpl>();
app.MapGrpcService<RiskyStarsGameServiceImpl>();
app.MapGrpcService<LobbyServiceImpl>();

app.MapGet("/", () => "RiskyStars gRPC Server");

app.Run();
