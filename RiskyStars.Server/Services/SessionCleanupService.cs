namespace RiskyStars.Server.Services;

public class SessionCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SessionCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);
    private readonly TimeSpan _inactivityThreshold = TimeSpan.FromMinutes(30);

    public SessionCleanupService(IServiceProvider serviceProvider, ILogger<SessionCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Session Cleanup Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_cleanupInterval, stoppingToken);

                using var scope = _serviceProvider.CreateScope();
                var sessionManager = scope.ServiceProvider.GetRequiredService<GameSessionManager>();

                _logger.LogDebug("Running session cleanup...");
                sessionManager.CleanupInactiveSessions(_inactivityThreshold);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during session cleanup");
            }
        }

        _logger.LogInformation("Session Cleanup Service stopped");
    }
}
