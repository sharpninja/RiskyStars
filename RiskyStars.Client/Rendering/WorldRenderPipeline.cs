namespace RiskyStars.Client;

internal static class WorldRenderPipeline
{
    public static readonly IReadOnlyList<WorldRenderPass> OrderedPasses =
    [
        WorldRenderPass.OffscreenZoomSurface,
        WorldRenderPass.BackBufferWorld,
        WorldRenderPass.UiOverlay
    ];

    public static bool HasSafeRenderTargetOrder(IReadOnlyList<WorldRenderPass> passes)
    {
        int offscreenIndex = IndexOf(passes, WorldRenderPass.OffscreenZoomSurface);
        int worldIndex = IndexOf(passes, WorldRenderPass.BackBufferWorld);

        return offscreenIndex >= 0 &&
            worldIndex >= 0 &&
            offscreenIndex < worldIndex;
    }

    private static int IndexOf(IReadOnlyList<WorldRenderPass> passes, WorldRenderPass pass)
    {
        for (int i = 0; i < passes.Count; i++)
        {
            if (passes[i] == pass)
            {
                return i;
            }
        }

        return -1;
    }
}

internal enum WorldRenderPass
{
    OffscreenZoomSurface,
    BackBufferWorld,
    UiOverlay
}
