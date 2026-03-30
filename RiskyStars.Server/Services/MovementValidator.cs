using RiskyStars.Server.Entities;

namespace RiskyStars.Server.Services;

public class MovementValidator
{
    private readonly Game _game;

    public MovementValidator(Game game)
    {
        _game = game;
    }

    public MovementValidationResult ValidateMovement(
        Army army,
        string destinationId,
        LocationType destinationType,
        int unitCount)
    {
        if (army.HasMovedThisTurn)
        {
            return MovementValidationResult.Failure("Army has already moved this turn");
        }

        if (army.IsInCombat)
        {
            return MovementValidationResult.Failure("Army is currently in combat and cannot move");
        }

        if (unitCount <= 0)
        {
            return MovementValidationResult.Failure("Must move at least 1 unit");
        }

        if (unitCount > army.UnitCount)
        {
            return MovementValidationResult.Failure($"Cannot move {unitCount} units; army only has {army.UnitCount} units");
        }

        var currentLocation = GetLocationInfo(army.LocationId, army.LocationType);
        if (currentLocation == null)
        {
            return MovementValidationResult.Failure("Current location not found");
        }

        var currentPlayer = _game.Players.FirstOrDefault(p => p.Id == army.OwnerId);
        if (currentPlayer == null)
        {
            return MovementValidationResult.Failure("Army owner not found");
        }

        var unitsRemaining = army.UnitCount - unitCount;
        var currentPlayerOwnsLocation = DoesPlayerOwnLocation(currentPlayer.Id, army.LocationId, army.LocationType);

        if (currentPlayerOwnsLocation && unitsRemaining < 1)
        {
            return MovementValidationResult.Failure(
                "Cannot abandon owned location; must leave at least 1 unit behind to maintain possession");
        }

        if (!currentPlayerOwnsLocation && unitsRemaining > 0)
        {
            return MovementValidationResult.Failure(
                "When leaving a location not owned by the player, the entire army must move together");
        }

        var destinationInfo = GetLocationInfo(destinationId, destinationType);
        if (destinationInfo == null)
        {
            return MovementValidationResult.Failure("Destination location not found");
        }

        if (army.LocationType == LocationType.Region && destinationType == LocationType.Region)
        {
            return ValidateIntraSystemMovement(army, currentLocation, destinationInfo);
        }
        else if (army.LocationType == LocationType.Region && destinationType == LocationType.HyperspaceLaneMouth)
        {
            return ValidateMovementToHyperspaceLaneMouth(army, currentLocation, destinationInfo);
        }
        else if (army.LocationType == LocationType.HyperspaceLaneMouth && destinationType == LocationType.HyperspaceLaneMouth)
        {
            return ValidateHyperspaceLaneTraversal(army, currentLocation, destinationInfo);
        }
        else if (army.LocationType == LocationType.HyperspaceLaneMouth && destinationType == LocationType.Region)
        {
            return ValidateMovementFromHyperspaceLaneMouth(army, currentLocation, destinationInfo);
        }

        return MovementValidationResult.Failure("Invalid movement type");
    }

    private MovementValidationResult ValidateIntraSystemMovement(
        Army army,
        LocationInfo currentLocation,
        LocationInfo destinationLocation)
    {
        if (currentLocation.StarSystemId != destinationLocation.StarSystemId)
        {
            return MovementValidationResult.Failure(
                "Cannot move directly between regions in different star systems; must use hyperspace lanes");
        }

        return MovementValidationResult.Success();
    }

    private MovementValidationResult ValidateMovementToHyperspaceLaneMouth(
        Army army,
        LocationInfo currentLocation,
        LocationInfo destinationLocation)
    {
        if (currentLocation.StarSystemId != destinationLocation.StarSystemId)
        {
            return MovementValidationResult.Failure(
                "Cannot move to a hyperspace lane mouth in a different star system");
        }

        return MovementValidationResult.Success();
    }

    private MovementValidationResult ValidateHyperspaceLaneTraversal(
        Army army,
        LocationInfo currentLocation,
        LocationInfo destinationLocation)
    {
        var hyperspaceLane = currentLocation.HyperspaceLane;
        if (hyperspaceLane == null)
        {
            return MovementValidationResult.Failure("Current location is not a hyperspace lane mouth");
        }

        var oppositeMouthId = hyperspaceLane.GetOppositeMouthId(army.LocationId);
        if (destinationLocation.Id != oppositeMouthId)
        {
            return MovementValidationResult.Failure(
                "Can only travel to the opposite end of the current hyperspace lane");
        }

        return MovementValidationResult.Success();
    }

    private MovementValidationResult ValidateMovementFromHyperspaceLaneMouth(
        Army army,
        LocationInfo currentLocation,
        LocationInfo destinationLocation)
    {
        if (currentLocation.StarSystemId != destinationLocation.StarSystemId)
        {
            return MovementValidationResult.Failure(
                "Cannot move from a hyperspace lane mouth to a region in a different star system");
        }

        return MovementValidationResult.Success();
    }

