using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
namespace RiskyStars.Client;

public class ServerStatusIndicator
{
    private Panel _container = null!;
    private Label _statusLabel = null!;
    private Panel _statusDot = null!;
    private Label _detailsLabel = null!;
    private Grid _grid = null!;
    private EmbeddedServerHost? _serverHost;

    public Panel Container => _container;

    public ServerStatusIndicator(int width = 400)
    {
        BuildUI(width);
    }

    private void BuildUI(int width)
    {
        int scaledWidth = ThemeManager.ScalePixels(width);
        _grid = new Grid
        {
            ColumnSpacing = ThemeManager.Spacing.Small,
            Width = scaledWidth
        };

        _grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        _grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        _grid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));

#pragma warning disable CS0618 // Type or member is obsolete
        _statusDot = new Panel
        {
            Width = ThemeManager.ScalePixels(12),
            Height = ThemeManager.ScalePixels(12),
            Background = ThemeManager.CreateSolidBrush(ThemeManager.Colors.DisabledColor),
            GridColumn = 0,
            VerticalAlignment = VerticalAlignment.Center
        };
#pragma warning restore CS0618 // Type or member is obsolete
        _grid.Widgets.Add(_statusDot);

#pragma warning disable CS0618 // Type or member is obsolete
        _statusLabel = new Label
        {
            Text = "Server: Stopped",
            Font = ThemeManager.UiFonts.Small,
            TextColor = ThemeManager.Colors.TextSecondary,
            GridColumn = 1,
            VerticalAlignment = VerticalAlignment.Center
        };
#pragma warning restore CS0618 // Type or member is obsolete
        _grid.Widgets.Add(_statusLabel);

#pragma warning disable CS0618 // Type or member is obsolete
        _detailsLabel = new Label
        {
            Text = "",
            Font = ThemeManager.UiFonts.Tiny,
            TextColor = ThemeManager.Colors.TextSecondary,
            GridColumn = 2,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right
        };
#pragma warning restore CS0618 // Type or member is obsolete
        _grid.Widgets.Add(_detailsLabel);

        _container = new Panel
        {
            Padding = ThemeManager.Padding.Small,
            Background = ThemeManager.AssetBrushes.TerminalPanel,
            Border = ThemeManager.CreateSolidBrush(ThemeManager.Colors.BorderNormal),
            BorderThickness = new Thickness(ThemeManager.BorderThickness.Thin)
        };
        _container.Widgets.Add(_grid);
    }

    public void SetServerHost(EmbeddedServerHost? serverHost)
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
                _statusDot.Background = ThemeManager.CreateSolidBrush(ThemeManager.Colors.DisabledColor);
                _statusLabel.Text = "Server: Stopped";
                _statusLabel.TextColor = ThemeManager.Colors.TextSecondary;
                _detailsLabel.Text = "";
                break;

            case ServerStatus.Starting:
                _statusDot.Background = ThemeManager.CreateSolidBrush(ThemeManager.Colors.TextWarning);
                _statusLabel.Text = "Server: Starting...";
                _statusLabel.TextColor = ThemeManager.Colors.TextWarning;
                _detailsLabel.Text = "Initializing";
                break;

            case ServerStatus.Running:
                _statusDot.Background = ThemeManager.CreateSolidBrush(ThemeManager.Colors.TextSuccess);
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
                _statusDot.Background = ThemeManager.CreateSolidBrush(ThemeManager.Colors.TextError);
                _statusLabel.Text = "Server: Error";
                _statusLabel.TextColor = ThemeManager.Colors.TextError;
                _detailsLabel.Text = errorMessage != null ? TruncateError(errorMessage) : "Failed";
                break;

            case ServerStatus.Reconnecting:
                _statusDot.Background = ThemeManager.CreateSolidBrush(ThemeManager.Colors.TextWarning);
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

    public void Resize(int width)
    {
        if (width <= 0)
        {
            return;
        }

        _grid.Width = ThemeManager.ScalePixels(width);
    }
}
