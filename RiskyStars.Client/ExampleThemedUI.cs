using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using static RiskyStars.Client.ThemeManager;

namespace RiskyStars.Client;

/// <summary>
/// Example class demonstrating how to use the theme system to build UI.
/// This serves as a reference for developers creating new UI screens.
/// </summary>
public class ExampleThemedUI
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly int _screenWidth;
    private readonly int _screenHeight;
    
    private Desktop? _desktop;
    private Panel? _mainPanel;

    public ExampleThemedUI(GraphicsDevice graphicsDevice, int screenWidth, int screenHeight)
    {
        _graphicsDevice = graphicsDevice;
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
    }

    public void Initialize()
    {
        _desktop = new Desktop();
        BuildUI();
    }

    private void BuildUI()
    {
        // Example 1: Using ThemedUIFactory for quick widget creation
        BuildWithFactory();
        
        // Example 2: Using ThemeManager constants directly
        // BuildWithConstants();
        
        // Example 3: Applying themes to existing widgets
        // BuildWithThemeApplication();
    }

    /// <summary>
    /// Example 1: Using ThemedUIFactory (Recommended approach)
    /// </summary>
    private void BuildWithFactory()
    {
        // Create main container
        var mainLayout = ThemedUIFactory.CreateSpaciousVerticalStack();
        mainLayout.HorizontalAlignment = HorizontalAlignment.Center;
        mainLayout.VerticalAlignment = VerticalAlignment.Center;

        // Title section
        var titlePanel = ThemedUIFactory.CreateAccentFramePanel();
        titlePanel.Width = 600;
        var titleLayout = ThemedUIFactory.CreateVerticalStack();
        
        titleLayout.Widgets.Add(ThemedUIFactory.CreateTitleLabel("Example UI Screen"));
        titleLayout.Widgets.Add(ThemedUIFactory.CreateSecondaryLabel("Demonstrating the theme system"));
        
        titlePanel.Widgets.Add(titleLayout);
        mainLayout.Widgets.Add(titlePanel);

        // Resource display section
        var resourcePanel = ThemedUIFactory.CreateResourcePanel();
        resourcePanel.Width = 600;
        var resourceLayout = ThemedUIFactory.CreateVerticalStack();
        
        resourceLayout.Widgets.Add(ThemedUIFactory.CreateHeadingLabel("Resources"));
        
        // Population row
        var popRow = ThemedUIFactory.CreateHorizontalStack();
        popRow.Widgets.Add(ThemedUIFactory.CreatePopulationIcon());
        popRow.Widgets.Add(ThemedUIFactory.CreateLabel("Population: 1000"));
        popRow.Widgets.Add(ThemedUIFactory.CreateLabel("+10/turn", LabelTheme.Success));
        resourceLayout.Widgets.Add(popRow);
        
        // Metal row
        var metalRow = ThemedUIFactory.CreateHorizontalStack();
        metalRow.Widgets.Add(ThemedUIFactory.CreateMetalIcon());
        metalRow.Widgets.Add(ThemedUIFactory.CreateLabel("Metal: 500"));
        metalRow.Widgets.Add(ThemedUIFactory.CreateLabel("+5/turn", LabelTheme.Success));
        resourceLayout.Widgets.Add(metalRow);
        
        // Fuel row
        var fuelRow = ThemedUIFactory.CreateHorizontalStack();
        fuelRow.Widgets.Add(ThemedUIFactory.CreateFuelIcon());
        fuelRow.Widgets.Add(ThemedUIFactory.CreateLabel("Fuel: 250"));
        fuelRow.Widgets.Add(ThemedUIFactory.CreateLabel("-2/turn", LabelTheme.Warning));
        resourceLayout.Widgets.Add(fuelRow);
        
        resourcePanel.Widgets.Add(resourceLayout);
        mainLayout.Widgets.Add(resourcePanel);

        // Input form section
        var formPanel = ThemedUIFactory.CreateFramePanel();
        formPanel.Width = 600;
        var formLayout = ThemedUIFactory.CreateVerticalStack();
        
        formLayout.Widgets.Add(ThemedUIFactory.CreateHeadingLabel("Player Settings"));
        
        // Text input
        var nameLabel = ThemedUIFactory.CreateSecondaryLabel("Player Name:");
        formLayout.Widgets.Add(nameLabel);
        
        var nameInput = ThemedUIFactory.CreateTextBox("Commander", 400);
        formLayout.Widgets.Add(nameInput);
        
        // Dropdown
        var difficultyLabel = ThemedUIFactory.CreateSecondaryLabel("AI Difficulty:");
        formLayout.Widgets.Add(difficultyLabel);
        
        var difficultyCombo = ThemedUIFactory.CreateComboBox(400);
        difficultyCombo.Items.Add(new ListItem("Easy"));
        difficultyCombo.Items.Add(new ListItem("Medium"));
        difficultyCombo.Items.Add(new ListItem("Hard"));
        difficultyCombo.SelectedIndex = 1;
        formLayout.Widgets.Add(difficultyCombo);
        
        // Checkbox
        var checkboxRow = ThemedUIFactory.CreateHorizontalStack();
        var soundCheckbox = ThemedUIFactory.CreateCheckButton(true);
        checkboxRow.Widgets.Add(soundCheckbox);
        checkboxRow.Widgets.Add(ThemedUIFactory.CreateLabel("Enable Sound Effects"));
        formLayout.Widgets.Add(checkboxRow);
        
        formPanel.Widgets.Add(formLayout);
        mainLayout.Widgets.Add(formPanel);

        // Buttons section
        var buttonRow = ThemedUIFactory.CreateHorizontalStack();
        buttonRow.HorizontalAlignment = HorizontalAlignment.Center;
        
        buttonRow.Widgets.Add(ThemedUIFactory.CreateButton("Confirm", ButtonTheme.Success));
        buttonRow.Widgets.Add(ThemedUIFactory.CreateButton("Apply", ButtonTheme.Primary));
        buttonRow.Widgets.Add(ThemedUIFactory.CreateButton("Cancel", ButtonTheme.Danger));
        
        mainLayout.Widgets.Add(buttonRow);

        // AI badges example
        var badgeRow = ThemedUIFactory.CreateHorizontalStack();
        badgeRow.HorizontalAlignment = HorizontalAlignment.Center;
        badgeRow.Widgets.Add(ThemedUIFactory.CreateAIEasyBadge());
        badgeRow.Widgets.Add(ThemedUIFactory.CreateAIMediumBadge());
        badgeRow.Widgets.Add(ThemedUIFactory.CreateAIHardBadge());
        mainLayout.Widgets.Add(badgeRow);

        // Create root panel
        _mainPanel = new Panel
        {
            Width = _screenWidth,
            Height = _screenHeight,
            Background = ThemeManager.CreateSolidBrush(Colors.PrimaryDark)
        };
        _mainPanel.Widgets.Add(mainLayout);

        if (_desktop != null)
            _desktop.Root = _mainPanel;
    }

    /// <summary>
    /// Example 2: Using ThemeManager constants directly
    /// </summary>
    private void BuildWithConstants()
    {
        var grid = new Grid
        {
            RowSpacing = Spacing.Medium,
            ColumnSpacing = Spacing.Medium,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        grid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        grid.RowsProportions.Add(new Proportion(ProportionType.Auto));

        // Title label with manual styling
        var titleLabel = new Label
        {
            Text = "Using Constants",
            TextColor = Colors.TextAccent,
            Scale = FontScale.XXLarge,
            HorizontalAlignment = HorizontalAlignment.Center,
            GridRow = 0
        };
        grid.Widgets.Add(titleLabel);

        // Button with manual styling
        var button = new TextButton
        {
            Text = "Custom Button",
            Width = Sizes.ButtonMediumWidth,
            Height = Sizes.ButtonMediumHeight,
            Background = ThemeManager.CreateSolidBrush(Colors.AccentBlue),
            OverBackground = ThemeManager.CreateSolidBrush(Colors.HoverColor),
            PressedBackground = ThemeManager.CreateSolidBrush(Colors.PressedColor),
            Border = ThemeManager.CreateSolidBrush(Colors.BorderNormal),
            BorderThickness = new Thickness(BorderThickness.Normal),
            TextColor = Colors.TextPrimary,
            Padding = Padding.Button,
            GridRow = 1,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        grid.Widgets.Add(button);

        // Panel with manual styling
        var panel = new Panel
        {
            Width = 500,
            Height = 300,
            Background = ThemeManager.CreateSolidBrush(Colors.BackgroundDark),
            Border = ThemeManager.CreateSolidBrush(Colors.AccentCyan),
            BorderThickness = new Thickness(BorderThickness.Normal),
            Padding = Padding.Large
        };
        panel.Widgets.Add(grid);

        _mainPanel = new Panel
        {
            Width = _screenWidth,
            Height = _screenHeight,
            Background = ThemeManager.CreateSolidBrush(Colors.PrimaryDark)
        };
        _mainPanel.Widgets.Add(panel);

        if (_desktop != null)
            _desktop.Root = _mainPanel;
    }

    /// <summary>
    /// Example 3: Applying themes to existing widgets
    /// </summary>
    private void BuildWithThemeApplication()
    {
        var layout = new VerticalStackPanel
        {
            Spacing = Spacing.Medium,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        // Create widgets without styling
        var titleLabel = new Label { Text = "Theme Application Example" };
        var button1 = new TextButton { Text = "Primary Action" };
        var button2 = new TextButton { Text = "Dangerous Action" };
        var panel = new Panel { Width = 400, Height = 200 };

        // Apply themes after creation
        ThemeManager.ApplyLabelTheme(titleLabel, LabelTheme.Title);
        ThemeManager.ApplyButtonTheme(button1, ButtonTheme.Primary);
        ThemeManager.ApplyButtonTheme(button2, ButtonTheme.Danger);
        ThemeManager.ApplyPanelTheme(panel, PanelTheme.AccentFrame);

        // Add to layout
        layout.Widgets.Add(titleLabel);
        
        var buttonRow = new HorizontalStackPanel { Spacing = Spacing.Medium };
        buttonRow.Widgets.Add(button1);
        buttonRow.Widgets.Add(button2);
        layout.Widgets.Add(buttonRow);
        
        panel.Widgets.Add(layout);

        _mainPanel = new Panel
        {
            Width = _screenWidth,
            Height = _screenHeight,
            Background = ThemeManager.CreateSolidBrush(Colors.PrimaryDark)
        };
        _mainPanel.Widgets.Add(panel);

        if (_desktop != null)
            _desktop.Root = _mainPanel;
    }

    public void Update(GameTime gameTime)
    {
        // Handle input if needed
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        _desktop?.Render();
    }

    /// <summary>
    /// Example of creating a complete dialog window
    /// </summary>
    public static Panel CreateExampleDialog()
    {
        var dialogPanel = ThemedUIFactory.CreateAccentFramePanel();
        dialogPanel.Width = 450;
        dialogPanel.HorizontalAlignment = HorizontalAlignment.Center;
        dialogPanel.VerticalAlignment = VerticalAlignment.Center;

        var layout = ThemedUIFactory.CreateSpaciousVerticalStack();

        // Header
        var header = ThemedUIFactory.CreateSubtitleLabel("Confirm Purchase");
        header.HorizontalAlignment = HorizontalAlignment.Center;
        layout.Widgets.Add(header);

        // Message
        var message = ThemedUIFactory.CreateLabel("Do you want to purchase 10 armies?");
        message.HorizontalAlignment = HorizontalAlignment.Center;
        layout.Widgets.Add(message);

        // Cost breakdown
        var costPanel = ThemedUIFactory.CreateDarkPanel();
        var costLayout = ThemedUIFactory.CreateCompactVerticalStack();
        
        var popCost = ThemedUIFactory.CreateHorizontalStack();
        popCost.Widgets.Add(ThemedUIFactory.CreatePopulationIcon());
        popCost.Widgets.Add(ThemedUIFactory.CreateLabel("Population: 10"));
        costLayout.Widgets.Add(popCost);
        
        var metalCost = ThemedUIFactory.CreateHorizontalStack();
        metalCost.Widgets.Add(ThemedUIFactory.CreateMetalIcon());
        metalCost.Widgets.Add(ThemedUIFactory.CreateLabel("Metal: 30"));
        costLayout.Widgets.Add(metalCost);
        
        var fuelCost = ThemedUIFactory.CreateHorizontalStack();
        fuelCost.Widgets.Add(ThemedUIFactory.CreateFuelIcon());
        fuelCost.Widgets.Add(ThemedUIFactory.CreateLabel("Fuel: 10"));
        costLayout.Widgets.Add(fuelCost);
        
        costPanel.Widgets.Add(costLayout);
        layout.Widgets.Add(costPanel);

        // Buttons
        var buttonRow = ThemedUIFactory.CreateHorizontalStack();
        buttonRow.HorizontalAlignment = HorizontalAlignment.Center;
        
        var confirmBtn = ThemedUIFactory.CreateButton("Confirm", ButtonTheme.Success);
        var cancelBtn = ThemedUIFactory.CreateButton("Cancel", ButtonTheme.Danger);
        
        buttonRow.Widgets.Add(confirmBtn);
        buttonRow.Widgets.Add(cancelBtn);
        layout.Widgets.Add(buttonRow);

        dialogPanel.Widgets.Add(layout);
        return dialogPanel;
    }

    /// <summary>
    /// Example of creating a resource display widget
    /// </summary>
    public static Panel CreateResourceDisplay(string resourceName, int stockpile, int rate, Color iconColor)
    {
        var panel = ThemedUIFactory.CreateDarkPanel();
        var layout = ThemedUIFactory.CreateHorizontalStack();

        // Icon
        var icon = ThemedUIFactory.CreateResourceIcon(iconColor);
        layout.Widgets.Add(icon);

        // Resource info
        var infoLayout = ThemedUIFactory.CreateCompactVerticalStack();
        
        var nameLabel = ThemedUIFactory.CreateLabel(resourceName);
        infoLayout.Widgets.Add(nameLabel);
        
        var stockpileLabel = ThemedUIFactory.CreateSmallLabel($"Stockpile: {stockpile}");
        infoLayout.Widgets.Add(stockpileLabel);
        
        var rateLabel = rate >= 0
            ? ThemedUIFactory.CreateLabel($"+{rate}/turn", LabelTheme.Success)
            : ThemedUIFactory.CreateLabel($"{rate}/turn", LabelTheme.Warning);
        rateLabel.Scale = FontScale.Small;
        infoLayout.Widgets.Add(rateLabel);
        
        layout.Widgets.Add(infoLayout);
        panel.Widgets.Add(layout);
        
        return panel;
    }
}
