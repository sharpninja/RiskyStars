using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace RiskyStars.Client;

public sealed class CombatHudOverlay
{
    private readonly Panel _backdrop;
    private readonly Panel _window;
    private readonly Label _titleLabel;
    private readonly Label _locationLabel;
    private readonly Label _roundLabel;
    private readonly Label _statusLabel;
    private readonly Label _reinforcementLabel;
    private readonly Label _instructionsLabel;
    private readonly VerticalStackPanel _attackersStack;
    private readonly VerticalStackPanel _defendersStack;
    private readonly VerticalStackPanel _attackerRollsStack;
    private readonly VerticalStackPanel _defenderRollsStack;
    private readonly VerticalStackPanel _pairingsStack;
    private readonly VerticalStackPanel _casualtiesStack;
    private readonly VerticalStackPanel _survivorsStack;
    private readonly ScrollViewer _scrollViewer;

    public Widget Backdrop => _backdrop;
    public Widget Window => _window;

    public CombatHudOverlay(int screenWidth, int screenHeight)
    {
        _backdrop = new Panel
        {
            Background = ThemeManager.CreateSolidBrush(Color.Black * 0.72f),
            Visible = false,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        _window = ThemedUIFactory.CreateGameplayPanel(ThemeManager.Colors.TextError);
        _window.Visible = false;
        _window.HorizontalAlignment = HorizontalAlignment.Center;
        _window.VerticalAlignment = VerticalAlignment.Center;

        var contentStack = ThemedUIFactory.CreateVerticalStack(ThemeManager.Spacing.Small);

        _titleLabel = ThemedUIFactory.CreateTitleLabel(string.Empty);
        _titleLabel.TextColor = ThemeManager.Colors.TextError;
        _titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        contentStack.Widgets.Add(_titleLabel);

        _locationLabel = ThemedUIFactory.CreateSecondaryLabel(string.Empty);
        _locationLabel.TextColor = ThemeManager.Colors.TextPrimary;
        _locationLabel.HorizontalAlignment = HorizontalAlignment.Center;
        contentStack.Widgets.Add(_locationLabel);

        _roundLabel = ThemedUIFactory.CreateSmallLabel(string.Empty);
        _roundLabel.TextColor = ThemeManager.Colors.TextWarning;
        _roundLabel.HorizontalAlignment = HorizontalAlignment.Center;
        contentStack.Widgets.Add(_roundLabel);

        _statusLabel = ThemedUIFactory.CreateSmallLabel(string.Empty);
        _statusLabel.TextColor = ThemeManager.Colors.TextSecondary;
        _statusLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _statusLabel.Wrap = true;
        contentStack.Widgets.Add(_statusLabel);

        _reinforcementLabel = ThemedUIFactory.CreateSmallLabel(string.Empty);
        _reinforcementLabel.TextColor = ThemeManager.Colors.TextSuccess;
        _reinforcementLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _reinforcementLabel.Wrap = true;
        _reinforcementLabel.Visible = false;
        contentStack.Widgets.Add(_reinforcementLabel);

        var forcesGrid = ThemedUIFactory.CreateGrid(ThemeManager.Spacing.Small, ThemeManager.Spacing.Large);
        forcesGrid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        forcesGrid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));

        _attackersStack = CreateSectionStack("Attackers", ThemeManager.Colors.TextError);
        _attackersStack.GridColumn = 0;
        forcesGrid.Widgets.Add(_attackersStack);

        _defendersStack = CreateSectionStack("Defenders", ThemeManager.Colors.TextAccent);
        _defendersStack.GridColumn = 1;
        forcesGrid.Widgets.Add(_defendersStack);
        contentStack.Widgets.Add(forcesGrid);

