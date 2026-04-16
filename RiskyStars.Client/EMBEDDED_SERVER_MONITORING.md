# Embedded Server Health Monitoring

This document describes the embedded server health monitoring system for RiskyStars single-player mode.

## Overview

The embedded server health monitoring system provides:
- **Periodic Connectivity Checks**: Automatic health checks every 5 seconds to ensure the server is responsive
- **Automatic Reconnection**: Exponential backoff retry strategy when health checks fail
- **Visual Status Indicators**: Real-time UI feedback showing server state

## Architecture

### Components

#### 1. ServerHealthMonitor
**File**: `ServerHealthMonitor.cs`

Performs periodic health checks on the embedded server using HTTP requests to the `/health` endpoint.

**Features**:
- Health check interval: 5 seconds
- Consecutive failure threshold: 3 failures before marking server as unhealthy
- Exponential backoff: 1s → 2s → 4s → 8s → 16s → 30s (max)
- Automatic recovery detection

**Configuration**:
```csharp
private const int HealthCheckIntervalMs = 5000;        // 5 seconds
private const int InitialRetryDelayMs = 1000;          // 1 second
private const int MaxRetryDelayMs = 30000;             // 30 seconds
private const int MaxConsecutiveFailures = 3;          // 3 failures
private const double BackoffMultiplier = 2.0;          // 2x backoff
```

**Public API**:
```csharp
public bool IsHealthy { get; }
public int ConsecutiveFailures { get; }
public int ReconnectAttempt { get; }
public DateTime LastSuccessfulCheck { get; }
public string? LastError { get; }

public void Start()
public void Stop()
public void ResetReconnectAttempts()
```

#### 2. ServerStatus Enum
**File**: `EmbeddedServerHost.cs`

Represents the current state of the embedded server:
- `Stopped`: Server has not been started
- `Starting`: Server is initializing
- `Running`: Server is healthy and operational
- `Error`: Server encountered an error
- `Reconnecting`: Server is unhealthy, attempting to reconnect

#### 3. EmbeddedServerHost
**File**: `EmbeddedServerHost.cs`

Enhanced to integrate health monitoring:
- Creates and starts `ServerHealthMonitor` after successful server startup
- Updates `Status` property based on health callbacks
- Stops health monitor during cleanup

**New Properties**:
```csharp
public ServerStatus Status { get; }
public ServerHealthMonitor? HealthMonitor { get; }
```

#### 4. ServerStatusIndicator
**File**: `ServerStatusIndicator.cs`

Visual UI component displaying server status:
- **Status Dot**: Color-coded indicator (Gray/Yellow/Green/Orange/Red)
- **Status Label**: Text describing current state
- **Details Label**: Additional context (time since last check, retry info)

**Status Colors**:
- Gray: Stopped
- Yellow: Starting
- Green: Running (healthy)
- Orange: Reconnecting
- Red: Error

**Usage**:
```csharp
var indicator = new ServerStatusIndicator(500);
indicator.SetServerHost(embeddedServerHost);
indicator.Update(); // Call in Update() loop
```

## Integration

### Single Player Lobby Screen
The status indicator is displayed prominently in the single-player lobby setup screen:
```csharp
_serverStatusIndicator = new ServerStatusIndicator(600);
_serverStatusIndicator.Container.GridRow = 2;
_serverStatusIndicator.Container.HorizontalAlignment = HorizontalAlignment.Center;
```

### In-Game Display
For single-player games, the status indicator appears at the bottom center of the screen:
```csharp
_serverStatusIndicator = new ServerStatusIndicator(500);
_serverStatusIndicator.SetServerHost(_lobbyManager.EmbeddedServer);
_serverStatusIndicator.Container.HorizontalAlignment = HorizontalAlignment.Center;
_serverStatusIndicator.Container.VerticalAlignment = VerticalAlignment.Bottom;
_serverStatusIndicator.Container.Top = screenHeight - 35;
```

### Initialization Screen
During server startup, status is shown as text overlay:
```csharp
var statusText = embeddedServerHost.Status switch
{
    ServerStatus.Starting => "Starting server...",
    ServerStatus.Running => "Server ready!",
    ServerStatus.Error => $"Error: {embeddedServerHost.LastError}",
    _ => ""
};
```

## Health Check Process

