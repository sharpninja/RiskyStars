# Context Menu System

The RiskyStars client implements a comprehensive context menu system using Myra.Menu for right-click interactions with game objects. The system provides context-aware actions based on ownership and game state.

## Overview

The `ContextMenuManager` class handles all context menu operations, providing different menus for:
- Armies
- Regions
- Stellar Bodies
- Hyperspace Lane Mouths

## Features

### Army Context Menu

**Own Armies:**
- View Info - Display army information dialog
- Move Army - Initiate movement (closes menu for destination selection)
- Split Army - Split army into two separate armies (if unit count > 1)
- Merge with Army - Merge with another army at the same location
- Assign Hero - Assign a hero to lead the army

**Enemy Armies:**
- View Info - Display basic army information
- Diplomacy - Open diplomacy menu with army owner (see Diplomacy Menu below)

### Region Context Menu

- View Info - Display region information (name, owner, armies, units)
- Reinforce Location - Deploy units from stockpile (owned regions only)
- Diplomacy - Open diplomacy menu with region owner (enemy regions only)
- Merge All Armies - Merge all player armies at this location into one

### Stellar Body Context Menu

- View Info - Display stellar body information (type, regions, ownership)
- Upgrade Stellar Body - Access upgrade options (owned regions required)
  - Production Facility
  - Defense Station
  - Research Lab
  - Shipyard

### Hyperspace Lane Mouth Context Menu

- View Info - Display lane information (connected systems, owners)
- Reinforce Portal - Deploy units to defend the portal (owned portals only)
- Merge All Armies - Merge all player armies at this portal into one

## Usage

### Opening Context Menus

Context menus are opened via right-click on game objects. The `InputController` integrates with `ContextMenuManager` to handle right-click events:

```csharp
// In RiskyStarsGame.cs initialization
_contextMenuManager = new ContextMenuManager(gameClient, _gameStateCache, _mapData, _camera, _inGameDesktop);
_inputController.SetContextMenuManager(_contextMenuManager);

// Set current player for ownership checks
_contextMenuManager?.SetCurrentPlayer(_currentPlayerId);
```

The context menu manager automatically:
1. Detects the object under the cursor
2. Checks ownership and game state
3. Builds appropriate menu items
4. Positions menu at cursor location

### Closing Context Menus

Context menus close automatically when:
- User clicks outside the menu
- User presses ESC key
- User selects a menu item
- User left-clicks anywhere

The ESC key handling in `RiskyStarsGame.cs`:

```csharp
if (keyState.IsKeyDown(Keys.Escape) && _previousKeyState.IsKeyUp(Keys.Escape))
{
    if (_contextMenuManager != null && _contextMenuManager.IsMenuOpen)
    {
        _contextMenuManager.CloseContextMenu();
    }
    // ... other ESC handling
}
```

## Action Dialogs

Each context menu action that requires user input opens a themed dialog:

### Split Army Dialog

Interactive dialog with:
- Spinner to select number of units to split off
- Real-time display of remaining units
- Confirm/Cancel buttons

### Merge Armies Dialog

Selection dialog with:
- List of available armies at the location
- Buttons to select target army for merge
- Unit counts displayed for each option

### Merge All Armies Dialog

Confirmation dialog showing:
- Total number of armies to merge
- Combined unit count
- Confirm/Cancel buttons

### Assign Hero Dialog

Selection dialog with:
- List of available heroes
- Hero class information
- Themed with hero colors

### Upgrade Stellar Body Dialog

Selection dialog with:
- List of available upgrades
- Cost and benefit information
- Multiple upgrade options

### Reinforce Location Dialog

Input dialog with:
- Spinner to select unit count
- Location name display
- Confirm/Cancel buttons

### Diplomacy Dialog

Interactive dialog for diplomatic actions with another player:
- Propose Alliance - Send alliance proposal to target player
- Break Alliance - End existing alliance
- View Player Info - Display detailed player statistics

### Player Info Dialog

Comprehensive player information including:
- Turn order
- Resource stockpiles (Population, Metal, Fuel)
- Territory control (Regions, Lane Mouths)
- Military strength (Armies, Total Units)

## Menu Styling

Context menus use `Myra.Menu` with ThemeManager styling:

