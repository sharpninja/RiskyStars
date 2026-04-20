using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RiskyStars.Shared;

namespace RiskyStars.Client;

public class AIActionIndicator
{
    private readonly GraphicsDevice _graphicsDevice;
    private Texture2D? _pixelTexture;
    private SpriteFont? _font;

    private bool _isAIThinking;
    private string? _aiPlayerName;

    private readonly List<ReinforcementHighlight> _reinforcementHighlights = new();
    private readonly List<ArmyMovementAnimation> _movementAnimations = new();
    private readonly List<GameLogEntry> _gameLog = new();
    private const int MaxLogEntries = 5;

    public AIActionIndicator(GraphicsDevice graphicsDevice, int screenWidth, int screenHeight)
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

    public void ResizeViewport(int screenWidth, int screenHeight)
    {
        // World-space effects do not need screen dimensions anymore.
    }

    public void Update(GameTime gameTime, GameStateCache gameStateCache, MapData mapData, string? currentPlayerId)
    {
        var currentPlayerIdInTurn = gameStateCache.GetCurrentPlayerId();
        
        if (currentPlayerIdInTurn != currentPlayerId && !string.IsNullOrEmpty(currentPlayerIdInTurn))
        {
            var playerState = gameStateCache.GetPlayerState(currentPlayerIdInTurn);
            if (playerState != null)
            {
                if (!_isAIThinking)
                {
                    StartAIThinking(playerState.PlayerName);
                }
            }
        }
        else
        {
            if (_isAIThinking)
            {
                StopAIThinking();
            }
        }

        if (_isAIThinking)
        {
            // State stays live so the Myra HUD can reflect active AI turns.
        }

        for (int i = _reinforcementHighlights.Count - 1; i >= 0; i--)
        {
            _reinforcementHighlights[i].Update(gameTime);
            if (_reinforcementHighlights[i].IsExpired)
            {
                _reinforcementHighlights.RemoveAt(i);
            }
        }

        for (int i = _movementAnimations.Count - 1; i >= 0; i--)
        {
            _movementAnimations[i].Update(gameTime);
            if (_movementAnimations[i].IsComplete)
            {
                _movementAnimations.RemoveAt(i);
            }
        }

        for (int i = _gameLog.Count - 1; i >= 0; i--)
        {
            _gameLog[i].Update(gameTime);
            if (_gameLog[i].IsExpired)
            {
                _gameLog.RemoveAt(i);
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch, Camera2D camera, MapData mapData)
    {
        if (_pixelTexture == null || _font == null)
        {
            return;
        }

        DrawWorldSpaceElements(spriteBatch, camera, mapData);
    }

    private void DrawWorldSpaceElements(SpriteBatch spriteBatch, Camera2D camera, MapData mapData)
    {
        if (_pixelTexture == null || _font == null)
        {
            return;
        }

        var transform = camera.GetTransformMatrix();

        spriteBatch.Begin(
            sortMode: SpriteSortMode.Deferred,
            blendState: BlendState.AlphaBlend,
            samplerState: SamplerState.LinearClamp,
            transformMatrix: transform);

        foreach (var highlight in _reinforcementHighlights)
        {
            var position = GetLocationPosition(highlight.LocationId, highlight.LocationType, mapData);
            if (position.HasValue)
            {
                DrawReinforcementHighlight(spriteBatch, position.Value, highlight.Progress, highlight.PlayerColor);
            }
        }

        foreach (var animation in _movementAnimations)
        {
            DrawMovementAnimation(spriteBatch, animation);
        }

        spriteBatch.End();
    }

    private void DrawReinforcementHighlight(SpriteBatch spriteBatch, Vector2 position, float progress, Color playerColor)
    {
        if (_pixelTexture == null)
        {
            return;
        }

        float pulseSize = 15f + (float)Math.Sin(progress * Math.PI * 4) * 5f;
        float alpha = 1f - progress;

        DrawCircleOutline(spriteBatch, position, pulseSize, playerColor * alpha, 3f);
        DrawCircleOutline(spriteBatch, position, pulseSize + 5f, playerColor * alpha * 0.5f, 2f);
    }

    private void DrawMovementAnimation(SpriteBatch spriteBatch, ArmyMovementAnimation animation)
    {
        if (_pixelTexture == null || _font == null)
        {
            return;
        }

        Vector2 currentPosition = Vector2.Lerp(animation.StartPosition, animation.EndPosition, animation.Progress);
        
        DrawFilledCircle(spriteBatch, currentPosition, 8f, animation.PlayerColor * 0.8f);
        DrawCircleOutline(spriteBatch, currentPosition, 9f, animation.PlayerColor, 2f);

        DrawLine(spriteBatch, animation.StartPosition, animation.EndPosition, animation.PlayerColor * 0.3f, 2f);

        if (animation.UnitCount > 0)
        {
            string countText = animation.UnitCount.ToString();
            var textSize = _font.MeasureString(countText) * 0.5f;
            spriteBatch.DrawString(_font, countText, currentPosition - textSize / 2, Color.White, 
                0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
        }
    }

    private void DrawCircleOutline(SpriteBatch spriteBatch, Vector2 center, float radius, Color color, float thickness)
    {
        if (_pixelTexture == null)
        {
            return;
        }

        int segments = Math.Max(16, (int)(radius * 2));
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

    private Vector2? GetLocationPosition(string locationId, LocationType locationType, MapData mapData)
    {
        if (locationType == LocationType.Region)
        {
            foreach (var system in mapData.StarSystems)
            {
                foreach (var body in system.StellarBodies)
                {
                    foreach (var region in body.Regions)
                    {
                        if (region.Id == locationId)
                        {
                            return region.Position;
                        }
                    }
                }
            }
        }
        else if (locationType == LocationType.HyperspaceLaneMouth)
        {
            foreach (var lane in mapData.HyperspaceLanes)
            {
                if (lane.MouthAId == locationId)
                {
                    return lane.MouthAPosition;
                }
                if (lane.MouthBId == locationId)
                {
                    return lane.MouthBPosition;
                }
            }
        }

        return null;
    }

    public void StartAIThinking(string aiPlayerName)
    {
        _isAIThinking = true;
        _aiPlayerName = aiPlayerName;
    }

    public void StopAIThinking()
    {
        _isAIThinking = false;
        _aiPlayerName = null;
    }

    public void ShowReinforcement(string locationId, LocationType locationType, int unitCount, Color playerColor)
    {
        _reinforcementHighlights.Add(new ReinforcementHighlight
        {
            LocationId = locationId,
            LocationType = locationType,
            UnitCount = unitCount,
            PlayerColor = playerColor,
            Progress = 0f,
            Duration = 2.0,
            IsExpired = false
        });
    }

    public void ShowArmyMovement(Vector2 startPosition, Vector2 endPosition, int unitCount, Color playerColor, string? armyId = null)
    {
        _movementAnimations.Add(new ArmyMovementAnimation
        {
            StartPosition = startPosition,
            EndPosition = endPosition,
            UnitCount = unitCount,
            PlayerColor = playerColor,
            ArmyId = armyId,
            Progress = 0f,
            Duration = 1.5,
            IsComplete = false
        });
    }

    public void AddLogEntry(string message, Color color)
    {
        _gameLog.Insert(0, new GameLogEntry
        {
            Message = message,
            Color = color,
            TimeRemaining = 5.0,
            FadeProgress = 0f,
            IsExpired = false
        });

        while (_gameLog.Count > MaxLogEntries)
        {
            _gameLog.RemoveAt(_gameLog.Count - 1);
        }
    }

    public void TrackArmyMovement(Camera2D camera, ArmyMovementAnimation animation)
    {
        if (animation.Progress < 0.8f)
        {
            Vector2 currentPosition = Vector2.Lerp(animation.StartPosition, animation.EndPosition, animation.Progress);
            camera.SmoothCenterOn(currentPosition);
        }
    }

    public bool HasActiveMovementAnimations()
    {
        return _movementAnimations.Count > 0;
    }

    public ArmyMovementAnimation? GetFirstMovementAnimation()
    {
        return _movementAnimations.Count > 0 ? _movementAnimations[0] : null;
    }

    public bool IsAIThinking => _isAIThinking;

    public string? ActiveAIPlayerName => _aiPlayerName;

    public IReadOnlyList<GameLogEntry> GetRecentLogEntries() => _gameLog;
}

public class ReinforcementHighlight
{
    public string LocationId { get; set; } = string.Empty;
    public LocationType LocationType { get; set; }
    public int UnitCount { get; set; }
    public Color PlayerColor { get; set; }
    public float Progress { get; set; }
    public double Duration { get; set; }
    public bool IsExpired { get; set; }

    public void Update(GameTime gameTime)
    {
        Progress += (float)(gameTime.ElapsedGameTime.TotalSeconds / Duration);
        if (Progress >= 1f)
        {
            IsExpired = true;
        }
    }
}

public class ArmyMovementAnimation
{
    public Vector2 StartPosition { get; set; }
    public Vector2 EndPosition { get; set; }
    public int UnitCount { get; set; }
    public Color PlayerColor { get; set; }
    public string? ArmyId { get; set; }
    public float Progress { get; set; }
    public double Duration { get; set; }
    public bool IsComplete { get; set; }

    public void Update(GameTime gameTime)
    {
        Progress += (float)(gameTime.ElapsedGameTime.TotalSeconds / Duration);
        if (Progress >= 1f)
        {
            Progress = 1f;
            IsComplete = true;
        }
    }
}

public class GameLogEntry
{
    public string Message { get; set; } = string.Empty;
    public Color Color { get; set; }
    public double TimeRemaining { get; set; }
    public float FadeProgress { get; set; }
    public bool IsExpired { get; set; }

    public void Update(GameTime gameTime)
    {
        TimeRemaining -= gameTime.ElapsedGameTime.TotalSeconds;
        
        if (TimeRemaining <= 1.0)
        {
            FadeProgress = 1f - (float)(TimeRemaining / 1.0);
        }
        
        if (TimeRemaining <= 0)
        {
            IsExpired = true;
        }
    }
}
