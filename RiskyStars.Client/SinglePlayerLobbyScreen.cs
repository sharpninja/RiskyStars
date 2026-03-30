using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Linq;

namespace RiskyStars.Client;

public class SinglePlayerLobbyScreen
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly int _screenWidth;
    private readonly int _screenHeight;
    private Texture2D? _pixelTexture;
    private SpriteFont? _font;

    private TextInputField _playerNameField;
    private DropdownField _mapDropdown;
    private Button _startGameButton;
    private Button _backButton;

    private List<PlayerSlot> _playerSlots;
    private List<DropdownField> _playerTypeDropdowns;
    private List<Button> _regenerateNameButtons;

    private KeyboardState _previousKeyState;
    private int _scrollOffset = 0;
    private const int MaxVisibleSlots = 4;
    private string? _errorMessage;
    private float _errorDisplayTime;
    private const float ErrorDisplayDuration = 5.0f;
    private const int MaxPlayers = 8;

    public bool ShouldStartGame { get; private set; }
    public bool ShouldGoBack { get; private set; }
    public string PlayerName { get; private set; } = "Player";
    public string SelectedMap { get; private set; } = "Default";
    public List<PlayerSlot> PlayerSlots => _playerSlots;

    public SinglePlayerLobbyScreen(GraphicsDevice graphicsDevice, int screenWidth, int screenHeight)
    {
        _graphicsDevice = graphicsDevice;
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;

        CreatePixelTexture();

        int panelWidth = 600;
        int centerX = (_screenWidth - panelWidth) / 2;
        int startY = 180;
        int fieldSpacing = 70;

        _playerNameField = new TextInputField(
            new Rectangle(centerX, startY, panelWidth, 40),
            "Your Name", 20);
        _playerNameField.Text = "Player";

        var maps = new List<string> { "Default", "Small", "Medium", "Large" };
        _mapDropdown = new DropdownField(
            new Rectangle(centerX, startY + fieldSpacing, panelWidth, 40),
            "Map", maps);

        _playerSlots = new List<PlayerSlot>();
        _playerTypeDropdowns = new List<DropdownField>();
        _regenerateNameButtons = new List<Button>();

        int aiSectionY = startY + fieldSpacing * 2 + 40;
        int slotHeight = 60;
        var playerTypeOptions = new List<string> { "Human", "Easy AI", "Medium AI", "Hard AI" };

        for (int i = 0; i < MaxPlayers; i++)
        {
            var slot = new PlayerSlot(i + 1);
            slot.IsHost = (i == 0);
            _playerSlots.Add(slot);

            int slotY = aiSectionY + i * slotHeight;

            var dropdown = new DropdownField(
                new Rectangle(centerX, slotY, 160, 40),
                "", playerTypeOptions);
            _playerTypeDropdowns.Add(dropdown);

            var regenButton = new Button(
                new Rectangle(centerX + 170, slotY, 40, 40),
                "↻");
            _regenerateNameButtons.Add(regenButton);
        }

        _playerSlots[0].PlayerType = PlayerType.Human;

        int buttonWidth = 150;
        int buttonY = aiSectionY + MaxVisibleSlots * slotHeight + 20;

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

        if (_errorDisplayTime > 0)
        {
            _errorDisplayTime -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_errorDisplayTime <= 0)
            {
                _errorMessage = null;
            }
        }

        _playerNameField.Update(mouseState, keyState, _previousKeyState);
        _mapDropdown.Update(mouseState);

        for (int i = 0; i < _playerSlots.Count; i++)
        {
            _playerTypeDropdowns[i].Update(mouseState);

            var selectedType = _playerTypeDropdowns[i].SelectedValue;
            var newPlayerType = selectedType switch
            {
                "Easy AI" => PlayerType.EasyAI,
                "Medium AI" => PlayerType.MediumAI,
                "Hard AI" => PlayerType.HardAI,
                _ => PlayerType.Human
            };

            if (_playerSlots[i].PlayerType != newPlayerType)
            {
                var oldType = _playerSlots[i].PlayerType;
                _playerSlots[i].PlayerType = newPlayerType;

                if (oldType == PlayerType.Human && newPlayerType != PlayerType.Human)
                {
                    _playerSlots[i].PlayerName = AINameGenerator.GenerateNameWithSeed(
                        i + 1, 
                        _playerSlots[i].GetDifficultyLevel());
                }
                else if (oldType != PlayerType.Human && newPlayerType != PlayerType.Human)
                {
                    _playerSlots[i].PlayerName = AINameGenerator.GenerateNameWithSeed(
                        i + 1, 
                        _playerSlots[i].GetDifficultyLevel());
                }
                else if (newPlayerType == PlayerType.Human && i > 0)
                {
                    _playerSlots[i].PlayerName = $"Player {i + 1}";
                }
            }

            if (_playerSlots[i].IsAI)
            {
                _regenerateNameButtons[i].Update(mouseState);
                if (_regenerateNameButtons[i].IsClicked)
                {
                    _playerSlots[i].PlayerName = AINameGenerator.GenerateName(
                        i + 1,
                        _playerSlots[i].GetDifficultyLevel());
                }
            }
        }

        _startGameButton.Update(mouseState);
        _backButton.Update(mouseState);

        if (_startGameButton.IsClicked)
        {
            if (!string.IsNullOrWhiteSpace(_playerNameField.Text))
            {
                PlayerName = _playerNameField.Text.Trim();
                SelectedMap = _mapDropdown.SelectedValue;
                _playerSlots[0].PlayerName = PlayerName;
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
        _scrollOffset = 0;
        _errorMessage = null;
        _errorDisplayTime = 0;

        for (int i = 0; i < _playerSlots.Count; i++)
        {
            if (i == 0)
            {
                _playerSlots[i].PlayerType = PlayerType.Human;
                _playerSlots[i].PlayerName = "Player";
                _playerTypeDropdowns[i].SelectedIndex = 0;
            }
            else
            {
                _playerSlots[i].PlayerType = PlayerType.Human;
                _playerSlots[i].PlayerName = $"Player {i + 1}";
                _playerTypeDropdowns[i].SelectedIndex = 0;
            }
        }
    }

    public void SetError(string errorMessage)
    {
        _errorMessage = errorMessage;
        _errorDisplayTime = ErrorDisplayDuration;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (_pixelTexture == null || _font == null)
            return;

        spriteBatch.Begin(sortMode: SpriteSortMode.Deferred, blendState: BlendState.AlphaBlend);

        var titleText = "Single Player Game Setup";
        var titleSize = _font.MeasureString(titleText);
        var titleScale = 1.5f;
        var titlePosition = new Vector2(
            (_screenWidth - titleSize.X * titleScale) / 2,
            60);

        spriteBatch.DrawString(_font, titleText, titlePosition, Color.Cyan, 0f, Vector2.Zero, titleScale, SpriteEffects.None, 0f);

        int panelWidth = 700;
        int panelHeight = 620;
        int panelX = (_screenWidth - panelWidth) / 2;
        int panelY = 140;

        var panel = new Rectangle(panelX, panelY, panelWidth, panelHeight);
        spriteBatch.Draw(_pixelTexture, panel, Color.Black * 0.9f);
        DrawRectangleOutline(spriteBatch, panel, Color.Cyan, 3);

        var subtitleText = "Configure your game and AI opponents";
        var subtitleSize = _font.MeasureString(subtitleText);
        spriteBatch.DrawString(_font, subtitleText,
            new Vector2(panelX + (panelWidth - subtitleSize.X * 0.7f) / 2, panelY + 15),
            Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);

        _playerNameField.Draw(spriteBatch, _pixelTexture, _font);
        _mapDropdown.Draw(spriteBatch, _pixelTexture, _font);

        int aiSectionY = panelY + 200;
        var aiHeaderText = "Player Slots";
        var aiHeaderSize = _font.MeasureString(aiHeaderText);
        spriteBatch.DrawString(_font, aiHeaderText,
            new Vector2(panelX + 20, aiSectionY - 25),
            Color.Yellow, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);

        int aiPanelHeight = 360;
        var aiPanel = new Rectangle(panelX + 10, aiSectionY, panelWidth - 20, aiPanelHeight);
        spriteBatch.Draw(_pixelTexture, aiPanel, Color.Black * 0.7f);
        DrawRectangleOutline(spriteBatch, aiPanel, Color.Gray, 2);

        int slotHeight = 60;
        int innerPadding = 10;

        for (int i = 0; i < Math.Min(MaxVisibleSlots, _playerSlots.Count); i++)
        {
            int slotIndex = _scrollOffset + i;
            if (slotIndex >= _playerSlots.Count) break;

            var slot = _playerSlots[slotIndex];
            int slotY = aiSectionY + innerPadding + i * slotHeight;

            Color slotColor = slot.IsAI ? new Color(40, 60, 80) : new Color(30, 40, 50);
            var slotBounds = new Rectangle(panelX + 20, slotY, panelWidth - 40, slotHeight - 10);
            spriteBatch.Draw(_pixelTexture, slotBounds, slotColor);
            DrawRectangleOutline(spriteBatch, slotBounds, Color.Gray, 1);

            string slotLabel = slotIndex == 0 ? "YOU:" : $"Slot {slotIndex + 1}:";
            spriteBatch.DrawString(_font, slotLabel,
                new Vector2(panelX + 30, slotY + 5),
                Color.LightGray, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);

            string displayName = slotIndex == 0 ? _playerNameField.Text : slot.PlayerName;
            Color nameColor = slot.IsAI ? Color.LightBlue : Color.White;

            spriteBatch.DrawString(_font, displayName,
                new Vector2(panelX + 30, slotY + 22),
                nameColor, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);

            if (slot.IsAI)
            {
                int badgeWidth = 60;
                int badgeHeight = 20;
                int badgeX = panelX + 230;
                int badgeY = slotY + 25;

                Color badgeColor = slot.PlayerType switch
                {
                    PlayerType.EasyAI => new Color(100, 180, 100),
                    PlayerType.MediumAI => new Color(200, 180, 100),
                    PlayerType.HardAI => new Color(200, 100, 100),
                    _ => Color.Gray
                };

                var badgeBounds = new Rectangle(badgeX, badgeY, badgeWidth, badgeHeight);
                spriteBatch.Draw(_pixelTexture, badgeBounds, badgeColor * 0.8f);
                DrawRectangleOutline(spriteBatch, badgeBounds, badgeColor, 1);

                var difficultyText = slot.GetDifficultyLevel().ToUpper();
                var difficultySize = _font.MeasureString(difficultyText);
                var difficultyPos = new Vector2(
                    badgeX + (badgeWidth - difficultySize.X * 0.5f) / 2,
                    badgeY + (badgeHeight - difficultySize.Y * 0.5f) / 2);

                spriteBatch.DrawString(_font, difficultyText, difficultyPos,
                    Color.White, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);

                _regenerateNameButtons[slotIndex].Draw(spriteBatch, _pixelTexture, _font);
            }

            _playerTypeDropdowns[slotIndex].Draw(spriteBatch, _pixelTexture, _font);
        }

        int aiCount = _playerSlots.Count(s => s.IsAI);
        int totalPlayers = _playerSlots.Count(s => s.PlayerType != PlayerType.Human || s.IsHost);
        var countText = $"AI Players: {aiCount} | Total: {totalPlayers}/{MaxPlayers}";
        var countSize = _font.MeasureString(countText);
        spriteBatch.DrawString(_font, countText,
            new Vector2(panelX + panelWidth - countSize.X * 0.7f - 20, aiSectionY - 25),
            Color.LightGray, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);

        _startGameButton.Draw(spriteBatch, _pixelTexture, _font);
        _backButton.Draw(spriteBatch, _pixelTexture, _font);

        if (!string.IsNullOrEmpty(_errorMessage))
        {
            int errorPanelWidth = 550;
            int errorPanelHeight = 80;
            int errorPanelX = (_screenWidth - errorPanelWidth) / 2;
            int errorPanelY = panelY + panelHeight + 20;

            var errorPanel = new Rectangle(errorPanelX, errorPanelY, errorPanelWidth, errorPanelHeight);
            spriteBatch.Draw(_pixelTexture, errorPanel, Color.DarkRed * 0.9f);
            DrawRectangleOutline(spriteBatch, errorPanel, Color.Red, 3);

            var errorText = WrapText(_errorMessage, errorPanelWidth - 40, _font);
            var errorTextSize = _font.MeasureString(errorText);
            var errorTextPosition = new Vector2(
                errorPanelX + (errorPanelWidth - errorTextSize.X * 0.7f) / 2,
                errorPanelY + (errorPanelHeight - errorTextSize.Y * 0.7f) / 2);

            spriteBatch.DrawString(_font, errorText, errorTextPosition, Color.Yellow, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
        }

        spriteBatch.End();
    }

    private string WrapText(string text, float maxWidth, SpriteFont font)
    {
        var words = text.Split(' ');
        var lines = new List<string>();
        var currentLine = "";

        foreach (var word in words)
        {
            var testLine = string.IsNullOrEmpty(currentLine) ? word : $"{currentLine} {word}";
            var size = font.MeasureString(testLine);

            if (size.X * 0.7f > maxWidth && !string.IsNullOrEmpty(currentLine))
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
        {
            lines.Add(currentLine);
        }

        return string.Join("\n", lines);
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
