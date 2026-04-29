using Microsoft.Xna.Framework;

namespace RiskyStars.Client;

public enum TutorialHighlightTarget
{
    TopBar,
    ResourceChips,
    MapViewport,
    HelpPanel,
    PlayerDashboard,
    SelectionPanel,
    EncyclopediaWindow
}

internal readonly record struct TutorialHighlightBounds(
    TutorialHighlightTarget Target,
    Rectangle Bounds,
    string Label);

internal static class TutorialHighlightTargets
{
    public static IReadOnlyList<TutorialHighlightTarget> ForCompletion(TutorialStepCompletion completion)
    {
        return completion switch
        {
            TutorialStepCompletion.WorldSynced => [TutorialHighlightTarget.TopBar],
            TutorialStepCompletion.OwnTurn => [TutorialHighlightTarget.TopBar, TutorialHighlightTarget.ResourceChips],
            TutorialStepCompletion.AnySelection => [TutorialHighlightTarget.MapViewport, TutorialHighlightTarget.SelectionPanel],
            TutorialStepCompletion.HelpOpen => [TutorialHighlightTarget.TopBar, TutorialHighlightTarget.HelpPanel],
            TutorialStepCompletion.PurchasePhase => [TutorialHighlightTarget.TopBar, TutorialHighlightTarget.ResourceChips],
            TutorialStepCompletion.DashboardOpen => [TutorialHighlightTarget.TopBar, TutorialHighlightTarget.PlayerDashboard],
            TutorialStepCompletion.ArmyPurchased => [TutorialHighlightTarget.PlayerDashboard],
            TutorialStepCompletion.ReinforcementPhase => [TutorialHighlightTarget.TopBar],
            TutorialStepCompletion.OwnedReinforcementTargetSelected => [TutorialHighlightTarget.MapViewport, TutorialHighlightTarget.SelectionPanel],
            TutorialStepCompletion.MovementPhase => [TutorialHighlightTarget.TopBar],
            TutorialStepCompletion.OwnArmySelected => [TutorialHighlightTarget.MapViewport, TutorialHighlightTarget.SelectionPanel],
            TutorialStepCompletion.ArmyMoved => [TutorialHighlightTarget.MapViewport],
            TutorialStepCompletion.ReferenceOpen => [TutorialHighlightTarget.TopBar, TutorialHighlightTarget.EncyclopediaWindow],
            _ => []
        };
    }
}

internal static class TutorialHighlightBoundsResolver
{
    public static IReadOnlyList<TutorialHighlightBounds> Resolve(
        IReadOnlyList<TutorialHighlightTarget> requestedTargets,
        IReadOnlyDictionary<TutorialHighlightTarget, Rectangle> visibleBounds)
    {
        if (requestedTargets.Count == 0 || visibleBounds.Count == 0)
        {
            return [];
        }

        var resolved = new List<TutorialHighlightBounds>(requestedTargets.Count);
        var seen = new HashSet<TutorialHighlightTarget>();

        foreach (var target in requestedTargets)
        {
            if (!seen.Add(target) ||
                !visibleBounds.TryGetValue(target, out Rectangle bounds) ||
                bounds.Width <= 0 ||
                bounds.Height <= 0)
            {
                continue;
            }

            resolved.Add(new TutorialHighlightBounds(target, bounds, GetLabel(target)));
        }

        return resolved;
    }

    public static Rectangle ExpandAndClamp(Rectangle bounds, int padding, int screenWidth, int screenHeight)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0 || screenWidth <= 0 || screenHeight <= 0)
        {
            return Rectangle.Empty;
        }

        int left = Math.Clamp(bounds.Left - padding, 0, screenWidth - 1);
        int top = Math.Clamp(bounds.Top - padding, 0, screenHeight - 1);
        int right = Math.Clamp(bounds.Right + padding, left + 1, screenWidth);
        int bottom = Math.Clamp(bounds.Bottom + padding, top + 1, screenHeight);

        return new Rectangle(left, top, right - left, bottom - top);
    }

    public static Rectangle PreferExplicitBounds(Rectangle actualBounds, int left, int top, int? width, int? height)
    {
        if (width.GetValueOrDefault() > 0 && height.GetValueOrDefault() > 0)
        {
            return new Rectangle(left, top, width!.Value, height!.Value);
        }

        return actualBounds.Width > 0 && actualBounds.Height > 0
            ? actualBounds
            : Rectangle.Empty;
    }

    public static bool ShouldFillHighlight(TutorialHighlightTarget target)
    {
        return target != TutorialHighlightTarget.MapViewport;
    }

    private static string GetLabel(TutorialHighlightTarget target)
    {
        return target switch
        {
            TutorialHighlightTarget.TopBar => "Turn and phase",
            TutorialHighlightTarget.ResourceChips => "Resources",
            TutorialHighlightTarget.MapViewport => "Map target",
            TutorialHighlightTarget.HelpPanel => "Shortcut sheet",
            TutorialHighlightTarget.PlayerDashboard => "Command dashboard",
            TutorialHighlightTarget.SelectionPanel => "Selection details",
            TutorialHighlightTarget.EncyclopediaWindow => "Reference layer",
            _ => "Tutorial target"
        };
    }
}

internal static class TutorialMapHighlightResolver
{
    public static Rectangle SelectBestTarget(
        IReadOnlyList<Rectangle> candidateBounds,
        Rectangle viewportBounds,
        Rectangle occluderBounds)
    {
        if (candidateBounds.Count == 0 || viewportBounds.Width <= 0 || viewportBounds.Height <= 0)
        {
            return Rectangle.Empty;
        }

        Vector2 viewportCenter = new(
            viewportBounds.Left + viewportBounds.Width / 2f,
            viewportBounds.Top + viewportBounds.Height / 2f);

        return candidateBounds
            .Select(candidate => Rectangle.Intersect(candidate, viewportBounds))
            .Where(candidate => candidate.Width > 0 && candidate.Height > 0)
            .Where(candidate => occluderBounds.Width <= 0 || occluderBounds.Height <= 0 || !candidate.Intersects(occluderBounds))
            .OrderBy(candidate => GetDistanceSquared(candidate, viewportCenter))
            .ThenByDescending(candidate => candidate.Width * candidate.Height)
            .FirstOrDefault(Rectangle.Empty);
    }

    public static Rectangle ToScreenBounds(
        Vector2 worldPosition,
        float worldRadius,
        Matrix worldToScreen,
        int minimumScreenRadius,
        int maximumScreenRadius)
    {
        Vector2 screenCenter = Vector2.Transform(worldPosition, worldToScreen);
        Vector2 screenEdge = Vector2.Transform(worldPosition + new Vector2(MathF.Max(1f, worldRadius), 0f), worldToScreen);
        int radius = (int)MathF.Ceiling(Vector2.Distance(screenCenter, screenEdge));
        radius = Math.Clamp(radius, Math.Max(1, minimumScreenRadius), Math.Max(Math.Max(1, minimumScreenRadius), maximumScreenRadius));

        return new Rectangle(
            (int)MathF.Round(screenCenter.X) - radius,
            (int)MathF.Round(screenCenter.Y) - radius,
            radius * 2,
            radius * 2);
    }

    private static float GetDistanceSquared(Rectangle bounds, Vector2 viewportCenter)
    {
        float centerX = bounds.Left + bounds.Width / 2f;
        float centerY = bounds.Top + bounds.Height / 2f;
        float x = centerX - viewportCenter.X;
        float y = centerY - viewportCenter.Y;
        return x * x + y * y;
    }
}
