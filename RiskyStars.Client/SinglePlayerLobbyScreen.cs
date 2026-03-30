using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace RiskyStars.Client;

public class SinglePlayerLobbyScreen
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly int _screenWidth;
    private readonly int _screenHeight;
    private Texture2D? _pixelTexture;
    private SpriteFont? _font;

    private TextInputField _playerNameField;
    private DropdownField _difficultyDropdown;
    private DropdownField _mapDropdown;
    private Button _startGameButton;
    private Button _backButton;

    private KeyboardState _previousKeyState;

    public bool ShouldStartGame { get; private set; }
    public bool ShouldGoBack { get; private set; }
    public string PlayerName { get; private set; } = "Player";
    public string SelectedDifficulty { get; private set; } = "Normal";
    public string SelectedMap { get; private set; } = "Default";

    public SinglePlayerLobbyScreen(GraphicsDevice graphicsDevice, int screenWidth, int screenHeight)
    {
        _graphicsDevice = graphicsDevice;
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;

        CreatePixelTexture();

        int panelWidth = 400;
        int centerX = (_screenWidth - panelWidth) / 2;
        int startY = 200;
        int fieldSpacing = 70;

        _playerNameField = new TextInputField(
            new Rectangle(centerX, startY, panelWidth, 40),
            "Your Name", 20);
        _playerNameField.Text = "Player";

        var difficulties = new List<string> { "Easy", "Normal", "Hard" };
        _difficultyDropdown = new DropdownField(
            new Rectangle(centerX, startY + fieldSpacing, panelWidth, 40),
            "Difficulty", difficulties);
        _difficultyDropdown.SelectedIndex = 1;

        var maps = new List<string> { "Default", "Small", "Medium", "Large" };
        _mapDropdown = new DropdownField(
            new Rectangle(centerX, startY + fieldSpacing * 2, panelWidth, 40),
            "Map", maps);

        int buttonWidth = 150;
        int buttonY = startY + fieldSpacing * 3 + 20;

        _startGameButton = new Button(
            new Rectangle(centerX + panelWidth / 2 - buttonWidth - 10, buttonY, buttonWidth, 50),
            "Start Game");

        _backButton = new Button(
            new Rectangle(centerX + panelWidth / 2 + 10, buttonY, buttonWidth, 50),
            "Back");
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
        ShouldStartGame = false;
        ShouldGoBack = false;

        _playerNameField.Update(mouseState, keyState, _previousKeyState);
        _difficultyDropdown.Update(mouseState);
        _mapDropdown.Update(mouseState);
        _startGameButton.Update(mouseState);
        _backButton.Update(mouseState);

        if (_startGameButton.IsClicked)
        {
            if (!string.IsNullOrWhiteSpace(_playerNameField.Text))
            {
                PlayerName = _playerNameField.Text.Trim();
                SelectedDifficulty = _difficultyDropdown.SelectedValue;
                SelectedMap = _mapDropdown.SelectedValue;
                ShouldStartGame = true;
            }
        }

        if (_backButton.IsClicked || (keyState.IsKeyDown(Keys.Escape) && _previousKeyState.IsKeyUp(Keys.Escape)))
        {
            ShouldGoBack = true;
        }

        _previousKeyState = keyState;
    }

    public void Reset()
    {
        ShouldStartGame = false;
        ShouldGoBack = false;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (_pixelTexture == null || _font == null)
            return;

        spriteBatch.Begin(sortMode: SpriteSortMode.Deferred, blendState: BlendState.AlphaBlend);

        var titleText = "Single Player";
        var titleSize = _font.MeasureString(titleText);
        var titleScale = 1.8f;
        var titlePosition = new Vector2(
            (_screenWidth - titleSize.X * titleScale) / 2,
            80);

        spriteBatch.DrawString(_font, titleText, titlePosition, Color.Cyan, 0f, Vector2.Zero, titleScale, SpriteEffects.None, 0f);

        int panelWidth = 500;
        int panelHeight = 450;
        int panelX = (_screenWidth - panelWidth) / 2;
        int panelY = (_screenHeight - panelHeight) / 2;

        var panel = new Rectangle(panelX, panelY, panelWidth, panelHeight);
        spriteBatch.Draw(_pixelTexture, panel, Color.Black * 0.9f);
        DrawRectangleOutline(spriteBatch, panel, Color.Cyan, 3);

        var subtitleText = "Configure your game";
        var subtitleSize = _font.MeasureString(subtitleText);
        spriteBatch.DrawString(_font, subtitleText,
            new Vector2(panelX + (panelWidth - subtitleSize.X * 0.8f) / 2, panelY + 20),
            Color.White, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);

        _playerNameField.Draw(spriteBatch, _pixelTexture, _font);
        _difficultyDropdown.Draw(spriteBatch, _pixelTexture, _font);
        _mapDropdown.Draw(spriteBatch, _pixelTexture, _font);
        _startGameButton.Draw(spriteBatch, _pixelTexture, _font);
        _backButton.Draw(spriteBatch, _pixelTexture, _font);

        spriteBatch.End();
    }

    private void DrawRectangleOutline(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness)
    {
        if (_pixelTexture == null) return;

        spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(_pixelTexture, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
    }
}
