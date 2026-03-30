using Grpc.Core;
using RiskyStars.Server.Entities;
using RiskyStars.Shared;

namespace RiskyStars.Server.Services;

public class LobbyServiceImpl : LobbyService.LobbyServiceBase
{
    private readonly GameSessionManager _sessionManager;

    public LobbyServiceImpl(GameSessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }

    public override Task<AuthenticateResponse> Authenticate(AuthenticateRequest request, ServerCallContext context)
    {
        try
        {
            var authToken = _sessionManager.AuthenticatePlayer(request.PlayerName, request.Password);

            return Task.FromResult(new AuthenticateResponse
            {
                Success = true,
                AuthToken = authToken,
                Message = "Authentication successful"
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new AuthenticateResponse
            {
                Success = false,
                Message = $"Authentication failed: {ex.Message}"
            });
        }
    }

    public override Task<CreateLobbyResponse> CreateLobby(CreateLobbyRequest request, ServerCallContext context)
    {
        try
        {
            context.ThrowIfNotAuthenticated(_sessionManager, out var playerId);

            var settings = new LobbySettings
            {
                MinPlayers = request.Settings.MinPlayers,
                MaxPlayers = request.Settings.MaxPlayers,
                GameMode = request.Settings.GameMode,
                MapName = request.Settings.MapName,
                StartingPopulation = request.Settings.StartingPopulation,
                StartingMetal = request.Settings.StartingMetal,
                StartingFuel = request.Settings.StartingFuel,
                AllowSpectators = request.Settings.AllowSpectators,
                TurnTimeLimit = request.Settings.TurnTimeLimit
            };

            var lobbyId = _sessionManager.CreateLobby(playerId, request.PlayerName, settings);

            return Task.FromResult(new CreateLobbyResponse
            {
                Success = true,
                LobbyId = lobbyId,
                Message = "Lobby created successfully"
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new CreateLobbyResponse
            {
                Success = false,
                Message = $"Failed to create lobby: {ex.Message}"
            });
        }
    }

    public override Task<JoinLobbyResponse> JoinLobby(JoinLobbyRequest request, ServerCallContext context)
    {
        try
        {
            context.ThrowIfNotAuthenticated(_sessionManager, out var playerId);

            var success = _sessionManager.JoinLobby(request.LobbyId, playerId, request.PlayerName);

            return Task.FromResult(new JoinLobbyResponse
            {
                Success = success,
                Message = success ? "Joined lobby successfully" : "Failed to join lobby"
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new JoinLobbyResponse
            {
                Success = false,
                Message = $"Failed to join lobby: {ex.Message}"
            });
        }
    }

    public override Task<LeaveLobbyResponse> LeaveLobby(LeaveLobbyRequest request, ServerCallContext context)
    {
        try
        {
            context.ThrowIfNotAuthenticated(_sessionManager, out var playerId);

            var success = _sessionManager.LeaveLobby(request.LobbyId, playerId);

            return Task.FromResult(new LeaveLobbyResponse
            {
                Success = success,
                Message = success ? "Left lobby successfully" : "Failed to leave lobby"
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new LeaveLobbyResponse
            {
                Success = false,
                Message = $"Failed to leave lobby: {ex.Message}"
            });
        }
    }

    public override Task<SetReadyResponse> SetReady(SetReadyRequest request, ServerCallContext context)
    {
        try
        {
            context.ThrowIfNotAuthenticated(_sessionManager, out var playerId);

            var success = _sessionManager.SetPlayerReady(request.LobbyId, playerId, request.IsReady);

            return Task.FromResult(new SetReadyResponse
            {
                Success = success,
                Message = success ? "Ready status updated" : "Failed to update ready status"
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new SetReadyResponse
            {
                Success = false,
                Message = $"Failed to set ready: {ex.Message}"
            });
        }
    }

    public override Task<StartGameResponse> StartGame(StartGameRequest request, ServerCallContext context)
    {
        try
        {
            context.ThrowIfNotAuthenticated(_sessionManager, out var playerId);

            var lobby = _sessionManager.GetLobby(request.LobbyId);
            if (lobby == null)
            {
                return Task.FromResult(new StartGameResponse
                {
                    Success = false,
                    Message = "Lobby not found"
                });
            }

            if (lobby.HostPlayerId != playerId)
            {
                return Task.FromResult(new StartGameResponse
                {
                    Success = false,
                    Message = "Only the host can start the game"
                });
            }

            var sessionId = _sessionManager.StartGame(request.LobbyId);

            if (sessionId == null)
            {
                return Task.FromResult(new StartGameResponse
                {
                    Success = false,
                    Message = "Failed to start game. Ensure all players are ready."
                });
            }

            return Task.FromResult(new StartGameResponse
            {
                Success = true,
                SessionId = sessionId,
                Message = "Game started successfully"
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new StartGameResponse
            {
                Success = false,
                Message = $"Failed to start game: {ex.Message}"
            });
        }
    }

    public override Task<ListLobbiesResponse> ListLobbies(ListLobbiesRequest request, ServerCallContext context)
    {
        try
        {
            var lobbies = _sessionManager.GetAvailableLobbies();

            var response = new ListLobbiesResponse
            {
                Success = true
            };

            foreach (var lobby in lobbies)
            {
                var lobbyInfo = new LobbyInfo
                {
                    LobbyId = lobby.LobbyId,
                    HostPlayerName = lobby.Players.FirstOrDefault(p => p.PlayerId == lobby.HostPlayerId)?.PlayerName ?? "Unknown",
                    CurrentPlayers = lobby.Players.Count,
                    MaxPlayers = lobby.Settings.MaxPlayers,
                    GameMode = lobby.Settings.GameMode,
                    MapName = lobby.Settings.MapName
                };

                foreach (var player in lobby.Players)
                {
                    var playerName = player.PlayerName;
                    if (player.IsAI)
                    {
                        playerName += $" [AI - {player.AIDifficulty}]";
                    }
                    lobbyInfo.PlayerNames.Add(playerName);
                }

                response.Lobbies.Add(lobbyInfo);
            }

            return Task.FromResult(response);
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ListLobbiesResponse
            {
                Success = false,
                Message = $"Failed to list lobbies: {ex.Message}"
            });
        }
    }

    public override Task<GetLobbyResponse> GetLobby(GetLobbyRequest request, ServerCallContext context)
    {
        try
        {
            var lobby = _sessionManager.GetLobby(request.LobbyId);

            if (lobby == null)
            {
                return Task.FromResult(new GetLobbyResponse
                {
                    Success = false,
                    Message = "Lobby not found"
                });
            }

            var lobbyInfo = new LobbyInfo
            {
                LobbyId = lobby.LobbyId,
                HostPlayerName = lobby.Players.FirstOrDefault(p => p.PlayerId == lobby.HostPlayerId)?.PlayerName ?? "Unknown",
                CurrentPlayers = lobby.Players.Count,
                MaxPlayers = lobby.Settings.MaxPlayers,
                GameMode = lobby.Settings.GameMode,
                MapName = lobby.Settings.MapName
            };

            foreach (var player in lobby.Players)
            {
                var playerName = player.PlayerName;
                if (player.IsAI)
                {
                    playerName += $" [AI - {player.AIDifficulty}]";
                }
                lobbyInfo.PlayerNames.Add(playerName);
            }

            return Task.FromResult(new GetLobbyResponse
            {
                Success = true,
                Lobby = lobbyInfo
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new GetLobbyResponse
            {
                Success = false,
                Message = $"Failed to get lobby: {ex.Message}"
            });
        }
    }
}
