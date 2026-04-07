using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D.UI;
using Myra.Graphics2D;
using System;

namespace RiskyStars.Client;

public class SettingsWindow
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly Settings _settings;
    private readonly Action<Settings>? _onApply;
    
    private Window? _window;
    private Desktop? _desktop;
    
    private Settings _tempSettings;
    
    private TabControl? _tabControl;
    
    private ComboBox? _resolutionComboBox;
    private CheckButton? _fullscreenCheckButton;
    private CheckButton? _vsyncCheckButton;
    private ComboBox? _frameRateComboBox;
    
    private HorizontalSlider? _masterVolumeSlider;
    private Label? _masterVolumeLabel;
    private HorizontalSlider? _musicVolumeSlider;
    private Label? _musicVolumeLabel;
    private HorizontalSlider? _sfxVolumeSlider;
    private Label? _sfxVolumeLabel;
    
    private HorizontalSlider? _panSpeedSlider;
    private Label? _panSpeedLabel;
    private HorizontalSlider? _zoomSpeedSlider;
    private Label? _zoomSpeedLabel;
    private CheckButton? _invertZoomCheckButton;
    
    private ValidatedTextBox? _serverAddressTextBox;
    
    private CheckButton? _showDebugCheckButton;
    private CheckButton? _showFpsCheckButton;
    
    public bool IsOpen => _window?.Visible ?? false;
    
    public SettingsWindow(GraphicsDeviceManager graphics, Settings settings, Action<Settings>? onApply = null)
    {
        _graphics = graphics;
        _settings = settings;
        _onApply = onApply;
        _tempSettings = settings.Clone();
        
        _desktop = new Desktop();
        CreateUI();
    }
    
    private void CreateUI()
    {
        _window = new Window
        {
            Title = "Settings",
            TitleTextColor = ThemeManager.Colors.TextAccent,
            Background = ThemeManager.CreateSolidBrush(ThemeManager.Colors.BackgroundDark),
            Border = ThemeManager.CreateSolidBrush(ThemeManager.Colors.AccentCyan),
            BorderThickness = new Thickness(ThemeManager.BorderThickness.Normal),
            Width = 700,
            Height = 550,
            Padding = ThemeManager.Padding.Large,
            Visible = false
        };
        
        var mainGrid = ThemedUIFactory.CreateGrid();
        mainGrid.RowsProportions.Add(new Proportion(ProportionType.Fill));
        mainGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        
        _tabControl = new TabControl();
        _tabControl.GridRow = 0;
        
        CreateGraphicsTab();
        CreateAudioTab();
        CreateControlsTab();
        CreateServerTab();
        
        mainGrid.Widgets.Add(_tabControl);
        
        var buttonPanel = CreateButtonPanel();
        buttonPanel.GridRow = 1;
        mainGrid.Widgets.Add(buttonPanel);
        
        _window.Content = mainGrid;
        _desktop.Widgets.Add(_window);
        
        CenterWindow();
    }
    
    private void CreateGraphicsTab()
    {
        var tabItem = new TabItem
        {
            Text = "Graphics"
        };
        
        var scrollViewer = new ScrollViewer
        {
            ShowVerticalScrollBar = true,
            ShowHorizontalScrollBar = false
        };
        
        var contentStack = ThemedUIFactory.CreateVerticalStack(ThemeManager.Spacing.Medium);
        
        contentStack.Widgets.Add(ThemedUIFactory.CreateHeadingLabel("Display Settings"));
        contentStack.Widgets.Add(ThemedUIFactory.CreateHorizontalSeparator());
        
        var resolutionGrid = ThemedUIFactory.CreateGrid();
        resolutionGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        resolutionGrid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        resolutionGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        
        var resolutionLabel = ThemedUIFactory.CreateLabel("Resolution:");
        resolutionLabel.GridColumn = 0;
        resolutionLabel.VerticalAlignment = VerticalAlignment.Center;
        resolutionGrid.Widgets.Add(resolutionLabel);
        
        _resolutionComboBox = ThemedUIFactory.CreateComboBox(250);
        _resolutionComboBox.GridColumn = 1;
        _resolutionComboBox.HorizontalAlignment = HorizontalAlignment.Left;
        
        var resolutions = new[] { "1280x720", "1366x768", "1920x1080", "2560x1440", "3840x2160" };
        foreach (var resolution in resolutions)
        {
            _resolutionComboBox.Items.Add(new ListItem(resolution));
        }
        
        var currentResolution = $"{_tempSettings.ResolutionWidth}x{_tempSettings.ResolutionHeight}";
        var selectedIndex = Array.IndexOf(resolutions, currentResolution);
        _resolutionComboBox.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
        
        resolutionGrid.Widgets.Add(_resolutionComboBox);
        contentStack.Widgets.Add(resolutionGrid);
        
        var fullscreenPanel = ThemedUIFactory.CreateHorizontalStack(ThemeManager.Spacing.Small);
        _fullscreenCheckButton = ThemedUIFactory.CreateCheckButton(_tempSettings.Fullscreen);
        fullscreenPanel.Widgets.Add(_fullscreenCheckButton);
        fullscreenPanel.Widgets.Add(ThemedUIFactory.CreateLabel("Fullscreen"));
        contentStack.Widgets.Add(fullscreenPanel);
        
        var vsyncPanel = ThemedUIFactory.CreateHorizontalStack(ThemeManager.Spacing.Small);
        _vsyncCheckButton = ThemedUIFactory.CreateCheckButton(_tempSettings.VSync);
        vsyncPanel.Widgets.Add(_vsyncCheckButton);
        vsyncPanel.Widgets.Add(ThemedUIFactory.CreateLabel("VSync"));
        contentStack.Widgets.Add(vsyncPanel);
        
        var frameRateGrid = ThemedUIFactory.CreateGrid();
        frameRateGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        frameRateGrid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        frameRateGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        
        var frameRateLabel = ThemedUIFactory.CreateLabel("Target Frame Rate:");
        frameRateLabel.GridColumn = 0;
        frameRateLabel.VerticalAlignment = VerticalAlignment.Center;
        frameRateGrid.Widgets.Add(frameRateLabel);
        
        _frameRateComboBox = ThemedUIFactory.CreateComboBox(150);
        _frameRateComboBox.GridColumn = 1;
        _frameRateComboBox.HorizontalAlignment = HorizontalAlignment.Left;
        
        var frameRates = new[] { "30", "60", "120", "144", "Unlimited" };
        foreach (var rate in frameRates)
        {
            _frameRateComboBox.Items.Add(new ListItem(rate));
        }
        
        var currentFrameRateIndex = _tempSettings.TargetFrameRate switch
        {
            30 => 0,
            60 => 1,
            120 => 2,
            144 => 3,
            _ => 4
        };
        _frameRateComboBox.SelectedIndex = currentFrameRateIndex;
        
        frameRateGrid.Widgets.Add(_frameRateComboBox);
        contentStack.Widgets.Add(frameRateGrid);
        
        contentStack.Widgets.Add(new Panel { Height = 20 });
        contentStack.Widgets.Add(ThemedUIFactory.CreateHeadingLabel("Debug Options"));
        contentStack.Widgets.Add(ThemedUIFactory.CreateHorizontalSeparator());
        
        var debugPanel = ThemedUIFactory.CreateHorizontalStack(ThemeManager.Spacing.Small);
        _showDebugCheckButton = ThemedUIFactory.CreateCheckButton(_tempSettings.ShowDebugInfo);
        debugPanel.Widgets.Add(_showDebugCheckButton);
        debugPanel.Widgets.Add(ThemedUIFactory.CreateLabel("Show Debug Info Window on Startup"));
        contentStack.Widgets.Add(debugPanel);
        
        var fpsPanel = ThemedUIFactory.CreateHorizontalStack(ThemeManager.Spacing.Small);
        _showFpsCheckButton = ThemedUIFactory.CreateCheckButton(_tempSettings.ShowFPS);
        fpsPanel.Widgets.Add(_showFpsCheckButton);
        fpsPanel.Widgets.Add(ThemedUIFactory.CreateLabel("Show FPS Counter"));
        contentStack.Widgets.Add(fpsPanel);
        
        scrollViewer.Content = contentStack;
        tabItem.Content = scrollViewer;
        _tabControl?.Items.Add(tabItem);
    }
    
    private void CreateAudioTab()
    {
        var tabItem = new TabItem
        {
            Text = "Audio"
        };
        
        var contentStack = ThemedUIFactory.CreateVerticalStack(ThemeManager.Spacing.Large);
        
        contentStack.Widgets.Add(ThemedUIFactory.CreateHeadingLabel("Volume Settings"));
        contentStack.Widgets.Add(ThemedUIFactory.CreateHorizontalSeparator());
        
        contentStack.Widgets.Add(ThemedUIFactory.CreateSecondaryLabel("Audio system will be implemented in a future update."));
        contentStack.Widgets.Add(new Panel { Height = 10 });
        
        var masterVolumeGrid = CreateVolumeSlider("Master Volume:", _tempSettings.MasterVolume, out _masterVolumeSlider, out _masterVolumeLabel);
        contentStack.Widgets.Add(masterVolumeGrid);
        
        var musicVolumeGrid = CreateVolumeSlider("Music Volume:", _tempSettings.MusicVolume, out _musicVolumeSlider, out _musicVolumeLabel);
        contentStack.Widgets.Add(musicVolumeGrid);
        
        var sfxVolumeGrid = CreateVolumeSlider("SFX Volume:", _tempSettings.SfxVolume, out _sfxVolumeSlider, out _sfxVolumeLabel);
        contentStack.Widgets.Add(sfxVolumeGrid);
        
        _masterVolumeSlider.ValueChanged += (s, e) => UpdateVolumeLabel(_masterVolumeLabel, _masterVolumeSlider.Value);
        _musicVolumeSlider.ValueChanged += (s, e) => UpdateVolumeLabel(_musicVolumeLabel, _musicVolumeSlider.Value);
        _sfxVolumeSlider.ValueChanged += (s, e) => UpdateVolumeLabel(_sfxVolumeLabel, _sfxVolumeSlider.Value);
        
        tabItem.Content = contentStack;
        _tabControl?.Items.Add(tabItem);
    }
    
    private Grid CreateVolumeSlider(string label, float value, out HorizontalSlider slider, out Label valueLabel)
    {
        var grid = ThemedUIFactory.CreateGrid();
        grid.ColumnsProportions.Add(new Proportion(ProportionType.Pixels, 150));
        grid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        grid.ColumnsProportions.Add(new Proportion(ProportionType.Pixels, 50));
        grid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        
        var labelWidget = ThemedUIFactory.CreateLabel(label);
        labelWidget.GridColumn = 0;
        labelWidget.VerticalAlignment = VerticalAlignment.Center;
        grid.Widgets.Add(labelWidget);
        
        slider = new HorizontalSlider
        {
            GridColumn = 1,
            Minimum = 0,
            Maximum = 100,
            Value = value * 100,
            Width = 300,
            VerticalAlignment = VerticalAlignment.Center
        };
        grid.Widgets.Add(slider);
        
        valueLabel = ThemedUIFactory.CreateLabel($"{(int)(value * 100)}%");
        valueLabel.GridColumn = 2;
        valueLabel.VerticalAlignment = VerticalAlignment.Center;
        valueLabel.HorizontalAlignment = HorizontalAlignment.Right;
        grid.Widgets.Add(valueLabel);
        
        return grid;
    }
    
    private void UpdateVolumeLabel(Label? label, float value)
    {
        if (label != null)
        {
            label.Text = $"{(int)value}%";
        }
    }
    
    private void CreateControlsTab()
    {
        var tabItem = new TabItem
        {
            Text = "Controls"
        };
        
        var scrollViewer = new ScrollViewer
        {
            ShowVerticalScrollBar = true,
            ShowHorizontalScrollBar = false
        };
        
        var contentStack = ThemedUIFactory.CreateVerticalStack(ThemeManager.Spacing.Large);
        
        contentStack.Widgets.Add(ThemedUIFactory.CreateHeadingLabel("Camera Controls"));
        contentStack.Widgets.Add(ThemedUIFactory.CreateHorizontalSeparator());
        
        var panSpeedGrid = CreateSliderControl("Pan Speed:", _tempSettings.CameraPanSpeed, 1.0f, 10.0f, out _panSpeedSlider, out _panSpeedLabel);
        contentStack.Widgets.Add(panSpeedGrid);
        
        var zoomSpeedGrid = CreateSliderControl("Zoom Speed:", _tempSettings.CameraZoomSpeed, 0.05f, 0.5f, out _zoomSpeedSlider, out _zoomSpeedLabel);
        contentStack.Widgets.Add(zoomSpeedGrid);
        
        var invertZoomPanel = ThemedUIFactory.CreateHorizontalStack(ThemeManager.Spacing.Small);
        _invertZoomCheckButton = ThemedUIFactory.CreateCheckButton(_tempSettings.InvertCameraZoom);
        invertZoomPanel.Widgets.Add(_invertZoomCheckButton);
        invertZoomPanel.Widgets.Add(ThemedUIFactory.CreateLabel("Invert Zoom Direction"));
        contentStack.Widgets.Add(invertZoomPanel);
        
        _panSpeedSlider.ValueChanged += (s, e) => UpdateFloatLabel(_panSpeedLabel, _panSpeedSlider.Value / 100f, 1.0f, 10.0f);
        _zoomSpeedSlider.ValueChanged += (s, e) => UpdateFloatLabel(_zoomSpeedLabel, _zoomSpeedSlider.Value / 100f, 0.05f, 0.5f);
        
        contentStack.Widgets.Add(new Panel { Height = 20 });
        contentStack.Widgets.Add(ThemedUIFactory.CreateHeadingLabel("Keyboard Shortcuts"));
        contentStack.Widgets.Add(ThemedUIFactory.CreateHorizontalSeparator());
        
        var shortcutsStack = ThemedUIFactory.CreateVerticalStack(ThemeManager.Spacing.Small);
        shortcutsStack.Widgets.Add(CreateShortcutLabel("ESC", "Open Settings / Close Dialogs"));
        shortcutsStack.Widgets.Add(CreateShortcutLabel("F1", "Toggle Debug Info Window"));
        shortcutsStack.Widgets.Add(CreateShortcutLabel("F2", "Toggle Player Dashboard"));
        shortcutsStack.Widgets.Add(CreateShortcutLabel("F3", "Toggle AI Visualization"));
        shortcutsStack.Widgets.Add(CreateShortcutLabel("WASD / Arrows", "Pan Camera"));
        shortcutsStack.Widgets.Add(CreateShortcutLabel("Mouse Wheel", "Zoom Camera"));
        shortcutsStack.Widgets.Add(CreateShortcutLabel("Middle Mouse", "Pan by Dragging"));
        shortcutsStack.Widgets.Add(CreateShortcutLabel("Shift", "Fast Pan Modifier"));
        contentStack.Widgets.Add(shortcutsStack);
        
        scrollViewer.Content = contentStack;
        tabItem.Content = scrollViewer;
        _tabControl?.Items.Add(tabItem);
    }
    
    private Grid CreateSliderControl(string label, float value, float min, float max, out HorizontalSlider slider, out Label valueLabel)
    {
        var grid = ThemedUIFactory.CreateGrid();
        grid.ColumnsProportions.Add(new Proportion(ProportionType.Pixels, 150));
        grid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        grid.ColumnsProportions.Add(new Proportion(ProportionType.Pixels, 60));
        grid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        
        var labelWidget = ThemedUIFactory.CreateLabel(label);
        labelWidget.GridColumn = 0;
        labelWidget.VerticalAlignment = VerticalAlignment.Center;
        grid.Widgets.Add(labelWidget);
        
        slider = new HorizontalSlider
        {
            GridColumn = 1,
            Minimum = 0,
            Maximum = 100,
            Value = ((value - min) / (max - min)) * 100,
            Width = 300,
            VerticalAlignment = VerticalAlignment.Center
        };
        grid.Widgets.Add(slider);
        
        valueLabel = ThemedUIFactory.CreateLabel(value.ToString("F2"));
        valueLabel.GridColumn = 2;
        valueLabel.VerticalAlignment = VerticalAlignment.Center;
        valueLabel.HorizontalAlignment = HorizontalAlignment.Right;
        grid.Widgets.Add(valueLabel);
        
        return grid;
    }
    
    private void UpdateFloatLabel(Label? label, float normalizedValue, float min, float max)
    {
        if (label != null)
        {
            var actualValue = min + (normalizedValue * (max - min));
            label.Text = actualValue.ToString("F2");
        }
    }
    
    private HorizontalStackPanel CreateShortcutLabel(string key, string description)
    {
        var stack = ThemedUIFactory.CreateHorizontalStack(ThemeManager.Spacing.Medium);
        
        var keyLabel = ThemedUIFactory.CreateLabel(key);
        keyLabel.TextColor = ThemeManager.Colors.AccentCyan;
        keyLabel.Width = 120;
        stack.Widgets.Add(keyLabel);
        
        var descLabel = ThemedUIFactory.CreateSecondaryLabel(description);
        stack.Widgets.Add(descLabel);
        
        return stack;
    }
    
    private void CreateServerTab()
    {
        var tabItem = new TabItem
        {
            Text = "Server"
        };
        
        var contentStack = ThemedUIFactory.CreateVerticalStack(ThemeManager.Spacing.Large);
        
        contentStack.Widgets.Add(ThemedUIFactory.CreateHeadingLabel("Connection Settings"));
        contentStack.Widgets.Add(ThemedUIFactory.CreateHorizontalSeparator());
        
        contentStack.Widgets.Add(ThemedUIFactory.CreateLabel("Server Address:"));
        
        _serverAddressTextBox = ThemedUIFactory.CreateValidatedServerAddressBox(500, true);
        _serverAddressTextBox.Text = _tempSettings.ServerAddress;
        contentStack.Widgets.Add(_serverAddressTextBox.Container);
        
        contentStack.Widgets.Add(new Panel { Height = 10 });
        
        var infoLabel = ThemedUIFactory.CreateSecondaryLabel(
            "Note: Changing the server address will take effect the next time you connect to a game."
        );
        infoLabel.Wrap = true;
        contentStack.Widgets.Add(infoLabel);
        
        tabItem.Content = contentStack;
        _tabControl?.Items.Add(tabItem);
    }
    
    private HorizontalStackPanel CreateButtonPanel()
    {
        var panel = ThemedUIFactory.CreateHorizontalStack(ThemeManager.Spacing.Medium);
        panel.HorizontalAlignment = HorizontalAlignment.Right;
        panel.Margin = new Thickness(0, ThemeManager.Spacing.Large, 0, 0);
        
        var applyButton = ThemedUIFactory.CreateButton("Apply", ThemeManager.ButtonTheme.Primary);
        applyButton.Width = ThemeManager.Sizes.ButtonMediumWidth;
        applyButton.Click += OnApplyClicked;
        panel.Widgets.Add(applyButton);
        
        var cancelButton = ThemedUIFactory.CreateButton("Cancel");
        cancelButton.Width = ThemeManager.Sizes.ButtonMediumWidth;
        cancelButton.Click += OnCancelClicked;
        panel.Widgets.Add(cancelButton);
        
        return panel;
    }
    
    private void OnApplyClicked(object? sender, EventArgs e)
    {
        ApplyChanges();
        _settings.Save();
        _onApply?.Invoke(_settings);
        Close();
    }
    
    private void OnCancelClicked(object? sender, EventArgs e)
    {
        _tempSettings = _settings.Clone();
        Close();
    }
    
    private void ApplyChanges()
    {
        if (_resolutionComboBox?.SelectedItem != null)
        {
            var resolution = _resolutionComboBox.SelectedItem.Text;
            var parts = resolution.Split('x');
            if (parts.Length == 2 && int.TryParse(parts[0], out int width) && int.TryParse(parts[1], out int height))
            {
                _tempSettings.ResolutionWidth = width;
                _tempSettings.ResolutionHeight = height;
            }
        }
        
        _tempSettings.Fullscreen = _fullscreenCheckButton?.IsPressed ?? false;
        _tempSettings.VSync = _vsyncCheckButton?.IsPressed ?? false;
        
        if (_frameRateComboBox?.SelectedItem != null)
        {
            var frameRateText = _frameRateComboBox.SelectedItem.Text;
            _tempSettings.TargetFrameRate = frameRateText switch
            {
                "30" => 30,
                "60" => 60,
                "120" => 120,
                "144" => 144,
                _ => 0
            };
        }
        
        if (_masterVolumeSlider != null)
            _tempSettings.MasterVolume = _masterVolumeSlider.Value / 100f;
        if (_musicVolumeSlider != null)
            _tempSettings.MusicVolume = _musicVolumeSlider.Value / 100f;
        if (_sfxVolumeSlider != null)
            _tempSettings.SfxVolume = _sfxVolumeSlider.Value / 100f;
        
        if (_panSpeedSlider != null)
            _tempSettings.CameraPanSpeed = 1.0f + (_panSpeedSlider.Value / 100f * 9.0f);
        if (_zoomSpeedSlider != null)
            _tempSettings.CameraZoomSpeed = 0.05f + (_zoomSpeedSlider.Value / 100f * 0.45f);
        
        _tempSettings.InvertCameraZoom = _invertZoomCheckButton?.IsPressed ?? false;
        
        if (_serverAddressTextBox != null && _serverAddressTextBox.IsValid)
            _tempSettings.ServerAddress = _serverAddressTextBox.Text.Trim();
        
        _tempSettings.ShowDebugInfo = _showDebugCheckButton?.IsPressed ?? false;
        _tempSettings.ShowFPS = _showFpsCheckButton?.IsPressed ?? false;
        
        _settings.ServerAddress = _tempSettings.ServerAddress;
        _settings.ResolutionWidth = _tempSettings.ResolutionWidth;
        _settings.ResolutionHeight = _tempSettings.ResolutionHeight;
        _settings.Fullscreen = _tempSettings.Fullscreen;
        _settings.VSync = _tempSettings.VSync;
        _settings.TargetFrameRate = _tempSettings.TargetFrameRate;
        _settings.MasterVolume = _tempSettings.MasterVolume;
        _settings.MusicVolume = _tempSettings.MusicVolume;
        _settings.SfxVolume = _tempSettings.SfxVolume;
        _settings.CameraPanSpeed = _tempSettings.CameraPanSpeed;
        _settings.CameraZoomSpeed = _tempSettings.CameraZoomSpeed;
        _settings.InvertCameraZoom = _tempSettings.InvertCameraZoom;
        _settings.ShowDebugInfo = _tempSettings.ShowDebugInfo;
        _settings.ShowFPS = _tempSettings.ShowFPS;
    }
    
    private void CenterWindow()
    {
        if (_window != null && _graphics != null)
        {
            var screenWidth = _graphics.PreferredBackBufferWidth;
            var screenHeight = _graphics.PreferredBackBufferHeight;
            var windowWidth = _window.Width ?? 700;
            var windowHeight = _window.Height ?? 550;
            
            _window.Left = (screenWidth - windowWidth) / 2;
            _window.Top = (screenHeight - windowHeight) / 2;
        }
    }
    
    public void Open()
    {
        if (_window != null)
        {
            _tempSettings = _settings.Clone();
            RefreshUI();
            CenterWindow();
            _window.Visible = true;
        }
    }
    
    public void Close()
    {
        if (_window != null)
        {
            _window.Visible = false;
        }
    }
    
    public void Toggle()
    {
        if (IsOpen)
            Close();
        else
            Open();
    }
    
    private void RefreshUI()
    {
        if (_resolutionComboBox != null)
        {
            var resolutions = new[] { "1280x720", "1366x768", "1920x1080", "2560x1440", "3840x2160" };
            var currentResolution = $"{_tempSettings.ResolutionWidth}x{_tempSettings.ResolutionHeight}";
            var selectedIndex = Array.IndexOf(resolutions, currentResolution);
            _resolutionComboBox.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
        }
        
        if (_fullscreenCheckButton != null)
            _fullscreenCheckButton.IsPressed = _tempSettings.Fullscreen;
        
        if (_vsyncCheckButton != null)
            _vsyncCheckButton.IsPressed = _tempSettings.VSync;
        
        if (_frameRateComboBox != null)
        {
            var currentFrameRateIndex = _tempSettings.TargetFrameRate switch
            {
                30 => 0,
                60 => 1,
                120 => 2,
                144 => 3,
                _ => 4
            };
            _frameRateComboBox.SelectedIndex = currentFrameRateIndex;
        }
        
        if (_masterVolumeSlider != null)
        {
            _masterVolumeSlider.Value = _tempSettings.MasterVolume * 100;
            UpdateVolumeLabel(_masterVolumeLabel, _masterVolumeSlider.Value);
        }
        
        if (_musicVolumeSlider != null)
        {
            _musicVolumeSlider.Value = _tempSettings.MusicVolume * 100;
            UpdateVolumeLabel(_musicVolumeLabel, _musicVolumeSlider.Value);
        }
        
        if (_sfxVolumeSlider != null)
        {
            _sfxVolumeSlider.Value = _tempSettings.SfxVolume * 100;
            UpdateVolumeLabel(_sfxVolumeLabel, _sfxVolumeSlider.Value);
        }
        
        if (_panSpeedSlider != null)
        {
            _panSpeedSlider.Value = ((_tempSettings.CameraPanSpeed - 1.0f) / 9.0f) * 100;
            UpdateFloatLabel(_panSpeedLabel, _panSpeedSlider.Value / 100f, 1.0f, 10.0f);
        }
        
        if (_zoomSpeedSlider != null)
        {
            _zoomSpeedSlider.Value = ((_tempSettings.CameraZoomSpeed - 0.05f) / 0.45f) * 100;
            UpdateFloatLabel(_zoomSpeedLabel, _zoomSpeedSlider.Value / 100f, 0.05f, 0.5f);
        }
        
        if (_invertZoomCheckButton != null)
            _invertZoomCheckButton.IsPressed = _tempSettings.InvertCameraZoom;
        
        if (_serverAddressTextBox != null)
            _serverAddressTextBox.Text = _tempSettings.ServerAddress;
        
        if (_showDebugCheckButton != null)
            _showDebugCheckButton.IsPressed = _tempSettings.ShowDebugInfo;
        
        if (_showFpsCheckButton != null)
            _showFpsCheckButton.IsPressed = _tempSettings.ShowFPS;
    }
    
    public void Render()
    {
        _desktop?.Render();
    }
}
