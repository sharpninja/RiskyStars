using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RiskyStars.Shared;

namespace RiskyStars.Client;

public class SelectionRenderer
{
    private readonly GraphicsDevice _graphicsDevice;
    private Texture2D? _pixelTexture;
    private SpriteFont? _font;
    
    private float _animationTime;
    
    public SelectionRenderer(GraphicsDevice graphicsDevice)
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
    
    public void Update(GameTime gameTime)
    {
        _animationTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
    }
    
    public void Draw(SpriteBatch spriteBatch, MapData mapData, GameStateCache gameStateCache, InputController inputController, Camera2D camera)
    {
        if (_pixelTexture == null)
        {
            return;
        }

        var selection = inputController.Selection;
        if (selection.Type == SelectionType.None)
        {
            return;
        }

        var transform = camera.GetTransformMatrix();
        
        spriteBatch.Begin(
            sortMode: SpriteSortMode.Deferred,
            blendState: BlendState.AlphaBlend,
            samplerState: SamplerState.LinearClamp,
            transformMatrix: transform);
        
        float pulse = 0.5f + 0.5f * MathF.Sin(_animationTime * 3f);
        Color selectionColor = Color.White * (0.6f + 0.4f * pulse);
        
        if (selection.SelectedArmy != null)
        {
            DrawArmySelection(spriteBatch, mapData, selection.SelectedArmy, selectionColor);
        }
        else if (selection.SelectedRegion != null)
        {
            DrawRegionSelection(spriteBatch, selection.SelectedRegion, selectionColor);
        }
        else if (selection.SelectedHyperspaceLaneMouthPosition != null)
        {
            DrawHyperspaceLaneMouthSelection(spriteBatch, selection.SelectedHyperspaceLaneMouthPosition.Value, selectionColor);
        }
        else if (selection.SelectedStellarBody != null)
        {
            DrawStellarBodySelection(spriteBatch, selection.SelectedStellarBody, selectionColor);
        }
        else if (selection.SelectedStarSystem != null)
        {
            DrawStarSystemSelection(spriteBatch, selection.SelectedStarSystem, selectionColor);
        }
        
        spriteBatch.End();
    }
    
    private void DrawArmySelection(SpriteBatch spriteBatch, MapData mapData, ArmyState army, Color color)
    {
        Vector2? position = null;
        
        if (army.LocationType == LocationType.Region)
        {
            foreach (var system in mapData.StarSystems)
            {
                foreach (var body in system.StellarBodies)
                {
                    foreach (var region in body.Regions)
                    {
                        if (region.Id == army.LocationId)
                        {
                            position = region.Position;
                            break;
                        }
                    }
                }
            }
        }
        else if (army.LocationType == LocationType.HyperspaceLaneMouth)
        {
            foreach (var lane in mapData.HyperspaceLanes)
            {
                if (lane.MouthAId == army.LocationId)
                {
                    position = lane.MouthAPosition;
                    break;
                }
                if (lane.MouthBId == army.LocationId)
                {
                    position = lane.MouthBPosition;
                    break;
                }
            }
        }
        
        if (position.HasValue)
        {
            DrawCircle(spriteBatch, position.Value, 18f, color, 3f);
        }
    }
    
    private void DrawRegionSelection(SpriteBatch spriteBatch, RegionData region, Color color)
    {
        DrawCircle(spriteBatch, region.Position, 12f, color, 2f);
    }
    
    private void DrawHyperspaceLaneMouthSelection(SpriteBatch spriteBatch, Vector2 position, Color color)
    {
        DrawSquare(spriteBatch, position, 14f, color, 3f);
    }
    
    private void DrawStellarBodySelection(SpriteBatch spriteBatch, StellarBodyData body, Color color)
    {
        float bodyRadius = body.Type switch
        {
            StellarBodyType.GasGiant => 25f,
            StellarBodyType.RockyPlanet => 20f,
            StellarBodyType.Planetoid => 13f,
            StellarBodyType.Comet => 11f,
            _ => 15f
        };
        
        DrawCircle(spriteBatch, body.Position, bodyRadius, color, 2f);
    }
    
    private void DrawStarSystemSelection(SpriteBatch spriteBatch, StarSystemData system, Color color)
    {
        DrawCircle(spriteBatch, system.Position, 85f, color, 3f);
    }
    
    private void DrawCircle(SpriteBatch spriteBatch, Vector2 center, float radius, Color color, float thickness)
    {
        if (_pixelTexture == null)
        {
            return;
        }

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
    
    private void DrawSquare(SpriteBatch spriteBatch, Vector2 center, float size, Color color, float thickness)
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
        
        DrawLine(spriteBatch, topLeft, topRight, color, thickness);
        DrawLine(spriteBatch, topRight, bottomRight, color, thickness);
        DrawLine(spriteBatch, bottomRight, bottomLeft, color, thickness);
        DrawLine(spriteBatch, bottomLeft, topLeft, color, thickness);
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
}
