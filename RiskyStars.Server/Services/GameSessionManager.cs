using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;
using RiskyStars.Server.Entities;
using RiskyStars.Shared;

namespace RiskyStars.Server.Services;

// GameSessionManager: Manages game lobbies, sessions, and player connections
// 
// AI Player Support:
// - Supports adding/removing AI players during lobby setup
// - Allows human-vs-AI and AI-vs-AI matchups
// - Tracks AI player lifecycle during game sessions
// - Automatically triggers AIPlayerController execution during AI player turns
// - Prevents AI players from connecting to game streams (they don't need network connections)
//
// Key Methods for AI Integration:
// - AddAIPlayerToLobby: Adds an AI player to a lobby with specified difficulty
// - RemoveAIPlayerFromLobby: Removes an AI player from a lobby
// - GetAIPlayer: Retrieves an AI player by ID
// - IsAIPlayer: Checks if a player ID belongs to an AI player
// - ProcessAITurnIfNeededAsync: Automatically triggers AI turn execution if current player is AI
// - TriggerAIPlayerTurnAsync: Manually triggers AI turn execution for a specific AI player

public class GameSessionManager
{
    private readonly ConcurrentDictionary<string, GameSession> _sessions = new();
    private readonly ConcurrentDictionary<string, GameLobby> _lobbies = new();
    private readonly ConcurrentDictionary<string, PlayerConnection> _playerConnections = new();
    private readonly ConcurrentDictionary<string, string> _authTokens = new();
    private readonly ConcurrentDictionary<string, AIPlayer> _aiPlayers = new();
    private readonly GameStateManager _gameStateManager;
    private readonly AIPlayerController? _aiPlayerController;
    private readonly object _lobbyLock = new();

    public GameSessionManager(GameStateManager gameStateManager, AIPlayerController? aiPlayerController = null)
    {
        _gameStateManager = gameStateManager;
        _aiPlayerController = aiPlayerController;
    }

    public string AuthenticatePlayer(string playerName, string? password = null)
    {
        var playerId = GeneratePlayerId(playerName);
        var authToken = GenerateAuthToken(playerId, playerName);
        
        _authTokens[authToken] = playerId;
        
        return authToken;
    }

    public bool ValidateAuthToken(string authToken, out string playerId)
    {
        return _authTokens.TryGetValue(authToken, out playerId!);
    }

    public void RevokeAuthToken(string authToken)
    {
        _authTokens.TryRemove(authToken, out _);
    }

    public string CreateLobby(string hostPlayerId, string hostPlayerName, LobbySettings settings)
    {
        var lobbyId = Guid.NewGuid().ToString();
        
        var lobby = new GameLobby
        {
            LobbyId = lobbyId,
            HostPlayerId = hostPlayerId,
            Settings = settings,
            State = LobbyState.Waiting,
            CreatedAt = DateTime.UtcNow
        };

        lobby.Players.Add(new LobbyPlayer
        {
            PlayerId = hostPlayerId,
            PlayerName = hostPlayerName,
            IsReady = true,
            IsAI = false,
            JoinedAt = DateTime.UtcNow
        });

        _lobbies[lobbyId] = lobby;
        
        return lobbyId;
    }

    public bool JoinLobby(string lobbyId, string playerId, string playerName)
    {
        if (!_lobbies.TryGetValue(lobbyId, out var lobby))
        {
            return false;
        }

        lock (_lobbyLock)
        {
            if (lobby.State != LobbyState.Waiting)
            {
                return false;
            }

            if (lobby.Players.Count >= lobby.Settings.MaxPlayers)
            {
                return false;
            }

            if (lobby.Players.Any(p => p.PlayerId == playerId))
            {
                return false;
            }

            lobby.Players.Add(new LobbyPlayer
            {
                PlayerId = playerId,
                PlayerName = playerName,
                IsReady = false,
                IsAI = false,
                JoinedAt = DateTime.UtcNow
            });
        }

        return true;
    }

