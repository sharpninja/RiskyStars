using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D;
using RiskyStars.Shared;

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
    
    private double _fpsUpdateTimer;
    private int _frameCount;
    private double _lastFps;
    
    public DebugInfoWindow(WindowPreferences preferences, int screenWidth, int screenHeight)
        : base("debug_info", "Debug Information", preferences, screenWidth, screenHeight, 380, 400)
    {
        BuildContent();
        
        DockTo(DockPosition.BottomLeft);
    }
    
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
        
        _window.Content = mainLayout;
    }
    
    private Widget BuildCameraPanel()
    {
        var panel = new Panel
        {
            Background = new SolidBrush(ThemeManager.Colors.BackgroundMedium),
            Border = new SolidBrush(ThemeManager.Colors.AccentCyan),
            BorderThickness = new Thickness(ThemeManager.BorderThickness.Normal),
            Padding = ThemeManager.Padding.Medium,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        
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
        var panel = new Panel
        {
            Background = new SolidBrush(ThemeManager.Colors.BackgroundMedium),
            Border = new SolidBrush(ThemeManager.Colors.AccentCyan),
            BorderThickness = new Thickness(ThemeManager.BorderThickness.Normal),
            Padding = ThemeManager.Padding.Medium,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        
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
        var panel = new Panel
        {
            Background = new SolidBrush(ThemeManager.Colors.BackgroundMedium),
            Border = new SolidBrush(ThemeManager.Colors.AccentCyan),
            BorderThickness = new Thickness(ThemeManager.BorderThickness.Normal),
            Padding = ThemeManager.Padding.Medium,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        
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
        var panel = new Panel
        {
            Background = new SolidBrush(ThemeManager.Colors.BackgroundMedium),
            Border = new SolidBrush(ThemeManager.Colors.AccentCyan),
            BorderThickness = new Thickness(ThemeManager.BorderThickness.Normal),
            Padding = ThemeManager.Padding.Medium,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        
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
}
