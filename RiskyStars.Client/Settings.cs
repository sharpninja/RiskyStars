using System.Text.Json;

namespace RiskyStars.Client;

public class Settings
{
    public string ServerAddress { get; set; } = "http://localhost:5000";
    public int ResolutionWidth { get; set; } = 1280;
    public int ResolutionHeight { get; set; } = 720;
    public bool Fullscreen { get; set; } = false;

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
}
