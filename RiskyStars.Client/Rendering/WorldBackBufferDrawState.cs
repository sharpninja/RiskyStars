using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RiskyStars.Client;

internal readonly record struct WorldBackBufferDrawState(Viewport Viewport, Rectangle ScissorRectangle)
{
    public static WorldBackBufferDrawState Create(int width, int height)
    {
        width = Math.Max(1, width);
        height = Math.Max(1, height);
        return new WorldBackBufferDrawState(
            new Viewport(0, 0, width, height),
            new Rectangle(0, 0, width, height));
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
}
