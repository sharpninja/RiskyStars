# Player Dashboard

The Player Dashboard is a comprehensive UI system in the RiskyStars client that provides resource management, army purchasing, and hero assignment interfaces.

## Overview

The dashboard is rendered on the right side of the screen and consists of three main panels:
1. **Resource Stockpiles Panel** - Displays current resources and production rates
2. **Army Purchase Panel** - Interactive buttons to purchase armies
3. **Hero Assignment Panel** - Controls for managing heroes (coming soon)

![Player Dashboard Overview](screenshots/player_dashboard_overview.png)

## Architecture

### PlayerDashboard Class

Located in `RiskyStars.Client/PlayerDashboard.cs`, this class manages all dashboard rendering and interactions.

**Key Components:**
- `_purchaseButtons` - List of interactive buttons for purchasing armies
- `_heroButtons` - List of interactive buttons for hero management
- `_currentPlayerId` - Tracks the current player for resource display
- `_lastKnownGameId` - Cached game ID for sending purchase actions
- `IsVisible` - Controls dashboard visibility (toggle with F2)

### Integration

The dashboard is integrated into the main game loop:
- **Initialize**: Created in `RiskyStarsGame.Initialize()`
- **LoadContent**: Font loaded in `RiskyStarsGame.LoadContent()`
- **Update**: Called in `RiskyStarsGame.Update()` with GameStateCache
- **Draw**: Rendered in `RiskyStarsGame.Draw()` with GameStateCache

## Panels

### 1. Resource Stockpiles Panel

**Location**: Top-right corner (width: 300px, height: 200px)

**Displays:**
- Player name
- Population stockpile + production rate per turn
- Metal stockpile + production rate per turn
- Fuel stockpile + production rate per turn
- Territory count (owned regions)
- Army count (owned armies)

**Visual Design:**
- Semi-transparent black background (85% opacity)
- Cyan border for emphasis
- Color-coded resource icons:
  - Population: Green (100, 200, 100)
  - Metal: Gray (180, 180, 180)
  - Fuel: Orange (220, 160, 80)
- Production rates displayed in light green if positive

**Data Source**: `GameStateCache.GetPlayerState()`, `GameStateCache.GetProductionRate()`

![Resource Panel](screenshots/player_dashboard_resources.png)

### 2. Army Purchase Panel

**Location**: Middle-right (width: 300px, height: 150px)

**Features:**
- Display of army cost: 1 Population, 3 Metal, 1 Fuel
- Four purchase buttons:
  - **Buy 1**: Purchase 1 army
  - **Buy 5**: Purchase 5 armies
  - **Buy 10**: Purchase 10 armies
  - **Buy 25**: Purchase 25 armies

**Button Interactions:**
- Buttons highlight on hover (lighter blue)
- Buttons are disabled (gray) if insufficient resources
- Click to purchase immediately
- Hover displays cost tooltip showing:
  - Required resources for purchase
  - Current available resources
  - Red text if insufficient for that resource
  - Green/white text if sufficient

**Purchase Phase Check:**
- Only functional during the Purchase phase
- Orange warning message displayed during other phases

**Actions**: Sends `PurchaseArmiesAction` to server via `GrpcGameClient.SendPurchaseArmiesAsync()`

![Army Purchase Panel](screenshots/player_dashboard_purchase.png)

![Purchase Tooltip](screenshots/player_dashboard_purchase_tooltip.png)

### 3. Hero Assignment Panel

**Location**: Bottom-right (width: 300px, height: 180px)

**Status**: Coming Soon (placeholder functionality)

**Planned Features:**
- Assign hero to specific army
- Assign hero to specific region
- Recall hero from assignment

**Current Display:**
- Purple-themed panel
- "No heroes available" message
- Three disabled buttons for future functionality

**Visual Design:**
- Purple border and header (180, 100, 200)
- Gray disabled buttons
- Placeholder text indicating upcoming feature

![Hero Assignment Panel](screenshots/player_dashboard_heroes.png)

## User Interactions

### Mouse Input

**Button Clicks:**
- Left-click on purchase buttons to buy armies
- Hover over buttons to see tooltips with cost breakdown
- Buttons provide visual feedback (hover state)

**State Management:**
- Buttons are enabled/disabled based on:
  - Current turn phase (Purchase phase for army buttons)
  - Available resources (checked against button cost)
  - Game connection status

### Keyboard Shortcuts

- **F2**: Toggle dashboard visibility on/off
- **B** or **1**: Quick purchase 1 army (via InputController)
- **5**: Quick purchase 5 armies (via InputController)
- **0**: Quick purchase 10 armies (via InputController)

## Resource Management

### Resource Costs

Army purchase costs are defined in `GetArmyCost()`:
```csharp
Population: count * 1
Metal: count * 3
Fuel: count * 1
```

### Affordability Checking

The `CanAfford()` method validates if a player has sufficient resources:
```csharp
playerState.PopulationStockpile >= cost.Population &&
playerState.MetalStockpile >= cost.Metal &&
playerState.FuelStockpile >= cost.Fuel
```

### Production Rates

Production rates are calculated in `GameStateCache.GetProductionRate()`:
- **Population**: 2 per region owned per turn
- **Metal**: 1 per region owned per turn
- **Fuel**: 1 per region owned per turn

*Note: These are client-side estimates. Actual production is handled by the server.*

## Visual Design

### Color Scheme

**Panel Backgrounds:**
- Semi-transparent black: `Color.Black * 0.85f`

**Panel Borders:**
- Resource Panel: Cyan (100, 180, 255)
- Purchase Panel: Cyan (100, 180, 255)
- Hero Panel: Purple (180, 100, 200)

