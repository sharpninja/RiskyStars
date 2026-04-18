using RiskyStars.Server;
using RiskyStars.Server.Services;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Configure application settings
builder.Services.Configure<ServerOptions>(
    builder.Configuration.GetSection("Server"));
builder.Services.Configure<GamePersistenceOptions>(
    builder.Configuration.GetSection("GamePersistence"));
builder.Services.Configure<SessionManagementOptions>(
    builder.Configuration.GetSection("SessionManagement"));
builder.Services.Configure<GrpcOptions>(
    builder.Configuration.GetSection("Grpc"));

// Get configuration objects
var serverConfig = builder.Configuration.GetSection("Server").Get<ServerOptions>() ?? new ServerOptions();
var grpcConfig = builder.Configuration.GetSection("Grpc").Get<GrpcOptions>() ?? new GrpcOptions();
var listenUri = ResolveListenUri(builder.Configuration["urls"], serverConfig);

// Configure Kestrel for gRPC
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(listenUri.Port, listenOptions =>
    {
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
    });
    
    // Configure connection limits from appsettings
    var kestrelLimits = builder.Configuration.GetSection("Kestrel:Limits");
    if (kestrelLimits.Exists())
    {
        var maxConnections = kestrelLimits.GetValue<int?>("MaxConcurrentConnections");
        if (maxConnections.HasValue)
        {
            options.Limits.MaxConcurrentConnections = maxConnections.Value;
        }
        
        var maxUpgradedConnections = kestrelLimits.GetValue<int?>("MaxConcurrentUpgradedConnections");
        if (maxUpgradedConnections.HasValue)
        {
            options.Limits.MaxConcurrentUpgradedConnections = maxUpgradedConnections.Value;
        }
        
        var maxRequestBodySize = kestrelLimits.GetValue<long?>("MaxRequestBodySize");
        if (maxRequestBodySize.HasValue)
        {
            options.Limits.MaxRequestBodySize = maxRequestBodySize.Value;
        }
    }
});

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Logging levels are configured in appsettings.json
if (builder.Environment.IsDevelopment())
{
    builder.Logging.SetMinimumLevel(LogLevel.Debug);
}
else
{
    builder.Logging.SetMinimumLevel(LogLevel.Information);
}

// Add gRPC services with configuration
builder.Services.AddGrpc(options =>
{
    options.MaxReceiveMessageSize = grpcConfig.MaxReceiveMessageSize;
    options.MaxSendMessageSize = grpcConfig.MaxSendMessageSize;
    options.EnableDetailedErrors = grpcConfig.EnableDetailedErrors || builder.Environment.IsDevelopment();
    
    // Enable response compression
    options.ResponseCompressionAlgorithm = "gzip";
    options.ResponseCompressionLevel = System.IO.Compression.CompressionLevel.Optimal;
});

// Add response compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

// Register core game services as singletons
builder.Services.AddSingleton<GameRepository>();
builder.Services.AddSingleton<GameStateManager>();
builder.Services.AddSingleton<GameSessionManager>();

// Register game logic services as singletons
builder.Services.AddSingleton<MapService>();
builder.Services.AddSingleton<ResourceManager>();
builder.Services.AddSingleton<HeroManager>();
builder.Services.AddSingleton<AllianceManager>();

// Register combat services
// Using transient to allow for different seeds per game/session
builder.Services.AddTransient<CombatResolver>();
builder.Services.AddTransient(sp => new CombatManager(sp.GetRequiredService<CombatResolver>()));

// Register stellar body upgrade system
builder.Services.AddSingleton<StellarBodyUpgradeSystem>();

// Register AI services
builder.Services.AddSingleton<RiskyStars.Server.Entities.GameStateEvaluator>();
builder.Services.AddSingleton<CombatPredictor>();
builder.Services.AddSingleton<AIPurchaseDecisionMaker>();
builder.Services.AddSingleton(sp => new RiskyStars.Server.Entities.AIReinforcementPlanner(
    sp.GetRequiredService<RiskyStars.Server.Entities.GameStateEvaluator>()));
