# Input System Documentation

## Overview

The RiskyStars client implements a comprehensive input handling system that processes mouse clicks, keyboard shortcuts, and sends player actions to the server via gRPC.

## Components

### InputController

The main input handling class that:
- Processes mouse input for selection and movement commands
- Handles keyboard shortcuts for game actions
- Captures player actions and sends them via `GrpcGameClient`
- Manages selection state

### SelectionRenderer

Provides visual feedback for selected objects with:
- Pulsing animation effect
- Different visual styles for each object type
- Color-coded highlights

### SelectionState

Tracks the currently selected object, which can be:
- Army unit
- Region
- Hyperspace lane mouth
- Stellar body
- Star system

## Mouse Controls

### Left Click
- **Select Army**: Click on an army to select it (prioritizes player's own armies)
- **Select Region**: Click on a region marker to select it
- **Select Hyperspace Lane Mouth**: Click on a lane mouth to select it
- **Select Stellar Body**: Click on a planet/body to select it
- **Select Star System**: Click on a star system to select it
- **Clear Selection**: Click on empty space to clear selection

### Right Click
- **Move Army**: Right-click on a destination while an army is selected to issue a move command
- Can target regions or hyperspace lane mouths
- Only works when an army is selected

### Middle Mouse
- **Pan Camera**: Hold middle mouse button and drag to pan the camera (handled by Camera2D)

### Mouse Wheel
- **Zoom**: Scroll to zoom in/out (handled by Camera2D)

## Keyboard Shortcuts

### Selection and Navigation
- **Tab**: Cycle through your armies
- **C**: Center camera on currently selected object
- **Esc**: Clear selection

### Game Actions
- **Space**: Advance to next phase
- **P**: Produce resources
- **B** or **1**: Purchase 1 army
- **5**: Purchase 5 armies
- **0**: Purchase 10 armies (0 key)
- **R**: Reinforce selected location with 1 unit

### UI Controls
- **H**: Toggle keyboard shortcuts help panel
- **F1**: Toggle debug info
- **WASD/Arrow Keys**: Pan camera (handled by Camera2D)
- **Shift + Movement**: Fast pan (handled by Camera2D)
- **+/-**: Zoom in/out (handled by Camera2D)

## Click Detection

The system uses distance-based hit detection with the following radii:

| Object Type | Detection Radius |
|-------------|------------------|
| Army (at region) | 15 units |
| Army (at lane mouth) | 15 units |
| Region marker | 10 units |
| Hyperspace lane mouth | 12 units |
| Gas Giant | 20 units |
| Rocky Planet | 15 units |
| Planetoid | 8 units |
| Comet | 6 units |
| Star System | 80 units |

## Selection Priority

When clicking, the system checks for objects in this order:
1. Armies (player's armies prioritized)
2. Regions
3. Hyperspace lane mouths
4. Stellar bodies
5. Star systems

## gRPC Commands

The InputController sends the following commands to the server:

### MoveArmyAction
```csharp
await _gameClient.SendMoveArmyAsync(playerId, armyId, targetLocationId, targetLocationType);
```

### ProduceResourcesAction
```csharp
await _gameClient.SendProduceResourcesAsync(playerId, gameId);
```

### PurchaseArmiesAction
```csharp
await _gameClient.SendPurchaseArmiesAsync(playerId, gameId, count);
```

### ReinforceLocationAction
```csharp
await _gameClient.SendReinforceLocationAsync(playerId, gameId, locationId, locationType, unitCount);
```

### AdvancePhaseAction
```csharp
await _gameClient.SendAdvancePhaseAsync(playerId, gameId);
```

## Visual Feedback

### Selection Highlights
- **Army**: Large pulsing circle (18 unit radius, white)
- **Region**: Medium pulsing circle (12 unit radius, white)
- **Hyperspace Lane Mouth**: Pulsing square (14 unit size, white)
- **Stellar Body**: Circle matching body size + 5 units (white)
- **Star System**: Large pulsing circle (85 unit radius, white)

### Selection Info Panel
Displayed at bottom center when an object is selected:
- Shows object type and properties
- Army: ID, owner, unit count, location, status
- Region: Name, owner, defense value
- Hyperspace Lane Mouth: Owner
- Stellar Body: Name, type, region count
- Star System: Name, type, body count

## Error Handling

All gRPC commands are executed asynchronously with error handling:
- Commands run on background threads using `Task.Run`
- Exceptions are caught and logged to console
- Failed commands don't block the UI

## Integration Points

### RiskyStarsGame
- Creates InputController during Initialize()
- Updates InputController every frame
- Sets current player ID when connection status changes

### Camera2D
- InputController uses camera for screen-to-world coordinate conversion
- Can command camera to center on selected objects

### GameStateCache
- InputController reads game state for validation
- Checks for army ownership, locations, etc.

### MapData
- InputController uses map data to find objects at click positions
- Looks up positions for army locations

## Future Enhancements

Potential improvements:
- Multi-select (shift-click for multiple armies)
- Selection boxes (drag to select multiple units)
- Command queuing (hold shift for waypoints)
- Context menus (right-click for options)
- Hotkey groups (Ctrl+1-9 to create groups)
- Double-click to select all armies at location
- Smart targeting (auto-select nearest valid target)
