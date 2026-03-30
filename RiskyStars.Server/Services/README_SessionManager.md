# GameSessionManager Documentation

## Overview

The `GameSessionManager` is responsible for managing player connections, game lobbies, player authentication, session lifecycle, and mapping client streams to game instances in the RiskyStars server.

## Key Components

### 1. GameSessionManager

Main manager class that coordinates all session-related operations.

**Features:**
- Player authentication with token generation
- Lobby creation and management
- Player connection tracking
- Session lifecycle management
- Automatic cleanup of inactive sessions and lobbies

### 2. Supporting Classes

#### GameSession
Represents an active game session with:
- Session ID and Game ID mapping
- Player list tracking
- Active connection monitoring
- Session state (Active, Paused, Ended)
- Session end reason tracking

#### GameLobby
Pre-game lobby where players gather before starting:
- Lobby settings (player counts, game mode, map, resources)
- Player ready status tracking
- Host management
- State transitions (Waiting → Starting → InGame → Ended)

#### PlayerConnection
Tracks individual player connections:
- Update channel for streaming game state
- Connection timestamps
- Activity tracking for timeout detection
- Active status monitoring

## Authentication Flow

1. **Player Authentication**
   ```csharp
   var authToken = sessionManager.AuthenticatePlayer(playerName, password);
   ```
   - Generates unique player ID based on player name
   - Creates secure authentication token
   - Stores token mapping for validation

2. **Token Validation**
   ```csharp
   if (sessionManager.ValidateAuthToken(authToken, out var playerId))
   {
       // Authenticated
   }
   ```

3. **Token Revocation**
   ```csharp
   sessionManager.RevokeAuthToken(authToken);
   ```

## Lobby Management Flow

### Creating a Lobby

```csharp
var settings = new LobbySettings
{
    MinPlayers = 2,
    MaxPlayers = 6,
    GameMode = "Standard",
    MapName = "Default",
    StartingPopulation = 100,
    StartingMetal = 50,
    StartingFuel = 50
};

var lobbyId = sessionManager.CreateLobby(hostPlayerId, hostPlayerName, settings);
```

### Joining a Lobby

```csharp
var success = sessionManager.JoinLobby(lobbyId, playerId, playerName);
```

### Player Ready Status

```csharp
sessionManager.SetPlayerReady(lobbyId, playerId, isReady: true);
```

### Starting the Game

```csharp
var sessionId = sessionManager.StartGame(lobbyId);
// Returns session ID on success, null on failure
```

**Start Game Requirements:**
- All players must be ready
- Player count must meet minimum requirements
- Lobby must be in Waiting state
- Only host can start the game

## Connection Management

### Connecting a Player

```csharp
var updateChannel = Channel.CreateUnbounded<GameStateUpdate>();
var connection = sessionManager.ConnectPlayer(playerId, sessionId, updateChannel);
```

### Disconnecting a Player

```csharp
sessionManager.DisconnectPlayer(playerId);
```

### Activity Tracking

```csharp
sessionManager.UpdatePlayerActivity(playerId);
```

Called whenever player sends an action to prevent timeout.

### Checking Connection Status

```csharp
var isConnected = sessionManager.IsPlayerConnected(playerId);
```

## Session Lifecycle

### Getting Session Information

```csharp
var session = sessionManager.GetSession(sessionId);
var sessionByGame = sessionManager.GetSessionByGameId(gameId);
```

### Ending a Session

```csharp
sessionManager.EndSession(sessionId, SessionEndReason.Normal);
```

**End Reasons:**
- Normal: Game completed normally
- Inactivity: Timed out due to inactivity
- HostLeft: Host player disconnected
- Error: Server error occurred
- AdminTerminated: Manually terminated by admin

### Active Players

```csharp
var activePlayers = sessionManager.GetActivePlayers(sessionId);
```

## Broadcasting and Messaging

### Broadcast to All Players in Session

```csharp
var update = new GameStateUpdate { /* ... */ };
sessionManager.BroadcastToSession(sessionId, update);
```

### Send to Individual Player

```csharp
var update = new GameStateUpdate { /* ... */ };
sessionManager.SendToPlayer(playerId, update);
```

## Automatic Cleanup

The `SessionCleanupService` runs as a background service that:
- Runs every 5 minutes
- Removes sessions inactive for 30+ minutes
- Removes empty lobbies older than 30 minutes

```csharp
sessionManager.CleanupInactiveSessions(TimeSpan.FromMinutes(30));
```

## gRPC Service Integration

### LobbyService

Provides lobby management RPC methods:
- `Authenticate` - Player authentication
- `CreateLobby` - Create new lobby
- `JoinLobby` - Join existing lobby
- `LeaveLobby` - Leave lobby
- `SetReady` - Set ready status
- `StartGame` - Start game from lobby
- `ListLobbies` - Get available lobbies
- `GetLobby` - Get lobby details

### RiskyStarsGameService

Handles bidirectional streaming for gameplay:
- `PlayGame` - Main game stream (PlayerAction → GameStateUpdate)
- Validates authentication via headers
- Maps player to session and game instance
- Handles connection/disconnection events
- Routes player actions to game logic

## Session Extensions

Helper methods for gRPC context handling:

```csharp
// Get auth token from headers
var token = context.GetAuthToken();

// Get session ID from headers
var sessionId = context.GetSessionId();

// Try authenticate
if (context.TryAuthenticatePlayer(sessionManager, out var playerId))
{
    // Authenticated
}

// Throw if not authenticated
context.ThrowIfNotAuthenticated(sessionManager, out var playerId);
```

## Thread Safety

All public methods are thread-safe using:
- `ConcurrentDictionary` for collections
- Lock objects for lobby operations
- Atomic operations where applicable

## Example Complete Flow

```csharp
// 1. Authenticate
var authToken = sessionManager.AuthenticatePlayer("PlayerOne", null);

// 2. Create lobby
var settings = new LobbySettings { MinPlayers = 2, MaxPlayers = 4 };
var lobbyId = sessionManager.CreateLobby(playerId, "PlayerOne", settings);

// 3. Other players join
sessionManager.JoinLobby(lobbyId, player2Id, "PlayerTwo");
sessionManager.SetPlayerReady(lobbyId, player2Id, true);

// 4. Host starts game
var sessionId = sessionManager.StartGame(lobbyId);

// 5. Players connect to game session
var channel1 = Channel.CreateUnbounded<GameStateUpdate>();
sessionManager.ConnectPlayer(playerId, sessionId, channel1);

var channel2 = Channel.CreateUnbounded<GameStateUpdate>();
sessionManager.ConnectPlayer(player2Id, sessionId, channel2);

// 6. Broadcast game state
var update = new GameStateUpdate { /* ... */ };
sessionManager.BroadcastToSession(sessionId, update);

// 7. Game ends
sessionManager.EndSession(sessionId, SessionEndReason.Normal);
```

## Configuration

Default values can be modified in `SessionCleanupService`:
- Cleanup interval: 5 minutes
- Inactivity threshold: 30 minutes

## Integration with GameStateManager

The `GameSessionManager` creates `Game` instances and registers them with `GameStateManager`:

```csharp
var game = CreateGameFromLobby(lobby);
_gameStateManager.CreateGame(game);
```

This establishes the mapping:
- Session → Game
- Players → Game.Players
- Client Streams → Game Updates
