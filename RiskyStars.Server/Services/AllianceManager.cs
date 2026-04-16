using RiskyStars.Server.Entities;

namespace RiskyStars.Server.Services;

public class AllianceManager
{
    public Alliance CreateAlliance(string id, string name, int currentTurn)
    {
        return new Alliance
        {
            Id = id,
            Name = name,
            CreatedTurn = currentTurn,
            MemberPlayerIds = new()
        };
    }

    public void AddPlayerToAlliance(Alliance alliance, Player player, int currentTurn)
    {
        if (player.AllianceId != null)
        {
            throw new InvalidOperationException("Player is already in an alliance");
        }

        if (!alliance.MemberPlayerIds.Contains(player.Id))
        {
            alliance.MemberPlayerIds.Add(player.Id);
        }

        player.AllianceId = alliance.Id;
        player.TurnsSinceLeftAlliance = 0;
    }

    public void RemovePlayerFromAlliance(Alliance alliance, Player player)
    {
        if (player.AllianceId != alliance.Id)
        {
            throw new InvalidOperationException("Player is not in this alliance");
        }

        alliance.MemberPlayerIds.Remove(player.Id);
        player.AllianceId = null;
        player.TurnsSinceLeftAlliance = 0;
    }

    public bool CanPlayerAttackTarget(Player attacker, Player target)
    {
        if (attacker.IsAlliedWith(target))
        {
            return false;
        }

        if (attacker.TurnsSinceLeftAlliance > 0 && attacker.TurnsSinceLeftAlliance <= 3)
        {
            return false;
        }

        return true;
    }

    public void UpdateTurnsSinceLeftAlliance(Player player)
    {
        if (player.AllianceId == null && player.TurnsSinceLeftAlliance > 0 && player.TurnsSinceLeftAlliance <= 3)
        {
            player.TurnsSinceLeftAlliance++;
        }
    }

    public (int totalPopulation, int totalMetal, int totalFuel) CalculateAlliancePooledResources(
        Alliance alliance, 
        IEnumerable<Player> allPlayers)
    {
        int totalPopulation = 0;
        int totalMetal = 0;
        int totalFuel = 0;

        foreach (var playerId in alliance.MemberPlayerIds)
        {
            var player = allPlayers.FirstOrDefault(p => p.Id == playerId);
            if (player != null)
            {
                totalPopulation += player.PopulationStockpile;
                totalMetal += player.MetalStockpile;
                totalFuel += player.FuelStockpile;
            }
        }

        return (totalPopulation, totalMetal, totalFuel);
    }

    public int CalculateMaxArmiesFromPooledResources(int totalPopulation, int totalMetal, int totalFuel)
    {
        const int populationCostPerArmy = 10;
        const int metalCostPerArmy = 3;
        const int fuelCostPerArmy = 3;

        int maxFromPopulation = totalPopulation / populationCostPerArmy;
        int maxFromMetal = totalMetal / metalCostPerArmy;
        int maxFromFuel = totalFuel / fuelCostPerArmy;

        return Math.Min(Math.Min(maxFromPopulation, maxFromMetal), maxFromFuel);
    }

    public Dictionary<string, int> CalculateAllianceArmyAllocation(
        Alliance alliance,
        IEnumerable<Player> allPlayers,
        int maxArmies)
    {
        var allocation = new Dictionary<string, int>();
        var playerRegionCounts = new Dictionary<string, int>();
        int totalRegions = 0;

        foreach (var playerId in alliance.MemberPlayerIds)
        {
            var player = allPlayers.FirstOrDefault(p => p.Id == playerId);
            if (player != null)
            {
                int regionCount = player.OwnedRegionIds.Count;
                playerRegionCounts[playerId] = regionCount;
                totalRegions += regionCount;
            }
        }

        if (totalRegions == 0)
        {
            foreach (var playerId in alliance.MemberPlayerIds)
            {
                allocation[playerId] = 0;
            }
            return allocation;
        }

        int armiesAllocated = 0;
        var remainders = new Dictionary<string, double>();

        foreach (var playerId in alliance.MemberPlayerIds)
        {
            if (playerRegionCounts.TryGetValue(playerId, out int regionCount))
            {
                double percentage = (double)regionCount / totalRegions;
                double exactAllocation = maxArmies * percentage;
                int allocatedArmies = (int)Math.Floor(exactAllocation);
                
                allocation[playerId] = allocatedArmies;
                armiesAllocated += allocatedArmies;
                remainders[playerId] = exactAllocation - allocatedArmies;
            }
        }

        int remainingArmies = maxArmies - armiesAllocated;
        var sortedByRemainder = remainders.OrderByDescending(kvp => kvp.Value).ToList();

        for (int i = 0; i < remainingArmies && i < sortedByRemainder.Count; i++)
        {
            allocation[sortedByRemainder[i].Key]++;
        }

        return allocation;
    }

