# Main Menu System

## Overview

The main menu system provides a user-friendly interface for connecting to the game server, configuring client settings, and handling connection errors with automatic retry logic.

![Main Menu Screen](screenshots/main_menu_screen.png)

## Components

### MainMenu.cs
The primary menu interface with the following screens:
- **Main Menu**: Connect to Server, Settings, Exit buttons
- **Settings**: Configure server address, resolution, and fullscreen mode
- **Connecting**: Loading screen during connection attempt
- **Error**: Displays connection errors with user-friendly messages

**UI Implementation:** Now uses Myra widgets (Desktop, TextButton, TextBox, ComboBox, CheckButton, Grid, Panel, Label)

### Settings.cs
Persistent client settings management:
- **ServerAddress**: Game server URL (default: http://localhost:5000)
- **ResolutionWidth/Height**: Window dimensions
- **Fullscreen**: Fullscreen mode toggle
- Settings are saved to `settings.json` and automatically loaded on startup

![Settings Screen](screenshots/main_menu_settings.png)

### ConnectionManager.cs
Robust connection handling with retry logic:
- **Auto-reconnect**: Automatically attempts to reconnect up to 3 times on connection loss
- **Reconnect Delay**: 2-second delay between retry attempts
- **Error Handling**: User-friendly error messages for different gRPC error codes
- **Status Tracking**: Connection status (Disconnected, Connecting, Connected, Error, Reconnecting)

### UI Controls (UIControls.cs)
**Status:** Legacy system, MainMenu has been migrated to Myra

Additional UI components (still used by other screens):
- **DropdownField**: Expandable dropdown menu for resolution selection
- **CheckboxField**: Toggle control for fullscreen setting
- **Button**: Clickable buttons
- **TextInputField**: Text input fields
- **NumericInputField**: Number input with increment/decrement
- **RadioButton/RadioButtonGroup**: Mutually exclusive options

**Note:** Other screens (LobbyScreen, CreateLobbyScreen, etc.) will be migrated to Myra in future updates.

## Myra Migration

### What Changed in MainMenu.cs

**Before (Custom UIControls):**
- Manual rendering using SpriteBatch and pixel textures
- Custom input handling (mouse states, keyboard events)
- Manual layout positioning with Rectangle bounds
- Custom focus management
- ~413 lines with extensive rendering code

**After (Myra Widgets):**
- Declarative UI construction using Myra widgets
- Automatic input handling by Desktop
- Grid-based responsive layouts
- Built-in focus and hover management
- ~535 lines but cleaner, more maintainable code
- No custom rendering code required

### Widget Mapping
| Custom Control | Myra Widget |
|---------------|-------------|
| `Button` | `TextButton` |
| `TextInputField` | `TextBox` |
| `DropdownField` | `ComboBox` |
| `CheckboxField` | `CheckButton` |
| Label rendering | `Label` |
| Manual layouts | `Grid` + `Panel` |
| Pixel texture drawing | Built-in widget rendering |

### Benefits
1. **Reduced Complexity**: No manual rendering code needed
2. **Better Maintainability**: Declarative UI is easier to modify
3. **Visual Consistency**: Myra provides consistent theming
4. **Less Code Duplication**: Reusable widgets across screens
5. **Professional Look**: Built-in styling and effects
6. **Input Handling**: Automatic mouse and keyboard management
7. **Focus Management**: Built-in focus system
8. **Accessibility**: Better support for keyboard navigation

### Myra Initialization
Myra is initialized in `RiskyStarsGame.cs` constructor:
```csharp
MyraEnvironment.Game = this;
```

The MainMenu creates a `Desktop` instance in `LoadContent()` which manages all UI rendering and input.

## Features

### Connection Retry Logic
When a connection fails or is lost:
1. Connection status changes to `Error`
2. Automatic reconnection begins after 2-second delay
3. Up to 3 reconnection attempts are made
4. If all attempts fail, user is returned to main menu with error message
5. Reconnection status is displayed in-game with attempt counter

![Connecting Screen](screenshots/main_menu_connecting.png)

### Error Handling
The system provides user-friendly error messages for common connection issues:
- **Server Unavailable**: Check server address
- **Connection Timeout**: Server may be slow or unreachable
- **Authentication Failed**: Invalid credentials
- **Server at Capacity**: Try again later
- And more specific gRPC error codes

![Error Screen](screenshots/main_menu_error.png)

### Settings Persistence
Settings are automatically:
- Loaded from `settings.json` on game startup
- Saved when "Save" is clicked in Settings menu
- Applied when connecting to server
- Resolution changes take effect when returning to main menu or connecting

## UI Structure

### Main Menu Screen
```
Panel (fullscreen)
└── Grid (centered, vertical)
    ├── Label "RiskyStars" (title, cyan, 2.5x scale)
    ├── TextButton "Connect to Server"
    ├── TextButton "Settings"
    └── TextButton "Exit"
```

### Settings Screen
```
Panel (fullscreen, semi-transparent background)
└── Grid (centered, vertical)
    ├── Label "Settings" (title, cyan, 1.2x scale)
    ├── Label "Server Address"
    ├── TextBox (server address input)
    ├── Label "Resolution"
    ├── ComboBox (resolution selector)
    ├── CheckButton "Fullscreen"
    └── Grid (horizontal buttons)
        ├── TextButton "Save"
        └── TextButton "Back"
```

### Connecting Screen
```
Panel (fullscreen)
└── Grid (centered)
    └── Label "Connecting to server..." (yellow)
```

### Error Screen
```
Panel (fullscreen, semi-transparent background)
└── Grid (centered, vertical)
    ├── Label "Connection Error" (red, 1.2x scale)
    ├── Label (error message, wrapped)
    └── TextButton "OK"
```

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
- **Myra**: UI framework for rendering and input handling

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

## Screenshots Needed

The following screenshots are required for complete documentation:

1. **Main Menu Screen** (`screenshots/main_menu_screen.png`)
   - Capture: Main menu with "Connect to Server", "Settings", and "Exit" buttons visible
   - Requirements: Fresh application start, default resolution

2. **Settings Screen** (`screenshots/main_menu_settings.png`)
   - Capture: Settings menu showing server address field, resolution dropdown (expanded), and fullscreen checkbox
   - Requirements: Settings menu open, dropdown expanded to show options

3. **Connecting Screen** (`screenshots/main_menu_connecting.png`)
   - Capture: Connection screen with loading indicator and "Connecting to server..." message
   - Requirements: Click "Connect to Server" and capture during connection attempt

4. **Error Screen** (`screenshots/main_menu_error.png`)
   - Capture: Error screen showing connection failure message with "Retry" and "Back to Menu" buttons
   - Requirements: Ensure server is not running, attempt connection to trigger error state
