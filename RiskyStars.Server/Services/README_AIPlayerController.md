# AI Player System

## Overview

The AI Player System enables computer-controlled players in RiskyStars games. It consists of three main components:

1. **AIPlayer Entity** - Represents an AI-controlled player with difficulty settings
2. **AIPlayerController** - Orchestrates AI turn execution through all game phases
3. **GameSessionManager** - Manages AI player lifecycle during lobby setup and game sessions

## AIPlayerController

The `AIPlayerController` orchestrates AI turn execution in the RiskyStars game by coordinating multiple AI decision-making components to execute complete turns for AI players.

## Architecture

The controller integrates with the following components:

- **GameStateManager**: Manages game state and turn flow
- **AIPurchaseDecisionMaker**: Determines optimal army purchases
- **AIReinforcementPlanner**: Allocates reinforcements to regions
- **AIMovementPlanner**: Plans and executes army movements
- **AIEconomicManager**: Handles upgrades and hero assignments

## Features

### Turn Execution
Executes a complete AI turn through all four phases:
1. **Production Phase**: Produces resources and advances
2. **Purchase Phase**: Purchases armies and executes economic decisions (upgrades/heroes)
3. **Reinforcement Phase**: Allocates purchased armies to regions
4. **Movement Phase**: Executes strategic army movements

### Difficulty-Based Behavior

The controller applies different thinking delays and move limits based on difficulty:

| Difficulty | Thinking Delay | Sub-Action Delay | Max Moves/Turn |
|-----------|----------------|------------------|----------------|
| Easy      | 1-2 seconds    | 300ms           | 3              |
| Medium    | 2-3 seconds    | 200ms           | 5              |
| Hard      | 3-4 seconds    | 100ms           | 10             |

### Action Generation

Can generate `PlayerActionMessage` lists representing all AI decisions for a phase, useful for:
- Recording AI decision history
- Replaying AI turns
- Debugging AI behavior

## Usage

### Basic Turn Execution

```csharp
// Inject the controller
public class GameService
{
    private readonly AIPlayerController _aiController;
    
    public GameService(AIPlayerController aiController)
    {
        _aiController = aiController;
    }
    
    public async Task ProcessTurnAsync(string gameId)
    {
        // Check if current player is AI
        if (_aiController.ShouldExecuteAITurn(gameId))
        {
            var aiPlayer = _aiController.GetCurrentAIPlayer(gameId);
            if (aiPlayer != null)
            {
                await _aiController.ExecuteAITurnAsync(gameId, aiPlayer);
            }
        }
    }
}
```

### Automatic AI Turn Processing

```csharp
// After advancing a phase or changing players
await _aiController.ProcessAITurnIfNeededAsync(gameId);
```

### Action Message Generation

```csharp
var aiPlayer = _aiController.GetCurrentAIPlayer(gameId);
if (aiPlayer != null)
{
    var actions = await _aiController.GeneratePlayerActionsAsync(gameId, aiPlayer);
    
    // Process or log actions
    foreach (var action in actions)
    {
        Console.WriteLine($"AI Action: {action.ActionType} at {action.Timestamp}");
    }
}
```

## GameSessionManager Integration

### Lobby Setup with AI Players

The `GameSessionManager` provides methods to add and manage AI players during lobby setup:

```csharp
// Add an AI player to a lobby
var success = _sessionManager.AddAIPlayerToLobby(lobbyId, "AI Commander", DifficultyLevel.Hard);

// Remove an AI player from a lobby
var removed = _sessionManager.RemoveAIPlayerFromLobby(lobbyId, aiPlayerId);

// Check if a player is AI
var isAI = _sessionManager.IsAIPlayer(playerId);

// Get AI player counts
var (humanCount, aiCount) = _sessionManager.GetLobbyPlayerCounts(lobbyId);
```

### Game Session Lifecycle

When a game starts from a lobby:

1. **Game Creation**: AI players are initialized with their difficulty settings
2. **Session Tracking**: AI player IDs are stored in `GameSession.AIPlayerIds`
3. **Turn Triggering**: After game start, AI turns are automatically triggered
4. **Cleanup**: AI players are removed from tracking when session ends

### Automatic AI Turn Triggering

AI turns are automatically triggered in two scenarios:

1. **Game Start**: If the first player is AI
```csharp
// In GameSessionManager.StartGame()
_ = Task.Run(async () =>
{
    await Task.Delay(1000);
    await ProcessAITurnIfNeededAsync(session.SessionId);
});
```

