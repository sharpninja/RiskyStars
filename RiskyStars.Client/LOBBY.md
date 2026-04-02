# Lobby System Documentation

## Overview

The RiskyStars client features a complete lobby system that allows players to:
- Connect to the game server
- Browse available game lobbies
- Create new game lobbies with custom settings
- Join existing lobbies
- Ready-up and start games

![Lobby Browser Screen](screenshots/lobby_browser.png)

## Architecture

The lobby system consists of several components:

### Core Components

1. **LobbyClient** (`LobbyClient.cs`)
   - gRPC client for lobby service communication
   - Handles authentication and lobby operations
   - Methods: Authenticate, CreateLobby, JoinLobby, LeaveLobby, SetReady, StartGame, ListLobbies, GetLobby

2. **LobbyManager** (`LobbyManager.cs`)
   - Main coordinator for lobby UI flow
   - Manages state transitions between screens
   - Handles async operations with the lobby service
   - States: Connection, Browser, CreateLobby, InLobby, StartingGame, InGame

### UI Screens

1. **ConnectionScreen** (`ConnectionScreen.cs`)
   - Initial connection screen
   - Player name and server address input
   - Authenticates with lobby service

![Connection Screen](screenshots/lobby_connection.png)

2. **LobbyBrowserScreen** (`LobbyBrowserScreen.cs`)
   - Displays list of available game lobbies
   - Shows lobby info: host, map, mode, player count
   - Auto-refreshes every 2 seconds
   - Buttons: Create Lobby, Join Lobby, Refresh

3. **CreateLobbyScreen** (`CreateLobbyScreen.cs`)
   - Lobby creation interface
   - Settings:
     - Map Name (text input)
     - Max Players (2-6, numeric input with +/- buttons)
     - Default settings: Standard mode, 100 population, 50 metal/fuel

![Create Lobby Screen](screenshots/lobby_create.png)

4. **LobbyScreen** (`LobbyScreen.cs`)
   - In-lobby player list and ready-up interface
   - Shows lobby info and player list
   - Ready button for non-hosts
   - Start Game button for host (enabled when all players ready)
   - Leave Lobby button
   - Auto-refreshes lobby state every 1 second

![In-Lobby Screen](screenshots/lobby_waiting_room.png)

### UI Controls

1. **Button** (`UIControls.cs`)
   - Standard clickable button with hover effects
   - Properties: IsClicked, IsEnabled

2. **TextInputField** (`UIControls.cs`)
   - Text input with focus, cursor, and keyboard input
   - Supports alphanumeric and special characters
   - Max length configurable

3. **NumericInputField** (`UIControls.cs`)
   - Numeric value selector with +/- buttons
   - Min/max value constraints
   - Used for player count selection

## Flow Diagram

```
Start
  ↓
ConnectionScreen (authenticate)
  ↓
LobbyBrowserScreen
  ├─→ CreateLobbyScreen → LobbyScreen
  └─→ Join Selected Lobby → LobbyScreen
         ↓
      LobbyScreen (ready-up)
         ↓
      Host Starts Game
         ↓
      Transition to Game
```

## Integration with Game

When the lobby manager transitions to `InGame` state:

1. LobbyManager provides `SessionId` and `PlayerName`
2. RiskyStarsGame calls `InitializeGame(sessionId, playerName)`
3. Creates GrpcGameClient and connects to game session
4. Initializes game components (InputController, PlayerDashboard)
5. Game begins with authenticated player

## Key Features

### Auto-Refresh
- Browser: Refreshes lobby list every 2 seconds
- In-Lobby: Refreshes lobby state every 1 second
- Manual refresh buttons available

### State Management
- All async operations tracked with `_pendingTask`
- Prevents multiple simultaneous requests
- Clean state transitions with Reset() methods

### Error Handling
- Try-catch blocks on all async operations
- Connection errors displayed to user
- Failed operations don't crash the client

### Visual Polish
- Cyan color scheme for active/focused elements
- Hover effects on buttons
- Selection highlighting in lobby browser
- Ready status indicators
- Host vs player UI differences

## Usage Example

```csharp
// In RiskyStarsGame.cs
private LobbyManager? _lobbyManager;

protected override void Initialize()
{
    _lobbyManager = new LobbyManager(GraphicsDevice, screenWidth, screenHeight);
}

protected override void Update(GameTime gameTime)
{
    if (_lobbyManager != null && !_lobbyManager.IsInGame)
    {
        _lobbyManager.Update(gameTime);
        
        if (_lobbyManager.IsInGame && _lobbyManager.SessionId != null)
        {
            InitializeGame(_lobbyManager.SessionId, _lobbyManager.PlayerName ?? "Player");
        }
    }
    else
    {
        // Normal game update logic
    }
}

protected override void Draw(GameTime gameTime)
{
    if (_lobbyManager != null && !_lobbyManager.IsInGame)
    {
        _lobbyManager.Draw(_spriteBatch);
    }
    else
    {
        // Normal game draw logic
    }
}
```

## Server Integration

The client lobby system communicates with the server's LobbyService (defined in `risky_stars.proto`):

- **Authenticate**: Get auth token for subsequent requests
- **CreateLobby**: Create new lobby with settings
- **JoinLobby**: Join existing lobby by ID
- **LeaveLobby**: Leave current lobby
- **SetReady**: Toggle ready status
- **StartGame**: Host starts game (returns session ID)
- **ListLobbies**: Get all available lobbies
- **GetLobby**: Get specific lobby details

All authenticated requests include `Authorization: Bearer {token}` header.

## Future Enhancements

Potential improvements:
- Chat system in lobby
- Player kick functionality for host
- Spectator mode support
- Custom game mode selection
- Map preview/selection
- Player color selection
- Lobby password protection
- Friend/invite system

## Screenshots Needed

The following screenshots are required for complete documentation:

1. **Connection Screen** (`screenshots/lobby_connection.png`)
   - Capture: Initial connection screen with player name field and "Connect" button
   - Requirements: Fresh application start, before authentication

2. **Lobby Browser Screen** (`screenshots/lobby_browser.png`)
   - Capture: Lobby browser showing multiple available lobbies with player counts and map names
   - Requirements: Connected to server, multiple lobbies created, show selection highlighting

3. **Create Lobby Screen** (`screenshots/lobby_create.png`)
   - Capture: Lobby creation interface with all settings visible (map name, max players selector)
   - Requirements: Click "Create Lobby" button from browser

4. **In-Lobby Screen** (`screenshots/lobby_waiting_room.png`)
   - Capture: Inside a lobby showing player list, ready status indicators, and host controls
   - Requirements: Multiple players in lobby, some ready/some not ready, show from host perspective
