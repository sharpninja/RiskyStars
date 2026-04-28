using RiskyStars.Client;
using RiskyStars.Shared;

namespace RiskyStars.Tests;

public class MapLoaderNamingTests
{
    [Fact]
    public void CreateSampleMap_NamesStarsFromRealStarCatalog()
    {
        var map = MapLoader.CreateSampleMap();

        for (int systemIndex = 0; systemIndex < map.StarSystems.Count; systemIndex++)
        {
            var system = map.StarSystems[systemIndex];

            Assert.Equal(MapNameCatalog.GetStarName(systemIndex), system.Name);
            Assert.DoesNotContain("Home System", system.Name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Featured System", system.Name, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void CreateSampleMap_NamesBodiesWithHostStarDesignations()
    {
        var map = MapLoader.CreateSampleMap();

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
    public void CreateSampleMap_NamesContinentsFromFictionalPlaceCatalog()
    {
        var map = MapLoader.CreateSampleMap();
        var namedContinents = map.StarSystems
            .SelectMany(system => system.StellarBodies)
            .Where(body => body.Type == StellarBodyType.RockyPlanet && body.Regions.Count > 1)
            .SelectMany(body => body.Regions)
            .ToList();

        Assert.NotEmpty(namedContinents);

        foreach (var continent in namedContinents)
        {
            Assert.Contains(continent.Name, MapNameCatalog.FictionalPlaceNames);
            Assert.DoesNotMatch(@"^Continent \d+$", continent.Name);
        }
    }

    [Fact]
    public void CreateSampleMap_RenamesHyperspaceLanesToUseStarNames()
    {
        var map = MapLoader.CreateSampleMap();
        var systemsById = map.StarSystems.ToDictionary(system => system.Id);

        foreach (var lane in map.HyperspaceLanes)
        {
            var expectedName = $"{systemsById[lane.StarSystemAId].Name} - {systemsById[lane.StarSystemBId].Name}";

            Assert.Equal(expectedName, lane.Name);
            Assert.DoesNotContain("Home System", lane.Name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Featured System", lane.Name, StringComparison.OrdinalIgnoreCase);
        }
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
