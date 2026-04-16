using Microsoft.Xna.Framework;
using RiskyStars.Client;
using Microsoft.Xna.Framework.Graphics;
using RiskyStars.Client;
using Microsoft.Xna.Framework.Input;
using RiskyStars.Client;

namespace RiskyStars.Client;

public class ConnectionScreen
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly int _screenWidth;
    private readonly int _screenHeight;
    private Texture2D? _pixelTexture;
    private SpriteFont? _font;
    
    private string _playerName = "";
    private string _serverAddress = Settings.Load().ServerAddress;
    private string _statusMessage = "";
    private bool _isConnecting = false;
    private Color _statusColor = Color.White;
    
    private ValidatedTextInputField _playerNameField;
    private ValidatedTextInputField _serverAddressField;
    private Button _connectButton;
    
    private KeyboardState _previousKeyState;

    public bool IsConnected { get; private set; }
    public string PlayerName => _playerName;
    public string ServerAddress => _serverAddress;

    public ConnectionScreen(GraphicsDevice graphicsDevice, int screenWidth, int screenHeight)
    {
        _graphicsDevice = graphicsDevice;
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
        
        CreatePixelTexture();
        
        int panelWidth = 400;
        int centerX = (_screenWidth - panelWidth) / 2;
        int centerY = _screenHeight / 2 - 100;
        
        _playerNameField = new ValidatedTextInputField(
            new Rectangle(centerX, centerY, panelWidth, 40),
            "Player Name", 20);
        _playerNameField.SetValidator(InputValidator.ValidatePlayerName);
        
        _serverAddressField = new ValidatedTextInputField(
            new Rectangle(centerX, centerY + 80, panelWidth, 40),
            "Server Address", 100);
        _serverAddressField.Text = _serverAddress;
        _serverAddressField.SetValidator(InputValidator.ValidateServerAddress);
        
        _connectButton = new Button(
            new Rectangle(centerX + panelWidth / 2 - 75, centerY + 160, 150, 50),
            "Connect");
    }

    private void CreatePixelTexture()
    {
        _pixelTexture = new Texture2D(_graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
    }

    public void LoadContent(SpriteFont font)
    {
        _font = font;
    }

    public void Update(GameTime gameTime, MouseState mouseState, KeyboardState keyState)
    {
        if (_isConnecting)
        {
            return;
        }

        _playerNameField.Update(mouseState, keyState, _previousKeyState);
        _serverAddressField.Update(mouseState, keyState, _previousKeyState);
        _connectButton.Update(mouseState);
        
        if (_connectButton.IsClicked || (keyState.IsKeyDown(Keys.Enter) && _previousKeyState.IsKeyUp(Keys.Enter)))
        {
            // Validate all inputs before connecting
            var nameValidation = _playerNameField.ValidateInput();
            var serverValidation = _serverAddressField.ValidateInput();

            if (!nameValidation.IsValid)
            {
                _statusMessage = nameValidation.Message;
                _statusColor = Color.Red;
            }
            else if (!serverValidation.IsValid)
            {
                _statusMessage = serverValidation.Message;
                _statusColor = Color.Red;
            }
            else
            {
                _playerName = _playerNameField.Text.Trim();
                _serverAddress = _serverAddressField.Text.Trim();
                _isConnecting = true;
                _statusMessage = "Connecting...";
                _statusColor = Color.Yellow;
            }
        }
        
        _previousKeyState = keyState;
    }

    public void SetConnectionResult(bool success, string message)
    {
        _isConnecting = false;
        _statusMessage = message;
        _statusColor = success ? Color.Green : Color.Red;
        IsConnected = success;
    }

    public bool ShouldAttemptConnection()
    {
        return _isConnecting;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (_pixelTexture == null || _font == null)
        {
            return;
        }

        spriteBatch.Begin(sortMode: SpriteSortMode.Deferred, blendState: BlendState.AlphaBlend);

        int panelWidth = 500;
        int panelHeight = 350;
        int panelX = (_screenWidth - panelWidth) / 2;
        int panelY = (_screenHeight - panelHeight) / 2;
        
        var panel = new Rectangle(panelX, panelY, panelWidth, panelHeight);
        spriteBatch.Draw(_pixelTexture, panel, Color.Black * 0.9f);
        DrawRectangleOutline(spriteBatch, panel, Color.Cyan, 3);

        var titleText = "Multiplayer - Connect to Server";
        var titleSize = _font.MeasureString(titleText);
        spriteBatch.DrawString(_font, titleText,
            new Vector2(panelX + (panelWidth - titleSize.X) / 2, panelY + 20),
            Color.Cyan, 0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0f);

        _playerNameField.Draw(spriteBatch, _pixelTexture, _font);
        _serverAddressField.Draw(spriteBatch, _pixelTexture, _font);
        _connectButton.Draw(spriteBatch, _pixelTexture, _font);

        if (!string.IsNullOrEmpty(_statusMessage))
        {
            var statusSize = _font.MeasureString(_statusMessage);
            spriteBatch.DrawString(_font, _statusMessage,
                new Vector2(panelX + (panelWidth - statusSize.X) / 2, panelY + panelHeight - 50),
                _statusColor, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
        }

        spriteBatch.End();
    }

    private void DrawRectangleOutline(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness)
    {
        if (_pixelTexture == null)
        {
            return;
        }

        spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(_pixelTexture, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
    }
}


