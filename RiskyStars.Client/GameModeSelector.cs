using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace RiskyStars.Client;

public class GameModeSelector
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly int _screenWidth;
    private readonly int _screenHeight;
    private Texture2D? _pixelTexture;
    private SpriteFont? _font;

    private RadioButtonGroup _modeGroup;
    private Button _continueButton;
    private Button _backButton;

    public GameMode? SelectedMode { get; private set; }
    public bool ShouldProceed { get; private set; }
    public bool ShouldGoBack { get; private set; }

    public GameModeSelector(GraphicsDevice graphicsDevice, int screenWidth, int screenHeight)
    {
        _graphicsDevice = graphicsDevice;
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;

        CreatePixelTexture();

        int radioWidth = 400;
        int radioHeight = 50;
        int radioSpacing = 30;
        int centerX = (_screenWidth - radioWidth) / 2;
        int startY = _screenHeight / 2 - 50;

        _modeGroup = new RadioButtonGroup();
        
        var multiplayerRadio = new RadioButton(
            new Rectangle(centerX, startY, radioWidth, radioHeight),
            "Multiplayer - Connect to server and play with others",
            "gameMode");
        
        var singlePlayerRadio = new RadioButton(
            new Rectangle(centerX, startY + radioHeight + radioSpacing, radioWidth, radioHeight),
            "Single Player - Play offline against AI",
            "gameMode");

        _modeGroup.AddRadioButton(multiplayerRadio);
        _modeGroup.AddRadioButton(singlePlayerRadio);
        _modeGroup.SetSelected(0);

        int buttonWidth = 150;
        int buttonHeight = 50;
        int buttonY = startY + (radioHeight + radioSpacing) * 2 + 40;

        _continueButton = new Button(
            new Rectangle(centerX + radioWidth / 2 - buttonWidth - 10, buttonY, buttonWidth, buttonHeight),
            "Continue");

        _backButton = new Button(
            new Rectangle(centerX + radioWidth / 2 + 10, buttonY, buttonWidth, buttonHeight),
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

    public void Update(GameTime gameTime, MouseState mouseState)
    {
        ShouldProceed = false;
        ShouldGoBack = false;

        _modeGroup.Update(mouseState);
        _continueButton.Update(mouseState);
        _backButton.Update(mouseState);

        if (_continueButton.IsClicked)
        {
            if (_modeGroup.SelectedIndex == 0)
            {
                SelectedMode = GameMode.Multiplayer;
            }
            else if (_modeGroup.SelectedIndex == 1)
            {
                SelectedMode = GameMode.SinglePlayer;
            }
            
            if (SelectedMode.HasValue)
            {
                ShouldProceed = true;
            }
        }

        if (_backButton.IsClicked)
        {
            ShouldGoBack = true;
        }
    }

    public void Reset()
    {
        SelectedMode = null;
        ShouldProceed = false;
        ShouldGoBack = false;
        _modeGroup.SetSelected(0);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (_pixelTexture == null || _font == null)
            return;

        spriteBatch.Begin(sortMode: SpriteSortMode.Deferred, blendState: BlendState.AlphaBlend);

        var titleText = "Select Game Mode";
        var titleSize = _font.MeasureString(titleText);
        var titleScale = 2.0f;
        var titlePosition = new Vector2(
            (_screenWidth - titleSize.X * titleScale) / 2,
            100);

        spriteBatch.DrawString(_font, titleText, titlePosition, Color.Cyan, 0f, Vector2.Zero, titleScale, SpriteEffects.None, 0f);

        var subtitleText = "Choose how you want to play";
        var subtitleSize = _font.MeasureString(subtitleText);
        var subtitlePosition = new Vector2(
            (_screenWidth - subtitleSize.X) / 2,
            180);

        spriteBatch.DrawString(_font, subtitleText, subtitlePosition, Color.White, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);

        int panelWidth = 600;
        int panelHeight = 350;
        int panelX = (_screenWidth - panelWidth) / 2;
        int panelY = _screenHeight / 2 - 100;

        var panel = new Rectangle(panelX, panelY, panelWidth, panelHeight);
        spriteBatch.Draw(_pixelTexture, panel, Color.Black * 0.85f);
        DrawRectangleOutline(spriteBatch, panel, Color.Cyan, 2);

        _modeGroup.Draw(spriteBatch, _pixelTexture, _font);
        _continueButton.Draw(spriteBatch, _pixelTexture, _font);
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
