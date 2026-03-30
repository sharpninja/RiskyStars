using System.Text.Json.Serialization;

namespace RiskyStars.Server.Entities;

public class StarSystem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public StarSystemType Type { get; set; }

    [JsonPropertyName("stellarBodies")]
    public List<StellarBody> StellarBodies { get; set; } = new();

    [JsonPropertyName("hyperspaceLanes")]
    public List<HyperspaceLane> HyperspaceLanes { get; set; } = new();

    public int GetTotalRegionCount()
    {
        return StellarBodies.Sum(body => body.GetRegionCount());
    }
}

public enum StarSystemType
{
    Home,
    Featured,
    Minor
}
