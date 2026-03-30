using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Linq;

namespace RiskyStars.Client;

public class AIPlayerSlot
{
    public int SlotIndex { get; set; }
    public bool IsEnabled { get; set; }
    public string AIName { get; set; }
    public string Difficulty { get; set; }

    public AIPlayerSlot(int slotIndex)
    {
        SlotIndex = slotIndex;
        IsEnabled = false;
        AIName = $"AI Player {slotIndex}";
        Difficulty = "Normal";
    }
}

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

    private List<AIPlayerSlot> _aiPlayerSlots;
    private List<CheckboxField> _aiEnabledCheckboxes;
    private List<TextInputField> _aiNameFields;
    private List<DropdownField> _aiDifficultyDropdowns;

    private KeyboardState _previousKeyState;
    private int _scrollOffset = 0;
    private const int MaxVisibleSlots = 3;

    public bool ShouldStartGame { get; private set; }
    public bool ShouldGoBack { get; private set; }
    public string PlayerName { get; private set; } = "Player";
    public string SelectedMap { get; private set; } = "Default";
    public List<AIPlayerSlot> AIPlayers => _aiPlayerSlots.Where(s => s.IsEnabled).ToList();

    public SinglePlayerLobbyScreen(GraphicsDevice graphicsDevice, int screenWidth, int screenHeight)
    {
        _graphicsDevice = graphicsDevice;
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;

        CreatePixelTexture();

        int panelWidth = 500;
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

        _aiPlayerSlots = new List<AIPlayerSlot>();
        _aiEnabledCheckboxes = new List<CheckboxField>();
        _aiNameFields = new List<TextInputField>();
        _aiDifficultyDropdowns = new List<DropdownField>();

        int aiSectionY = startY + fieldSpacing * 2 + 40;
        int slotHeight = 100;
        var difficulties = new List<string> { "Easy", "Normal", "Hard" };

        for (int i = 0; i < 7; i++)
        {
            var aiSlot = new AIPlayerSlot(i + 1);
            _aiPlayerSlots.Add(aiSlot);

            int slotY = aiSectionY + i * slotHeight;

            var checkbox = new CheckboxField(
                new Rectangle(centerX, slotY, 40, 40),
                $"AI Player {i + 1}");
            _aiEnabledCheckboxes.Add(checkbox);

            var nameField = new TextInputField(
                new Rectangle(centerX + 150, slotY, 200, 40),
                "", 15);
            nameField.Text = $"AI Player {i + 1}";
            _aiNameFields.Add(nameField);

            var difficultyDropdown = new DropdownField(
                new Rectangle(centerX + 370, slotY, 130, 40),
                "", difficulties);
            difficultyDropdown.SelectedIndex = 1;
            _aiDifficultyDropdowns.Add(difficultyDropdown);
        }

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

        _playerNameField.Update(mouseState, keyState, _previousKeyState);
        _mapDropdown.Update(mouseState);

        for (int i = 0; i < _aiPlayerSlots.Count; i++)
        {
            _aiEnabledCheckboxes[i].Update(mouseState);
            _aiPlayerSlots[i].IsEnabled = _aiEnabledCheckboxes[i].IsChecked;

            if (_aiPlayerSlots[i].IsEnabled)
            {
                _aiNameFields[i].Update(mouseState, keyState, _previousKeyState);
                _aiDifficultyDropdowns[i].Update(mouseState);
                
                _aiPlayerSlots[i].AIName = _aiNameFields[i].Text;
                _aiPlayerSlots[i].Difficulty = _aiDifficultyDropdowns[i].SelectedValue;
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

        int panelWidth = 600;
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
        var aiHeaderText = "AI Opponents";
        var aiHeaderSize = _font.MeasureString(aiHeaderText);
        spriteBatch.DrawString(_font, aiHeaderText,
            new Vector2(panelX + 20, aiSectionY - 25),
            Color.Yellow, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);

        int aiPanelHeight = 320;
        var aiPanel = new Rectangle(panelX + 10, aiSectionY, panelWidth - 20, aiPanelHeight);
        spriteBatch.Draw(_pixelTexture, aiPanel, Color.Black * 0.7f);
        DrawRectangleOutline(spriteBatch, aiPanel, Color.Gray, 2);

        for (int i = 0; i < Math.Min(MaxVisibleSlots, _aiPlayerSlots.Count); i++)
        {
            int slotIndex = _scrollOffset + i;
            if (slotIndex >= _aiPlayerSlots.Count) break;

            _aiEnabledCheckboxes[slotIndex].Draw(spriteBatch, _pixelTexture, _font);

            if (_aiPlayerSlots[slotIndex].IsEnabled)
            {
                _aiNameFields[slotIndex].Draw(spriteBatch, _pixelTexture, _font);
                _aiDifficultyDropdowns[slotIndex].Draw(spriteBatch, _pixelTexture, _font);
            }
        }

        int enabledCount = _aiPlayerSlots.Count(s => s.IsEnabled);
        var countText = $"AI Players: {enabledCount}/7";
        var countSize = _font.MeasureString(countText);
        spriteBatch.DrawString(_font, countText,
            new Vector2(panelX + panelWidth - countSize.X * 0.7f - 20, aiSectionY - 25),
            Color.LightGray, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);

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
