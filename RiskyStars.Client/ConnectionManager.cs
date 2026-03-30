using Grpc.Core;
using RiskyStars.Shared;

namespace RiskyStars.Client;

public enum ConnectionStatus
{
    Disconnected,
    Connecting,
    Connected,
    Error,
    Reconnecting
}

public class ConnectionManager
{
    private GrpcGameClient? _gameClient;
    private string? _currentPlayerId;
    private string? _currentPlayerName;
    private string? _currentSessionId;
    private string _serverAddress;

    private ConnectionStatus _status = ConnectionStatus.Disconnected;
    private string _errorMessage = "";
    private int _reconnectAttempts = 0;
    private const int MaxReconnectAttempts = 3;
    private const int ReconnectDelayMs = 2000;
    private DateTime _lastReconnectAttempt = DateTime.MinValue;
    private bool _autoReconnect = true;

    private Task? _connectionTask;
    private CancellationTokenSource? _cancellationTokenSource;

    public ConnectionStatus Status => _status;
    public string ErrorMessage => _errorMessage;
    public bool IsConnected => _status == ConnectionStatus.Connected && _gameClient?.IsConnected == true;
    public GrpcGameClient? GameClient => _gameClient;
    public string? CurrentPlayerId => _currentPlayerId;
    public int ReconnectAttempts => _reconnectAttempts;
    public int MaxAttempts => MaxReconnectAttempts;

    public ConnectionManager(string serverAddress)
    {
        _serverAddress = serverAddress;
    }

    public void UpdateServerAddress(string serverAddress)
    {
        _serverAddress = serverAddress;
    }

    public async Task<bool> ConnectAsync(string playerName, string sessionId)
    {
        if (_status == ConnectionStatus.Connecting || _status == ConnectionStatus.Reconnecting)
        {
            return false;
        }

        _currentPlayerName = playerName;
        _currentSessionId = sessionId;
        _reconnectAttempts = 0;

        return await AttemptConnectionAsync();
    }

    private async Task<bool> AttemptConnectionAsync()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();

        if (_reconnectAttempts == 0)
            _status = ConnectionStatus.Connecting;
        else
            _status = ConnectionStatus.Reconnecting;

        _errorMessage = "";

        try
        {
            _gameClient?.Dispose();
            _gameClient = new GrpcGameClient(_serverAddress);
            
            _currentPlayerId = Guid.NewGuid().ToString();
            
            await _gameClient.ConnectAsync(_currentPlayerId, _currentPlayerName!, _currentSessionId!);

            _status = ConnectionStatus.Connected;
            _reconnectAttempts = 0;
            _errorMessage = "";
            return true;
        }
        catch (RpcException ex)
        {
            _errorMessage = GetUserFriendlyErrorMessage(ex);
            _status = ConnectionStatus.Error;
            return false;
        }
        catch (Exception ex)
        {
            _errorMessage = $"Connection failed: {ex.Message}";
            _status = ConnectionStatus.Error;
            return false;
        }
    }

    public void Update()
    {
        if (_status == ConnectionStatus.Connected && _gameClient != null)
        {
            if (!_gameClient.IsConnected)
            {
                _status = ConnectionStatus.Error;
                _errorMessage = "Connection lost to server";

                if (_autoReconnect && _reconnectAttempts < MaxReconnectAttempts)
                {
                    TryReconnect();
                }
            }
        }

        if (_status == ConnectionStatus.Error && _autoReconnect && 
            _reconnectAttempts < MaxReconnectAttempts &&
            (DateTime.Now - _lastReconnectAttempt).TotalMilliseconds >= ReconnectDelayMs)
        {
            TryReconnect();
        }
    }

    private void TryReconnect()
    {
        if (_connectionTask == null || _connectionTask.IsCompleted)
        {
            _reconnectAttempts++;
            _lastReconnectAttempt = DateTime.Now;
            _connectionTask = Task.Run(async () => await AttemptConnectionAsync());
        }
    }

    public void CancelConnection()
    {
        _cancellationTokenSource?.Cancel();
        _status = ConnectionStatus.Disconnected;
    }

    public async Task DisconnectAsync()
    {
        _autoReconnect = false;
        
        if (_gameClient != null)
        {
            try
            {
                await _gameClient.DisconnectAsync("User disconnect");
            }
            catch
            {
            }

            _gameClient.Dispose();
            _gameClient = null;
        }

        _status = ConnectionStatus.Disconnected;
        _currentPlayerId = null;
        _currentPlayerName = null;
        _currentSessionId = null;
        _reconnectAttempts = 0;
        _errorMessage = "";
    }

    public void ResetReconnectAttempts()
    {
        _reconnectAttempts = 0;
        _autoReconnect = true;
    }

    private string GetUserFriendlyErrorMessage(RpcException ex)
    {
        return ex.StatusCode switch
        {
            StatusCode.Unavailable => "Server is unavailable. Please check the server address and try again.",
            StatusCode.DeadlineExceeded => "Connection timed out. The server may be slow or unreachable.",
            StatusCode.Cancelled => "Connection was cancelled.",
            StatusCode.Unauthenticated => "Authentication failed. Please check your credentials.",
            StatusCode.PermissionDenied => "Permission denied. You may not have access to this server.",
            StatusCode.NotFound => "Server endpoint not found. Please check the server address.",
            StatusCode.AlreadyExists => "A connection with this player already exists.",
            StatusCode.ResourceExhausted => "Server is at capacity. Please try again later.",
            StatusCode.FailedPrecondition => "Connection failed. The session may not be available.",
            StatusCode.Aborted => "Connection was aborted by the server.",
            StatusCode.OutOfRange => "Invalid connection parameters.",
            StatusCode.Unimplemented => "This feature is not supported by the server.",
            StatusCode.Internal => "Internal server error. Please try again later.",
            StatusCode.DataLoss => "Data loss detected. Connection may be unstable.",
            _ => $"Connection failed: {ex.Status.Detail}"
        };
    }

    public void Dispose()
    {
        _cancellationTokenSource?.Cancel();
        _gameClient?.Dispose();
    }
}
