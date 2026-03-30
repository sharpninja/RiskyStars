using RiskyStars.Server.Entities;

namespace RiskyStars.Server.Services;

public class ResourceManager
{
    public void AddResources(Player player, int population, int metal, int fuel)
    {
        player.PopulationStockpile += population;
        player.MetalStockpile += metal;
        player.FuelStockpile += fuel;
    }

    public void SubtractResources(Player player, int population, int metal, int fuel)
    {
        player.PopulationStockpile -= population;
        player.MetalStockpile -= metal;
        player.FuelStockpile -= fuel;
    }

    public bool HasSufficientResources(Player player, int population, int metal, int fuel)
    {
        return player.PopulationStockpile >= population &&
               player.MetalStockpile >= metal &&
               player.FuelStockpile >= fuel;
    }

    public int CalculateRegionProduction(Region region, StellarBody stellarBody, IEnumerable<Hero> heroesOnBody)
    {
        int baseProduction = Region.BaseProductionPerRegion;
        double yieldMultiplier = stellarBody.GetYieldMultiplier();
        
        double afterUpgrade = baseProduction * yieldMultiplier;

        foreach (var hero in heroesOnBody)
        {
            afterUpgrade = hero.ApplyYieldModifier(afterUpgrade);
        }

        int total = (int)Math.Round(afterUpgrade);

        foreach (var hero in heroesOnBody)
        {
            total += hero.GetFixedResourceBonus();
        }

        return total;
    }

    public void ProduceResourcesForPlayer(Player player, IEnumerable<Region> ownedRegions, IEnumerable<StellarBody> stellarBodies)
    {
        var stellarBodyDict = stellarBodies.ToDictionary(sb => sb.Id);

        foreach (var region in ownedRegions)
        {
            if (!stellarBodyDict.TryGetValue(region.StellarBodyId, out var stellarBody))
                continue;

            var heroesOnBody = stellarBody.Heroes;
            int production = CalculateRegionProduction(region, stellarBody, heroesOnBody);

            switch (stellarBody.ResourceType)
            {
                case ResourceType.Population:
                    player.PopulationStockpile += production;
                    break;
                case ResourceType.Metal:
                    player.MetalStockpile += production;
                    break;
                case ResourceType.Fuel:
                    player.FuelStockpile += production;
                    break;
            }
        }
    }

    public bool CanPurchaseArmies(Player player, int count)
    {
        const int populationCostPerArmy = 10;
        const int metalCostPerArmy = 3;
        const int fuelCostPerArmy = 3;

        return HasSufficientResources(
            player,
            populationCostPerArmy * count,
            metalCostPerArmy * count,
            fuelCostPerArmy * count
        );
    }

    public void PurchaseArmies(Player player, int count)
    {
        const int populationCostPerArmy = 10;
        const int metalCostPerArmy = 3;
        const int fuelCostPerArmy = 3;

        if (!CanPurchaseArmies(player, count))
            throw new InvalidOperationException("Insufficient resources to purchase armies");

        SubtractResources(
            player,
            populationCostPerArmy * count,
            metalCostPerArmy * count,
            fuelCostPerArmy * count
        );
    }

    public (int population, int metal, int fuel) GetArmyCost(int count)
    {
        const int populationCostPerArmy = 10;
        const int metalCostPerArmy = 3;
        const int fuelCostPerArmy = 3;

        return (
            populationCostPerArmy * count,
            metalCostPerArmy * count,
            fuelCostPerArmy * count
        );
    }

    public (int population, int metal, int fuel) GetPlayerStockpiles(Player player)
    {
        return (player.PopulationStockpile, player.MetalStockpile, player.FuelStockpile);
    }

    public void SetPlayerStockpiles(Player player, int population, int metal, int fuel)
    {
        player.PopulationStockpile = population;
        player.MetalStockpile = metal;
        player.FuelStockpile = fuel;
    }
}
