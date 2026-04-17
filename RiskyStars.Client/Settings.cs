using System.Text.Json;
using RiskyStars.Client;

namespace RiskyStars.Client;

public class Settings
{
    public UiThemeSettings Theme { get; set; } = new();
    public string ServerAddress { get; set; } = "http://localhost:5000";
    public int ResolutionWidth { get; set; } = 1280;
    public int ResolutionHeight { get; set; } = 720;
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
                settings.Theme ??= new UiThemeSettings();
                settings.Theme.Normalize();
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

