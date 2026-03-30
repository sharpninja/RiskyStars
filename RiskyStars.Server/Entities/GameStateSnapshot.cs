using System.Text.Json.Serialization;
using RiskyStars.Server.Services;

namespace RiskyStars.Server.Entities;

public class GameStateSnapshot
{
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    [JsonPropertyName("savedAt")]
    public DateTime SavedAt { get; set; }

    [JsonPropertyName("gameId")]
    public string GameId { get; set; } = string.Empty;

    [JsonPropertyName("turnNumber")]
    public int TurnNumber { get; set; }

    [JsonPropertyName("currentPhase")]
    public TurnPhase CurrentPhase { get; set; }

    [JsonPropertyName("currentPlayerIndex")]
    public int CurrentPlayerIndex { get; set; }

    [JsonPropertyName("players")]
    public List<Player> Players { get; set; } = new();

    [JsonPropertyName("starSystems")]
    public List<StarSystem> StarSystems { get; set; } = new();

    [JsonPropertyName("alliances")]
    public List<Alliance> Alliances { get; set; } = new();

    [JsonPropertyName("activeCombats")]
    public List<CombatSessionSnapshot> ActiveCombats { get; set; } = new();

    public static GameStateSnapshot FromGame(Game game, IEnumerable<CombatSessionSnapshot>? activeCombats = null)
    {
        return new GameStateSnapshot
        {
            Version = 1,
            SavedAt = DateTime.UtcNow,
            GameId = game.Id,
            TurnNumber = game.TurnNumber,
            CurrentPhase = game.CurrentPhase,
            CurrentPlayerIndex = game.CurrentPlayerIndex,
            Players = game.Players,
            StarSystems = game.StarSystems,
            Alliances = game.Alliances,
            ActiveCombats = activeCombats?.ToList() ?? new List<CombatSessionSnapshot>()
        };
    }

    public Game ToGame()
    {
        return new Game
        {
            Id = GameId,
            TurnNumber = TurnNumber,
            CurrentPhase = CurrentPhase,
            CurrentPlayerIndex = CurrentPlayerIndex,
            Players = Players,
            StarSystems = StarSystems,
            Alliances = Alliances
        };
    }
}

public class CombatSessionSnapshot
{
    [JsonPropertyName("locationId")]
    public string LocationId { get; set; } = string.Empty;

    [JsonPropertyName("attackingArmies")]
    public List<Army> AttackingArmies { get; set; } = new();

    [JsonPropertyName("defendingArmies")]
    public List<Army> DefendingArmies { get; set; } = new();

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("roundNumber")]
    public int RoundNumber { get; set; }

    [JsonPropertyName("reinforcementArrivals")]
    public List<ReinforcementArrivalSnapshot> ReinforcementArrivals { get; set; } = new();

    [JsonPropertyName("nextReinforcementOrder")]
    public int NextReinforcementOrder { get; set; }

    [JsonPropertyName("combatHistoryCount")]
    public int CombatHistoryCount { get; set; }

    public static CombatSessionSnapshot FromCombatSession(CombatSession session)
    {
        return new CombatSessionSnapshot
        {
            LocationId = session.LocationId,
            AttackingArmies = session.AttackingArmies.ToList(),
            DefendingArmies = session.DefendingArmies.ToList(),
            IsActive = session.IsActive,
            RoundNumber = session.RoundNumber,
            ReinforcementArrivals = session.ReinforcementArrivals
                .Select(ReinforcementArrivalSnapshot.FromReinforcementArrival)
                .ToList(),
            NextReinforcementOrder = session.NextReinforcementOrder,
            CombatHistoryCount = session.CombatHistory.Count
        };
    }

    public CombatSession ToCombatSession()
    {
        return new CombatSession
        {
            LocationId = LocationId,
            AttackingArmies = AttackingArmies,
            DefendingArmies = DefendingArmies,
            IsActive = IsActive,
            RoundNumber = RoundNumber,
            ReinforcementArrivals = ReinforcementArrivals
                .Select(r => r.ToReinforcementArrival())
                .ToList(),
            NextReinforcementOrder = NextReinforcementOrder
        };
    }
}

public class ReinforcementArrivalSnapshot
{
    [JsonPropertyName("armyId")]
    public string ArmyId { get; set; } = string.Empty;

    [JsonPropertyName("isAttacker")]
    public bool IsAttacker { get; set; }

    [JsonPropertyName("arrivalOrder")]
    public int ArrivalOrder { get; set; }

    public static ReinforcementArrivalSnapshot FromReinforcementArrival(ReinforcementArrival arrival)
    {
        return new ReinforcementArrivalSnapshot
        {
            ArmyId = arrival.Army.Id,
            IsAttacker = arrival.IsAttacker,
            ArrivalOrder = arrival.ArrivalOrder
        };
    }

    public ReinforcementArrival ToReinforcementArrival()
    {
        return new ReinforcementArrival
        {
            Army = null!,
            IsAttacker = IsAttacker,
            ArrivalOrder = ArrivalOrder
        };
    }
}
