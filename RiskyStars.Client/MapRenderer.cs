using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RiskyStars.Client;

public class MapRenderer
{
    private readonly GraphicsDevice _graphicsDevice;
    private Texture2D? _pixelTexture;
    private SpriteFont? _font;

    public MapRenderer(GraphicsDevice graphicsDevice)
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

    public void Draw(SpriteBatch spriteBatch, MapData mapData, Camera2D camera)
    {
        if (_pixelTexture == null) return;

        var transform = camera.GetTransformMatrix();

        spriteBatch.Begin(
            sortMode: SpriteSortMode.Deferred,
            blendState: BlendState.AlphaBlend,
            samplerState: SamplerState.LinearClamp,
            transformMatrix: transform);

        foreach (var lane in mapData.HyperspaceLanes)
        {
            var systemA = mapData.StarSystems.FirstOrDefault(s => s.Id == lane.StarSystemAId);
            var systemB = mapData.StarSystems.FirstOrDefault(s => s.Id == lane.StarSystemBId);

            if (systemA != null && systemB != null)
            {
                DrawLine(spriteBatch, systemA.Position, systemB.Position, Color.Gray, 2f);
            }
        }

        foreach (var system in mapData.StarSystems)
        {
            DrawStarSystem(spriteBatch, system);
        }

        spriteBatch.End();
    }

    private void DrawStarSystem(SpriteBatch spriteBatch, StarSystemData system)
    {
        if (_pixelTexture == null) return;

        Color systemColor = system.Type switch
        {
            StarSystemType.Home => Color.Yellow,
            StarSystemType.Featured => Color.Orange,
            StarSystemType.Minor => Color.LightGray,
            _ => Color.White
        };

        DrawCircle(spriteBatch, system.Position, 80f, systemColor, 3f);

        if (_font != null)
        {
            var textSize = _font.MeasureString(system.Name);
            spriteBatch.DrawString(_font, system.Name, 
                system.Position - new Vector2(textSize.X / 2, -90), 
                Color.White, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
        }

        foreach (var body in system.StellarBodies)
        {
            DrawStellarBody(spriteBatch, body);
        }
    }

    private void DrawStellarBody(SpriteBatch spriteBatch, StellarBodyData body)
    {
        if (_pixelTexture == null) return;

        Color bodyColor = body.Type switch
        {
            StellarBodyType.GasGiant => new Color(200, 150, 100),
            StellarBodyType.RockyPlanet => new Color(100, 150, 200),
            StellarBodyType.Planetoid => new Color(150, 150, 150),
            StellarBodyType.Comet => new Color(150, 200, 255),
            _ => Color.White
        };

        float bodyRadius = body.Type switch
        {
            StellarBodyType.GasGiant => 20f,
            StellarBodyType.RockyPlanet => 15f,
            StellarBodyType.Planetoid => 8f,
            StellarBodyType.Comet => 6f,
            _ => 10f
        };

        DrawFilledCircle(spriteBatch, body.Position, bodyRadius, bodyColor);

        foreach (var region in body.Regions)
        {
            DrawRegionMarker(spriteBatch, region);
        }
    }

    private void DrawRegionMarker(SpriteBatch spriteBatch, RegionData region)
    {
        if (_pixelTexture == null) return;

        DrawCircle(spriteBatch, region.Position, 3f, Color.White, 1f);
    }

    private void DrawLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, float thickness)
    {
        if (_pixelTexture == null) return;

        var distance = Vector2.Distance(start, end);
        var angle = (float)Math.Atan2(end.Y - start.Y, end.X - start.X);

        spriteBatch.Draw(_pixelTexture, start, null, color, angle, 
            new Vector2(0, 0.5f), new Vector2(distance, thickness), 
            SpriteEffects.None, 0f);
    }

    private void DrawCircle(SpriteBatch spriteBatch, Vector2 center, float radius, Color color, float thickness)
    {
        if (_pixelTexture == null) return;

        int segments = Math.Max(16, (int)(radius / 2));
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
        if (_pixelTexture == null) return;

        int segments = Math.Max(16, (int)(radius));
        for (int layer = 0; layer < radius; layer++)
        {
            float currentRadius = radius - layer;
            int layerSegments = Math.Max(8, segments - layer / 2);
            Vector2 previousPoint = center + new Vector2(currentRadius, 0);

            for (int i = 1; i <= layerSegments; i++)
            {
                float angle = (float)(i * 2 * Math.PI / layerSegments);
                Vector2 currentPoint = center + new Vector2(
                    currentRadius * (float)Math.Cos(angle),
                    currentRadius * (float)Math.Sin(angle)
                );

                DrawLine(spriteBatch, previousPoint, currentPoint, color, 1f);
                previousPoint = currentPoint;
            }
        }
    }
}
