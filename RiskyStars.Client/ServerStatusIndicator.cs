using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.Brushes;

namespace RiskyStars.Client;

public class ServerStatusIndicator
{
    private Panel _container;
    private Label _statusLabel;
    private Panel _statusDot;
    private Label _detailsLabel;
    private EmbeddedServerHost? _serverHost;

    public Panel Container => _container;

    public ServerStatusIndicator(int width = 400)
    {
        BuildUI(width);
    }

    private void BuildUI(int width)
    {
        var grid = new Grid
        {
            ColumnSpacing = ThemeManager.Spacing.Small,
            Width = width
        };

        grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        grid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));

        _statusDot = new Panel
        {
            Width = 12,
            Height = 12,
            Background = new SolidBrush(Color.Gray),
            GridColumn = 0,
            VerticalAlignment = VerticalAlignment.Center
        };
        grid.Widgets.Add(_statusDot);

        _statusLabel = new Label
        {
            Text = "Server: Stopped",
            TextColor = ThemeManager.Colors.TextSecondary,
            Scale = ThemeManager.FontScale.Small,
            GridColumn = 1,
            VerticalAlignment = VerticalAlignment.Center
        };
        grid.Widgets.Add(_statusLabel);

        _detailsLabel = new Label
        {
            Text = "",
            TextColor = ThemeManager.Colors.TextSecondary,
            Scale = ThemeManager.FontScale.Tiny,
            GridColumn = 2,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        grid.Widgets.Add(_detailsLabel);

        _container = new Panel
        {
            Padding = ThemeManager.Padding.Small,
            Background = new SolidBrush(ThemeManager.Colors.BackgroundMedium)
        };
        _container.Widgets.Add(grid);
    }

    public void SetServerHost(EmbeddedServerHost serverHost)
    {
        _serverHost = serverHost;
        Update();
    }

    public void Update()
    {
        if (_serverHost == null)
        {
            UpdateStatus(ServerStatus.Stopped, null, null);
            return;
        }

        var healthMonitor = _serverHost.HealthMonitor;
        UpdateStatus(_serverHost.Status, _serverHost.LastError, healthMonitor);
    }

    private void UpdateStatus(ServerStatus status, string? errorMessage, ServerHealthMonitor? healthMonitor)
    {
        switch (status)
        {
            case ServerStatus.Stopped:
                _statusDot.Background = new SolidBrush(Color.Gray);
                _statusLabel.Text = "Server: Stopped";
                _statusLabel.TextColor = ThemeManager.Colors.TextSecondary;
                _detailsLabel.Text = "";
                break;

            case ServerStatus.Starting:
                _statusDot.Background = new SolidBrush(Color.Yellow);
                _statusLabel.Text = "Server: Starting...";
                _statusLabel.TextColor = ThemeManager.Colors.TextWarning;
                _detailsLabel.Text = "Initializing";
                break;

            case ServerStatus.Running:
                _statusDot.Background = new SolidBrush(Color.LimeGreen);
                _statusLabel.Text = "Server: Running";
                _statusLabel.TextColor = ThemeManager.Colors.TextSuccess;
                
                if (healthMonitor != null)
                {
                    var timeSinceCheck = DateTime.UtcNow - healthMonitor.LastSuccessfulCheck;
                    _detailsLabel.Text = $"Healthy ({(int)timeSinceCheck.TotalSeconds}s ago)";
                }
                else
                {
                    _detailsLabel.Text = "Healthy";
                }
                break;

            case ServerStatus.Error:
                _statusDot.Background = new SolidBrush(Color.Red);
                _statusLabel.Text = "Server: Error";
                _statusLabel.TextColor = ThemeManager.Colors.TextError;
                _detailsLabel.Text = errorMessage != null ? TruncateError(errorMessage) : "Failed";
                break;

            case ServerStatus.Reconnecting:
                _statusDot.Background = new SolidBrush(Color.Orange);
                _statusLabel.Text = "Server: Reconnecting";
                _statusLabel.TextColor = ThemeManager.Colors.TextWarning;
                
                if (healthMonitor != null)
                {
                    int nextRetryMs = healthMonitor.ReconnectAttempt > 0 
                        ? CalculateExponentialBackoff(healthMonitor.ReconnectAttempt)
                        : 1000;
                    _detailsLabel.Text = $"Attempt {healthMonitor.ReconnectAttempt} (retry in {nextRetryMs / 1000}s)";
                }
                else
                {
                    _detailsLabel.Text = "Reconnecting...";
                }
                break;
        }
    }

    private string TruncateError(string error, int maxLength = 40)
    {
        if (error.Length <= maxLength)
        {
            return error;
        }

        return error.Substring(0, maxLength - 3) + "...";
    }

    private int CalculateExponentialBackoff(int attempt)
    {
        const int InitialRetryDelayMs = 1000;
        const int MaxRetryDelayMs = 30000;
        const double BackoffMultiplier = 2.0;
        
        int delay = (int)(InitialRetryDelayMs * Math.Pow(BackoffMultiplier, attempt - 1));
        return Math.Min(delay, MaxRetryDelayMs);
    }
}
