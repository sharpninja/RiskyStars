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

    public bool ShouldToggleReady { get; private set; }
    public bool ShouldStartGame { get; private set; }
    public bool ShouldLeaveLobby { get; private set; }
    public bool ShouldRefresh { get; private set; }
    public bool GameStarted { get; private set; }
    public string? SessionId { get; private set; }

    public LobbyScreen(GraphicsDevice graphicsDevice, int screenWidth, int screenHeight)
    {
        _graphicsDevice = graphicsDevice;
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;

        CreatePixelTexture();

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
        return _lobbyInfo.CurrentPlayers >= 2;
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
        int panelHeight = 350;

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
        spriteBatch.DrawString(_font, $"Players ({_lobbyInfo.CurrentPlayers}/{_lobbyInfo.MaxPlayers}):",
            new Vector2(textX, textY),
            Color.Cyan, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);

        textY += 35;
        int playerIndex = 1;
        foreach (var playerName in _lobbyInfo.PlayerNames)
        {
            bool isCurrentPlayer = playerName == GetPlayerName(_currentPlayerId ?? "");
            Color playerColor = isCurrentPlayer ? Color.Yellow : Color.White;
            string readyStatus = isCurrentPlayer && _isReady ? " [READY]" : "";
            
            spriteBatch.DrawString(_font, $"{playerIndex}. {playerName}{readyStatus}",
                new Vector2(textX + 20, textY),
                playerColor, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            
            textY += 28;
            playerIndex++;
        }

        if (_isHost)
        {
            textY = panelY + panelHeight - 60;
            var hostText = "You are the host. Start the game when ready!";
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
