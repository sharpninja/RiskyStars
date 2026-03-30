using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RiskyStars.Shared;

namespace RiskyStars.Client;

public class UIRenderer
{
    private readonly GraphicsDevice _graphicsDevice;
    private Texture2D? _pixelTexture;
    private SpriteFont? _font;
    private readonly int _screenWidth;
    private readonly int _screenHeight;

    public UIRenderer(GraphicsDevice graphicsDevice, int screenWidth, int screenHeight)
    {
        _graphicsDevice = graphicsDevice;
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
        CreatePixelTexture();
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

    public void Draw(SpriteBatch spriteBatch, GameStateCache gameStateCache, string? currentPlayerId)
    {
        if (_pixelTexture == null || _font == null) return;

        spriteBatch.Begin(
            sortMode: SpriteSortMode.Deferred,
            blendState: BlendState.AlphaBlend,
            samplerState: SamplerState.LinearClamp);

        DrawTopBar(spriteBatch, gameStateCache);
        DrawResourcePanel(spriteBatch, gameStateCache, currentPlayerId);
        DrawGameInfo(spriteBatch, gameStateCache);

        spriteBatch.End();
    }

    private void DrawTopBar(SpriteBatch spriteBatch, GameStateCache gameStateCache)
    {
        if (_pixelTexture == null || _font == null) return;

        int barHeight = 40;
        Rectangle topBar = new Rectangle(0, 0, _screenWidth, barHeight);
        spriteBatch.Draw(_pixelTexture, topBar, Color.Black * 0.8f);

        int turnNumber = gameStateCache.GetTurnNumber();
        TurnPhase currentPhase = gameStateCache.GetCurrentPhase();
        string? currentPlayerId = gameStateCache.GetCurrentPlayerId();
        string? eventMessage = gameStateCache.GetEventMessage();

        string phaseText = currentPhase switch
        {
            TurnPhase.Production => "Production Phase",
            TurnPhase.Purchase => "Purchase Phase",
            TurnPhase.Reinforcement => "Reinforcement Phase",
            TurnPhase.Movement => "Movement Phase",
            _ => "Unknown Phase"
        };

        string topText = $"Turn {turnNumber} - {phaseText}";
        if (!string.IsNullOrEmpty(currentPlayerId))
        {
            var playerState = gameStateCache.GetPlayerState(currentPlayerId);
            if (playerState != null)
            {
                topText += $" - Current Player: {playerState.PlayerName}";
            }
        }

        spriteBatch.DrawString(_font, topText, new Vector2(10, 10), Color.White);

        if (!string.IsNullOrEmpty(eventMessage))
        {
            var messageSize = _font.MeasureString(eventMessage);
            spriteBatch.DrawString(_font, eventMessage,
                new Vector2(_screenWidth - messageSize.X - 10, 10),
                Color.Yellow);
        }
    }

    private void DrawResourcePanel(SpriteBatch spriteBatch, GameStateCache gameStateCache, string? playerId)
    {
        if (_pixelTexture == null || _font == null || string.IsNullOrEmpty(playerId)) return;

        var playerState = gameStateCache.GetPlayerState(playerId);
        if (playerState == null) return;

        int panelWidth = 300;
        int panelHeight = 150;
        int panelX = _screenWidth - panelWidth - 10;
        int panelY = 50;

        Rectangle panel = new Rectangle(panelX, panelY, panelWidth, panelHeight);
        spriteBatch.Draw(_pixelTexture, panel, Color.Black * 0.7f);

        DrawRectangleOutline(spriteBatch, panel, Color.White, 2);

        int yOffset = panelY + 10;
        spriteBatch.DrawString(_font, $"Player: {playerState.PlayerName}",
            new Vector2(panelX + 10, yOffset), Color.White);

        yOffset += 30;
        DrawResourceDisplay(spriteBatch, panelX + 10, yOffset, "Population", playerState.PopulationStockpile, new Color(100, 200, 100));
        yOffset += 30;
        DrawResourceDisplay(spriteBatch, panelX + 10, yOffset, "Metal", playerState.MetalStockpile, new Color(150, 150, 150));
        yOffset += 30;
        DrawResourceDisplay(spriteBatch, panelX + 10, yOffset, "Fuel", playerState.FuelStockpile, new Color(200, 150, 100));

        int regionCount = gameStateCache.GetRegionsOwnedByPlayer(playerId).Count;
        int armyCount = gameStateCache.GetArmiesOwnedByPlayer(playerId).Count;
        yOffset += 35;
        spriteBatch.DrawString(_font, $"Regions: {regionCount} | Armies: {armyCount}",
            new Vector2(panelX + 10, yOffset), Color.LightGray, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
    }

    private void DrawResourceDisplay(SpriteBatch spriteBatch, int x, int y, string resourceName, int amount, Color color)
    {
        if (_font == null || _pixelTexture == null) return;

        int iconSize = 20;
        Rectangle iconRect = new Rectangle(x, y, iconSize, iconSize);
        spriteBatch.Draw(_pixelTexture, iconRect, color);

        string text = $"{resourceName}: {amount}";
        spriteBatch.DrawString(_font, text, new Vector2(x + iconSize + 10, y), Color.White, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
    }

    private void DrawGameInfo(SpriteBatch spriteBatch, GameStateCache gameStateCache)
    {
        if (_pixelTexture == null || _font == null) return;

        int panelWidth = 250;
        int panelHeight = 200;
        int panelX = 10;
        int panelY = 50;

        Rectangle panel = new Rectangle(panelX, panelY, panelWidth, panelHeight);
        spriteBatch.Draw(_pixelTexture, panel, Color.Black * 0.7f);

        DrawRectangleOutline(spriteBatch, panel, Color.White, 2);

        int yOffset = panelY + 10;
        spriteBatch.DrawString(_font, "Game Info", new Vector2(panelX + 10, yOffset), Color.Yellow);

        yOffset += 30;
        var playerStates = gameStateCache.GetAllPlayerStates();
        foreach (var player in playerStates.OrderBy(p => p.TurnOrder))
        {
            Color playerColor = Color.White;
            if (player.PlayerId == gameStateCache.GetCurrentPlayerId())
            {
                playerColor = Color.Yellow;
            }

            string playerText = $"{player.TurnOrder + 1}. {player.PlayerName}";
            spriteBatch.DrawString(_font, playerText,
                new Vector2(panelX + 10, yOffset), playerColor, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            yOffset += 25;
        }

        if (gameStateCache.HasCombatEvents())
        {
            yOffset += 10;
            spriteBatch.DrawString(_font, "Combat Active!",
                new Vector2(panelX + 10, yOffset), Color.Red, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
        }
    }

    private void DrawRectangleOutline(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness)
    {
        if (_pixelTexture == null) return;

        spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(_pixelTexture, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
    }

    public void DrawDebugInfo(SpriteBatch spriteBatch, Camera2D camera)
    {
        if (_font == null) return;

        spriteBatch.Begin();

        string debugText = $"Camera Pos: ({camera.Position.X:F0}, {camera.Position.Y:F0})\n" +
                          $"Zoom: {camera.Zoom:F2}";

        spriteBatch.DrawString(_font, debugText,
            new Vector2(10, _screenHeight - 60), Color.Lime, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);

        spriteBatch.End();
    }
}
