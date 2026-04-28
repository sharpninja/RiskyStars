# In-Game Settings Window Documentation

## Overview

The RiskyStars client features a comprehensive in-game settings overlay accessible via the Escape key. This allows players to adjust graphics, audio, controls, and server settings without returning to the main menu.

## Components

### SettingsWindow

**Location**: `RiskyStars.Client/SettingsWindow.cs`

The `SettingsWindow` class provides a tabbed settings interface using Myra's Window widget.

#### Features

- **Tabbed Interface**: Organized settings across multiple tabs
- **Runtime Changes**: Apply settings changes without restarting
- **Validation**: Input validation for server addresses
- **Visual Feedback**: Real-time slider value updates
- **Themed UI**: Consistent styling using ThemeManager

#### Tabs

##### 1. Graphics Tab

Display and rendering settings:

- **Resolution**: Common resolutions from 1280x720 to 3840x2160
- **Fullscreen Mode**: Toggle fullscreen on/off
- **VSync**: Enable/disable vertical synchronization
- **Target Frame Rate**: Choose from 30, 60, 120, 144 FPS, or Unlimited
- **Show Debug Info**: Auto-show debug window on startup
- **Show FPS Counter**: Display frames per second

##### 2. Audio Tab

Volume control settings (placeholder for future implementation):

- **Master Volume**: Overall volume control (0-100%)
- **Music Volume**: Background music volume (0-100%)
- **SFX Volume**: Sound effects volume (0-100%)

Note: Audio system implementation is planned for a future update.

##### 3. Controls Tab

Camera and input configuration:

- **Pan Speed**: Camera movement speed (1.0 - 10.0)
- **Zoom Speed**: Camera zoom sensitivity (0.05 - 0.5)
- **Invert Zoom**: Reverse mouse wheel zoom direction
- **Keyboard Shortcuts Reference**: Quick guide to all keyboard controls

##### 4. Server Tab

Connection management:

- **Server Address**: Change multiplayer server URL
- **Input Validation**: Ensures valid server address format
- **Connection Note**: Changes apply to next connection

## Settings

**Location**: `RiskyStars.Client/Settings.cs`

Extended settings class with new properties:

```csharp
public class Settings
{
    // Display
    public int ResolutionWidth { get; set; }
    public int ResolutionHeight { get; set; }
    public bool Fullscreen { get; set; }
    public bool VSync { get; set; }
    public int TargetFrameRate { get; set; }
    
    // Audio
    public float MasterVolume { get; set; }
    public float MusicVolume { get; set; }
    public float SfxVolume { get; set; }
    
    // Camera
    public float CameraPanSpeed { get; set; }
    public float CameraZoomSpeed { get; set; }
    public bool InvertCameraZoom { get; set; }
    
    // Debug
    public bool ShowDebugInfo { get; set; }
    public bool ShowFPS { get; set; }
    
    // Server
    public string ServerAddress { get; set; }
}
```

Settings are automatically saved to `settings.json` when Apply is clicked.

## Camera2D Updates

**Location**: `RiskyStars.Client/Camera2D.cs`

Added configurable camera properties:

```csharp
public class Camera2D
{
    public float PanSpeed { get; set; }
    public float ZoomSpeed { get; set; }
}
```

These properties are now runtime-configurable through the settings window.

## Usage

### Opening the Settings Window

**In-Game**: Press `ESC` key
- First press opens the settings window
- Second press closes the settings window

### Applying Settings

1. Make desired changes in any tab
2. Click **Apply** button to save and apply changes
3. Click **Cancel** button to discard changes

### Runtime Effects

When settings are applied:

- **Resolution/Fullscreen Changes**: Graphics device is reconfigured, camera is recreated
- **VSync Changes**: Graphics device sync mode is updated
- **Frame Rate Changes**: Game loop timing is adjusted
- **Camera Speed Changes**: Camera properties are updated immediately
- **Server Address**: Stored for next connection

## Integration with RiskyStarsGame

**Location**: `RiskyStars.Client/RiskyStarsGame.cs`

### Initialization

```csharp
protected override void Initialize()
{
    _settingsWindow = new SettingsWindow(_graphics, _settings, OnSettingsApplied);
    // ...
}
```

### Input Handling

```csharp
if (keyState.IsKeyDown(Keys.Escape) && _previousKeyState.IsKeyUp(Keys.Escape))
{
    if (_settingsWindow?.IsOpen ?? false)
    {
        _settingsWindow.Close();
    }
    else if (_combatEventDialog?.IsOpen ?? false)
    {
        // Combat dialog is open, ESC does nothing
    }
    else
    {
        _settingsWindow?.Open();
    }
}
```

### Settings Application

