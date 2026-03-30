using System.Text.Json.Serialization;

namespace RiskyStars.Server.Entities;

public class Region
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("stellarBodyId")]
    public string StellarBodyId { get; set; } = string.Empty;

    [JsonPropertyName("ownerId")]
    public string? OwnerId { get; set; }

    [JsonPropertyName("army")]
    public Army? Army { get; set; }

    public const int BaseProductionPerRegion = 100;
}
