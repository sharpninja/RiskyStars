using Grpc.Net.Client;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

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
    private Process? _serverProcess;
    private GrpcChannel? _channel;
    private SocketsHttpHandler? _httpHandler;
    private readonly string _serverUrl;
    private readonly int _port;
    private bool _disposed;
    private readonly object _lock = new object();
    private ServerHealthMonitor? _healthMonitor;

    public GrpcChannel? Channel => _channel;
    public bool IsRunning => _serverProcess != null && !_serverProcess.HasExited;
    public string? LastError { get; private set; }
    public ServerStatus Status { get; private set; } = ServerStatus.Stopped;
    public ServerHealthMonitor? HealthMonitor => _healthMonitor;
    public string ServerUrl => _serverUrl;

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

            var serverPath = ResolveServerPath();

            if (serverPath == null)
            {
                LastError = $"Server executable not found. Looked beside the client and under RiskyStars.Server/bin from: {AppContext.BaseDirectory}";
                Status = ServerStatus.Error;
                return false;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"\"{serverPath}\" --urls {_serverUrl}",
                WorkingDirectory = Path.GetDirectoryName(serverPath) ?? AppContext.BaseDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            _serverProcess = new Process { StartInfo = startInfo };
            
            _serverProcess.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Console.WriteLine($"[Server] {e.Data}");
                }
            };

            _serverProcess.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Console.WriteLine($"[Server Error] {e.Data}");
                }
            };

            _serverProcess.Start();
            _serverProcess.BeginOutputReadLine();
            _serverProcess.BeginErrorReadLine();

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
            LastError = $"Server initialization failed due to I/O error: {ex.Message}. The port {_port} may already be used.";
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

            if (_serverProcess != null && !_serverProcess.HasExited)
            {
                try
                {
                    _serverProcess.Kill();
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    await _serverProcess.WaitForExitAsync(cts.Token);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error stopping server process: {ex.Message}");
                }

                try
                {
                    _serverProcess.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error disposing server process: {ex.Message}");
                }

                _serverProcess = null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during resource cleanup: {ex.Message}");
        }
    }

    private async Task WaitForServerReady()
    {
        var maxAttempts = 50;
        var delayMs = 100;
        var serverUri = new Uri(_serverUrl);

        for (int i = 0; i < maxAttempts; i++)
        {
            try
            {
                using var tcpClient = new TcpClient();
                using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
                await tcpClient.ConnectAsync(serverUri.Host, serverUri.Port, cts.Token);
                if (tcpClient.Connected)
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

    private static string? ResolveServerPath()
    {
        foreach (var searchRoot in EnumerateSearchRoots())
        {
            var directPath = Path.Combine(searchRoot, "RiskyStars.Server.dll");
            if (File.Exists(directPath))
            {
                return directPath;
            }

            var serverProjectDirectory = Path.Combine(searchRoot, "RiskyStars.Server");
            if (!Directory.Exists(serverProjectDirectory))
            {
                continue;
            }

            var serverBinDirectory = Path.Combine(serverProjectDirectory, "bin");
            if (!Directory.Exists(serverBinDirectory))
            {
                continue;
            }

            var builtServerPath = Directory
                .EnumerateFiles(serverBinDirectory, "RiskyStars.Server.dll", SearchOption.AllDirectories)
                .Where(path =>
                    !path.Contains($"{Path.DirectorySeparatorChar}ref{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) &&
                    !path.Contains($"{Path.DirectorySeparatorChar}refint{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();

            if (builtServerPath != null)
            {
                return builtServerPath;
            }
        }

        return null;
    }

    private static IEnumerable<string> EnumerateSearchRoots()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var basePath in new[] { AppContext.BaseDirectory, Environment.CurrentDirectory })
        {
            if (string.IsNullOrWhiteSpace(basePath) || !Directory.Exists(basePath))
            {
                continue;
            }

            for (var current = new DirectoryInfo(basePath); current != null; current = current.Parent)
            {
                if (seen.Add(current.FullName))
                {
                    yield return current.FullName;
                }
            }
        }
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
