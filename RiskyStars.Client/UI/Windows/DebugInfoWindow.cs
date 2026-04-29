using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI;
using Myra.Graphics2D;
using RiskyStars.Shared;
using MyraButton = Myra.Graphics2D.UI.Button;

namespace RiskyStars.Client;

public class DebugInfoWindow : DockableWindow
{
    private Label? _cameraPositionLabel;
    private Label? _cameraZoomLabel;
    private Label? _fpsLabel;
    private Label? _gameStateLabel;
    private Label? _connectionStatusLabel;
    private Label? _playerCountLabel;
    private Label? _turnPhaseLabel;
    private Label? _selectionLabel;
    private Label? _uiAuditSummaryLabel;
    private Label? _uiAuditScaleLabel;
    private Label? _uiAuditWarningLabel;
    private Label? _uiAuditDetailsLabel;
    private VerticalStackPanel? _visualTreeRowsPanel;
    private Label? _visualTreeSelectionLabel;
    private string? _selectedVisualElementId;
    private string _visualTreeRowsSignature = string.Empty;
    
    private double _fpsUpdateTimer;
    private int _frameCount;
    private double _lastFps;

    public string? SelectedVisualElementId => _selectedVisualElementId;
    
    public DebugInfoWindow(WindowPreferences preferences, int screenWidth, int screenHeight)
        : base("debug_info", "Debug Information", preferences, screenWidth, screenHeight, 520, 640)
    {
        BuildContent();
        
        DockTo(DockPosition.BottomLeft);
    }

    public event EventHandler<string?>? VisualElementSelected;
    
