using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Myra.Graphics2D.UI;
using MyraButton = Myra.Graphics2D.UI.Button;

namespace RiskyStars.Client;

public class ConnectionScreen
{
    private readonly int _screenWidth;
    private readonly int _screenHeight;

    private Desktop? _desktop;
    private Panel? _mainPanel;
    private ValidatedTextBox? _playerNameTextBox;
    private ValidatedTextBox? _serverAddressTextBox;
    private MyraButton? _connectButton;
    private MyraButton? _backButton;
    private Label? _statusLabel;

    private bool _isConnecting;
    private bool _shouldGoBack;
    private KeyboardState _previousKeyState;

    public bool IsConnected { get; private set; }
    public string PlayerName => _playerNameTextBox?.Text ?? "";
    public string ServerAddress => _serverAddressTextBox?.Text ?? Settings.Load().ServerAddress;

    public ConnectionScreen(GraphicsDevice graphicsDevice, int screenWidth, int screenHeight)
    {
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
    }

    public void LoadContent(SpriteFont font)
    {
        _desktop = new Desktop();
        ThemeManager.ApplyDesktopTheme(_desktop);
        BuildUI();
    }

    private void BuildUI()
    {
        int frameWidth = Math.Min(_screenWidth - 160, 920);
        int frameHeight = Math.Min(_screenHeight - 120, 680);
        var frame = ThemedUIFactory.CreateViewportFrame(frameWidth, frameHeight);
        frame.HorizontalAlignment = HorizontalAlignment.Center;
        frame.VerticalAlignment = VerticalAlignment.Center;

        var layout = ThemedUIFactory.CreateVerticalStack(ThemeManager.Spacing.Large);
        layout.Widgets.Add(ThemedUIFactory.CreateHeaderPlate("Multiplayer Uplink", "Authenticate your commander and connect to a lobby server"));

        var contentGrid = ThemedUIFactory.CreateGrid(ThemeManager.Spacing.Large, ThemeManager.Spacing.Large);
        contentGrid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        contentGrid.ColumnsProportions.Add(new Proportion(ProportionType.Pixels, 280));

        int cardWidth = frameWidth - 96 - 280 - ThemeManager.Spacing.Large;

        var inputStack = ThemedUIFactory.CreateVerticalStack(ThemeManager.Spacing.Large);
        _playerNameTextBox = ThemedUIFactory.CreateValidatedPlayerNameBox(cardWidth - 40, showErrorLabel: true);
        inputStack.Widgets.Add(ThemedUIFactory.CreateFieldCard("Commander Identity", "The name visible to other players in lobbies and matches.", _playerNameTextBox.Container, cardWidth));

        _serverAddressTextBox = ThemedUIFactory.CreateValidatedServerAddressBox(cardWidth - 40, showErrorLabel: true);
        _serverAddressTextBox.Text = Settings.Load().ServerAddress;
        inputStack.Widgets.Add(ThemedUIFactory.CreateFieldCard("Server Endpoint", "Use an IP or hostname for the multiplayer server.", _serverAddressTextBox.Container, cardWidth));
        inputStack.GridColumn = 0;
        contentGrid.Widgets.Add(inputStack);

        var actionPanel = ThemedUIFactory.CreateFramePanel();
        actionPanel.GridColumn = 1;
        var actionStack = ThemedUIFactory.CreateVerticalStack(ThemeManager.Spacing.Medium);

        var heading = ThemedUIFactory.CreateHeadingLabel("Link State");
        actionStack.Widgets.Add(heading);

        var summary = ThemedUIFactory.CreateSecondaryLabel("Press connect to authenticate with the selected server and move into the lobby browser.");
        summary.Wrap = true;
        summary.Width = 220;
        summary.TextColor = ThemeManager.Colors.TextPrimary;
        actionStack.Widgets.Add(summary);

        _connectButton = ThemedUIFactory.CreateLargeButton("Connect", ThemeManager.ButtonTheme.Primary);
        _connectButton.Click += (_, _) => AttemptConnection();
        actionStack.Widgets.Add(_connectButton);

        _backButton = ThemedUIFactory.CreateLargeButton("Back", ThemeManager.ButtonTheme.Default);
        _backButton.Click += (_, _) => _shouldGoBack = true;
        actionStack.Widgets.Add(_backButton);

        _statusLabel = ThemedUIFactory.CreateSmallLabel("Awaiting uplink command.");
        _statusLabel.TextColor = ThemeManager.Colors.TextSecondary;
        _statusLabel.Wrap = true;
        _statusLabel.Width = 220;
        actionStack.Widgets.Add(_statusLabel);

        actionPanel.Widgets.Add(actionStack);
        contentGrid.Widgets.Add(actionPanel);

        layout.Widgets.Add(contentGrid);
        frame.Widgets.Add(ThemedUIFactory.CreateAutoScrollViewer(layout, frameHeight - 96));

        _mainPanel = ThemedUIFactory.CreateScreenRoot(_screenWidth, _screenHeight);
        _mainPanel.Widgets.Add(frame);

        if (_desktop != null)
        {
            _desktop.Root = _mainPanel;
        }
    }

    private void AttemptConnection()
    {
        if (_playerNameTextBox == null || _serverAddressTextBox == null)
        {
            return;
        }

        _shouldGoBack = false;

        var nameValidation = _playerNameTextBox.ValidateInput();
        var serverValidation = _serverAddressTextBox.ValidateInput();

        if (!nameValidation.IsValid)
        {
            SetStatus(nameValidation.Message, ThemeManager.Colors.TextError);
            return;
        }

        if (!serverValidation.IsValid)
        {
            SetStatus(serverValidation.Message, ThemeManager.Colors.TextError);
            return;
        }

        _isConnecting = true;
        SetStatus("Authenticating commander and opening uplink...", ThemeManager.Colors.TextWarning);

        if (_connectButton != null)
        {
            _connectButton.Enabled = false;
        }
    }

    private void SetStatus(string message, Color color)
    {
        if (_statusLabel != null)
        {
            _statusLabel.Text = message;
            _statusLabel.TextColor = color;
        }
    }

    public void Update(GameTime gameTime, MouseState mouseState, KeyboardState keyState)
    {
        if (keyState.IsKeyDown(Keys.Enter) && _previousKeyState.IsKeyUp(Keys.Enter) && !_isConnecting)
        {
            AttemptConnection();
        }

        if (keyState.IsKeyDown(Keys.Escape) && _previousKeyState.IsKeyUp(Keys.Escape))
        {
            _shouldGoBack = true;
        }

        _previousKeyState = keyState;
    }

    public void SetConnectionResult(bool success, string message)
    {
        _isConnecting = false;
        IsConnected = success;
        SetStatus(message, success ? ThemeManager.Colors.TextSuccess : ThemeManager.Colors.TextError);

        if (_connectButton != null)
        {
            _connectButton.Enabled = true;
        }
    }

    public bool ShouldAttemptConnection()
    {
        return _isConnecting;
    }

    public bool ShouldGoBack()
    {
        bool result = _shouldGoBack;
        _shouldGoBack = false;
        return result;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        _desktop?.Render();
    }
}
