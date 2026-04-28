using RiskyStars.Shared;
using ServerMapLoader = RiskyStars.Server.Services.MapLoader;

namespace RiskyStars.Tests;

public class ServerMapLoaderNamingTests
{
    [Fact]
    public void LoadFromJson_ReplacesPlaceholderMapNamesWithCatalogNames()
    {
        var map = new ServerMapLoader().LoadFromJson(PlaceholderJson);
        var homeSystem = map.StarSystems[0];
        var body = homeSystem.StellarBodies[0];

        Assert.Equal("Sirius", homeSystem.Name);
        Assert.Equal("Canopus", map.StarSystems[1].Name);
        Assert.Equal("Sirius b", body.Name);
        Assert.Equal("Sirius - Canopus", map.HyperspaceLanes[0].Name);
        Assert.DoesNotContain("Home System", map.HyperspaceLanes[0].Name, StringComparison.OrdinalIgnoreCase);

        foreach (var region in body.Regions)
        {
            Assert.Contains(region.Name, MapNameCatalog.FictionalPlaceNames);
            Assert.DoesNotMatch(@"^Continent \d+$", region.Name);
        }
    }

    [Fact]
    public void LoadFromFile_LoadsJsonAndAppliesCatalogNames()
    {
        var path = CreateTempMapPath();
        try
        {
            File.WriteAllText(path, PlaceholderJson);

            var map = new ServerMapLoader().LoadFromFile(path);

            Assert.Equal("Sirius", map.StarSystems[0].Name);
            Assert.Equal("Sirius b", map.StarSystems[0].StellarBodies[0].Name);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task LoadFromFileAsync_LoadsJsonAndAppliesCatalogNames()
    {
        var path = CreateTempMapPath();
        try
        {
            await File.WriteAllTextAsync(path, PlaceholderJson);

            var map = await new ServerMapLoader().LoadFromFileAsync(path);

            Assert.Equal("Sirius", map.StarSystems[0].Name);
            Assert.Equal("Sirius b", map.StarSystems[0].StellarBodies[0].Name);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void LoadFromFile_RejectsMissingFile()
    {
        var path = CreateTempMapPath();

        Assert.Throws<FileNotFoundException>(() => new ServerMapLoader().LoadFromFile(path));
    }

    [Fact]
    public async Task LoadFromFileAsync_RejectsMissingFile()
    {
        var path = CreateTempMapPath();

        await Assert.ThrowsAsync<FileNotFoundException>(() => new ServerMapLoader().LoadFromFileAsync(path));
    }

    [Fact]
    public void SaveToJson_WritesCatalogNames()
    {
        var loader = new ServerMapLoader();
        var map = loader.LoadFromJson(PlaceholderJson);

        var json = loader.SaveToJson(map);

        Assert.Contains("\"name\": \"Sirius\"", json, StringComparison.Ordinal);
        Assert.Contains("\"name\": \"Sirius b\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("Home System", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Planet 5432", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SaveToFile_WritesCatalogNames()
    {
        var loader = new ServerMapLoader();
        var map = loader.LoadFromJson(PlaceholderJson);
        var path = CreateTempMapPath();
        try
        {
            loader.SaveToFile(map, path);

            var json = File.ReadAllText(path);
            Assert.Contains("\"name\": \"Sirius\"", json, StringComparison.Ordinal);
            Assert.DoesNotContain("Home System", json, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task SaveToFileAsync_WritesCatalogNames()
    {
        var loader = new ServerMapLoader();
        var map = loader.LoadFromJson(PlaceholderJson);
        var path = CreateTempMapPath();
        try
        {
            await loader.SaveToFileAsync(map, path);

            var json = await File.ReadAllTextAsync(path);
            Assert.Contains("\"name\": \"Sirius\"", json, StringComparison.Ordinal);
            Assert.DoesNotContain("Home System", json, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void LoadFromJson_RejectsMapWithoutStarSystems()
    {
        const string json = """
        {
          "configuration": {
            "playerCount": 2,
            "homeSystemRegionCount": 8
          },
          "starSystems": [],
          "hyperspaceLanes": []
        }
        """;

        Assert.Throws<InvalidOperationException>(() => new ServerMapLoader().LoadFromJson(json));
    }

    [Fact]
    public void LoadFromJson_RejectsNullMapPayload()
    {
        Assert.Throws<InvalidOperationException>(() => new ServerMapLoader().LoadFromJson("null"));
    }

    [Fact]
    public void LoadFromJson_InitializesMissingCollections()
    {
        const string json = """
        {
          "configuration": {
            "playerCount": 1,
            "homeSystemRegionCount": 1
          },
          "starSystems": [
            {
              "id": "home_1",
              "name": "Home System 1",
              "type": 0,
              "stellarBodies": [
                {
                  "id": "body_1",
                  "name": "Gas Giant 7821",
                  "type": 0,
                  "surfaceType": null,
                  "continentCount": null,
                  "starSystemId": "home_1",
                  "upgradeLevel": 0,
                  "regions": null,
                  "heroes": null
                }
              ],
              "hyperspaceLanes": null
            },
            {
              "id": "featured",
              "name": "Featured System",
              "type": 1,
              "stellarBodies": null,
              "hyperspaceLanes": null
            }
          ],
          "hyperspaceLanes": null
        }
        """;

        var map = new ServerMapLoader().LoadFromJson(json);
        var system = map.StarSystems[0];
        var body = system.StellarBodies[0];

        Assert.Empty(map.HyperspaceLanes);
        Assert.Empty(system.HyperspaceLanes);
        Assert.Empty(map.StarSystems[1].StellarBodies);
        Assert.Empty(map.StarSystems[1].HyperspaceLanes);
        Assert.Empty(body.Regions);
        Assert.Empty(body.Heroes);
        Assert.Equal("Sirius b", body.Name);
    }

    [Fact]
    public void LoadFromJson_RejectsStarSystemWithoutId()
    {
        const string json = """
        {
          "starSystems": [
            {
              "id": "",
              "name": "Home System 1",
              "type": 0,
              "stellarBodies": [],
              "hyperspaceLanes": []
            }
          ],
          "hyperspaceLanes": []
        }
        """;

        Assert.Throws<InvalidOperationException>(() => new ServerMapLoader().LoadFromJson(json));
    }

    [Fact]
    public void LoadFromJson_RejectsDuplicateStarSystemIds()
    {
        const string json = """
        {
          "starSystems": [
            {
              "id": "home_1",
              "name": "Home System 1",
              "type": 0,
              "stellarBodies": [],
              "hyperspaceLanes": []
            },
            {
              "id": "home_1",
              "name": "Featured System",
              "type": 1,
              "stellarBodies": [],
              "hyperspaceLanes": []
            }
          ],
          "hyperspaceLanes": []
        }
        """;

        Assert.Throws<InvalidOperationException>(() => new ServerMapLoader().LoadFromJson(json));
    }

    [Fact]
    public void LoadFromJson_RejectsLaneWithoutBothEndpoints()
    {
        var json = CreateInvalidLaneJson(starSystemAId: "", starSystemBId: "featured");

        Assert.Throws<InvalidOperationException>(() => new ServerMapLoader().LoadFromJson(json));
    }

    [Fact]
    public void LoadFromJson_RejectsLaneWithUnknownFirstEndpoint()
    {
        var json = CreateInvalidLaneJson(starSystemAId: "unknown", starSystemBId: "featured");

        Assert.Throws<InvalidOperationException>(() => new ServerMapLoader().LoadFromJson(json));
    }

    [Fact]
    public void LoadFromJson_RejectsLaneWithUnknownSecondEndpoint()
    {
        var json = CreateInvalidLaneJson(starSystemAId: "home_1", starSystemBId: "unknown");

        Assert.Throws<InvalidOperationException>(() => new ServerMapLoader().LoadFromJson(json));
    }

    private static string CreateTempMapPath()
    {
        return Path.Combine(Path.GetTempPath(), $"risky-stars-map-{Guid.NewGuid():N}.json");
    }

    private static string CreateInvalidLaneJson(string starSystemAId, string starSystemBId)
    {
        return $$"""
        {
          "starSystems": [
            {
              "id": "home_1",
              "name": "Home System 1",
              "type": 0,
              "stellarBodies": [],
              "hyperspaceLanes": []
            },
            {
              "id": "featured",
              "name": "Featured System",
              "type": 1,
              "stellarBodies": [],
              "hyperspaceLanes": []
            }
          ],
          "hyperspaceLanes": [
            {
              "id": "lane_1",
              "name": "Home System 1 - Featured System",
              "starSystemAId": "{{starSystemAId}}",
              "starSystemBId": "{{starSystemBId}}",
              "mouthAId": "lane_1_mouth_a",
              "mouthBId": "lane_1_mouth_b",
              "mouthAOwnerId": null,
              "mouthBOwnerId": null,
              "mouthAArmy": null,
              "mouthBArmy": null
            }
          ]
        }
        """;
    }

    private const string PlaceholderJson = """
        {
          "configuration": {
            "playerCount": 2,
            "homeSystemRegionCount": 8
          },
          "starSystems": [
            {
              "id": "home_1",
              "name": "Home System 1",
              "type": 0,
              "stellarBodies": [
                {
                  "id": "body_1",
                  "name": "Planet 5432",
                  "type": 1,
                  "surfaceType": 1,
                  "continentCount": 2,
                  "starSystemId": "home_1",
                  "upgradeLevel": 0,
                  "regions": [
                    {
                      "id": "body_1_region_1",
                      "name": "Continent 1",
                      "stellarBodyId": "body_1",
                      "ownerId": null,
                      "army": null
                    },
                    {
                      "id": "body_1_region_2",
                      "name": "Continent 2",
                      "stellarBodyId": "body_1",
                      "ownerId": null,
                      "army": null
                    }
                  ],
                  "heroes": []
                }
              ],
              "hyperspaceLanes": []
            },
            {
              "id": "featured",
              "name": "Featured System",
              "type": 1,
              "stellarBodies": [],
              "hyperspaceLanes": []
            }
          ],
          "hyperspaceLanes": [
            {
              "id": "lane_1",
              "name": "Home System 1 - Featured System",
              "starSystemAId": "home_1",
              "starSystemBId": "featured",
              "mouthAId": "lane_1_mouth_a",
              "mouthBId": "lane_1_mouth_b",
              "mouthAOwnerId": null,
              "mouthBOwnerId": null,
              "mouthAArmy": null,
              "mouthBArmy": null
            }
          ]
        }
        """;
}