```csharp
_activeMenu = new Menu
{
    Left = (int)screenPosition.X,
    Top = (int)screenPosition.Y
};

_activeMenu.Background = CreateSolidBrush(Colors.BackgroundDark);
_activeMenu.Border = CreateSolidBrush(Colors.BorderNormal);
_activeMenu.BorderThickness = new Thickness(BorderThickness.Normal);
_activeMenu.Padding = Padding.Small;
```

Menu items include:
- **Headers** - Cyan colored labels for context (e.g., army ID, region name)
- **Separators** - Visual dividers between menu sections
- **Actions** - Interactive menu items with hover effects

## Backend Integration

### Implemented Actions

Currently implemented with full gRPC integration:
- Reinforce Location - Uses `SendReinforceLocationAsync`

### Placeholder Actions

The following actions have UI dialogs ready but require server-side implementation:
- Split Army
- Merge Armies
- Merge All Armies
- Assign Hero
- Upgrade Stellar Body
- Form Alliance
- Break Alliance

These actions currently log to console and are ready for backend integration when server APIs are available.

## Object Detection

The context menu manager uses spatial queries to find objects at click positions:

```csharp
// Detection radii
Army detection: 15 units
Region detection: 10 units
Hyperspace Lane Mouth detection: 12 units
Stellar Body detection: Variable by type
  - Gas Giant: 20 units
  - Rocky Planet: 15 units
  - Planetoid: 8 units
  - Comet: 6 units
```

## Context-Aware Behavior

Menu items are filtered based on:
- **Ownership** - Only show relevant actions for owned objects
- **Game State** - Enable/disable based on current turn phase
- **Object State** - Adapt to army count, unit count, etc.
- **Location** - Check for other armies at same location

## Future Enhancements

Planned features for the context menu system:
1. ✓ Form Alliance - Propose alliance with another player (IMPLEMENTED)
2. ✓ Break Alliance - End existing alliance (IMPLEMENTED)
3. Transfer Units - Transfer units between armies
4. Disband Army - Remove empty or unwanted armies
5. Set Rally Point - Automated reinforcement routing
6. Attack Move - Aggressive movement orders
7. Patrol Route - Set automated patrol paths
8. Trade Resources - Offer resource trades to allies
9. Request Assistance - Ask allies for military support

## Technical Details

### Dependencies

- **Myra.Menu** - Core menu widget system
- **ThemeManager** - Consistent styling
- **ThemedUIFactory** - Dialog creation
- **GrpcGameClient** - Server communication
- **GameStateCache** - Game state queries
- **MapData** - Spatial data for object lookup

### Performance

- Menus are created on-demand, not cached
- Object detection uses simple distance checks
- Dialogs use Myra's modal system for efficiency
- Menu cleanup is automatic via Desktop widget management

## Examples

### Basic Usage

```csharp
// Right-click handler in InputController
if (mouseState.RightButton == ButtonState.Pressed && _previousMouseState.RightButton == ButtonState.Released)
{
    var screenPosition = new Vector2(mouseState.X, mouseState.Y);
    var worldPosition = _camera.ScreenToWorld(screenPosition);
    
    if (_contextMenuManager != null)
    {
        _contextMenuManager.OpenContextMenu(screenPosition, worldPosition, _selectionState);
    }
}
```

### Adding New Context Menu Actions

```csharp
// In OpenArmyContextMenu
items.Add(CreateMenuItem("New Action", () =>
{
    CloseContextMenu();
    ShowNewActionDialog(army);
}));

// Create corresponding dialog
private void ShowNewActionDialog(ArmyState army)
{
    var dialog = new Dialog();
    // ... dialog setup
    dialog.ShowModal(_desktop);
}

// Add server command
private void SendNewActionCommand(string armyId, params...)
{
    Task.Run(async () =>
    {
        try
        {
            await _gameClient.SendNewActionAsync(...);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Failed: {ex.Message}");
        }
    });
}
```

## Best Practices

1. **Always close menu before showing dialog** - Prevents menu/dialog overlap
2. **Use themed dialogs** - Maintain consistent UI appearance
3. **Validate actions** - Check ownership and game state before enabling
4. **Provide feedback** - Show confirmation dialogs for destructive actions
5. **Handle errors gracefully** - Catch and log server communication errors
6. **Keep menus concise** - Only show relevant actions
7. **Use clear labels** - Action names should be self-explanatory
