namespace RiskyStars.Server.Entities;

public class Game
{
    public string Id { get; set; } = string.Empty;
    public List<Player> Players { get; set; } = new();
    public List<StarSystem> StarSystems { get; set; } = new();
    public List<Alliance> Alliances { get; set; } = new();
    public int TurnNumber { get; set; } = 1;
    public TurnPhase CurrentPhase { get; set; } = TurnPhase.Production;
    public int CurrentPlayerIndex { get; set; } = 0;
    
    public Player CurrentPlayer => Players[CurrentPlayerIndex];

    public IEnumerable<Region> GetAllRegions()
    {
        return StarSystems
            .SelectMany(system => system.StellarBodies)
            .SelectMany(body => body.Regions);
    }

    public IEnumerable<Army> GetAllArmies()
    {
        var regionArmies = GetAllRegions()
            .Where(r => r.Army != null)
            .Select(r => r.Army!);

        var hyperspaceLaneArmies = StarSystems
            .SelectMany(system => system.HyperspaceLanes)
            .SelectMany(lane => new[] { lane.MouthAArmy, lane.MouthBArmy })
            .Where(army => army != null)
            .Select(army => army!);

        return regionArmies.Concat(hyperspaceLaneArmies);
    }

    public IEnumerable<HyperspaceLane> GetAllHyperspaceLanes()
    {
        return StarSystems.SelectMany(system => system.HyperspaceLanes);
    }

    public IEnumerable<StellarBody> GetPlayerOwnedBodies(string playerId)
    {
        return StarSystems
            .SelectMany(system => system.StellarBodies)
            .Where(body => body.Regions.Any(r => r.OwnerId == playerId));
    }
}

public enum TurnPhase
{
    Production,
    Purchase,
    Reinforcement,
    Movement
}
