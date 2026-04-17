using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Myra;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.Brushes;

namespace RiskyStars.Client;

public class ConnectionScreen
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly int _screenWidth;
    private readonly int _screenHeight;
    private SpriteFont? _font;
    
    private Desktop? _desktop;
    private Panel? _mainPanel;
    private ValidatedTextBox? _playerNameTextBox;
    private ValidatedTextBox? _serverAddressTextBox;
#pragma warning disable CS0618 // Type or member is obsolete
    private TextButton? _connectButton;
    private Label? _statusLabel;
#pragma warning restore CS0618 // Type or member is obsolete
    
    private bool _isConnecting = false;
    private KeyboardState _previousKeyState;

    public bool IsConnected { get; private set; }
    public string PlayerName => _playerNameTextBox?.Text ?? "";
    public string ServerAddress => _serverAddressTextBox?.Text ?? Settings.Load().ServerAddress;

    public ConnectionScreen(GraphicsDevice graphicsDevice, int screenWidth, int screenHeight)
    {
        _graphicsDevice = graphicsDevice;
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
    }

    public void LoadContent(SpriteFont font)
    {
        _font = font;
        _desktop = new Desktop();
        BuildUI();
    }

    private void BuildUI()
    {
        var rootGrid = new Grid
        {
            RowSpacing = 20,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Width = _screenWidth,
            Height = _screenHeight
        };

        rootGrid.RowsProportions.Add(new Proportion(ProportionType.Auto)); // Title
        rootGrid.RowsProportions.Add(new Proportion(ProportionType.Auto)); // Player name
        rootGrid.RowsProportions.Add(new Proportion(ProportionType.Auto)); // Server address
        rootGrid.RowsProportions.Add(new Proportion(ProportionType.Auto)); // Connect button
        rootGrid.RowsProportions.Add(new Proportion(ProportionType.Auto)); // Status

        // Title
#pragma warning disable CS0618 // Type or member is obsolete
        var titleLabel = new Label
        {
            Text = "Multiplayer - Connect to Server",
            TextColor = Color.Cyan,
            Scale = new Vector2(1.5f, 1.5f),
            HorizontalAlignment = HorizontalAlignment.Center,
            GridRow = 0,
            Margin = new Thickness(0, 0, 0, 20)
        };
#pragma warning restore CS0618 // Type or member is obsolete
        rootGrid.Widgets.Add(titleLabel);

        // Container Panel
        var containerPanel = new Panel
        {
            Width = 500,
            Padding = new Thickness(40, 30),
            Background = new SolidBrush(new Color(0, 0, 0, 220)),
            Border = new SolidBrush(Color.Cyan),
            BorderThickness = new Thickness(3),
            HorizontalAlignment = HorizontalAlignment.Center,
            GridRow = 1,
            GridRowSpan = 3
        };

        var containerGrid = new Grid
        {
            RowSpacing = 25,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        containerGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        containerGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        containerGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));

        // Player Name Field
        _playerNameTextBox = ThemedUIFactory.CreateValidatedPlayerNameBox(420, showErrorLabel: true);
        _playerNameTextBox.Container.GridRow = 0;
        containerGrid.Widgets.Add(_playerNameTextBox.Container);

        // Server Address Field
        _serverAddressTextBox = ThemedUIFactory.CreateValidatedServerAddressBox(420, showErrorLabel: true);
        _serverAddressTextBox.Text = Settings.Load().ServerAddress;
        _serverAddressTextBox.Container.GridRow = 1;
        containerGrid.Widgets.Add(_serverAddressTextBox.Container);

        // Connect Button
#pragma warning disable CS0618 // Type or member is obsolete
        _connectButton = new TextButton
        {
            Text = "Connect",
            Width = 150,
            Height = 50,
            GridRow = 2,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 10, 0, 0)
        };
#pragma warning restore CS0618 // Type or member is obsolete

        _connectButton.Click += (s, a) => AttemptConnection();

        containerGrid.Widgets.Add(_connectButton);
        containerPanel.Widgets.Add(containerGrid);
        rootGrid.Widgets.Add(containerPanel);

        // Status Label
#pragma warning disable CS0618 // Type or member is obsolete
        _statusLabel = new Label
        {
            Text = "",
            TextColor = Color.White,
            Scale = new Vector2(0.8f, 0.8f),
            HorizontalAlignment = HorizontalAlignment.Center,
            GridRow = 4,
            Margin = new Thickness(0, 20, 0, 0)
        };
#pragma warning restore CS0618 // Type or member is obsolete
        rootGrid.Widgets.Add(_statusLabel);

        _mainPanel = new Panel
        {
            Width = _screenWidth,
            Height = _screenHeight,
            Background = new SolidBrush(new Color(10, 10, 20))
        };

        _mainPanel.Widgets.Add(rootGrid);

        if (_desktop != null)
        {
            _desktop.Root = _mainPanel;
        }
    }

    private void AttemptConnection()
    {
        if (_playerNameTextBox == null || _serverAddressTextBox == null)
            return;

        // Validate all inputs before connecting
        var nameValidation = _playerNameTextBox.ValidateInput();
        var serverValidation = _serverAddressTextBox.ValidateInput();

        if (!nameValidation.IsValid)
        {
            SetStatus(nameValidation.Message, Color.Red);
        }
        else if (!serverValidation.IsValid)
        {
            SetStatus(serverValidation.Message, Color.Red);
        }
        else
        {
            _isConnecting = true;
            SetStatus("Connecting...", Color.Yellow);
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

        _previousKeyState = keyState;
    }

    public void SetConnectionResult(bool success, string message)
    {
        _isConnecting = false;
        IsConnected = success;
        
        SetStatus(message, success ? Color.LimeGreen : Color.Red);
        
        if (_connectButton != null)
        {
            _connectButton.Enabled = true;
        }
    }

    public bool ShouldAttemptConnection()
    {
        return _isConnecting;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        _desktop?.Render();
    }
}