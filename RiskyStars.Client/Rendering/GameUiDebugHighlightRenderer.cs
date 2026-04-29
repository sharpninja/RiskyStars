using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RiskyStars.Client;

internal static class GameUiDebugHighlightRenderer
{
    [ExcludeFromCodeCoverage]
    public static void Draw(
        SpriteBatch spriteBatch,
        Texture2D pixelTexture,
        Rectangle bounds,
        int screenWidth,
        int screenHeight)
    {
        Rectangle clamped = TutorialHighlightBoundsResolver.ExpandAndClamp(
            bounds,
            ThemeManager.ScalePixels(6),
            screenWidth,
            screenHeight);
        if (clamped.Width <= 0 || clamped.Height <= 0)
        {
            return;
        }

        int primaryThickness = Math.Max(2, ThemeManager.ScalePixels(2));
        int secondaryThickness = Math.Max(1, ThemeManager.ScalePixels(1));
        var fill = ThemeManager.Colors.TextWarning * 0.12f;
        var primary = ThemeManager.Colors.TextWarning;
        var secondary = Color.White * 0.86f;

        spriteBatch.Begin(sortMode: SpriteSortMode.Deferred, blendState: BlendState.AlphaBlend, samplerState: SamplerState.PointClamp);
        spriteBatch.Draw(pixelTexture, clamped, fill);
        DrawBorder(spriteBatch, pixelTexture, clamped, secondary, primaryThickness + secondaryThickness);
        DrawBorder(spriteBatch, pixelTexture, clamped, primary, primaryThickness);

        int inset = Math.Max(primaryThickness * 4, ThemeManager.ScalePixels(8));
        if (clamped.Width > inset * 2 && clamped.Height > inset * 2)
        {
            DrawBorder(
                spriteBatch,
                pixelTexture,
                new Rectangle(clamped.Left + inset, clamped.Top + inset, clamped.Width - inset * 2, clamped.Height - inset * 2),
                primary * 0.65f,
                secondaryThickness);
        }

        spriteBatch.End();
    }

    [ExcludeFromCodeCoverage]
    private static void DrawBorder(SpriteBatch spriteBatch, Texture2D pixelTexture, Rectangle bounds, Color color, int thickness)
    {
        spriteBatch.Draw(pixelTexture, new Rectangle(bounds.Left, bounds.Top, bounds.Width, thickness), color);
        spriteBatch.Draw(pixelTexture, new Rectangle(bounds.Left, bounds.Bottom - thickness, bounds.Width, thickness), color);
        spriteBatch.Draw(pixelTexture, new Rectangle(bounds.Left, bounds.Top, thickness, bounds.Height), color);
        spriteBatch.Draw(pixelTexture, new Rectangle(bounds.Right - thickness, bounds.Top, thickness, bounds.Height), color);
    }
}
