using System.Text.Json.Serialization;

namespace RiskyStars.Server.Entities;

public class Hero
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("class")]
    public HeroClass Class { get; set; }

    [JsonPropertyName("fixedResourceAmount")]
    public int FixedResourceAmount { get; set; }

    [JsonPropertyName("assignedStellarBodyId")]
    public string? AssignedStellarBodyId { get; set; }

    public double ApplyYieldModifier(double currentYield)
    {
        if (Class == HeroClass.ClassII || Class == HeroClass.ClassIII)
        {
            return currentYield * 1.25;
        }
        return currentYield;
    }

    public int GetFixedResourceBonus()
    {
        if (Class == HeroClass.ClassI || Class == HeroClass.ClassIII)
        {
            return FixedResourceAmount;
        }
        return 0;
    }
}
