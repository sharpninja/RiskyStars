using Grpc.Net.Client;
using RiskyStars.Shared;

namespace RiskyStars.Client;

public class LobbyClient : IDisposable
{
    private readonly GrpcChannel _channel;
    private readonly LobbyService.LobbyServiceClient _client;
    private string? _authToken;
    private string? _playerId;
    private bool _disposed;

    public string? AuthToken => _authToken;
    public string? PlayerId => _playerId;
    public bool IsAuthenticated => !string.IsNullOrEmpty(_authToken);

    public LobbyClient(string serverAddress)
    {
        _channel = GrpcChannel.ForAddress(serverAddress);
        _client = new LobbyService.LobbyServiceClient(_channel);
    }

    public async Task<AuthenticateResponse> AuthenticateAsync(string playerName, string password = "")
    {
        var request = new AuthenticateRequest
        {
            PlayerName = playerName,
            Password = password
        };

        var response = await _client.AuthenticateAsync(request, GetMetadata());
        
        if (response.Success)
        {
            _authToken = response.AuthToken;
            _playerId = ExtractPlayerIdFromToken(response.AuthToken);
        }

        return response;
    }

    public async Task<CreateLobbyResponse> CreateLobbyAsync(string playerName, LobbySettingsProto settings)
    {
        if (!IsAuthenticated)
            throw new InvalidOperationException("Not authenticated");

        var request = new CreateLobbyRequest
        {
            PlayerName = playerName,
            Settings = settings
        };

        return await _client.CreateLobbyAsync(request, GetMetadata());
    }

    public async Task<JoinLobbyResponse> JoinLobbyAsync(string lobbyId, string playerName)
    {
        if (!IsAuthenticated)
            throw new InvalidOperationException("Not authenticated");

        var request = new JoinLobbyRequest
        {
            LobbyId = lobbyId,
            PlayerName = playerName
        };

        return await _client.JoinLobbyAsync(request, GetMetadata());
    }

    public async Task<LeaveLobbyResponse> LeaveLobbyAsync(string lobbyId)
    {
        if (!IsAuthenticated)
            throw new InvalidOperationException("Not authenticated");

        var request = new LeaveLobbyRequest
        {
            LobbyId = lobbyId
        };

        return await _client.LeaveLobbyAsync(request, GetMetadata());
    }

    public async Task<SetReadyResponse> SetReadyAsync(string lobbyId, bool isReady)
    {
        if (!IsAuthenticated)
            throw new InvalidOperationException("Not authenticated");

        var request = new SetReadyRequest
        {
            LobbyId = lobbyId,
            IsReady = isReady
        };

        return await _client.SetReadyAsync(request, GetMetadata());
    }

    public async Task<StartGameResponse> StartGameAsync(string lobbyId)
    {
        if (!IsAuthenticated)
            throw new InvalidOperationException("Not authenticated");

        var request = new StartGameRequest
        {
            LobbyId = lobbyId
        };

        return await _client.StartGameAsync(request, GetMetadata());
    }

    public async Task<ListLobbiesResponse> ListLobbiesAsync()
    {
        var request = new ListLobbiesRequest();
        return await _client.ListLobbiesAsync(request);
    }

    public async Task<GetLobbyResponse> GetLobbyAsync(string lobbyId)
    {
        var request = new GetLobbyRequest
        {
            LobbyId = lobbyId
        };

        return await _client.GetLobbyAsync(request);
    }

    private Grpc.Core.Metadata GetMetadata()
    {
        var metadata = new Grpc.Core.Metadata();
        if (!string.IsNullOrEmpty(_authToken))
        {
            metadata.Add("Authorization", $"Bearer {_authToken}");
        }
        return metadata;
    }

    private string? ExtractPlayerIdFromToken(string token)
    {
        return token.Length > 8 ? token.Substring(0, 8) : token;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _channel.Dispose();
        GC.SuppressFinalize(this);
    }
}
