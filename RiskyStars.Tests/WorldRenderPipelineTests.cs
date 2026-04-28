using RiskyStars.Client;

namespace RiskyStars.Tests;

public class WorldRenderPipelineTests
{
    [Fact]
    public void OrderedPasses_RendersOffscreenZoomBeforeBackBufferWorld()
    {
        var passes = WorldRenderPipeline.OrderedPasses;

        Assert.True(WorldRenderPipeline.HasSafeRenderTargetOrder(passes));
        Assert.Collection(
            passes,
            pass => Assert.Equal(WorldRenderPass.OffscreenZoomSurface, pass),
            pass => Assert.Equal(WorldRenderPass.BackBufferWorld, pass),
            pass => Assert.Equal(WorldRenderPass.UiOverlay, pass));
    }

    [Fact]
    public void HasSafeRenderTargetOrder_ReturnsFalseForOldBackBufferThenZoomOrder()
    {
        var oldBadOrder = new[]
        {
            WorldRenderPass.BackBufferWorld,
            WorldRenderPass.OffscreenZoomSurface,
            WorldRenderPass.UiOverlay
        };

        Assert.False(WorldRenderPipeline.HasSafeRenderTargetOrder(oldBadOrder));
    }

    [Fact]
    public void HasSafeRenderTargetOrder_ReturnsFalseWhenOffscreenZoomPassIsMissing()
    {
        var missingOffscreenZoom = new[]
        {
            WorldRenderPass.BackBufferWorld,
            WorldRenderPass.UiOverlay
        };

        Assert.False(WorldRenderPipeline.HasSafeRenderTargetOrder(missingOffscreenZoom));
    }

    [Fact]
    public void HasSafeRenderTargetOrder_ReturnsFalseWhenBackBufferWorldPassIsMissing()
    {
        var missingBackBufferWorld = new[]
        {
            WorldRenderPass.OffscreenZoomSurface,
            WorldRenderPass.UiOverlay
        };

        Assert.False(WorldRenderPipeline.HasSafeRenderTargetOrder(missingBackBufferWorld));
    }
}
