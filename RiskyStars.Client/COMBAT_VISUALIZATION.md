# Combat Visualization System

## Overview

The `CombatScreen` class provides a full-featured combat visualization UI that displays streamed `CombatEvent` messages from the server. It shows dice roll animations, attacker/defender pairings, casualty updates, and reinforcement arrivals in an engaging and informative way.

## Features

### 1. Combat Event Types
- **COMBAT_INITIATED**: Initial combat screen display
- **COMBAT_ROUND_COMPLETE**: Shows results of each combat round
- **COMBAT_ENDED**: Final results and surviving armies
- **REINFORCEMENTS_ARRIVED**: Special message when reinforcements join combat

### 2. Animated Dice Rolls
- Sequential reveal of attacker and defender dice rolls
- Visual dice representation with color-coding (Red for attackers, Blue for defenders)
- Unrevealed dice show "?" placeholder
- Staggered animation timing (0.1s delay between each die)

### 3. Roll Pairing Visualization
- Shows which attacker rolls paired with which defender rolls
- Color-codes winners (Red for attacker win, Blue for defender win)
- Marks discarded rolls clearly
- Sequential reveal animation

### 4. Casualty Display
- Shows casualties for each army after each round
- Displays remaining unit counts
- Color-coded for impact (OrangeRed for casualties)
- Per-player breakdown with combat role

### 5. Army State Tracking
- Side-by-side display of attackers (left panel, red) and defenders (right panel, blue)
- Real-time unit counts
- Player identification

### 6. Reinforcement Messages
- Special green-bordered message panel
- Highlights when new units arrive at the battlefield

## Animation States

The combat screen uses a state machine to control animation flow:

1. **Idle**: No combat active
2. **ShowingIntro**: Initial display with combat info
3. **RollingDice**: Dice reveal animation
4. **ShowingRolls**: All dice revealed
5. **ShowingPairings**: Roll pairing animation
6. **ShowingCasualties**: Casualty reveal animation
7. **RoundComplete**: Brief pause before next round
8. **ShowingOutro**: Final results screen

## User Controls

- **ENTER/SPACE**: Advance through animation stages or close when complete
- **ESC**: Skip/close combat screen immediately

## Integration

The `CombatScreen` is integrated into `RiskyStarsGame`:

```csharp
// Initialization
_combatScreen = new CombatScreen(GraphicsDevice, screenWidth, screenHeight);

// Load content
_combatScreen.LoadContent(font);

// Update loop - only updates when active
if (_combatScreen.IsActive)
{
    _combatScreen.Update(gameTime);
    
    if (_combatScreen.IsComplete)
    {
        _combatScreen.Close();
    }
}

// Draw loop - renders over main game when active
if (_combatScreen.IsActive)
{
    _combatScreen.Draw(spriteBatch);
}

// Processing combat events from server
if (gameState.CombatEvents.Count > 0)
{
    foreach (var combatEvent in gameState.CombatEvents)
    {
        if (!_combatScreen.IsActive)
        {
            _combatScreen.StartCombat(combatEvent);
            break;
        }
    }
}
```

## Visual Design

### Color Scheme
- **Attackers**: Red theme (DarkRed panels, Red text/borders)
- **Defenders**: Blue theme (DarkBlue panels, Blue text/borders)
- **Winners**: Highlighted in their team color
- **Casualties**: OrangeRed for damaged units
- **Reinforcements**: Green theme
- **Background**: Semi-transparent black overlay (90% opacity)

### Layout
- **Top**: Combat title and location info
- **Upper-Left**: Attacker army states panel
- **Upper-Right**: Defender army states panel
- **Center**: Dice roll visualization area
- **Middle**: Roll pairings panel
- **Bottom**: Casualties panel
- **Final**: Victory/completion overlay

## Performance

- Efficient state-based rendering (only draws relevant UI for current state)
- Timed animations prevent frame-by-frame calculations
- Reuses texture resources (single pixel texture for all rectangles)
- No continuous animation (discrete state transitions)

## Example Combat Flow

1. Server sends `CombatEvent` with type `COMBAT_INITIATED`
2. `CombatScreen.StartCombat()` is called
3. User sees initial combat screen with army states
4. Press SPACE to begin dice rolling
5. Attacker dice reveal one by one (left side)
6. Defender dice reveal one by one (right side)
7. Press SPACE to show pairings
8. Roll pairings appear sequentially with winner highlighting
9. Press SPACE to show casualties
10. Casualty information displays for each army
11. If more rounds exist, cycle repeats from step 4
12. Final screen shows surviving armies
13. Press SPACE to close and return to main game

## Extension Points

The system can be extended with:
- Sound effects for dice rolls, casualties, etc.
- More elaborate dice roll animations (tumbling, spinning)
- Particle effects for combat impacts
- Army/unit portraits instead of text
- Animated transitions between states
- Combat statistics summary screen
