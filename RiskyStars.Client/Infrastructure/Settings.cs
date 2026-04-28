using System.Text.Json;
using System.Text.Json.Serialization;
using RiskyStars.Client;

namespace RiskyStars.Client;

public class Settings
{
    public static readonly string[] SupportedResolutions =
    [
        "1280x720",
        "1366x768",
        "1600x900",
        "1920x1080",
        "2560x1440",
        "3840x2160"
    ];

    public static readonly string[] WindowModeOptions =
    [
        "Normal",
        "Maximized",
        "Full"
    ];

    public static List<string> GetResolutionOptions(string? currentResolution = null)
    {
        var options = SupportedResolutions.ToList();
        if (!string.IsNullOrWhiteSpace(currentResolution) &&
            !options.Contains(currentResolution, StringComparer.OrdinalIgnoreCase))
        {
            options.Insert(0, currentResolution);
        }

        return options;
    }

    public UiThemeSettings Theme { get; set; } = new();
    public string ServerAddress { get; set; } = "http://localhost:5000";
    public int ResolutionWidth { get; set; } = 1280;
    public int ResolutionHeight { get; set; } = 720;
    public int UiScalePercent { get; set; } = 100;
    [JsonConverter(typeof(JsonStringEnumConverter<GameWindowMode>))]
    public GameWindowMode WindowMode { get; set; } = GameWindowMode.Normal;
    public bool Fullscreen { get; set; } = false;
    public bool VSync { get; set; } = true;
    public int TargetFrameRate { get; set; } = 60;
    
    public float MasterVolume { get; set; } = 1.0f;
    public float MusicVolume { get; set; } = 0.7f;
    public float SfxVolume { get; set; } = 0.8f;
    
    public float CameraPanSpeed { get; set; } = 5.0f;
    public float CameraZoomSpeed { get; set; } = 0.1f;
    public bool InvertCameraZoom { get; set; } = false;
    
    public bool ShowDebugInfo { get; set; } = false;
    public bool ShowFPS { get; set; } = true;

    private static readonly string SettingsPath = "settings.json";

    public static Settings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                var settings = JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
                settings.Normalize();
                return settings;
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Failed to load settings: {ex.Message}");
        }
        return new Settings();
    }

    public void Save()
    {
        try
        {
            Normalize();
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }
    
    public Settings Clone()
    {
        return new Settings
        {
            Theme = Theme.Clone(),
            ServerAddress = ServerAddress,
            ResolutionWidth = ResolutionWidth,
            ResolutionHeight = ResolutionHeight,
            UiScalePercent = UiScalePercent,
            WindowMode = WindowMode,
            Fullscreen = Fullscreen,
            VSync = VSync,
            TargetFrameRate = TargetFrameRate,
            MasterVolume = MasterVolume,
            MusicVolume = MusicVolume,
            SfxVolume = SfxVolume,
            CameraPanSpeed = CameraPanSpeed,
            CameraZoomSpeed = CameraZoomSpeed,
            InvertCameraZoom = InvertCameraZoom,
            ShowDebugInfo = ShowDebugInfo,
            ShowFPS = ShowFPS
        };
    }

    public void Normalize()
    {
        Theme ??= new UiThemeSettings();
        Theme.Normalize();

        if (string.IsNullOrWhiteSpace(ServerAddress))
        {
            ServerAddress = "http://localhost:5000";
        }

        if (!Enum.IsDefined(WindowMode))
        {
            WindowMode = Fullscreen ? GameWindowMode.Full : GameWindowMode.Normal;
        }

        if (Fullscreen && WindowMode == GameWindowMode.Normal)
        {
            WindowMode = GameWindowMode.Full;
        }

        Fullscreen = WindowMode == GameWindowMode.Full;
        UiScalePercent = Math.Clamp(UiScalePercent <= 0 ? 100 : UiScalePercent, 80, 160);
        ResolutionWidth = Math.Max(800, ResolutionWidth);
        ResolutionHeight = Math.Max(600, ResolutionHeight);
    }

    public static GameWindowMode ParseWindowModeOption(string? option)
    {
        return option?.Trim() switch
        {
            "Normal" or "Windowed" => GameWindowMode.Normal,
            "Maximized" => GameWindowMode.Maximized,
            "Full" or "Fullscreen" => GameWindowMode.Full,
            _ => GameWindowMode.Normal
        };
    }

    public static string GetWindowModeOption(GameWindowMode mode)
    {
        return mode switch
        {
            GameWindowMode.Maximized => "Maximized",
            GameWindowMode.Full => "Full",
            _ => "Normal"
        };
    }

    public static int GetWindowModeOptionIndex(GameWindowMode mode)
    {
        var option = GetWindowModeOption(mode);
        var index = Array.IndexOf(WindowModeOptions, option);
        return index >= 0 ? index : 0;
    }
}

public enum GameWindowMode
{
    Normal,
    Maximized,
    Full
}

public class UiThemeSettings
{
    public static readonly string[] AccentColorOptions =
    [
        "Classic Green",
        "Ice Cyan",
        "Amber Gold",
        "Signal Red"
    ];

    public static readonly string[] WarningColorOptions =
    [
        "Amber",
        "Ivory",
        "Crimson",
        "Cyan"
    ];

    public static readonly string[] FontStyleOptions =
    [
        "Compact",
        "Standard",
        "Command"
    ];

    public string AccentColor { get; set; } = "Classic Green";
    public string WarningColor { get; set; } = "Amber";
    public string FontStyle { get; set; } = "Standard";
    public int FontScalePercent { get; set; } = 100;
    public int PaddingScalePercent { get; set; } = 100;
    public int FramePaddingPercent { get; set; } = 100;
    public int ContrastPercent { get; set; } = 100;

    public UiThemeSettings Clone()
    {
        return new UiThemeSettings
        {
            AccentColor = AccentColor,
            WarningColor = WarningColor,
            FontStyle = FontStyle,
            FontScalePercent = FontScalePercent,
            PaddingScalePercent = PaddingScalePercent,
            FramePaddingPercent = FramePaddingPercent,
            ContrastPercent = ContrastPercent
        };
    }

    public void Normalize()
    {
        if (!AccentColorOptions.Contains(AccentColor))
        {
            AccentColor = AccentColorOptions[0];
        }

        if (!WarningColorOptions.Contains(WarningColor))
        {
            WarningColor = WarningColorOptions[0];
        }

        if (!FontStyleOptions.Contains(FontStyle))
        {
            FontStyle = FontStyleOptions[1];
        }

        FontScalePercent = Math.Clamp(FontScalePercent, 80, 140);
        PaddingScalePercent = Math.Clamp(PaddingScalePercent, 80, 150);
        FramePaddingPercent = Math.Clamp(FramePaddingPercent, 70, 140);
        ContrastPercent = Math.Clamp(ContrastPercent, 85, 140);
    }
}