    public void DeductAllianceResourcesForArmies(
        Alliance alliance,
        IEnumerable<Player> allPlayers,
        int armyCount)
    {
        const int populationCostPerArmy = 10;
        const int metalCostPerArmy = 3;
        const int fuelCostPerArmy = 3;

        int totalPopulationNeeded = populationCostPerArmy * armyCount;
        int totalMetalNeeded = metalCostPerArmy * armyCount;
        int totalFuelNeeded = fuelCostPerArmy * armyCount;

        var alliancePlayers = allPlayers.Where(p => alliance.MemberPlayerIds.Contains(p.Id)).ToList();
        
        int totalPopulation = alliancePlayers.Sum(p => p.PopulationStockpile);
        int totalMetal = alliancePlayers.Sum(p => p.MetalStockpile);
        int totalFuel = alliancePlayers.Sum(p => p.FuelStockpile);

        if (totalPopulation < totalPopulationNeeded || 
            totalMetal < totalMetalNeeded || 
            totalFuel < totalFuelNeeded)
        {
            throw new InvalidOperationException("Insufficient pooled resources for army production");
        }

        DeductResourceProportionally(alliancePlayers, totalPopulationNeeded, 
            p => p.PopulationStockpile, (p, v) => p.PopulationStockpile = v);
        DeductResourceProportionally(alliancePlayers, totalMetalNeeded, 
            p => p.MetalStockpile, (p, v) => p.MetalStockpile = v);
        DeductResourceProportionally(alliancePlayers, totalFuelNeeded, 
            p => p.FuelStockpile, (p, v) => p.FuelStockpile = v);
    }

    private void DeductResourceProportionally(
        List<Player> players,
        int totalNeeded,
        Func<Player, int> getStockpile,
        Action<Player, int> setStockpile)
    {
        int totalAvailable = players.Sum(getStockpile);
        if (totalAvailable == 0)
        {
            return;
        }

        int remaining = totalNeeded;

        foreach (var player in players.OrderByDescending(getStockpile))
        {
            if (remaining <= 0)
            {
                break;
            }

            int playerStockpile = getStockpile(player);
            double proportion = (double)playerStockpile / totalAvailable;
            int deduction = Math.Min((int)Math.Ceiling(totalNeeded * proportion), playerStockpile);
            deduction = Math.Min(deduction, remaining);

            setStockpile(player, playerStockpile - deduction);
            remaining -= deduction;
        }

        if (remaining > 0)
        {
            foreach (var player in players.OrderByDescending(getStockpile))
            {
                if (remaining <= 0)
                {
                    break;
                }

                int playerStockpile = getStockpile(player);
                int deduction = Math.Min(remaining, playerStockpile);
                setStockpile(player, playerStockpile - deduction);
                remaining -= deduction;
            }
        }
    }

    public List<Player> GetAllianceMembers(Alliance alliance, IEnumerable<Player> allPlayers)
    {
        return allPlayers.Where(p => alliance.MemberPlayerIds.Contains(p.Id)).ToList();
    }

    public bool IsPlayerInAlliance(Player player, Alliance alliance)
    {
        return player.AllianceId == alliance.Id && alliance.MemberPlayerIds.Contains(player.Id);
    }

    public Alliance? GetPlayerAlliance(Player player, IEnumerable<Alliance> alliances)
    {
        if (player.AllianceId == null)
        {
            return null;
        }

        return alliances.FirstOrDefault(a => a.Id == player.AllianceId);
    }

    public bool CanDisbandAlliance(Alliance alliance)
    {
        return alliance.MemberPlayerIds.Count == 0;
    }

    public void ValidateAttack(Player attacker, Player target)
    {
        if (!CanPlayerAttackTarget(attacker, target))
        {
            if (attacker.IsAlliedWith(target))
            {
                throw new InvalidOperationException("Cannot attack allied players");
            }
            else if (attacker.TurnsSinceLeftAlliance > 0 && attacker.TurnsSinceLeftAlliance <= 3)
            {
                throw new InvalidOperationException(
                    $"Cannot attack for {4 - attacker.TurnsSinceLeftAlliance} more turn(s) after leaving alliance");
            }
        }
    }
}
