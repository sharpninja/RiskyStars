using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RiskyStars.Shared;

namespace RiskyStars.Client;

public class CreateLobbyScreen
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly int _screenWidth;
    private readonly int _screenHeight;
    private Texture2D? _pixelTexture;
    private SpriteFont? _font;

    private TextInputField _mapNameField;
    private NumericInputField _maxPlayersField;
    private Button _createButton;
    private Button _cancelButton;

    private KeyboardState _previousKeyState;

    public bool ShouldCreate { get; private set; }
    public bool ShouldCancel { get; private set; }
    public LobbySettingsProto? LobbySettings { get; private set; }

    public CreateLobbyScreen(GraphicsDevice graphicsDevice, int screenWidth, int screenHeight)
    {
        _graphicsDevice = graphicsDevice;
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;

        CreatePixelTexture();

        int panelWidth = 400;
        int centerX = (_screenWidth - panelWidth) / 2;
        int centerY = _screenHeight / 2 - 120;

        _mapNameField = new TextInputField(
            new Rectangle(centerX, centerY, panelWidth, 40),
            "Map Name", 30);
        _mapNameField.Text = "Default";

        _maxPlayersField = new NumericInputField(
            new Rectangle(centerX, centerY + 80, panelWidth, 40),
            "Max Players (2-6)", 4, 2, 6);

        int buttonWidth = 150;
        int buttonY = centerY + 160;
        
        _createButton = new Button(
            new Rectangle(centerX + 50, buttonY, buttonWidth, 50),
            "Create");

        _cancelButton = new Button(
            new Rectangle(centerX + panelWidth - buttonWidth - 50, buttonY, buttonWidth, 50),
            "Cancel");
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
        ShouldCreate = false;
        ShouldCancel = false;

        _mapNameField.Update(mouseState, keyState, _previousKeyState);
        _maxPlayersField.Update(mouseState);
        _createButton.Update(mouseState);
        _cancelButton.Update(mouseState);

        if (_createButton.IsClicked)
        {
            if (TryCreateLobbySettings(out var settings))
            {
                LobbySettings = settings;
                ShouldCreate = true;
            }
        }

        if (_cancelButton.IsClicked || (keyState.IsKeyDown(Keys.Escape) && _previousKeyState.IsKeyUp(Keys.Escape)))
        {
            ShouldCancel = true;
        }

        _previousKeyState = keyState;
    }

    private bool TryCreateLobbySettings(out LobbySettingsProto? settings)
    {
        settings = null;

        if (string.IsNullOrWhiteSpace(_mapNameField.Text))
            return false;

        settings = new LobbySettingsProto
        {
            MinPlayers = 2,
            MaxPlayers = _maxPlayersField.Value,
            GameMode = "Standard",
            MapName = _mapNameField.Text.Trim(),
            StartingPopulation = 100,
            StartingMetal = 50,
            StartingFuel = 50,
            AllowSpectators = false,
            TurnTimeLimit = 300
        };

        return true;
    }

    public void Reset()
    {
        ShouldCreate = false;
        ShouldCancel = false;
        LobbySettings = null;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (_pixelTexture == null || _font == null)
            return;

        spriteBatch.Begin(sortMode: SpriteSortMode.Deferred, blendState: BlendState.AlphaBlend);

        int panelWidth = 500;
        int panelHeight = 350;
        int panelX = (_screenWidth - panelWidth) / 2;
        int panelY = (_screenHeight - panelHeight) / 2;

        var panel = new Rectangle(panelX, panelY, panelWidth, panelHeight);
        spriteBatch.Draw(_pixelTexture, panel, Color.Black * 0.95f);
        DrawRectangleOutline(spriteBatch, panel, Color.Cyan, 3);

        var titleText = "Create Lobby";
        var titleSize = _font.MeasureString(titleText);
        spriteBatch.DrawString(_font, titleText,
            new Vector2(panelX + (panelWidth - titleSize.X) / 2, panelY + 20),
            Color.Cyan, 0f, Vector2.Zero, 1.2f, SpriteEffects.None, 0f);

        _mapNameField.Draw(spriteBatch, _pixelTexture, _font);
        _maxPlayersField.Draw(spriteBatch, _pixelTexture, _font);
        _createButton.Draw(spriteBatch, _pixelTexture, _font);
        _cancelButton.Draw(spriteBatch, _pixelTexture, _font);

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