    public bool AddAIPlayerToLobby(string lobbyId, string aiName, DifficultyLevel difficulty)
    {
        if (!_lobbies.TryGetValue(lobbyId, out var lobby))
        {
            return false;
        }

        lock (_lobbyLock)
        {
            if (lobby.State != LobbyState.Waiting)
            {
                return false;
            }

            if (lobby.Players.Count >= lobby.Settings.MaxPlayers)
            {
                return false;
            }

            var aiPlayerId = GenerateAIPlayerId(aiName, lobby.Players.Count);
            
            if (lobby.Players.Any(p => p.PlayerId == aiPlayerId))
            {
                return false;
            }

            lobby.Players.Add(new LobbyPlayer
            {
                PlayerId = aiPlayerId,
                PlayerName = aiName,
                IsReady = true,
                IsAI = true,
                AIDifficulty = difficulty,
                JoinedAt = DateTime.UtcNow
            });
        }

        return true;
    }

    public bool RemoveAIPlayerFromLobby(string lobbyId, string aiPlayerId)
    {
        if (!_lobbies.TryGetValue(lobbyId, out var lobby))
        {
            return false;
        }

        lock (_lobbyLock)
        {
            var player = lobby.Players.FirstOrDefault(p => p.PlayerId == aiPlayerId && p.IsAI);
            if (player == null)
            {
                return false;
            }

            lobby.Players.Remove(player);
        }

        return true;
    }

    public bool LeaveLobby(string lobbyId, string playerId)
    {
        if (!_lobbies.TryGetValue(lobbyId, out var lobby))
        {
            return false;
        }

        lock (_lobbyLock)
        {
            var player = lobby.Players.FirstOrDefault(p => p.PlayerId == playerId);
            if (player == null)
            {
                return false;
            }

            if (player.IsAI)
            {
                return false;
            }

            lobby.Players.Remove(player);

            if (lobby.Players.Count == 0)
            {
                _lobbies.TryRemove(lobbyId, out _);
            }
            else if (lobby.HostPlayerId == playerId)
            {
                var nextHost = lobby.Players.FirstOrDefault(p => !p.IsAI);
                if (nextHost != null)
                {
                    lobby.HostPlayerId = nextHost.PlayerId;
                }
                else
                {
                    lobby.HostPlayerId = lobby.Players.First().PlayerId;
                }
            }
        }

        return true;
    }

    public bool SetPlayerReady(string lobbyId, string playerId, bool isReady)
    {
        if (!_lobbies.TryGetValue(lobbyId, out var lobby))
        {
            return false;
        }

        lock (_lobbyLock)
        {
            var player = lobby.Players.FirstOrDefault(p => p.PlayerId == playerId);
            if (player == null)
            {
                return false;
            }

            if (player.IsAI)
            {
                return false;
            }

            player.IsReady = isReady;
        }

        return true;
    }

    public GameLobby? GetLobby(string lobbyId)
    {
        _lobbies.TryGetValue(lobbyId, out var lobby);
        return lobby;
    }

    public List<GameLobby> GetAvailableLobbies()
    {
        return _lobbies.Values
            .Where(l => l.State == LobbyState.Waiting && l.Players.Count < l.Settings.MaxPlayers)
            .OrderByDescending(l => l.CreatedAt)
            .ToList();
    }