**Button Colors:**
- Purchase buttons: Blue (60, 120, 180) / Hover: Light Blue (80, 150, 220)
- Hero buttons: Purple (120, 60, 140) / Hover: Light Purple (150, 80, 170)
- Recall button: Red (140, 60, 60) / Hover: Light Red (170, 80, 80)
- Disabled: Dark Gray

**Text Colors:**
- Headers: Themed per panel (cyan/purple)
- Primary text: White
- Secondary text: Light Gray
- Warning text: Orange
- Error/Insufficient: Red
- Success/Sufficient: Green

### Layout

All panels are positioned on the right side of the screen with consistent spacing:
- Panel width: 300px
- Right margin: 10px from screen edge
- Vertical spacing: 10px between panels

## Network Communication

### Purchase Actions

When a purchase button is clicked:

1. Validate player ID and game ID
2. Create async task to send purchase action
3. Call `GrpcGameClient.SendPurchaseArmiesAsync()`
4. Server processes `PurchaseArmiesAction`
5. Server sends back updated `GameState` with new army and resources
6. Client updates `GameStateCache`
7. Dashboard reflects new values on next frame

### Error Handling

Purchase attempts that fail (network error, server rejection) are logged to console:
```csharp
catch (Exception ex)
{
    Console.WriteLine($"Failed to purchase armies: {ex.Message}");
}
```

## Future Enhancements

### Hero System

When heroes are implemented in the proto definitions:
1. Add `HeroState` to `turn_based_game.proto`
2. Update `GameStateCache` to track hero assignments
3. Implement hero assignment actions in `game.proto`
4. Enable hero buttons in dashboard
5. Display hero stats and abilities
6. Add hero-specific tooltips

### Production Rate Server Sync

Currently production rates are estimated client-side. Future enhancement:
1. Add production rate fields to `PlayerState` in proto
2. Server calculates actual production rates
3. Client displays server-provided rates
4. Show detailed production breakdown tooltip

### Resource Breakdown Tooltip

Add detailed tooltip when hovering over resources:
- Base production per region
- Bonuses from buildings/heroes
- Consumption from armies/maintenance
- Net production rate

### Queue System

Add visual queue for reinforcement placement:
- Show pending armies awaiting deployment
- Drag-and-drop assignment to regions
- Visual indicators on map for queued reinforcements

### Cost Preview

When hovering over purchase buttons:
- Show resource bars with preview of post-purchase amounts
- Animate resource deduction
- Show warning if purchase would leave resources too low

## Testing Notes

### Manual Testing Checklist

1. **Resource Display**
   - [ ] Resources update when game state changes
   - [ ] Production rates calculate correctly
   - [ ] Territory and army counts are accurate

2. **Purchase Buttons**
   - [ ] Buttons enable/disable based on resources
   - [ ] Hover tooltips show correct costs
   - [ ] Clicks send purchase actions to server
   - [ ] Multiple purchases work correctly

3. **Phase Checks**
   - [ ] Purchase buttons work during Purchase phase
   - [ ] Warning message shows during other phases
   - [ ] Phase changes update UI correctly

4. **Visual Feedback**
   - [ ] Hover states work on all buttons
   - [ ] Disabled buttons appear grayed out
   - [ ] Tooltips position correctly and are readable
   - [ ] No UI overlap with other panels

5. **Keyboard Integration**
   - [ ] F2 toggles dashboard visibility
   - [ ] Quick purchase shortcuts work (B, 1, 5, 0)
   - [ ] Dashboard state persists across toggle

## Dependencies

**Required Classes:**
- `GameStateCache` - Provides game state and resource data
- `GrpcGameClient` - Sends purchase actions to server
- `Microsoft.Xna.Framework` - Graphics and input handling
- `RiskyStars.Shared` - Proto-generated types

**Proto Messages Used:**
- `PlayerState` - Resource stockpiles and player info
- `PurchaseArmiesAction` - Army purchase request
- `TurnPhase` - Current game phase
- `GameUpdate` - Server state updates

## Performance Considerations

- Dashboard updates only when visible
- Button state recalculated per frame (negligible cost)
- Tooltip rendering only on hover
- Resource queries use cached GameStateCache (thread-safe locks)
- Async purchases don't block main game thread

## Screenshots Needed

The following screenshots are required for complete documentation:

1. **Player Dashboard Overview** (`screenshots/player_dashboard_overview.png`)
   - Capture: Full dashboard showing all three panels (resources, purchase, heroes) on right side of screen
   - Requirements: In-game, dashboard visible (F2 if hidden), sufficient resources to enable buttons

2. **Resource Panel** (`screenshots/player_dashboard_resources.png`)
   - Capture: Close-up of resource stockpiles panel showing population, metal, fuel with production rates
   - Requirements: In-game with positive production rates, multiple territories owned

3. **Army Purchase Panel** (`screenshots/player_dashboard_purchase.png`)
   - Capture: Purchase panel during Purchase phase with enabled buttons
   - Requirements: Purchase phase active, sufficient resources to show enabled state

4. **Purchase Tooltip** (`screenshots/player_dashboard_purchase_tooltip.png`)
   - Capture: Mouse hovering over a purchase button showing cost breakdown tooltip
   - Requirements: Hover over "Buy 5" or "Buy 10" button to show detailed cost display

5. **Hero Assignment Panel** (`screenshots/player_dashboard_heroes.png`)
   - Capture: Hero panel showing placeholder state with disabled buttons
   - Requirements: In-game, dashboard visible

6. **Phase Warning** (`screenshots/player_dashboard_phase_warning.png`)
   - Capture: Purchase panel during non-Purchase phase showing orange warning message
   - Requirements: Switch to Movement, Combat, or Production phase to display warning
