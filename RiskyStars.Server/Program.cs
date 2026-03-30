using RiskyStars.Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddSingleton<GameStateManager>();

var app = builder.Build();

app.MapGrpcService<GameServiceImpl>();
app.MapGrpcService<TurnBasedGameServiceImpl>();

app.MapGet("/", () => "RiskyStars gRPC Server");

app.Run();