```csharp
private void OnSettingsApplied(Settings settings)
{
    // Handle resolution changes
    if (resolutionChanged)
    {
        ApplySettings();
        _camera = new Camera2D(_graphics.PreferredBackBufferWidth, 
                              _graphics.PreferredBackBufferHeight);
        _camera.PanSpeed = settings.CameraPanSpeed;
        _camera.ZoomSpeed = settings.CameraZoomSpeed;
    }
    
    // Handle VSync changes
    if (_graphics.SynchronizeWithVerticalRetrace != settings.VSync)
    {
        _graphics.SynchronizeWithVerticalRetrace = settings.VSync;
        _graphics.ApplyChanges();
    }
    
    // Handle frame rate changes
    if (settings.TargetFrameRate > 0)
    {
        TargetElapsedTime = TimeSpan.FromSeconds(1.0 / settings.TargetFrameRate);
        IsFixedTimeStep = true;
    }
    else
    {
        IsFixedTimeStep = false;
    }
    
    // Update camera speeds
    if (_camera != null)
    {
        _camera.PanSpeed = settings.CameraPanSpeed;
        _camera.ZoomSpeed = settings.CameraZoomSpeed;
    }
}
```

### Input Blocking

When the settings window is open, game input is blocked:

```csharp
else if (!(_combatEventDialog?.IsOpen ?? false) && !(_settingsWindow?.IsOpen ?? false))
{
    _camera?.Update(gameTime);
    _inputController?.Update(gameTime);
    // ... other game updates
}
```

### Rendering

Settings window is rendered on top of all other UI:

```csharp
_inGameDesktop?.Render();
_settingsWindow?.Render();
```

## Theme Integration

All UI elements use ThemeManager for consistent styling:

- **Window**: Themed window with cyan accent border
- **Tabs**: Primary text color for tab labels
- **Labels**: Heading, secondary, and default label themes
- **Buttons**: Primary theme for Apply, default for Cancel
- **Sliders**: Standard Myra slider appearance
- **CheckButtons**: Themed checkboxes
- **TextBoxes**: Validated input with error feedback
- **Separators**: Themed horizontal separators

## Keyboard Shortcuts Reference

The Controls tab displays all keyboard shortcuts:

| Key | Action |
|-----|--------|
| ESC | Open Settings / Close Dialogs |
| F1 | Toggle Debug Info Window |
| F2 | Toggle Player Dashboard |
| F3 | Toggle AI Visualization |
| F5 | Toggle Encyclopedia |
| F6 | Toggle Guided Tutorial |
| WASD / Arrows | Pan Camera |
| Mouse Wheel | Zoom Camera |
| Right Mouse Drag | Pan Camera |
| Right Mouse Click | Open Context Menu |
| Shift | Fast Pan Modifier |

## Best Practices

### 1. Initialize in Game Constructor

```csharp
_settingsWindow = new SettingsWindow(_graphics, _settings, OnSettingsApplied);
```

### 2. Check Window State Before Processing Input

```csharp
if (_settingsWindow?.IsOpen ?? false)
{
    // Skip game input processing
    return;
}
```

### 3. Apply Camera Settings on Initialization

```csharp
_camera = new Camera2D(width, height);
_camera.PanSpeed = _settings.CameraPanSpeed;
_camera.ZoomSpeed = _settings.CameraZoomSpeed;
```

### 4. Handle Settings Changes Gracefully

```csharp
private void OnSettingsApplied(Settings settings)
{
    // Check what changed and apply only necessary updates
    // Save settings immediately
    settings.Save();
}
```

## Future Enhancements

Potential future improvements:

1. **Audio System**: Full audio implementation with volume controls
2. **Keybinding Customization**: Allow players to remap controls
3. **Graphics Quality Presets**: Low/Medium/High/Ultra presets
4. **Advanced Graphics**: Anti-aliasing, shadows, particle effects
5. **Accessibility Options**: Colorblind modes, text scaling
6. **Controller Support**: Gamepad configuration
7. **Network Settings**: Connection timeout, retry settings
8. **Language Selection**: Localization support

## Troubleshooting

### Settings Not Saving
- Check file permissions for `settings.json`
- Verify settings directory is writable

### Resolution Change Not Applying
- Ensure graphics device supports selected resolution
- Check display adapter compatibility

### Camera Speeds Not Updating
- Verify OnSettingsApplied callback is connected
- Check that camera instance exists

### Settings Window Not Opening
- Verify initialization in game constructor
- Check ESC key handling in UpdateInGame

## Example Usage

### Basic Setup

```csharp
// In game initialization
_settingsWindow = new SettingsWindow(_graphics, _settings, OnSettingsApplied);

// In game update
if (Keyboard.GetState().IsKeyDown(Keys.Escape))
{
    _settingsWindow.Toggle();
}

// In game draw
_settingsWindow?.Render();
```

### Custom Settings Callback

```csharp
private void OnSettingsApplied(Settings settings)
{
    // Apply graphics settings
    ApplyGraphicsSettings(settings);
    
    // Update camera configuration
    UpdateCameraSettings(settings);
    
    // Notify other systems
    _audioManager?.ApplyVolumeSettings(settings);
    
    // Save to disk
    settings.Save();
}
```
