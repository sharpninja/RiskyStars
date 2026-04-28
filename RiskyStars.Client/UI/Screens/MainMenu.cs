using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D.UI;
using MyraButton = Myra.Graphics2D.UI.Button;

namespace RiskyStars.Client;

public enum MainMenuState
{
    Main,
    Settings,
    Connecting
}

public class MainMenu
{
    private int _screenWidth;
    private int _screenHeight;
    private Settings _settings;

    private Desktop? _desktop;
    private DialogManager? _dialogManager;
    private MainMenuState _state = MainMenuState.Main;

    private Panel? _mainMenuPanel;
    private MyraButton? _connectButton;
    private MyraButton? _settingsButton;
    private MyraButton? _exitButton;
    private MyraButton? _tutorialButton;

    private Panel? _settingsPanel;
    private ValidatedTextBox? _serverAddressTextBox;
#pragma warning disable CS0618
    private ComboBox? _resolutionComboBox;
    private ComboBox? _windowModeComboBox;
    private ComboBox? _themeAccentComboBox = null;
    private ComboBox? _themeWarningComboBox = null;
    private ComboBox? _themeFontStyleComboBox = null;
#pragma warning restore CS0618
    private HorizontalSlider? _uiScaleSlider;
    private Label? _uiScaleValueLabel;
    private HorizontalSlider? _themeFontScaleSlider = null;
    private HorizontalSlider? _themePaddingSlider = null;
    private HorizontalSlider? _themeFramePaddingSlider = null;
    private HorizontalSlider? _themeContrastSlider = null;
    private Label? _themeFontScaleValueLabel = null;
    private Label? _themePaddingValueLabel = null;
    private Label? _themeFramePaddingValueLabel = null;
    private Label? _themeContrastValueLabel = null;
    private MyraButton? _saveSettingsButton;
    private MyraButton? _backButton;

    private Panel? _connectingPanel;

    private sealed class SettingsViewDraft
    {
        public string ServerAddress { get; init; } = string.Empty;
        public string AccentColor { get; init; } = UiThemeSettings.AccentColorOptions[0];
        public string WarningColor { get; init; } = UiThemeSettings.WarningColorOptions[0];
        public string FontStyle { get; init; } = UiThemeSettings.FontStyleOptions[1];
        public int UiScalePercent { get; init; } = 100;
        public int FontScalePercent { get; init; } = 100;
        public int PaddingScalePercent { get; init; } = 100;
        public int FramePaddingPercent { get; init; } = 100;
        public int ContrastPercent { get; init; } = 100;
    }

    public bool ShouldConnect { get; private set; }
    public bool ShouldStartSinglePlayer { get; private set; }
    public bool ShouldStartTutorial { get; private set; }
    public bool ShouldExit { get; private set; }
    public Settings Settings => _settings;
    public MainMenuState State => _state;

    public MainMenu(GraphicsDevice graphicsDevice, int screenWidth, int screenHeight, Settings settings)
    {
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
        _settings = settings;
    }

    public void LoadContent(SpriteFont font)
    {
        _desktop = new Desktop();
        ThemeManager.ApplyThemeSettings(_settings);
        ThemeManager.ApplyDesktopTheme(_desktop);
        _dialogManager = new DialogManager(_desktop);

        BuildMainMenuUI();
        BuildSettingsUI();
        BuildConnectingUI();
        ShowMainMenuUI();
    }

