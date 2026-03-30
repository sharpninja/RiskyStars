# GameSessionManager AI Player Integration

## Overview

The `GameSessionManager` has been updated to fully support AI players throughout the game lifecycle, from lobby setup through game execution and cleanup.

## Key Features

### 1. AI Player Lobby Management

**Add AI Players to Lobby**
```csharp
public bool AddAIPlayerToLobby(string lobbyId, string aiName, DifficultyLevel difficulty)
```
- Adds an AI player to a lobby with specified name and difficulty
- AI players are automatically marked as ready
- Generates unique AI player IDs with format: `ai_{name}_{count}_{guid}`
- Enforces lobby player limits

**Remove AI Players from Lobby**
```csharp
public bool RemoveAIPlayerFromLobby(string lobbyId, string aiPlayerId)
```
- Removes an AI player from a lobby by ID
- Only AI players can be removed this way (human players use LeaveLobby)

### 2. AI Player Lifecycle Management

**Game Creation**
- When a game starts, AI players are created as `AIPlayer` instances
- Each AI player is initialized with:
  - Player ID from lobby
  - Player name from lobby
  - Difficulty level from lobby settings
  - Starting resources from lobby settings
  - AI configuration based on difficulty

**Session Tracking**
- `GameSession.AIPlayerIds` tracks all AI players in the session
- `_aiPlayers` dictionary maintains active AI player instances
- AI players are tracked separately from human player connections

**Cleanup**
- When a session ends, all AI players are removed from tracking
- Human players are disconnected normally
- AI players don't maintain network connections, so no disconnection needed

### 3. AI Turn Execution

**Automatic Turn Triggering**

AI turns are automatically triggered at two points:

1. **Game Start** (if first player is AI)
```csharp
// After game creation in StartGame()
_ = Task.Run(async () =>
{
    await Task.Delay(1000);  // Give game state time to stabilize
    await ProcessAITurnIfNeededAsync(session.SessionId);
});
```

2. **After Phase Advancement** (if next player is AI)
```csharp
// In GameServiceImpl after AdvancePhase
var session = _sessionManager.GetSessionByGameId(gameId);
if (session != null)
{
    _ = Task.Run(async () => 
    {
        await Task.Delay(500);  // Brief delay for state consistency
        await _sessionManager.ProcessAITurnIfNeededAsync(session.SessionId);
    });
}
```

**Manual Turn Triggering**
```csharp
public async Task TriggerAIPlayerTurnAsync(string gameId, string aiPlayerId)
```
- Manually triggers a turn for a specific AI player
- Useful for testing or recovery scenarios

### 4. Helper Methods

**Query Methods**
```csharp
public AIPlayer? GetAIPlayer(string playerId)
public List<AIPlayer> GetSessionAIPlayers(string sessionId)
public bool IsAIPlayer(string playerId)
public (int humanCount, int aiCount) GetLobbyPlayerCounts(string lobbyId)
public (int humanCount, int aiCount) GetSessionPlayerCounts(string sessionId)
```

### 5. AI Player Restrictions

The following restrictions are enforced to maintain game integrity:

1. **Cannot Leave Lobby** - Only host can remove AI via `RemoveAIPlayerFromLobby`
2. **Cannot Set Ready Status** - Always ready by default
3. **Cannot Connect to Game Streams** - No network connection needed
4. **Cannot Be Lobby Host** - Host reassignment skips AI players

## Data Model Changes

### LobbyPlayer
```csharp
public class LobbyPlayer
{
    public string PlayerId { get; set; }
    public string PlayerName { get; set; }
    public bool IsReady { get; set; }
    public bool IsAI { get; set; }                    // NEW
    public DifficultyLevel? AIDifficulty { get; set; } // NEW
    public DateTime JoinedAt { get; set; }
}
```

### GameSession
```csharp
public class GameSession
{
    public string SessionId { get; set; }
    public string GameId { get; set; }
    public string LobbyId { get; set; }
    public SessionState State { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public SessionEndReason? EndReason { get; set; }
    public List<string> PlayerIds { get; set; }
    public List<string> AIPlayerIds { get; set; }      // NEW
    public HashSet<string> ActivePlayerConnections { get; set; }
}
```

### GameSessionManager Fields
```csharp
private readonly ConcurrentDictionary<string, AIPlayer> _aiPlayers = new();     // NEW
private readonly AIPlayerController? _aiPlayerController;                        // NEW
```

## Integration with Other Services

### LobbyServiceImpl

AI player functionality is available through the `GameSessionManager` API but gRPC endpoints are not yet implemented (would require proto file updates):

```csharp
// Direct API usage (non-gRPC)
_sessionManager.AddAIPlayerToLobby(lobbyId, "AI Commander", DifficultyLevel.Hard);
_sessionManager.RemoveAIPlayerFromLobby(lobbyId, aiPlayerId);
```

