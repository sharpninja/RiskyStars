using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI;

namespace RiskyStars.Client;

internal static class TutorialModeWindowAnchor
{
    private const int MapGap = 12;
    private const int ScreenEdgeMargin = 10;
    private const int TitleBarHeight = 30;

    public static Point Calculate(
        int screenWidth,
        int screenHeight,
        int leftDockRight,
        int rightDockLeft,
        int mapTop,
        int windowWidth,
        int windowHeight)
    {
        if (screenWidth <= 0 || screenHeight <= 0)
        {
            return Point.Zero;
        }

        int gap = ThemeManager.ScalePixels(MapGap);
        int edgeMargin = ThemeManager.ScalePixels(ScreenEdgeMargin);
        int safeWindowWidth = Math.Max(1, windowWidth);
        int safeWindowHeight = Math.Max(1, windowHeight);
        int mapLeft = Math.Clamp(leftDockRight, 0, screenWidth);
        int mapRight = rightDockLeft > mapLeft
            ? Math.Clamp(rightDockLeft, mapLeft, screenWidth)
            : screenWidth;

        int preferredLeft = mapLeft + gap;
        int maxScreenLeft = Math.Max(edgeMargin, screenWidth - safeWindowWidth - edgeMargin);
        int maxMapLeft = Math.Max(preferredLeft, mapRight - safeWindowWidth - gap);
        int maxLeft = Math.Max(edgeMargin, Math.Min(maxScreenLeft, maxMapLeft));
        int left = Math.Clamp(preferredLeft, edgeMargin, maxLeft);

        int minTop = Math.Max(ThemeManager.ScalePixels(TitleBarHeight) + edgeMargin, mapTop);
        int maxTop = Math.Max(minTop, screenHeight - safeWindowHeight - edgeMargin);
        int top = Math.Clamp(minTop, ThemeManager.ScalePixels(TitleBarHeight) + edgeMargin, maxTop);

        return new Point(left, top);
    }

    public static Point Apply(
        Window window,
        int screenWidth,
        int screenHeight,
        int leftDockRight,
        int rightDockLeft,
        int mapTop,
        int defaultWindowWidth,
        int defaultWindowHeight)
    {
        var anchor = Calculate(
            screenWidth,
            screenHeight,
            leftDockRight,
            rightDockLeft,
            mapTop,
            window.Width ?? defaultWindowWidth,
            window.Height ?? defaultWindowHeight);

        window.Left = anchor.X;
        window.Top = anchor.Y;
        return anchor;
    }
}
