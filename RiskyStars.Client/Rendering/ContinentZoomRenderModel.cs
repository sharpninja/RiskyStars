using Microsoft.Xna.Framework;

namespace RiskyStars.Client;

public readonly struct ContinentZoomRenderOptions
{
    public ContinentZoomRenderOptions(
        Color neutralFillColor,
        Color boundaryColor,
        Color selectedBoundaryColor,
        Color labelTextColor,
        Color labelBackgroundColor,
        int sampleSize,
        int boundaryThickness)
    {
        NeutralFillColor = neutralFillColor;
        BoundaryColor = boundaryColor;
        SelectedBoundaryColor = selectedBoundaryColor;
        LabelTextColor = labelTextColor;
        LabelBackgroundColor = labelBackgroundColor;
        SampleSize = sampleSize;
        BoundaryThickness = boundaryThickness;
    }

    public Color NeutralFillColor { get; }
    public Color BoundaryColor { get; }
    public Color SelectedBoundaryColor { get; }
    public Color LabelTextColor { get; }
    public Color LabelBackgroundColor { get; }
    public int SampleSize { get; }
    public int BoundaryThickness { get; }
}

public readonly struct ContinentZoomRegionRenderInfo
{
    public ContinentZoomRegionRenderInfo(
        RegionData region,
        Rectangle hitBounds,
        Vector2 center,
        Color fillColor,
        Color boundaryColor,
        bool isSelected,
        string? ownerId)
    {
        Region = region;
        HitBounds = hitBounds;
        Center = center;
        FillColor = fillColor;
        BoundaryColor = boundaryColor;
        IsSelected = isSelected;
        OwnerId = ownerId;
    }

    public RegionData Region { get; }
    public Rectangle HitBounds { get; }
    public Vector2 Center { get; }
    public Color FillColor { get; }
    public Color BoundaryColor { get; }
    public bool IsSelected { get; }
    public string? OwnerId { get; }
}

public readonly struct ContinentZoomTerritoryTile
{
    public ContinentZoomTerritoryTile(Rectangle bounds, string regionId, Color fillColor)
    {
        Bounds = bounds;
        RegionId = regionId;
        FillColor = fillColor;
    }

    public Rectangle Bounds { get; }
    public string RegionId { get; }
    public Color FillColor { get; }
}

public readonly struct ContinentZoomBoundarySegment
{
    public ContinentZoomBoundarySegment(Rectangle bounds, Color color, bool isSelectedBoundary)
    {
        Bounds = bounds;
        Color = color;
        IsSelectedBoundary = isSelectedBoundary;
    }

    public Rectangle Bounds { get; }
    public Color Color { get; }
    public bool IsSelectedBoundary { get; }
}

public static class ContinentZoomRenderModel
{
    public static ContinentZoomRenderOptions DefaultOptions => CreateDefaultOptions(1f);

    public static ContinentZoomRenderOptions CreateDefaultOptions(float uiScaleFactor)
    {
        return new ContinentZoomRenderOptions(
            new Color(31, 43, 38),
            new Color(171, 214, 139),
            Color.Yellow,
            Color.WhiteSmoke,
            Color.Black * 0.72f,
            GetDpiAwareSampleSize(uiScaleFactor),
            GetDpiAwareBoundaryThickness(uiScaleFactor));
    }

    public static int GetDpiAwareSampleSize(float uiScaleFactor)
    {
        float scale = Math.Clamp(uiScaleFactor, 0.8f, 1.6f);
        return Math.Max(2, (int)MathF.Round(5f / scale));
    }

    public static int GetDpiAwareBoundaryThickness(float uiScaleFactor)
    {
        float scale = Math.Clamp(uiScaleFactor, 0.8f, 1.6f);
        return Math.Max(2, (int)MathF.Round(2f * scale));
    }

