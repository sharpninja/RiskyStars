using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RiskyStars.Shared;

namespace RiskyStars.Client;

public class UIRenderer
{
    private readonly GraphicsDevice _graphicsDevice;
    private Texture2D? _pixelTexture;
    private SpriteFont? _font;
    private int _screenWidth;
    private int _screenHeight;

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

    public void ResizeViewport(int screenWidth, int screenHeight)
    {
        if (screenWidth <= 0 || screenHeight <= 0)
        {
            return;
        }

        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
    }

    public void Draw(SpriteBatch spriteBatch, GameStateCache gameStateCache, string? currentPlayerId)
    {
        if (_pixelTexture == null || _font == null)
        {
            return;
        }

        spriteBatch.Begin(
            sortMode: SpriteSortMode.Deferred,
            blendState: BlendState.AlphaBlend,
            samplerState: SamplerState.LinearClamp);

        DrawTopBar(spriteBatch, gameStateCache);
        DrawResourcePanel(spriteBatch, gameStateCache, currentPlayerId);
        DrawGameInfo(spriteBatch, gameStateCache);

        spriteBatch.End();
    }
    
    public void DrawSelectionInfo(SpriteBatch spriteBatch, SelectionState selection, GameStateCache gameStateCache)
    {
        if (_pixelTexture == null || _font == null)
        {
            return;
        }

        spriteBatch.Begin(
            sortMode: SpriteSortMode.Deferred,
            blendState: BlendState.AlphaBlend,
            samplerState: SamplerState.LinearClamp);

        if (selection.Type != SelectionType.None)
        {
            DrawSelectionPanel(spriteBatch, selection, gameStateCache);
        }

        spriteBatch.End();
    }
    
    public void DrawKeyboardShortcuts(SpriteBatch spriteBatch, bool showHelp)
    {
        if (_pixelTexture == null || _font == null || !showHelp)
        {
            return;
        }

        spriteBatch.Begin(
            sortMode: SpriteSortMode.Deferred,
            blendState: BlendState.AlphaBlend,
            samplerState: SamplerState.LinearClamp);

        DrawShortcutsPanel(spriteBatch);

        spriteBatch.End();
    }

    private void DrawTopBar(SpriteBatch spriteBatch, GameStateCache gameStateCache)
    {
        if (_pixelTexture == null || _font == null)
        {
            return;
        }

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
    }

