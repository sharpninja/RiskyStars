using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.Brushes;

namespace RiskyStars.Client;

public enum MainMenuState
{
    Main,
    Settings,
    Connecting
}

public class MainMenu
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly int _screenWidth;
    private readonly int _screenHeight;
    private Settings _settings;

    private Desktop? _desktop;
    private DialogManager? _dialogManager;
    private MainMenuState _state = MainMenuState.Main;

    // Main menu widgets
    private Panel? _mainMenuPanel;
    private TextButton? _connectButton;
    private TextButton? _settingsButton;
    private TextButton? _exitButton;

    // Settings screen widgets
    private Panel? _settingsPanel;
    private TextBox? _serverAddressTextBox;
    private ComboBox? _resolutionComboBox;
    private CheckButton? _fullscreenCheckButton;
    private TextButton? _saveSettingsButton;
    private TextButton? _backButton;

    // Connecting screen widgets
    private Panel? _connectingPanel;

    public bool ShouldConnect { get; private set; }
    public bool ShouldExit { get; private set; }
    public Settings Settings => _settings;
    public MainMenuState State => _state;

    public MainMenu(GraphicsDevice graphicsDevice, int screenWidth, int screenHeight, Settings settings)
    {
        _graphicsDevice = graphicsDevice;
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
        _settings = settings;
    }

    public void LoadContent(SpriteFont font)
    {
        // Create desktop for UI rendering
        _desktop = new Desktop();
        _dialogManager = new DialogManager(_desktop);

        // Build all UI panels
        BuildMainMenuUI();
        BuildSettingsUI();
        BuildConnectingUI();

        // Show main menu by default
        ShowMainMenuUI();
    }

    private void BuildMainMenuUI()
    {
        var grid = new Grid
        {
            RowSpacing = 20,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        grid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        grid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        grid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        grid.RowsProportions.Add(new Proportion(ProportionType.Auto));

        // Title
        var titleLabel = new Label
        {
            Text = "RiskyStars",
            TextColor = Color.Cyan,
            Scale = new Vector2(2.5f, 2.5f),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        grid.Widgets.Add(titleLabel);

        // Connect button
        _connectButton = new TextButton
        {
            Text = "Connect to Server",
            Width = 250,
            Height = 50,
            HorizontalAlignment = HorizontalAlignment.Center,
            GridRow = 1
        };
        _connectButton.Click += (s, a) => OnConnectClicked();
        grid.Widgets.Add(_connectButton);

        // Settings button
        _settingsButton = new TextButton
        {
            Text = "Settings",
            Width = 250,
            Height = 50,
            HorizontalAlignment = HorizontalAlignment.Center,
            GridRow = 2
        };
        _settingsButton.Click += (s, a) => OnSettingsClicked();
        grid.Widgets.Add(_settingsButton);

        // Exit button
        _exitButton = new TextButton
        {
            Text = "Exit",
            Width = 250,
            Height = 50,
            HorizontalAlignment = HorizontalAlignment.Center,
            GridRow = 3
        };
        _exitButton.Click += (s, a) => OnExitClicked();
        grid.Widgets.Add(_exitButton);

        _mainMenuPanel = new Panel
        {
            Width = _screenWidth,
            Height = _screenHeight
        };
        _mainMenuPanel.Widgets.Add(grid);
    }

    private void BuildSettingsUI()
    {
        var mainGrid = new Grid
        {
            RowSpacing = 8,
            ColumnSpacing = 8,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        // Set up grid structure
        mainGrid.RowsProportions.Add(new Proportion(ProportionType.Auto)); // Title
        mainGrid.RowsProportions.Add(new Proportion(ProportionType.Auto)); // Server Address Label
        mainGrid.RowsProportions.Add(new Proportion(ProportionType.Auto)); // Server Address TextBox
        mainGrid.RowsProportions.Add(new Proportion(ProportionType.Auto)); // Resolution Label
        mainGrid.RowsProportions.Add(new Proportion(ProportionType.Auto)); // Resolution ComboBox
        mainGrid.RowsProportions.Add(new Proportion(ProportionType.Auto)); // Fullscreen
        mainGrid.RowsProportions.Add(new Proportion(ProportionType.Auto)); // Buttons

        // Title
        var titleLabel = new Label
        {
            Text = "Settings",
            TextColor = Color.Cyan,
            Scale = new Vector2(1.2f, 1.2f),
            HorizontalAlignment = HorizontalAlignment.Center,
            GridRow = 0
        };
        mainGrid.Widgets.Add(titleLabel);

        // Server Address Label
        var serverLabel = new Label
        {
            Text = "Server Address",
            TextColor = Color.White,
            GridRow = 1,
            Margin = new Thickness(0, 10, 0, 0)
        };
        mainGrid.Widgets.Add(serverLabel);

        // Server Address TextBox
        _serverAddressTextBox = new TextBox
        {
            Text = _settings.ServerAddress,
            Width = 500,
            GridRow = 2
        };
        mainGrid.Widgets.Add(_serverAddressTextBox);

        // Resolution Label
        var resolutionLabel = new Label
        {
            Text = "Resolution",
            TextColor = Color.White,
            GridRow = 3,
            Margin = new Thickness(0, 10, 0, 0)
        };
        mainGrid.Widgets.Add(resolutionLabel);

        // Resolution ComboBox
        _resolutionComboBox = new ComboBox
        {
            Width = 500,
            GridRow = 4
        };
        
        var resolutions = new List<string>
        {
            "1280x720",
            "1366x768",
            "1920x1080",
            "2560x1440",
            "3840x2160"
        };

        foreach (var resolution in resolutions)
        {
            _resolutionComboBox.Items.Add(new ListItem(resolution));
        }

        var currentResolution = $"{_settings.ResolutionWidth}x{_settings.ResolutionHeight}";
        var selectedIndex = resolutions.IndexOf(currentResolution);
        if (selectedIndex >= 0)
        {
            _resolutionComboBox.SelectedIndex = selectedIndex;
        }
        else
        {
            _resolutionComboBox.SelectedIndex = 0;
        }

        mainGrid.Widgets.Add(_resolutionComboBox);

        // Fullscreen CheckButton with Label
        var fullscreenGrid = new Grid
        {
            ColumnSpacing = 10,
            GridRow = 5,
            Margin = new Thickness(0, 10, 0, 0)
        };
        fullscreenGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        fullscreenGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        
        _fullscreenCheckButton = new CheckButton
        {
            IsPressed = _settings.Fullscreen,
            GridColumn = 0
        };
        fullscreenGrid.Widgets.Add(_fullscreenCheckButton);
        
        var fullscreenLabel = new Label
        {
            Text = "Fullscreen",
            TextColor = Color.White,
            GridColumn = 1,
            VerticalAlignment = VerticalAlignment.Center
        };
        fullscreenGrid.Widgets.Add(fullscreenLabel);
        mainGrid.Widgets.Add(fullscreenGrid);

        // Buttons Grid
        var buttonsGrid = new Grid
        {
            ColumnSpacing = 20,
            HorizontalAlignment = HorizontalAlignment.Center,
            GridRow = 6,
            Margin = new Thickness(0, 20, 0, 0)
        };

        buttonsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        buttonsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));

        _saveSettingsButton = new TextButton
        {
            Text = "Save",
            Width = 150,
            Height = 50,
            GridColumn = 0
        };
        _saveSettingsButton.Click += (s, a) => OnSaveSettingsClicked();
        buttonsGrid.Widgets.Add(_saveSettingsButton);

        _backButton = new TextButton
        {
            Text = "Back",
            Width = 150,
            Height = 50,
            GridColumn = 1
        };
        _backButton.Click += (s, a) => OnBackClicked();
        buttonsGrid.Widgets.Add(_backButton);

        mainGrid.Widgets.Add(buttonsGrid);

        // Create panel with background
        _settingsPanel = new Panel
        {
            Width = _screenWidth,
            Height = _screenHeight,
            Background = new SolidBrush(new Color(10, 10, 20) * 0.95f)
        };
        _settingsPanel.Widgets.Add(mainGrid);
    }

    private void BuildConnectingUI()
    {
        var grid = new Grid
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var messageLabel = new Label
        {
            Text = "Connecting to server...",
            TextColor = Color.Yellow,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        grid.Widgets.Add(messageLabel);

        _connectingPanel = new Panel
        {
            Width = _screenWidth,
            Height = _screenHeight
        };
        _connectingPanel.Widgets.Add(grid);
    }

    private void ShowMainMenuUI()
    {
        if (_desktop != null)
            _desktop.Root = _mainMenuPanel;
    }

    private void ShowSettingsUI()
    {
        if (_serverAddressTextBox != null)
            _serverAddressTextBox.Text = _settings.ServerAddress;

        if (_resolutionComboBox != null)
        {
            var currentResolution = $"{_settings.ResolutionWidth}x{_settings.ResolutionHeight}";
            var resolutions = new List<string> { "1280x720", "1366x768", "1920x1080", "2560x1440", "3840x2160" };
            var selectedIndex = resolutions.IndexOf(currentResolution);
            if (selectedIndex >= 0)
                _resolutionComboBox.SelectedIndex = selectedIndex;
        }

        if (_fullscreenCheckButton != null)
            _fullscreenCheckButton.IsPressed = _settings.Fullscreen;

        if (_desktop != null)
            _desktop.Root = _settingsPanel;
    }

    private void ShowConnectingUI()
    {
        if (_desktop != null)
            _desktop.Root = _connectingPanel;
    }

    public void SetState(MainMenuState state)
    {
        _state = state;
        UpdateUI();
    }

    public void ShowError(string message)
    {
        _dialogManager?.ShowError("Connection Error", message, (result) =>
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
        if (_serverAddressTextBox != null)
            _settings.ServerAddress = _serverAddressTextBox.Text.Trim();

        if (_resolutionComboBox != null && _resolutionComboBox.SelectedItem != null)
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
            _settings.Fullscreen = _fullscreenCheckButton.IsPressed;

        _settings.Save();

        _state = MainMenuState.Main;
        UpdateUI();
    }

    private void OnBackClicked()
    {
        // Restore original settings
        if (_serverAddressTextBox != null)
            _serverAddressTextBox.Text = _settings.ServerAddress;

        if (_resolutionComboBox != null)
        {
            var currentResolution = $"{_settings.ResolutionWidth}x{_settings.ResolutionHeight}";
            var resolutions = new List<string> { "1280x720", "1366x768", "1920x1080", "2560x1440", "3840x2160" };
            var selectedIndex = resolutions.IndexOf(currentResolution);
            if (selectedIndex >= 0)
                _resolutionComboBox.SelectedIndex = selectedIndex;
        }

        if (_fullscreenCheckButton != null)
            _fullscreenCheckButton.IsPressed = _settings.Fullscreen;

        _state = MainMenuState.Main;
        UpdateUI();
    }

    public void Update(GameTime gameTime)
    {
        ShouldConnect = false;
        _dialogManager?.Update();
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        _desktop?.Render();
    }
}
