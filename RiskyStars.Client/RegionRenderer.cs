using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RiskyStars.Shared;

namespace RiskyStars.Client;

public class RegionRenderer
{
    private readonly GraphicsDevice _graphicsDevice;
    private Texture2D? _pixelTexture;
    private SpriteFont? _font;

    private readonly Dictionary<string, Color> _playerColors = new();
    private readonly Color[] _defaultColors = new[]
    {
        Color.Red,
        Color.Blue,
        Color.Green,
        Color.Yellow,
        Color.Purple,
        Color.Cyan
    };

    public RegionRenderer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
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

    public void Draw(SpriteBatch spriteBatch, MapData mapData, GameStateCache gameStateCache, Camera2D camera)
    {
        if (_pixelTexture == null)
        {
            return;
        }

        var transform = camera.GetTransformMatrix();

        spriteBatch.Begin(
            sortMode: SpriteSortMode.Deferred,
            blendState: BlendState.AlphaBlend,
            samplerState: SamplerState.LinearClamp,
            transformMatrix: transform);

        AssignPlayerColors(gameStateCache);

        foreach (var system in mapData.StarSystems)
        {
            foreach (var body in system.StellarBodies)
            {
                foreach (var region in body.Regions)
                {
                    DrawRegionOwnership(spriteBatch, region, gameStateCache);
                }
            }
        }

        foreach (var lane in mapData.HyperspaceLanes)
        {
            DrawHyperspaceLaneMouth(spriteBatch, lane.MouthAPosition, lane.MouthAId, gameStateCache);
            DrawHyperspaceLaneMouth(spriteBatch, lane.MouthBPosition, lane.MouthBId, gameStateCache);
        }

        foreach (var system in mapData.StarSystems)
        {
            foreach (var body in system.StellarBodies)
            {
                foreach (var region in body.Regions)
                {
                    DrawArmiesAtRegion(spriteBatch, region, gameStateCache);
                }
            }
        }

        spriteBatch.End();
    }

    private void AssignPlayerColors(GameStateCache gameStateCache)
    {
        var playerStates = gameStateCache.GetAllPlayerStates();
        int colorIndex = 0;

        foreach (var playerState in playerStates)
        {
            if (!_playerColors.ContainsKey(playerState.PlayerId))
            {
                _playerColors[playerState.PlayerId] = _defaultColors[colorIndex % _defaultColors.Length];
                colorIndex++;
            }
        }
    }

    private void DrawRegionOwnership(SpriteBatch spriteBatch, RegionData region, GameStateCache gameStateCache)
    {
        if (_pixelTexture == null)
        {
            return;
        }

        var ownership = gameStateCache.GetRegionOwnership(region.Id);
        if (ownership == null || string.IsNullOrEmpty(ownership.OwnerId))
        {
            return;
        }

        Color ownerColor = GetPlayerColor(ownership.OwnerId);
        DrawFilledCircle(spriteBatch, region.Position, 5f, ownerColor * 0.7f);
        DrawCircle(spriteBatch, region.Position, 6f, ownerColor, 2f);
    }

    private void DrawHyperspaceLaneMouth(SpriteBatch spriteBatch, Vector2 position, string mouthId, GameStateCache gameStateCache)
    {
        if (_pixelTexture == null)
        {
            return;
        }

        var ownership = gameStateCache.GetHyperspaceLaneMouthOwnership(mouthId);
        if (ownership == null || string.IsNullOrEmpty(ownership.OwnerId))
        {
            DrawSquare(spriteBatch, position, 8f, Color.Gray * 0.5f);
            return;
        }

        Color ownerColor = GetPlayerColor(ownership.OwnerId);
        DrawFilledSquare(spriteBatch, position, 8f, ownerColor * 0.7f);
        DrawSquare(spriteBatch, position, 9f, ownerColor);

        var armies = gameStateCache.GetArmiesAtLocation(mouthId, LocationType.HyperspaceLaneMouth);
        if (armies.Count > 0)
        {
            int totalUnits = armies.Sum(a => a.UnitCount);
            DrawArmyCount(spriteBatch, position + new Vector2(0, -15), totalUnits, ownerColor);
        }
    }

