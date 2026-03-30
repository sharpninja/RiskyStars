using System.Text.Json;
using RiskyStars.Server.Entities;

namespace RiskyStars.Server.Services;

public class MapLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public MapData LoadFromJson(string jsonContent)
    {
        var mapData = JsonSerializer.Deserialize<MapData>(jsonContent, JsonOptions);
        if (mapData == null)
        {
            throw new InvalidOperationException("Failed to deserialize map data");
        }

        ValidateMapData(mapData);
        ReconstructHyperspaceLaneReferences(mapData);
        
        return mapData;
    }

    public async Task<MapData> LoadFromFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Map file not found: {filePath}");
        }

        var jsonContent = await File.ReadAllTextAsync(filePath);
        return LoadFromJson(jsonContent);
    }

    public MapData LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Map file not found: {filePath}");
        }

        var jsonContent = File.ReadAllText(filePath);
        return LoadFromJson(jsonContent);
    }

    public string SaveToJson(MapData mapData)
    {
        ValidateMapData(mapData);
        return JsonSerializer.Serialize(mapData, JsonOptions);
    }

    public async Task SaveToFileAsync(MapData mapData, string filePath)
    {
        var jsonContent = SaveToJson(mapData);
        await File.WriteAllTextAsync(filePath, jsonContent);
    }

    public void SaveToFile(MapData mapData, string filePath)
    {
        var jsonContent = SaveToJson(mapData);
        File.WriteAllText(filePath, jsonContent);
    }

    private void ValidateMapData(MapData mapData)
    {
        if (mapData.StarSystems == null || mapData.StarSystems.Count == 0)
        {
            throw new InvalidOperationException("Map must contain at least one star system");
        }

        if (mapData.HyperspaceLanes == null)
        {
            mapData.HyperspaceLanes = new List<HyperspaceLane>();
        }

        var systemIds = new HashSet<string>();
        foreach (var system in mapData.StarSystems)
        {
            if (string.IsNullOrWhiteSpace(system.Id))
            {
                throw new InvalidOperationException("All star systems must have an ID");
            }

            if (!systemIds.Add(system.Id))
            {
                throw new InvalidOperationException($"Duplicate star system ID: {system.Id}");
            }

            if (system.StellarBodies == null)
            {
                system.StellarBodies = new List<StellarBody>();
            }

            if (system.HyperspaceLanes == null)
            {
                system.HyperspaceLanes = new List<HyperspaceLane>();
            }

            foreach (var body in system.StellarBodies)
            {
                if (body.Regions == null)
                {
                    body.Regions = new List<Region>();
                }

                if (body.Heroes == null)
                {
                    body.Heroes = new List<Hero>();
                }
            }
        }

        foreach (var lane in mapData.HyperspaceLanes)
        {
            if (string.IsNullOrWhiteSpace(lane.StarSystemAId) || string.IsNullOrWhiteSpace(lane.StarSystemBId))
            {
                throw new InvalidOperationException($"Hyperspace lane {lane.Id} must reference two star systems");
            }

            if (!systemIds.Contains(lane.StarSystemAId))
            {
                throw new InvalidOperationException($"Hyperspace lane {lane.Id} references unknown star system: {lane.StarSystemAId}");
            }

            if (!systemIds.Contains(lane.StarSystemBId))
            {
                throw new InvalidOperationException($"Hyperspace lane {lane.Id} references unknown star system: {lane.StarSystemBId}");
            }
        }
    }

    private void ReconstructHyperspaceLaneReferences(MapData mapData)
    {
        var systemsById = mapData.StarSystems.ToDictionary(s => s.Id);

        foreach (var lane in mapData.HyperspaceLanes)
        {
            if (systemsById.TryGetValue(lane.StarSystemAId, out var systemA))
            {
                if (!systemA.HyperspaceLanes.Any(l => l.Id == lane.Id))
                {
                    systemA.HyperspaceLanes.Add(lane);
                }
            }

            if (systemsById.TryGetValue(lane.StarSystemBId, out var systemB))
            {
                if (!systemB.HyperspaceLanes.Any(l => l.Id == lane.Id))
                {
                    systemB.HyperspaceLanes.Add(lane);
                }
            }
        }
    }
}
