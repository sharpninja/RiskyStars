using System.Text.Json.Serialization;

namespace RiskyStars.Server.Entities;

public class Alliance
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("memberPlayerIds")]
    public List<string> MemberPlayerIds { get; set; } = new();

    [JsonPropertyName("createdTurn")]
    public int CreatedTurn { get; set; }
}
