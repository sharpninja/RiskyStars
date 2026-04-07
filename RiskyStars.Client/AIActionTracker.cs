using Microsoft.Xna.Framework;
using RiskyStars.Shared;

namespace RiskyStars.Client;

public class AIActionTracker
{
    private readonly AIActionIndicator _aiActionIndicator;
    private readonly MapData _mapData;
    private readonly GameStateCache _gameStateCache;
    private readonly RegionRenderer _regionRenderer;
    
    private Dictionary<string, ArmyState> _previousArmyStates = new();
    private Dictionary<string, RegionOwnership> _previousRegionOwnerships = new();
    private Dictionary<string, HyperspaceLaneMouthOwnership> _previousHyperspaceLaneMouthOwnerships = new();
    private Dictionary<string, PlayerState> _previousPlayerStates = new();
    
    private AIVisualizationWindow? _aiVisualizationWindow;

    public AIActionTracker(AIActionIndicator aiActionIndicator, MapData mapData, GameStateCache gameStateCache, RegionRenderer regionRenderer)
    {
        _aiActionIndicator = aiActionIndicator;
        _mapData = mapData;
        _gameStateCache = gameStateCache;
        _regionRenderer = regionRenderer;
    }
    
    public void SetAIVisualizationWindow(AIVisualizationWindow? window)
    {
        _aiVisualizationWindow = window;
    }

    public void ProcessGameUpdate(GameUpdate update, string? currentPlayerId)
    {
        if (update.UpdateCase != GameUpdate.UpdateOneofCase.GameState)
        {
            return;
        }

        var gameState = update.GameState;
        if (gameState == null)
        {
            return;
        }

        var aiPlayerId = gameState.CurrentPlayerId;
        if (string.IsNullOrEmpty(aiPlayerId) || aiPlayerId == currentPlayerId)
        {
            UpdatePreviousStates(gameState);
            return;
        }

        DetectAndVisualizeAIActions(gameState, aiPlayerId);
        UpdatePreviousStates(gameState);
    }

    private void DetectAndVisualizeAIActions(TurnBasedGameStateUpdate gameState, string aiPlayerId)
    {
        var playerColor = _regionRenderer.GetPlayerColor(aiPlayerId);
        var playerState = gameState.PlayerStates.FirstOrDefault(p => p.PlayerId == aiPlayerId);
        var playerName = playerState?.PlayerName ?? "AI";

        DetectPurchases(gameState, aiPlayerId, playerName, playerColor);
        DetectReinforcements(gameState, aiPlayerId, playerName, playerColor);
        DetectArmyMovements(gameState, aiPlayerId, playerName, playerColor);
        DetectOwnershipChanges(gameState, aiPlayerId, playerName, playerColor);
    }

    private void DetectPurchases(TurnBasedGameStateUpdate gameState, string aiPlayerId, string playerName, Color playerColor)
    {
        if (!_previousPlayerStates.TryGetValue(aiPlayerId, out var previousState))
        {
            return;
        }

        var currentState = gameState.PlayerStates.FirstOrDefault(p => p.PlayerId == aiPlayerId);
        if (currentState == null)
        {
            return;
        }

        int previousTotal = previousState.PopulationStockpile + previousState.MetalStockpile + previousState.FuelStockpile;
        int currentTotal = currentState.PopulationStockpile + currentState.MetalStockpile + currentState.FuelStockpile;

        if (currentTotal < previousTotal)
        {
            int resourcesSpent = previousTotal - currentTotal;
            int armiesPurchased = resourcesSpent / 3;
            
            if (armiesPurchased > 0)
            {
                string message = $"{playerName} purchased {armiesPurchased} {(armiesPurchased == 1 ? "army" : "armies")}";
                _aiActionIndicator.AddLogEntry(message, playerColor);
                _aiVisualizationWindow?.LogActivity(message);
            }
        }
    }

    private void DetectReinforcements(TurnBasedGameStateUpdate gameState, string aiPlayerId, string playerName, Color playerColor)
    {
        foreach (var currentArmy in gameState.ArmyStates.Where(a => a.OwnerId == aiPlayerId))
        {
            if (!_previousArmyStates.TryGetValue(currentArmy.ArmyId, out var previousArmy))
            {
                if (currentArmy.UnitCount > 0)
                {
                    string message = $"{playerName} reinforced with {currentArmy.UnitCount} units";
                    _aiActionIndicator.AddLogEntry(message, playerColor);
                    _aiVisualizationWindow?.LogActivity(message);
                    
                    if (_aiVisualizationWindow?.ShowReinforcementHighlights ?? true)
                    {
                        _aiActionIndicator.ShowReinforcement(currentArmy.LocationId, currentArmy.LocationType, 
                            currentArmy.UnitCount, playerColor);
                    }
                }
                continue;
            }

            if (currentArmy.UnitCount > previousArmy.UnitCount && 
                currentArmy.LocationId == previousArmy.LocationId)
            {
                int unitsAdded = currentArmy.UnitCount - previousArmy.UnitCount;
                string message = $"{playerName} reinforced with {unitsAdded} units";
                _aiActionIndicator.AddLogEntry(message, playerColor);
                _aiVisualizationWindow?.LogActivity(message);
                
                if (_aiVisualizationWindow?.ShowReinforcementHighlights ?? true)
                {
                    _aiActionIndicator.ShowReinforcement(currentArmy.LocationId, currentArmy.LocationType, 
                        unitsAdded, playerColor);
                }
            }
        }
    }

