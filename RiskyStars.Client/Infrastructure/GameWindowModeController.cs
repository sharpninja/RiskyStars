using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace RiskyStars.Client;

public static class GameWindowModeController
{
    public static WindowModePlan CreatePlan(GameWindowMode mode)
    {
        return mode switch
        {
            GameWindowMode.Maximized => new WindowModePlan(false, true, false),
            GameWindowMode.Full => new WindowModePlan(true, false, false),
            _ => new WindowModePlan(false, false, true)
        };
    }

    public static GameWindowMode DetectMode(bool isFullscreen, bool isMaximized)
    {
        if (isFullscreen)
        {
            return GameWindowMode.Full;
        }

        return isMaximized ? GameWindowMode.Maximized : GameWindowMode.Normal;
    }

    [ExcludeFromCodeCoverage]
    public static bool TryDetectMaximized(IntPtr windowHandle, out bool isMaximized)
    {
        return SdlWindowStateInterop.TryGetIsMaximized(windowHandle, out isMaximized);
    }

    [ExcludeFromCodeCoverage]
    public static void ApplyDesktopMode(IntPtr windowHandle, GameWindowMode mode)
    {
        if (windowHandle == IntPtr.Zero)
        {
            return;
        }

        if (mode == GameWindowMode.Maximized)
        {
            SdlWindowStateInterop.TryMaximize(windowHandle);
            return;
        }

        SdlWindowStateInterop.TryRestore(windowHandle);
    }
}

public readonly record struct WindowModePlan(bool IsFullscreen, bool MaximizeWindow, bool RestoreWindow);

[ExcludeFromCodeCoverage]
internal static class SdlWindowStateInterop
{
    private const uint SdlWindowMaximized = 0x00000080;

    public static bool TryGetIsMaximized(IntPtr windowHandle, out bool isMaximized)
    {
        isMaximized = false;
        if (windowHandle == IntPtr.Zero)
        {
            return false;
        }

        try
        {
            isMaximized = (SdlGetWindowFlags(windowHandle) & SdlWindowMaximized) != 0;
            return true;
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
    }

    public static bool TryMaximize(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero)
        {
            return false;
        }

        try
        {
            SdlMaximizeWindow(windowHandle);
            return true;
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
    }

    public static bool TryRestore(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero)
        {
            return false;
        }

        try
        {
            SdlRestoreWindow(windowHandle);
            return true;
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
    }

    [DllImport("SDL2", EntryPoint = "SDL_GetWindowFlags", CallingConvention = CallingConvention.Cdecl)]
    private static extern uint SdlGetWindowFlags(IntPtr window);

    [DllImport("SDL2", EntryPoint = "SDL_MaximizeWindow", CallingConvention = CallingConvention.Cdecl)]
    private static extern void SdlMaximizeWindow(IntPtr window);

    [DllImport("SDL2", EntryPoint = "SDL_RestoreWindow", CallingConvention = CallingConvention.Cdecl)]
    private static extern void SdlRestoreWindow(IntPtr window);
}
