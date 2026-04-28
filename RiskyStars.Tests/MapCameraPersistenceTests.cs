using Microsoft.Xna.Framework;
using RiskyStars.Client;

namespace RiskyStars.Tests;

public class MapCameraPersistenceTests
{
    [Fact]
    public void CaptureAndRestore_AppliesLastMapPositionAndZoom()
    {
        var sourceCamera = new Camera2D(1280, 720);
        sourceCamera.SetView(new Vector2(325.5f, -140.25f), 2.25f);
        var settings = new MapCameraSettings();

        MapCameraPersistence.Capture(settings, sourceCamera);

        var restoredCamera = new Camera2D(1920, 1080);
        bool restored = MapCameraPersistence.Restore(settings, restoredCamera);

        Assert.True(restored);
        Assert.Equal(sourceCamera.Position.X, restoredCamera.Position.X);
        Assert.Equal(sourceCamera.Position.Y, restoredCamera.Position.Y);
        Assert.Equal(sourceCamera.Zoom, restoredCamera.Zoom);
    }

    [Fact]
    public void Restore_DoesNotOverwriteCurrentCameraWhenNoSavedViewExists()
    {
        var settings = new MapCameraSettings();
        var camera = new Camera2D(1280, 720);
        camera.SetView(new Vector2(50f, 75f), 1.5f);

        bool restored = MapCameraPersistence.Restore(settings, camera);

        Assert.False(restored);
        Assert.Equal(50f, camera.Position.X);
        Assert.Equal(75f, camera.Position.Y);
        Assert.Equal(1.5f, camera.Zoom);
    }

    [Theory]
    [InlineData(float.NaN, 0f, 1f)]
    [InlineData(0f, float.PositiveInfinity, 1f)]
    [InlineData(1_000_001f, 0f, 1f)]
    [InlineData(0f, 0f, float.NegativeInfinity)]
    public void Normalize_RejectsCorruptPersistedCameraView(float positionX, float positionY, float zoom)
    {
        var settings = new MapCameraSettings
        {
            HasSavedView = true,
            PositionX = positionX,
            PositionY = positionY,
            Zoom = zoom
        };

        settings.Normalize();

        Assert.False(settings.HasSavedView);
        Assert.Equal(0f, settings.PositionX);
        Assert.Equal(0f, settings.PositionY);
        Assert.Equal(1f, settings.Zoom);
    }

    [Theory]
    [InlineData(50f, Camera2D.MaximumZoom)]
    [InlineData(0.001f, Camera2D.MinimumZoom)]
    public void Restore_ClampsOutOfRangeZoomInsteadOfApplyingBadValue(float persistedZoom, float expectedZoom)
    {
        var settings = new MapCameraSettings
        {
            HasSavedView = true,
            PositionX = 120f,
            PositionY = 240f,
            Zoom = persistedZoom
        };
        var camera = new Camera2D(1280, 720);

        bool restored = MapCameraPersistence.Restore(settings, camera);

        Assert.True(restored);
        Assert.Equal(120f, camera.Position.X);
        Assert.Equal(240f, camera.Position.Y);
        Assert.Equal(expectedZoom, camera.Zoom);
    }

    [Fact]
    public void Clone_CopiesCameraViewWithoutSharingReference()
    {
        var settings = new MapCameraSettings();
        settings.Capture(42f, -12f, 1.75f);

        var clone = settings.Clone();
        clone.Capture(99f, 100f, 2f);

        Assert.True(settings.HasSavedView);
        Assert.Equal(42f, settings.PositionX);
        Assert.Equal(-12f, settings.PositionY);
        Assert.Equal(1.75f, settings.Zoom);
        Assert.Equal(99f, clone.PositionX);
        Assert.Equal(100f, clone.PositionY);
        Assert.Equal(2f, clone.Zoom);
    }
}
