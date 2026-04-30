using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI;
using Myra.Graphics2D;
using RiskyStars.Shared;

namespace RiskyStars.Client;

public class AIVisualizationWindow : DockableWindow
{
    private Label? _aiStatusLabel;
    private Label? _currentActionLabel;
    private CheckButton? _showMovementCheckbox;
    private CheckButton? _showReinforcementsCheckbox;
    private CheckButton? _showPurchasesCheckbox;
    private CheckButton? _autoFollowCheckbox;
    private VerticalStackPanel? _logContainer;
    
    private readonly List<string> _activityLog = new();
    private const int MaxLogEntries = 10;
    
    public bool ShowMovementAnimations { get; private set; } = true;
    public bool ShowReinforcementHighlights { get; private set; } = true;
    public bool ShowPurchaseIndicators { get; private set; } = true;
    public bool AutoFollowAIActions { get; private set; } = true;
    
    public AIVisualizationWindow(WindowPreferences preferences, int screenWidth, int screenHeight)
        : base("ai_visualization", "AI Visualization", preferences, screenWidth, screenHeight, 400, 500)
    {
        BuildContent();
        
        DockTo(DockPosition.TopLeft);
    }
    
    private void BuildContent()
    {
        var mainLayout = new VerticalStackPanel
        {
            Spacing = ThemeManager.Spacing.Medium,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        
        mainLayout.Widgets.Add(BuildStatusPanel());
        mainLayout.Widgets.Add(BuildOptionsPanel());
        mainLayout.Widgets.Add(BuildActivityLogPanel());
        
        SetScrollableContent(mainLayout);
    }
    
    private Widget BuildStatusPanel()
    {
        var panel = ThemedUIFactory.CreateGameplayPanel();
        panel.HorizontalAlignment = HorizontalAlignment.Stretch;
        
        var layout = new VerticalStackPanel
        {
            Spacing = ThemeManager.Spacing.Small
        };
        
        var titleLabel = ThemedUIFactory.CreateLabel("AI Status", ThemeManager.LabelTheme.Heading);
        layout.Widgets.Add(titleLabel);
        
        _aiStatusLabel = ThemedUIFactory.CreateLabel("No AI activity");
        _aiStatusLabel.TextColor = ThemeManager.Colors.TextSecondary;
        layout.Widgets.Add(_aiStatusLabel);
        
        _currentActionLabel = ThemedUIFactory.CreateSmallLabel("");
        _currentActionLabel.Visible = false;
        layout.Widgets.Add(_currentActionLabel);
        
        panel.Widgets.Add(layout);
        return panel;
    }
    
    private Widget BuildOptionsPanel()
    {
        var panel = ThemedUIFactory.CreateGameplayPanel();
        panel.HorizontalAlignment = HorizontalAlignment.Stretch;
        
        var layout = new VerticalStackPanel
        {
            Spacing = ThemeManager.Spacing.Small
        };
        
        var titleLabel = ThemedUIFactory.CreateLabel("Visualization Options", ThemeManager.LabelTheme.Heading);
        layout.Widgets.Add(titleLabel);
        
        var movementRow = CreateCheckboxRow("Show Movement Animations", true, out _showMovementCheckbox);
        _showMovementCheckbox.PressedChanged += (s, a) => ShowMovementAnimations = _showMovementCheckbox?.IsPressed ?? true;
        layout.Widgets.Add(movementRow);
        
        var reinforcementRow = CreateCheckboxRow("Show Reinforcement Highlights", true, out _showReinforcementsCheckbox);
        _showReinforcementsCheckbox.PressedChanged += (s, a) => ShowReinforcementHighlights = _showReinforcementsCheckbox?.IsPressed ?? true;
        layout.Widgets.Add(reinforcementRow);
        
        var purchaseRow = CreateCheckboxRow("Show Purchase Indicators", true, out _showPurchasesCheckbox);
        _showPurchasesCheckbox.PressedChanged += (s, a) => ShowPurchaseIndicators = _showPurchasesCheckbox?.IsPressed ?? true;
        layout.Widgets.Add(purchaseRow);
        
        var followRow = CreateCheckboxRow("Auto-Follow AI Actions", true, out _autoFollowCheckbox);
        _autoFollowCheckbox.PressedChanged += (s, a) => AutoFollowAIActions = _autoFollowCheckbox?.IsPressed ?? true;
        layout.Widgets.Add(followRow);
        
        panel.Widgets.Add(layout);
        return panel;
    }
    
    private Widget BuildActivityLogPanel()
    {
        var panel = ThemedUIFactory.CreateGameplayPanel();
        panel.HorizontalAlignment = HorizontalAlignment.Stretch;
        
        var layout = new VerticalStackPanel
        {
            Spacing = ThemeManager.Spacing.Small
        };
        
        var titleLabel = ThemedUIFactory.CreateLabel("Activity Log", ThemeManager.LabelTheme.Heading);
        layout.Widgets.Add(titleLabel);
        
        _logContainer = new VerticalStackPanel
        {
            Spacing = ThemeManager.Spacing.XSmall
        };
        
        var emptyLabel = ThemedUIFactory.CreateSmallLabel("No recent activity");
        emptyLabel.TextColor = ThemeManager.Colors.TextSecondary;
        _logContainer.Widgets.Add(emptyLabel);
        
        layout.Widgets.Add(_logContainer);
        
        panel.Widgets.Add(layout);
        return panel;
    }
    
    private HorizontalStackPanel CreateCheckboxRow(string labelText, bool isChecked, out CheckButton checkbox)
    {
        var row = new HorizontalStackPanel
        {
            Spacing = ThemeManager.Spacing.Small
        };
        
        checkbox = ThemedUIFactory.CreateCheckButton(isChecked);
        row.Widgets.Add(checkbox);
        
        var label = ThemedUIFactory.CreateLabel(labelText);
        row.Widgets.Add(label);
        
        return row;
    }
    
    public void UpdateAIStatus(string aiPlayerName, bool isThinking)
    {
        if (_aiStatusLabel == null)
        {
            return;
        }

        if (isThinking)
        {
            _aiStatusLabel.Text = $"{aiPlayerName} is thinking...";
            _aiStatusLabel.TextColor = ThemeManager.Colors.TextWarning;
        }
        else
        {
            _aiStatusLabel.Text = "No AI activity";
            _aiStatusLabel.TextColor = ThemeManager.Colors.TextSecondary;
        }
    }
    
    public void UpdateCurrentAction(string action)
    {
        if (_currentActionLabel == null)
        {
            return;
        }

        _currentActionLabel.Text = action;
        _currentActionLabel.Visible = !string.IsNullOrEmpty(action);
    }
    
    public void LogActivity(string message)
    {
        _activityLog.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {message}");
        
        while (_activityLog.Count > MaxLogEntries)
        {
            _activityLog.RemoveAt(_activityLog.Count - 1);
        }
        
        RefreshLogDisplay();
    }
    
    private void RefreshLogDisplay()
    {
        if (_logContainer == null)
        {
            return;
        }

        _logContainer.Widgets.Clear();
        
        if (_activityLog.Count == 0)
        {
            var emptyLabel = ThemedUIFactory.CreateSmallLabel("No recent activity");
            emptyLabel.TextColor = ThemeManager.Colors.TextSecondary;
            _logContainer.Widgets.Add(emptyLabel);
        }
        else
        {
            foreach (var entry in _activityLog)
            {
                var entryLabel = ThemedUIFactory.CreateSmallLabel(entry);
                entryLabel.TextColor = ThemeManager.Colors.TextPrimary;
                _logContainer.Widgets.Add(entryLabel);
            }
        }
    }
    
    public void ClearLog()
    {
        _activityLog.Clear();
        RefreshLogDisplay();
    }
}