    private void BuildMainMenuUI()
    {
        int frameWidth = ThemedUIFactory.ResolveResponsiveExtent(_screenWidth, 120, 1080);
        int frameHeight = ThemedUIFactory.ResolveResponsiveExtent(_screenHeight, 100, 620);
        int narrativeWidth = Math.Max(420, frameWidth - 420);
        var frame = ThemedUIFactory.CreateViewportFrame(frameWidth, frameHeight);
        frame.HorizontalAlignment = HorizontalAlignment.Center;
        frame.VerticalAlignment = VerticalAlignment.Center;

        var layout = ThemedUIFactory.CreateGrid(ThemeManager.Spacing.Large, ThemeManager.Spacing.Large);
        layout.HorizontalAlignment = HorizontalAlignment.Stretch;
        layout.VerticalAlignment = VerticalAlignment.Stretch;
        layout.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        layout.ColumnsProportions.Add(new Proportion(ProportionType.Pixels, 280));
        layout.RowsProportions.Add(new Proportion(ProportionType.Auto));
        layout.RowsProportions.Add(new Proportion(ProportionType.Fill));
        layout.RowsProportions.Add(new Proportion(ProportionType.Auto));

        var headerStack = ThemedUIFactory.CreateCompactVerticalStack();
        headerStack.Spacing = ThemeManager.Spacing.XSmall;
        headerStack.GridRow = 0;
        headerStack.GridColumnSpan = 2;

        var titleLabel = ThemedUIFactory.CreateTitleLabel("RiskyStars");
        titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        headerStack.Widgets.Add(titleLabel);

        var subtitleLabel = ThemedUIFactory.CreateSubtitleLabel("Fleet command interface");
        subtitleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        subtitleLabel.TextColor = ThemeManager.Colors.TextPrimary;
        headerStack.Widgets.Add(subtitleLabel);
        layout.Widgets.Add(headerStack);

        var narrativeStack = ThemedUIFactory.CreateVerticalStack(ThemeManager.Spacing.Small);
        narrativeStack.GridRow = 1;
        narrativeStack.GridColumn = 0;
        narrativeStack.Margin = ThemeManager.Padding.Medium;
        var overline = ThemedUIFactory.CreateSmallLabel("SECTOR BRIEF");
        overline.TextColor = ThemeManager.Colors.TextWarning;
        narrativeStack.Widgets.Add(overline);

        var headline = ThemedUIFactory.CreateTitleLabel("Chart a system. Build a faction. Risk the stars.");
        headline.Wrap = true;
        headline.Width = narrativeWidth - 32;
        narrativeStack.Widgets.Add(headline);

        var summary = ThemedUIFactory.CreateSecondaryLabel("A framed command deck for multiplayer campaigns, fast single-player setup, and in-game ship-console tooling.");
        summary.Wrap = true;
        summary.Width = narrativeWidth - 44;
        summary.TextColor = ThemeManager.Colors.TextPrimary;
        narrativeStack.Widgets.Add(summary);

        var featureStack = ThemedUIFactory.CreateVerticalStack(ThemeManager.Spacing.Small);
        featureStack.Widgets.Add(CreateMenuBullet("Multiplayer lobbies with a shared command shell.", narrativeWidth - 36));
        featureStack.Widgets.Add(CreateMenuBullet("Single-player lineup builder with AI command slots.", narrativeWidth - 36));
        featureStack.Widgets.Add(CreateMenuBullet("Dockable in-game windows styled as one console family.", narrativeWidth - 36));
        narrativeStack.Widgets.Add(featureStack);
        layout.Widgets.Add(narrativeStack);

        var commandPanel = ThemedUIFactory.CreateConsolePanel();
        commandPanel.GridRow = 1;
        commandPanel.GridColumn = 1;

        var commandStack = ThemedUIFactory.CreateVerticalStack(ThemeManager.Spacing.Small);
        var commandHeading = ThemedUIFactory.CreateHeadingLabel("Command Actions");
        commandStack.Widgets.Add(commandHeading);

        _connectButton = ThemedUIFactory.CreateButton("Multiplayer", 236, ThemeManager.Sizes.ButtonMediumHeight, ThemeManager.ButtonTheme.Primary);
        _connectButton.Click += (_, _) => OnConnectClicked();
        commandStack.Widgets.Add(_connectButton);

        var singlePlayerButton = ThemedUIFactory.CreateButton("Single Player", 236, ThemeManager.Sizes.ButtonMediumHeight, ThemeManager.ButtonTheme.Primary);
        singlePlayerButton.Click += (_, _) => OnSinglePlayerClicked();
        commandStack.Widgets.Add(singlePlayerButton);

        _tutorialButton = ThemedUIFactory.CreateButton("Tutorial Mode", 236, ThemeManager.Sizes.ButtonMediumHeight, ThemeManager.ButtonTheme.Hero);
        _tutorialButton.Click += (_, _) => OnTutorialClicked();
        commandStack.Widgets.Add(_tutorialButton);

        _settingsButton = ThemedUIFactory.CreateButton("Settings", 236, ThemeManager.Sizes.ButtonMediumHeight, ThemeManager.ButtonTheme.Default);
        _settingsButton.Click += (_, _) => OnSettingsClicked();
        commandStack.Widgets.Add(_settingsButton);

        _exitButton = ThemedUIFactory.CreateButton("Exit", 236, ThemeManager.Sizes.ButtonMediumHeight, ThemeManager.ButtonTheme.Danger);
        _exitButton.Click += (_, _) => OnExitClicked();
        commandStack.Widgets.Add(_exitButton);

        commandPanel.Widgets.Add(commandStack);
        layout.Widgets.Add(commandPanel);

        var footerGrid = ThemedUIFactory.CreateGrid(0, ThemeManager.Spacing.Large);
        footerGrid.GridRow = 2;
        footerGrid.GridColumnSpan = 2;
        footerGrid.Margin = ThemeManager.Padding.SmallVertical;
        footerGrid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        footerGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));

        var endpointLabel = ThemedUIFactory.CreateSmallLabel($"Default uplink: {_settings.ServerAddress}");
        endpointLabel.TextColor = ThemeManager.Colors.TextSecondary;
        endpointLabel.GridColumn = 0;
        footerGrid.Widgets.Add(endpointLabel);

        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        var versionLabel = ThemedUIFactory.CreateSmallLabel($"Build {version?.Major}.{version?.Minor}.{version?.Build}-{version?.Revision}");
        versionLabel.TextColor = ThemeManager.Colors.TextSecondary;
        versionLabel.GridColumn = 1;
        footerGrid.Widgets.Add(versionLabel);
        layout.Widgets.Add(footerGrid);

        frame.Widgets.Add(layout);

        _mainMenuPanel = ThemedUIFactory.CreateScreenRoot(_screenWidth, _screenHeight);
        _mainMenuPanel.Widgets.Add(frame);
    }

    private void BuildSettingsUI()
    {
        int frameWidth = ThemedUIFactory.ResolveResponsiveExtent(_screenWidth, 140, 1040);
        int frameHeight = ThemedUIFactory.ResolveResponsiveExtent(_screenHeight, 120, 760);
        int contentWidth = frameWidth - 96;

        var frame = ThemedUIFactory.CreateViewportFrame(frameWidth, frameHeight);
        frame.HorizontalAlignment = HorizontalAlignment.Center;
        frame.VerticalAlignment = VerticalAlignment.Center;

        var layout = ThemedUIFactory.CreateGrid(ThemeManager.Spacing.Large, 0);
        layout.RowsProportions.Add(new Proportion(ProportionType.Auto));
        layout.RowsProportions.Add(new Proportion(ProportionType.Fill));
        layout.RowsProportions.Add(new Proportion(ProportionType.Auto));

        var header = ThemedUIFactory.CreateHeaderPlate("Command Settings", "Display, server, session, and theme controls");
        header.GridRow = 0;
        layout.Widgets.Add(header);

        var settingsContent = BuildSettingsContent(contentWidth);
        var scrollViewer = ThemedUIFactory.CreateAutoScrollViewer(settingsContent, frameHeight - 260);
        scrollViewer.GridRow = 1;
        layout.Widgets.Add(scrollViewer);

        var actions = ThemedUIFactory.CreateActionBar();
        actions.HorizontalAlignment = HorizontalAlignment.Center;

        _saveSettingsButton = ThemedUIFactory.CreateButton("Save Settings", 220, ThemeManager.Sizes.ButtonMediumHeight, ThemeManager.ButtonTheme.Primary);
        _saveSettingsButton.Click += (_, _) => OnSaveSettingsClicked();
        actions.Widgets.Add(_saveSettingsButton);

        _backButton = ThemedUIFactory.CreateButton("Back", 180, ThemeManager.Sizes.ButtonMediumHeight, ThemeManager.ButtonTheme.Default);
        _backButton.Click += (_, _) => OnBackClicked();
        actions.Widgets.Add(_backButton);

        actions.GridRow = 2;
        layout.Widgets.Add(actions);

        frame.Widgets.Add(layout);

        _settingsPanel = ThemedUIFactory.CreateScreenRoot(_screenWidth, _screenHeight);
        _settingsPanel.Widgets.Add(frame);
    }

    private VerticalStackPanel BuildSettingsContent(int contentWidth)
    {
        var contentStack = ThemedUIFactory.CreateVerticalStack(ThemeManager.Spacing.Large);
        contentStack.Width = contentWidth;

        var cards = ThemedUIFactory.CreateGrid(ThemeManager.Spacing.Large, ThemeManager.Spacing.Large);
        cards.Width = contentWidth;
        cards.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        cards.ColumnsProportions.Add(new Proportion(ProportionType.Fill));

        int cardWidth = (contentWidth - ThemeManager.Spacing.Large) / 2;

        _serverAddressTextBox = new ValidatedTextBox(cardWidth - 40, Settings.Load().ServerAddress, showErrorLabel: true);
        _serverAddressTextBox.Text = _settings.ServerAddress;
        _serverAddressTextBox.SetValidator(InputValidator.ValidateServerAddress);
        var serverCard = ThemedUIFactory.CreateFieldCard("Server Endpoint", "Used as the default multiplayer uplink.", _serverAddressTextBox.Container, cardWidth);
        serverCard.GridColumn = 0;
        cards.Widgets.Add(serverCard);

        int displayInnerWidth = cardWidth - (int)(ThemeManager.Padding.Medium.Left + ThemeManager.Padding.Medium.Right);
        int displayFieldWidth = Math.Max(220, (displayInnerWidth - ThemeManager.Spacing.Medium) / 2);
        int displayControlWidth = Math.Min(cardWidth - 40, Math.Max(180, displayFieldWidth - 12));
        int displaySliderWidth = Math.Min(260, Math.Max(180, displayFieldWidth - 72));

#pragma warning disable CS0618
        _resolutionComboBox = ThemedUIFactory.CreateComboBox(displayControlWidth);
#pragma warning restore CS0618
        foreach (var resolution in Settings.GetResolutionOptions($"{_settings.ResolutionWidth}x{_settings.ResolutionHeight}"))
        {
#pragma warning disable CS0618
            _resolutionComboBox.Items.Add(new ListItem(resolution));
#pragma warning restore CS0618
        }

        var currentResolution = $"{_settings.ResolutionWidth}x{_settings.ResolutionHeight}";
        var selectedIndex = Array.IndexOf(Settings.SupportedResolutions, currentResolution);
        _resolutionComboBox.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;

        var displayGrid = ThemedUIFactory.CreateGrid(ThemeManager.Spacing.Small, ThemeManager.Spacing.Medium);
        displayGrid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        displayGrid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));

        var resolutionField = CreateThemeSelectField("Resolution", "Choose the backbuffer size used in windowed or fullscreen mode.", _resolutionComboBox);
        resolutionField.Width = displayFieldWidth;
        resolutionField.GridColumn = 0;
        displayGrid.Widgets.Add(resolutionField);

        _windowModeComboBox = CreateSettingsComboBox(Settings.WindowModeOptions, displayControlWidth);
        var windowModeField = CreateThemeSelectField("Window Mode", "Switch between a resizable window and fullscreen output.", _windowModeComboBox);
        windowModeField.Width = displayFieldWidth;
        windowModeField.GridColumn = 1;
        displayGrid.Widgets.Add(windowModeField);

        var uiScaleField = CreateThemeSliderField("UI Scale", "Scales Myra menus, panels, buttons, and window chrome.", 80, 160, displaySliderWidth, out _uiScaleSlider, out _uiScaleValueLabel);
        uiScaleField.GridRow = 1;
        uiScaleField.GridColumnSpan = 2;
        displayGrid.Widgets.Add(uiScaleField);

        var displayCard = ThemedUIFactory.CreateFieldCard("Display Profile", "Choose the monitor profile for the command deck.", displayGrid, cardWidth);
        displayCard.GridColumn = 1;
        cards.Widgets.Add(displayCard);

        contentStack.Widgets.Add(cards);
        contentStack.Widgets.Add(BuildThemeSettingsCard(contentWidth));
        HookThemeSlider(_uiScaleSlider, _uiScaleValueLabel);
        SyncDisplayControlsFromSettings();
        return contentStack;
    }

    private Panel BuildThemeSettingsCard(int contentWidth)
    {
        var panel = ThemedUIFactory.CreateConsolePanel();
        panel.Width = contentWidth;

        var stack = ThemedUIFactory.CreateVerticalStack(ThemeManager.Spacing.Medium);
        var heading = ThemedUIFactory.CreateHeadingLabel("Visual Palette");
        stack.Widgets.Add(heading);

        var description = ThemedUIFactory.CreateSmallLabel("Accent colors live here. Geometry and scale are unified across gameplay and Myra surfaces, so there are no separate spacing or chrome controls.");
        description.Wrap = true;
        description.TextColor = ThemeManager.Colors.TextPrimary;
        stack.Widgets.Add(description);

        int panelInnerWidth = contentWidth - (int)(ThemeManager.Padding.Large.Left + ThemeManager.Padding.Large.Right);
        int themeFieldWidth = Math.Max(240, (panelInnerWidth - ThemeManager.Spacing.Large) / 2);
        int themeControlWidth = Math.Min(320, Math.Max(190, themeFieldWidth - 20));

        stack.Widgets.Add(CreateThemeSectionHeader("Palette"));

        var paletteGrid = ThemedUIFactory.CreateGrid(ThemeManager.Spacing.Small, ThemeManager.Spacing.Large);
        paletteGrid.Width = panelInnerWidth;
        paletteGrid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        paletteGrid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));

        _themeAccentComboBox = CreateSettingsComboBox(UiThemeSettings.AccentColorOptions, themeControlWidth);
        var accentField = CreateThemeSelectField("Accent Color", "Primary action, selection, and focus color.", _themeAccentComboBox);
        accentField.Width = themeFieldWidth;
        accentField.GridColumn = 0;
        paletteGrid.Widgets.Add(accentField);

        _themeWarningComboBox = CreateSettingsComboBox(UiThemeSettings.WarningColorOptions, themeControlWidth);
        var warningField = CreateThemeSelectField("Warning Tone", "Headings, warnings, and secondary data highlights.", _themeWarningComboBox);
        warningField.Width = themeFieldWidth;
        warningField.GridColumn = 1;
        paletteGrid.Widgets.Add(warningField);

        stack.Widgets.Add(paletteGrid);
        panel.Widgets.Add(stack);

        SyncThemeControlsFromSettings();
        return panel;
    }

    private Widget CreateThemeSectionHeader(string title)
    {
        var titleLabel = ThemedUIFactory.CreateHeadingLabel(title);
        titleLabel.TextColor = ThemeManager.Colors.TextWarning;
        return titleLabel;
    }

    private Widget CreateThemeSelectField(string title, string description, Widget control)
    {
        var stack = ThemedUIFactory.CreateCompactVerticalStack();
        stack.Spacing = ThemeManager.Spacing.Small;

        var titleLabel = ThemedUIFactory.CreateSmallLabel(title);
        titleLabel.TextColor = ThemeManager.Colors.TextWarning;
        stack.Widgets.Add(titleLabel);

        var descriptionLabel = ThemedUIFactory.CreateSmallLabel(description);
        descriptionLabel.TextColor = ThemeManager.Colors.TextSecondary;
        descriptionLabel.Wrap = true;
        stack.Widgets.Add(descriptionLabel);
        stack.Widgets.Add(control);

        return stack;
    }

    private Widget CreateThemeSliderField(string title, string description, int min, int max, int sliderWidth, out HorizontalSlider slider, out Label valueLabel)
    {
        var stack = ThemedUIFactory.CreateCompactVerticalStack();
        stack.Spacing = ThemeManager.Spacing.Small;

        var titleLabel = ThemedUIFactory.CreateSmallLabel(title);
        titleLabel.TextColor = ThemeManager.Colors.TextWarning;
        stack.Widgets.Add(titleLabel);

        var descriptionLabel = ThemedUIFactory.CreateSmallLabel(description);
        descriptionLabel.TextColor = ThemeManager.Colors.TextSecondary;
        descriptionLabel.Wrap = true;
        stack.Widgets.Add(descriptionLabel);

        var sliderGrid = ThemedUIFactory.CreateGrid(0, ThemeManager.Spacing.Medium);
        sliderGrid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        sliderGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));

        slider = new HorizontalSlider
        {
            Minimum = min,
            Maximum = max,
            Value = min,
            Width = sliderWidth,
            VerticalAlignment = VerticalAlignment.Center
        };
        slider.GridColumn = 0;
        sliderGrid.Widgets.Add(slider);

        valueLabel = ThemedUIFactory.CreateSmallLabel($"{min}%");
        valueLabel.TextColor = ThemeManager.Colors.TextPrimary;
        valueLabel.GridColumn = 1;
        valueLabel.VerticalAlignment = VerticalAlignment.Center;
        sliderGrid.Widgets.Add(valueLabel);

        stack.Widgets.Add(sliderGrid);
        return stack;
    }

    private static void HookThemeSlider(HorizontalSlider? slider, Label? valueLabel)
    {
        if (slider == null || valueLabel == null)
        {
            return;
        }

        slider.ValueChanged += (_, _) => valueLabel.Text = $"{(int)Math.Round(slider.Value)}%";
    }

    private static void SetComboSelection(ComboBox? comboBox, string[] options, string selectedValue)
    {
        if (comboBox == null)
        {
            return;
        }

        int selectedIndex = Array.IndexOf(options, selectedValue);
        comboBox.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
    }

    private static ComboBox CreateSettingsComboBox(string[] options, int width)
    {
#pragma warning disable CS0618
        var comboBox = ThemedUIFactory.CreateComboBox(width);
        foreach (var option in options)
        {
            comboBox.Items.Add(new ListItem(option));
        }

        return comboBox;
#pragma warning restore CS0618
    }

    private void SyncThemeControlsFromSettings()
    {
        var theme = _settings.Theme;
        theme.Normalize();

        SetComboSelection(_themeAccentComboBox, UiThemeSettings.AccentColorOptions, theme.AccentColor);
        SetComboSelection(_themeWarningComboBox, UiThemeSettings.WarningColorOptions, theme.WarningColor);
        SetComboSelection(_themeFontStyleComboBox, UiThemeSettings.FontStyleOptions, theme.FontStyle);

        if (_themeFontScaleSlider != null)
        {
            _themeFontScaleSlider.Value = theme.FontScalePercent;
        }

        if (_themePaddingSlider != null)
        {
            _themePaddingSlider.Value = theme.PaddingScalePercent;
        }

        if (_themeFramePaddingSlider != null)
        {
            _themeFramePaddingSlider.Value = theme.FramePaddingPercent;
        }

        if (_themeContrastSlider != null)
        {
            _themeContrastSlider.Value = theme.ContrastPercent;
        }

        if (_themeFontScaleValueLabel != null)
        {
            _themeFontScaleValueLabel.Text = $"{theme.FontScalePercent}%";
        }

        if (_themePaddingValueLabel != null)
        {
            _themePaddingValueLabel.Text = $"{theme.PaddingScalePercent}%";
        }

        if (_themeFramePaddingValueLabel != null)
        {
            _themeFramePaddingValueLabel.Text = $"{theme.FramePaddingPercent}%";
        }

        if (_themeContrastValueLabel != null)
        {
            _themeContrastValueLabel.Text = $"{theme.ContrastPercent}%";
        }
    }

    private void SyncDisplayControlsFromSettings()
    {
        if (_resolutionComboBox != null)
        {
            var currentResolution = $"{_settings.ResolutionWidth}x{_settings.ResolutionHeight}";
            EnsureResolutionOptionPresent(_resolutionComboBox, currentResolution);
#pragma warning disable CS0618
            var resolutionOptions = _resolutionComboBox.Items.Select(item => item.Text).ToList();
#pragma warning restore CS0618
            var selectedIndex = resolutionOptions.FindIndex(option => string.Equals(option, currentResolution, StringComparison.OrdinalIgnoreCase));
            _resolutionComboBox.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
        }

        SetComboSelection(_windowModeComboBox, Settings.WindowModeOptions, Settings.GetWindowModeOption(_settings.WindowMode));

        if (_uiScaleSlider != null)
        {
            _uiScaleSlider.Value = _settings.UiScalePercent;
        }

        if (_uiScaleValueLabel != null)
        {
            _uiScaleValueLabel.Text = $"{_settings.UiScalePercent}%";
        }
    }

    private static void EnsureResolutionOptionPresent(ComboBox comboBox, string resolution)
    {
        if (string.IsNullOrWhiteSpace(resolution))
        {
            return;
        }

#pragma warning disable CS0618
        bool exists = comboBox.Items.Any(item => string.Equals(item.Text, resolution, StringComparison.OrdinalIgnoreCase));
        if (!exists)
        {
            comboBox.Items.Insert(0, new ListItem(resolution));
        }
#pragma warning restore CS0618
    }

    private void ApplyThemeSelectionsToSettings()
    {
        _settings.Theme.AccentColor = _themeAccentComboBox?.SelectedItem?.Text ?? _settings.Theme.AccentColor;
        _settings.Theme.WarningColor = _themeWarningComboBox?.SelectedItem?.Text ?? _settings.Theme.WarningColor;
        _settings.Theme.FontStyle = _themeFontStyleComboBox?.SelectedItem?.Text ?? _settings.Theme.FontStyle;
        _settings.Theme.FontScalePercent = (int)Math.Round(_themeFontScaleSlider?.Value ?? _settings.Theme.FontScalePercent);
        _settings.Theme.PaddingScalePercent = (int)Math.Round(_themePaddingSlider?.Value ?? _settings.Theme.PaddingScalePercent);
        _settings.Theme.FramePaddingPercent = (int)Math.Round(_themeFramePaddingSlider?.Value ?? _settings.Theme.FramePaddingPercent);
        _settings.Theme.ContrastPercent = (int)Math.Round(_themeContrastSlider?.Value ?? _settings.Theme.ContrastPercent);
        _settings.Theme.Normalize();
    }

    private void ApplyDisplaySelectionsToSettings()
    {
        if (_resolutionComboBox?.SelectedItem != null)
        {
            var selectedResolution = _resolutionComboBox.SelectedItem.Text;
            if (!string.IsNullOrEmpty(selectedResolution))
            {
                var parts = selectedResolution.Split('x');
                if (parts.Length == 2 &&
                    int.TryParse(parts[0], out int width) &&
                    int.TryParse(parts[1], out int height))
                {
                    _settings.ResolutionWidth = width;
                    _settings.ResolutionHeight = height;
                }
            }
        }

        _settings.WindowMode = Settings.ParseWindowModeOption(_windowModeComboBox?.SelectedItem?.Text);
        _settings.UiScalePercent = (int)Math.Round(_uiScaleSlider?.Value ?? _settings.UiScalePercent);
        _settings.Normalize();
    }

    private void BuildConnectingUI()
    {
        int frameWidth = ThemedUIFactory.ResolveResponsiveExtent(_screenWidth, 240, 700);
        int frameHeight = ThemedUIFactory.ResolveResponsiveExtent(_screenHeight, 180, 420);
        var frame = ThemedUIFactory.CreateViewportFrame(frameWidth, frameHeight);
        frame.HorizontalAlignment = HorizontalAlignment.Center;
        frame.VerticalAlignment = VerticalAlignment.Center;

        var stack = ThemedUIFactory.CreateVerticalStack(ThemeManager.Spacing.Large);
        stack.Widgets.Add(ThemedUIFactory.CreateHeaderPlate("Establishing Uplink", "Preparing the next command surface"));

        var messagePanel = ThemedUIFactory.CreateConsolePanel();
        var messageStack = ThemedUIFactory.CreateVerticalStack(ThemeManager.Spacing.Small);
        var headline = ThemedUIFactory.CreateHeadingLabel("Connecting to the sector network...");
        headline.HorizontalAlignment = HorizontalAlignment.Center;
        messageStack.Widgets.Add(headline);

        var subtitle = ThemedUIFactory.CreateSecondaryLabel("The multiplayer lobby browser will appear when the uplink is ready.");
        subtitle.HorizontalAlignment = HorizontalAlignment.Center;
        subtitle.Wrap = true;
        subtitle.Width = Math.Max(320, frameWidth - 220);
        messageStack.Widgets.Add(subtitle);

        messagePanel.Widgets.Add(messageStack);
        stack.Widgets.Add(messagePanel);

        frame.Widgets.Add(ThemedUIFactory.CreateAutoScrollViewer(stack, frameHeight - 96));

        _connectingPanel = ThemedUIFactory.CreateScreenRoot(_screenWidth, _screenHeight);
        _connectingPanel.Widgets.Add(frame);
    }

    private static Widget CreateMenuBullet(string text, int width)
    {
        var row = ThemedUIFactory.CreateHorizontalStack(ThemeManager.Spacing.Small);

        var tag = ThemedUIFactory.CreateSmallLabel("SYS");
        tag.TextColor = ThemeManager.Colors.TextAccent;
        row.Widgets.Add(tag);

        var label = ThemedUIFactory.CreateSecondaryLabel(text);
        label.TextColor = ThemeManager.Colors.TextPrimary;
        label.Wrap = true;
        label.Width = Math.Max(280, width);
        row.Widgets.Add(label);
        return row;
    }

    private void ShowMainMenuUI()
    {
        if (_desktop != null)
        {
            _desktop.Root = _mainMenuPanel;
        }
    }

    private void ShowSettingsUI()
    {
        if (_desktop == null || _settingsPanel == null)
        {
            return;
        }

        if (_serverAddressTextBox != null)
        {
            _serverAddressTextBox.Text = _settings.ServerAddress;
            _serverAddressTextBox.ValidateInput();
        }

        if (_resolutionComboBox != null)
        {
            SyncDisplayControlsFromSettings();
        }

        SyncThemeControlsFromSettings();

        _desktop.Root = _settingsPanel;
    }

    private void ShowConnectingUI()
    {
        if (_desktop != null)
        {
            _desktop.Root = _connectingPanel;
        }
    }

    public void SetState(MainMenuState state)
    {
        _state = state;
        UpdateUI();
    }

    public void ResizeViewport(int screenWidth, int screenHeight)
    {
        if (screenWidth <= 0 || screenHeight <= 0)
        {
            return;
        }

        var settingsDraft = CaptureSettingsViewDraft();
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;

        BuildMainMenuUI();
        BuildSettingsUI();
        BuildConnectingUI();
        RestoreSettingsViewDraft(settingsDraft);
        UpdateUI();
    }

    public void ShowError(string message)
    {
        _dialogManager?.ShowError("Connection Error", message, _ =>
        {
            _state = MainMenuState.Main;
            UpdateUI();
        });
    }

    private void UpdateUI()
    {
        switch (_state)
        {
            case MainMenuState.Main:
                ShowMainMenuUI();
                break;
            case MainMenuState.Settings:
                ShowSettingsUI();
                break;
            case MainMenuState.Connecting:
                ShowConnectingUI();
                break;
        }
    }

    private void OnConnectClicked()
    {
        ShouldConnect = true;
        _state = MainMenuState.Connecting;
        UpdateUI();
    }

    private void OnSinglePlayerClicked()
    {
        ShouldStartSinglePlayer = true;
        _state = MainMenuState.Connecting;
        UpdateUI();
    }

    private void OnTutorialClicked()
    {
        ShouldStartTutorial = true;
        _state = MainMenuState.Connecting;
        UpdateUI();
    }

    private void OnSettingsClicked()
    {
        _state = MainMenuState.Settings;
        UpdateUI();
    }

    private void OnExitClicked()
    {
        ShouldExit = true;
    }

    public void ResetNavigationRequests()
    {
        ShouldConnect = false;
        ShouldStartSinglePlayer = false;
        ShouldStartTutorial = false;
    }

    private void OnSaveSettingsClicked()
    {
        if (_serverAddressTextBox != null && !_serverAddressTextBox.IsValid)
        {
            _dialogManager?.ShowError("Validation Error", "Please fix the server address before saving.");
            return;
        }

        if (_serverAddressTextBox != null)
        {
            _settings.ServerAddress = _serverAddressTextBox.Text.Trim();
        }

        ApplyDisplaySelectionsToSettings();
        ApplyThemeSelectionsToSettings();
        ThemeManager.ApplyThemeSettings(_settings);
        _settings.Save();
        BuildMainMenuUI();
        BuildSettingsUI();
        BuildConnectingUI();
        _state = MainMenuState.Main;
        UpdateUI();
    }

    private void OnBackClicked()
    {
        if (_serverAddressTextBox != null)
        {
            _serverAddressTextBox.Text = _settings.ServerAddress;
            _serverAddressTextBox.ClearValidation();
        }

        SyncDisplayControlsFromSettings();
        SyncThemeControlsFromSettings();

        _state = MainMenuState.Main;
        UpdateUI();
    }

    public void Update(GameTime gameTime)
    {
        _dialogManager?.Update();
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        _desktop?.Render();
    }

    private SettingsViewDraft CaptureSettingsViewDraft()
    {
        return new SettingsViewDraft
        {
            ServerAddress = _serverAddressTextBox?.Text ?? _settings.ServerAddress,
            AccentColor = _themeAccentComboBox?.SelectedItem?.Text ?? _settings.Theme.AccentColor,
            WarningColor = _themeWarningComboBox?.SelectedItem?.Text ?? _settings.Theme.WarningColor,
            FontStyle = _themeFontStyleComboBox?.SelectedItem?.Text ?? _settings.Theme.FontStyle,
            UiScalePercent = (int)Math.Round(_uiScaleSlider?.Value ?? _settings.UiScalePercent),
            FontScalePercent = (int)Math.Round(_themeFontScaleSlider?.Value ?? _settings.Theme.FontScalePercent),
            PaddingScalePercent = (int)Math.Round(_themePaddingSlider?.Value ?? _settings.Theme.PaddingScalePercent),
            FramePaddingPercent = (int)Math.Round(_themeFramePaddingSlider?.Value ?? _settings.Theme.FramePaddingPercent),
            ContrastPercent = (int)Math.Round(_themeContrastSlider?.Value ?? _settings.Theme.ContrastPercent)
        };
    }

    private void RestoreSettingsViewDraft(SettingsViewDraft draft)
    {
        if (_serverAddressTextBox != null)
        {
            _serverAddressTextBox.Text = draft.ServerAddress;
        }

        SetComboSelection(_themeAccentComboBox, UiThemeSettings.AccentColorOptions, draft.AccentColor);
        SetComboSelection(_themeWarningComboBox, UiThemeSettings.WarningColorOptions, draft.WarningColor);
        SetComboSelection(_themeFontStyleComboBox, UiThemeSettings.FontStyleOptions, draft.FontStyle);

        if (_uiScaleSlider != null)
        {
            _uiScaleSlider.Value = draft.UiScalePercent;
        }

        if (_uiScaleValueLabel != null)
        {
            _uiScaleValueLabel.Text = $"{draft.UiScalePercent}%";
        }

        if (_themeFontScaleSlider != null)
        {
            _themeFontScaleSlider.Value = draft.FontScalePercent;
        }

        if (_themeFontScaleValueLabel != null)
        {
            _themeFontScaleValueLabel.Text = $"{draft.FontScalePercent}%";
        }

        if (_themePaddingSlider != null)
        {
            _themePaddingSlider.Value = draft.PaddingScalePercent;
        }

        if (_themePaddingValueLabel != null)
        {
            _themePaddingValueLabel.Text = $"{draft.PaddingScalePercent}%";
        }

        if (_themeFramePaddingSlider != null)
        {
            _themeFramePaddingSlider.Value = draft.FramePaddingPercent;
        }

        if (_themeFramePaddingValueLabel != null)
        {
            _themeFramePaddingValueLabel.Text = $"{draft.FramePaddingPercent}%";
        }

        if (_themeContrastSlider != null)
        {
            _themeContrastSlider.Value = draft.ContrastPercent;
        }

        if (_themeContrastValueLabel != null)
        {
            _themeContrastValueLabel.Text = $"{draft.ContrastPercent}%";
        }
    }
}
