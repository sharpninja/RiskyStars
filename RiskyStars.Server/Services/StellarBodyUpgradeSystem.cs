using RiskyStars.Server.Entities;

namespace RiskyStars.Server.Services;

public class StellarBodyUpgradeSystem
{
    private readonly ResourceManager _resourceManager;

    public StellarBodyUpgradeSystem(ResourceManager resourceManager)
    {
        _resourceManager = resourceManager;
    }

    public bool CanUpgrade(StellarBody stellarBody, Player player, int targetLevel)
    {
        if (targetLevel < 1 || targetLevel > 3)
            return false;

        if (targetLevel <= stellarBody.UpgradeLevel)
            return false;

        if (!IsOwner(stellarBody, player))
            return false;

        var cost = GetUpgradeCost(stellarBody, targetLevel);
        return HasSufficientResources(player, stellarBody, cost);
    }

    public void ApplyUpgrade(StellarBody stellarBody, Player player, int targetLevel)
    {
        if (!CanUpgrade(stellarBody, player, targetLevel))
            throw new InvalidOperationException("Cannot upgrade stellar body");

        var cost = GetUpgradeCost(stellarBody, targetLevel);
        DeductUpgradeCost(player, stellarBody, cost);
        stellarBody.UpgradeLevel = targetLevel;
    }

    public (int population, int metal, int fuel) GetUpgradeCost(StellarBody stellarBody, int targetLevel)
    {
        int baseCost = stellarBody.GetUpgradeCost(targetLevel);

        return stellarBody.ResourceType switch
        {
            ResourceType.Population => (0, baseCost, baseCost),
            ResourceType.Metal => (baseCost, 0, baseCost),
            ResourceType.Fuel => (baseCost, baseCost, 0),
            _ => throw new InvalidOperationException($"Unknown resource type: {stellarBody.ResourceType}")
        };
    }

    public void RemoveUpgradesOnOwnershipChange(StellarBody stellarBody)
    {
        stellarBody.RemoveUpgrades();
    }

    private bool IsOwner(StellarBody stellarBody, Player player)
    {
        return stellarBody.Regions.Any(r => r.OwnerId == player.Id);
    }

    private bool HasSufficientResources(Player player, StellarBody stellarBody, (int population, int metal, int fuel) cost)
    {
        return _resourceManager.HasSufficientResources(player, cost.population, cost.metal, cost.fuel);
    }

    private void DeductUpgradeCost(Player player, StellarBody stellarBody, (int population, int metal, int fuel) cost)
    {
        _resourceManager.SubtractResources(player, cost.population, cost.metal, cost.fuel);
    }
}
