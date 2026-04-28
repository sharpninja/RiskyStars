using Microsoft.Xna.Framework;

namespace RiskyStars.Client;

public static class ContinentZoomLayout
{
    public const int MinimumButtonSize = 48;
    public const int MaximumButtonSize = 120;
    private const int FallbackMaximumButtonSize = 84;
    private const float PositionEpsilon = 0.001f;

    public static bool IsZoomableBody(StellarBodyData? body)
    {
        return body?.Regions.Count > 1;
    }

    public static StellarBodyData? FindZoomableBodyAtPosition(MapData mapData, Vector2 worldPosition)
    {
        foreach (var system in mapData.StarSystems)
        {
            foreach (var body in system.StellarBodies)
            {
                if (!IsZoomableBody(body))
                {
                    continue;
                }

                if (Vector2.Distance(worldPosition, body.Position) <= GetBodyHitRadius(body))
                {
                    return body;
                }
            }
        }

        return null;
    }

    public static IReadOnlyList<ContinentZoomButtonLayout> Build(StellarBodyData body, int width, int height)
    {
        if (body.Regions.Count == 0 || width <= 0 || height <= 0)
        {
            return [];
        }

        var surfaceBounds = GetPlanetSurfaceBounds(width, height);
        if (surfaceBounds.Width <= 0 || surfaceBounds.Height <= 0)
        {
            return [];
        }

        int buttonSize = GetRegionCellSize(body.Regions.Count, surfaceBounds.Width);
        float centerX = surfaceBounds.Center.X;
        float centerY = surfaceBounds.Center.Y;
        float targetRadius = Math.Max(buttonSize, (surfaceBounds.Width - buttonSize) / 2f);

        var regionOffsets = body.Regions
            .Select(region => new
            {
                Region = region,
                Offset = region.Position - body.Position
            })
            .ToArray();
        float sourceRadius = regionOffsets.Max(item => item.Offset.Length());

        if (sourceRadius <= PositionEpsilon)
        {
            return BuildFallbackRing(body, surfaceBounds, centerX, centerY);
        }

        float scale = targetRadius / sourceRadius;
        var layouts = new List<ContinentZoomButtonLayout>(regionOffsets.Length);
        foreach (var item in regionOffsets)
        {
            var scaledOffset = item.Offset * scale;
            if (scaledOffset.Length() is > PositionEpsilon and < MaximumButtonSize)
            {
                scaledOffset = Vector2.Normalize(scaledOffset) * MaximumButtonSize;
            }

            var buttonCenter = new Vector2(centerX, centerY) + scaledOffset;

            int left = (int)MathF.Round(buttonCenter.X - buttonSize / 2f);
            int top = (int)MathF.Round(buttonCenter.Y - buttonSize / 2f);
            left = Math.Clamp(left, surfaceBounds.Left, Math.Max(surfaceBounds.Left, surfaceBounds.Right - buttonSize));
            top = Math.Clamp(top, surfaceBounds.Top, Math.Max(surfaceBounds.Top, surfaceBounds.Bottom - buttonSize));

            layouts.Add(new ContinentZoomButtonLayout(item.Region, new Rectangle(left, top, buttonSize, buttonSize)));
        }

        return layouts;
    }

    public static Rectangle GetPlanetSurfaceBounds(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            return Rectangle.Empty;
        }

        int margin = Math.Max(16, Math.Min(width, height) / 18);
        int surfaceSize = Math.Max(MinimumButtonSize, Math.Min(width, height) - margin * 2);
        return new Rectangle((width - surfaceSize) / 2, (height - surfaceSize) / 2, surfaceSize, surfaceSize);
    }

    private static int GetRegionCellSize(int regionCount, int surfaceSize)
    {
        int divisions = regionCount <= 5 ? 3 : 4;
        return Math.Clamp(surfaceSize / divisions, MinimumButtonSize, MaximumButtonSize);
    }

    private static IReadOnlyList<ContinentZoomButtonLayout> BuildFallbackRing(StellarBodyData body, Rectangle surfaceBounds, float centerX, float centerY)
    {
        int buttonSize = Math.Clamp(surfaceBounds.Width / 5, MinimumButtonSize, FallbackMaximumButtonSize);
        float targetRadius = Math.Max(buttonSize, (surfaceBounds.Width - buttonSize) / 2f);
        var layouts = new List<ContinentZoomButtonLayout>(body.Regions.Count);
        for (int displayIndex = 0; displayIndex < body.Regions.Count; displayIndex++)
        {
            var angle = -MathF.PI / 2f + displayIndex * MathF.Tau / body.Regions.Count;
            var buttonCenter = new Vector2(
                centerX + MathF.Cos(angle) * targetRadius,
                centerY + MathF.Sin(angle) * targetRadius);

            int left = (int)MathF.Round(buttonCenter.X - buttonSize / 2f);
            int top = (int)MathF.Round(buttonCenter.Y - buttonSize / 2f);
            left = Math.Clamp(left, surfaceBounds.Left, Math.Max(surfaceBounds.Left, surfaceBounds.Right - buttonSize));
            top = Math.Clamp(top, surfaceBounds.Top, Math.Max(surfaceBounds.Top, surfaceBounds.Bottom - buttonSize));

            layouts.Add(new ContinentZoomButtonLayout(body.Regions[displayIndex], new Rectangle(left, top, buttonSize, buttonSize)));
        }

        return layouts;
    }

    public static float GetBodyHitRadius(StellarBodyData body)
    {
        return body.Type switch
        {
            StellarBodyType.GasGiant => 20f,
            StellarBodyType.RockyPlanet => 15f,
            StellarBodyType.Planetoid => 8f,
            StellarBodyType.Comet => 6f,
            _ => 10f
        };
    }
}

public readonly record struct ContinentZoomButtonLayout(RegionData Region, Rectangle Bounds);
