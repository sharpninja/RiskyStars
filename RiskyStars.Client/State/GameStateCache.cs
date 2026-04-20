using RiskyStars.Shared;
using System.Collections.Concurrent;

namespace RiskyStars.Client;

public class GameStateCache
{
    private readonly object _lock = new object();
    
    private string? _gameId;
    private int _turnNumber;
    private TurnPhase _currentPhase;
    private string? _currentPlayerId;
    private string? _eventMessage;
    
    private readonly Dictionary<string, PlayerState> _playerStates;
    private readonly Dictionary<string, RegionOwnership> _regionOwnerships;
    private readonly Dictionary<string, HyperspaceLaneMouthOwnership> _hyperspaceLaneMouthOwnerships;
    private readonly Dictionary<string, ArmyState> _armyStates;
    private readonly List<CombatEvent> _combatEvents;
    
    private long _lastUpdateTimestamp;

    public GameStateCache()
    {
        _playerStates = new Dictionary<string, PlayerState>();
        _regionOwnerships = new Dictionary<string, RegionOwnership>();
        _hyperspaceLaneMouthOwnerships = new Dictionary<string, HyperspaceLaneMouthOwnership>();
        _armyStates = new Dictionary<string, ArmyState>();
        _combatEvents = new List<CombatEvent>();
        _turnNumber = 0;
        _currentPhase = TurnPhase.Production;
        _lastUpdateTimestamp = 0;
    }

    public void ApplyUpdate(GameUpdate update)
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

