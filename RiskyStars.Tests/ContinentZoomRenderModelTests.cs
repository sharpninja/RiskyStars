using Microsoft.Xna.Framework;
using RiskyStars.Client;

namespace RiskyStars.Tests;

public class ContinentZoomRenderModelTests
{
    [Fact]
    public void DefaultOptions_ExposeRendererLabelAndSamplingSettings()
    {
        var options = ContinentZoomRenderModel.DefaultOptions;

        Assert.Equal(Color.WhiteSmoke, options.LabelTextColor);
        Assert.Equal(Color.Black * 0.72f, options.LabelBackgroundColor);
        Assert.Equal(5, options.SampleSize);
        Assert.Equal(2, options.BoundaryThickness);
    }

    [Fact]
    public void DpiAwareMetrics_IncreaseResolutionAndStrokeAtHighScale()
    {
        int standardSampleSize = ContinentZoomRenderModel.GetDpiAwareSampleSize(1f);
        int highDpiSampleSize = ContinentZoomRenderModel.GetDpiAwareSampleSize(1.6f);
        int standardBoundaryThickness = ContinentZoomRenderModel.GetDpiAwareBoundaryThickness(1f);
        int highDpiBoundaryThickness = ContinentZoomRenderModel.GetDpiAwareBoundaryThickness(1.6f);

        Assert.True(standardSampleSize < 7);
        Assert.True(highDpiSampleSize < standardSampleSize);
        Assert.True(highDpiBoundaryThickness > standardBoundaryThickness);
        Assert.Equal(ContinentZoomRenderModel.GetDpiAwareSampleSize(0.8f), ContinentZoomRenderModel.GetDpiAwareSampleSize(0.1f));
        Assert.Equal(ContinentZoomRenderModel.GetDpiAwareBoundaryThickness(1.6f), ContinentZoomRenderModel.GetDpiAwareBoundaryThickness(9f));

        var highDpiOptions = ContinentZoomRenderModel.CreateDefaultOptions(1.6f);
        Assert.Equal(highDpiSampleSize, highDpiOptions.SampleSize);
        Assert.Equal(highDpiBoundaryThickness, highDpiOptions.BoundaryThickness);
    }

    [Fact]
    public void ModelMethods_ReturnEmptyResultsForInvalidInputs()
    {
        var body = CreateCrossBody();
        var regions = ContinentZoomRenderModel.BuildRegions(
            body,
            width: 640,
            height: 420,
            _ => null,
            _ => Color.White,
            selectedRegionId: null,
            ContinentZoomRenderModel.DefaultOptions);

        Assert.Empty(ContinentZoomRenderModel.BuildRegions(null, 640, 420, _ => null, _ => Color.White, null, ContinentZoomRenderModel.DefaultOptions));
        Assert.Empty(ContinentZoomRenderModel.BuildRegions(body, 0, 420, _ => null, _ => Color.White, null, ContinentZoomRenderModel.DefaultOptions));
        Assert.Empty(ContinentZoomRenderModel.BuildRegions(body, 640, 0, _ => null, _ => Color.White, null, ContinentZoomRenderModel.DefaultOptions));
        Assert.Null(ContinentZoomRenderModel.HitTest(null, new Rectangle(0, 0, 640, 420), Point.Zero));
        Assert.Null(ContinentZoomRenderModel.HitTest(body, Rectangle.Empty, Point.Zero));
        Assert.Null(ContinentZoomRenderModel.FindNearestRegion([], Point.Zero));
        Assert.Empty(ContinentZoomRenderModel.BuildTerritoryTiles(0, 420, regions, ContinentZoomRenderModel.DefaultOptions));
        Assert.Empty(ContinentZoomRenderModel.BuildTerritoryTiles(640, 0, regions, ContinentZoomRenderModel.DefaultOptions));
        Assert.Empty(ContinentZoomRenderModel.BuildTerritoryTiles(640, 420, [], ContinentZoomRenderModel.DefaultOptions));
        Assert.Empty(ContinentZoomRenderModel.BuildBoundarySegments(0, 420, regions, ContinentZoomRenderModel.DefaultOptions, selectedOnly: false));
        Assert.Empty(ContinentZoomRenderModel.BuildBoundarySegments(640, 0, regions, ContinentZoomRenderModel.DefaultOptions, selectedOnly: false));
        Assert.Empty(ContinentZoomRenderModel.BuildBoundarySegments(640, 420, [], ContinentZoomRenderModel.DefaultOptions, selectedOnly: false));
        Assert.Empty(ContinentZoomRenderModel.BuildBoundarySegments(640, 420, regions, ContinentZoomRenderModel.DefaultOptions, selectedOnly: true));
    }