    private void BuildContent()
    {
        var mainLayout = new VerticalStackPanel
        {
            Spacing = ThemeManager.Spacing.Medium,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        
        mainLayout.Widgets.Add(BuildCameraPanel());
        mainLayout.Widgets.Add(BuildPerformancePanel());
        mainLayout.Widgets.Add(BuildGameStatePanel());
        mainLayout.Widgets.Add(BuildSelectionPanel());
        mainLayout.Widgets.Add(BuildUiAuditPanel());
        
        _window.Content = mainLayout;
    }
    
    private Widget BuildCameraPanel()
    {
        var panel = ThemedUIFactory.CreateGameplayPanel();
        panel.HorizontalAlignment = HorizontalAlignment.Stretch;
        
        var layout = new VerticalStackPanel
        {
            Spacing = ThemeManager.Spacing.XSmall
        };
        
        var titleLabel = ThemedUIFactory.CreateLabel("Camera", ThemeManager.LabelTheme.Heading);
        layout.Widgets.Add(titleLabel);
        
        _cameraPositionLabel = ThemedUIFactory.CreateSmallLabel("Position: (0, 0)");
        layout.Widgets.Add(_cameraPositionLabel);
        
        _cameraZoomLabel = ThemedUIFactory.CreateSmallLabel("Zoom: 1.00x");
        layout.Widgets.Add(_cameraZoomLabel);
        
        panel.Widgets.Add(layout);
        return panel;
    }
    
    private Widget BuildPerformancePanel()
    {
        var panel = ThemedUIFactory.CreateGameplayPanel();
        panel.HorizontalAlignment = HorizontalAlignment.Stretch;
        
        var layout = new VerticalStackPanel
        {
            Spacing = ThemeManager.Spacing.XSmall
        };
        
        var titleLabel = ThemedUIFactory.CreateLabel("Performance", ThemeManager.LabelTheme.Heading);
        layout.Widgets.Add(titleLabel);
        
        _fpsLabel = ThemedUIFactory.CreateSmallLabel("FPS: 0");
        layout.Widgets.Add(_fpsLabel);
        
        panel.Widgets.Add(layout);
        return panel;
    }
    
    private Widget BuildGameStatePanel()
    {
        var panel = ThemedUIFactory.CreateGameplayPanel();
        panel.HorizontalAlignment = HorizontalAlignment.Stretch;
        
        var layout = new VerticalStackPanel
        {
            Spacing = ThemeManager.Spacing.XSmall
        };
        
        var titleLabel = ThemedUIFactory.CreateLabel("Game State", ThemeManager.LabelTheme.Heading);
        layout.Widgets.Add(titleLabel);
        
        _gameStateLabel = ThemedUIFactory.CreateSmallLabel("State: N/A");
        layout.Widgets.Add(_gameStateLabel);
        
        _connectionStatusLabel = ThemedUIFactory.CreateSmallLabel("Connection: N/A");
        layout.Widgets.Add(_connectionStatusLabel);
        
        _playerCountLabel = ThemedUIFactory.CreateSmallLabel("Players: 0");
        layout.Widgets.Add(_playerCountLabel);
        
        _turnPhaseLabel = ThemedUIFactory.CreateSmallLabel("Phase: N/A");
        layout.Widgets.Add(_turnPhaseLabel);
        
        panel.Widgets.Add(layout);
        return panel;
    }
    
    private Widget BuildSelectionPanel()
    {
        var panel = ThemedUIFactory.CreateGameplayPanel();
        panel.HorizontalAlignment = HorizontalAlignment.Stretch;
        
        var layout = new VerticalStackPanel
        {
            Spacing = ThemeManager.Spacing.XSmall
        };
        
        var titleLabel = ThemedUIFactory.CreateLabel("Selection", ThemeManager.LabelTheme.Heading);
        layout.Widgets.Add(titleLabel);
        
        _selectionLabel = ThemedUIFactory.CreateSmallLabel("None");
        layout.Widgets.Add(_selectionLabel);
        
        panel.Widgets.Add(layout);
        return panel;
    }

    [ExcludeFromCodeCoverage(Justification = "Thin Myra widget composition; deterministic audit text is covered by DebugUiAuditText tests.")]
    private Widget BuildUiAuditPanel()
    {
        var panel = ThemedUIFactory.CreateGameplayPanel();
        panel.HorizontalAlignment = HorizontalAlignment.Stretch;

        var layout = new VerticalStackPanel
        {
            Spacing = ThemeManager.Spacing.XSmall
        };

        var titleLabel = ThemedUIFactory.CreateLabel("UI Audit", ThemeManager.LabelTheme.Heading);
        layout.Widgets.Add(titleLabel);

        _uiAuditSummaryLabel = ThemedUIFactory.CreateSmallLabel("UI: waiting for layout");
        layout.Widgets.Add(_uiAuditSummaryLabel);

        _uiAuditScaleLabel = ThemedUIFactory.CreateSmallLabel("Scale: N/A");
        layout.Widgets.Add(_uiAuditScaleLabel);

        _uiAuditWarningLabel = ThemedUIFactory.CreateSmallLabel("Warnings: N/A");
        layout.Widgets.Add(_uiAuditWarningLabel);

        _uiAuditDetailsLabel = ThemedUIFactory.CreateSmallLabel("Elements: N/A");
        _uiAuditDetailsLabel.Wrap = true;
        _uiAuditDetailsLabel.TextColor = ThemeManager.Colors.TextSecondary;
        layout.Widgets.Add(_uiAuditDetailsLabel);

        var treeTitleLabel = ThemedUIFactory.CreateSmallLabel("Visual Tree");
        treeTitleLabel.TextColor = ThemeManager.Colors.TextAccent;
        layout.Widgets.Add(treeTitleLabel);

        _visualTreeRowsPanel = new VerticalStackPanel
        {
            Spacing = Math.Max(1, ThemeManager.ScalePixels(1)),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        layout.Widgets.Add(ThemedUIFactory.CreateAutoScrollViewer(_visualTreeRowsPanel, ThemeManager.ScalePixels(220)));

        _visualTreeSelectionLabel = ThemedUIFactory.CreateSmallLabel("Selected: none");
        _visualTreeSelectionLabel.Wrap = true;
        _visualTreeSelectionLabel.TextColor = ThemeManager.Colors.TextSecondary;
        layout.Widgets.Add(_visualTreeSelectionLabel);

        panel.Widgets.Add(layout);
        return panel;
    }
    
    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        
        _frameCount++;
        _fpsUpdateTimer += gameTime.ElapsedGameTime.TotalSeconds;
        
        if (_fpsUpdateTimer >= 1.0)
        {
            _lastFps = _frameCount / _fpsUpdateTimer;
            _frameCount = 0;
            _fpsUpdateTimer = 0;
            
            if (_fpsLabel != null)
            {
                _fpsLabel.Text = $"FPS: {_lastFps:F0}";
                
                if (_lastFps >= 55)
                {
                    _fpsLabel.TextColor = ThemeManager.Colors.TextSuccess;
                }
                else if (_lastFps >= 30)
                {
                    _fpsLabel.TextColor = ThemeManager.Colors.TextWarning;
                }
                else
                {
                    _fpsLabel.TextColor = ThemeManager.Colors.TextError;
                }
            }
        }
    }
    
    public void UpdateCameraInfo(Camera2D camera)
    {
        if (_cameraPositionLabel != null)
        {
            _cameraPositionLabel.Text = $"Position: ({camera.Position.X:F0}, {camera.Position.Y:F0})";
        }
        
        if (_cameraZoomLabel != null)
        {
            _cameraZoomLabel.Text = $"Zoom: {camera.Zoom:F2}x";
        }
    }
    
    public void UpdateGameStateInfo(GameStateCache gameStateCache, ConnectionManager? connectionManager)
    {
        if (_gameStateLabel != null)
        {
            var gameId = gameStateCache.GetGameId();
            _gameStateLabel.Text = string.IsNullOrEmpty(gameId) ? "State: No game" : $"State: Game {gameId.Substring(0, Math.Min(8, gameId.Length))}";
        }
        
        if (_connectionStatusLabel != null && connectionManager != null)
        {
            var statusText = connectionManager.Status switch
            {
                ConnectionStatus.Connected => "Connected",
                ConnectionStatus.Connecting => "Connecting...",
                ConnectionStatus.Reconnecting => $"Reconnecting ({connectionManager.ReconnectAttempts}/{connectionManager.MaxAttempts})",
                ConnectionStatus.Disconnected => "Disconnected",
                ConnectionStatus.Error => "Error",
                _ => "Unknown"
            };
            
            _connectionStatusLabel.Text = $"Connection: {statusText}";
            
            _connectionStatusLabel.TextColor = connectionManager.Status switch
            {
                ConnectionStatus.Connected => ThemeManager.Colors.TextSuccess,
                ConnectionStatus.Connecting => ThemeManager.Colors.TextWarning,
                ConnectionStatus.Reconnecting => ThemeManager.Colors.TextWarning,
                ConnectionStatus.Error => ThemeManager.Colors.TextError,
                _ => ThemeManager.Colors.TextSecondary
            };
        }
        
        if (_playerCountLabel != null)
        {
            var players = gameStateCache.GetAllPlayerStates();
            _playerCountLabel.Text = $"Players: {players.Count}";
        }
        
        if (_turnPhaseLabel != null)
        {
            var phase = gameStateCache.GetCurrentPhase();
            var phaseText = phase switch
            {
                TurnPhase.Production => "Production",
                TurnPhase.Purchase => "Purchase",
                TurnPhase.Reinforcement => "Reinforcement",
                TurnPhase.Movement => "Movement",
                _ => "Unknown"
            };
            
            var turnNumber = gameStateCache.GetTurnNumber();
            _turnPhaseLabel.Text = $"Phase: Turn {turnNumber} - {phaseText}";
        }
    }
    
    public void UpdateSelectionInfo(SelectionState selection)
    {
        if (_selectionLabel == null)
        {
            return;
        }

        string selectionText = selection.Type switch
        {
            SelectionType.Army => $"Army: {selection.SelectedArmy?.ArmyId?.Substring(0, Math.Min(8, selection.SelectedArmy.ArmyId.Length))}",
            SelectionType.Region => $"Region: {selection.SelectedRegion?.Name}",
            SelectionType.StellarBody => $"Body: {selection.SelectedStellarBody?.Name}",
            SelectionType.StarSystem => $"System: {selection.SelectedStarSystem?.Name}",
            SelectionType.HyperspaceLaneMouth => "Lane Mouth",
            _ => "None"
        };
        
        _selectionLabel.Text = selectionText;
    }

    [ExcludeFromCodeCoverage(Justification = "Thin Myra label wiring; deterministic audit text is covered by DebugUiAuditText tests.")]
    internal void UpdateUiAuditInfo(GameUiAuditReport auditReport)
    {
        if (_uiAuditSummaryLabel != null)
        {
            _uiAuditSummaryLabel.Text = DebugUiAuditText.FormatSummary(auditReport);
        }

        if (_uiAuditScaleLabel != null)
        {
            _uiAuditScaleLabel.Text = DebugUiAuditText.FormatScale(auditReport.Scale);
        }

        if (_uiAuditWarningLabel != null)
        {
            _uiAuditWarningLabel.Text = DebugUiAuditText.FormatWarnings(auditReport);
            _uiAuditWarningLabel.TextColor = auditReport.WarningCount == 0
                ? ThemeManager.Colors.TextSuccess
                : ThemeManager.Colors.TextWarning;
        }

        if (_uiAuditDetailsLabel != null)
        {
            _uiAuditDetailsLabel.Text = DebugUiAuditText.FormatDetails(auditReport);
        }

        UpdateVisualTreeRows(auditReport);
        if (_visualTreeSelectionLabel != null)
        {
            _visualTreeSelectionLabel.Text = GameUiVisualTreeInspector.FormatSelectionDetails(auditReport, _selectedVisualElementId);
        }
    }

    [ExcludeFromCodeCoverage(Justification = "Thin Myra row creation; deterministic row text and selection behavior are covered separately.")]
    private void UpdateVisualTreeRows(GameUiAuditReport auditReport)
    {
        if (_visualTreeRowsPanel == null)
        {
            return;
        }

        IReadOnlyList<GameUiVisualTreeRow> rows = GameUiVisualTreeInspector.BuildRows(auditReport, _selectedVisualElementId);
        string signature = string.Join(
            Environment.NewLine,
            rows.Select(row => $"{row.Id}|{row.DisplayText}|{row.BoundsText}|{row.IsSelected}|{row.HasWarnings}|{row.HasValidScreenBounds}"));
        if (string.Equals(_visualTreeRowsSignature, signature, StringComparison.Ordinal))
        {
            return;
        }

        _visualTreeRowsSignature = signature;
        _visualTreeRowsPanel.Widgets.Clear();
        foreach (GameUiVisualTreeRow row in rows)
        {
            _visualTreeRowsPanel.Widgets.Add(CreateVisualTreeRowButton(row));
        }
    }

    [ExcludeFromCodeCoverage(Justification = "Thin Myra button wiring; deterministic selection state is covered separately.")]
    private Widget CreateVisualTreeRowButton(GameUiVisualTreeRow row)
    {
        var label = new Label
        {
            Text = $"{row.DisplayText}  {row.BoundsText}",
            Wrap = false,
            SingleLine = true,
            HorizontalAlignment = HorizontalAlignment.Left,
            TextColor = row.HasWarnings
                ? ThemeManager.Colors.TextWarning
                : row.HasValidScreenBounds
                    ? ThemeManager.Colors.TextPrimary
                    : ThemeManager.Colors.TextError
        };

        var button = new MyraButton
        {
            Height = Math.Max(24, ThemeManager.ScalePixels(26)),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Content = label
        };
        ThemeManager.ApplyButtonTheme(button, row.IsSelected ? ThemeManager.ButtonTheme.Primary : ThemeManager.ButtonTheme.Default);

        string selectedId = row.Id;
        button.Click += (_, _) => SelectVisualElement(selectedId);
        return button;
    }

    private void SelectVisualElement(string id)
    {
        if (string.Equals(_selectedVisualElementId, id, StringComparison.Ordinal))
        {
            return;
        }

        _selectedVisualElementId = id;
        _visualTreeRowsSignature = string.Empty;
        VisualElementSelected?.Invoke(this, _selectedVisualElementId);
    }
}

internal static class DebugUiAuditText
{
    public static string FormatSummary(GameUiAuditReport report)
    {
        return $"Elements: {report.Entries.Count} ({report.MyraCount} Myra, {report.XnaCount} XNA)";
    }