        lock (_lock)
        {
            _gameId = gameState.GameId;
            _turnNumber = gameState.TurnNumber;
            _currentPhase = gameState.CurrentPhase;
            _currentPlayerId = gameState.CurrentPlayerId;
            _eventMessage = gameState.EventMessage;
            _lastUpdateTimestamp = update.Timestamp;

            foreach (var playerState in gameState.PlayerStates)
            {
                _playerStates[playerState.PlayerId] = playerState;
            }

            foreach (var regionOwnership in gameState.RegionOwnerships)
            {
                _regionOwnerships[regionOwnership.RegionId] = regionOwnership;
            }

            foreach (var hlmOwnership in gameState.HyperspaceLaneMouthOwnerships)
            {
                _hyperspaceLaneMouthOwnerships[hlmOwnership.HyperspaceLaneMouthId] = hlmOwnership;
            }

            foreach (var armyState in gameState.ArmyStates)
            {
                _armyStates[armyState.ArmyId] = armyState;
            }

            if (gameState.CombatEvents.Count > 0)
            {
                _combatEvents.Clear();
                _combatEvents.AddRange(gameState.CombatEvents);
            }
        }
    }

    public void ApplyIncrementalUpdate(TurnBasedGameStateUpdate gameState)
    {
        if (gameState == null)
        {
            return;
        }

        lock (_lock)
        {
            _gameId = gameState.GameId;
            _turnNumber = gameState.TurnNumber;
            _currentPhase = gameState.CurrentPhase;
            _currentPlayerId = gameState.CurrentPlayerId;
            _eventMessage = gameState.EventMessage;

            foreach (var playerState in gameState.PlayerStates)
            {
                _playerStates[playerState.PlayerId] = playerState;
            }

            foreach (var regionOwnership in gameState.RegionOwnerships)
            {
                _regionOwnerships[regionOwnership.RegionId] = regionOwnership;
            }

            foreach (var hlmOwnership in gameState.HyperspaceLaneMouthOwnerships)
            {
                _hyperspaceLaneMouthOwnerships[hlmOwnership.HyperspaceLaneMouthId] = hlmOwnership;
            }

            foreach (var armyState in gameState.ArmyStates)
            {
                _armyStates[armyState.ArmyId] = armyState;
            }

            if (gameState.CombatEvents.Count > 0)
            {
                _combatEvents.Clear();
                _combatEvents.AddRange(gameState.CombatEvents);
            }
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _gameId = null;
            _turnNumber = 0;
            _currentPhase = TurnPhase.Production;
            _currentPlayerId = null;
            _eventMessage = null;
            _playerStates.Clear();
            _regionOwnerships.Clear();
            _hyperspaceLaneMouthOwnerships.Clear();
            _armyStates.Clear();
            _combatEvents.Clear();
            _lastUpdateTimestamp = 0;
        }
    }

    public string? GetGameId()
    {
        lock (_lock)
        {
            return _gameId;
        }
    }

    public int GetTurnNumber()
    {
        lock (_lock)
        {
            return _turnNumber;
        }
    }

    public TurnPhase GetCurrentPhase()
    {
        lock (_lock)
        {
            return _currentPhase;
        }
    }

    public string? GetCurrentPlayerId()
    {
        lock (_lock)
        {
            return _currentPlayerId;
        }
    }

    public string? GetEventMessage()
    {
        lock (_lock)
        {
            return _eventMessage;
        }
    }

    public long GetLastUpdateTimestamp()
    {
        lock (_lock)
        {
            return _lastUpdateTimestamp;
        }
    }

    public PlayerState? GetPlayerState(string playerId)
    {
        lock (_lock)
        {
            return _playerStates.TryGetValue(playerId, out var state) ? state : null;
        }
    }

    public IReadOnlyList<PlayerState> GetAllPlayerStates()
    {
        lock (_lock)
        {
            return _playerStates.Values.ToList();
        }
    }

    public RegionOwnership? GetRegionOwnership(string regionId)
    {
        lock (_lock)
        {
            return _regionOwnerships.TryGetValue(regionId, out var ownership) ? ownership : null;
        }
    }

    public IReadOnlyList<RegionOwnership> GetAllRegionOwnerships()
    {
        lock (_lock)
        {
            return _regionOwnerships.Values.ToList();
        }
    }

    public IReadOnlyList<string> GetRegionsOwnedByPlayer(string playerId)
    {
        lock (_lock)
        {
            return _regionOwnerships.Values
                .Where(r => r.OwnerId == playerId)
                .Select(r => r.RegionId)
                .ToList();
        }
    }

    public HyperspaceLaneMouthOwnership? GetHyperspaceLaneMouthOwnership(string hyperspaceLaneMouthId)
    {
        lock (_lock)
        {
            return _hyperspaceLaneMouthOwnerships.TryGetValue(hyperspaceLaneMouthId, out var ownership) ? ownership : null;
        }
    }

    public IReadOnlyList<HyperspaceLaneMouthOwnership> GetAllHyperspaceLaneMouthOwnerships()
    {
        lock (_lock)
        {
            return _hyperspaceLaneMouthOwnerships.Values.ToList();
        }
    }

    public IReadOnlyList<string> GetHyperspaceLaneMouthsOwnedByPlayer(string playerId)
    {
        lock (_lock)
        {
            return _hyperspaceLaneMouthOwnerships.Values
                .Where(h => h.OwnerId == playerId)
                .Select(h => h.HyperspaceLaneMouthId)
                .ToList();
        }
    }

    public ArmyState? GetArmyState(string armyId)
    {
        lock (_lock)
        {
            return _armyStates.TryGetValue(armyId, out var state) ? state : null;
        }
    }

    public IReadOnlyList<ArmyState> GetAllArmyStates()
    {
        lock (_lock)
        {
            return _armyStates.Values.ToList();
        }
    }

    public IReadOnlyList<ArmyState> GetArmiesOwnedByPlayer(string playerId)
    {
        lock (_lock)
        {
            return _armyStates.Values
                .Where(a => a.OwnerId == playerId)
                .ToList();
        }
    }

    public IReadOnlyList<ArmyState> GetArmiesAtLocation(string locationId, LocationType locationType)
    {
        lock (_lock)
        {
            return _armyStates.Values
                .Where(a => a.LocationId == locationId && a.LocationType == locationType)
                .ToList();
        }
    }

    public IReadOnlyList<ArmyState> GetArmiesInCombat()
    {
        lock (_lock)
        {
            return _armyStates.Values
                .Where(a => a.IsInCombat)
                .ToList();
        }
    }

    public IReadOnlyList<ArmyState> GetArmiesThatHaveNotMoved()
    {
        lock (_lock)
        {
            return _armyStates.Values
                .Where(a => !a.HasMovedThisTurn)
                .ToList();
        }
    }

    public IReadOnlyList<CombatEvent> GetCombatEvents()
    {
        lock (_lock)
        {
            return _combatEvents.ToList();
        }
    }

    public bool HasCombatEvents()
    {
        lock (_lock)
        {
            return _combatEvents.Count > 0;
        }
    }

    public void ClearCombatEvents()
    {
        lock (_lock)
        {
            _combatEvents.Clear();
        }
    }

    public GameStateSnapshot GetSnapshot()
    {
        lock (_lock)
        {
            return new GameStateSnapshot
            {
                GameId = _gameId,
                TurnNumber = _turnNumber,
                CurrentPhase = _currentPhase,
                CurrentPlayerId = _currentPlayerId,
                EventMessage = _eventMessage,
                PlayerStates = _playerStates.Values.ToList(),
                RegionOwnerships = _regionOwnerships.Values.ToList(),
                HyperspaceLaneMouthOwnerships = _hyperspaceLaneMouthOwnerships.Values.ToList(),
                ArmyStates = _armyStates.Values.ToList(),
                CombatEvents = _combatEvents.ToList(),
                LastUpdateTimestamp = _lastUpdateTimestamp
            };
        }
    }

    public int GetPlayerCount()
    {
        lock (_lock)
        {
            return _playerStates.Count;
        }
    }

    public int GetArmyCount()
    {
        lock (_lock)
        {
            return _armyStates.Count;
        }
    }

    public int GetRegionCount()
    {
        lock (_lock)
        {
            return _regionOwnerships.Count;
        }
    }

    public int GetHyperspaceLaneMouthCount()
    {
        lock (_lock)
        {
            return _hyperspaceLaneMouthOwnerships.Count;
        }
    }

    public bool IsPlayerTurn(string playerId)
    {
        lock (_lock)
        {
            return _currentPlayerId == playerId;
        }
    }

    public bool HasGameStarted()
    {
        lock (_lock)
        {
            return _gameId != null && _playerStates.Count > 0;
        }
    }
    
    public int GetProductionRate(string playerId, string resourceType)
    {
        lock (_lock)
        {
            var playerState = GetPlayerState(playerId);
            if (playerState == null)
            {
                return 0;
            }

            int regionCount = GetRegionsOwnedByPlayer(playerId).Count;
            
            return resourceType switch
            {
                "population" => regionCount * 2,
                "metal" => regionCount * 1,
                "fuel" => regionCount * 1,
                _ => 0
            };
        }
    }
}

public class GameStateSnapshot
{
    public string? GameId { get; init; }
    public int TurnNumber { get; init; }
    public TurnPhase CurrentPhase { get; init; }
    public string? CurrentPlayerId { get; init; }
    public string? EventMessage { get; init; }
    public List<PlayerState> PlayerStates { get; init; } = new();
    public List<RegionOwnership> RegionOwnerships { get; init; } = new();
    public List<HyperspaceLaneMouthOwnership> HyperspaceLaneMouthOwnerships { get; init; } = new();
    public List<ArmyState> ArmyStates { get; init; } = new();
    public List<CombatEvent> CombatEvents { get; init; } = new();
    public long LastUpdateTimestamp { get; init; }
}
