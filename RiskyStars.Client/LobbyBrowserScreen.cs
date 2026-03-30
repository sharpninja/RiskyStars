using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RiskyStars.Shared;

namespace RiskyStars.Client;

public class LobbyBrowserScreen
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly int _screenWidth;
    private readonly int _screenHeight;
    private Texture2D? _pixelTexture;
    private SpriteFont? _font;

    private List<LobbyInfo> _lobbies = new();
    private int _selectedLobbyIndex = -1;
    private double _refreshTimer = 0;
    private const double RefreshInterval = 2000;

    private Button _createLobbyButton;
    private Button _joinLobbyButton;
    private Button _refreshButton;

    private int _scrollOffset = 0;
    private const int MaxVisibleLobbies = 8;
    private const int LobbyItemHeight = 60;

    public string? SelectedLobbyId { get; private set; }
    public bool ShouldCreateLobby { get; private set; }
    public bool ShouldJoinLobby { get; private set; }
    public bool ShouldRefresh { get; private set; }

    public LobbyBrowserScreen(GraphicsDevice graphicsDevice, int screenWidth, int screenHeight)
    {
        _graphicsDevice = graphicsDevice;
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;

        CreatePixelTexture();

        int buttonWidth = 150;
        int buttonHeight = 40;
        int buttonSpacing = 20;
        int bottomY = screenHeight - 80;

        _createLobbyButton = new Button(
            new Rectangle(screenWidth / 2 - buttonWidth - buttonSpacing / 2 - buttonWidth, bottomY, buttonWidth, buttonHeight),
            "Create Lobby");

        _joinLobbyButton = new Button(
            new Rectangle(screenWidth / 2 - buttonWidth / 2, bottomY, buttonWidth, buttonHeight),
            "Join Lobby");
        _joinLobbyButton.IsEnabled = false;

        _refreshButton = new Button(
            new Rectangle(screenWidth / 2 + buttonWidth / 2 + buttonSpacing, bottomY, buttonWidth, buttonHeight),
            "Refresh");
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
        ShouldCreateLobby = false;
        ShouldJoinLobby = false;
        ShouldRefresh = false;

        _refreshTimer += gameTime.ElapsedGameTime.TotalMilliseconds;
        if (_refreshTimer >= RefreshInterval)
        {
            ShouldRefresh = true;
            _refreshTimer = 0;
        }

        _createLobbyButton.Update(mouseState);
        _joinLobbyButton.Update(mouseState);
        _refreshButton.Update(mouseState);

        if (_createLobbyButton.IsClicked)
        {
            ShouldCreateLobby = true;
        }

        if (_joinLobbyButton.IsClicked && _selectedLobbyIndex >= 0 && _selectedLobbyIndex < _lobbies.Count)
        {
            SelectedLobbyId = _lobbies[_selectedLobbyIndex].LobbyId;
            ShouldJoinLobby = true;
        }

        if (_refreshButton.IsClicked)
        {
            ShouldRefresh = true;
            _refreshTimer = 0;
        }

        int listY = 120;
        int listHeight = _screenHeight - listY - 120;
        
        for (int i = 0; i < Math.Min(_lobbies.Count, MaxVisibleLobbies); i++)
        {
            int displayIndex = i + _scrollOffset;
            if (displayIndex >= _lobbies.Count)
                break;

            var itemRect = new Rectangle(50, listY + i * LobbyItemHeight, _screenWidth - 100, LobbyItemHeight - 10);
            
            if (itemRect.Contains(mouseState.Position) && mouseState.LeftButton == ButtonState.Pressed)
            {
                _selectedLobbyIndex = displayIndex;
                _joinLobbyButton.IsEnabled = true;
            }
        }
    }

    public void SetLobbies(List<LobbyInfo> lobbies)
    {
        _lobbies = lobbies ?? new List<LobbyInfo>();
        
        if (_selectedLobbyIndex >= _lobbies.Count)
        {
            _selectedLobbyIndex = -1;
            _joinLobbyButton.IsEnabled = false;
        }
    }

    public void Reset()
    {
        ShouldCreateLobby = false;
        ShouldJoinLobby = false;
        ShouldRefresh = false;
        SelectedLobbyId = null;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (_pixelTexture == null || _font == null)
            return;

        spriteBatch.Begin(sortMode: SpriteSortMode.Deferred, blendState: BlendState.AlphaBlend);

        var titleText = "Game Lobbies";
        var titleSize = _font.MeasureString(titleText);
        spriteBatch.DrawString(_font, titleText,
            new Vector2((_screenWidth - titleSize.X) / 2, 30),
            Color.Cyan, 0f, Vector2.Zero, 1.5f, SpriteEffects.None, 0f);

        var subText = $"Available Lobbies: {_lobbies.Count}";
        var subSize = _font.MeasureString(subText);
        spriteBatch.DrawString(_font, subText,
            new Vector2((_screenWidth - subSize.X) / 2, 80),
            Color.White, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);

        int listY = 120;
        int listHeight = _screenHeight - listY - 120;

        if (_lobbies.Count == 0)
        {
            var noLobbiesText = "No lobbies available. Create one to start playing!";
            var noLobbiesSize = _font.MeasureString(noLobbiesText);
            spriteBatch.DrawString(_font, noLobbiesText,
                new Vector2((_screenWidth - noLobbiesSize.X) / 2, listY + listHeight / 2),
                Color.Gray, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
        }
        else
        {
            for (int i = 0; i < Math.Min(_lobbies.Count, MaxVisibleLobbies); i++)
            {
                int displayIndex = i + _scrollOffset;
                if (displayIndex >= _lobbies.Count)
                    break;

                var lobby = _lobbies[displayIndex];
                var itemRect = new Rectangle(50, listY + i * LobbyItemHeight, _screenWidth - 100, LobbyItemHeight - 10);

                bool isSelected = displayIndex == _selectedLobbyIndex;
                Color bgColor = isSelected ? new Color(50, 80, 120) : new Color(30, 30, 40);
                Color borderColor = isSelected ? Color.Cyan : Color.Gray;

                spriteBatch.Draw(_pixelTexture, itemRect, bgColor);
                DrawRectangleOutline(spriteBatch, itemRect, borderColor, 2);

                int textX = itemRect.X + 10;
                int textY = itemRect.Y + 5;

                var hostText = $"Host: {lobby.HostPlayerName}";
                spriteBatch.DrawString(_font, hostText,
                    new Vector2(textX, textY),
                    Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);

                var modeText = $"Mode: {lobby.GameMode} | Map: {lobby.MapName}";
                spriteBatch.DrawString(_font, modeText,
                    new Vector2(textX, textY + 20),
                    Color.LightGray, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);

                var playersText = $"{lobby.CurrentPlayers}/{lobby.MaxPlayers} Players";
                var playersSize = _font.MeasureString(playersText);
                Color playersColor = lobby.CurrentPlayers >= lobby.MaxPlayers ? Color.Red : Color.LightGreen;
                spriteBatch.DrawString(_font, playersText,
                    new Vector2(itemRect.Right - playersSize.X * 0.7f - 10, textY + 15),
                    playersColor, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            }
        }

        _createLobbyButton.Draw(spriteBatch, _pixelTexture, _font);
        _joinLobbyButton.Draw(spriteBatch, _pixelTexture, _font);
        _refreshButton.Draw(spriteBatch, _pixelTexture, _font);

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
