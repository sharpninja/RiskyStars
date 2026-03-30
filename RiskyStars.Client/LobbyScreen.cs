using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RiskyStars.Shared;

namespace RiskyStars.Client;

public class LobbyScreen
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly int _screenWidth;
    private readonly int _screenHeight;
    private Texture2D? _pixelTexture;
    private SpriteFont? _font;

    private LobbyInfo? _lobbyInfo;
    private string? _currentPlayerId;
    private bool _isHost;
    private bool _isReady;
    private double _refreshTimer = 0;
    private const double RefreshInterval = 1000;

    private Button _readyButton;
    private Button _startGameButton;
    private Button _leaveLobbyButton;

    private List<PlayerSlot> _playerSlots;
    private List<DropdownField> _playerTypeDropdowns;
    private int _maxPlayers = 4;

    public bool ShouldToggleReady { get; private set; }
    public bool ShouldStartGame { get; private set; }
    public bool ShouldLeaveLobby { get; private set; }
    public bool ShouldRefresh { get; private set; }
    public bool GameStarted { get; private set; }
    public string? SessionId { get; private set; }
    public List<PlayerSlot> PlayerSlots => _playerSlots;

    public LobbyScreen(GraphicsDevice graphicsDevice, int screenWidth, int screenHeight)
    {
        _graphicsDevice = graphicsDevice;
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;

        CreatePixelTexture();

        _playerSlots = new List<PlayerSlot>();
        _playerTypeDropdowns = new List<DropdownField>();

        InitializePlayerSlots();

        int buttonWidth = 150;
        int buttonHeight = 40;
        int bottomY = screenHeight - 80;

        _readyButton = new Button(
            new Rectangle(screenWidth / 2 - buttonWidth - 20, bottomY, buttonWidth, buttonHeight),
            "Ready");

        _startGameButton = new Button(
            new Rectangle(screenWidth / 2 - buttonWidth / 2, bottomY - 60, buttonWidth, buttonHeight),
            "Start Game");
        _startGameButton.IsEnabled = false;

        _leaveLobbyButton = new Button(
            new Rectangle(screenWidth / 2 + 20, bottomY, buttonWidth, buttonHeight),
            "Leave Lobby");
    }

    private void InitializePlayerSlots()
    {
        _playerSlots.Clear();
        _playerTypeDropdowns.Clear();

        int panelX = 100;
        int panelY = 240;
        int slotHeight = 50;

        var playerTypeOptions = new List<string> { "Human", "Easy AI", "Medium AI", "Hard AI" };

        for (int i = 0; i < 8; i++)
        {
            var slot = new PlayerSlot(i + 1);
            _playerSlots.Add(slot);

            var dropdown = new DropdownField(
                new Rectangle(panelX + 350, panelY + i * slotHeight + 5, 150, 35),
                "", playerTypeOptions);
            _playerTypeDropdowns.Add(dropdown);
        }
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

    public void SetLobbyInfo(LobbyInfo lobbyInfo, string playerId)
    {
        _lobbyInfo = lobbyInfo;
        _currentPlayerId = playerId;
        _maxPlayers = lobbyInfo.MaxPlayers;
        
        if (_lobbyInfo != null)
        {
            _isHost = _lobbyInfo.HostPlayerName == GetPlayerName(playerId);
            _startGameButton.IsEnabled = _isHost && AllPlayersReady();
        }
    }

    public void SetCurrentPlayer(string playerId)
    {
        _currentPlayerId = playerId;
    }

    public void SetReady(bool ready)
    {
        _isReady = ready;
        _readyButton.IsEnabled = !_isReady;
    }

    public void OnGameStarted(string sessionId)
    {
        SessionId = sessionId;
        GameStarted = true;
    }

    private bool AllPlayersReady()
    {
        if (_lobbyInfo == null)
            return false;

        int occupiedSlots = _playerSlots.Count(s => s.PlayerType == PlayerType.Human || s.IsAI);
        return occupiedSlots >= 2;
    }

    private string GetPlayerName(string playerId)
    {
        if (_lobbyInfo == null || string.IsNullOrEmpty(playerId))
            return "Unknown";
        
        return _lobbyInfo.PlayerNames.FirstOrDefault() ?? "Unknown";
    }

    public void Update(GameTime gameTime, MouseState mouseState)
    {
        ShouldToggleReady = false;
        ShouldStartGame = false;
        ShouldLeaveLobby = false;
        ShouldRefresh = false;

        _refreshTimer += gameTime.ElapsedGameTime.TotalMilliseconds;
        if (_refreshTimer >= RefreshInterval)
        {
            ShouldRefresh = true;
            _refreshTimer = 0;
        }

        if (_isHost)
        {
            for (int i = 0; i < Math.Min(_maxPlayers, _playerSlots.Count); i++)
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
                        _playerSlots[i].IsReady = true;
                    }
                    else if (oldType != PlayerType.Human && newPlayerType != PlayerType.Human)
                    {
                        _playerSlots[i].PlayerName = AINameGenerator.GenerateNameWithSeed(
                            i + 1, 
                            _playerSlots[i].GetDifficultyLevel());
                    }
                    else if (newPlayerType == PlayerType.Human)
                    {
                        _playerSlots[i].PlayerName = $"Player {i + 1}";
                        _playerSlots[i].IsReady = false;
                    }
                }
            }
        }

        _readyButton.Update(mouseState);
        _startGameButton.Update(mouseState);
        _leaveLobbyButton.Update(mouseState);

        if (_readyButton.IsClicked)
        {
            ShouldToggleReady = true;
        }

        if (_startGameButton.IsClicked && _isHost)
        {
            ShouldStartGame = true;
        }

        if (_leaveLobbyButton.IsClicked)
        {
            ShouldLeaveLobby = true;
        }

        if (_isHost)
        {
            int occupiedSlots = 0;
            for (int i = 0; i < Math.Min(_maxPlayers, _playerSlots.Count); i++)
            {
                if (_playerSlots[i].PlayerType != PlayerType.Human || _playerSlots[i].IsReady)
                {
                    occupiedSlots++;
                }
            }
            _startGameButton.IsEnabled = occupiedSlots >= 2;
        }
    }

    public void Reset()
    {
        ShouldToggleReady = false;
        ShouldStartGame = false;
        ShouldLeaveLobby = false;
        ShouldRefresh = false;
        GameStarted = false;
        SessionId = null;
        _isReady = false;
        _readyButton.IsEnabled = true;

        for (int i = 0; i < _playerSlots.Count; i++)
        {
            _playerSlots[i].PlayerType = PlayerType.Human;
            _playerSlots[i].PlayerName = $"Player {i + 1}";
            _playerSlots[i].IsReady = false;
            _playerSlots[i].IsHost = false;
            _playerTypeDropdowns[i].SelectedIndex = 0;
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (_pixelTexture == null || _font == null || _lobbyInfo == null)
            return;

        spriteBatch.Begin(sortMode: SpriteSortMode.Deferred, blendState: BlendState.AlphaBlend);

        var titleText = "Game Lobby";
        var titleSize = _font.MeasureString(titleText);
        spriteBatch.DrawString(_font, titleText,
            new Vector2((_screenWidth - titleSize.X) / 2, 30),
            Color.Cyan, 0f, Vector2.Zero, 1.5f, SpriteEffects.None, 0f);

        int panelX = 100;
        int panelY = 100;
        int panelWidth = _screenWidth - 200;
        int panelHeight = 500;

        var panel = new Rectangle(panelX, panelY, panelWidth, panelHeight);
        spriteBatch.Draw(_pixelTexture, panel, Color.Black * 0.7f);
        DrawRectangleOutline(spriteBatch, panel, Color.Cyan, 2);

        int textX = panelX + 20;
        int textY = panelY + 20;

        spriteBatch.DrawString(_font, $"Host: {_lobbyInfo.HostPlayerName}",
            new Vector2(textX, textY),
            Color.Yellow, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);

        textY += 35;
        spriteBatch.DrawString(_font, $"Map: {_lobbyInfo.MapName}",
            new Vector2(textX, textY),
            Color.White, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);

        textY += 30;
        spriteBatch.DrawString(_font, $"Game Mode: {_lobbyInfo.GameMode}",
            new Vector2(textX, textY),
            Color.White, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);

        textY += 40;
        spriteBatch.DrawString(_font, $"Player Slots:",
            new Vector2(textX, textY),
            Color.Cyan, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);

        textY += 35;
        int slotHeight = 50;

        for (int i = 0; i < Math.Min(_maxPlayers, _playerSlots.Count); i++)
        {
            var slot = _playerSlots[i];
            int slotY = textY + i * slotHeight;

            Color slotColor = Color.DarkGray * 0.5f;
            if (slot.PlayerType != PlayerType.Human || slot.IsReady)
            {
                slotColor = Color.DarkGreen * 0.6f;
            }

            var slotBounds = new Rectangle(panelX + 20, slotY, panelWidth - 40, slotHeight - 5);
            spriteBatch.Draw(_pixelTexture, slotBounds, slotColor);
            DrawRectangleOutline(spriteBatch, slotBounds, Color.Gray, 1);

            string slotText = $"{i + 1}. {slot.PlayerName}";
            Color textColor = Color.White;

            if (slot.IsAI)
            {
                textColor = Color.LightBlue;
            }
            else if (slot.IsReady)
            {
                slotText += " [READY]";
                textColor = Color.LightGreen;
            }

            spriteBatch.DrawString(_font, slotText,
                new Vector2(panelX + 30, slotY + 10),
                textColor, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);

            if (slot.IsAI)
            {
                int badgeWidth = 60;
                int badgeHeight = 22;
                int badgeX = panelX + 250;
                int badgeY = slotY + 12;

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
            }

            if (_isHost)
            {
                _playerTypeDropdowns[i].Draw(spriteBatch, _pixelTexture, _font);
            }
        }

        if (_isHost)
        {
            textY = panelY + panelHeight - 60;
            var hostText = "Configure player slots and start when ready!";
            var hostSize = _font.MeasureString(hostText);
            spriteBatch.DrawString(_font, hostText,
                new Vector2(panelX + (panelWidth - hostSize.X * 0.7f) / 2, textY),
                Color.Yellow, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            
            _startGameButton.Draw(spriteBatch, _pixelTexture, _font);
        }
        else
        {
            textY = panelY + panelHeight - 60;
            var waitText = "Waiting for host to start the game...";
            var waitSize = _font.MeasureString(waitText);
            spriteBatch.DrawString(_font, waitText,
                new Vector2(panelX + (panelWidth - waitSize.X * 0.7f) / 2, textY),
                Color.Gray, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
        }

        _readyButton.Draw(spriteBatch, _pixelTexture, _font);
        _leaveLobbyButton.Draw(spriteBatch, _pixelTexture, _font);

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