    public string? StartGame(string lobbyId)
    {
        if (!_lobbies.TryGetValue(lobbyId, out var lobby))
        {
            return null;
        }

        lock (_lobbyLock)
        {
            if (lobby.State != LobbyState.Waiting)
            {
                return null;
            }

            if (!lobby.Players.All(p => p.IsReady))
            {
                return null;
            }

            if (lobby.Players.Count < lobby.Settings.MinPlayers)
            {
                return null;
            }

            lobby.State = LobbyState.Starting;

            var game = CreateGameFromLobby(lobby);
            var gameId = game.Id;

            var session = new GameSession
            {
                SessionId = Guid.NewGuid().ToString(),
                GameId = gameId,
                LobbyId = lobbyId,
                State = SessionState.Active,
                StartedAt = DateTime.UtcNow
            };

            foreach (var lobbyPlayer in lobby.Players)
            {
                session.PlayerIds.Add(lobbyPlayer.PlayerId);
                
                if (lobbyPlayer.IsAI)
                {
                    session.AIPlayerIds.Add(lobbyPlayer.PlayerId);
                }
            }

            _sessions[session.SessionId] = session;
            _gameStateManager.CreateGame(game);

            lobby.State = LobbyState.InGame;
            lobby.GameId = gameId;
            lobby.SessionId = session.SessionId;

            _ = Task.Run(async () =>
            {
                await Task.Delay(1000);
                await ProcessAITurnIfNeededAsync(session.SessionId);
            });

            return session.SessionId;
        }
    }

    public GameSession? GetSession(string sessionId)
    {
        _sessions.TryGetValue(sessionId, out var session);
        return session;
    }

    public GameSession? GetSessionByGameId(string gameId)
    {
        return _sessions.Values.FirstOrDefault(s => s.GameId == gameId);
    }

    public bool EndSession(string sessionId, SessionEndReason reason)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
        {
            return false;
        }

        session.State = SessionState.Ended;
        session.EndedAt = DateTime.UtcNow;
        session.EndReason = reason;

        foreach (var playerId in session.PlayerIds)
        {
            if (!IsAIPlayer(playerId))
            {
                DisconnectPlayer(playerId);
            }
        }

        foreach (var aiPlayerId in session.AIPlayerIds)
        {
            _aiPlayers.TryRemove(aiPlayerId, out _);
        }

        if (!string.IsNullOrEmpty(session.LobbyId))
        {
            if (_lobbies.TryGetValue(session.LobbyId, out var lobby))
            {
                lobby.State = LobbyState.Ended;
            }
        }

