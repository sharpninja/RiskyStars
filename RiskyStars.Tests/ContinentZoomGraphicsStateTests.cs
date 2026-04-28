using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RiskyStars.Client;

namespace RiskyStars.Tests;

public class ContinentZoomGraphicsStateTests
{
    [Fact]
    public void Matches_ReturnsTrueForRestoredBackBufferViewportAndScissor()
    {
        var viewport = new Viewport(0, 0, 2048, 1152);
        var scissor = new Rectangle(0, 0, 2048, 1152);
        var snapshot = ContinentZoomGraphicsState.Capture(viewport, scissor);

        Assert.True(snapshot.Matches(viewport, scissor));
    }

    [Fact]
    public void Matches_ReturnsFalseForRenderTargetViewportLeak()
    {
        var snapshot = ContinentZoomGraphicsState.Capture(
            new Viewport(0, 0, 2048, 1152),
            new Rectangle(0, 0, 2048, 1152));

        bool matchesLeakedZoomViewport = snapshot.Matches(
            new Viewport(0, 0, 760, 430),
            new Rectangle(0, 0, 2048, 1152));

        Assert.False(matchesLeakedZoomViewport);
    }

    [Fact]
    public void Matches_ReturnsFalseForRenderTargetScissorLeak()
    {
        var snapshot = ContinentZoomGraphicsState.Capture(
            new Viewport(0, 0, 2048, 1152),
            new Rectangle(0, 0, 2048, 1152));

        bool matchesLeakedZoomScissor = snapshot.Matches(
            new Viewport(0, 0, 2048, 1152),
            new Rectangle(0, 0, 760, 430));

        Assert.False(matchesLeakedZoomScissor);
    }

    [Fact]
    public void GetRenderTargetRestoreMode_UsesBackBufferForEmptyPreviousTargets()
    {
        var mode = ContinentZoomGraphicsState.GetRenderTargetRestoreMode(0);

        Assert.Equal(RenderTargetRestoreMode.BackBuffer, mode);
    }

    [Fact]
    public void GetRenderTargetRestoreMode_DoesNotTreatEmptyTargetsAsRestorableTargetStack()
    {
        var mode = ContinentZoomGraphicsState.GetRenderTargetRestoreMode(0);

        Assert.NotEqual(RenderTargetRestoreMode.PreviousTargets, mode);
    }

    [Fact]
    public void GetRenderTargetRestoreMode_RestoresPreviousTargetsWhenNestedRenderTargetWasActive()
    {
        var mode = ContinentZoomGraphicsState.GetRenderTargetRestoreMode(1);

        Assert.Equal(RenderTargetRestoreMode.PreviousTargets, mode);
    }
}
