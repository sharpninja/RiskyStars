using System.Text.Json;
using Microsoft.Xna.Framework;

namespace RiskyStars.Client;

public class WindowPreferences
{
    public Dictionary<string, WindowState> Windows { get; set; } = new();
    public int LeftPanelWidth { get; set; } = 0;
    public int RightPanelWidth { get; set; } = 0;
    public bool LeftPanelCollapsed { get; set; }
    public bool RightPanelCollapsed { get; set; }

    private static readonly string PreferencesPath = "window_preferences.json";

    public static WindowPreferences Load()
    {
        try
        {
            if (File.Exists(PreferencesPath))
            {
                var json = File.ReadAllText(PreferencesPath);
                return JsonSerializer.Deserialize<WindowPreferences>(json) ?? new WindowPreferences();
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Failed to load window preferences: {ex.Message}");
        }
        return new WindowPreferences();
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(PreferencesPath, json);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Failed to save window preferences: {ex.Message}");
        }
    }

    public WindowState? GetWindowState(string windowId)
    {
        return Windows.TryGetValue(windowId, out var state) ? state : null;
    }

    public void SetWindowState(string windowId, WindowState state)
    {
        Windows[windowId] = state;
    }
}

public class WindowState
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public bool IsVisible { get; set; } = true;
    public DockPosition DockPosition { get; set; } = DockPosition.None;
}

public enum DockPosition
{
    None,
    Left,
    Right,
    Top,
    Bottom,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
}