    public static string FormatScale(GameUiScaleContext scale)
    {
        return
            $"Scale: UI {scale.UiScalePercent}% ({scale.UiScaleFactor:F2}x), " +
            $"DPI {scale.DpiScaleX:F2}x{scale.DpiScaleY:F2}, " +
            $"BB {scale.BackBufferWidth}x{scale.BackBufferHeight}, Client {scale.ClientWidth}x{scale.ClientHeight}";
    }

    public static string FormatWarnings(GameUiAuditReport report)
    {
        return $"Warnings: {report.WarningCount}, hidden: {report.HiddenCount}, invalid bounds: {report.InvalidCount}";
    }

    public static string FormatDetails(GameUiAuditReport report, int maxEntries = 6)
    {
        if (report.Entries.Count == 0)
        {
            return "Elements: none";
        }

        var prioritizedEntries = report.Entries
            .OrderByDescending(entry => entry.Warnings.Count > 0)
            .ThenBy(entry => entry.Source)
            .ThenBy(entry => entry.Id, StringComparer.Ordinal)
            .Take(Math.Max(1, maxEntries))
            .Select(FormatEntry);

        return string.Join(Environment.NewLine, prioritizedEntries);
    }

    private static string FormatEntry(GameUiAuditEntry entry)
    {
        Rectangle bounds = entry.ScreenBounds;
        string warningSuffix = entry.Warnings.Count == 0
            ? string.Empty
            : $" [{string.Join(", ", entry.Warnings)}]";

        return $"{entry.Id}: {entry.Source} {bounds.Width}x{bounds.Height} @ {bounds.X},{bounds.Y}{warningSuffix}";
    }
}
