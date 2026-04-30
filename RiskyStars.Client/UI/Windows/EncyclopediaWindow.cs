using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace RiskyStars.Client;

public class EncyclopediaWindow : DockableWindow
{
    private readonly Dictionary<Myra.Graphics2D.UI.Button, EncyclopediaArticle> _articleButtons = new();

    private EncyclopediaArticle _selectedArticle = GameReferenceData.EncyclopediaArticles[0];
    private Label? _titleLabel;
    private Label? _summaryLabel;
    private VerticalStackPanel? _detailsStack;
    private VerticalStackPanel? _commandsStack;

    public EncyclopediaWindow(WindowPreferences preferences, int screenWidth, int screenHeight)
        : base("encyclopedia", "In-Game Encyclopedia", preferences, screenWidth, screenHeight, 920, 620)
    {
        bool hasSavedState = _preferences.GetWindowState(_windowId) != null;
        BuildContent();
        DockTo(DockPosition.Left);
        if (!hasSavedState)
        {
            Hide();
        }
    }

    private void BuildContent()
    {
        var root = ThemedUIFactory.CreateGrid(ThemeManager.Spacing.Medium, ThemeManager.Spacing.Medium);
        root.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        root.ColumnsProportions.Add(new Proportion(ProportionType.Fill));

        var navPanel = ThemedUIFactory.CreateGameplayPanel();
        navPanel.Width = ThemeManager.ScalePixels(230);
        navPanel.GridColumn = 0;
        navPanel.Widgets.Add(BuildArticleNavigator());
        root.Widgets.Add(navPanel);

        var contentPanel = ThemedUIFactory.CreateGameplayPanel();
        contentPanel.GridColumn = 1;
        contentPanel.Widgets.Add(BuildArticleContent());
        root.Widgets.Add(contentPanel);

        _window.Content = root;
        UpdateArticleView();
    }

    private Widget BuildArticleNavigator()
    {
        var stack = ThemedUIFactory.CreateGrid(ThemeManager.Spacing.Small, 0);
        stack.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        stack.RowsProportions.Add(new Proportion(ProportionType.Auto));
        stack.RowsProportions.Add(new Proportion(ProportionType.Fill));

        var heading = ThemedUIFactory.CreateHeadingLabel("Articles");
        heading.GridRow = 0;
        heading.TextColor = ThemeManager.Colors.TextAccent;
        stack.Widgets.Add(heading);

        var navContent = ThemedUIFactory.CreateCompactVerticalStack();
        navContent.Spacing = ThemeManager.Spacing.XSmall;

        foreach (var categoryGroup in GameReferenceData.EncyclopediaArticles.GroupBy(article => article.Category))
        {
            var categoryLabel = ThemedUIFactory.CreateSmallLabel(categoryGroup.Key);
            categoryLabel.TextColor = ThemeManager.Colors.TextWarning;
            navContent.Widgets.Add(categoryLabel);

            foreach (var article in categoryGroup)
            {
                var button = ThemedUIFactory.CreateButton(article.Title, ThemeManager.ScalePixels(200), ThemeManager.Sizes.ButtonSmallHeight, ThemeManager.ButtonTheme.Default);
                button.HorizontalAlignment = HorizontalAlignment.Stretch;
                button.Click += (s, a) => SelectArticle(article);
                _articleButtons[button] = article;
                navContent.Widgets.Add(button);
            }

            navContent.Widgets.Add(ThemedUIFactory.CreateHorizontalSeparator());
        }

        var navScroller = ThemedUIFactory.CreateAutoScrollViewer(navContent);
        navScroller.GridRow = 1;
        stack.Widgets.Add(navScroller);
        return stack;
    }

    private Widget BuildArticleContent()
    {
        var content = ThemedUIFactory.CreateVerticalStack(ThemeManager.Spacing.Small);

        _titleLabel = ThemedUIFactory.CreateTitleLabel(string.Empty);
        _titleLabel.TextColor = ThemeManager.Colors.TextAccent;
        content.Widgets.Add(_titleLabel);

        _summaryLabel = ThemedUIFactory.CreateSecondaryLabel(string.Empty);
        _summaryLabel.Wrap = true;
        content.Widgets.Add(_summaryLabel);

        content.Widgets.Add(ThemedUIFactory.CreateHorizontalSeparator());

        var detailsHeading = ThemedUIFactory.CreateHeadingLabel("Key Points");
        detailsHeading.TextColor = ThemeManager.Colors.TextWarning;
        content.Widgets.Add(detailsHeading);

        _detailsStack = ThemedUIFactory.CreateCompactVerticalStack();
        _detailsStack.Spacing = ThemeManager.Spacing.XSmall;
        content.Widgets.Add(_detailsStack);

        content.Widgets.Add(ThemedUIFactory.CreateHorizontalSeparator());

        var commandsHeading = ThemedUIFactory.CreateHeadingLabel("Useful Commands");
        commandsHeading.TextColor = ThemeManager.Colors.TextWarning;
        content.Widgets.Add(commandsHeading);

        _commandsStack = ThemedUIFactory.CreateCompactVerticalStack();
        _commandsStack.Spacing = ThemeManager.Spacing.XSmall;
        content.Widgets.Add(_commandsStack);

        return ThemedUIFactory.CreateAutoScrollViewer(content);
    }

    private void SelectArticle(EncyclopediaArticle article)
    {
        _selectedArticle = article;
        UpdateArticleView();
    }

    private void UpdateArticleView()
    {
        if (_titleLabel == null || _summaryLabel == null || _detailsStack == null || _commandsStack == null)
        {
            return;
        }

        _titleLabel.Text = _selectedArticle.Title;
        _summaryLabel.Text = _selectedArticle.Summary;

        _detailsStack.Widgets.Clear();
        foreach (var point in _selectedArticle.KeyPoints)
        {
            _detailsStack.Widgets.Add(CreateBulletLabel(point));
        }

        _commandsStack.Widgets.Clear();
        foreach (var command in _selectedArticle.Commands)
        {
            _commandsStack.Widgets.Add(CreateBulletLabel(command));
        }

        foreach (var pair in _articleButtons)
        {
            ThemeManager.ApplyButtonTheme(pair.Key, pair.Value.Id == _selectedArticle.Id ? ThemeManager.ButtonTheme.Primary : ThemeManager.ButtonTheme.Default);
        }
    }

    private static Label CreateBulletLabel(string text)
    {
        var label = ThemedUIFactory.CreateSmallLabel($"- {text}");
        label.Wrap = true;
        label.TextColor = ThemeManager.Colors.TextPrimary;
        return label;
    }
}
