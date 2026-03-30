using System.Text.Json.Serialization;

namespace RiskyStars.Server.Entities;

public class HyperspaceLane
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("starSystemAId")]
    public string StarSystemAId { get; set; } = string.Empty;

    [JsonPropertyName("starSystemBId")]
    public string StarSystemBId { get; set; } = string.Empty;

    [JsonPropertyName("mouthAId")]
    public string MouthAId { get; set; } = string.Empty;

    [JsonPropertyName("mouthBId")]
    public string MouthBId { get; set; } = string.Empty;

    [JsonPropertyName("mouthAOwnerId")]
    public string? MouthAOwnerId { get; set; }

    [JsonPropertyName("mouthBOwnerId")]
    public string? MouthBOwnerId { get; set; }

    [JsonPropertyName("mouthAArmy")]
    public Army? MouthAArmy { get; set; }

    [JsonPropertyName("mouthBArmy")]
    public Army? MouthBArmy { get; set; }

    public string GetOppositeMouthId(string mouthId)
    {
        if (mouthId == MouthAId)
            return MouthBId;
        if (mouthId == MouthBId)
            return MouthAId;
        throw new ArgumentException($"Mouth ID {mouthId} does not belong to this hyperspace lane");
    }

    public string GetOppositeStarSystemId(string starSystemId)
    {
        if (starSystemId == StarSystemAId)
            return StarSystemBId;
        if (starSystemId == StarSystemBId)
            return StarSystemAId;
        throw new ArgumentException($"Star system ID {starSystemId} does not belong to this hyperspace lane");
    }
}