    private void DetectArmyMovements(TurnBasedGameStateUpdate gameState, string aiPlayerId, string playerName, Color playerColor)
    {
        foreach (var currentArmy in gameState.ArmyStates.Where(a => a.OwnerId == aiPlayerId))
        {
            if (!_previousArmyStates.TryGetValue(currentArmy.ArmyId, out var previousArmy))
            {
                continue;
            }

            if (currentArmy.LocationId != previousArmy.LocationId || 
                currentArmy.LocationType != previousArmy.LocationType)
            {
                var startPos = GetLocationPosition(previousArmy.LocationId, previousArmy.LocationType);
                var endPos = GetLocationPosition(currentArmy.LocationId, currentArmy.LocationType);

                if (startPos.HasValue && endPos.HasValue)
                {
                    string startName = GetLocationName(previousArmy.LocationId, previousArmy.LocationType);
                    string endName = GetLocationName(currentArmy.LocationId, currentArmy.LocationType);
                    
                    string message = $"{playerName} moved {currentArmy.UnitCount} units: {startName} → {endName}";
                    _aiActionIndicator.AddLogEntry(message, playerColor);
                    _aiVisualizationWindow?.LogActivity(message);
                    
                    if (_aiVisualizationWindow?.ShowMovementAnimations ?? true)
                    {
                        _aiActionIndicator.ShowArmyMovement(startPos.Value, endPos.Value, 
                            currentArmy.UnitCount, playerColor, currentArmy.ArmyId);
                    }
                }
            }
        }
    }

    private void DetectOwnershipChanges(TurnBasedGameStateUpdate gameState, string aiPlayerId, string playerName, Color playerColor)
    {
        foreach (var currentOwnership in gameState.RegionOwnerships.Where(o => o.OwnerId == aiPlayerId))
        {
            if (!_previousRegionOwnerships.TryGetValue(currentOwnership.RegionId, out var previousOwnership) ||
                previousOwnership.OwnerId != aiPlayerId)
            {
                string regionName = GetLocationName(currentOwnership.RegionId, LocationType.Region);
                string message = $"{playerName} captured {regionName}";
                _aiActionIndicator.AddLogEntry(message, playerColor);
                _aiVisualizationWindow?.LogActivity(message);
            }
        }

        foreach (var currentOwnership in gameState.HyperspaceLaneMouthOwnerships.Where(o => o.OwnerId == aiPlayerId))
        {
            if (!_previousHyperspaceLaneMouthOwnerships.TryGetValue(currentOwnership.HyperspaceLaneMouthId, out var previousOwnership) ||
                previousOwnership.OwnerId != aiPlayerId)
            {
                string laneMouthName = GetLocationName(currentOwnership.HyperspaceLaneMouthId, LocationType.HyperspaceLaneMouth);
                string message = $"{playerName} captured {laneMouthName}";
                _aiActionIndicator.AddLogEntry(message, playerColor);
                _aiVisualizationWindow?.LogActivity(message);
            }
        }
    }

    private void UpdatePreviousStates(TurnBasedGameStateUpdate gameState)
    {
        _previousArmyStates.Clear();
        foreach (var army in gameState.ArmyStates)
        {
            _previousArmyStates[army.ArmyId] = army;
        }

        _previousRegionOwnerships.Clear();
        foreach (var ownership in gameState.RegionOwnerships)
        {
            _previousRegionOwnerships[ownership.RegionId] = ownership;
        }

        _previousHyperspaceLaneMouthOwnerships.Clear();
        foreach (var ownership in gameState.HyperspaceLaneMouthOwnerships)
        {
            _previousHyperspaceLaneMouthOwnerships[ownership.HyperspaceLaneMouthId] = ownership;
        }

        _previousPlayerStates.Clear();
        foreach (var player in gameState.PlayerStates)
        {
            _previousPlayerStates[player.PlayerId] = player;
        }
    }

    private Vector2? GetLocationPosition(string locationId, LocationType locationType)
    {
        if (locationType == LocationType.Region)
        {
            foreach (var system in _mapData.StarSystems)
            {
                foreach (var body in system.StellarBodies)
                {
                    foreach (var region in body.Regions)
                    {
                        if (region.Id == locationId)
                        {
                            return region.Position;
                        }
                    }
                }
            }
        }
        else if (locationType == LocationType.HyperspaceLaneMouth)
        {
            foreach (var lane in _mapData.HyperspaceLanes)
            {
                if (lane.MouthAId == locationId)
                {
                    return lane.MouthAPosition;
                }
                if (lane.MouthBId == locationId)
                {
                    return lane.MouthBPosition;
                }
            }
        }

        return null;
    }

    private string GetLocationName(string locationId, LocationType locationType)
    {
        if (locationType == LocationType.Region)
        {
            foreach (var system in _mapData.StarSystems)
            {
                foreach (var body in system.StellarBodies)
                {
                    foreach (var region in body.Regions)
                    {
                        if (region.Id == locationId)
                        {
                            return region.Name;
                        }
                    }
                }
            }
        }
        else if (locationType == LocationType.HyperspaceLaneMouth)
        {
            foreach (var lane in _mapData.HyperspaceLanes)
            {
                if (lane.MouthAId == locationId)
                {
                    return $"Lane Mouth (near {GetSystemName(lane.StarSystemAId)})";
                }
                if (lane.MouthBId == locationId)
                {
                    return $"Lane Mouth (near {GetSystemName(lane.StarSystemBId)})";
                }
            }
        }

        return "Unknown Location";
    }

    private string GetSystemName(string systemId)
    {
        var system = _mapData.StarSystems.FirstOrDefault(s => s.Id == systemId);
        return system?.Name ?? "Unknown System";
    }
}