    public static IReadOnlyList<ContinentZoomRegionRenderInfo> BuildRegions(
        StellarBodyData? body,
        int width,
        int height,
        Func<RegionData, string?> resolveOwnerId,
        Func<string, Color> resolvePlayerColor,
        string? selectedRegionId,
        ContinentZoomRenderOptions options)
    {
        if (body == null || width <= 0 || height <= 0)
        {
            return [];
        }

        var layouts = ContinentZoomLayout.Build(body, width, height);
        var regions = new List<ContinentZoomRegionRenderInfo>(layouts.Count);
        foreach (var layout in layouts)
        {
            string? ownerId = resolveOwnerId(layout.Region);
            bool isOwned = !string.IsNullOrWhiteSpace(ownerId);
            bool isSelected = string.Equals(layout.Region.Id, selectedRegionId, StringComparison.OrdinalIgnoreCase);
            var fillColor = isOwned && ownerId != null
                ? resolvePlayerColor(ownerId)
                : options.NeutralFillColor;

            regions.Add(new ContinentZoomRegionRenderInfo(
                layout.Region,
                layout.Bounds,
                CenterOf(layout.Bounds),
                fillColor,
                isSelected ? options.SelectedBoundaryColor : options.BoundaryColor,
                isSelected,
                ownerId));
        }

        return regions;
    }

    public static RegionData? HitTest(StellarBodyData? body, Rectangle canvasBounds, Point screenPoint)
    {
        if (body == null || canvasBounds.Width <= 0 || canvasBounds.Height <= 0 || !canvasBounds.Contains(screenPoint))
        {
            return null;
        }

        var localPoint = new Point(screenPoint.X - canvasBounds.X, screenPoint.Y - canvasBounds.Y);
        var planetBounds = ContinentZoomLayout.GetPlanetSurfaceBounds(canvasBounds.Width, canvasBounds.Height);
        if (!IsInsidePlanetSurface(localPoint, planetBounds))
        {
            return null;
        }

        var regions = BuildRegions(
            body,
            canvasBounds.Width,
            canvasBounds.Height,
            _ => null,
            _ => Color.White,
            selectedRegionId: null,
            DefaultOptions);

        return FindNearestRegion(regions, localPoint)?.Region;
    }