        return true;
    }

    public PlayerConnection ConnectPlayer(string playerId, string sessionId, Channel<GameStateUpdate> updateChannel)
    {
        var connection = new PlayerConnection
        {
            PlayerId = playerId,
            SessionId = sessionId,
            UpdateChannel = updateChannel,
            ConnectedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
            IsActive = true
        };

        _playerConnections[playerId] = connection;

        if (_sessions.TryGetValue(sessionId, out var session))
        {
            session.ActivePlayerConnections.Add(playerId);
        }

        return connection;
    }

    public bool DisconnectPlayer(string playerId)
    {
        if (!_playerConnections.TryRemove(playerId, out var connection))
        {
            return false;
        }

        connection.IsActive = false;
        connection.DisconnectedAt = DateTime.UtcNow;
        connection.UpdateChannel?.Writer.TryComplete();

        if (_sessions.TryGetValue(connection.SessionId, out var session))
        {
            session.ActivePlayerConnections.Remove(playerId);
        }

        return true;
    }

    public PlayerConnection? GetPlayerConnection(string playerId)
    {
        _playerConnections.TryGetValue(playerId, out var connection);
        return connection;
    }

    public void UpdatePlayerActivity(string playerId)
    {
        if (_playerConnections.TryGetValue(playerId, out var connection))
        {
            connection.LastActivityAt = DateTime.UtcNow;
        }
    }

    public List<string> GetActivePlayers(string sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
        {
            return new List<string>();
        }

        return session.ActivePlayerConnections.ToList();
    }

    public bool IsPlayerConnected(string playerId)
    {
        return _playerConnections.TryGetValue(playerId, out var connection) && connection.IsActive;
    }

    public void BroadcastToSession(string sessionId, GameStateUpdate update)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
        {
            return;
        }

        foreach (var playerId in session.ActivePlayerConnections.ToList())
        {
            if (_playerConnections.TryGetValue(playerId, out var connection))
            {
                connection.UpdateChannel?.Writer.TryWrite(update);
            }
        }
    }

    public void SendToPlayer(string playerId, GameStateUpdate update)
    {
        if (_playerConnections.TryGetValue(playerId, out var connection))
        {
            connection.UpdateChannel?.Writer.TryWrite(update);
        }
    }

    public void CleanupInactiveSessions(TimeSpan inactivityThreshold)
    {
        var now = DateTime.UtcNow;
        
        var inactiveSessions = _sessions.Values
            .Where(s => s.State == SessionState.Active && 
                       (now - s.LastActivityAt) > inactivityThreshold)
            .ToList();

        foreach (var session in inactiveSessions)
        {
            EndSession(session.SessionId, SessionEndReason.Inactivity);
        }

        var oldLobbies = _lobbies.Values
            .Where(l => l.State == LobbyState.Waiting && 
                       l.Players.Count == 0 && 
                       (now - l.CreatedAt) > inactivityThreshold)
            .ToList();

        foreach (var lobby in oldLobbies)
        {
            _lobbies.TryRemove(lobby.LobbyId, out _);
        }
    }

    private Game CreateGameFromLobby(GameLobby lobby)
    {
        var game = new Game
        {
            Id = Guid.NewGuid().ToString(),
            TurnNumber = 1,
            CurrentPhase = Entities.TurnPhase.Production,
            CurrentPlayerIndex = 0
        };

        foreach (var lobbyPlayer in lobby.Players)
        {
            Player player;
            
            if (lobbyPlayer.IsAI)
            {
                var aiPlayer = new AIPlayer(lobbyPlayer.AIDifficulty ?? DifficultyLevel.Medium)
                {
                    Id = lobbyPlayer.PlayerId,
                    Name = lobbyPlayer.PlayerName,
                    PopulationStockpile = lobby.Settings.StartingPopulation,
                    MetalStockpile = lobby.Settings.StartingMetal,
                    FuelStockpile = lobby.Settings.StartingFuel
                };
                
                _aiPlayers[aiPlayer.Id] = aiPlayer;
                player = aiPlayer;
            }
            else
            {
                player = new Player
                {
                    Id = lobbyPlayer.PlayerId,
                    Name = lobbyPlayer.PlayerName,
                    PopulationStockpile = lobby.Settings.StartingPopulation,
                    MetalStockpile = lobby.Settings.StartingMetal,
                    FuelStockpile = lobby.Settings.StartingFuel
                };
            }
            
            game.Players.Add(player);
        }

        return game;
    }

    private string GeneratePlayerId(string playerName)
    {
        var sanitized = new string(playerName.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
        var hash = BitConverter.ToString(SHA256.HashData(Encoding.UTF8.GetBytes(playerName + DateTime.UtcNow.Ticks)))
            .Replace("-", "").Substring(0, 8).ToLower();
        return $"{sanitized}_{hash}";
    }

    private string GenerateAuthToken(string playerId, string playerName)
    {
        var tokenData = $"{playerId}:{playerName}:{DateTime.UtcNow.Ticks}:{Guid.NewGuid()}";
        var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(tokenData)));
        return hash.Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    private string GenerateAIPlayerId(string aiName, int playerCount)
    {
        var sanitized = new string(aiName.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
        return $"ai_{sanitized}_{playerCount}_{Guid.NewGuid().ToString().Substring(0, 8)}";
    }

    public AIPlayer? GetAIPlayer(string playerId)
    {
        _aiPlayers.TryGetValue(playerId, out var aiPlayer);
        return aiPlayer;
    }

    public List<AIPlayer> GetSessionAIPlayers(string sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
        {
            return new List<AIPlayer>();
        }

        return session.PlayerIds
            .Where(id => _aiPlayers.ContainsKey(id))
            .Select(id => _aiPlayers[id])
            .ToList();
    }

    public async Task TriggerAIPlayerTurnAsync(string gameId, string aiPlayerId)
    {
        if (_aiPlayerController == null)
        {
            return;
        }

        var aiPlayer = GetAIPlayer(aiPlayerId);
        if (aiPlayer == null)
        {
            return;
        }

        await _aiPlayerController.ExecuteAITurnAsync(gameId, aiPlayer);
    }

    public async Task ProcessAITurnIfNeededAsync(string sessionId)
    {
        if (_aiPlayerController == null)
        {
            return;
        }

        var session = GetSession(sessionId);
        if (session == null)
        {
            return;
        }

        var game = _gameStateManager.GetGame(session.GameId);
        if (game == null)
        {
            return;
        }

        var currentPlayer = game.CurrentPlayer;
        if (currentPlayer is AIPlayer aiPlayer)
        {
            await _aiPlayerController.ExecuteAITurnAsync(game.Id, aiPlayer);
        }
    }

    public bool IsAIPlayer(string playerId)
    {
        return _aiPlayers.ContainsKey(playerId);
    }

    public (int humanCount, int aiCount) GetLobbyPlayerCounts(string lobbyId)
    {
        if (!_lobbies.TryGetValue(lobbyId, out var lobby))
        {
            return (0, 0);
        }

        var humanCount = lobby.Players.Count(p => !p.IsAI);
        var aiCount = lobby.Players.Count(p => p.IsAI);

        return (humanCount, aiCount);
    }

    public (int humanCount, int aiCount) GetSessionPlayerCounts(string sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
        {
            return (0, 0);
        }

        var humanCount = session.PlayerIds.Count - session.AIPlayerIds.Count;
        var aiCount = session.AIPlayerIds.Count;

        return (humanCount, aiCount);
    }
}

