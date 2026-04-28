using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RiskyStars.Client;

internal readonly record struct ContinentZoomGraphicsState(Viewport Viewport, Rectangle ScissorRectangle)
{
    public static ContinentZoomGraphicsState Capture(Viewport viewport, Rectangle scissorRectangle)
    {
        return new ContinentZoomGraphicsState(viewport, scissorRectangle);
    }

    public bool Matches(Viewport viewport, Rectangle scissorRectangle)
    {
        return Viewport.X == viewport.X &&
            Viewport.Y == viewport.Y &&
            Viewport.Width == viewport.Width &&
            Viewport.Height == viewport.Height &&
            Viewport.MinDepth.Equals(viewport.MinDepth) &&
            Viewport.MaxDepth.Equals(viewport.MaxDepth) &&
            ScissorRectangle == scissorRectangle;
    }

    public static RenderTargetRestoreMode GetRenderTargetRestoreMode(int previousRenderTargetCount)
    {
        return previousRenderTargetCount > 0
            ? RenderTargetRestoreMode.PreviousTargets
            : RenderTargetRestoreMode.BackBuffer;
    }
}

internal enum RenderTargetRestoreMode
{
    BackBuffer,
    PreviousTargets
}
