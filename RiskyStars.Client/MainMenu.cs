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
    private readonly int _screenWidth;
    private readonly int _screenHeight;
    private Settings _settings;

    private Desktop? _desktop;
    private DialogManager? _dialogManager;
    private MainMenuState _state = MainMenuState.Main;

    private Panel? _mainMenuPanel;
    private MyraButton? _connectButton;
    private MyraButton? _settingsButton;
    private MyraButton? _exitButton;

    private Panel? _settingsPanel;
    private ValidatedTextBox? _serverAddressTextBox;
#pragma warning disable CS0618
    private ComboBox? _resolutionComboBox;
    private ComboBox? _themeAccentComboBox;
    private ComboBox? _themeWarningComboBox;
    private ComboBox? _themeFontStyleComboBox;
#pragma warning restore CS0618
    private CheckButton? _fullscreenCheckButton;
    private HorizontalSlider? _themeFontScaleSlider;
    private HorizontalSlider? _themePaddingSlider;
    private HorizontalSlider? _themeFramePaddingSlider;
    private HorizontalSlider? _themeContrastSlider;
    private Label? _themeFontScaleValueLabel;
    private Label? _themePaddingValueLabel;
    private Label? _themeFramePaddingValueLabel;
    private Label? _themeContrastValueLabel;
    private MyraButton? _saveSettingsButton;
    private MyraButton? _backButton;

    private Panel? _connectingPanel;

    public bool ShouldConnect { get; private set; }
    public bool ShouldStartSinglePlayer { get; private set; }
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
        ThemeManager.ApplyThemeSettings(_settings.Theme);
        ThemeManager.ApplyDesktopTheme(_desktop);
        _dialogManager = new DialogManager(_desktop);

        BuildMainMenuUI();
        BuildSettingsUI();
        BuildConnectingUI();
        ShowMainMenuUI();
    }

    private void BuildMainMenuUI()
    {
        int frameWidth = Math.Min(_screenWidth - 120, 1160);
        int frameHeight = Math.Min(_screenHeight - 120, 700);
        var frame = ThemedUIFactory.CreateViewportFrame(frameWidth, frameHeight);
        frame.HorizontalAlignment = HorizontalAlignment.Center;
        frame.VerticalAlignment = VerticalAlignment.Center;

        var layout = ThemedUIFactory.CreateGrid(ThemeManager.Spacing.Large, ThemeManager.Spacing.XXLarge);
        layout.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        layout.ColumnsProportions.Add(new Proportion(ProportionType.Pixels, 330));
        layout.RowsProportions.Add(new Proportion(ProportionType.Auto));
        layout.RowsProportions.Add(new Proportion(ProportionType.Fill));
        layout.RowsProportions.Add(new Proportion(ProportionType.Auto));

        var header = ThemedUIFactory.CreateHeaderPlate("RiskyStars", "Fleet command interface");
        header.GridRow = 0;
        header.GridColumnSpan = 2;
        layout.Widgets.Add(header);

        var narrativePanel = ThemedUIFactory.CreateConsolePanel();
        narrativePanel.GridRow = 1;
        narrativePanel.GridColumn = 0;

        var narrativeStack = ThemedUIFactory.CreateVerticalStack(ThemeManager.Spacing.Medium);
        var overline = ThemedUIFactory.CreateSmallLabel("SECTOR BRIEF");
        overline.TextColor = ThemeManager.Colors.TextWarning;
        narrativeStack.Widgets.Add(overline);

        var headline = ThemedUIFactory.CreateTitleLabel("Chart a system. Build a faction. Risk the stars.");
        headline.Wrap = true;
        headline.Width = 540;
        narrativeStack.Widgets.Add(headline);

        var summary = ThemedUIFactory.CreateSecondaryLabel("A framed command deck for multiplayer campaigns, fast single-player setup, and in-game ship-console tooling.");
        summary.Wrap = true;
        summary.Width = 520;
        summary.TextColor = ThemeManager.Colors.TextPrimary;
        narrativeStack.Widgets.Add(summary);

        var featureStack = ThemedUIFactory.CreateVerticalStack(ThemeManager.Spacing.Small);
        featureStack.Widgets.Add(CreateMenuBullet("Multiplayer lobbies with a shared command shell."));
        featureStack.Widgets.Add(CreateMenuBullet("Single-player lineup builder with AI command slots."));
        featureStack.Widgets.Add(CreateMenuBullet("Dockable in-game windows styled as one console family."));
        narrativeStack.Widgets.Add(featureStack);

        narrativePanel.Widgets.Add(narrativeStack);
        layout.Widgets.Add(narrativePanel);

        var commandPanel = ThemedUIFactory.CreateFramePanel();
        commandPanel.GridRow = 1;
        commandPanel.GridColumn = 1;

        var commandStack = ThemedUIFactory.CreateVerticalStack(ThemeManager.Spacing.Medium);
        var commandHeading = ThemedUIFactory.CreateHeadingLabel("Command Actions");
        commandStack.Widgets.Add(commandHeading);

        _connectButton = ThemedUIFactory.CreateLargeButton("Multiplayer", ThemeManager.ButtonTheme.Primary);
        _connectButton.Click += (_, _) => OnConnectClicked();
        commandStack.Widgets.Add(_connectButton);

        var singlePlayerButton = ThemedUIFactory.CreateLargeButton("Single Player", ThemeManager.ButtonTheme.Primary);
        singlePlayerButton.Click += (_, _) => OnSinglePlayerClicked();
        commandStack.Widgets.Add(singlePlayerButton);

        _settingsButton = ThemedUIFactory.CreateLargeButton("Settings", ThemeManager.ButtonTheme.Default);
        _settingsButton.Click += (_, _) => OnSettingsClicked();
        commandStack.Widgets.Add(_settingsButton);

        _exitButton = ThemedUIFactory.CreateLargeButton("Exit", ThemeManager.ButtonTheme.Danger);
        _exitButton.Click += (_, _) => OnExitClicked();
        commandStack.Widgets.Add(_exitButton);

        commandPanel.Widgets.Add(commandStack);
        layout.Widgets.Add(commandPanel);

        var footer = ThemedUIFactory.CreateConsolePanel();
        footer.GridRow = 2;
        footer.GridColumnSpan = 2;

        var footerGrid = ThemedUIFactory.CreateGrid(0, ThemeManager.Spacing.Large);
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

        footer.Widgets.Add(footerGrid);
        layout.Widgets.Add(footer);

        var scrollViewer = ThemedUIFactory.CreateAutoScrollViewer(layout, frameHeight - 96);
        frame.Widgets.Add(scrollViewer);

        _mainMenuPanel = ThemedUIFactory.CreateScreenRoot(_screenWidth, _screenHeight);
        _mainMenuPanel.Widgets.Add(frame);
    }

    private void BuildSettingsUI()
    {
        int frameWidth = Math.Min(_screenWidth - 140, 1040);
        int frameHeight = Math.Min(_screenHeight - 120, 760);
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

#pragma warning disable CS0618
        _resolutionComboBox = ThemedUIFactory.CreateComboBox(cardWidth - 40);
#pragma warning restore CS0618
        var resolutions = new List<string> { "1280x720", "1366x768", "1920x1080", "2560x1440", "3840x2160" };
        foreach (var resolution in resolutions)
        {
#pragma warning disable CS0618
            _resolutionComboBox.Items.Add(new ListItem(resolution));
#pragma warning restore CS0618
        }

        var currentResolution = $"{_settings.ResolutionWidth}x{_settings.ResolutionHeight}";
        var selectedIndex = resolutions.IndexOf(currentResolution);
        _resolutionComboBox.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;

        var displayStack = ThemedUIFactory.CreateVerticalStack(ThemeManager.Spacing.Small);
        displayStack.Widgets.Add(_resolutionComboBox);

        var fullscreenRow = ThemedUIFactory.CreateHorizontalStack(ThemeManager.Spacing.Small);
        _fullscreenCheckButton = ThemedUIFactory.CreateCheckButton(_settings.Fullscreen);
        fullscreenRow.Widgets.Add(_fullscreenCheckButton);
        fullscreenRow.Widgets.Add(ThemedUIFactory.CreateLabel("Fullscreen output"));
        displayStack.Widgets.Add(fullscreenRow);

        var displayCard = ThemedUIFactory.CreateFieldCard("Display Profile", "Choose the monitor profile for the command deck.", displayStack, cardWidth);
        displayCard.GridColumn = 1;
        cards.Widgets.Add(displayCard);

        contentStack.Widgets.Add(cards);
        contentStack.Widgets.Add(BuildThemeSettingsCard(contentWidth));
        return contentStack;
    }

    private Panel BuildThemeSettingsCard(int contentWidth)
    {
        var panel = ThemedUIFactory.CreateFramePanel();
        panel.Width = contentWidth;

        var stack = ThemedUIFactory.CreateVerticalStack(ThemeManager.Spacing.Medium);
        var heading = ThemedUIFactory.CreateHeadingLabel("Theme Console");
        stack.Widgets.Add(heading);

        var description = ThemedUIFactory.CreateSmallLabel("Adjust accent colors, font profile, scale, contrast, and padding. Panel contents are scrollable so the command deck can grow without clipping.");
        description.Wrap = true;
        description.TextColor = ThemeManager.Colors.TextPrimary;
        stack.Widgets.Add(description);

        var columns = ThemedUIFactory.CreateGrid(ThemeManager.Spacing.Large, ThemeManager.Spacing.Large);
        columns.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        columns.ColumnsProportions.Add(new Proportion(ProportionType.Fill));

        int columnWidth = (contentWidth - (ThemeManager.Spacing.Large + ThemeManager.Padding.Panel.Left + ThemeManager.Padding.Panel.Right)) / 2;

        var palettePanel = ThemedUIFactory.CreateDarkPanel();
        palettePanel.Width = columnWidth;
        palettePanel.GridColumn = 0;

        var paletteStack = ThemedUIFactory.CreateVerticalStack(ThemeManager.Spacing.Medium);
        paletteStack.Widgets.Add(ThemedUIFactory.CreateHeadingLabel("Palette"));

        _themeAccentComboBox = CreateSettingsComboBox(UiThemeSettings.AccentColorOptions, 220);
        paletteStack.Widgets.Add(CreateThemeSelectField("Accent Color", "Primary action, selection, and focus color.", _themeAccentComboBox));

        _themeWarningComboBox = CreateSettingsComboBox(UiThemeSettings.WarningColorOptions, 220);
        paletteStack.Widgets.Add(CreateThemeSelectField("Warning Tone", "Headings, warnings, and secondary data highlights.", _themeWarningComboBox));
        palettePanel.Widgets.Add(paletteStack);
        columns.Widgets.Add(palettePanel);

        var metricsPanel = ThemedUIFactory.CreateDarkPanel();
        metricsPanel.Width = columnWidth;
        metricsPanel.GridColumn = 1;

        var metricsStack = ThemedUIFactory.CreateVerticalStack(ThemeManager.Spacing.Medium);
        metricsStack.Widgets.Add(ThemedUIFactory.CreateHeadingLabel("Typography & Spacing"));

        _themeFontStyleComboBox = CreateSettingsComboBox(UiThemeSettings.FontStyleOptions, 220);
        metricsStack.Widgets.Add(CreateThemeSelectField("Font Profile", "Compact, neutral, or heavier command-deck typography.", _themeFontStyleComboBox));

        metricsStack.Widgets.Add(CreateThemeSliderField("Font Size", "Scales all Myra text.", 80, 140, out _themeFontScaleSlider, out _themeFontScaleValueLabel));
        metricsStack.Widgets.Add(CreateThemeSliderField("Panel Padding", "Adjusts general spacing inside fields and consoles.", 80, 150, out _themePaddingSlider, out _themePaddingValueLabel));
        metricsStack.Widgets.Add(CreateThemeSliderField("Frame Padding", "Controls the chrome margin around framed screens.", 70, 140, out _themeFramePaddingSlider, out _themeFramePaddingValueLabel));
        metricsStack.Widgets.Add(CreateThemeSliderField("Contrast", "Brightens text and accents against the dark panels.", 85, 140, out _themeContrastSlider, out _themeContrastValueLabel));

        metricsPanel.Widgets.Add(metricsStack);
        columns.Widgets.Add(metricsPanel);

        stack.Widgets.Add(columns);
        panel.Widgets.Add(stack);

        HookThemeSlider(_themeFontScaleSlider, _themeFontScaleValueLabel);
        HookThemeSlider(_themePaddingSlider, _themePaddingValueLabel);
        HookThemeSlider(_themeFramePaddingSlider, _themeFramePaddingValueLabel);
        HookThemeSlider(_themeContrastSlider, _themeContrastValueLabel);

        SyncThemeControlsFromSettings();
        return panel;
    }

    private Panel CreateThemeSelectField(string title, string description, Widget control)
    {
        var field = ThemedUIFactory.CreateFramePanel();
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

        field.Widgets.Add(stack);
        return field;
    }

    private Panel CreateThemeSliderField(string title, string description, int min, int max, out HorizontalSlider slider, out Label valueLabel)
    {
        var field = ThemedUIFactory.CreateFramePanel();
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
            Width = 220,
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
        field.Widgets.Add(stack);
        return field;
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

    private void BuildConnectingUI()
    {
        int frameHeight = Math.Min(_screenHeight - 180, 420);
        var frame = ThemedUIFactory.CreateViewportFrame(Math.Min(_screenWidth - 240, 700), frameHeight);
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
        subtitle.Width = 440;
        messageStack.Widgets.Add(subtitle);

        messagePanel.Widgets.Add(messageStack);
        stack.Widgets.Add(messagePanel);

        frame.Widgets.Add(ThemedUIFactory.CreateAutoScrollViewer(stack, frameHeight - 96));

        _connectingPanel = ThemedUIFactory.CreateScreenRoot(_screenWidth, _screenHeight);
        _connectingPanel.Widgets.Add(frame);
    }

    private static Widget CreateMenuBullet(string text)
    {
        var row = ThemedUIFactory.CreateHorizontalStack(ThemeManager.Spacing.Small);
        row.Widgets.Add(ThemedUIFactory.CreateStatusBadge(ThemeManager.Colors.TextAccent, "SYS", 68));

        var label = ThemedUIFactory.CreateSecondaryLabel(text);
        label.TextColor = ThemeManager.Colors.TextPrimary;
        label.Wrap = true;
        label.Width = 420;
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
            var currentResolution = $"{_settings.ResolutionWidth}x{_settings.ResolutionHeight}";
            var resolutions = new List<string> { "1280x720", "1366x768", "1920x1080", "2560x1440", "3840x2160" };
            var selectedIndex = resolutions.IndexOf(currentResolution);
            if (selectedIndex >= 0)
            {
                _resolutionComboBox.SelectedIndex = selectedIndex;
            }
        }

        if (_fullscreenCheckButton != null)
        {
            _fullscreenCheckButton.IsPressed = _settings.Fullscreen;
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

    private void OnSettingsClicked()
    {
        _state = MainMenuState.Settings;
        UpdateUI();
    }

    private void OnExitClicked()
    {
        ShouldExit = true;
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

        if (_fullscreenCheckButton != null)
        {
            _settings.Fullscreen = _fullscreenCheckButton.IsPressed;
        }

        ApplyThemeSelectionsToSettings();
        ThemeManager.ApplyThemeSettings(_settings.Theme);
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

        if (_fullscreenCheckButton != null)
        {
            _fullscreenCheckButton.IsPressed = _settings.Fullscreen;
        }

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
}
