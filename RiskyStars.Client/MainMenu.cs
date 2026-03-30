using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace RiskyStars.Client;

public enum MainMenuState
{
    Main,
    Settings,
    Connecting,
    Error
}

public class MainMenu
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly int _screenWidth;
    private readonly int _screenHeight;
    private Texture2D? _pixelTexture;
    private SpriteFont? _font;

    private MainMenuState _state = MainMenuState.Main;
    private Settings _settings;

    private Button _connectButton;
    private Button _settingsButton;
    private Button _exitButton;

    private Button _backButton;
    private Button _saveSettingsButton;
    private TextInputField _serverAddressField;
    private DropdownField _resolutionDropdown;
    private CheckboxField _fullscreenCheckbox;

    private string _errorMessage = "";
    private Button _errorOkButton;

    private KeyboardState _previousKeyState;

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

        CreatePixelTexture();
        InitializeMainMenuControls();
        InitializeSettingsControls();
        InitializeErrorControls();
    }

    private void CreatePixelTexture()
    {
        _pixelTexture = new Texture2D(_graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
    }

    private void InitializeMainMenuControls()
    {
        int buttonWidth = 250;
        int buttonHeight = 50;
        int buttonSpacing = 20;
        int centerX = (_screenWidth - buttonWidth) / 2;
        int startY = _screenHeight / 2 - 50;

        _connectButton = new Button(
            new Rectangle(centerX, startY, buttonWidth, buttonHeight),
            "Connect to Server");

        _settingsButton = new Button(
            new Rectangle(centerX, startY + buttonHeight + buttonSpacing, buttonWidth, buttonHeight),
            "Settings");

        _exitButton = new Button(
            new Rectangle(centerX, startY + (buttonHeight + buttonSpacing) * 2, buttonWidth, buttonHeight),
            "Exit");
    }

    private void InitializeSettingsControls()
    {
        int panelWidth = 500;
        int centerX = (_screenWidth - panelWidth) / 2;
        int startY = 200;
        int fieldHeight = 40;
        int fieldSpacing = 70;

        _serverAddressField = new TextInputField(
            new Rectangle(centerX, startY, panelWidth, fieldHeight),
            "Server Address", 100);
        _serverAddressField.Text = _settings.ServerAddress;

        var resolutions = new List<string>
        {
            "1280x720",
            "1366x768",
            "1920x1080",
            "2560x1440",
            "3840x2160"
        };

        _resolutionDropdown = new DropdownField(
            new Rectangle(centerX, startY + fieldSpacing, panelWidth, fieldHeight),
            "Resolution", resolutions);
        _resolutionDropdown.SelectedIndex = resolutions.IndexOf($"{_settings.ResolutionWidth}x{_settings.ResolutionHeight}");
        if (_resolutionDropdown.SelectedIndex == -1)
            _resolutionDropdown.SelectedIndex = 0;

        _fullscreenCheckbox = new CheckboxField(
            new Rectangle(centerX, startY + fieldSpacing * 2, panelWidth, fieldHeight),
            "Fullscreen");
        _fullscreenCheckbox.IsChecked = _settings.Fullscreen;

        int buttonWidth = 150;
        int buttonY = startY + fieldSpacing * 3;
        _saveSettingsButton = new Button(
            new Rectangle(centerX + panelWidth - buttonWidth * 2 - 20, buttonY, buttonWidth, 50),
            "Save");

        _backButton = new Button(
            new Rectangle(centerX + panelWidth - buttonWidth, buttonY, buttonWidth, 50),
            "Back");
    }

    private void InitializeErrorControls()
    {
        int buttonWidth = 150;
        int buttonHeight = 50;
        int centerX = (_screenWidth - buttonWidth) / 2;
        int centerY = _screenHeight / 2 + 100;

        _errorOkButton = new Button(
            new Rectangle(centerX, centerY, buttonWidth, buttonHeight),
            "OK");
    }

    public void LoadContent(SpriteFont font)
    {
        _font = font;
    }

    public void SetState(MainMenuState state)
    {
        _state = state;
    }

    public void ShowError(string message)
    {
        _errorMessage = message;
        _state = MainMenuState.Error;
    }

    public void Update(GameTime gameTime)
    {
        var mouseState = Mouse.GetState();
        var keyState = Keyboard.GetState();

        ShouldConnect = false;

        switch (_state)
        {
            case MainMenuState.Main:
                UpdateMainMenu(mouseState);
                break;

            case MainMenuState.Settings:
                UpdateSettings(mouseState, keyState);
                break;

            case MainMenuState.Error:
                UpdateError(mouseState);
                break;

            case MainMenuState.Connecting:
                break;
        }

        _previousKeyState = keyState;
    }

    private void UpdateMainMenu(MouseState mouseState)
    {
        _connectButton.Update(mouseState);
        _settingsButton.Update(mouseState);
        _exitButton.Update(mouseState);

        if (_connectButton.IsClicked)
        {
            ShouldConnect = true;
            _state = MainMenuState.Connecting;
        }

        if (_settingsButton.IsClicked)
        {
            _state = MainMenuState.Settings;
        }

        if (_exitButton.IsClicked)
        {
            ShouldExit = true;
        }
    }

    private void UpdateSettings(MouseState mouseState, KeyboardState keyState)
    {
        _serverAddressField.Update(mouseState, keyState, _previousKeyState);
        _resolutionDropdown.Update(mouseState);
        _fullscreenCheckbox.Update(mouseState);
        _saveSettingsButton.Update(mouseState);
        _backButton.Update(mouseState);

        if (_saveSettingsButton.IsClicked)
        {
            _settings.ServerAddress = _serverAddressField.Text.Trim();

            var selectedResolution = _resolutionDropdown.SelectedValue;
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

            _settings.Fullscreen = _fullscreenCheckbox.IsChecked;
            _settings.Save();

            _state = MainMenuState.Main;
        }

        if (_backButton.IsClicked)
        {
            _serverAddressField.Text = _settings.ServerAddress;
            _resolutionDropdown.SelectedIndex = _resolutionDropdown.Options.IndexOf($"{_settings.ResolutionWidth}x{_settings.ResolutionHeight}");
            _fullscreenCheckbox.IsChecked = _settings.Fullscreen;
            _state = MainMenuState.Main;
        }
    }

    private void UpdateError(MouseState mouseState)
    {
        _errorOkButton.Update(mouseState);

        if (_errorOkButton.IsClicked)
        {
            _state = MainMenuState.Main;
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (_pixelTexture == null || _font == null)
            return;

        spriteBatch.Begin(sortMode: SpriteSortMode.Deferred, blendState: BlendState.AlphaBlend);

        DrawBackground(spriteBatch);

        switch (_state)
        {
            case MainMenuState.Main:
                DrawMainMenu(spriteBatch);
                break;

            case MainMenuState.Settings:
                DrawSettings(spriteBatch);
                break;

            case MainMenuState.Connecting:
                DrawConnecting(spriteBatch);
                break;

            case MainMenuState.Error:
                DrawError(spriteBatch);
                break;
        }

        spriteBatch.End();
    }

    private void DrawBackground(SpriteBatch spriteBatch)
    {
        var titleText = "RiskyStars";
        var titleSize = _font!.MeasureString(titleText);
        var titleScale = 2.5f;
        var titlePosition = new Vector2(
            (_screenWidth - titleSize.X * titleScale) / 2,
            80);

        spriteBatch.DrawString(_font, titleText, titlePosition, Color.Cyan, 0f, Vector2.Zero, titleScale, SpriteEffects.None, 0f);
    }

    private void DrawMainMenu(SpriteBatch spriteBatch)
    {
        _connectButton.Draw(spriteBatch, _pixelTexture!, _font!);
        _settingsButton.Draw(spriteBatch, _pixelTexture!, _font!);
        _exitButton.Draw(spriteBatch, _pixelTexture!, _font!);
    }

    private void DrawSettings(SpriteBatch spriteBatch)
    {
        int panelWidth = 600;
        int panelHeight = 450;
        int panelX = (_screenWidth - panelWidth) / 2;
        int panelY = (_screenHeight - panelHeight) / 2;

        var panel = new Rectangle(panelX, panelY, panelWidth, panelHeight);
        spriteBatch.Draw(_pixelTexture!, panel, Color.Black * 0.9f);
        DrawRectangleOutline(spriteBatch, panel, Color.Cyan, 3);

        var titleText = "Settings";
        var titleSize = _font!.MeasureString(titleText);
        spriteBatch.DrawString(_font, titleText,
            new Vector2(panelX + (panelWidth - titleSize.X) / 2, panelY + 20),
            Color.Cyan, 0f, Vector2.Zero, 1.2f, SpriteEffects.None, 0f);

        _serverAddressField.Draw(spriteBatch, _pixelTexture!, _font);
        _resolutionDropdown.Draw(spriteBatch, _pixelTexture!, _font);
        _fullscreenCheckbox.Draw(spriteBatch, _pixelTexture!, _font);
        _saveSettingsButton.Draw(spriteBatch, _pixelTexture!, _font);
        _backButton.Draw(spriteBatch, _pixelTexture!, _font);
    }

    private void DrawConnecting(SpriteBatch spriteBatch)
    {
        var message = "Connecting to server...";
        var messageSize = _font!.MeasureString(message);
        var messagePosition = new Vector2(
            (_screenWidth - messageSize.X) / 2,
            _screenHeight / 2);

        spriteBatch.DrawString(_font, message, messagePosition, Color.Yellow);
    }

    private void DrawError(SpriteBatch spriteBatch)
    {
        int panelWidth = 500;
        int panelHeight = 300;
        int panelX = (_screenWidth - panelWidth) / 2;
        int panelY = (_screenHeight - panelHeight) / 2;

        var panel = new Rectangle(panelX, panelY, panelWidth, panelHeight);
        spriteBatch.Draw(_pixelTexture!, panel, Color.Black * 0.95f);
        DrawRectangleOutline(spriteBatch, panel, Color.Red, 3);

        var titleText = "Connection Error";
        var titleSize = _font!.MeasureString(titleText);
        spriteBatch.DrawString(_font, titleText,
            new Vector2(panelX + (panelWidth - titleSize.X) / 2, panelY + 20),
            Color.Red, 0f, Vector2.Zero, 1.2f, SpriteEffects.None, 0f);

        var lines = WrapText(_errorMessage, _font, panelWidth - 60);
        float y = panelY + 80;
        foreach (var line in lines)
        {
            var lineSize = _font.MeasureString(line);
            spriteBatch.DrawString(_font, line,
                new Vector2(panelX + (panelWidth - lineSize.X) / 2, y),
                Color.White, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
            y += lineSize.Y + 5;
        }

        _errorOkButton.Draw(spriteBatch, _pixelTexture!, _font);
    }

    private void DrawRectangleOutline(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness)
    {
        if (_pixelTexture == null) return;

        spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(_pixelTexture, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
    }

    private List<string> WrapText(string text, SpriteFont font, float maxWidth)
    {
        var lines = new List<string>();
        var words = text.Split(' ');
        var currentLine = "";

        foreach (var word in words)
        {
            var testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
            var testSize = font.MeasureString(testLine);

            if (testSize.X * 0.8f > maxWidth && !string.IsNullOrEmpty(currentLine))
            {
                lines.Add(currentLine);
                currentLine = word;
            }
            else
            {
                currentLine = testLine;
            }
        }

        if (!string.IsNullOrEmpty(currentLine))
            lines.Add(currentLine);

        return lines;
    }
}
