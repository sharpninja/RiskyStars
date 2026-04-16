# AI Action Visualization System

## Overview

The AI action visualization system provides visual feedback to players when AI opponents take their turns. This includes:

1. **AI Thinking Indicator**: A spinner overlay showing when an AI player is processing their turn
2. **Reinforcement Highlights**: Pulsing circles that highlight locations where AI places reinforcements
3. **Army Movement Animations**: Animated movement of armies from source to destination with camera tracking
4. **Game Log**: On-screen notifications of AI actions (purchases, reinforcements, movements, captures)

![AI Thinking Indicator](screenshots/ai_thinking.png)

## Components

### AIActionIndicator

The `AIActionIndicator` class is responsible for rendering all AI action visualizations.

#### Features:

- **Thinking Indicator**: Displays a centered panel with the AI player's name and an animated spinner
- **Reinforcement Highlights**: Shows pulsing circular highlights at locations receiving reinforcements
  - Duration: 2 seconds
  - Visual: Dual pulsing circles that fade out

![AI Reinforcement Highlight](screenshots/ai_reinforcement.png)

- **Movement Animations**: Animates armies moving between locations
  - Duration: 1.5 seconds
  - Shows moving circle with unit count
  - Displays path line between start and end positions

![AI Army Movement](screenshots/ai_movement.png)

- **Game Log**: Bottom-left overlay showing recent AI actions
  - Maximum 5 entries
  - 5-second lifetime with 1-second fade-out
  - Color-coded by player

![AI Game Log](screenshots/ai_game_log.png)

#### Methods:

```csharp
// Start/stop AI thinking indicator
void StartAIThinking(string aiPlayerName)
void StopAIThinking()

// Show visualizations
void ShowReinforcement(string locationId, LocationType locationType, int unitCount, Color playerColor)
void ShowArmyMovement(Vector2 startPosition, Vector2 endPosition, int unitCount, Color playerColor, string? armyId = null)

// Add log entries
void AddLogEntry(string message, Color color)

// Camera tracking
void TrackArmyMovement(Camera2D camera, ArmyMovementAnimation animation)
bool HasActiveMovementAnimations()
ArmyMovementAnimation? GetFirstMovementAnimation()
```

### AIActionTracker

The `AIActionTracker` class monitors game state updates and detects AI actions to trigger visualizations.

#### Detection Logic:

1. **Purchase Detection**: Compares resource stockpiles between states
   - Detects when total resources decrease
   - Calculates armies purchased (cost: 3 resources per army)
   
2. **Reinforcement Detection**: Tracks new armies and unit count increases
   - New armies at locations trigger reinforcement highlights
   - Existing armies gaining units show reinforcement
   
3. **Movement Detection**: Compares army locations between updates
   - LocationId or LocationType changes indicate movement
   - Triggers movement animation and log entry
   
4. **Ownership Detection**: Tracks region and hyperspace lane mouth ownership changes
   - New ownership by AI triggers capture notification

#### Methods:

```csharp
void ProcessGameUpdate(GameUpdate update, string? currentPlayerId)
```

### Camera2D Enhancements

Enhanced camera with smooth tracking support:

```csharp
// Instant centering
void CenterOn(Vector2 position)

// Smooth interpolated centering (for AI movement tracking)
void SmoothCenterOn(Vector2 position)
```

#### Tracking Behavior:

- User input (keyboard, mouse) cancels automatic tracking
- Smooth interpolation factor: 0.1 (10% per frame)
- Tracking stops when within 1 pixel of target

## Integration

### In RiskyStarsGame.cs:

