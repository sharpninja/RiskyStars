using System.Text.Json.Serialization;

namespace RiskyStars.Server.Entities;

public class Army
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("ownerId")]
    public string OwnerId { get; set; } = string.Empty;

    [JsonPropertyName("unitCount")]
    public int UnitCount { get; set; }

    [JsonPropertyName("locationId")]
    public string LocationId { get; set; } = string.Empty;

    [JsonPropertyName("locationType")]
    public LocationType LocationType { get; set; }

    [JsonPropertyName("launchLocationId")]
    public string? LaunchLocationId { get; set; }

    [JsonPropertyName("hasMovedThisTurn")]
    public bool HasMovedThisTurn { get; set; }

    [JsonPropertyName("isInCombat")]
    public bool IsInCombat { get; set; }

    [JsonPropertyName("combatRole")]
    public CombatRole? CombatRole { get; set; }

    public void Move(string newLocationId, LocationType newLocationType)
    {
        if (HasMovedThisTurn)
            throw new InvalidOperationException("Army has already moved this turn");

        LaunchLocationId = LocationId;
        LocationId = newLocationId;
        LocationType = newLocationType;
        HasMovedThisTurn = true;
    }

    public void ReturnToLaunchLocation()
    {
        if (LaunchLocationId == null)
            throw new InvalidOperationException("No launch location to return to");

        LocationId = LaunchLocationId;
        LaunchLocationId = null;
    }

    public void ResetTurn()
    {
        HasMovedThisTurn = false;
        LaunchLocationId = null;
    }

    public void JoinArmy(Army other)
    {
        if (OwnerId != other.OwnerId)
            throw new InvalidOperationException("Cannot join armies of different owners");

        UnitCount += other.UnitCount;
    }
}

public enum LocationType
{
    Region,
    HyperspaceLaneMouth
}

public enum CombatRole
{
    Attacker,
    Defender,
    AttackingReinforcement,
    DefendingReinforcement
}
