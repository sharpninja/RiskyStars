using System.Text.Json;
using RiskyStars.Client;

namespace RiskyStars.Client;

public class Settings
{
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
                return JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
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

