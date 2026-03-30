using System.Text.Json.Serialization;

namespace RiskyStars.Server.Entities;

public class StellarBody
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public StellarBodyType Type { get; set; }

    [JsonPropertyName("surfaceType")]
    public RockyPlanetSurfaceType? SurfaceType { get; set; }

    [JsonPropertyName("continentCount")]
    public int? ContinentCount { get; set; }

    [JsonPropertyName("starSystemId")]
    public string StarSystemId { get; set; } = string.Empty;

    [JsonPropertyName("upgradeLevel")]
    public int UpgradeLevel { get; set; }

    [JsonPropertyName("regions")]
    public List<Region> Regions { get; set; } = new();

    [JsonPropertyName("heroes")]
    public List<Hero> Heroes { get; set; } = new();

    [JsonIgnore]
    public ResourceType ResourceType => Type switch
    {
        StellarBodyType.GasGiant => ResourceType.Fuel,
        StellarBodyType.RockyPlanet => ResourceType.Population,
        StellarBodyType.Planetoid => ResourceType.Metal,
        StellarBodyType.Comet => ResourceType.Fuel,
        _ => throw new InvalidOperationException($"Unknown stellar body type: {Type}")
    };

    public int GetRegionCount()
    {
        if (Type == StellarBodyType.GasGiant || Type == StellarBodyType.Comet || Type == StellarBodyType.Planetoid)
            return 1;

        if (Type == StellarBodyType.RockyPlanet && SurfaceType.HasValue)
        {
            return SurfaceType.Value switch
            {
                RockyPlanetSurfaceType.Barren => 2,
                RockyPlanetSurfaceType.Ocean => 1,
                RockyPlanetSurfaceType.Gaia => ContinentCount ?? 0,
                _ => throw new InvalidOperationException($"Unknown surface type: {SurfaceType}")
            };
        }

        return 0;
    }

    public int CalculateBaseProduction()
    {
        return GetRegionCount() * Region.BaseProductionPerRegion;
    }

    public double GetYieldMultiplier()
    {
        return UpgradeLevel switch
        {
            0 => 1.0,
            1 => 1.1,
            2 => 1.5,
            3 => 2.0,
            _ => 1.0
        };
    }

    public int GetUpgradeCost(int targetLevel)
    {
        return targetLevel switch
        {
            1 => 500,
            2 => 2500,
            3 => 5000,
            _ => 0
        };
    }

    public int CalculateTotalProduction()
    {
        int baseProduction = CalculateBaseProduction();
        double yieldMultiplier = GetYieldMultiplier();
        
        double afterUpgrade = baseProduction * yieldMultiplier;

        foreach (var hero in Heroes)
        {
            afterUpgrade = hero.ApplyYieldModifier(afterUpgrade);
        }

        int total = (int)Math.Round(afterUpgrade);

        foreach (var hero in Heroes)
        {
            total += hero.GetFixedResourceBonus();
        }

        return total;
    }

    public bool CanAssignHero(Hero hero)
    {
        if (Heroes.Count >= 3)
            return false;

        if (Heroes.Count >= GetRegionCount())
            return false;

        return true;
    }

    public void RemoveHeroes()
    {
        Heroes.Clear();
    }

    public void RemoveUpgrades()
    {
        UpgradeLevel = 0;
    }
}
