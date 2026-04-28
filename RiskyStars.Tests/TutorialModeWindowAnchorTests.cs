using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;
using RiskyStars.Client;

namespace RiskyStars.Tests;

[Collection("Myra UI tests")]
public class TutorialModeWindowAnchorTests
{
    [Fact]
    public void CalculateMapLeftAnchor_AttachesWindowToRightOfExpandedLeftDock()
    {
        ThemeManager.Initialize();
        int screenWidth = 1920;
        int screenHeight = 1080;
        int leftDockRight = 280;
        int rightDockLeft = 1660;
        int mapTop = 92;
        int windowWidth = 520;
        int windowHeight = 640;

        Point anchor = TutorialModeWindowAnchor.Calculate(
            screenWidth,
            screenHeight,
            leftDockRight,
            rightDockLeft,
            mapTop,
            windowWidth,
            windowHeight);

        Assert.Equal(leftDockRight + ThemeManager.ScalePixels(12), anchor.X);
        Assert.Equal(mapTop, anchor.Y);
    }

    [Fact]
    public void CalculateMapLeftAnchor_DoesNotUseOldScreenEdgeDockPosition()
    {
        ThemeManager.Initialize();
        int oldTopLeftDockX = ThemeManager.ScalePixels(10);

        Point anchor = TutorialModeWindowAnchor.Calculate(
            screenWidth: 1920,
            screenHeight: 1080,
            leftDockRight: 280,
            rightDockLeft: 1660,
            mapTop: 92,
            windowWidth: 520,
            windowHeight: 640);

        Assert.NotEqual(oldTopLeftDockX, anchor.X);
        Assert.True(anchor.X > oldTopLeftDockX);
    }

    [Fact]
    public void CalculateMapLeftAnchor_TracksCollapsedLeftDockWidth()
    {
        ThemeManager.Initialize();
        int collapsedDockRight = 60;
        int staleExpandedDockRight = 280;

        Point anchor = TutorialModeWindowAnchor.Calculate(
            screenWidth: 1920,
            screenHeight: 1080,
            leftDockRight: collapsedDockRight,
            rightDockLeft: 1660,
            mapTop: 92,
            windowWidth: 520,
            windowHeight: 640);

        Assert.Equal(collapsedDockRight + ThemeManager.ScalePixels(12), anchor.X);
        Assert.NotEqual(staleExpandedDockRight + ThemeManager.ScalePixels(12), anchor.X);
    }

    [Fact]
    public void CalculateMapLeftAnchor_ClampsInsideScreenWhenMapSpaceIsTooSmall()
    {
        ThemeManager.Initialize();

        Point anchor = TutorialModeWindowAnchor.Calculate(
            screenWidth: 640,
            screenHeight: 480,
            leftDockRight: 500,
            rightDockLeft: 620,
            mapTop: 430,
            windowWidth: 260,
            windowHeight: 160);

        Assert.InRange(anchor.X, ThemeManager.ScalePixels(10), 640 - 260 - ThemeManager.ScalePixels(10));
        Assert.True(anchor.Y >= ThemeManager.ScalePixels(40));
    }

    [Fact]
    public void CalculateMapLeftAnchor_ReturnsZeroForInvalidViewport()
    {
        ThemeManager.Initialize();

        Point anchor = TutorialModeWindowAnchor.Calculate(
            screenWidth: 0,
            screenHeight: 480,
            leftDockRight: 280,
            rightDockLeft: 620,
            mapTop: 92,
            windowWidth: 520,
            windowHeight: 640);

        Assert.Equal(Point.Zero, anchor);
    }

    [Fact]
    public void ApplyMapLeftAnchor_ReplacesStaleCenteredWindowPosition()
    {
        Stylesheet.Current = new Stylesheet
        {
            LabelStyle = new LabelStyle(),
            ButtonStyle = new ButtonStyle(),
            WindowStyle = new WindowStyle()
        };
        ThemeManager.Initialize();
        var window = new Window
        {
            Left = 680,
            Top = 144,
            Width = 520,
            Height = 640
        };

        Point anchor = TutorialModeWindowAnchor.Apply(
            window,
            screenWidth: 1920,
            screenHeight: 1080,
            leftDockRight: 280,
            rightDockLeft: 1660,
            mapTop: 92,
            defaultWindowWidth: 520,
            defaultWindowHeight: 640);

        Assert.Equal(280 + ThemeManager.ScalePixels(12), window.Left);
        Assert.Equal(92, window.Top);
        Assert.Equal(anchor, new Point(window.Left, window.Top));
        Assert.NotEqual(680, window.Left);
    }
}
