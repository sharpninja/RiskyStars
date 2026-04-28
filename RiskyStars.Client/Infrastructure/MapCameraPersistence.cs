using Microsoft.Xna.Framework;

namespace RiskyStars.Client;

internal static class MapCameraPersistence
{
    public static void Capture(MapCameraSettings settings, Camera2D camera)
    {
        settings.Capture(camera.Position.X, camera.Position.Y, camera.Zoom);
    }

    public static bool Restore(MapCameraSettings settings, Camera2D camera)
    {
        settings.Normalize();
        if (!settings.HasSavedView)
        {
            return false;
        }

        camera.SetView(new Vector2(settings.PositionX, settings.PositionY), settings.Zoom);
        return true;
    }
}
