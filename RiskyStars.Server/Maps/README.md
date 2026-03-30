# Map Files

This directory contains map data files for RiskyStars in JSON format.

## Map Structure

Maps follow the game rules defined in `1.0_Rules/1.0.00_Gameplay.md`:

- **Home Systems**: Equal number to player count, each with configurable regions (default: 8)
- **Featured System**: 1 system with 2x the regions of Home systems (default: 16)
- **Minor Systems**: 2 per player, each with 25% of Home system regions (default: 2)

## JSON Format

### Map Configuration
```json
{
  "configuration": {
    "playerCount": 2,
    "homeSystemRegionCount": 8
  }
}
```

### Star Systems
Each star system has:
- `id`: Unique identifier
- `name`: Display name
- `type`: 0 (Home), 1 (Featured), 2 (Minor)
- `stellarBodies`: Array of planets, gas giants, etc.
- `hyperspaceLanes`: Array of connected lanes

### Stellar Bodies
Types (enum):
- `0`: GasGiant (Fuel) - 1 region
- `1`: RockyPlanet (Population) - Variable regions based on surface type
- `2`: Planetoid (Metal) - 1 region
- `3`: Comet (Fuel) - 1 region

Rocky Planet Surface Types:
- `0`: Barren - 2 regions (Northern/Southern Hemisphere)
- `1`: Gaia - 2-10 regions (continents)
- `2`: Ocean - 1 region

### Hyperspace Lanes
Connect star systems and have two mouths (one at each end):
- `starSystemAId` / `starSystemBId`: Connected systems
- `mouthAId` / `mouthBId`: Unique identifiers for lane mouths
- Each mouth can be owned and defended separately

## Sample Maps

- `sample_2player_map.json`: 2-player map with 8 regions per Home system

## Usage

```csharp
var mapService = new MapService();

// Load a map
var map = mapService.LoadMap("Maps/sample_2player_map.json");

// Generate a new map
var newMap = mapService.GenerateNewMap(playerCount: 3, homeSystemRegionCount: 10);

// Save a map
mapService.SaveCurrentMap("Maps/my_custom_map.json");
```
