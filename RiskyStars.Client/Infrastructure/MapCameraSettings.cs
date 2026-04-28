namespace RiskyStars.Client;

public class MapCameraSettings
{
    private const float DefaultZoom = 1.0f;
    private const float MaximumPersistedPositionMagnitude = 1_000_000f;

    public bool HasSavedView { get; set; }
    public float PositionX { get; set; }
    public float PositionY { get; set; }
    public float Zoom { get; set; } = DefaultZoom;

    public void Capture(float positionX, float positionY, float zoom)
    {
        HasSavedView = true;
        PositionX = positionX;
        PositionY = positionY;
        Zoom = zoom;
        Normalize();
    }

    public MapCameraSettings Clone()
    {
        return new MapCameraSettings
        {
            HasSavedView = HasSavedView,
            PositionX = PositionX,
            PositionY = PositionY,
            Zoom = Zoom
        };
    }

    public void Normalize()
    {
        if (!HasSavedView)
        {
            Reset();
            return;
        }

        if (!IsValidPosition(PositionX) || !IsValidPosition(PositionY) || !float.IsFinite(Zoom))
        {
            Reset();
            return;
        }

        Zoom = Math.Clamp(Zoom, Camera2D.MinimumZoom, Camera2D.MaximumZoom);
    }

    private void Reset()
    {
        HasSavedView = false;
        PositionX = 0f;
        PositionY = 0f;
        Zoom = DefaultZoom;
    }

    private static bool IsValidPosition(float value)
    {
        return float.IsFinite(value) && Math.Abs(value) <= MaximumPersistedPositionMagnitude;
    }
}
