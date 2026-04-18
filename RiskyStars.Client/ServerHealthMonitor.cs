using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace RiskyStars.Client;

public class ServerHealthMonitor
{
    private readonly string _serverUrl;
    private readonly Action<bool, string?> _statusCallback;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _monitoringTask;
    private bool _isRunning;
    private int _consecutiveFailures;
    private int _reconnectAttempt;
    private DateTime _lastSuccessfulCheck;
    private bool _lastKnownHealthy = true;

    private const int HealthCheckIntervalMs = 5000;
    private const int InitialRetryDelayMs = 1000;
    private const int MaxRetryDelayMs = 30000;
    private const int MaxConsecutiveFailures = 3;
    private const double BackoffMultiplier = 2.0;

    public bool IsHealthy { get; private set; } = true;
    public int ConsecutiveFailures => _consecutiveFailures;
    public int ReconnectAttempt => _reconnectAttempt;
    public DateTime LastSuccessfulCheck => _lastSuccessfulCheck;
    public string? LastError { get; private set; }

    public ServerHealthMonitor(string serverUrl, Action<bool, string?> statusCallback)
    {
        _serverUrl = serverUrl;
        _statusCallback = statusCallback;
        _lastSuccessfulCheck = DateTime.UtcNow;
    }

    public void Start()
    {
        if (_isRunning)
        {
            return;
        }

        _isRunning = true;
        _cancellationTokenSource = new CancellationTokenSource();
        _monitoringTask = Task.Run(async () => await MonitorHealthAsync(_cancellationTokenSource.Token));
    }

    public void Stop()
    {
        if (!_isRunning)
        {
            return;
        }

        _isRunning = false;
        _cancellationTokenSource?.Cancel();

        if (_monitoringTask != null)
        {
            try
            {
                _monitoringTask.Wait(TimeSpan.FromSeconds(5));
            }
            catch (AggregateException)
            {
            }
        }

        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        _monitoringTask = null;
    }

    private async Task MonitorHealthAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                bool isHealthy = await PerformHealthCheckAsync();

                if (isHealthy)
                {
                    if (!_lastKnownHealthy)
                    {
                        Console.WriteLine($"Server health restored after {_consecutiveFailures} failures");
                    }

                    IsHealthy = true;
                    _consecutiveFailures = 0;
                    _reconnectAttempt = 0;
                    _lastSuccessfulCheck = DateTime.UtcNow;
                    LastError = null;

                    if (!_lastKnownHealthy)
                    {
                        _statusCallback(true, null);
                        _lastKnownHealthy = true;
                    }

                    await Task.Delay(HealthCheckIntervalMs, cancellationToken);
                }
                else
                {
                    _consecutiveFailures++;

                    if (_consecutiveFailures >= MaxConsecutiveFailures)
                    {
                        IsHealthy = false;

                        if (_lastKnownHealthy)
                        {
                            _statusCallback(false, LastError);
                            _lastKnownHealthy = false;
                        }

                        _reconnectAttempt++;
                        int retryDelay = CalculateExponentialBackoff(_reconnectAttempt);
                        
                        Console.WriteLine($"Server unhealthy (attempt {_reconnectAttempt}), retrying in {retryDelay}ms");
                        await Task.Delay(retryDelay, cancellationToken);
                    }
                    else
                    {
                        await Task.Delay(HealthCheckIntervalMs / 2, cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Health monitoring error: {ex.Message}");
                await Task.Delay(HealthCheckIntervalMs, cancellationToken);
            }
        }
    }

    private async Task<bool> PerformHealthCheckAsync()
    {
        var serverUri = new Uri(_serverUrl);

        try
        {
            using var tcpClient = new TcpClient();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            await tcpClient.ConnectAsync(serverUri.Host, serverUri.Port, cts.Token);

            if (tcpClient.Connected)
            {
                return true;
            }
        }
        catch (TaskCanceledException)
        {
            LastError = "Health check timed out";
            return false;
        }
        catch (Exception ex)
        {
            LastError = $"Health check error: {ex.Message}";
            return false;
        }

        LastError = "Server socket did not report a connected state";
        return false;
    }

    private int CalculateExponentialBackoff(int attempt)
    {
        int delay = (int)(InitialRetryDelayMs * Math.Pow(BackoffMultiplier, attempt - 1));
        return Math.Min(delay, MaxRetryDelayMs);
    }

    public void ResetReconnectAttempts()
    {
        _reconnectAttempt = 0;
        _consecutiveFailures = 0;
    }
}