    public static ContinentZoomRegionRenderInfo? FindNearestRegion(
        IReadOnlyList<ContinentZoomRegionRenderInfo> regions,
        Point localPoint)
    {
        if (regions.Count == 0)
        {
            return null;
        }

        var point = new Vector2(localPoint.X, localPoint.Y);
        int closestIndex = 0;
        float closestDistance = float.MaxValue;
        for (int i = 0; i < regions.Count; i++)
        {
            float distance = Vector2.DistanceSquared(point, regions[i].Center);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        return regions[closestIndex];
    }

    public static IReadOnlyList<ContinentZoomTerritoryTile> BuildTerritoryTiles(
        int width,
        int height,
        IReadOnlyList<ContinentZoomRegionRenderInfo> regions,
        ContinentZoomRenderOptions options)
    {
        if (width <= 0 || height <= 0 || regions.Count == 0)
        {
            return [];
        }

        var tiles = new List<ContinentZoomTerritoryTile>();
        var planetBounds = ContinentZoomLayout.GetPlanetSurfaceBounds(width, height);
        int sampleSize = Math.Max(2, options.SampleSize);
        for (int y = planetBounds.Top; y < planetBounds.Bottom; y += sampleSize)
        {
            for (int x = planetBounds.Left; x < planetBounds.Right; x += sampleSize)
            {
                var localPoint = new Point(x + sampleSize / 2, y + sampleSize / 2);
                if (!IsInsidePlanetSurface(localPoint, planetBounds))
                {
                    continue;
                }

                var region = FindNearestRegion(regions, localPoint);
                if (region == null)
                {
                    continue;
                }

                tiles.Add(new ContinentZoomTerritoryTile(
                    new Rectangle(
                        x,
                        y,
                        Math.Min(sampleSize, planetBounds.Right - x),
                        Math.Min(sampleSize, planetBounds.Bottom - y)),
                    region.Value.Region.Id,
                    region.Value.FillColor));
            }
        }

        return tiles;
    }

    public static IReadOnlyList<ContinentZoomBoundarySegment> BuildBoundarySegments(
        int width,
        int height,
        IReadOnlyList<ContinentZoomRegionRenderInfo> regions,
        ContinentZoomRenderOptions options,
        bool selectedOnly)
    {
        if (width <= 0 || height <= 0 || regions.Count == 0)
        {
            return [];
        }

        var segments = new List<ContinentZoomBoundarySegment>();
        var planetBounds = ContinentZoomLayout.GetPlanetSurfaceBounds(width, height);
        int sampleSize = Math.Max(2, options.SampleSize);
        for (int y = planetBounds.Top; y < planetBounds.Bottom; y += sampleSize)
        {
            for (int x = planetBounds.Left; x < planetBounds.Right; x += sampleSize)
            {
                var localPoint = new Point(x + sampleSize / 2, y + sampleSize / 2);
                if (!IsInsidePlanetSurface(localPoint, planetBounds))
                {
                    continue;
                }

                var current = FindNearestRegion(regions, localPoint);
                if (current == null || selectedOnly && !current.Value.IsSelected)
                {
                    continue;
                }

                AddBoundarySegment(segments, planetBounds, regions, options, current.Value, localPoint, sampleSize, BoundaryDirection.Right, selectedOnly);
                AddBoundarySegment(segments, planetBounds, regions, options, current.Value, localPoint, sampleSize, BoundaryDirection.Down, selectedOnly);
                if (selectedOnly)
                {
                    AddBoundarySegment(segments, planetBounds, regions, options, current.Value, localPoint, sampleSize, BoundaryDirection.Left, selectedOnly);
                    AddBoundarySegment(segments, planetBounds, regions, options, current.Value, localPoint, sampleSize, BoundaryDirection.Up, selectedOnly);
                }
            }
        }

        return segments;
    }

    public static bool IsInsidePlanetSurface(Point localPoint, Rectangle planetBounds)
    {
        if (planetBounds.Width <= 0 || planetBounds.Height <= 0 || !planetBounds.Contains(localPoint))
        {
            return false;
        }

        float radiusX = planetBounds.Width / 2f;
        float radiusY = planetBounds.Height / 2f;
        float normalizedX = (localPoint.X - planetBounds.Center.X) / radiusX;
        float normalizedY = (localPoint.Y - planetBounds.Center.Y) / radiusY;
        return normalizedX * normalizedX + normalizedY * normalizedY <= 1f;
    }

    private static Vector2 CenterOf(Rectangle bounds)
    {
        return new Vector2(bounds.Left + bounds.Width / 2f, bounds.Top + bounds.Height / 2f);
    }

    private static void AddBoundarySegment(
        List<ContinentZoomBoundarySegment> segments,
        Rectangle planetBounds,
        IReadOnlyList<ContinentZoomRegionRenderInfo> regions,
        ContinentZoomRenderOptions options,
        ContinentZoomRegionRenderInfo current,
        Point localPoint,
        int sampleSize,
        BoundaryDirection direction,
        bool selectedOnly)
    {
        var neighborPoint = direction switch
        {
            BoundaryDirection.Right => new Point(localPoint.X + sampleSize, localPoint.Y),
            BoundaryDirection.Down => new Point(localPoint.X, localPoint.Y + sampleSize),
            BoundaryDirection.Left => new Point(localPoint.X - sampleSize, localPoint.Y),
            BoundaryDirection.Up => new Point(localPoint.X, localPoint.Y - sampleSize),
            _ => localPoint
        };

        var neighbor = IsInsidePlanetSurface(neighborPoint, planetBounds)
            ? FindNearestRegion(regions, neighborPoint)
            : null;

        bool boundary = neighbor == null ||
            !string.Equals(neighbor.Value.Region.Id, current.Region.Id, StringComparison.OrdinalIgnoreCase);
        if (!boundary)
        {
            return;
        }

        int thickness = selectedOnly ? options.BoundaryThickness + 2 : options.BoundaryThickness;
        var lineBounds = direction switch
        {
            BoundaryDirection.Right => new Rectangle(localPoint.X + sampleSize / 2, localPoint.Y - sampleSize / 2, thickness, sampleSize),
            BoundaryDirection.Down => new Rectangle(localPoint.X - sampleSize / 2, localPoint.Y + sampleSize / 2, sampleSize, thickness),
            BoundaryDirection.Left => new Rectangle(localPoint.X - sampleSize / 2, localPoint.Y - sampleSize / 2, thickness, sampleSize),
            BoundaryDirection.Up => new Rectangle(localPoint.X - sampleSize / 2, localPoint.Y - sampleSize / 2, sampleSize, thickness),
            _ => Rectangle.Empty
        };

        segments.Add(new ContinentZoomBoundarySegment(
            lineBounds,
            selectedOnly ? current.BoundaryColor : options.BoundaryColor,
            selectedOnly));
    }

    private enum BoundaryDirection
    {
        Right,
        Down,
        Left,
        Up
    }
}
