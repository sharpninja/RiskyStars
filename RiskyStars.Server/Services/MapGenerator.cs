using RiskyStars.Server.Entities;

namespace RiskyStars.Server.Services;

public class MapGenerator
{
    private readonly Random _random;
    private int _idCounter;

    public MapGenerator(int? seed = null)
    {
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
        _idCounter = 1;
    }

    public MapData GenerateMap(MapConfiguration config)
    {
        var mapData = new MapData
        {
            Configuration = config,
            StarSystems = new List<StarSystem>()
        };

        var homeSystems = GenerateHomeSystems(config);
        var featuredSystem = GenerateFeaturedSystem(config);
        var minorSystems = GenerateMinorSystems(config);

        mapData.StarSystems.AddRange(homeSystems);
        mapData.StarSystems.Add(featuredSystem);
        mapData.StarSystems.AddRange(minorSystems);

        GenerateHyperspaceLanes(mapData, homeSystems, featuredSystem, minorSystems);

        return mapData;
    }

    private List<StarSystem> GenerateHomeSystems(MapConfiguration config)
    {
        var systems = new List<StarSystem>();

        for (int i = 0; i < config.PlayerCount; i++)
        {
            var system = new StarSystem
            {
                Id = $"home_{i + 1}",
                Name = $"Home System {i + 1}",
                Type = StarSystemType.Home,
                StellarBodies = new List<StellarBody>(),
                HyperspaceLanes = new List<HyperspaceLane>()
            };

            GenerateStellarBodiesForSystem(system, config.HomeSystemRegionCount);
            systems.Add(system);
        }

        return systems;
    }

    private StarSystem GenerateFeaturedSystem(MapConfiguration config)
    {
        var system = new StarSystem
        {
            Id = "featured",
            Name = "Featured System",
            Type = StarSystemType.Featured,
            StellarBodies = new List<StellarBody>(),
            HyperspaceLanes = new List<HyperspaceLane>()
        };

        int featuredRegionCount = config.HomeSystemRegionCount * 2;
        GenerateStellarBodiesForSystem(system, featuredRegionCount);

        return system;
    }

    private List<StarSystem> GenerateMinorSystems(MapConfiguration config)
    {
        var systems = new List<StarSystem>();
        int minorSystemCount = config.PlayerCount * 2;
        int minorRegionCount = (int)Math.Ceiling(config.HomeSystemRegionCount * 0.25);

        for (int i = 0; i < minorSystemCount; i++)
        {
            var system = new StarSystem
            {
                Id = $"minor_{i + 1}",
                Name = $"Minor System {i + 1}",
                Type = StarSystemType.Minor,
                StellarBodies = new List<StellarBody>(),
                HyperspaceLanes = new List<HyperspaceLane>()
            };

            GenerateStellarBodiesForSystem(system, minorRegionCount);
            systems.Add(system);
        }

        return systems;
    }

    private void GenerateStellarBodiesForSystem(StarSystem system, int targetRegionCount)
    {
        int currentRegionCount = 0;
        var bodyTypes = new[] { StellarBodyType.GasGiant, StellarBodyType.RockyPlanet, StellarBodyType.Planetoid, StellarBodyType.Comet };

        while (currentRegionCount < targetRegionCount)
        {
            var bodyType = bodyTypes[_random.Next(bodyTypes.Length)];
            var body = GenerateStellarBody(system.Id, bodyType);

            int bodyRegionCount = body.GetRegionCount();
            if (currentRegionCount + bodyRegionCount <= targetRegionCount)
            {
                system.StellarBodies.Add(body);
                currentRegionCount += bodyRegionCount;
            }
            else if (bodyType == StellarBodyType.RockyPlanet)
            {
                int remainingRegions = targetRegionCount - currentRegionCount;
                var adjustedBody = GenerateRockyPlanetWithExactRegions(system.Id, remainingRegions);
                if (adjustedBody != null)
                {
                    system.StellarBodies.Add(adjustedBody);
                    currentRegionCount += adjustedBody.GetRegionCount();
                }
            }
        }
    }

    private StellarBody GenerateStellarBody(string starSystemId, StellarBodyType type)
    {
        var body = new StellarBody
        {
            Id = $"body_{_idCounter++}",
            Name = GenerateBodyName(type),
            Type = type,
            StarSystemId = starSystemId,
            UpgradeLevel = 0,
            Regions = new List<Region>(),
            Heroes = new List<Hero>()
        };

        if (type == StellarBodyType.RockyPlanet)
        {
            var surfaceTypes = Enum.GetValues<RockyPlanetSurfaceType>();
            body.SurfaceType = surfaceTypes[_random.Next(surfaceTypes.Length)];

            if (body.SurfaceType == RockyPlanetSurfaceType.Gaia)
            {
                body.ContinentCount = _random.Next(2, 11);
            }
        }

        GenerateRegionsForBody(body);
        return body;
    }

