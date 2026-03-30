# Main Menu System

## Overview

The main menu system provides a user-friendly interface for connecting to the game server, configuring client settings, and handling connection errors with automatic retry logic.

## Components

### MainMenu.cs
The primary menu interface with the following screens:
- **Main Menu**: Connect to Server, Settings, Exit buttons
- **Settings**: Configure server address, resolution, and fullscreen mode
- **Connecting**: Loading screen during connection attempt
- **Error**: Displays connection errors with user-friendly messages

### Settings.cs
Persistent client settings management:
- **ServerAddress**: Game server URL (default: http://localhost:5000)
- **ResolutionWidth/Height**: Window dimensions
- **Fullscreen**: Fullscreen mode toggle
- Settings are saved to `settings.json` and automatically loaded on startup

### ConnectionManager.cs
Robust connection handling with retry logic:
- **Auto-reconnect**: Automatically attempts to reconnect up to 3 times on connection loss
- **Reconnect Delay**: 2-second delay between retry attempts
- **Error Handling**: User-friendly error messages for different gRPC error codes
- **Status Tracking**: Connection status (Disconnected, Connecting, Connected, Error, Reconnecting)

### UI Controls (UIControls.cs)
Additional UI components added:
- **DropdownField**: Expandable dropdown menu for resolution selection
- **CheckboxField**: Toggle control for fullscreen setting

## Features

### Connection Retry Logic
When a connection fails or is lost:
1. Connection status changes to `Error`
2. Automatic reconnection begins after 2-second delay
3. Up to 3 reconnection attempts are made
4. If all attempts fail, user is returned to main menu with error message
5. Reconnection status is displayed in-game with attempt counter

### Error Handling
The system provides user-friendly error messages for common connection issues:
- **Server Unavailable**: Check server address
- **Connection Timeout**: Server may be slow or unreachable
- **Authentication Failed**: Invalid credentials
- **Server at Capacity**: Try again later
- And more specific gRPC error codes

### Settings Persistence
Settings are automatically:
- Loaded from `settings.json` on game startup
- Saved when "Save" is clicked in Settings menu
- Applied when connecting to server
- Resolution changes take effect when returning to main menu or connecting

## Game Flow

```
Main Menu
  ├─> Connect to Server
  │     └─> Lobby System (existing)
  │           └─> In Game
  │                 ├─> Connection Lost → Auto-reconnect
  │                 └─> ESC → Return to Main Menu
  │
  ├─> Settings
  │     ├─> Server Address (text input)
  │     ├─> Resolution (dropdown)
  │     ├─> Fullscreen (checkbox)
  │     └─> Save/Back
  │
  └─> Exit
```

## Controls

### Main Menu
- **Mouse**: Click buttons to navigate
- **ESC** (in-game): Return to main menu

### Settings Screen
- **Click** text fields to edit server address
- **Click** dropdown to select resolution
- **Click** checkbox to toggle fullscreen
- **Save**: Apply and save settings
- **Back**: Cancel changes and return

## Integration

The main menu integrates with existing systems:
- **LobbyManager**: Manages lobby browsing and game sessions
- **ConnectionManager**: Handles gRPC client connections
- **GrpcGameClient**: Low-level gRPC communication
- **RiskyStarsGame**: Main game loop and state management

## Usage

The game now starts at the main menu instead of directly connecting. Players must:
1. Configure server settings if needed (Settings menu)
2. Click "Connect to Server" to proceed to lobby
3. Use existing lobby system to join/create games
4. Play game with automatic reconnection support
5. Press ESC to return to main menu at any time

## Error Recovery

When connection errors occur:
- **Temporary Network Issues**: Auto-reconnect handles brief disconnections
- **Server Restart**: Client will retry connection automatically
- **Permanent Failures**: After 3 failed attempts, user returns to menu
- **User Control**: Players can manually disconnect via ESC key

## Technical Details

### State Management
The game uses a `GameState` enum to track current screen:
- `MainMenu`: Showing main menu or settings
- `Lobby`: In lobby system (existing)
- `InGame`: Actively playing with connection monitoring

### Connection Monitoring
The `ConnectionManager.Update()` method is called every frame to:
- Check if connection is still alive
- Initiate reconnection attempts if needed
- Update reconnection status

### Thread Safety
Connection attempts run asynchronously using `Task.Run()` to avoid blocking the game loop during network operations.