### 1. Startup
When `EmbeddedServerHost.StartAsync()` is called:
1. Server status set to `Starting`
2. ASP.NET Core server initialized
3. Health endpoint verified via `WaitForServerReady()`
4. Status set to `Running`
5. `ServerHealthMonitor` created and started

### 2. Monitoring Loop
The health monitor runs continuously:
1. HTTP GET request to `http://localhost:{port}/health`
2. If successful (200 OK):
   - Reset failure counters
   - Update last successful check timestamp
   - Status remains `Running`
3. If failed:
   - Increment consecutive failures
   - If failures >= 3:
     - Status changes to `Reconnecting`
     - Exponential backoff applied
     - Retry continues

### 3. Recovery
When health check succeeds after failures:
1. Status changes from `Reconnecting` to `Running`
2. Failure counters reset to 0
3. UI updates to show healthy state

### 4. Cleanup
When server is disposed:
1. Health monitor stopped
2. HTTP client disposed
3. Server shutdown
4. Status set to `Stopped`

## Exponential Backoff Algorithm

The retry delay increases exponentially with each failed reconnection attempt:

```
Attempt 1: 1,000ms (1s)
Attempt 2: 2,000ms (2s)
Attempt 3: 4,000ms (4s)
Attempt 4: 8,000ms (8s)
Attempt 5: 16,000ms (16s)
Attempt 6+: 30,000ms (30s max)
```

Formula: `delay = min(InitialDelay * (2 ^ (attempt - 1)), MaxDelay)`

## Error Handling

### Common Errors
- **Connection Failed**: Network error or server not responding
- **Health Check Timed Out**: Request took longer than 2 seconds
- **Health Check Failed with Status**: Non-200 HTTP response
- **Server Runtime Error**: Exception in server process

### User Feedback
All errors are:
1. Logged to console
2. Stored in `LastError` property
3. Displayed in UI via status indicator
4. Truncated for display (max 40 chars)

## Example Usage

### Creating and Starting Server
```csharp
var serverHost = new EmbeddedServerHost();
bool success = await serverHost.StartAsync();

if (success)
{
    Console.WriteLine($"Server status: {serverHost.Status}");
    Console.WriteLine($"Health monitor active: {serverHost.HealthMonitor != null}");
}
```

### Monitoring Status
```csharp
// In Update() loop
serverStatusIndicator?.Update();

// Check status
if (serverHost.Status == ServerStatus.Reconnecting)
{
    Console.WriteLine($"Reconnecting... Attempt {serverHost.HealthMonitor?.ReconnectAttempt}");
}
```

### Cleanup
```csharp
await serverHost.StopAsync();
// or
await serverHost.DisposeAsync();
```

## Testing

### Manual Testing
1. Start single-player game
2. Observe "Starting server..." → "Server ready!" transition
3. Monitor green status indicator during gameplay
4. To test reconnection:
   - Use debugger to pause server process
   - Observe status change to "Reconnecting"
   - Resume server
   - Observe recovery to "Running"

### Status Transitions
```
Stopped → Starting → Running → (healthy)
                   ↓
                   Error (permanent failure)
                   ↓
                Reconnecting → Running (recovery)
```

## Performance Impact

- **Health Check Overhead**: Minimal - single HTTP GET every 5 seconds
- **Network Traffic**: ~100 bytes per check (20 bytes/second average)
- **CPU Impact**: Negligible - async operation, no blocking
- **Memory**: ~8KB for HttpClient and monitoring state

## Configuration

To adjust monitoring behavior, modify constants in `ServerHealthMonitor.cs`:

```csharp
// How often to check health when server is healthy
private const int HealthCheckIntervalMs = 5000;

// Initial retry delay when health check fails
private const int InitialRetryDelayMs = 1000;

// Maximum retry delay (caps exponential backoff)
private const int MaxRetryDelayMs = 30000;

// How many consecutive failures before marking unhealthy
private const int MaxConsecutiveFailures = 3;

// Multiplier for exponential backoff
private const double BackoffMultiplier = 2.0;
```

## Future Enhancements

Potential improvements:
- Configurable health check intervals
- Health check metrics (response time, success rate)
- Automatic server restart on critical failures
- Alert notifications for prolonged outages
- Health check endpoint authentication
- Circuit breaker pattern for rapid failures
