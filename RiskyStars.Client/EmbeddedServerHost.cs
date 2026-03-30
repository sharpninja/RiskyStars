using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RiskyStars.Server;
using RiskyStars.Server.Services;
using System.Net;

namespace RiskyStars.Client;

public class EmbeddedServerHost
{
    private WebApplication? _app;
    private Task? _runTask;
    private CancellationTokenSource? _cts;
    private GrpcChannel? _channel;
    private readonly string _serverUrl;
    private readonly int _port;

    public GrpcChannel? Channel => _channel;
    public bool IsRunning => _app != null && _runTask != null && !_runTask.IsCompleted;

    public EmbeddedServerHost(int port = 0)
    {
        _port = port == 0 ? GetAvailablePort() : port;
        _serverUrl = $"http://localhost:{_port}";
    }

    public async Task StartAsync()
    {
        if (IsRunning)
        {
            throw new InvalidOperationException("Server is already running");
        }

        _cts = new CancellationTokenSource();

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });

        ConfigureServices(builder);
        ConfigureKestrel(builder);

        _app = builder.Build();

        ConfigureMiddleware(_app);
        MapEndpoints(_app);

        _channel = GrpcChannel.ForAddress(_serverUrl, new GrpcChannelOptions
        {
            HttpHandler = new SocketsHttpHandler
            {
                PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
                KeepAlivePingDelay = TimeSpan.FromSeconds(60),
                KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
                EnableMultipleHttp2Connections = true
            }
        });

        _runTask = Task.Run(async () =>
        {
            try
            {
                await _app.RunAsync(_serverUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Embedded server error: {ex.Message}");
            }
        }, _cts.Token);

        await WaitForServerReady();
    }

    public async Task StopAsync()
    {
        if (!IsRunning)
        {
            return;
        }

        try
        {
            if (_channel != null)
            {
                await _channel.ShutdownAsync();
                _channel.Dispose();
                _channel = null;
            }

            _cts?.Cancel();

            if (_app != null)
            {
                await _app.StopAsync();
                await _app.DisposeAsync();
                _app = null;
            }

            if (_runTask != null)
            {
                try
                {
                    await _runTask;
                }
                catch (OperationCanceledException)
                {
                }
            }

            _cts?.Dispose();
            _cts = null;
            _runTask = null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error stopping embedded server: {ex.Message}");
        }
    }

    private void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.Configure<GamePersistenceOptions>(options =>
        {
            options.SavePath = Path.Combine(Path.GetTempPath(), "RiskyStars", "EmbeddedGameSaves");
            options.AutoSaveEnabled = true;
            options.AutoRecoveryEnabled = false;
            options.MaxBackupsPerGame = 5;
        });

        builder.Services.Configure<SessionManagementOptions>(options =>
        {
            options.SessionTimeoutMinutes = 60;
            options.CleanupIntervalMinutes = 10;
            options.MaxActiveGames = 10;
            options.MaxPlayersPerGame = 8;
        });

        builder.Services.Configure<GrpcOptions>(options =>
        {
            options.MaxReceiveMessageSize = 16 * 1024 * 1024;
            options.MaxSendMessageSize = 16 * 1024 * 1024;
            options.EnableDetailedErrors = true;
        });

        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);

        var grpcOptions = new GrpcOptions
        {
            MaxReceiveMessageSize = 16 * 1024 * 1024,
            MaxSendMessageSize = 16 * 1024 * 1024,
            EnableDetailedErrors = true
        };

        builder.Services.AddGrpc(options =>
        {
            options.MaxReceiveMessageSize = grpcOptions.MaxReceiveMessageSize;
            options.MaxSendMessageSize = grpcOptions.MaxSendMessageSize;
            options.EnableDetailedErrors = grpcOptions.EnableDetailedErrors;
            options.ResponseCompressionAlgorithm = "gzip";
            options.ResponseCompressionLevel = System.IO.Compression.CompressionLevel.Optimal;
        });

        builder.Services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
        });

        builder.Services.AddSingleton<GameRepository>();
        builder.Services.AddSingleton<GameStateManager>();
        builder.Services.AddSingleton<GameSessionManager>();

        builder.Services.AddSingleton<MapService>();
        builder.Services.AddSingleton<ResourceManager>();
        builder.Services.AddSingleton<HeroManager>();
        builder.Services.AddSingleton<AllianceManager>();

        builder.Services.AddTransient<CombatResolver>();
        builder.Services.AddTransient(sp => new CombatManager(sp.GetRequiredService<CombatResolver>()));

        builder.Services.AddSingleton<StellarBodyUpgradeSystem>();
    }

    private void ConfigureKestrel(WebApplicationBuilder builder)
    {
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Listen(IPAddress.Loopback, _port, listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http2;
            });

            options.Limits.MaxConcurrentConnections = 100;
            options.Limits.MaxConcurrentUpgradedConnections = 100;
            options.Limits.MaxRequestBodySize = 16 * 1024 * 1024;
        });
    }

    private void ConfigureMiddleware(WebApplication app)
    {
        app.UseResponseCompression();
    }

    private void MapEndpoints(WebApplication app)
    {
        app.MapGrpcService<GameServiceImpl>();
        app.MapGrpcService<TurnBasedGameServiceImpl>();
        app.MapGrpcService<RiskyStarsGameServiceImpl>();
        app.MapGrpcService<LobbyServiceImpl>();

        app.MapGet("/", () => Results.Ok(new
        {
            service = "RiskyStars Embedded gRPC Server",
            status = "Running",
            version = "1.0.0"
        }));

        app.MapGet("/health", () => Results.Ok(new
        {
            status = "Healthy"
        }));
    }

    private async Task WaitForServerReady()
    {
        var maxAttempts = 50;
        var delayMs = 100;

        for (int i = 0; i < maxAttempts; i++)
        {
            try
            {
                using var httpClient = new HttpClient();
                var response = await httpClient.GetAsync($"{_serverUrl}/health");
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch
            {
            }

            await Task.Delay(delayMs);
        }

        throw new TimeoutException("Embedded server failed to start within the expected time");
    }

    private static int GetAvailablePort()
    {
        using var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
