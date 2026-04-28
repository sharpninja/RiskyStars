using RiskyStars.Server.Services;
using RiskyStars.Shared;
using ServerRockyPlanetSurfaceType = RiskyStars.Server.Entities.RockyPlanetSurfaceType;
using ServerStellarBody = RiskyStars.Server.Entities.StellarBody;
using ServerStellarBodyType = RiskyStars.Server.Entities.StellarBodyType;

namespace RiskyStars.Tests;

public class MapGeneratorNamingTests
{
    [Fact]
    public void GenerateMap_NamesStarsFromRealStarCatalog()
    {
        var map = GenerateMap(seed: 7);

        for (int systemIndex = 0; systemIndex < map.StarSystems.Count; systemIndex++)
        {
            var system = map.StarSystems[systemIndex];

            Assert.Equal(MapNameCatalog.GetStarName(systemIndex), system.Name);
            Assert.DoesNotContain("Home System", system.Name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Featured System", system.Name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Minor System", system.Name, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void GenerateMap_NamesBodiesWithHostStarDesignations()
    {
        var map = GenerateMap(seed: 11);

        foreach (var system in map.StarSystems)
        {
            for (int bodyIndex = 0; bodyIndex < system.StellarBodies.Count; bodyIndex++)
            {
                var body = system.StellarBodies[bodyIndex];

                Assert.Equal(MapNameCatalog.GetStellarBodyName(system.Name, bodyIndex), body.Name);
                Assert.False(UsesPlaceholderBodyName(body.Name), $"Unexpected placeholder body name: {body.Name}");
            }
        }
    }

    [Fact]
    public void GenerateMap_NamesGaiaContinentsFromFictionalPlaceCatalog()
    {
        var gaiaBody = GenerateGaiaBody();

        Assert.NotNull(gaiaBody);
        foreach (var region in gaiaBody!.Regions)
        {
            Assert.Contains(region.Name, MapNameCatalog.FictionalPlaceNames);
            Assert.DoesNotMatch(@"^Continent \d+$", region.Name);
        }
    }

    [Fact]
    public void GenerateMap_NamesHyperspaceLanesWithGeneratedStarNames()
    {
        var map = GenerateMap(seed: 13);
        var systemsById = map.StarSystems.ToDictionary(system => system.Id);

        foreach (var lane in map.HyperspaceLanes)
        {
            var expectedName = $"{systemsById[lane.StarSystemAId].Name} - {systemsById[lane.StarSystemBId].Name}";

            Assert.Equal(expectedName, lane.Name);
            Assert.DoesNotContain("Home System", lane.Name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Featured System", lane.Name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Minor System", lane.Name, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static MapData GenerateMap(int seed)
    {
        return new MapGenerator(seed).GenerateMap(new MapConfiguration
        {
            PlayerCount = 2,
            HomeSystemRegionCount = 8
        });
    }

    private static ServerStellarBody? GenerateGaiaBody()
    {
        for (int seed = 0; seed < 100; seed++)
        {
            var gaiaBody = GenerateMap(seed)
                .StarSystems
                .SelectMany(system => system.StellarBodies)
                .FirstOrDefault(body =>
                    body.Type == ServerStellarBodyType.RockyPlanet
                    && body.SurfaceType == ServerRockyPlanetSurfaceType.Gaia
                    && body.Regions.Count > 1);

            if (gaiaBody != null)
            {
                return gaiaBody;
            }
        }

        return null;
    }

    private static bool UsesPlaceholderBodyName(string bodyName)
    {
        return bodyName.StartsWith("Planet ", StringComparison.OrdinalIgnoreCase)
            || bodyName.StartsWith("Gas Giant ", StringComparison.OrdinalIgnoreCase)
            || bodyName.StartsWith("Planetoid ", StringComparison.OrdinalIgnoreCase)
            || bodyName.StartsWith("Comet ", StringComparison.OrdinalIgnoreCase)
            || bodyName.StartsWith("Body ", StringComparison.OrdinalIgnoreCase);
    }
}
