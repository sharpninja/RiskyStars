using System.Text.Json.Serialization;

namespace RiskyStars.Server.Entities;

public class Player
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("allianceId")]
    public string? AllianceId { get; set; }

    [JsonPropertyName("turnsSinceLeftAlliance")]
    public int TurnsSinceLeftAlliance { get; set; }

    [JsonPropertyName("homeStarSystemId")]
    public string HomeStarSystemId { get; set; } = string.Empty;

    [JsonPropertyName("populationStockpile")]
    public int PopulationStockpile { get; set; }

    [JsonPropertyName("metalStockpile")]
    public int MetalStockpile { get; set; }

    [JsonPropertyName("fuelStockpile")]
    public int FuelStockpile { get; set; }

    [JsonPropertyName("ownedRegionIds")]
    public List<string> OwnedRegionIds { get; set; } = new();

    [JsonPropertyName("ownedHyperspaceLaneMouthIds")]
    public List<string> OwnedHyperspaceLaneMouthIds { get; set; } = new();

    [JsonPropertyName("heroes")]
    public List<Hero> Heroes { get; set; } = new();

    public bool IsAlliedWith(Player other)
    {
        return AllianceId != null && AllianceId == other.AllianceId;
    }

    public bool CanAttack(Player other)
    {
        if (IsAlliedWith(other))
        {
            return false;
        }

        if (TurnsSinceLeftAlliance > 0 && TurnsSinceLeftAlliance <= 3)
        {
            return false;
        }

        return true;
    }

    public void ProduceResources(IEnumerable<StellarBody> ownedBodies)
    {
        foreach (var body in ownedBodies)
        {
            var production = body.CalculateTotalProduction();
            switch (body.ResourceType)
            {
                case ResourceType.Population:
                    PopulationStockpile += production;
                    break;
                case ResourceType.Metal:
                    MetalStockpile += production;
                    break;
                case ResourceType.Fuel:
                    FuelStockpile += production;
                    break;
            }
        }
    }

    public bool CanPurchaseArmy(int count = 1)
    {
        return PopulationStockpile >= 10 * count &&
               MetalStockpile >= 3 * count &&
               FuelStockpile >= 3 * count;
    }

    public void PurchaseArmy(int count = 1)
    {
        if (!CanPurchaseArmy(count))
        {
            throw new InvalidOperationException("Insufficient resources to purchase army");
        }

        PopulationStockpile -= 10 * count;
        MetalStockpile -= 3 * count;
        FuelStockpile -= 3 * count;
    }
}