builder.Services.AddSingleton(sp => new RiskyStars.Server.Entities.AIMovementPlanner(
    sp.GetRequiredService<RiskyStars.Server.Entities.GameStateEvaluator>(),
    sp.GetRequiredService<CombatPredictor>()));
builder.Services.AddSingleton<AIEconomicManager>();
builder.Services.AddSingleton<AIPlayerController>();

// Register hosted background services
builder.Services.AddHostedService<SessionCleanupService>();
builder.Services.AddHostedService<GameRecoveryService>();

// Configure CORS for development
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .WithExposedHeaders("Grpc-Status", "Grpc-Message", "Grpc-Encoding", "Grpc-Accept-Encoding");
        });
    });
}

// Build the application
var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseCors("AllowAll");
}

// Enable response compression
app.UseResponseCompression();

// Map gRPC service endpoints
app.MapGrpcService<GameServiceImpl>();
app.MapGrpcService<TurnBasedGameServiceImpl>();
app.MapGrpcService<RiskyStarsGameServiceImpl>();
app.MapGrpcService<LobbyServiceImpl>();

// Add HTTP GET endpoints for health checks and service information
app.MapGet("/", () => Results.Ok(new
{
    service = "RiskyStars gRPC Server",
    status = "Running",
    version = "1.0.0",
    environment = app.Environment.EnvironmentName,
    timestamp = DateTimeOffset.UtcNow.ToString("o")
}));

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    timestamp = DateTimeOffset.UtcNow.ToString("o")
}));

app.MapGet("/info", (IOptions<GamePersistenceOptions> persistenceOptions,
                     IOptions<SessionManagementOptions> sessionOptions) => Results.Ok(new
{
    service = "RiskyStars gRPC Server",
    version = "1.0.0",
    environment = app.Environment.EnvironmentName,
    configuration = new
    {
        persistence = new
        {
            savePath = persistenceOptions.Value.SavePath,
            autoSaveEnabled = persistenceOptions.Value.AutoSaveEnabled,
            autoRecoveryEnabled = persistenceOptions.Value.AutoRecoveryEnabled,
            maxBackupsPerGame = persistenceOptions.Value.MaxBackupsPerGame
        },
        sessionManagement = new
        {
            sessionTimeoutMinutes = sessionOptions.Value.SessionTimeoutMinutes,
            cleanupIntervalMinutes = sessionOptions.Value.CleanupIntervalMinutes,
            maxActiveGames = sessionOptions.Value.MaxActiveGames,
            maxPlayersPerGame = sessionOptions.Value.MaxPlayersPerGame
        }
    },
    timestamp = DateTimeOffset.UtcNow.ToString("o")
}));

// Log startup information
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("=================================================");
logger.LogInformation("RiskyStars gRPC Server Starting");
logger.LogInformation("=================================================");
logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);
logger.LogInformation("Listening on: {ListenAddress}", $"{listenUri.Scheme}://0.0.0.0:{listenUri.Port}");
logger.LogInformation("Protocol: HTTP/2 (gRPC)");
logger.LogInformation("Max Receive Message Size: {Size} MB", grpcConfig.MaxReceiveMessageSize / (1024 * 1024));
logger.LogInformation("Max Send Message Size: {Size} MB", grpcConfig.MaxSendMessageSize / (1024 * 1024));
logger.LogInformation("Detailed Errors: {Enabled}", grpcConfig.EnableDetailedErrors || app.Environment.IsDevelopment());
logger.LogInformation("=================================================");

// Run the application
app.Run();

static Uri ResolveListenUri(string? configuredUrls, ServerOptions serverOptions)
{
    if (!string.IsNullOrWhiteSpace(configuredUrls))
    {
        foreach (var candidate in configuredUrls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Uri.TryCreate(candidate, UriKind.Absolute, out var parsedUri))
            {
                return parsedUri;
            }
        }
    }

    var fallbackScheme = serverOptions.UseHttps ? "https" : "http";
    var fallbackPort = serverOptions.UseHttps ? serverOptions.HttpsPort : serverOptions.Port;
    return new Uri($"{fallbackScheme}://0.0.0.0:{fallbackPort}");
}