    public ArrivalResult ProcessArrival(
        Army arrivingArmy,
        string destinationId,
        LocationType destinationType)
    {
        var currentPlayer = _game.Players.FirstOrDefault(p => p.Id == arrivingArmy.OwnerId);
        if (currentPlayer == null)
        {
            return new ArrivalResult
            {
                Success = false,
                Message = "Army owner not found"
            };
        }

        var destinationInfo = GetLocationInfo(destinationId, destinationType);
        if (destinationInfo == null)
        {
            return new ArrivalResult
            {
                Success = false,
                Message = "Destination not found"
            };
        }

        var existingArmy = GetArmyAtLocation(destinationId, destinationType);

        if (existingArmy == null)
        {
            return new ArrivalResult
            {
                Success = true,
                Action = ArrivalAction.TakePossession,
                Message = "Army takes possession of unoccupied location"
            };
        }

        var existingArmyOwner = _game.Players.FirstOrDefault(p => p.Id == existingArmy.OwnerId);
        if (existingArmyOwner == null)
        {
            return new ArrivalResult
            {
                Success = false,
                Message = "Existing army owner not found"
            };
        }

        if (existingArmy.OwnerId == arrivingArmy.OwnerId)
        {
            return new ArrivalResult
            {
                Success = true,
                Action = ArrivalAction.MergeArmies,
                Message = "Armies of the same player merge together",
                ExistingArmy = existingArmy
            };
        }

        if (currentPlayer.IsAlliedWith(existingArmyOwner))
        {
            return new ArrivalResult
            {
                Success = true,
                Action = ArrivalAction.MaintainNeutralPosture,
                Message = "Allied armies maintain neutral posture and remain separate",
                ExistingArmy = existingArmy
            };
        }

        if (existingArmy.IsInCombat)
        {
            var combatInvolvesCurrentPlayerOrAlliance = IsCombatInvolvingPlayerOrAlliance(existingArmy, currentPlayer);
            
            if (combatInvolvesCurrentPlayerOrAlliance)
            {
                return new ArrivalResult
                {
                    Success = true,
                    Action = ArrivalAction.JoinCombatAsReinforcement,
                    Message = "Army joins combat as reinforcement",
                    ExistingArmy = existingArmy
                };
            }
            else
            {
                return new ArrivalResult
                {
                    Success = false,
                    Action = ArrivalAction.ReturnToLaunchPoint,
                    Message = "Cannot join combat between non-allied players; returning to launch point",
                    ExistingArmy = existingArmy
                };
            }
        }

        var locationOwnerId = GetLocationOwnerId(destinationId, destinationType);
        if (locationOwnerId != null && locationOwnerId != arrivingArmy.OwnerId)
        {
            var locationOwner = _game.Players.FirstOrDefault(p => p.Id == locationOwnerId);
            if (locationOwner != null && !currentPlayer.IsAlliedWith(locationOwner))
            {
                return new ArrivalResult
                {
                    Success = true,
                    Action = ArrivalAction.InitiateCombat,
                    Message = "Combat initiated with non-allied army",
                    ExistingArmy = existingArmy
                };
            }
        }

        if (locationOwnerId != null && locationOwnerId != arrivingArmy.OwnerId)
        {
            var locationOwner = _game.Players.FirstOrDefault(p => p.Id == locationOwnerId);
            if (locationOwner != null && currentPlayer.IsAlliedWith(locationOwner))
            {
                return new ArrivalResult
                {
                    Success = true,
                    Action = ArrivalAction.MaintainNeutralPosture,
                    Message = "Allied armies maintain neutral posture",
                    ExistingArmy = existingArmy
                };
            }
        }

        return new ArrivalResult
        {
            Success = true,
            Action = ArrivalAction.InitiateCombat,
            Message = "Combat initiated with non-allied army",
            ExistingArmy = existingArmy
        };
    }

    private bool IsCombatInvolvingPlayerOrAlliance(Army combatArmy, Player currentPlayer)
    {
        var allArmiesAtLocation = GetAllArmiesAtLocation(combatArmy.LocationId, combatArmy.LocationType);
        
        foreach (var army in allArmiesAtLocation)
        {
            if (army.OwnerId == currentPlayer.Id)
            {
                return true;
            }

            var armyOwner = _game.Players.FirstOrDefault(p => p.Id == army.OwnerId);
            if (armyOwner != null && currentPlayer.IsAlliedWith(armyOwner))
            {
                return true;
            }
        }

        return false;
    }