    private void DrawArmiesAtRegion(SpriteBatch spriteBatch, RegionData region, GameStateCache gameStateCache)
    {
        if (_pixelTexture == null || _font == null)
        {
            return;
        }

        var armies = gameStateCache.GetArmiesAtLocation(region.Id, LocationType.Region);
        if (armies.Count == 0)
        {
            return;
        }

        var groupedArmies = armies.GroupBy(a => a.OwnerId);
        int offsetY = 10;

        foreach (var group in groupedArmies)
        {
            int totalUnits = group.Sum(a => a.UnitCount);
            Color playerColor = GetPlayerColor(group.Key);

            DrawArmyCount(spriteBatch, region.Position + new Vector2(0, offsetY), totalUnits, playerColor);
            offsetY += 15;
        }
    }

    private void DrawArmyCount(SpriteBatch spriteBatch, Vector2 position, int count, Color color)
    {
        if (_font == null)
        {
            return;
        }

        string text = count.ToString();
        var textSize = _font.MeasureString(text);
        var bgRect = new Rectangle(
            (int)(position.X - textSize.X / 2 - 2),
            (int)(position.Y - textSize.Y / 2 - 2),
            (int)(textSize.X + 4),
            (int)(textSize.Y + 4)
        );

        spriteBatch.Draw(_pixelTexture, bgRect, Color.Black * 0.7f);

        spriteBatch.DrawString(_font, text,
            position - textSize / 2,
            color, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
    }

    private void DrawCircle(SpriteBatch spriteBatch, Vector2 center, float radius, Color color, float thickness)
    {
        if (_pixelTexture == null)
        {
            return;
        }

        int segments = Math.Max(12, (int)(radius));
        Vector2 previousPoint = center + new Vector2(radius, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = (float)(i * 2 * Math.PI / segments);
            Vector2 currentPoint = center + new Vector2(
                radius * (float)Math.Cos(angle),
                radius * (float)Math.Sin(angle)
            );

            DrawLine(spriteBatch, previousPoint, currentPoint, color, thickness);
            previousPoint = currentPoint;
        }
    }

    private void DrawFilledCircle(SpriteBatch spriteBatch, Vector2 center, float radius, Color color)
    {
        if (_pixelTexture == null)
        {
            return;
        }

        Rectangle rect = new Rectangle(
            (int)(center.X - radius),
            (int)(center.Y - radius),
            (int)(radius * 2),
            (int)(radius * 2)
        );

        spriteBatch.Draw(_pixelTexture, rect, color);
    }

    private void DrawSquare(SpriteBatch spriteBatch, Vector2 center, float size, Color color)
    {
        if (_pixelTexture == null)
        {
            return;
        }

        float halfSize = size / 2;
        Vector2 topLeft = center - new Vector2(halfSize, halfSize);
        Vector2 topRight = center + new Vector2(halfSize, -halfSize);
        Vector2 bottomRight = center + new Vector2(halfSize, halfSize);
        Vector2 bottomLeft = center + new Vector2(-halfSize, halfSize);

        DrawLine(spriteBatch, topLeft, topRight, color, 2f);
        DrawLine(spriteBatch, topRight, bottomRight, color, 2f);
        DrawLine(spriteBatch, bottomRight, bottomLeft, color, 2f);
        DrawLine(spriteBatch, bottomLeft, topLeft, color, 2f);
    }

    private void DrawFilledSquare(SpriteBatch spriteBatch, Vector2 center, float size, Color color)
    {
        if (_pixelTexture == null)
        {
            return;
        }

        Rectangle rect = new Rectangle(
            (int)(center.X - size / 2),
            (int)(center.Y - size / 2),
            (int)size,
            (int)size
        );

        spriteBatch.Draw(_pixelTexture, rect, color);
    }

    private void DrawLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, float thickness)
    {
        if (_pixelTexture == null)
        {
            return;
        }

        var distance = Vector2.Distance(start, end);
        var angle = (float)Math.Atan2(end.Y - start.Y, end.X - start.X);

        spriteBatch.Draw(_pixelTexture, start, null, color, angle,
            new Vector2(0, 0.5f), new Vector2(distance, thickness),
            SpriteEffects.None, 0f);
    }

    public void SetPlayerColor(string playerId, Color color)
    {
        _playerColors[playerId] = color;
    }

    public Color GetPlayerColor(string playerId)
    {
        return _playerColors.TryGetValue(playerId, out var color) ? color : Color.White;
    }
}
