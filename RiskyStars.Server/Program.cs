using RiskyStars.Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();

var app = builder.Build();

app.MapGrpcService<GameServiceImpl>();

app.MapGet("/", () => "RiskyStars gRPC Server");

app.Run();