    [Fact]
    public void BuildRegions_UsesSolidOwnerColorForClaimedRegion()
    {
        var body = CreateCrossBody();
        var ownerColor = Color.CornflowerBlue;

        var regions = ContinentZoomRenderModel.BuildRegions(
            body,
            width: 640,
            height: 420,
            region => region.Id == "left" ? "player-1" : null,
            ownerId => ownerId == "player-1" ? ownerColor : Color.White,
            selectedRegionId: null,
            ContinentZoomRenderModel.DefaultOptions);

        var claimedRegion = Assert.Single(regions, region => region.Region.Id == "left");
        Assert.Equal("player-1", claimedRegion.OwnerId);
        Assert.Equal(ownerColor, claimedRegion.FillColor);
        Assert.NotEqual(ContinentZoomRenderModel.DefaultOptions.NeutralFillColor, claimedRegion.FillColor);
    }

    [Fact]
    public void BuildRegions_LeavesUnclaimedRegionNeutralInsteadOfInventingPlayerColor()
    {
        var body = CreateCrossBody();

        var regions = ContinentZoomRenderModel.BuildRegions(
            body,
            width: 640,
            height: 420,
            _ => null,
            _ => Color.Red,
            selectedRegionId: null,
            ContinentZoomRenderModel.DefaultOptions);

        Assert.All(regions, region =>
        {
            Assert.Null(region.OwnerId);
            Assert.Equal(ContinentZoomRenderModel.DefaultOptions.NeutralFillColor, region.FillColor);
        });
    }

    [Fact]
    public void BuildRegions_HighlightsOnlyTheSelectedBoundary()
    {
        var body = CreateCrossBody();

        var regions = ContinentZoomRenderModel.BuildRegions(
            body,
            width: 640,
            height: 420,
            _ => null,
            _ => Color.White,
            selectedRegionId: "center",
            ContinentZoomRenderModel.DefaultOptions);

        var selectedRegion = Assert.Single(regions, region => region.Region.Id == "center");
        var unselectedRegion = Assert.Single(regions, region => region.Region.Id == "left");
        Assert.True(selectedRegion.IsSelected);
        Assert.Equal(ContinentZoomRenderModel.DefaultOptions.SelectedBoundaryColor, selectedRegion.BoundaryColor);
        Assert.False(unselectedRegion.IsSelected);
        Assert.NotEqual(ContinentZoomRenderModel.DefaultOptions.SelectedBoundaryColor, unselectedRegion.BoundaryColor);
    }

    [Fact]
    public void HitTest_ReturnsCenterRegionForCenterPlanetClick()
    {
        var body = CreateCrossBody();
        var canvasBounds = new Rectangle(120, 80, 640, 420);

        var selectedRegion = ContinentZoomRenderModel.HitTest(body, canvasBounds, canvasBounds.Center);

        Assert.NotNull(selectedRegion);
        Assert.Equal("center", selectedRegion.Id);
    }

    [Fact]
    public void HitTest_SelectsNearestRegionInsidePlanetEvenWhenClickMissesButtonBounds()
    {
        var body = CreateCrossBody();
        var canvasBounds = new Rectangle(120, 80, 640, 420);
        var localClick = new Point(220, 110);
        var click = new Point(canvasBounds.X + localClick.X, canvasBounds.Y + localClick.Y);
        Assert.DoesNotContain(
            ContinentZoomLayout.Build(body, canvasBounds.Width, canvasBounds.Height),
            layout => layout.Bounds.Contains(localClick));

        var selectedRegion = ContinentZoomRenderModel.HitTest(body, canvasBounds, click);

        Assert.NotNull(selectedRegion);
        Assert.Equal("top", selectedRegion.Id);
    }

    [Fact]
    public void HitTest_IgnoresClicksOutsideThePlanetSurface()
    {
        var body = CreateCrossBody();
        var canvasBounds = new Rectangle(120, 80, 640, 420);
        var outsidePlanet = new Point(canvasBounds.X + 120, canvasBounds.Y + 40);

        var selectedRegion = ContinentZoomRenderModel.HitTest(body, canvasBounds, outsidePlanet);

        Assert.Null(selectedRegion);
    }