public class GameSession
{
    public string SessionId { get; set; } = string.Empty;
    public string GameId { get; set; } = string.Empty;
    public string LobbyId { get; set; } = string.Empty;
    public SessionState State { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    public SessionEndReason? EndReason { get; set; }
    public List<string> PlayerIds { get; set; } = new();
    public List<string> AIPlayerIds { get; set; } = new();
    public HashSet<string> ActivePlayerConnections { get; set; } = new();
}

public class GameLobby
{
    public string LobbyId { get; set; } = string.Empty;
    public string HostPlayerId { get; set; } = string.Empty;
    public LobbySettings Settings { get; set; } = new();
    public LobbyState State { get; set; }
    public List<LobbyPlayer> Players { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public string? GameId { get; set; }
    public string? SessionId { get; set; }
}

public class LobbyPlayer
{
    public string PlayerId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public bool IsReady { get; set; }
    public bool IsAI { get; set; }
    public DifficultyLevel? AIDifficulty { get; set; }
    public DateTime JoinedAt { get; set; }
}

public class LobbySettings
{
    public int MinPlayers { get; set; } = 2;
    public int MaxPlayers { get; set; } = 6;
    public string GameMode { get; set; } = "Standard";
    public string MapName { get; set; } = "Default";
    public int StartingPopulation { get; set; } = 100;
    public int StartingMetal { get; set; } = 50;
    public int StartingFuel { get; set; } = 50;
    public bool AllowSpectators { get; set; } = false;
    public int TurnTimeLimit { get; set; } = 300;
}

public class PlayerConnection
{
    public string PlayerId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public Channel<GameStateUpdate>? UpdateChannel { get; set; }
    public DateTime ConnectedAt { get; set; }
    public DateTime? DisconnectedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public bool IsActive { get; set; }
}

public enum SessionState
{
    Active,
    Paused,
    Ended
}

public enum SessionEndReason
{
    Normal,
    Inactivity,
    HostLeft,
    Error,
    AdminTerminated
}

public enum LobbyState
{
    Waiting,
    Starting,
    InGame,
    Ended
}
