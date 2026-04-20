using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace RiskyStars.Client;

public class TutorialWindow : DockableWindow
{
    private readonly Dictionary<Myra.Graphics2D.UI.Button, TutorialLesson> _lessonButtons = new();

    private TutorialLesson _selectedLesson = GameReferenceData.TutorialLessons[0];
    private string _recommendedLessonId = GameReferenceData.TutorialLessons[0].Id;
    private Label? _contextTitleLabel;
    private Label? _contextSummaryLabel;
    private Label? _contextFocusLabel;
    private VerticalStackPanel? _contextStepsStack;
    private Myra.Graphics2D.UI.Button? _suggestedLessonButton;
    private Label? _lessonTitleLabel;
    private Label? _lessonSummaryLabel;
    private VerticalStackPanel? _lessonStepsStack;
    private VerticalStackPanel? _lessonTipsStack;

    public TutorialWindow(WindowPreferences preferences, int screenWidth, int screenHeight)
        : base("tutorial", "In-Game Tutorial", preferences, screenWidth, screenHeight, 960, 660)
    {
        BuildContent();
        DockTo(DockPosition.BottomRight);
        if (_preferences.GetWindowState(_windowId) == null)
        {
            Hide();
        }
    }

    private void BuildContent()
    {
        var root = ThemedUIFactory.CreateVerticalStack(ThemeManager.Spacing.Medium);
        root.Widgets.Add(BuildCurrentGuidancePanel());

        var lowerGrid = ThemedUIFactory.CreateGrid(ThemeManager.Spacing.Medium, ThemeManager.Spacing.Medium);
        lowerGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        lowerGrid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));

        var lessonPanel = ThemedUIFactory.CreateGameplayPanel();
        lessonPanel.Width = ThemeManager.ScalePixels(230);
        lessonPanel.GridColumn = 0;
        lessonPanel.Widgets.Add(BuildLessonNavigator());
        lowerGrid.Widgets.Add(lessonPanel);

        var detailPanel = ThemedUIFactory.CreateGameplayPanel();
        detailPanel.GridColumn = 1;
        detailPanel.Widgets.Add(BuildLessonDetails());
        lowerGrid.Widgets.Add(detailPanel);

        root.Widgets.Add(lowerGrid);
        _window.Content = root;
        UpdateLessonView();
    }

    private Widget BuildCurrentGuidancePanel()
    {
        var panel = ThemedUIFactory.CreateGameplayPanel(ThemeManager.Colors.TextAccent);
        var stack = ThemedUIFactory.CreateVerticalStack(ThemeManager.Spacing.Small);

        var heading = ThemedUIFactory.CreateHeadingLabel("Current Guidance");
        heading.TextColor = ThemeManager.Colors.TextAccent;
        stack.Widgets.Add(heading);

        _contextTitleLabel = ThemedUIFactory.CreateSecondaryLabel(string.Empty);
        _contextTitleLabel.TextColor = ThemeManager.Colors.TextPrimary;
        stack.Widgets.Add(_contextTitleLabel);

        _contextSummaryLabel = ThemedUIFactory.CreateSmallLabel(string.Empty);
        _contextSummaryLabel.Wrap = true;
        stack.Widgets.Add(_contextSummaryLabel);

        _contextFocusLabel = ThemedUIFactory.CreateSmallLabel(string.Empty);
        _contextFocusLabel.TextColor = ThemeManager.Colors.TextWarning;
        _contextFocusLabel.Wrap = true;
        stack.Widgets.Add(_contextFocusLabel);

        _suggestedLessonButton = ThemedUIFactory.CreateButton("Open Suggested Lesson", ThemeManager.ScalePixels(190), ThemeManager.Sizes.ButtonSmallHeight, ThemeManager.ButtonTheme.Primary);
        _suggestedLessonButton.Click += (s, a) => OpenRecommendedLesson();
        stack.Widgets.Add(_suggestedLessonButton);

        _contextStepsStack = ThemedUIFactory.CreateCompactVerticalStack();
        _contextStepsStack.Spacing = ThemeManager.Spacing.XSmall;
        stack.Widgets.Add(_contextStepsStack);

        panel.Widgets.Add(stack);
        return panel;
    }

    private Widget BuildLessonNavigator()
    {
        var stack = ThemedUIFactory.CreateCompactVerticalStack();
        stack.Spacing = ThemeManager.Spacing.Small;

        var heading = ThemedUIFactory.CreateHeadingLabel("Lessons");
        heading.TextColor = ThemeManager.Colors.TextAccent;
        stack.Widgets.Add(heading);

        var navContent = ThemedUIFactory.CreateCompactVerticalStack();
        navContent.Spacing = ThemeManager.Spacing.XSmall;

        foreach (var categoryGroup in GameReferenceData.TutorialLessons.GroupBy(lesson => lesson.Category))
        {
            var categoryLabel = ThemedUIFactory.CreateSmallLabel(categoryGroup.Key);
            categoryLabel.TextColor = ThemeManager.Colors.TextWarning;
            navContent.Widgets.Add(categoryLabel);

            foreach (var lesson in categoryGroup)
            {
                var button = ThemedUIFactory.CreateButton(lesson.Title, ThemeManager.ScalePixels(200), ThemeManager.Sizes.ButtonSmallHeight, ThemeManager.ButtonTheme.Default);
                button.HorizontalAlignment = HorizontalAlignment.Stretch;
                button.Click += (s, a) => SelectLesson(lesson);
                _lessonButtons[button] = lesson;
                navContent.Widgets.Add(button);
            }

            navContent.Widgets.Add(ThemedUIFactory.CreateHorizontalSeparator());
        }

        stack.Widgets.Add(ThemedUIFactory.CreateAutoScrollViewer(navContent));
        return stack;
    }

    private Widget BuildLessonDetails()
    {
        var content = ThemedUIFactory.CreateVerticalStack(ThemeManager.Spacing.Small);

        _lessonTitleLabel = ThemedUIFactory.CreateTitleLabel(string.Empty);
        _lessonTitleLabel.TextColor = ThemeManager.Colors.TextAccent;
        content.Widgets.Add(_lessonTitleLabel);

        _lessonSummaryLabel = ThemedUIFactory.CreateSecondaryLabel(string.Empty);
        _lessonSummaryLabel.Wrap = true;
        content.Widgets.Add(_lessonSummaryLabel);

        content.Widgets.Add(ThemedUIFactory.CreateHorizontalSeparator());

        var stepsHeading = ThemedUIFactory.CreateHeadingLabel("Steps");
        stepsHeading.TextColor = ThemeManager.Colors.TextWarning;
        content.Widgets.Add(stepsHeading);

        _lessonStepsStack = ThemedUIFactory.CreateCompactVerticalStack();
        _lessonStepsStack.Spacing = ThemeManager.Spacing.XSmall;
        content.Widgets.Add(_lessonStepsStack);

        content.Widgets.Add(ThemedUIFactory.CreateHorizontalSeparator());

        var tipsHeading = ThemedUIFactory.CreateHeadingLabel("Tips");
        tipsHeading.TextColor = ThemeManager.Colors.TextWarning;
        content.Widgets.Add(tipsHeading);

        _lessonTipsStack = ThemedUIFactory.CreateCompactVerticalStack();
        _lessonTipsStack.Spacing = ThemeManager.Spacing.XSmall;
        content.Widgets.Add(_lessonTipsStack);

        return ThemedUIFactory.CreateAutoScrollViewer(content);
    }

    public void UpdateContent(GameStateCache? gameStateCache, string? currentPlayerId, SelectionState? selection, bool combatActive)
    {
        if (_contextTitleLabel == null || _contextSummaryLabel == null || _contextFocusLabel == null || _contextStepsStack == null)
        {
            return;
        }

        var context = GameReferenceData.BuildTutorialContext(gameStateCache, currentPlayerId, selection, combatActive);
        _recommendedLessonId = context.RecommendedLessonId;
        _contextTitleLabel.Text = context.Title;
        _contextSummaryLabel.Text = context.Summary;
        _contextFocusLabel.Text = context.Focus;
        if (_suggestedLessonButton != null)
        {
            var suggested = GameReferenceData.TutorialLessons.FirstOrDefault(lesson => lesson.Id == _recommendedLessonId);
            if (_suggestedLessonButton.Content is Label label)
            {
                label.Text = suggested == null ? "Open Suggested Lesson" : $"Open: {suggested.Title}";
            }
        }

        _contextStepsStack.Widgets.Clear();
        foreach (var step in context.NextSteps)
        {
            _contextStepsStack.Widgets.Add(CreateBulletLabel(step));
        }
    }

    private void SelectLesson(TutorialLesson lesson)
    {
        _selectedLesson = lesson;
        UpdateLessonView();
    }

    private void UpdateLessonView()
    {
        if (_lessonTitleLabel == null || _lessonSummaryLabel == null || _lessonStepsStack == null || _lessonTipsStack == null)
        {
            return;
        }

        _lessonTitleLabel.Text = _selectedLesson.Title;
        _lessonSummaryLabel.Text = _selectedLesson.Summary;

        _lessonStepsStack.Widgets.Clear();
        foreach (var step in _selectedLesson.Steps)
        {
            _lessonStepsStack.Widgets.Add(CreateBulletLabel(step));
        }

        _lessonTipsStack.Widgets.Clear();
        foreach (var tip in _selectedLesson.Tips)
        {
            _lessonTipsStack.Widgets.Add(CreateBulletLabel(tip));
        }

        foreach (var pair in _lessonButtons)
        {
            ThemeManager.ApplyButtonTheme(pair.Key, pair.Value.Id == _selectedLesson.Id ? ThemeManager.ButtonTheme.Primary : ThemeManager.ButtonTheme.Default);
        }
    }

    private static Label CreateBulletLabel(string text)
    {
        var label = ThemedUIFactory.CreateSmallLabel($"- {text}");
        label.Wrap = true;
        label.TextColor = ThemeManager.Colors.TextPrimary;
        return label;
    }

    private void OpenRecommendedLesson()
    {
        var recommended = GameReferenceData.TutorialLessons.FirstOrDefault(lesson => lesson.Id == _recommendedLessonId);
        if (recommended == null)
        {
            return;
        }

        _selectedLesson = recommended;
        UpdateLessonView();
    }
}