    private StellarBody? GenerateRockyPlanetWithExactRegions(string starSystemId, int regionCount)
    {
        var body = new StellarBody
        {
            Id = $"body_{_idCounter++}",
            Name = GenerateBodyName(StellarBodyType.RockyPlanet),
            Type = StellarBodyType.RockyPlanet,
            StarSystemId = starSystemId,
            UpgradeLevel = 0,
            Regions = new List<Region>(),
            Heroes = new List<Hero>()
        };

        if (regionCount == 1)
        {
            body.SurfaceType = RockyPlanetSurfaceType.Ocean;
        }
        else if (regionCount == 2)
        {
            body.SurfaceType = RockyPlanetSurfaceType.Barren;
        }
        else if (regionCount >= 2 && regionCount <= 10)
        {
            body.SurfaceType = RockyPlanetSurfaceType.Gaia;
            body.ContinentCount = regionCount;
        }
        else
        {
            return null;
        }

        GenerateRegionsForBody(body);
        return body;
    }

    private void GenerateRegionsForBody(StellarBody body)
    {
        int regionCount = body.GetRegionCount();

        for (int i = 0; i < regionCount; i++)
        {
            var region = new Region
            {
                Id = $"{body.Id}_region_{i + 1}",
                Name = GenerateRegionName(body.Type, body.SurfaceType, i, regionCount),
                StellarBodyId = body.Id,
                OwnerId = null,
                Army = null
            };

            body.Regions.Add(region);
        }
    }

    private void GenerateHyperspaceLanes(MapData mapData, List<StarSystem> homeSystems, StarSystem featuredSystem, List<StarSystem> minorSystems)
    {
        int laneCounter = 1;

        for (int i = 0; i < homeSystems.Count; i++)
        {
            var homeSystem = homeSystems[i];
            var lane = CreateHyperspaceLane($"lane_{laneCounter++}", homeSystem, featuredSystem);
            mapData.HyperspaceLanes.Add(lane);
        }

        int minorIndex = 0;
        for (int i = 0; i < homeSystems.Count; i++)
        {
            var homeSystem = homeSystems[i];

            if (minorIndex < minorSystems.Count)
            {
                var lane = CreateHyperspaceLane($"lane_{laneCounter++}", homeSystem, minorSystems[minorIndex]);
                mapData.HyperspaceLanes.Add(lane);
                minorIndex++;
            }

            if (minorIndex < minorSystems.Count)
            {
                var lane = CreateHyperspaceLane($"lane_{laneCounter++}", homeSystem, minorSystems[minorIndex]);
                mapData.HyperspaceLanes.Add(lane);
                minorIndex++;
            }
        }
    }

    private HyperspaceLane CreateHyperspaceLane(string id, StarSystem systemA, StarSystem systemB)
    {
        var lane = new HyperspaceLane
        {
            Id = id,
            Name = $"{systemA.Name} - {systemB.Name}",
            StarSystemAId = systemA.Id,
            StarSystemBId = systemB.Id,
            MouthAId = $"{id}_mouth_a",
            MouthBId = $"{id}_mouth_b",
            MouthAOwnerId = null,
            MouthBOwnerId = null,
            MouthAArmy = null,
            MouthBArmy = null
        };

        systemA.HyperspaceLanes.Add(lane);
        systemB.HyperspaceLanes.Add(lane);

        return lane;
    }

    private string GenerateBodyName(StellarBodyType type)
    {
        return type switch
        {
            StellarBodyType.GasGiant => $"Gas Giant {_random.Next(1000, 9999)}",
            StellarBodyType.RockyPlanet => $"Planet {_random.Next(1000, 9999)}",
            StellarBodyType.Planetoid => $"Planetoid {_random.Next(1000, 9999)}",
            StellarBodyType.Comet => $"Comet {_random.Next(1000, 9999)}",
            _ => $"Body {_random.Next(1000, 9999)}"
        };
    }

    private string GenerateRegionName(StellarBodyType type, RockyPlanetSurfaceType? surfaceType, int index, int totalRegions)
    {
        if (type == StellarBodyType.GasGiant || type == StellarBodyType.Planetoid || type == StellarBodyType.Comet)
        {
            return "Surface";
        }

        if (type == StellarBodyType.RockyPlanet && surfaceType.HasValue)
        {
            return surfaceType.Value switch
            {
                RockyPlanetSurfaceType.Barren => index == 0 ? "Northern Hemisphere" : "Southern Hemisphere",
                RockyPlanetSurfaceType.Ocean => "Ocean Surface",
                RockyPlanetSurfaceType.Gaia => $"Continent {index + 1}",
                _ => $"Region {index + 1}"
            };
        }

        return $"Region {index + 1}";
    }
}

public class MapConfiguration
{
    public int PlayerCount { get; set; } = 2;
    public int HomeSystemRegionCount { get; set; } = 8;
}

public class MapData
{
    public MapConfiguration Configuration { get; set; } = new();
    public List<StarSystem> StarSystems { get; set; } = new();
    public List<HyperspaceLane> HyperspaceLanes { get; set; } = new();
}
