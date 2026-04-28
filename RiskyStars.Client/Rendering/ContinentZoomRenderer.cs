using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D.TextureAtlases;

namespace RiskyStars.Client;

[ExcludeFromCodeCoverage(Justification = "Thin MonoGame GPU draw adapter; continent layout, hit testing, fills, and boundaries are unit-tested in ContinentZoomRenderModel.")]
public sealed class ContinentZoomRenderer : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private Texture2D? _pixelTexture;
    private RenderTarget2D? _renderTarget;
    private TextureRegion? _renderTargetRegion;
    private SpriteFont? _font;

    public ContinentZoomRenderer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
        CreatePixelTexture();
    }

    public void LoadContent(SpriteFont font)
    {
        _font = font;
    }

    public void UpdateSurface(
        SpriteBatch spriteBatch,
        ContinentZoomWindow? window,
        GameStateCache? gameStateCache,
        SelectionState? selection,
        RegionRenderer? regionRenderer)
    {
        if (_pixelTexture == null || window?.IsVisible != true || window.CurrentBody == null)
        {
            window?.SetRenderedSurface(null);
            return;
        }

        var canvasBounds = window.CanvasBounds;
        if (canvasBounds.Width <= 0 || canvasBounds.Height <= 0)
        {
            window.SetRenderedSurface(null);
            return;
        }

        var options = CreateOptions();
        regionRenderer?.SyncPlayerColors(gameStateCache);
        var regions = ContinentZoomRenderModel.BuildRegions(
            window.CurrentBody,
            canvasBounds.Width,
            canvasBounds.Height,
            region => gameStateCache?.GetRegionOwnership(region.Id)?.OwnerId,
            ownerId => regionRenderer?.GetPlayerColor(ownerId) ?? Color.White,
            selection?.SelectedRegion?.Id,
            options);

        if (regions.Count == 0)
        {
            window.SetRenderedSurface(null);
            return;
        }

        EnsureRenderTarget(canvasBounds.Width, canvasBounds.Height);
        if (_renderTarget == null)
        {
            window.SetRenderedSurface(null);
            return;
        }

        bool spriteBatchBegun = false;
        var previousTargets = _graphicsDevice.GetRenderTargets();
        var previousGraphicsState = ContinentZoomGraphicsState.Capture(_graphicsDevice.Viewport, _graphicsDevice.ScissorRectangle);
        try
        {
            _graphicsDevice.SetRenderTarget(_renderTarget);
            _graphicsDevice.Clear(Color.Transparent);

            spriteBatch.Begin(
                sortMode: SpriteSortMode.Deferred,
                blendState: BlendState.AlphaBlend,
                samplerState: SamplerState.LinearClamp);
            spriteBatchBegun = true;

            var localBounds = new Rectangle(0, 0, canvasBounds.Width, canvasBounds.Height);
            DrawCanvasFrame(spriteBatch, localBounds, options);
            DrawTerritories(spriteBatch, localBounds, regions, options);
            DrawDividerLines(spriteBatch, localBounds, regions, options);
            DrawSelectedRegionBoundary(spriteBatch, localBounds, regions, options);
            DrawPlanetBoundary(spriteBatch, localBounds, options);
            DrawRegionLabels(spriteBatch, localBounds, regions, options);

            spriteBatch.End();
            spriteBatchBegun = false;
        }
        finally
        {
            if (spriteBatchBegun)
            {
                spriteBatch.End();
            }

            if (ContinentZoomGraphicsState.GetRenderTargetRestoreMode(previousTargets.Length) == RenderTargetRestoreMode.BackBuffer)
            {
                _graphicsDevice.SetRenderTarget(null);
            }
            else
            {
                _graphicsDevice.SetRenderTargets(previousTargets);
            }

            _graphicsDevice.Viewport = previousGraphicsState.Viewport;
            _graphicsDevice.ScissorRectangle = previousGraphicsState.ScissorRectangle;
            _graphicsDevice.Textures[0] = null;
        }

        window.SetRenderedSurface(_renderTargetRegion);
    }

    private static ContinentZoomRenderOptions CreateOptions()
    {
        float uiScaleFactor = ThemeManager.CurrentUiScaleFactor;
        return new ContinentZoomRenderOptions(
            new Color(31, 43, 38),
            ThemeManager.Colors.BorderFocus * 0.72f,
            ThemeManager.Colors.TextWarning,
            ThemeManager.Colors.TextPrimary,
            Color.Black * 0.76f,
            ContinentZoomRenderModel.GetDpiAwareSampleSize(uiScaleFactor),
            Math.Max(
                ContinentZoomRenderModel.GetDpiAwareBoundaryThickness(uiScaleFactor),
                ThemeManager.ScalePixels(ThemeManager.BorderThickness.Normal, minimum: 2)));
    }

    private void CreatePixelTexture()
    {
        _pixelTexture = new Texture2D(_graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
    }

    private void EnsureRenderTarget(int width, int height)
    {
        if (_renderTarget != null &&
            _renderTarget.Width == width &&
            _renderTarget.Height == height &&
            !_renderTarget.IsDisposed)
        {
            return;
        }

        _renderTarget?.Dispose();
        _renderTarget = new RenderTarget2D(
            _graphicsDevice,
            width,
            height,
            mipMap: false,
            SurfaceFormat.Color,
            DepthFormat.None,
            preferredMultiSampleCount: 0,
            RenderTargetUsage.PreserveContents);
        _renderTargetRegion = new TextureRegion(_renderTarget);
    }

    private void DrawCanvasFrame(SpriteBatch spriteBatch, Rectangle canvasBounds, ContinentZoomRenderOptions options)
    {
        if (_pixelTexture == null)
        {
            return;
        }

        spriteBatch.Draw(_pixelTexture, canvasBounds, Color.Black * 0.86f);
        DrawRectangle(spriteBatch, canvasBounds, options.BoundaryColor, options.BoundaryThickness);
    }

    private void DrawTerritories(
        SpriteBatch spriteBatch,
        Rectangle canvasBounds,
        IReadOnlyList<ContinentZoomRegionRenderInfo> regions,
        ContinentZoomRenderOptions options)
    {
        if (_pixelTexture == null)
        {
            return;
        }

        foreach (var tile in ContinentZoomRenderModel.BuildTerritoryTiles(canvasBounds.Width, canvasBounds.Height, regions, options))
        {
            spriteBatch.Draw(_pixelTexture, Offset(tile.Bounds, canvasBounds), tile.FillColor);
        }
    }

    private void DrawDividerLines(
        SpriteBatch spriteBatch,
        Rectangle canvasBounds,
        IReadOnlyList<ContinentZoomRegionRenderInfo> regions,
        ContinentZoomRenderOptions options)
    {
        foreach (var segment in ContinentZoomRenderModel.BuildBoundarySegments(canvasBounds.Width, canvasBounds.Height, regions, options, selectedOnly: false))
        {
            DrawFilledRectangle(spriteBatch, Offset(segment.Bounds, canvasBounds), segment.Color);
        }
    }

    private void DrawSelectedRegionBoundary(
        SpriteBatch spriteBatch,
        Rectangle canvasBounds,
        IReadOnlyList<ContinentZoomRegionRenderInfo> regions,
        ContinentZoomRenderOptions options)
    {
        var selected = regions.FirstOrDefault(region => region.IsSelected);
        if (selected.Region == null)
        {
            return;
        }

        foreach (var segment in ContinentZoomRenderModel.BuildBoundarySegments(canvasBounds.Width, canvasBounds.Height, regions, options, selectedOnly: true))
        {
            DrawFilledRectangle(spriteBatch, Offset(segment.Bounds, canvasBounds), segment.Color);
        }
    }

    private void DrawPlanetBoundary(SpriteBatch spriteBatch, Rectangle canvasBounds, ContinentZoomRenderOptions options)
    {
        var planetBounds = ContinentZoomLayout.GetPlanetSurfaceBounds(canvasBounds.Width, canvasBounds.Height);
        var center = new Vector2(canvasBounds.X + planetBounds.Center.X, canvasBounds.Y + planetBounds.Center.Y);
        DrawCircle(spriteBatch, center, planetBounds.Width / 2f, options.BoundaryColor, options.BoundaryThickness + 1);
    }

    private void DrawRegionLabels(
        SpriteBatch spriteBatch,
        Rectangle canvasBounds,
        IReadOnlyList<ContinentZoomRegionRenderInfo> regions,
        ContinentZoomRenderOptions options)
    {
        if (_pixelTexture == null || _font == null)
        {
            return;
        }

        foreach (var region in regions)
        {
            string name = region.Region.Name;
            float uiScaleFactor = ThemeManager.CurrentUiScaleFactor;
            float scale = Math.Clamp(region.HitBounds.Width / 145f, 0.72f, 1.05f) * uiScaleFactor;
            var measured = _font.MeasureString(name) * scale;
            int paddingX = ThemeManager.ScalePixels(5);
            int paddingY = ThemeManager.ScalePixels(3);
            int borderThickness = ThemeManager.ScalePixels(1);
            var position = new Vector2(
                canvasBounds.X + region.Center.X - measured.X / 2f,
                canvasBounds.Y + region.Center.Y - measured.Y / 2f);
            var labelBounds = new Rectangle(
                (int)MathF.Round(position.X - paddingX),
                (int)MathF.Round(position.Y - paddingY),
                (int)MathF.Round(measured.X + paddingX * 2),
                (int)MathF.Round(measured.Y + paddingY * 2));

            spriteBatch.Draw(_pixelTexture, labelBounds, options.LabelBackgroundColor);
            DrawRectangle(spriteBatch, labelBounds, region.IsSelected ? options.SelectedBoundaryColor : options.BoundaryColor, borderThickness);
            spriteBatch.DrawString(_font, name, position, options.LabelTextColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
    }

    private void DrawFilledRectangle(SpriteBatch spriteBatch, Rectangle bounds, Color color)
    {
        if (_pixelTexture == null || bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        spriteBatch.Draw(_pixelTexture, bounds, color);
    }

    private void DrawRectangle(SpriteBatch spriteBatch, Rectangle bounds, Color color, int thickness)
    {
        DrawFilledRectangle(spriteBatch, new Rectangle(bounds.X, bounds.Y, bounds.Width, thickness), color);
        DrawFilledRectangle(spriteBatch, new Rectangle(bounds.X, bounds.Bottom - thickness, bounds.Width, thickness), color);
        DrawFilledRectangle(spriteBatch, new Rectangle(bounds.X, bounds.Y, thickness, bounds.Height), color);
        DrawFilledRectangle(spriteBatch, new Rectangle(bounds.Right - thickness, bounds.Y, thickness, bounds.Height), color);
    }

    private static Rectangle Offset(Rectangle bounds, Rectangle canvasBounds)
    {
        return new Rectangle(
            canvasBounds.X + bounds.X,
            canvasBounds.Y + bounds.Y,
            bounds.Width,
            bounds.Height);
    }

    private void DrawCircle(SpriteBatch spriteBatch, Vector2 center, float radius, Color color, float thickness)
    {
        int segments = Math.Max(96, (int)MathF.Round(radius * ThemeManager.CurrentUiScaleFactor / 2f));
        Vector2 previousPoint = center + new Vector2(radius, 0);
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * MathF.Tau / segments;
            Vector2 currentPoint = center + new Vector2(MathF.Cos(angle) * radius, MathF.Sin(angle) * radius);
            DrawLine(spriteBatch, previousPoint, currentPoint, color, thickness);
            previousPoint = currentPoint;
        }
    }

    private void DrawLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, float thickness)
    {
        if (_pixelTexture == null)
        {
            return;
        }

        float distance = Vector2.Distance(start, end);
        float angle = MathF.Atan2(end.Y - start.Y, end.X - start.X);
        spriteBatch.Draw(
            _pixelTexture,
            start,
            null,
            color,
            angle,
            new Vector2(0, 0.5f),
            new Vector2(distance, thickness),
            SpriteEffects.None,
            0f);
    }

    public void Dispose()
    {
        _renderTargetRegion = null;
        _renderTarget?.Dispose();
        _renderTarget = null;
        _pixelTexture?.Dispose();
        _pixelTexture = null;
    }
}
