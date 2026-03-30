using RiskyStars.Server.Entities;

namespace RiskyStars.Server.Services;

public class MapService
{
    private readonly MapGenerator _generator;
    private readonly MapLoader _loader;
    private MapData? _currentMap;

    public MapService()
    {
        _generator = new MapGenerator();
        _loader = new MapLoader();
    }

    public MapService(int seed)
    {
        _generator = new MapGenerator(seed);
        _loader = new MapLoader();
    }

    public MapData GetCurrentMap()
    {
        if (_currentMap == null)
        {
            throw new InvalidOperationException("No map is currently loaded. Generate or load a map first.");
        }
        return _currentMap;
    }

    public MapData GenerateNewMap(int playerCount = 2, int homeSystemRegionCount = 8)
    {
        var config = new MapConfiguration
        {
            PlayerCount = playerCount,
            HomeSystemRegionCount = homeSystemRegionCount
        };

        _currentMap = _generator.GenerateMap(config);
        return _currentMap;
    }

    public MapData LoadMap(string filePath)
    {
        _currentMap = _loader.LoadFromFile(filePath);
        return _currentMap;
    }

    public async Task<MapData> LoadMapAsync(string filePath)
    {
        _currentMap = await _loader.LoadFromFileAsync(filePath);
        return _currentMap;
    }

    public MapData LoadMapFromJson(string jsonContent)
    {
        _currentMap = _loader.LoadFromJson(jsonContent);
        return _currentMap;
    }

    public void SaveCurrentMap(string filePath)
    {
        if (_currentMap == null)
        {
            throw new InvalidOperationException("No map is currently loaded. Generate or load a map first.");
        }

        _loader.SaveToFile(_currentMap, filePath);
    }

    public async Task SaveCurrentMapAsync(string filePath)
    {
        if (_currentMap == null)
        {
            throw new InvalidOperationException("No map is currently loaded. Generate or load a map first.");
        }

        await _loader.SaveToFileAsync(_currentMap, filePath);
    }

    public string ExportCurrentMapToJson()
    {
        if (_currentMap == null)
        {
            throw new InvalidOperationException("No map is currently loaded. Generate or load a map first.");
        }

        return _loader.SaveToJson(_currentMap);
    }

    public StarSystem? GetStarSystemById(string systemId)
    {
        if (_currentMap == null)
        {
            return null;
        }

        return _currentMap.StarSystems.FirstOrDefault(s => s.Id == systemId);
    }

    public List<StarSystem> GetStarSystemsByType(StarSystemType type)
    {
        if (_currentMap == null)
        {
            return new List<StarSystem>();
        }

        return _currentMap.StarSystems.Where(s => s.Type == type).ToList();
    }

    public HyperspaceLane? GetHyperspaceLaneById(string laneId)
    {
        if (_currentMap == null)
        {
            return null;
        }

        return _currentMap.HyperspaceLanes.FirstOrDefault(l => l.Id == laneId);
    }

    public List<HyperspaceLane> GetHyperspaceLanesForSystem(string systemId)
    {
        if (_currentMap == null)
        {
            return new List<HyperspaceLane>();
        }

        return _currentMap.HyperspaceLanes
            .Where(l => l.StarSystemAId == systemId || l.StarSystemBId == systemId)
            .ToList();
    }

    public List<StarSystem> GetConnectedSystems(string systemId)
    {
        if (_currentMap == null)
        {
            return new List<StarSystem>();
        }

        var lanes = GetHyperspaceLanesForSystem(systemId);
        var connectedSystemIds = lanes.Select(l => 
            l.StarSystemAId == systemId ? l.StarSystemBId : l.StarSystemAId
        ).ToList();

        return _currentMap.StarSystems
            .Where(s => connectedSystemIds.Contains(s.Id))
            .ToList();
    }

    public int GetTotalRegionCount()
    {
        if (_currentMap == null)
        {
            return 0;
        }

        return _currentMap.StarSystems.Sum(s => s.GetTotalRegionCount());
    }

    public Dictionary<StarSystemType, int> GetSystemCountByType()
    {
        if (_currentMap == null)
        {
            return new Dictionary<StarSystemType, int>();
        }

        return _currentMap.StarSystems
            .GroupBy(s => s.Type)
            .ToDictionary(g => g.Key, g => g.Count());
    }
}
