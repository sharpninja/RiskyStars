using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI.Styles;
using RiskyStars.Client;

namespace RiskyStars.Tests;

[Collection("Myra UI tests")]
public class ContinentZoomWindowTests
{
    [Fact]
    public void Show_DisplaysBodyStarAndContinentLayouts()
    {
        var window = CreateWindow();
        var body = CreateBody(regionCount: 4);
        var starSystem = CreateStarSystem(body);

        window.Show(body, starSystem);

        Assert.True(window.IsVisible);
        Assert.Equal("Sirius b", window.TitleText);
        Assert.Contains("Sirius", window.SubtitleText, StringComparison.Ordinal);
        Assert.Equal(body.Regions.Count, window.CurrentLayouts.Count);
        Assert.All(window.CurrentLayouts, layout =>
        {
            Assert.True(layout.Bounds.Width >= ContinentZoomLayout.MinimumButtonSize);
            Assert.True(layout.Bounds.Height >= ContinentZoomLayout.MinimumButtonSize);
        });
    }

    [Fact]
    public void Show_HandlesMissingStarSystem()
    {
        var window = CreateWindow();
        var body = CreateBody(regionCount: 3);

        window.Show(body, starSystem: null);

        Assert.True(window.IsVisible);
        Assert.Equal("Select a continent.", window.SubtitleText);
        Assert.Equal(body.Regions.Count, window.CurrentLayouts.Count);
    }

    [Fact]
    public void SelectRegion_RaisesSelectionAndClosesWindow()
    {
        var window = CreateWindow();
        var body = CreateBody(regionCount: 3);
        RegionData? selectedRegion = null;
        window.RegionSelected += region => selectedRegion = region;

        window.Show(body, CreateStarSystem(body));
        window.SelectRegion(body.Regions[1]);

        Assert.Same(body.Regions[1], selectedRegion);
        Assert.False(window.IsVisible);
    }

    [Fact]
    public void SelectRegion_ClosesWindowWithoutSubscriber()
    {
        var window = CreateWindow();
        var body = CreateBody(regionCount: 3);

        window.Show(body, CreateStarSystem(body));
        window.SelectRegion(body.Regions[0]);

        Assert.False(window.IsVisible);
    }

    [Fact]
    public void Hide_ClosesVisibleWindow()
    {
        var window = CreateWindow();
        var body = CreateBody(regionCount: 2);

        window.Show(body, CreateStarSystem(body));
        window.Hide();

        Assert.False(window.IsVisible);
    }

    [Fact]
    public void ResizeViewport_RebuildsLayoutsForVisibleBody()
    {
        var window = CreateWindow();
        var body = CreateBody(regionCount: 5);
        window.Show(body, CreateStarSystem(body));
        var originalBounds = window.CurrentLayouts.Select(layout => layout.Bounds).ToArray();

        window.ResizeViewport(500, 400);

        Assert.True(window.IsVisible);
        Assert.Equal(body.Regions.Count, window.CurrentLayouts.Count);
        Assert.NotEqual(originalBounds, window.CurrentLayouts.Select(layout => layout.Bounds).ToArray());
    }

    [Fact]
    public void ResizeViewport_IgnoresInvalidSize()
    {
        var window = CreateWindow();
        int? left = window.Window.Left;
        int? top = window.Window.Top;

        window.ResizeViewport(0, 700);

        Assert.Equal(left, window.Window.Left);
        Assert.Equal(top, window.Window.Top);
    }

    private static ContinentZoomWindow CreateWindow()
    {
        Stylesheet.Current = new Stylesheet
        {
            LabelStyle = new LabelStyle(),
            ButtonStyle = new ButtonStyle(),
            WindowStyle = new WindowStyle()
        };
        ThemeManager.Initialize();
        return new ContinentZoomWindow(1280, 720);
    }

    private static StarSystemData CreateStarSystem(StellarBodyData body)
    {
        return new StarSystemData
        {
            Id = "star-1",
            Name = "Sirius",
            Position = Vector2.Zero,
            Type = StarSystemType.Home,
            StellarBodies = { body }
        };
    }

    private static StellarBodyData CreateBody(int regionCount)
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
                Position = body.Position
            });
        }

        return body;
    }
}