    private List<Army> GetAllArmiesAtLocation(string locationId, LocationType locationType)
    {
        var armies = new List<Army>();

        if (locationType == LocationType.Region)
        {
            var region = GetRegionById(locationId);
            if (region?.Army != null)
            {
                armies.Add(region.Army);
            }
        }
        else if (locationType == LocationType.HyperspaceLaneMouth)
        {
            var hyperspaceLane = GetHyperspaceLaneByMouthId(locationId);
            if (hyperspaceLane != null)
            {
                if (hyperspaceLane.MouthAId == locationId && hyperspaceLane.MouthAArmy != null)
                {
                    armies.Add(hyperspaceLane.MouthAArmy);
                }
                else if (hyperspaceLane.MouthBId == locationId && hyperspaceLane.MouthBArmy != null)
                {
                    armies.Add(hyperspaceLane.MouthBArmy);
                }
            }
        }

        return armies;
    }

    private Army? GetArmyAtLocation(string locationId, LocationType locationType)
    {
        if (locationType == LocationType.Region)
        {
            var region = GetRegionById(locationId);
            return region?.Army;
        }
        else if (locationType == LocationType.HyperspaceLaneMouth)
        {
            var hyperspaceLane = GetHyperspaceLaneByMouthId(locationId);
            if (hyperspaceLane != null)
            {
                if (hyperspaceLane.MouthAId == locationId)
                {
                    return hyperspaceLane.MouthAArmy;
                }
                else if (hyperspaceLane.MouthBId == locationId)
                {
                    return hyperspaceLane.MouthBArmy;
                }
            }
        }

        return null;
    }

    private string? GetLocationOwnerId(string locationId, LocationType locationType)
    {
        if (locationType == LocationType.Region)
        {
            var region = GetRegionById(locationId);
            return region?.OwnerId;
        }
        else if (locationType == LocationType.HyperspaceLaneMouth)
        {
            var hyperspaceLane = GetHyperspaceLaneByMouthId(locationId);
            if (hyperspaceLane != null)
            {
                if (hyperspaceLane.MouthAId == locationId)
                {
                    return hyperspaceLane.MouthAOwnerId;
                }
                else if (hyperspaceLane.MouthBId == locationId)
                {
                    return hyperspaceLane.MouthBOwnerId;
                }
            }
        }

        return null;
    }

    private bool DoesPlayerOwnLocation(string playerId, string locationId, LocationType locationType)
    {
        var ownerId = GetLocationOwnerId(locationId, locationType);
        return ownerId == playerId;
    }

    private LocationInfo? GetLocationInfo(string locationId, LocationType locationType)
    {
        if (locationType == LocationType.Region)
        {
            var region = GetRegionById(locationId);
            if (region != null)
            {
                var stellarBody = GetStellarBodyById(region.StellarBodyId);
                if (stellarBody != null)
                {
                    return new LocationInfo
                    {
                        Id = locationId,
                        Type = locationType,
                        StarSystemId = stellarBody.StarSystemId,
                        Region = region
                    };
                }
            }
        }
        else if (locationType == LocationType.HyperspaceLaneMouth)
        {
            var hyperspaceLane = GetHyperspaceLaneByMouthId(locationId);
            if (hyperspaceLane != null)
            {
                var starSystemId = locationId == hyperspaceLane.MouthAId
                    ? hyperspaceLane.StarSystemAId
                    : hyperspaceLane.StarSystemBId;

                return new LocationInfo
                {
                    Id = locationId,
                    Type = locationType,
                    StarSystemId = starSystemId,
                    HyperspaceLane = hyperspaceLane
                };
            }
        }

        return null;
    }

    private Region? GetRegionById(string regionId)
    {
        return _game.GetAllRegions().FirstOrDefault(r => r.Id == regionId);
    }

    private StellarBody? GetStellarBodyById(string stellarBodyId)
    {
        return _game.StarSystems
            .SelectMany(system => system.StellarBodies)
            .FirstOrDefault(body => body.Id == stellarBodyId);
    }

    private HyperspaceLane? GetHyperspaceLaneByMouthId(string mouthId)
    {
        return _game.GetAllHyperspaceLanes()
            .FirstOrDefault(lane => lane.MouthAId == mouthId || lane.MouthBId == mouthId);
    }

    private class LocationInfo
    {
        public string Id { get; set; } = string.Empty;
        public LocationType Type { get; set; }
        public string StarSystemId { get; set; } = string.Empty;
        public Region? Region { get; set; }
        public HyperspaceLane? HyperspaceLane { get; set; }
    }
}

public class MovementValidationResult
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;

    public static MovementValidationResult Success()
    {
        return new MovementValidationResult { IsValid = true, Message = "Movement is valid" };
    }

    public static MovementValidationResult Failure(string message)
    {
        return new MovementValidationResult { IsValid = false, Message = message };
    }
}

public class ArrivalResult
{
    public bool Success { get; set; }
    public ArrivalAction Action { get; set; }
    public string Message { get; set; } = string.Empty;
    public Army? ExistingArmy { get; set; }
}

public enum ArrivalAction
{
    TakePossession,
    MergeArmies,
    MaintainNeutralPosture,
    InitiateCombat,
    JoinCombatAsReinforcement,
    ReturnToLaunchPoint
}
