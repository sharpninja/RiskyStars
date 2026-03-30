using Microsoft.Extensions.Options;

namespace RiskyStars.Server.Services;

public class SessionCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SessionCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval;
    private readonly TimeSpan _inactivityThreshold;

    public SessionCleanupService(
        IServiceProvider serviceProvider, 
        ILogger<SessionCleanupService> logger,
        IOptions<SessionManagementOptions> sessionOptions)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _cleanupInterval = TimeSpan.FromMinutes(sessionOptions.Value.CleanupIntervalMinutes);
        _inactivityThreshold = TimeSpan.FromMinutes(sessionOptions.Value.SessionTimeoutMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Session Cleanup Service started - Cleanup Interval: {Interval}, Inactivity Threshold: {Threshold}", 
            _cleanupInterval, _inactivityThreshold);

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
