# RiskyStars Rendering System

## Overview

The MonoGame rendering system consists of map/world renderers plus a Myra gameplay HUD:

1. **MapRenderer** - Displays star systems, stellar bodies, and hyperspace lanes
2. **RegionRenderer** - Shows ownership indicators and army counts for regions and hyperspace lane mouths
3. **GameplayHudOverlay** - Myra-based gameplay HUD for status, resources, legend, selection details, and help

## Camera Controls

The `Camera2D` class provides full camera control for navigating the map:

### Keyboard Controls
- **W/Up Arrow** - Pan camera up
- **S/Down Arrow** - Pan camera down
- **A/Left Arrow** - Pan camera left
- **D/Right Arrow** - Pan camera right
- **Shift + Movement** - Fast pan (3x speed)
- **+/=** - Zoom in
- **-** - Zoom out

### Mouse Controls
- **Right Mouse Button (Hold & Drag)** - Pan camera
- **Right Mouse Button (Click)** - Open context menu
- **Scroll Wheel** - Zoom in/out

### Debug Controls
- **F1** - Toggle debug overlay (camera position and zoom level)
- **Escape** - Exit game

## Components

### MapRenderer

Displays the strategic map with:
- Star systems (circles colored by type: Yellow=Home, Orange=Featured, Gray=Minor)
- Stellar bodies (colored circles by type)
  - Gas Giants (brown)
  - Rocky Planets (blue)
  - Planetoids (gray)
  - Comets (light blue)
- Hyperspace lanes (gray lines connecting systems)
- Region markers (small circles on stellar bodies)
- System and body labels

### RegionRenderer

Displays dynamic game state:
- Region ownership (colored circles matching player color)
- Hyperspace lane mouth ownership (colored squares)
- Army counts at regions (text labels with player color)
- Multiple armies at same location (stacked display)
- Player color assignment (Red, Blue, Green, Yellow, Purple, Cyan)

### GameplayHudOverlay

Displays gameplay HUD elements through Myra:
- **Top Bar**: Turn number, current phase, current player, event messages
- **Resource chips**: Population/Metal/Fuel stockpiles with production deltas
- **Map Key**: Always-visible legend for orbit/system/body markers
- **Selection Panel**: Current army/system/body/region details
- **AI Activity Panel**: Current AI turn plus recent AI action log
- **Help Overlay**: Keyboard/mouse shortcut reference

## MapData Structure

The map is defined using the following data structures:

- `MapData` - Top-level container with star systems and hyperspace lanes
- `StarSystemData` - Individual star system with position, type, and stellar bodies
- `StellarBodyData` - Planets, gas giants, etc. with regions
- `RegionData` - Individual controllable regions
- `HyperspaceLaneData` - Connections between systems with mouth positions

## Coordinate System

- Origin (0,0) is at the center of the featured system
- Positive X is to the right, positive Y is down
- Units are in world space (not pixels)
- Camera transforms world coordinates to screen coordinates

## Rendering Order

1. Map background
2. Hyperspace lanes
3. Star systems and stellar bodies
4. Region ownership indicators
5. Army count displays
6. Myra gameplay HUD and docked panels
7. Debug overlay

## Integration with Game State

The renderers read from `GameStateCache` which is updated via gRPC:
- Region ownership via `RegionOwnership` messages
- Hyperspace lane mouth ownership via `HyperspaceLaneMouthOwnership` messages
- Army positions via `ArmyState` messages
- Player resources via `PlayerState` messages
- Turn/phase info from `TurnBasedGameStateUpdate` messages

## Extending the Renderers

To add new visual elements:

1. **Static map elements** - Add to MapRenderer (e.g., nebulae, asteroids)
2. **Dynamic game state** - Add to RegionRenderer (e.g., combat indicators, fleet icons)
3. **Gameplay UI elements** - Add to Myra overlays/windows (e.g., docked panels, HUD widgets, dialogs)

Map/world renderers use `SpriteBatch` for 2D rendering with basic primitives created from 1x1 pixel textures. Gameplay workspace UI should be implemented in Myra.
