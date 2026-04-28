using Microsoft.Xna.Framework;

namespace RiskyStars.Client;

public static class ContinentZoomLayout
{
    public const int MinimumButtonSize = 48;
    public const int MaximumButtonSize = 84;

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

        int buttonSize = Math.Clamp(Math.Min(width, height) / 6, MinimumButtonSize, MaximumButtonSize);
        int margin = Math.Max(16, buttonSize / 3);
        float centerX = width / 2f;
        float centerY = height / 2f;
        float radius = Math.Max(
            buttonSize,
            Math.Min(width - buttonSize - margin * 2, height - buttonSize - margin * 2) / 2f);

        var sortedRegions = body.Regions
            .Select((region, index) => new
            {
                Region = region,
                Index = index,
                Angle = MathF.Atan2(region.Position.Y - body.Position.Y, region.Position.X - body.Position.X)
            })
            .OrderBy(item => item.Angle)
            .ThenBy(item => item.Index)
            .ToArray();

        var layouts = new List<ContinentZoomButtonLayout>(sortedRegions.Length);
        for (int displayIndex = 0; displayIndex < sortedRegions.Length; displayIndex++)
        {
            var angle = -MathF.PI / 2f + displayIndex * MathF.Tau / sortedRegions.Length;
            var buttonCenter = new Vector2(
                centerX + MathF.Cos(angle) * radius,
                centerY + MathF.Sin(angle) * radius);

            int left = (int)MathF.Round(buttonCenter.X - buttonSize / 2f);
            int top = (int)MathF.Round(buttonCenter.Y - buttonSize / 2f);
            left = Math.Clamp(left, margin, Math.Max(margin, width - buttonSize - margin));
            top = Math.Clamp(top, margin, Math.Max(margin, height - buttonSize - margin));

            layouts.Add(new ContinentZoomButtonLayout(sortedRegions[displayIndex].Region, new Rectangle(left, top, buttonSize, buttonSize)));
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
