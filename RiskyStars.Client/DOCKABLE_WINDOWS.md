# Dockable Windows System

The RiskyStars client implements a resizable and dockable window management system using Myra's Window component, with user preference persistence.

## Overview

Three primary UI panels are implemented as dockable windows:
- **Player Dashboard Window** - Resource management, army purchasing, and hero assignment
- **AI Visualization Window** - Real-time AI action tracking and visualization controls
- **Debug Info Window** - Performance metrics, camera info, and game state debugging

## Features

### Window Management
- **Resizable** - All windows can be resized by dragging edges/corners (Myra built-in)
- **Draggable** - Windows can be moved freely around the screen
- **Dockable** - Windows can snap to screen edges and corners
- **Toggleable** - Windows can be shown/hidden with keyboard shortcuts
- **Persistent** - Window positions, sizes, and visibility are saved to disk

### Docking Positions
Windows can be docked to:
- **Edges**: Left, Right, Top, Bottom
- **Corners**: TopLeft, TopRight, BottomLeft, BottomRight
- **None**: Free-floating anywhere on screen

### Keyboard Shortcuts
- **F1** - Toggle Debug Info Window
- **F2** - Toggle Player Dashboard Window
- **F3** - Toggle AI Visualization Window

## Architecture

### Core Classes

#### `WindowPreferences`
Manages persistence of window states to `window_preferences.json`.

```csharp
public class WindowState
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public bool IsVisible { get; set; }
    public bool IsMinimized { get; set; }
    public DockPosition DockPosition { get; set; }
}
```

#### `DockableWindow`
Base class providing common window functionality:
- Position and size management
- Docking logic and snap detection
- State persistence
- Theme integration

```csharp
public class DockableWindow
{
    protected Window _window;
    protected readonly string _windowId;
    protected readonly WindowPreferences _preferences;
    
    public void Toggle();
    public void Show();
    public void Hide();
    public void DockTo(DockPosition position);
    public void SaveState();
}
```

### Window Implementations

#### `PlayerDashboardWindow`
Displays player resources, army purchasing options, and hero management.

**Default Position**: Right edge  
**Default Size**: 340x600

**Features**:
- Real-time resource display (Population, Metal, Fuel)
- Production rates with color-coded indicators
- Territory and army counts
- Army purchase buttons (1, 5, 10, 25 units)
- Purchase availability based on phase and resources
- Hero assignment interface (coming soon)

**Usage**:
```csharp
var dashboard = new PlayerDashboardWindow(
    gameClient, 
    windowPreferences, 
    screenWidth, 
    screenHeight
);
dashboard.SetCurrentPlayer(playerId);
dashboard.UpdateContent(gameStateCache);
```

#### `AIVisualizationWindow`
Controls and displays AI player activity visualization.

**Default Position**: Top-left corner  
**Default Size**: 400x500

**Features**:
- AI thinking status indicator
- Current action display
- Visualization options:
  - Show/hide movement animations
  - Show/hide reinforcement highlights
  - Show/hide purchase indicators
  - Auto-follow AI actions with camera
- Activity log (last 10 events with timestamps)

**Usage**:
```csharp
var aiViz = new AIVisualizationWindow(
    windowPreferences, 
    screenWidth, 
    screenHeight
);
aiViz.UpdateAIStatus("AI Player Name", isThinking: true);
aiViz.LogActivity("AI moved 10 units: Alpha → Beta");
```

#### `DebugInfoWindow`
Displays debugging information and performance metrics.

**Default Position**: Bottom-left corner  
**Default Size**: 380x400

**Features**:
- Camera position and zoom level
- FPS counter with color-coded health (green/yellow/red)
- Game state information
- Connection status with color indicators
- Player count and turn phase
- Current selection details

**Usage**:
```csharp
var debug = new DebugInfoWindow(
    windowPreferences, 
    screenWidth, 
    screenHeight
);
debug.UpdateCameraInfo(camera);
debug.UpdateGameStateInfo(gameStateCache, connectionManager);
debug.UpdateSelectionInfo(selectionState);
```

## Integration with AI Tracking

The `AIActionTracker` integrates with `AIVisualizationWindow` to provide real-time feedback:

```csharp
_aiActionTracker.SetAIVisualizationWindow(_aiVisualizationWindow);
```

AI actions are automatically logged to the visualization window:
- Army purchases
- Reinforcements
- Army movements
- Territory captures

Visualization options in the window control whether animations are shown:
- `ShowMovementAnimations` - Control army movement animations
- `ShowReinforcementHighlights` - Control reinforcement pulse effects
- `AutoFollowAIActions` - Camera follows AI movements

## Styling

All windows use the ThemeManager for consistent styling:
- **Background**: Semi-transparent dark panels
- **Borders**: Cyan accent color (changes to lighter cyan on hover)
- **Title Bar**: Themed with accent text color
- **Content**: Uses ThemedUIFactory for all UI elements

## State Persistence

Window states are automatically saved when:
- Window is moved
- Window is resized
- Window visibility changes
- Window is docked to a new position

States are loaded on initialization:
- Position and size restored
- Visibility state restored
- Dock position applied

## Docking Behavior

Windows automatically detect proximity to screen edges:
- Within 50 pixels of an edge triggers dock suggestion
- Docking respects a 10-pixel margin from screen edges
- Docked windows maintain their width/height where appropriate
- Corner docking takes precedence over edge docking

## Desktop Management

All windows are managed through Myra's Desktop:

```csharp
_inGameDesktop = new Desktop();
_inGameDesktop.Widgets.Add(_playerDashboardWindow.Window);
_inGameDesktop.Widgets.Add(_aiVisualizationWindow.Window);
_inGameDesktop.Widgets.Add(_debugInfoWindow.Window);
_inGameDesktop.Render();
```

## Best Practices

### Creating New Dockable Windows

1. Extend `DockableWindow` base class
2. Call base constructor with unique window ID
3. Build UI content using ThemedUIFactory
4. Set default size and dock position
5. Implement update methods for dynamic content

Example:
```csharp
public class MyWindow : DockableWindow
{
    public MyWindow(WindowPreferences preferences, int screenWidth, int screenHeight)
        : base("my_window_id", "My Window Title", preferences, 
               screenWidth, screenHeight, defaultWidth: 300, defaultHeight: 400)
    {
        BuildContent();
        DockTo(DockPosition.TopRight);
    }
    
    private void BuildContent()
    {
        var layout = ThemedUIFactory.CreateVerticalStack();
        // Add widgets...
        _window.Content = layout;
    }
}
```

### Performance Considerations

- Only update window content when visible
- Use efficient data structures for activity logs
- Throttle FPS calculations to reduce overhead
- Cache frequently accessed data

### User Experience

- Provide clear visual feedback for window states
- Use consistent keyboard shortcuts (F-keys)
- Preserve window states across sessions
- Ensure windows don't overlap critical game UI
- Auto-dock to sensible default positions

## File Locations

- `WindowPreferences.cs` - Preference management and persistence
- `DockableWindow.cs` - Base window class
- `PlayerDashboardWindow.cs` - Player dashboard implementation
- `AIVisualizationWindow.cs` - AI visualization implementation
- `DebugInfoWindow.cs` - Debug info implementation
- `window_preferences.json` - Saved window states (created at runtime)

## Future Enhancements

- Custom window layouts (save/load presets)
- Window tabbing (combine multiple windows)
- Window minimization to taskbar
- Transparency adjustment per window
- Snap-to-grid alignment
- Multi-monitor support
- Window focus management
- Keyboard navigation between windows
