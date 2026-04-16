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
            System.Console.WriteLine("[Property] ConnectionManager.Status get - " + _status);
            return _status; 
        } 
    }
    
    public string ErrorMessage 
    { 
        get 
        { 
            System.Console.WriteLine("[Property] ConnectionManager.ErrorMessage get - " + _errorMessage);
            return _errorMessage; 
        } 
    }
    
    public bool IsConnected 
    { 
        get 
        { 
            bool connected = _status == ConnectionStatus.Connected && _gameClient?.IsConnected == true;
            System.Console.WriteLine("[Property] ConnectionManager.IsConnected get - " + connected);
            return connected;
        } 
    }
    
    public GrpcGameClient? GameClient 
    { 
        get 
        { 
            System.Console.WriteLine("[Property] ConnectionManager.GameClient get");
            return _gameClient; 
        } 
    }
    
    public string? CurrentPlayerId 
    { 
        get 
        { 
            System.Console.WriteLine("[Property] ConnectionManager.CurrentPlayerId get - " + (_currentPlayerId ?? "null"));
            return _currentPlayerId; 
        } 
    }
    
    public int ReconnectAttempts 
    { 
        get 
        { 
            System.Console.WriteLine("[Property] ConnectionManager.ReconnectAttempts get - " + _reconnectAttempts);
            return _reconnectAttempts; 
        } 
    }
    
    public int MaxAttempts 
    { 
        get 
        { 
            System.Console.WriteLine("[Property] ConnectionManager.MaxAttempts get - " + MaxReconnectAttempts);
            return MaxReconnectAttempts; 
        } 
    }

    public ConnectionManager(string serverAddress)
    {
        System.Console.WriteLine($"[Entry] ConnectionManager(string serverAddress) - Address: {serverAddress}");
        try
        {
            _serverAddress = serverAddress;
            System.Console.WriteLine("[Exit] ConnectionManager(string serverAddress)");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[Error] ConnectionManager(string serverAddress): {ex.Message}");
            throw;
        }
    }

    public ConnectionManager(GrpcGameClient gameClient)
    {
        System.Console.WriteLine("[Entry] ConnectionManager(GrpcGameClient) - Using embedded server");
        try
        {
            _gameClient = gameClient;
            _serverAddress = "embedded";
            System.Console.WriteLine("[Exit] ConnectionManager(GrpcGameClient)");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[Error] ConnectionManager(GrpcGameClient): {ex.Message}");
            throw;
        }
    }

    public void UpdateServerAddress(string serverAddress)
    {
        System.Console.WriteLine($"[Entry] UpdateServerAddress({serverAddress})");
        try
        {
            _serverAddress = serverAddress;
            System.Console.WriteLine("[Exit] UpdateServerAddress");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[Error] UpdateServerAddress: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> ConnectAsync(string playerName, string sessionId)
    {
        System.Console.WriteLine($"[Entry] ConnectAsync - Server: {_serverAddress}, Player: {playerName}, Session: {sessionId}, Current Status: {_status}");
        if (_status == ConnectionStatus.Connecting || _status == ConnectionStatus.Reconnecting)
        {
            System.Console.WriteLine("[Exit] ConnectAsync - Already connecting/reconnecting");
            return false;
        }

        try
        {
            _currentPlayerName = playerName;
            _currentSessionId = sessionId;
            _reconnectAttempts = 0;

            var result = await AttemptConnectionAsync();
            System.Console.WriteLine($"[Exit] ConnectAsync - Result: {result}");
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
        System.Console.WriteLine($"[Entry] AttemptConnectionAsync - Status: {_status}, Server: {_serverAddress}");
        
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
                System.Console.WriteLine($"[Client] Creating gRPC channel to: {_serverAddress}");
                _gameClient?.Dispose();
                _gameClient = new GrpcGameClient(_serverAddress);
                System.Console.WriteLine("[Client] GrpcGameClient created successfully");
            }
            
            _currentPlayerId = Guid.NewGuid().ToString();
            
            if (_gameClient != null)
            {
                System.Console.WriteLine("[Client] Calling gameClient.ConnectAsync...");
                await _gameClient.ConnectAsync(_currentPlayerId, _currentPlayerName!, _currentSessionId!);
                System.Console.WriteLine("[Client] gameClient.ConnectAsync completed");
            }

            _status = ConnectionStatus.Connected;
            _reconnectAttempts = 0;
            _errorMessage = "";
            System.Console.WriteLine("[Exit] AttemptConnectionAsync - SUCCESS");
            return true;
        }
        catch (RpcException ex)
        {
            System.Console.WriteLine($"[Error] RpcException in AttemptConnectionAsync: {ex.StatusCode} - {ex.Message}");
            System.Console.WriteLine($"[Error] Detail: {ex.Status.Detail}");
            _errorMessage = GetUserFriendlyErrorMessage(ex);
            _status = ConnectionStatus.Error;
            return false;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[Error] General exception in AttemptConnectionAsync: {ex.Message}");
            System.Console.WriteLine($"[Error] Stack: {ex.StackTrace}");
            _errorMessage = $"Connection failed: {ex.Message}";
            _status = ConnectionStatus.Error;
            return false;
        }
    }

    public void Update()
    {
        System.Console.WriteLine($"[Entry] ConnectionManager.Update() - Current Status: {_status}");
        try
        {
            if (_status == ConnectionStatus.Connected && _gameClient != null)
            {
                if (!_gameClient.IsConnected)
                {
                    System.Console.WriteLine("[Client] Connection lost detected in Update()");
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
            System.Console.WriteLine("[Exit] ConnectionManager.Update()");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[Error] Update(): {ex.Message}");
        }
    }

    private void TryReconnect()
    {
        System.Console.WriteLine($"[Entry] TryReconnect() - Attempt {_reconnectAttempts + 1}/{MaxReconnectAttempts}");
        try
        {
            if (_connectionTask == null || _connectionTask.IsCompleted)
            {
                _reconnectAttempts++;
                _lastReconnectAttempt = DateTime.Now;
                _connectionTask = Task.Run(async () => await AttemptConnectionAsync());
                System.Console.WriteLine("[Exit] TryReconnect - Started reconnect task");
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[Error] TryReconnect: {ex.Message}");
        }
    }

    public void CancelConnection()
    {
        System.Console.WriteLine("[Entry] CancelConnection()");
        try
        {
            _cancellationTokenSource?.Cancel();
            _status = ConnectionStatus.Disconnected;
            System.Console.WriteLine("[Exit] CancelConnection");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[Error] CancelConnection: {ex.Message}");
        }
    }

    public async Task DisconnectAsync(string reason = "Client disconnect")
    {
        System.Console.WriteLine($"[Entry] DisconnectAsync - Reason: {reason}");
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
            System.Console.WriteLine("[Exit] DisconnectAsync");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[Error] DisconnectAsync: {ex.Message}");
            _status = ConnectionStatus.Error;
        }
    }

    public void ResetReconnectAttempts()
    {
        System.Console.WriteLine("[Entry] ResetReconnectAttempts");
        _reconnectAttempts = 0;
        _autoReconnect = true;
        System.Console.WriteLine("[Exit] ResetReconnectAttempts");
    }

    private string GetUserFriendlyErrorMessage(RpcException ex)
    {
        System.Console.WriteLine($"[Entry] GetUserFriendlyErrorMessage - StatusCode: {ex.StatusCode}");
        var message = ex.StatusCode switch
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
        System.Console.WriteLine($"[Exit] GetUserFriendlyErrorMessage - Message: {message}");
        return message;
    }

    public void Dispose()
    {
        System.Console.WriteLine("[Entry] ConnectionManager.Dispose()");
        try
        {
            _cancellationTokenSource?.Cancel();
            
            if (_gameClient != null && _serverAddress != "embedded")
            {
                _gameClient.Dispose();
            }
            System.Console.WriteLine("[Exit] ConnectionManager.Dispose()");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[Error] Dispose: {ex.Message}");
        }
    }
}
