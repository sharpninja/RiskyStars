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
            Assert.True(ContinentZoomLayout.GetPlanetSurfaceBounds(640, 420).Contains(layout.Bounds));
        });
    }

    [Fact]
    public void Build_PreservesPlanetRelativePositionsForCrossLayout()
    {
        var body = CreateCrossBody();

        var layouts = ContinentZoomLayout.Build(body, width: 640, height: 420)
            .ToDictionary(layout => layout.Region.Id, layout => CenterOf(layout.Bounds));
        var center = layouts["center"];

        Assert.InRange(Vector2.Distance(center, new Vector2(320, 210)), 0, 1);
        Assert.True(layouts["left"].X < center.X, "Left continent should remain left of the planet center.");
        Assert.True(layouts["right"].X > center.X, "Right continent should remain right of the planet center.");
        Assert.True(layouts["top"].Y < center.Y, "Top continent should remain above the planet center.");
        Assert.True(layouts["bottom"].Y > center.Y, "Bottom continent should remain below the planet center.");
        Assert.InRange(layouts["left"].Y, center.Y - 1, center.Y + 1);
        Assert.InRange(layouts["right"].Y, center.Y - 1, center.Y + 1);
        Assert.InRange(layouts["top"].X, center.X - 1, center.X + 1);
        Assert.InRange(layouts["bottom"].X, center.X - 1, center.X + 1);
    }

    [Fact]
    public void GetPlanetSurfaceBounds_CreatesCenteredSquareSurface()
    {
        var bounds = ContinentZoomLayout.GetPlanetSurfaceBounds(width: 640, height: 420);

        Assert.Equal(bounds.Width, bounds.Height);
        Assert.Equal(320, bounds.Center.X);
        Assert.Equal(210, bounds.Center.Y);
        Assert.True(bounds.Width < 420);
        Assert.True(bounds.Width > ContinentZoomLayout.MaximumButtonSize);
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

    private static Vector2 CenterOf(Rectangle bounds)
    {
        return new Vector2(bounds.Left + bounds.Width / 2f, bounds.Top + bounds.Height / 2f);
    }
}
