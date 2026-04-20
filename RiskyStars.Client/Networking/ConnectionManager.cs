using Grpc.Core;
using RiskyStars.Shared;
using System;
using System.Threading;
using System.Threading.Tasks;

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

    public ConnectionStatus Status 
    { 
        get 
        { 
            return _status; 
        } 
    }
    
    public string ErrorMessage 
    { 
        get 
        { 
            return _errorMessage; 
        } 
    }
    
    public bool IsConnected 
    { 
        get 
        { 
            return _status == ConnectionStatus.Connected && _gameClient?.IsConnected == true;
        } 
    }
    
    public GrpcGameClient? GameClient 
    { 
        get 
        { 
            return _gameClient; 
        } 
    }

    public GrpcGameClient EnsureGameClient()
    {
        if (_gameClient == null)
        {
            if (_serverAddress == "embedded")
            {
                throw new InvalidOperationException("Embedded connection requires a preconfigured game client.");
            }

            _gameClient = new GrpcGameClient(_serverAddress);
        }

        return _gameClient;
    }
    
    public string? CurrentPlayerId 
    { 
        get 
        { 
            return _currentPlayerId; 
        } 
    }
    
    public int ReconnectAttempts 
    { 
        get 
        { 
            return _reconnectAttempts; 
        } 
    }
    
    public int MaxAttempts 
    { 
        get 
        { 
            return MaxReconnectAttempts; 
        } 
    }

    public ConnectionManager(string serverAddress)
    {
        try
        {
            _serverAddress = serverAddress;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[Error] ConnectionManager(string serverAddress): {ex.Message}");
            throw;
        }
    }

    public ConnectionManager(GrpcGameClient gameClient)
    {
        try
        {
            _gameClient = gameClient;
            _serverAddress = "embedded";
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[Error] ConnectionManager(GrpcGameClient): {ex.Message}");
            throw;
        }
    }

    public void UpdateServerAddress(string serverAddress)
    {
        try
        {
            _serverAddress = serverAddress;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[Error] UpdateServerAddress: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> ConnectAsync(string playerId, string playerName, string sessionId)
    {
        if (_status == ConnectionStatus.Connecting || _status == ConnectionStatus.Reconnecting)
        {
            return false;
        }

        try
        {
            _currentPlayerId = playerId;
            _currentPlayerName = playerName;
            _currentSessionId = sessionId;
            _reconnectAttempts = 0;

            var result = await AttemptConnectionAsync();
            return result;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[Error] ConnectAsync: {ex.Message}");
            _status = ConnectionStatus.Error;
            _errorMessage = ex.Message;
            return false;
        }
    }

    private async Task<bool> AttemptConnectionAsync()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();

        if (_reconnectAttempts == 0)
        {
            _status = ConnectionStatus.Connecting;
        }
        else
        {
            _status = ConnectionStatus.Reconnecting;
        }

        _errorMessage = "";

        try
        {
            if (_serverAddress != "embedded")
            {
                _gameClient?.Dispose();
                _gameClient = new GrpcGameClient(_serverAddress);
            }
            
            if (string.IsNullOrWhiteSpace(_currentPlayerId))
            {
                throw new InvalidOperationException("Player ID is required before connecting");
            }
            
            if (_gameClient != null)
            {
                await _gameClient.ConnectAsync(_currentPlayerId, _currentPlayerName!, _currentSessionId!);
            }

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
        try
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
        catch (Exception ex)
        {
            System.Console.WriteLine($"[Error] Update(): {ex.Message}");
        }
    }

    private void TryReconnect()
    {
        try
        {
            if (_connectionTask == null || _connectionTask.IsCompleted)
            {
                _reconnectAttempts++;
                _lastReconnectAttempt = DateTime.Now;
                _connectionTask = Task.Run(async () => await AttemptConnectionAsync());
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[Error] TryReconnect: {ex.Message}");
        }
    }

    public void CancelConnection()
    {
        try
        {
            _cancellationTokenSource?.Cancel();
            _status = ConnectionStatus.Disconnected;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[Error] CancelConnection: {ex.Message}");
        }
    }

    public async Task DisconnectAsync(string reason = "Client disconnect")
    {
        _autoReconnect = false;
        
        try
        {
            if (_gameClient != null)
            {
                try
                {
                    await _gameClient.DisconnectAsync(reason);
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"[Warning] Error during client disconnect: {ex.Message}");
                }

                if (_serverAddress != "embedded")
                {
                    _gameClient.Dispose();
                }
                _gameClient = null;
            }

            _status = ConnectionStatus.Disconnected;
            _currentPlayerId = null;
            _currentPlayerName = null;
            _currentSessionId = null;
            _reconnectAttempts = 0;
            _errorMessage = "";
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[Error] DisconnectAsync: {ex.Message}");
            _status = ConnectionStatus.Error;
        }
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
        try
        {
            _cancellationTokenSource?.Cancel();
            
            if (_gameClient != null && _serverAddress != "embedded")
            {
                _gameClient.Dispose();
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[Error] Dispose: {ex.Message}");
        }
    }
}
