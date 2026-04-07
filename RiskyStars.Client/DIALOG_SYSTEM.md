# Myra Dialog System Documentation

## Overview

The RiskyStars client now uses Myra's Dialog system for displaying error messages, confirmation prompts, and combat event notifications. This replaces custom modal overlays with a consistent, themed dialog system.

## Components

### DialogManager

**Location**: `RiskyStars.Client/DialogManager.cs`

The `DialogManager` class provides a centralized system for showing various types of dialogs.

#### Features
- **Error Dialogs**: Display error messages with red theme
- **Warning Dialogs**: Display warnings with yellow theme
- **Info Dialogs**: Display informational messages
- **Success Dialogs**: Display success messages with green theme
- **Question Dialogs**: Display yes/no questions
- **Confirmation Dialogs**: Display OK/Cancel confirmations
- **Retry Dialogs**: Display retry/cancel options
- **Combat Event Dialogs**: Display combat-related notifications

#### Usage

```csharp
// Initialize (usually in LoadContent)
_desktop = new Desktop();
_dialogManager = new DialogManager(_desktop);

// Show an error dialog
_dialogManager.ShowError("Error Title", "Error message here", (result) =>
{
    // Optional callback when dialog is closed
    Console.WriteLine($"Dialog closed with result: {result}");
});

// Show a confirmation dialog
_dialogManager.ShowConfirmation("Confirm Action", "Are you sure?", (result) =>
{
    if (result == DialogResult.OK)
    {
        // User confirmed
    }
});

// Show a question dialog
_dialogManager.ShowQuestion("Save Changes?", "Would you like to save?", (result) =>
{
    if (result == DialogResult.Yes)
    {
        // Save changes
    }
});

// Update (in game Update method)
_dialogManager.Update();
```

#### Dialog Types

- `DialogType.Error` - Red border, error icon theme
- `DialogType.Warning` - Yellow border, warning theme  
- `DialogType.Success` - Green border, success theme
- `DialogType.Info` - Default cyan border
- `DialogType.Question` - Cyan border for questions
- `DialogType.CombatEvent` - Orange border for combat notifications

#### Dialog Results

- `DialogResult.OK` - User clicked OK
- `DialogResult.Cancel` - User clicked Cancel
- `DialogResult.Yes` - User clicked Yes
- `DialogResult.No` - User clicked No
- `DialogResult.Retry` - User clicked Retry
- `DialogResult.Close` - User clicked Close
- `DialogResult.None` - No result (dialog not closed yet)

### CombatEventDialog

**Location**: `RiskyStars.Client/CombatEventDialog.cs`

Specialized dialog for displaying combat events with formatted information about armies, casualties, and battle outcomes.

#### Features
- **Combat Initiated**: Shows attacking and defending armies
- **Reinforcements Arrived**: Shows reinforcement details
- **Combat Ended**: Shows battle results and survivors

#### Usage

```csharp
// Initialize
_inGameDesktop = new Desktop();
_combatEventDialog = new CombatEventDialog(_inGameDesktop);

// Show combat initiated notification
_combatEventDialog.ShowCombatInitiated(combatEvent, () =>
{
    // Called when user closes the dialog
    _combatScreen?.StartCombat(combatEvent);
});

// Show reinforcements notification
_combatEventDialog.ShowReinforcementsArrived(combatEvent);

// Show combat ended notification
_combatEventDialog.ShowCombatEnded(combatEvent);
```

## Integration Points

### MainMenu

The MainMenu now uses DialogManager instead of a custom error panel:

```csharp
public void ShowError(string message)
{
    _dialogManager?.ShowError("Connection Error", message, (result) =>
    {
        _state = MainMenuState.Main;
        UpdateUI();
    });
}
```

### RiskyStarsGame

The main game class integrates both DialogManager and CombatEventDialog:

```csharp
// In Initialize()
_inGameDesktop = new Desktop();
_inGameDialogManager = new DialogManager(_inGameDesktop);
_combatEventDialog = new CombatEventDialog(_inGameDesktop);

// In UpdateInGame()
_inGameDialogManager?.Update();

// In ProcessGameUpdate()
private void ShowCombatEventNotification(CombatEvent combatEvent)
{
    switch (combatEvent.EventType)
    {
        case CombatEvent.Types.CombatEventType.CombatInitiated:
            _combatEventDialog.ShowCombatInitiated(combatEvent, () =>
            {
                _combatScreen?.StartCombat(combatEvent);
            });
            break;
        // ... other cases
    }
}

// In DrawInGame()
_inGameDesktop?.Render();
```

### SinglePlayerLobbyScreen

Uses DialogManager for error notifications:

```csharp
public void SetError(string errorMessage)
{
    _dialogManager?.ShowError("Game Setup Error", errorMessage);
}
```

### ConnectionManager Error Handling

Connection errors are now displayed through MainMenu's DialogManager:

```csharp
if (_connectionManager?.Status == ConnectionStatus.Error && 
    _connectionManager.ReconnectAttempts >= _connectionManager.MaxAttempts)
{
    var errorMessage = $"Connection lost: {_connectionManager.ErrorMessage}";
    ReturnToMainMenu();
    _mainMenu?.ShowError(errorMessage);
}
```

## Theme Integration

All dialogs use the ThemeManager for consistent styling:

- Title labels use themed heading styles
- Buttons use themed button styles (Primary, Danger, Default)
- Panels use themed accent frame panels
- Border colors match dialog type (Error=Red, Warning=Yellow, etc.)

## Best Practices

### 1. Always Initialize Desktop First

```csharp
_desktop = new Desktop();
_dialogManager = new DialogManager(_desktop);
```

### 2. Call Update in Game Loop

```csharp
_dialogManager?.Update();
```

### 3. Render Desktop After Other Drawing

```csharp
// Draw other game content first
_spriteBatch.Begin();
// ... draw game content
_spriteBatch.End();

// Then render dialogs on top
_desktop?.Render();
```

### 4. Use Callbacks for Actions

```csharp
_dialogManager.ShowConfirmation("Delete Item?", "This cannot be undone", (result) =>
{
    if (result == DialogResult.OK)
    {
        DeleteItem();
    }
});
```

### 5. Check Dialog State Before Other Input

```csharp
if (_combatEventDialog?.IsOpen ?? false)
{
    // Don't process game input while dialog is open
    return;
}
```

## Replacing Custom Modal Overlays

### Before (Custom Modal)
```csharp
// Custom error panel with manual rendering
private Panel? _errorPanel;
private Label? _errorMessageLabel;

private void ShowErrorUI()
{
    if (_errorMessageLabel != null)
        _errorMessageLabel.Text = _errorMessage;
    if (_desktop != null)
        _desktop.Root = _errorPanel;
}
```

### After (DialogManager)
```csharp
// Clean dialog-based approach
public void ShowError(string message)
{
    _dialogManager?.ShowError("Error", message);
}
```

## Future Enhancements

Potential future improvements to the dialog system:

1. **Custom Dialog Templates**: Create reusable dialog templates for common scenarios
2. **Dialog Queuing**: Queue multiple dialogs to show in sequence
3. **Input Dialogs**: Add text input dialogs for user prompts
4. **List Selection Dialogs**: Show dialogs with selectable lists
5. **Progress Dialogs**: Non-modal progress indicators
6. **Tooltip Dialogs**: Small contextual information popups
7. **Animated Transitions**: Fade in/out animations for dialogs
8. **Sound Effects**: Audio cues when dialogs open/close

## Troubleshooting

### Dialog Not Showing
- Ensure Desktop is initialized
- Verify DialogManager.Update() is called
- Check that Desktop.Render() is called after drawing

### Dialog Behind Other Elements
- Ensure Desktop.Render() is called last in Draw method
- Check z-ordering of drawing calls

### Callback Not Firing
- Verify callback is not null
- Check that dialog is actually closed (not just hidden)
- Ensure no exceptions in callback code
