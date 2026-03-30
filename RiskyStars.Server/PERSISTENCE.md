# Game Persistence System

## Overview

The RiskyStars server includes a comprehensive game persistence system that automatically saves and recovers game state to JSON files with versioning support.

## Features

- **Automatic Saving**: Games are automatically saved when a turn completes (Movement phase ends)
- **Auto-Recovery**: On server startup, all saved games are automatically restored
- **Versioning**: Game state files include version numbers for future migration support
- **Backup System**: Each save creates a backup, keeping up to 10 historical versions per game
- **Combat State Preservation**: Active combat sessions are saved and restored with full state
- **Manual Save/Load**: Programmatic API for manual save/load operations

## Configuration

Configure persistence settings in `appsettings.json`:

```json
{
  "GamePersistence": {
    "SavePath": "GameSaves",
    "AutoSaveEnabled": true,
    "AutoRecoveryEnabled": true,
    "MaxBackupsPerGame": 10
  }
}
```

### Configuration Options

- **SavePath**: Directory path for game saves (relative or absolute)
- **AutoSaveEnabled**: Enable/disable automatic saves on turn completion
- **AutoRecoveryEnabled**: Enable/disable automatic game recovery on server startup
- **MaxBackupsPerGame**: Maximum number of backup files to keep per game

## File Structure

Game saves are stored as JSON files in the configured `SavePath` directory:

```
GameSaves/
  ├── game_{gameId}.json                    # Current game state
  ├── game_{gameId}_backup_20240101_120000.json  # Timestamped backup
  ├── game_{gameId}_backup_20240101_113000.json
  └── ...
```

## Game State Snapshot

Each saved game includes:

- **Version**: Schema version for migration support
- **SavedAt**: Timestamp of when the save was created
- **Game Data**:
  - Game ID and turn number
  - Current phase and player index
  - All player data (resources, stockpiles, owned regions)
  - Star systems with stellar bodies and regions
  - Alliances
  - Army positions and states
- **Combat State**:
  - Active combat sessions
  - Attacking and defending armies
  - Reinforcement arrival order
  - Combat round number

## Auto-Save Behavior

Games are automatically saved when:

1. A new game is created
2. A turn completes (Movement phase → Production phase transition)

The save operation runs asynchronously to avoid blocking game operations.

## Auto-Recovery

On server startup, the `GameRecoveryService` will:

1. Scan the `GameSaves` directory for saved games
2. Load each game's state
3. Restore all active combat sessions
4. Re-establish game state in memory

Recovered games are immediately available for player connections.

## Manual Operations

The `GameStateManager` provides methods for manual persistence operations:

```csharp
// Save a game
await gameStateManager.SaveGameStateAsync(gameId);

// Load a specific game
await gameStateManager.LoadGameStateAsync(gameId);

// Recover a game if not already loaded
await gameStateManager.RecoverGameAsync(gameId);

// Get list of all saved games
var savedGames = await gameStateManager.GetSavedGamesAsync();

// Delete a game and its saves
await gameStateManager.DeleteGameAsync(gameId);
```

## Repository API

The `GameRepository` provides low-level persistence operations:

```csharp
// Save game state with optional combat manager
await repository.SaveGameStateAsync(game, combatManager);

// Load game state
var result = await repository.LoadGameStateAsync(gameId);
if (result.HasValue)
{
    var (game, combatSessions) = result.Value;
}

// Get all saved game IDs
var gameIds = await repository.GetAllSavedGameIdsAsync();

// Get metadata without loading full game
var metadata = await repository.GetGameMetadataAsync(gameId);

// Restore from backup
await repository.RestoreFromBackupAsync(gameId);

// Delete game saves
await repository.DeleteGameStateAsync(gameId);
```

## Backup Management

- Backups are created automatically before each save
- Old backups are automatically cleaned up based on `MaxBackupsPerGame`
- Backups are timestamped with format: `yyyyMMdd_HHmmss`
- Manual restoration from specific backup timestamps is supported

## Error Handling

The persistence system includes comprehensive error handling:

- Failed saves log errors but don't crash the server
- Load failures are logged with detailed error information
- Corrupted save files are detected and reported
- Missing directories are automatically created

## Version Migration

The current version is **1**. Future versions can include migration logic:

1. Check `snapshot.Version` when loading
2. Apply transformations based on version number
3. Save with updated version

## Performance Considerations

- Saves are performed asynchronously
- JSON serialization is optimized with custom options
- Large game states are handled efficiently
- Backup cleanup prevents disk space issues

## Logging

The persistence system logs important events:

- `Information`: Save/load operations, recovery status
- `Warning`: Missing files, version mismatches, backup failures
- `Error`: Save/load failures, corruption detection

## Disabling Persistence

To disable persistence features:

```json
{
  "GamePersistence": {
    "AutoSaveEnabled": false,
    "AutoRecoveryEnabled": false
  }
}
```

Note: The system will still be available for manual operations.
