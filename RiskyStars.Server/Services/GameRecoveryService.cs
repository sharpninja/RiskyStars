using Microsoft.Extensions.Options;

namespace RiskyStars.Server.Services;

public class GameRecoveryService : IHostedService
{
    private readonly ILogger<GameRecoveryService> _logger;
    private readonly GameStateManager _gameStateManager;
    private readonly bool _autoRecoveryEnabled;

    public GameRecoveryService(ILogger<GameRecoveryService> logger, GameStateManager gameStateManager, IOptions<GamePersistenceOptions> options)
    {
        _logger = logger;
        _gameStateManager = gameStateManager;
        _autoRecoveryEnabled = options.Value.AutoRecoveryEnabled;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_autoRecoveryEnabled)
        {
            _logger.LogInformation("Game auto-recovery is disabled");
            return;
        }

        _logger.LogInformation("Starting game recovery service...");

        try
        {
            await _gameStateManager.RecoverAllGamesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during game recovery");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Game recovery service stopped");
        return Task.CompletedTask;
    }
}