    [Fact]
    public void BuildTerritoryTiles_UsesOwnerFillColorsForPlanetTiles()
    {
        var body = CreateCrossBody();
        var ownerColor = Color.OrangeRed;
        var regions = ContinentZoomRenderModel.BuildRegions(
            body,
            width: 640,
            height: 420,
            region => region.Id == "right" ? "player-2" : null,
            ownerId => ownerId == "player-2" ? ownerColor : Color.White,
            selectedRegionId: null,
            ContinentZoomRenderModel.DefaultOptions);

        var tiles = ContinentZoomRenderModel.BuildTerritoryTiles(640, 420, regions, ContinentZoomRenderModel.DefaultOptions);

        Assert.NotEmpty(tiles);
        Assert.Contains(tiles, tile => tile.RegionId == "right" && tile.FillColor == ownerColor);
        Assert.Contains(tiles, tile => tile.RegionId == "center" && tile.FillColor == ContinentZoomRenderModel.DefaultOptions.NeutralFillColor);
        Assert.All(tiles, tile => Assert.True(ContinentZoomLayout.GetPlanetSurfaceBounds(640, 420).Intersects(tile.Bounds)));
    }

    [Fact]
    public void BuildBoundarySegments_CreatesDividerLinesBetweenContinentRegions()
    {
        var body = CreateCrossBody();
        var regions = ContinentZoomRenderModel.BuildRegions(
            body,
            width: 640,
            height: 420,
            _ => null,
            _ => Color.White,
            selectedRegionId: null,
            ContinentZoomRenderModel.DefaultOptions);

        var segments = ContinentZoomRenderModel.BuildBoundarySegments(640, 420, regions, ContinentZoomRenderModel.DefaultOptions, selectedOnly: false);

        Assert.NotEmpty(segments);
        Assert.All(segments, segment =>
        {
            Assert.True(segment.Bounds.Width > 0);
            Assert.True(segment.Bounds.Height > 0);
            Assert.False(segment.IsSelectedBoundary);
            Assert.Equal(ContinentZoomRenderModel.DefaultOptions.BoundaryColor, segment.Color);
        });
    }

    [Fact]
    public void BuildBoundarySegments_HighlightsSelectedContinentBoundaryOnly()
    {
        var body = CreateCrossBody();
        var regions = ContinentZoomRenderModel.BuildRegions(
            body,
            width: 640,
            height: 420,
            _ => null,
            _ => Color.White,
            selectedRegionId: "right",
            ContinentZoomRenderModel.DefaultOptions);
        var selectedRegion = Assert.Single(regions, region => region.Region.Id == "right");

        var selectedSegments = ContinentZoomRenderModel.BuildBoundarySegments(640, 420, regions, ContinentZoomRenderModel.DefaultOptions, selectedOnly: true);

        Assert.NotEmpty(selectedSegments);
        Assert.Contains(selectedSegments, segment => segment.Bounds.Center.X < selectedRegion.Center.X);
        Assert.Contains(selectedSegments, segment => segment.Bounds.Center.X > selectedRegion.Center.X);
        Assert.Contains(selectedSegments, segment => segment.Bounds.Center.Y < selectedRegion.Center.Y);
        Assert.Contains(selectedSegments, segment => segment.Bounds.Center.Y > selectedRegion.Center.Y);
        Assert.All(selectedSegments, segment =>
        {
            Assert.True(segment.Bounds.Width > 0);
            Assert.True(segment.Bounds.Height > 0);
            Assert.True(segment.IsSelectedBoundary);
            Assert.Equal(ContinentZoomRenderModel.DefaultOptions.SelectedBoundaryColor, segment.Color);
        });
    }

    private static StellarBodyData CreateCrossBody()
    {
        var body = new StellarBodyData
        {
            Id = "body-1",
            Name = "Canopus b",
            StarSystemId = "star-1",
            Type = StellarBodyType.RockyPlanet,
            Position = new Vector2(100, 100)
        };

        body.Regions.Add(new RegionData { Id = "left", Name = "Left", StellarBodyId = body.Id, Position = body.Position + new Vector2(-12, 0) });
        body.Regions.Add(new RegionData { Id = "top", Name = "Top", StellarBodyId = body.Id, Position = body.Position + new Vector2(0, -8) });
        body.Regions.Add(new RegionData { Id = "right", Name = "Right", StellarBodyId = body.Id, Position = body.Position + new Vector2(12, 0) });
        body.Regions.Add(new RegionData { Id = "bottom", Name = "Bottom", StellarBodyId = body.Id, Position = body.Position + new Vector2(0, 8) });
        body.Regions.Add(new RegionData { Id = "center", Name = "Center", StellarBodyId = body.Id, Position = body.Position });

        return body;
    }
}
