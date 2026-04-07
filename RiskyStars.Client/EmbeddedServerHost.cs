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

public enum ServerStatus
{
    Stopped,
    Starting,
    Running,
    Error,
    Reconnecting
}

public class EmbeddedServerHost : IAsyncDisposable, IDisposable
{
    private WebApplication? _app;
    private Task? _runTask;
    private CancellationTokenSource? _cts;
    private GrpcChannel? _channel;
    private SocketsHttpHandler? _httpHandler;
    private readonly string _serverUrl;
    private readonly int _port;
    private bool _disposed;
    private readonly object _lock = new object();
    private ServerHealthMonitor? _healthMonitor;

    public GrpcChannel? Channel => _channel;
    public bool IsRunning => _app != null && _runTask != null && !_runTask.IsCompleted;
    public string? LastError { get; private set; }
    public ServerStatus Status { get; private set; } = ServerStatus.Stopped;
    public ServerHealthMonitor? HealthMonitor => _healthMonitor;

    public EmbeddedServerHost(int port = 0)
    {
        _port = port == 0 ? GetAvailablePort() : port;
        _serverUrl = $"http://localhost:{_port}";
    }

    public async Task<bool> StartAsync()
    {
        if (_disposed)
        {
            LastError = "Cannot start a disposed server host";
            Status = ServerStatus.Error;
            return false;
        }

        if (IsRunning)
        {
            LastError = "Server is already running";
            return false;
        }

        try
        {
            Status = ServerStatus.Starting;
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

            _httpHandler = new SocketsHttpHandler
            {
                PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
                KeepAlivePingDelay = TimeSpan.FromSeconds(60),
                KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
                EnableMultipleHttp2Connections = true
            };

            _channel = GrpcChannel.ForAddress(_serverUrl, new GrpcChannelOptions
            {
                HttpHandler = _httpHandler
            });

            _runTask = Task.Run(async () =>
            {
                try
                {
                    await _app.RunAsync(_serverUrl);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    LastError = $"Server runtime error: {ex.Message}";
                    Status = ServerStatus.Error;
                    Console.WriteLine($"Embedded server error: {ex.Message}");
                }
            }, _cts.Token);

            await WaitForServerReady();
            Status = ServerStatus.Running;
            LastError = null;
            
            _healthMonitor = new ServerHealthMonitor(_serverUrl, OnHealthStatusChanged);
            _healthMonitor.Start();
            
            return true;
        }
        catch (TimeoutException ex)
        {
            LastError = "Server failed to start within the expected time. The port may be in use or the server configuration is incorrect.";
            Status = ServerStatus.Error;
            Console.WriteLine($"Server initialization failed: {ex.Message}");
            await CleanupResources();
            return false;
        }
        catch (IOException ex)
        {
            LastError = $"Server initialization failed due to I/O error: {ex.Message}. The port {_port} may already be in use.";
            Status = ServerStatus.Error;
            Console.WriteLine($"Server initialization failed: {ex.Message}");
            await CleanupResources();
            return false;
        }
        catch (Exception ex)
        {
            LastError = $"Server initialization failed: {ex.Message}";
            Status = ServerStatus.Error;
            Console.WriteLine($"Server initialization failed: {ex.Message}");
            await CleanupResources();
            return false;
        }
    }
    
    private void OnHealthStatusChanged(bool isHealthy, string? errorMessage)
    {
        if (!isHealthy && Status == ServerStatus.Running)
        {
            Status = ServerStatus.Reconnecting;
            LastError = errorMessage ?? "Server health check failed";
            Console.WriteLine($"Server health degraded: {LastError}");
        }
        else if (isHealthy && Status == ServerStatus.Reconnecting)
        {
            Status = ServerStatus.Running;
            LastError = null;
            Console.WriteLine("Server health restored");
        }
    }

    public async Task StopAsync()
    {
        if (_disposed)
        {
            return;
        }

        Status = ServerStatus.Stopped;
        await CleanupResources();
    }

    private async Task CleanupResources()
    {
        lock (_lock)
        {
            if (_disposed)
            {
                return;
            }
        }

        try
        {
            if (_healthMonitor != null)
            {
                try
                {
                    _healthMonitor.Stop();
                    _healthMonitor = null;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error stopping health monitor: {ex.Message}");
                }
            }
            

            if (_channel != null)
            {
                try
                {
                    await _channel.ShutdownAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error shutting down gRPC channel: {ex.Message}");
                }

                try
                {
                    _channel.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error disposing gRPC channel: {ex.Message}");
                }

                _channel = null;
            }

            if (_httpHandler != null)
            {
                try
                {
                    _httpHandler.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error disposing HTTP handler: {ex.Message}");
                }

                _httpHandler = null;
            }

            _cts?.Cancel();

            if (_app != null)
            {
                try
                {
                    await _app.StopAsync(TimeSpan.FromSeconds(5));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error stopping embedded server: {ex.Message}");
                }

                try
                {
                    await _app.DisposeAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error disposing embedded server: {ex.Message}");
                }

                _app = null;
            }

            if (_runTask != null)
            {
                try
                {
                    await _runTask.WaitAsync(TimeSpan.FromSeconds(5));
                }
                catch (OperationCanceledException)
                {
                }
                catch (TimeoutException)
                {
                    Console.WriteLine("Server run task did not complete within timeout");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error waiting for server task: {ex.Message}");
                }

                _runTask = null;
            }

            if (_cts != null)
            {
                try
                {
                    _cts.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error disposing cancellation token source: {ex.Message}");
                }

                _cts = null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during resource cleanup: {ex.Message}");
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
                httpClient.Timeout = TimeSpan.FromMilliseconds(500);
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

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await CleanupResources();

        lock (_lock)
        {
            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            CleanupResources().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during synchronous disposal: {ex.Message}");
        }

        lock (_lock)
        {
            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }
}
