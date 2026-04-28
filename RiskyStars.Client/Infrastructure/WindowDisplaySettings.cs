namespace RiskyStars.Client;

public static class WindowDisplaySettings
{
    public static bool HasDisplaySettingsChanged(Settings requested, AppliedWindowDisplayState applied)
    {
        var modeChanged = requested.WindowMode != applied.WindowMode ||
                          requested.Fullscreen != applied.IsFullscreen;
        var resolutionChanged = requested.WindowMode != GameWindowMode.Maximized &&
                                (requested.ResolutionWidth != applied.BackBufferWidth ||
                                 requested.ResolutionHeight != applied.BackBufferHeight);

        return modeChanged || resolutionChanged;
    }

    public static void CaptureCurrentDisplay(Settings settings, AppliedWindowDisplayState captured)
    {
        settings.WindowMode = captured.WindowMode;
        settings.Fullscreen = captured.WindowMode == GameWindowMode.Full;

        if (ShouldCaptureResolution(captured.WindowMode, captured.BackBufferWidth, captured.BackBufferHeight))
        {
            settings.ResolutionWidth = captured.BackBufferWidth;
            settings.ResolutionHeight = captured.BackBufferHeight;
        }

        settings.Normalize();
    }

    public static bool ShouldCaptureResolution(GameWindowMode mode, int width, int height)
    {
        return width > 0 &&
               height > 0 &&
               mode != GameWindowMode.Maximized;
    }
}

public readonly record struct AppliedWindowDisplayState(
    int BackBufferWidth,
    int BackBufferHeight,
    bool IsFullscreen,
    GameWindowMode WindowMode);
