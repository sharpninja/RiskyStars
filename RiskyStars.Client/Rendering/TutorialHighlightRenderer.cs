using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RiskyStars.Client;

internal static class TutorialHighlightRenderer
{
    [ExcludeFromCodeCoverage]
    public static void Draw(
        SpriteBatch spriteBatch,
        Texture2D pixelTexture,
        IReadOnlyList<TutorialHighlightBounds> highlights,
        int screenWidth,
        int screenHeight)
    {
        if (highlights.Count == 0 || screenWidth <= 0 || screenHeight <= 0)
        {
            return;
        }

        spriteBatch.Begin(sortMode: SpriteSortMode.Deferred, blendState: BlendState.AlphaBlend, samplerState: SamplerState.PointClamp);
        foreach (var highlight in highlights)
        {
            int padding = TutorialHighlightBoundsResolver.GetHighlightPadding(
                highlight.Target,
                ThemeManager.ScalePixels(8),
                ThemeManager.ScalePixels(14));
            Rectangle bounds = TutorialHighlightBoundsResolver.ExpandAndClamp(
                highlight.Bounds,
                padding,
                screenWidth,
                screenHeight);

            if (bounds.IsEmpty)
            {
                continue;
            }

            DrawHighlight(spriteBatch, pixelTexture, highlight.Target, bounds);
        }

        spriteBatch.End();
    }

    [ExcludeFromCodeCoverage]
    private static void DrawHighlight(
        SpriteBatch spriteBatch,
        Texture2D pixelTexture,
        TutorialHighlightTarget target,
        Rectangle bounds)
    {
        int thickness = Math.Max(2, ThemeManager.ScalePixels(2));
        int cornerLength = Math.Max(18, Math.Min(bounds.Width, bounds.Height) / 5);
        var glow = ThemeManager.Colors.TextAccent * 0.18f;
        var edge = ThemeManager.Colors.TextAccent;
        var hotEdge = ThemeManager.Colors.TextWarning;

        if (target == TutorialHighlightTarget.MapViewport)
        {
            DrawMapTargetHighlight(spriteBatch, pixelTexture, bounds, hotEdge);
            return;
        }

        if (TutorialHighlightBoundsResolver.ShouldFillHighlight(target))
        {
            spriteBatch.Draw(pixelTexture, bounds, glow);
        }

        DrawBorder(spriteBatch, pixelTexture, bounds, edge, thickness);

        DrawCorner(spriteBatch, pixelTexture, bounds.Left, bounds.Top, cornerLength, thickness, hotEdge, horizontalRight: true, verticalDown: true);
        DrawCorner(spriteBatch, pixelTexture, bounds.Right, bounds.Top, cornerLength, thickness, hotEdge, horizontalRight: false, verticalDown: true);
        DrawCorner(spriteBatch, pixelTexture, bounds.Left, bounds.Bottom, cornerLength, thickness, hotEdge, horizontalRight: true, verticalDown: false);
        DrawCorner(spriteBatch, pixelTexture, bounds.Right, bounds.Bottom, cornerLength, thickness, hotEdge, horizontalRight: false, verticalDown: false);
    }

    [ExcludeFromCodeCoverage]
    private static void DrawMapTargetHighlight(SpriteBatch spriteBatch, Texture2D pixelTexture, Rectangle bounds, Color color)
    {
        int primaryThickness = Math.Max(3, ThemeManager.ScalePixels(3));
        int secondaryThickness = Math.Max(2, ThemeManager.ScalePixels(2));
        var halo = color * 0.12f;
        var edge = color;

        spriteBatch.Draw(pixelTexture, bounds, halo);
        DrawBorder(spriteBatch, pixelTexture, bounds, edge, primaryThickness);

        int inset = Math.Max(primaryThickness * 3, ThemeManager.ScalePixels(8));
        if (bounds.Width > inset * 2 && bounds.Height > inset * 2)
        {
            DrawBorder(
                spriteBatch,
                pixelTexture,
                new Rectangle(bounds.Left + inset, bounds.Top + inset, bounds.Width - inset * 2, bounds.Height - inset * 2),
                edge * 0.75f,
                secondaryThickness);
        }

        int centerX = bounds.Left + bounds.Width / 2;
        int centerY = bounds.Top + bounds.Height / 2;
        int tickLength = Math.Max(12, Math.Min(bounds.Width, bounds.Height) / 5);

        spriteBatch.Draw(pixelTexture, new Rectangle(centerX - tickLength / 2, bounds.Top, tickLength, primaryThickness), edge);
        spriteBatch.Draw(pixelTexture, new Rectangle(centerX - tickLength / 2, bounds.Bottom - primaryThickness, tickLength, primaryThickness), edge);
        spriteBatch.Draw(pixelTexture, new Rectangle(bounds.Left, centerY - tickLength / 2, primaryThickness, tickLength), edge);
        spriteBatch.Draw(pixelTexture, new Rectangle(bounds.Right - primaryThickness, centerY - tickLength / 2, primaryThickness, tickLength), edge);
    }

    [ExcludeFromCodeCoverage]
    private static void DrawBorder(SpriteBatch spriteBatch, Texture2D pixelTexture, Rectangle bounds, Color color, int thickness)
    {
        spriteBatch.Draw(pixelTexture, new Rectangle(bounds.Left, bounds.Top, bounds.Width, thickness), color);
        spriteBatch.Draw(pixelTexture, new Rectangle(bounds.Left, bounds.Bottom - thickness, bounds.Width, thickness), color);
        spriteBatch.Draw(pixelTexture, new Rectangle(bounds.Left, bounds.Top, thickness, bounds.Height), color);
        spriteBatch.Draw(pixelTexture, new Rectangle(bounds.Right - thickness, bounds.Top, thickness, bounds.Height), color);
    }

    [ExcludeFromCodeCoverage]
    private static void DrawCorner(
        SpriteBatch spriteBatch,
        Texture2D pixelTexture,
        int x,
        int y,
        int length,
        int thickness,
        Color color,
        bool horizontalRight,
        bool verticalDown)
    {
        int horizontalX = horizontalRight ? x : x - length;
        int horizontalY = verticalDown ? y : y - thickness;
        int verticalX = horizontalRight ? x : x - thickness;
        int verticalY = verticalDown ? y : y - length;

        spriteBatch.Draw(pixelTexture, new Rectangle(horizontalX, horizontalY, length, thickness), color);
        spriteBatch.Draw(pixelTexture, new Rectangle(verticalX, verticalY, thickness, length), color);
    }
}