    private void DrawResourceDisplay(SpriteBatch spriteBatch, int x, int y, string resourceName, int amount, Color color)
    {
        if (_font == null || _pixelTexture == null)
        {
            return;
        }

        int iconSize = 20;
        Rectangle iconRect = new Rectangle(x, y, iconSize, iconSize);
        spriteBatch.Draw(_pixelTexture, iconRect, color);

        string text = $"{resourceName}: {amount}";
        spriteBatch.DrawString(_font, text, new Vector2(x + iconSize + 10, y), Color.White, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
    }

    private void DrawGameInfo(SpriteBatch spriteBatch, GameStateCache gameStateCache)
    {
        if (_pixelTexture == null || _font == null)
        {
            return;
        }

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
        if (_pixelTexture == null)
        {
            return;
        }

        spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(_pixelTexture, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
    }

    private void DrawSelectionPanel(SpriteBatch spriteBatch, SelectionState selection, GameStateCache gameStateCache)
    {
        if (_pixelTexture == null || _font == null)
        {
            return;
        }

        int panelWidth = 300;
        int panelHeight = 200;
        int panelX = (_screenWidth - panelWidth) / 2;
        int panelY = _screenHeight - panelHeight - 10;

        Rectangle panel = new Rectangle(panelX, panelY, panelWidth, panelHeight);
        spriteBatch.Draw(_pixelTexture, panel, Color.Black * 0.8f);
        DrawRectangleOutline(spriteBatch, panel, Color.Yellow, 2);

        int yOffset = panelY + 10;

        if (selection.SelectedArmy != null)
        {
            var army = selection.SelectedArmy;
            spriteBatch.DrawString(_font, "Selected: Army", new Vector2(panelX + 10, yOffset), Color.Yellow);
            yOffset += 30;
            spriteBatch.DrawString(_font, $"ID: {army.ArmyId}", new Vector2(panelX + 10, yOffset), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            yOffset += 25;
            spriteBatch.DrawString(_font, $"Owner: {army.OwnerId}", new Vector2(panelX + 10, yOffset), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            yOffset += 25;
            spriteBatch.DrawString(_font, $"Units: {army.UnitCount}", new Vector2(panelX + 10, yOffset), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            yOffset += 25;
            spriteBatch.DrawString(_font, $"Location: {army.LocationId}", new Vector2(panelX + 10, yOffset), Color.White, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
            yOffset += 25;
            string status = army.HasMovedThisTurn ? "Moved" : "Ready";
            if (army.IsInCombat)
            {
                status = "In Combat";
            }

            spriteBatch.DrawString(_font, $"Status: {status}", new Vector2(panelX + 10, yOffset), army.HasMovedThisTurn ? Color.Gray : Color.Green, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
        }
        else if (selection.SelectedRegion != null)
        {
            var region = selection.SelectedRegion;
            spriteBatch.DrawString(_font, "Selected: Region", new Vector2(panelX + 10, yOffset), Color.Yellow);
            yOffset += 30;
            spriteBatch.DrawString(_font, $"Name: {region.Name}", new Vector2(panelX + 10, yOffset), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            yOffset += 25;
            
            var ownership = gameStateCache.GetRegionOwnership(region.Id);
            if (ownership != null && !string.IsNullOrEmpty(ownership.OwnerId))
            {
                spriteBatch.DrawString(_font, $"Owner: {ownership.OwnerId}", new Vector2(panelX + 10, yOffset), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            }
            else
            {
                spriteBatch.DrawString(_font, "Unowned", new Vector2(panelX + 10, yOffset), Color.Gray, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            }
        }
        else if (selection.SelectedHyperspaceLaneMouthId != null)
        {
            spriteBatch.DrawString(_font, "Selected: Hyperspace Lane Mouth", new Vector2(panelX + 10, yOffset), Color.Yellow);
            yOffset += 30;
            
            var ownership = gameStateCache.GetHyperspaceLaneMouthOwnership(selection.SelectedHyperspaceLaneMouthId);
            if (ownership != null && !string.IsNullOrEmpty(ownership.OwnerId))
            {
                spriteBatch.DrawString(_font, $"Owner: {ownership.OwnerId}", new Vector2(panelX + 10, yOffset), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            }
            else
            {
                spriteBatch.DrawString(_font, "Unowned", new Vector2(panelX + 10, yOffset), Color.Gray, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            }
        }
        else if (selection.SelectedStellarBody != null)
        {
            var body = selection.SelectedStellarBody;
            spriteBatch.DrawString(_font, "Selected: Stellar Body", new Vector2(panelX + 10, yOffset), Color.Yellow);
            yOffset += 30;
            spriteBatch.DrawString(_font, $"Name: {body.Name}", new Vector2(panelX + 10, yOffset), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            yOffset += 25;
            spriteBatch.DrawString(_font, $"Type: {body.Type}", new Vector2(panelX + 10, yOffset), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            yOffset += 25;
            spriteBatch.DrawString(_font, $"Regions: {body.Regions.Count}", new Vector2(panelX + 10, yOffset), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
        }
        else if (selection.SelectedStarSystem != null)
        {
            var system = selection.SelectedStarSystem;
            spriteBatch.DrawString(_font, "Selected: Star System", new Vector2(panelX + 10, yOffset), Color.Yellow);
            yOffset += 30;
            spriteBatch.DrawString(_font, $"Name: {system.Name}", new Vector2(panelX + 10, yOffset), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            yOffset += 25;
            spriteBatch.DrawString(_font, $"Type: {system.Type}", new Vector2(panelX + 10, yOffset), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            yOffset += 25;
            spriteBatch.DrawString(_font, $"Bodies: {system.StellarBodies.Count}", new Vector2(panelX + 10, yOffset), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
        }
    }
    
    private void DrawShortcutsPanel(SpriteBatch spriteBatch)
    {
        if (_pixelTexture == null || _font == null)
        {
            return;
        }

        int panelWidth = 350;
        int panelHeight = 300;
        int panelX = (_screenWidth - panelWidth) / 2;
        int panelY = (_screenHeight - panelHeight) / 2;

        Rectangle panel = new Rectangle(panelX, panelY, panelWidth, panelHeight);
        spriteBatch.Draw(_pixelTexture, panel, Color.Black * 0.9f);
        DrawRectangleOutline(spriteBatch, panel, Color.Cyan, 2);

        int yOffset = panelY + 10;
        spriteBatch.DrawString(_font, "Keyboard Shortcuts", new Vector2(panelX + 10, yOffset), Color.Cyan);
        yOffset += 35;

        var shortcuts = new[]
        {
            ("Left Click", "Select unit/location"),
            ("Right Click", "Move selected army"),
            ("Tab", "Cycle through armies"),
            ("C", "Center on selection"),
            ("Space", "Advance phase"),
            ("P", "Produce resources"),
            ("B / 1", "Purchase 1 army"),
            ("5", "Purchase 5 armies"),
            ("0", "Purchase 10 armies"),
            ("R", "Reinforce location"),
            ("Esc", "Clear selection"),
            ("F1", "Toggle debug info"),
            ("F2", "Toggle dashboard"),
            ("H", "Toggle this help")
        };

        foreach (var (key, description) in shortcuts)
        {
            spriteBatch.DrawString(_font, key, new Vector2(panelX + 10, yOffset), Color.Yellow, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
            spriteBatch.DrawString(_font, description, new Vector2(panelX + 120, yOffset), Color.White, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
            yOffset += 20;
        }
    }

    public void DrawDebugInfo(SpriteBatch spriteBatch, Camera2D camera)
    {
        if (_font == null)
        {
            return;
        }

        spriteBatch.Begin();

        string debugText = $"Camera Pos: ({camera.Position.X:F0}, {camera.Position.Y:F0})\n" +
                          $"Zoom: {camera.Zoom:F2}";

        spriteBatch.DrawString(_font, debugText,
            new Vector2(10, _screenHeight - 60), Color.Lime, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);

        spriteBatch.End();
    }
}