```csharp
// Initialization
_aiActionIndicator = new AIActionIndicator(GraphicsDevice, screenWidth, screenHeight);
_aiActionTracker = new AIActionTracker(_aiActionIndicator, _mapData, _gameStateCache, _regionRenderer);

// Update loop
_aiActionIndicator?.Update(gameTime, _gameStateCache, _mapData, _currentPlayerId);

// Process game updates
_aiActionTracker?.ProcessGameUpdate(update, _currentPlayerId);

// Camera tracking
var activeAnimation = _aiActionIndicator.GetFirstMovementAnimation();
if (activeAnimation != null)
{
    _aiActionIndicator.TrackArmyMovement(_camera, activeAnimation);
}

// Rendering
_aiActionIndicator?.Draw(spriteBatch, _camera, _mapData);
```

## Visual Design

### Colors

Player colors are shared with the RegionRenderer to maintain consistency:
- Red, Blue, Green, Yellow, Purple, Cyan (cycling)

### Positioning

- **AI Thinking Indicator**: Top-center (60px from top)
- **Game Log**: Bottom-left corner (200px from bottom)
- **Reinforcement Highlights**: At location positions (world space)
- **Movement Animations**: Between start and end positions (world space)

### Timing

- **Thinking Spinner**: 3 rotations per second
- **Reinforcement Pulse**: 4 pulses over 2 seconds
- **Movement Animation**: 1.5 seconds total
- **Log Entry Display**: 5 seconds (with 1-second fade)
- **Camera Tracking**: During first 80% of movement animation

![AI Camera Tracking](screenshots/ai_camera_tracking.png)

## Usage Examples

### Detecting and Visualizing a Purchase

When the AIActionTracker detects a resource decrease:

```
AI Player resource total: 15 → 6
Resources spent: 9
Armies purchased: 3
Log entry: "AI Player purchased 3 armies"
```

### Detecting and Visualizing a Reinforcement

When a new army appears or units are added:

```
New army at Region_A with 5 units
Highlight appears at Region_A position
Log entry: "AI Player reinforced with 5 units"
```

### Detecting and Visualizing Movement

When an army changes location:

```
Army moved from Region_A to Region_B
Animation shows movement path
Camera smoothly tracks the movement
Log entry: "AI Player moved 3 units: North Sector → South Sector"
```

## Performance Considerations

- Uses sprite batching for efficient rendering
- Movement animations are culled when complete
- Log entries automatically expire and are removed
- Previous game states are replaced (not accumulated)

## Future Enhancements

Potential improvements:
- Sound effects for AI actions
- More detailed combat result visualizations
- Player preference for tracking speed/enable/disable
- Replay mode to review AI actions
- Multiple simultaneous movement animations

## Screenshots Needed

The following screenshots are required for complete documentation:

1. **AI Thinking Indicator** (`screenshots/ai_thinking.png`)
   - Capture: Top-center thinking indicator with AI player name and animated spinner
   - Requirements: AI turn active, StartAIThinking() called, show full panel

2. **AI Reinforcement Highlight** (`screenshots/ai_reinforcement.png`)
   - Capture: Pulsing circular highlight at location where AI placed reinforcements
   - Requirements: AI reinforcement action detected, during 2-second animation, dual circles visible

3. **AI Army Movement** (`screenshots/ai_movement.png`)
   - Capture: Army movement animation showing moving circle with unit count and path line
   - Requirements: AI movement action detected, mid-animation (0.5-1.0 seconds), clear path visible

4. **AI Game Log** (`screenshots/ai_game_log.png`)
   - Capture: Bottom-left game log showing multiple AI action entries with color coding
   - Requirements: Multiple AI actions logged, at least 3-4 entries visible, show variety (purchase, movement, capture)

5. **AI Camera Tracking** (`screenshots/ai_camera_tracking.png`)
   - Capture: Camera following AI army movement, show map centered on moving unit
   - Requirements: AI movement animation active, camera tracking enabled, clear center position

6. **AI Capture Notification** (`screenshots/ai_capture.png`)
   - Capture: Game log entry showing AI captured a region, with ownership change visible on map
   - Requirements: AI capture action, log entry and map ownership both visible in screenshot
