using Microsoft.Xna.Framework;
using RiskyStars.Client;

namespace RiskyStars.Tests;

public class ContinentZoomLayoutTests
{
    [Fact]
    public void FindZoomableBodyAtPosition_ReturnsMultiRegionBodyWhenClickIsAtPlanetCenter()
    {
        var body = CreateBody(regionCount: 4, centerPacked: true);
        var mapData = CreateMapData(body);

        var zoomableBody = ContinentZoomLayout.FindZoomableBodyAtPosition(mapData, body.Position);

        Assert.Same(body, zoomableBody);
    }

    [Fact]
    public void FindZoomableBodyAtPosition_IgnoresSingleRegionBody()
    {
        var body = CreateBody(regionCount: 1, centerPacked: true);
        var mapData = CreateMapData(body);

        var zoomableBody = ContinentZoomLayout.FindZoomableBodyAtPosition(mapData, body.Position);

        Assert.Null(zoomableBody);
    }

    [Fact]
    public void FindZoomableBodyAtPosition_IgnoresDistantClick()
    {
        var body = CreateBody(regionCount: 4, centerPacked: true);
        var mapData = CreateMapData(body);

        var zoomableBody = ContinentZoomLayout.FindZoomableBodyAtPosition(mapData, body.Position + new Vector2(200, 0));

        Assert.Null(zoomableBody);
    }

    [Fact]
    public void Build_CreatesLargeClickableButtonForEveryContinent()
    {
        var body = CreateBody(regionCount: 6, centerPacked: false);

        var layouts = ContinentZoomLayout.Build(body, width: 640, height: 420);

        Assert.Equal(body.Regions.Count, layouts.Count);
        Assert.Equal(body.Regions.Select(region => region.Id), layouts.Select(layout => layout.Region.Id).OrderBy(id => id));
        Assert.All(layouts, layout =>
        {
            Assert.True(layout.Bounds.Width >= ContinentZoomLayout.MinimumButtonSize);
            Assert.True(layout.Bounds.Height >= ContinentZoomLayout.MinimumButtonSize);
            Assert.InRange(layout.Bounds.Left, 0, 640 - layout.Bounds.Width);
            Assert.InRange(layout.Bounds.Top, 0, 420 - layout.Bounds.Height);
        });
    }

    [Fact]
    public void Build_SpreadsOverlappingCenterContinentsApart()
    {
        var body = CreateBody(regionCount: 6, centerPacked: true);

        var layouts = ContinentZoomLayout.Build(body, width: 640, height: 420);

        Assert.Equal(body.Regions.Count, layouts.Select(layout => layout.Bounds.Location).Distinct().Count());
        for (int i = 0; i < layouts.Count; i++)
        {
            for (int j = i + 1; j < layouts.Count; j++)
            {
                Assert.False(layouts[i].Bounds.Intersects(layouts[j].Bounds));
            }
        }
    }

    [Fact]
    public void Build_ReturnsEmptyForInvalidInput()
    {
        var body = CreateBody(regionCount: 3, centerPacked: true);

        Assert.Empty(ContinentZoomLayout.Build(body, width: 0, height: 420));
        Assert.Empty(ContinentZoomLayout.Build(body, width: 640, height: 0));
        Assert.Empty(ContinentZoomLayout.Build(CreateBody(regionCount: 0, centerPacked: true), width: 640, height: 420));
    }

    [Theory]
    [InlineData(StellarBodyType.GasGiant, 20f)]
    [InlineData(StellarBodyType.RockyPlanet, 15f)]
    [InlineData(StellarBodyType.Planetoid, 8f)]
    [InlineData(StellarBodyType.Comet, 6f)]
    public void GetBodyHitRadius_UsesRenderedPlanetSize(StellarBodyType bodyType, float expectedRadius)
    {
        var body = CreateBody(regionCount: 2, centerPacked: true);
        body.Type = bodyType;

        Assert.Equal(expectedRadius, ContinentZoomLayout.GetBodyHitRadius(body));
    }

    private static MapData CreateMapData(StellarBodyData body)
    {
        return new MapData
        {
            StarSystems =
            {
                new StarSystemData
                {
                    Id = "star-1",
                    Name = "Sirius",
                    Position = Vector2.Zero,
                    Type = StarSystemType.Home,
                    StellarBodies = { body }
                }
            }
        };
    }

    private static StellarBodyData CreateBody(int regionCount, bool centerPacked)
    {
        var body = new StellarBodyData
        {
            Id = "body-1",
            Name = "Sirius b",
            StarSystemId = "star-1",
            Type = StellarBodyType.RockyPlanet,
            Position = new Vector2(100, 100)
        };

        for (int i = 0; i < regionCount; i++)
        {
            body.Regions.Add(new RegionData
            {
                Id = $"region-{i:D2}",
                Name = $"Continent {i:D2}",
                StellarBodyId = body.Id,
                Position = centerPacked
                    ? body.Position
                    : body.Position + new Vector2(MathF.Cos(i), MathF.Sin(i)) * 12f
            });
        }

        return body;
    }
}