        var rollsGrid = ThemedUIFactory.CreateGrid(ThemeManager.Spacing.Small, ThemeManager.Spacing.Large);
        rollsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        rollsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));

        _attackerRollsStack = CreateSectionStack("Attacker rolls", ThemeManager.Colors.TextError);
        _attackerRollsStack.GridColumn = 0;
        rollsGrid.Widgets.Add(_attackerRollsStack);

        _defenderRollsStack = CreateSectionStack("Defender rolls", ThemeManager.Colors.TextAccent);
        _defenderRollsStack.GridColumn = 1;
        rollsGrid.Widgets.Add(_defenderRollsStack);
        contentStack.Widgets.Add(rollsGrid);

        _pairingsStack = CreateSectionStack("Pairings", ThemeManager.Colors.TextWarning);
        contentStack.Widgets.Add(_pairingsStack);

        _casualtiesStack = CreateSectionStack("Casualties", ThemeManager.Colors.TextError);
        contentStack.Widgets.Add(_casualtiesStack);

        _survivorsStack = CreateSectionStack("Survivors", ThemeManager.Colors.TextSuccess);
        contentStack.Widgets.Add(_survivorsStack);

        _instructionsLabel = ThemedUIFactory.CreateSmallLabel(string.Empty);
        _instructionsLabel.TextColor = ThemeManager.Colors.TextSecondary;
        _instructionsLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _instructionsLabel.Wrap = true;
        contentStack.Widgets.Add(_instructionsLabel);

        _scrollViewer = ThemedUIFactory.CreateAutoScrollViewer(contentStack);
        _scrollViewer.HorizontalAlignment = HorizontalAlignment.Stretch;
        _scrollViewer.VerticalAlignment = VerticalAlignment.Stretch;
        _window.Widgets.Add(_scrollViewer);

        ResizeViewport(screenWidth, screenHeight);
    }

    public void ResizeViewport(int screenWidth, int screenHeight)
    {
        _backdrop.Width = screenWidth;
        _backdrop.Height = screenHeight;

        int windowWidth = Math.Min(ThemeManager.ScalePixels(920), Math.Max(ThemeManager.ScalePixels(620), screenWidth - ThemeManager.ScalePixels(120)));
        int windowHeight = Math.Min(ThemeManager.ScalePixels(720), Math.Max(ThemeManager.ScalePixels(420), screenHeight - ThemeManager.ScalePixels(120)));

        _window.Width = windowWidth;
        _window.Height = windowHeight;
        _scrollViewer.Height = Math.Max(ThemeManager.ScalePixels(320), windowHeight - ThemeManager.ScalePixels(80));
    }

    public void Update(CombatPresentation? presentation)
    {
        bool visible = presentation != null;
        _backdrop.Visible = visible;
        _window.Visible = visible;

        if (!visible || presentation == null)
        {
            return;
        }

        _titleLabel.Text = presentation.Title;
        _locationLabel.Text = $"Location: {presentation.Location}";
        _roundLabel.Text = presentation.Round;
        _statusLabel.Text = presentation.Status;
        _instructionsLabel.Text = presentation.Instructions;

        _reinforcementLabel.Visible = !string.IsNullOrWhiteSpace(presentation.ReinforcementMessage);
        _reinforcementLabel.Text = presentation.ReinforcementMessage ?? string.Empty;

        PopulateSection(_attackersStack, presentation.Attackers, ThemeManager.Colors.TextPrimary);
        PopulateSection(_defendersStack, presentation.Defenders, ThemeManager.Colors.TextPrimary);
        PopulateSection(_attackerRollsStack, presentation.AttackerRolls, ThemeManager.Colors.TextPrimary);
        PopulateSection(_defenderRollsStack, presentation.DefenderRolls, ThemeManager.Colors.TextPrimary);
        PopulateSection(_pairingsStack, presentation.Pairings, ThemeManager.Colors.TextPrimary);
        PopulateSection(_casualtiesStack, presentation.Casualties, ThemeManager.Colors.TextPrimary);
        PopulateSection(_survivorsStack, presentation.Survivors, ThemeManager.Colors.TextPrimary);
    }

    private static VerticalStackPanel CreateSectionStack(string title, Color titleColor)
    {
        var stack = ThemedUIFactory.CreateCompactVerticalStack();
        stack.Spacing = ThemeManager.Spacing.XSmall;

        var heading = ThemedUIFactory.CreateHeadingLabel(title);
        heading.TextColor = titleColor;
        stack.Widgets.Add(heading);

        return stack;
    }

    private static void PopulateSection(VerticalStackPanel stack, IReadOnlyList<string> lines, Color textColor)
    {
        while (stack.Widgets.Count > 1)
        {
            stack.Widgets.RemoveAt(stack.Widgets.Count - 1);
        }

        if (lines.Count == 0)
        {
            var emptyLabel = ThemedUIFactory.CreateSmallLabel("No activity yet.");
            emptyLabel.TextColor = ThemeManager.Colors.TextSecondary;
            stack.Widgets.Add(emptyLabel);
            return;
        }

        foreach (var line in lines)
        {
            var label = ThemedUIFactory.CreateSmallLabel(line);
            label.TextColor = textColor;
            label.Wrap = true;
            stack.Widgets.Add(label);
        }
    }
}