Features:
- Host-only operations (only lobby host should be able to add/remove AI)
- Difficulty level support (Easy, Medium, Hard)
- Lobby validation (state, player count, etc.)
- AI player tagging in lobby listings (marked with [AI - Difficulty])

### GameServiceImpl

Updated to integrate with AI system:

```csharp
// Prevents AI players from connecting
if (_sessionManager.IsAIPlayer(playerId))
{
    await SendConnectionError(responseStream, "AI players cannot connect to game streams");
    return;
}

// Triggers AI turns after phase advancement
case GamePlayerAction.ActionOneofCase.AdvancePhase:
    _gameStateManager.AdvancePhase(gameId);
    var session = _sessionManager.GetSessionByGameId(gameId);
    if (session != null)
    {
        _ = Task.Run(async () => await _sessionManager.ProcessAITurnIfNeededAsync(session.SessionId));
    }
    break;
```

## Supported Game Modes

The system now supports all player configurations:

1. **Pure Human** - Traditional multiplayer (2-6 human players)
2. **Human vs AI** - Mixed games (e.g., 2 humans + 2 AI)
3. **Pure AI** - Simulation mode (2-6 AI players)
4. **Single Player** - One human vs multiple AI opponents

## Example Usage

### Creating a Game with AI Players

```csharp
// Host creates lobby
var lobbyId = _sessionManager.CreateLobby(hostPlayerId, "Host Player", settings);

// Host adds AI players
_sessionManager.AddAIPlayerToLobby(lobbyId, "AI Commander Alpha", DifficultyLevel.Hard);
_sessionManager.AddAIPlayerToLobby(lobbyId, "AI Commander Beta", DifficultyLevel.Medium);
_sessionManager.AddAIPlayerToLobby(lobbyId, "AI Commander Gamma", DifficultyLevel.Easy);

// Additional human player joins
_sessionManager.JoinLobby(lobbyId, player2Id, "Human Player 2");

// Human player sets ready
_sessionManager.SetPlayerReady(lobbyId, player2Id, true);

// Host starts game (AI players are always ready)
var sessionId = _sessionManager.StartGame(lobbyId);

// Game automatically handles AI turns as they come up
```

### Querying AI Player Information

```csharp
// Check if current player is AI
var game = _gameStateManager.GetGame(gameId);
if (game.CurrentPlayer is AIPlayer aiPlayer)
{
    Console.WriteLine($"AI player {aiPlayer.Name} ({aiPlayer.DifficultyLevel}) is taking their turn");
}

// Get player counts
var (humanCount, aiCount) = _sessionManager.GetSessionPlayerCounts(sessionId);
Console.WriteLine($"Game has {humanCount} human players and {aiCount} AI players");

// Get all AI players in session
var aiPlayers = _sessionManager.GetSessionAIPlayers(sessionId);
foreach (var ai in aiPlayers)
{
    Console.WriteLine($"AI: {ai.Name} - {ai.DifficultyLevel}");
}
```

## Thread Safety

All AI player operations are thread-safe:
- Uses `ConcurrentDictionary` for AI player storage
- Lobby operations protected by `_lobbyLock`
- AI turn execution uses GameStateManager's existing locking
- Async operations use `Task.Run` to avoid blocking

## Error Handling

The system handles various error scenarios:

1. **Invalid Lobby State** - Cannot add AI to starting/running games
2. **Lobby Full** - Respects max player limits
3. **Duplicate AI Players** - Generates unique IDs to prevent conflicts
4. **Missing AIPlayerController** - Gracefully handles when controller not injected
5. **Invalid AI Player Operations** - Prevents AI players from using human-only operations

## Performance Considerations

- AI player tracking uses `O(1)` lookups via dictionary
- AI turn triggering is async and non-blocking
- Minimal overhead for human-only games (no AI code executed)
- AI execution happens on background threads
- Thinking delays add realism but can be adjusted per difficulty

## Testing Support

The system provides several features useful for testing:

1. **Pure AI Games** - Test AI strategy without human intervention
2. **Manual Turn Triggering** - `TriggerAIPlayerTurnAsync` for controlled testing
3. **Player Count Queries** - Verify correct lobby/session composition
4. **IsAIPlayer Checks** - Validate player type in tests

## Future Enhancements

Possible future improvements:

1. **AI Personality Types** - Aggressive, Defensive, Balanced personalities
2. **AI Learning** - Adaptive difficulty based on player performance
3. **AI Handicaps** - Resource modifiers for balancing
4. **Spectator AI** - AI that observes but doesn't play
5. **AI Replay** - Record and replay AI decision-making
6. **Custom AI Scripts** - Player-definable AI behaviors
