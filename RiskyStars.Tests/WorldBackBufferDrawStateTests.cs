using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RiskyStars.Client;

namespace RiskyStars.Tests;

public class WorldBackBufferDrawStateTests
{
    [Fact]
    public void Create_ReturnsFullBackBufferViewportAndScissor()
    {
        var state = WorldBackBufferDrawState.Create(2048, 1152);

        Assert.True(state.Matches(
            new Viewport(0, 0, 2048, 1152),
            new Rectangle(0, 0, 2048, 1152)));
    }

    [Fact]
    public void Matches_ReturnsFalseForStaleZoomViewport()
    {
        var state = WorldBackBufferDrawState.Create(2048, 1152);

        bool matchesStaleZoomViewport = state.Matches(
            new Viewport(0, 0, 760, 430),
            new Rectangle(0, 0, 2048, 1152));

        Assert.False(matchesStaleZoomViewport);
    }

    [Fact]
    public void Matches_ReturnsFalseForStaleMyraScissor()
    {
        var state = WorldBackBufferDrawState.Create(2048, 1152);

        bool matchesStaleScissor = state.Matches(
            new Viewport(0, 0, 2048, 1152),
            new Rectangle(900, 250, 20, 20));

        Assert.False(matchesStaleScissor);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-100, 1)]
    public void Create_RepairsInvalidBackBufferSize(int invalidSize, int expectedSize)
    {
        var state = WorldBackBufferDrawState.Create(invalidSize, invalidSize);

        Assert.True(state.Matches(
            new Viewport(0, 0, expectedSize, expectedSize),
            new Rectangle(0, 0, expectedSize, expectedSize)));
    }
}