2. **Phase Advancement**: After any player advances phase
```csharp
// In GameServiceImpl.ProcessPlayerAction()
case GamePlayerAction.ActionOneofCase.AdvancePhase:
    _gameStateManager.AdvancePhase(gameId);
    
    var session = _sessionManager.GetSessionByGameId(gameId);
    if (session != null)
    {
        _ = Task.Run(async () => 
        {
            await Task.Delay(500);
            await _sessionManager.ProcessAITurnIfNeededAsync(session.SessionId);
        });
    }
    break;
```

### AI Player Restrictions

AI players have the following restrictions enforced by GameSessionManager:

- Cannot use `LeaveLobby` (only host can remove them via `RemoveAIPlayerFromLobby`)
- Cannot use `SetPlayerReady` (always ready by default)
- Cannot connect to game streams (no network connection needed)
- Cannot be promoted to lobby host (host reassignment skips AI players)

## Integration Points

### Direct API Integration

The `GameSessionManager` provides direct API methods for AI player management:

```csharp
// Add AI player via direct API
var success = _sessionManager.AddAIPlayerToLobby(lobbyId, "AI Commander", DifficultyLevel.Hard);

// Remove AI player via direct API
var success = _sessionManager.RemoveAIPlayerFromLobby(lobbyId, aiPlayerId);

// Note: gRPC endpoints would require proto file updates and are not currently implemented
```

### Human vs AI Matchups

The system supports all matchup types:

- **Human vs Human**: Traditional multiplayer (no AI players)
- **Human vs AI**: Mixed lobbies with both human and AI players
- **AI vs AI**: Pure AI lobbies for simulation or testing
- **Single Player**: One human vs multiple AI opponents

## Decision Flow

### Production Phase
1. Apply thinking delay
2. Execute `ProduceResources` on GameStateManager
3. Apply thinking delay
4. Advance to Purchase phase

### Purchase Phase
1. Apply thinking delay
2. Get purchase decision from `AIPurchaseDecisionMaker`
3. Execute army purchases
4. Get economic decisions from `AIEconomicManager`
5. Execute upgrades and hero assignments
6. Apply thinking delay
7. Advance to Reinforcement phase

### Reinforcement Phase
1. Apply thinking delay
2. Calculate available armies
3. Get allocation plan from `AIReinforcementPlanner`
4. Reinforce each region with sub-action delays
5. Apply thinking delay
6. Advance to Movement phase

### Movement Phase
1. Apply thinking delay
2. Loop up to max moves:
   - Get best move from `AIMovementPlanner`
   - Execute move on GameStateManager
   - Apply sub-action delay
3. Apply thinking delay
4. Advance phase (ends turn)

## Logging

The controller provides detailed logging at multiple levels:

- **Information**: Turn start/end, major decisions (purchases, moves executed)
- **Debug**: Phase execution, individual actions
- **Warning**: Failed moves, invalid states
- **Error**: Exceptions during turn execution
- **Trace**: Thinking delays

## Error Handling

- Validates game existence before execution
- Validates current player is the AI player
- Catches and logs exceptions during move execution
- Continues with remaining moves if one fails
- Throws exceptions for critical failures to allow recovery

## Thread Safety

- Uses GameStateManager's locking mechanisms
- Safe for concurrent access through dependency injection
- Async execution prevents blocking main thread

## Extensibility

To extend AI behavior:

1. **Add new decision makers**: Inject and call in appropriate phase
2. **Customize delays**: Override `GetThinkingDelayRange` and `GetSubActionDelay`
3. **Modify move limits**: Override `GetMaxMovesPerTurn`
4. **Add new phases**: Create new `ExecuteXPhaseAsync` methods

## Testing

Example unit test structure:

```csharp
[Test]
public async Task ExecuteAITurn_CompletesAllPhases()
{
    // Arrange
    var gameId = "test-game";
    var aiPlayer = new AIPlayer(DifficultyLevel.Medium) { Id = "ai-1" };
    
    // Act
    await _aiController.ExecuteAITurnAsync(gameId, aiPlayer);
    
    // Assert
    // Verify all phases executed
    // Verify game state updated
}
```

## Performance Considerations

- Thinking delays add realistic pacing but increase turn time
- Can be reduced/removed for testing or simulation
- Movement phase can be expensive with many armies
- Decision makers use caching where applicable

## Dependencies

All dependencies are registered in `Program.cs`:

```csharp
builder.Services.AddSingleton<GameStateEvaluator>();
builder.Services.AddSingleton<CombatPredictor>();
builder.Services.AddSingleton<AIPurchaseDecisionMaker>();
builder.Services.AddSingleton<AIReinforcementPlanner>();
builder.Services.AddSingleton<AIMovementPlanner>();
builder.Services.AddSingleton<AIEconomicManager>();
builder.Services.AddSingleton<AIPlayerController>();
```
