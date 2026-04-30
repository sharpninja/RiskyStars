using System.ClientModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace RiskyStars.Tests;

public sealed class AgentWireframeComparisonTests
{
    [Fact]
    public void Configuration_DefaultsToDisabledGrokEndpoint()
    {
        AgentWireframeComparisonOptions options = AgentWireframeComparisonConfiguration.Load(
            AgentWireframeComparisonConfiguration.BuildForTests(new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)));

        Assert.Equal("Grok", options.Provider);
        Assert.Equal("https://api.x.ai/v1", options.Endpoint);
        Assert.Equal("grok-4", options.Model);
        Assert.Equal(string.Empty, options.ApiKey);
        Assert.False(options.Enabled);
        Assert.Equal("RiskyStars.Client/Screenshots/AgentComparisons", options.OutputDirectory);
        Assert.Equal(0, options.Temperature);
        Assert.Equal(300, options.TimeoutSeconds);
    }

    [Fact]
    public void Configuration_AppSettingsEnablesGrokWithExtendedTimeout()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(FindRepositoryFile("RiskyStars.Tests"))
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        AgentWireframeComparisonOptions options = AgentWireframeComparisonConfiguration.Load(configuration);

        Assert.Equal("Grok", options.Provider);
        Assert.Equal("https://api.x.ai/v1", options.Endpoint);
        Assert.Equal("grok-4", options.Model);
        Assert.Equal(string.Empty, options.ApiKey);
        Assert.True(options.Enabled);
        Assert.Equal(300, options.TimeoutSeconds);
    }

    [Fact]
    public void Configuration_RiskyStarsEnvironmentOverridesJson()
    {
        Dictionary<string, string?> environment = new(StringComparer.OrdinalIgnoreCase)
        {
            ["RISKYSTARS_AgentWireframeComparison__Provider"] = "OpenAICompatible",
            ["RISKYSTARS_AgentWireframeComparison__Endpoint"] = "https://example.test/v1",
            ["RISKYSTARS_AgentWireframeComparison__Model"] = "custom-vision-model",
            ["RISKYSTARS_AgentWireframeComparison__ApiKey"] = "riskystars-key",
            ["RISKYSTARS_AgentWireframeComparison__Enabled"] = "true",
            ["RISKYSTARS_AgentWireframeComparison__OutputDirectory"] = "custom-output",
            ["RISKYSTARS_AgentWireframeComparison__Temperature"] = "0.2",
            ["RISKYSTARS_AgentWireframeComparison__TimeoutSeconds"] = "30"
        };

        AgentWireframeComparisonOptions options = AgentWireframeComparisonConfiguration.Load(
            AgentWireframeComparisonConfiguration.BuildForTests(environment));

        Assert.Equal("OpenAICompatible", options.Provider);
        Assert.Equal("https://example.test/v1", options.Endpoint);
        Assert.Equal("custom-vision-model", options.Model);
        Assert.Equal("riskystars-key", options.ApiKey);
        Assert.True(options.Enabled);
        Assert.Equal("custom-output", options.OutputDirectory);
        Assert.Equal(0.2f, options.Temperature);
        Assert.Equal(30, options.TimeoutSeconds);
    }

    [Fact]
    public void Configuration_UsesXaiApiKeyFallbackWhenSectionKeyIsEmpty()
    {
        Dictionary<string, string?> environment = new(StringComparer.OrdinalIgnoreCase)
        {
            ["AgentWireframeComparison:ApiKey"] = string.Empty,
            ["XAI_API_KEY"] = "xai-key"
        };

        AgentWireframeComparisonOptions options = AgentWireframeComparisonConfiguration.Load(
            AgentWireframeComparisonConfiguration.BuildForTests(environment));

        Assert.Equal("xai-key", options.ApiKey);
        Assert.True(options.Enabled);
    }

    [Fact]
    public void Configuration_RiskyStarsApiKeyWinsOverXaiApiKeyFallback()
    {
        Dictionary<string, string?> environment = new(StringComparer.OrdinalIgnoreCase)
        {
            ["RISKYSTARS_AgentWireframeComparison__ApiKey"] = "section-key",
            ["XAI_API_KEY"] = "xai-key"
        };

        AgentWireframeComparisonOptions options = AgentWireframeComparisonConfiguration.Load(
            AgentWireframeComparisonConfiguration.BuildForTests(environment));

        Assert.Equal("section-key", options.ApiKey);
    }

    [Fact]
    public void Configuration_UsesGenericApiKeyFallbackWhenSpecificKeysAreEmpty()
    {
        Dictionary<string, string?> environment = new(StringComparer.OrdinalIgnoreCase)
        {
            ["AgentWireframeComparison:ApiKey"] = string.Empty,
            ["ApiKey"] = "generic-key"
        };

        AgentWireframeComparisonOptions options = AgentWireframeComparisonConfiguration.Load(
            AgentWireframeComparisonConfiguration.BuildForTests(environment));

        Assert.Equal("generic-key", options.ApiKey);
        Assert.True(options.Enabled);
    }

    [Fact]
    public void Configuration_UsesMachineGenericApiKeyFallbackWhenProcessEnvironmentIsStale()
    {
        IReadOnlyDictionary<string, string?> fallback =
            AgentWireframeComparisonConfiguration.CreateExternalEnvironmentFallbackConfiguration((name, target) =>
                name == "ApiKey" && target == EnvironmentVariableTarget.Machine
                    ? "machine-generic-key"
                    : null);
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(fallback)
            .Build();

        AgentWireframeComparisonOptions options = AgentWireframeComparisonConfiguration.Load(configuration);

        Assert.Equal("machine-generic-key", options.ApiKey);
        Assert.True(options.Enabled);
    }

    [Fact]
    public void Configuration_PrefersRiskyStarsMachineApiKeyOverGenericMachineFallback()
    {
        IReadOnlyDictionary<string, string?> fallback =
            AgentWireframeComparisonConfiguration.CreateExternalEnvironmentFallbackConfiguration((name, target) =>
            {
                if (target != EnvironmentVariableTarget.Machine)
                {
                    return null;
                }

                return name switch
                {
                    "RISKYSTARS_AgentWireframeComparison__ApiKey" => "riskystars-machine-key",
                    "ApiKey" => "machine-generic-key",
                    _ => null
                };
            });
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(fallback)
            .Build();

        AgentWireframeComparisonOptions options = AgentWireframeComparisonConfiguration.Load(configuration);

        Assert.Equal("riskystars-machine-key", options.ApiKey);
        Assert.True(options.Enabled);
    }

    [Fact]
    public void Configuration_EnabledWithoutApiKeyIsBadBehavior()
    {
        Dictionary<string, string?> environment = new(StringComparer.OrdinalIgnoreCase)
        {
            ["AgentWireframeComparison:Enabled"] = "true",
            ["AgentWireframeComparison:ApiKey"] = string.Empty
        };

        AgentWireframeComparisonOptions options = AgentWireframeComparisonConfiguration.Load(
            AgentWireframeComparisonConfiguration.BuildForTests(environment));

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() => options.ValidateForRun());
        Assert.Contains("API key", failure.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Configuration_BadEndpointIsRejected()
    {
        Dictionary<string, string?> environment = new(StringComparer.OrdinalIgnoreCase)
        {
            ["AgentWireframeComparison:Endpoint"] = "not-a-uri"
        };

        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() =>
            AgentWireframeComparisonConfiguration.Load(AgentWireframeComparisonConfiguration.BuildForTests(environment)));
        Assert.Contains("Endpoint", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Prompt_IncludesStrictJsonContractAndWireframeSuitabilityRequirement()
    {
        AgentWireframeComparisonScenario scenario = AgentWireframeComparisonScenarioCatalog.Load(
            AgentWireframeComparisonScenarioCatalog.ScenarioPaths[0]);

        string instructions = AgentWireframeComparisonPrompt.BuildInstructions();
        string userPrompt = AgentWireframeComparisonPrompt.BuildUserPrompt(scenario);

        Assert.Contains("Return only valid JSON", instructions, StringComparison.Ordinal);
        Assert.Contains("correctElements", instructions, StringComparison.Ordinal);
        Assert.Contains("incorrectElements", instructions, StringComparison.Ordinal);
        Assert.Contains("wireframeSuitability", instructions, StringComparison.Ordinal);
        Assert.Contains("whyIncorrect", instructions, StringComparison.Ordinal);
        Assert.Contains("YAML scenario", instructions, StringComparison.Ordinal);
        Assert.Contains("resultSchema", instructions, StringComparison.Ordinal);
        Assert.Contains("Use exactly one of these lowercase rating values", instructions, StringComparison.Ordinal);
        Assert.Contains("Do not use alternate labels such as partially suitable", instructions, StringComparison.Ordinal);
        Assert.Contains("wireframe itself", instructions, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("YAML scenario file content", userPrompt, StringComparison.Ordinal);
        Assert.Contains(scenario.RawYaml, userPrompt, StringComparison.Ordinal);
        Assert.Contains("\"$schema\"", userPrompt, StringComparison.Ordinal);
        Assert.Contains("\"rating\": { \"type\": \"string\", \"enum\": [\"suitable\", \"partially_suitable\", \"unsuitable\"] }", userPrompt, StringComparison.Ordinal);
        Assert.Contains("actual HWND screenshot", userPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("prompt wireframe", userPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(scenario.ScreenId, userPrompt, StringComparison.Ordinal);
        foreach (string promptLine in NormalizeNewlines(scenario.Prompt).Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            Assert.Contains(promptLine, userPrompt, StringComparison.Ordinal);
        }

        Assert.Contains(scenario.ActualScreenshotPath, userPrompt, StringComparison.Ordinal);
        Assert.Contains(scenario.WireframeScreenshotPath, userPrompt, StringComparison.Ordinal);
    }

    [Fact]
    public void StructuredOutput_UsesJsonSchemaResponseFormat()
    {
        ChatResponseFormat format = AgentWireframeComparisonJson.CreateResponseFormat(AgentWireframeComparisonJson.SchemaJson);

        Assert.IsType<ChatResponseFormatJson>(format);
    }

    [Fact]
    public void ResultParser_AcceptsCompleteStrictJson()
    {
        string json = """
        {
          "screenId": "debug-info-window",
          "summary": "Debug window generally matches, but the top bar bounds do not.",
          "correctElements": [
            {
              "element": "Debug Information window title",
              "requirement": "Window title must identify the active debug panel",
              "evidence": "Visible title matches expected label",
              "confidence": 0.95
            }
          ],
          "incorrectElements": [
            {
              "element": "Top bar bounds",
              "requirement": "Top bar visual bounds must match the documented dock area",
              "expected": "Wireframe and screenshot align on the top bar height",
              "actual": "Screenshot top bar is shorter than highlighted bounds",
              "whyIncorrect": "The visual tree bounds do not match the rendered panel.",
              "severity": "high",
              "confidence": 0.9
            }
          ],
          "wireframeSuitability": {
            "rating": "partially_suitable",
            "why": "It captures the major regions but cannot validate exact panel bounds.",
            "missingRequirements": ["Exact top bar height"],
            "recommendedWireframeChanges": ["Add measured top bar bounds"]
          }
        }
        """;

        AgentWireframeComparisonResult result = AgentWireframeComparisonValidator.ParseAndValidate(
            json,
            "debug-info-window");

        Assert.Equal("debug-info-window", result.ScreenId);
        Assert.Single(result.CorrectElements);
        Assert.Single(result.IncorrectElements);
        Assert.Equal("partially_suitable", result.WireframeSuitability!.Rating);
    }

    [Fact]
    public void ResultParser_RejectsMalformedJsonAsBadBehavior()
    {
        Assert.Throws<JsonException>(() =>
            AgentWireframeComparisonValidator.ParseAndValidate("{ not json", "debug-info-window"));
    }

    [Fact]
    public void ResultParser_RejectsMissingSuitabilityAsBadBehavior()
    {
        string json = """
        {
          "screenId": "debug-info-window",
          "summary": "Incomplete result",
          "correctElements": [
            {
              "element": "Title",
              "requirement": "Has title",
              "evidence": "Visible",
              "confidence": 0.9
            }
          ],
          "incorrectElements": [
            {
              "element": "Bounds",
              "requirement": "Bounds align",
              "expected": "Aligned",
              "actual": "Misaligned",
              "whyIncorrect": "Mismatch",
              "severity": "high",
              "confidence": 0.9
            }
          ]
        }
        """;

        InvalidDataException failure = Assert.Throws<InvalidDataException>(() =>
            AgentWireframeComparisonValidator.ParseAndValidate(json, "debug-info-window"));

        Assert.Contains("wireframeSuitability", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ResultParser_RejectsEmptyIncorrectElementsAsBadBehavior()
    {
        string json = """
        {
          "screenId": "debug-info-window",
          "summary": "Incomplete result",
          "correctElements": [
            {
              "element": "Title",
              "requirement": "Has title",
              "evidence": "Visible",
              "confidence": 0.9
            }
          ],
          "incorrectElements": [],
          "wireframeSuitability": {
            "rating": "suitable",
            "why": "It is adequate.",
            "missingRequirements": [],
            "recommendedWireframeChanges": []
          }
        }
        """;

        InvalidDataException failure = Assert.Throws<InvalidDataException>(() =>
            AgentWireframeComparisonValidator.ParseAndValidate(json, "debug-info-window"));

        Assert.Contains("incorrectElements", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ResultParser_RejectsInvalidSuitabilityRatingAsBadBehavior()
    {
        string json = """
        {
          "screenId": "debug-info-window",
          "summary": "The result uses an invalid suitability enum.",
          "correctElements": [
            {
              "element": "Title",
              "requirement": "Has title",
              "evidence": "Visible",
              "confidence": 0.9
            }
          ],
          "incorrectElements": [
            {
              "element": "Bounds",
              "requirement": "Bounds align",
              "expected": "Aligned",
              "actual": "Misaligned",
              "whyIncorrect": "Mismatch",
              "severity": "high",
              "confidence": 0.9
            }
          ],
          "wireframeSuitability": {
            "rating": "partially suitable",
            "why": "Invalid enum spelling with a space.",
            "missingRequirements": [],
            "recommendedWireframeChanges": []
          }
        }
        """;

        InvalidDataException failure = Assert.Throws<InvalidDataException>(() =>
            AgentWireframeComparisonValidator.ParseAndValidate(json, "debug-info-window"));

        Assert.Contains("wireframeSuitability.rating", failure.Message, StringComparison.Ordinal);
        Assert.Contains("partially_suitable", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AgentComparisonYamlScenarios_CoverEveryDocumentedPromptScreen()
    {
        IReadOnlyList<AgentWireframeComparisonScenario> scenarios = AgentWireframeComparisonScenarioCatalog.LoadAll();
        IReadOnlyList<string> documentedScreenIds = AgentWireframeComparisonScenarioCatalog.LoadDocumentedScreenIds();

        Assert.True(scenarios.Count >= 28, $"Expected the full documented UI screen set, found {scenarios.Count}.");
        Assert.Equal(documentedScreenIds.Order(StringComparer.Ordinal), scenarios.Select(item => item.ScreenId).Order(StringComparer.Ordinal));
        Assert.Equal(scenarios.Count, scenarios.Select(item => item.SourcePath).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(scenarios.Count, scenarios.Select(item => item.ScreenId).Distinct(StringComparer.Ordinal).Count());

        foreach (AgentWireframeComparisonScenario scenario in scenarios)
        {
            Assert.Contains("Compare the actual HWND screenshot", scenario.Prompt, StringComparison.Ordinal);
            Assert.Contains(scenario.ScreenId, scenario.Prompt, StringComparison.Ordinal);
            Assert.Contains("\"$schema\"", scenario.ResultSchema, StringComparison.Ordinal);
            Assert.NotEmpty(scenario.FunctionalRequirements);
            Assert.NotEmpty(scenario.TechnicalRequirements);
            Assert.All(scenario.FunctionalRequirements, item => Assert.StartsWith("FR-", item.Id, StringComparison.Ordinal));
            Assert.All(scenario.TechnicalRequirements, item => Assert.StartsWith("TR-", item.Id, StringComparison.Ordinal));
            Assert.Equal("AGENTS-README-FIRST.yaml", scenario.AgentsReadmeFirstPath);
            Assert.Contains("Prioritize correctness over speed", scenario.AgentsReadmeFirstContent, StringComparison.Ordinal);
            Assert.Contains("agentsReadmeFirstContent: |", scenario.ModelPayloadYaml, StringComparison.Ordinal);
            Assert.True(File.Exists(scenario.ActualScreenshotFullPath), $"Missing actual screenshot for {scenario.ScreenId}.");
            Assert.True(File.Exists(scenario.WireframeScreenshotFullPath), $"Missing prompt wireframe for {scenario.ScreenId}.");
            Assert.Equal((1536, 832), PngProbe.ReadDimensions(scenario.ActualScreenshotFullPath));
            Assert.Equal((1536, 832), PngProbe.ReadDimensions(scenario.WireframeScreenshotFullPath));
            Assert.True(new FileInfo(scenario.ActualScreenshotFullPath).Length > 4096, $"{scenario.ActualScreenshotPath} is too small.");
            Assert.True(new FileInfo(scenario.WireframeScreenshotFullPath).Length > 4096, $"{scenario.WireframeScreenshotPath} is too small.");
        }
    }

    [Fact]
    public void AgentComparisonYamlScenarios_MatchYamlDotNetGeneratedContent()
    {
        IReadOnlyDictionary<string, WireframePromptScreen> promptScreens = AgentWireframeComparisonScenarioCatalog.LoadDocumentedScreens()
            .ToDictionary(screen => screen.Id, StringComparer.Ordinal);

        foreach (AgentWireframeComparisonScenario scenario in AgentWireframeComparisonScenarioCatalog.LoadAll())
        {
            WireframePromptScreen promptScreen = promptScreens[scenario.ScreenId];
            string expectedYaml = AgentWireframeComparisonScenarioYaml.Serialize(
                AgentWireframeComparisonScenarioYaml.CreateScenarioDocument(promptScreen));

            Assert.Equal(NormalizeNewlines(expectedYaml), NormalizeNewlines(scenario.RawYaml));
        }
    }

    [Fact]
    public void AgentComparisonYamlScenario_RejectsMissingSchemaAsBadBehavior()
    {
        string yaml = BuildScenarioYamlForTests(resultSchema: string.Empty);

        InvalidDataException failure = Assert.Throws<InvalidDataException>(() =>
            AgentWireframeComparisonScenarioCatalog.LoadFromYamlForTests(yaml));

        Assert.Contains("resultSchema", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AgentComparisonYamlScenario_RejectsMissingImagePathAsBadBehavior()
    {
        string yaml = BuildScenarioYamlForTests(actualScreenshotPath: "RiskyStars.Client/Screenshots/Actual/does-not-exist.png");

        InvalidDataException failure = Assert.Throws<InvalidDataException>(() =>
            AgentWireframeComparisonScenarioCatalog.LoadFromYamlForTests(yaml));

        Assert.Contains("actualScreenshotPath", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AgentComparisonYamlScenario_RejectsMissingRequirementRefsAsBadBehavior()
    {
        string yaml = BuildScenarioYamlForTests(includeRequirements: false);

        InvalidDataException failure = Assert.Throws<InvalidDataException>(() =>
            AgentWireframeComparisonScenarioCatalog.LoadFromYamlForTests(yaml));

        Assert.Contains("functionalRequirements", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AgentComparisonYamlScenario_RejectsMissingAgentsReadmeAsBadBehavior()
    {
        string yaml = BuildScenarioYamlForTests(agentsReadmeFirstPath: "missing-agents-readme.yaml");

        InvalidDataException failure = Assert.Throws<InvalidDataException>(() =>
            AgentWireframeComparisonScenarioCatalog.LoadFromYamlForTests(yaml));

        Assert.Contains("agentsReadmeFirstPath", failure.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("RiskyStars.Tests/AgentWireframeComparisons/ai-action-indicator.yaml")]
    [InlineData("RiskyStars.Tests/AgentWireframeComparisons/ai-visualization-window.yaml")]
    [InlineData("RiskyStars.Tests/AgentWireframeComparisons/combat-event-dialog.yaml")]
    [InlineData("RiskyStars.Tests/AgentWireframeComparisons/combat-hud-overlay.yaml")]
    [InlineData("RiskyStars.Tests/AgentWireframeComparisons/combat-screen.yaml")]
    [InlineData("RiskyStars.Tests/AgentWireframeComparisons/connection-screen.yaml")]
    [InlineData("RiskyStars.Tests/AgentWireframeComparisons/context-menu-manager.yaml")]
    [InlineData("RiskyStars.Tests/AgentWireframeComparisons/continent-zoom-window.yaml")]
    [InlineData("RiskyStars.Tests/AgentWireframeComparisons/create-lobby.yaml")]
    [InlineData("RiskyStars.Tests/AgentWireframeComparisons/debug-info-window.yaml")]
    [InlineData("RiskyStars.Tests/AgentWireframeComparisons/dialog-manager.yaml")]
    [InlineData("RiskyStars.Tests/AgentWireframeComparisons/encyclopedia-window.yaml")]
    [InlineData("RiskyStars.Tests/AgentWireframeComparisons/game-mode-selector.yaml")]
    [InlineData("RiskyStars.Tests/AgentWireframeComparisons/gameplay-hud-legend.yaml")]
    [InlineData("RiskyStars.Tests/AgentWireframeComparisons/gameplay-hud-top-bar.yaml")]
    [InlineData("RiskyStars.Tests/AgentWireframeComparisons/legacy-player-dashboard.yaml")]
    [InlineData("RiskyStars.Tests/AgentWireframeComparisons/lobby-browser.yaml")]
    [InlineData("RiskyStars.Tests/AgentWireframeComparisons/main-menu-connecting.yaml")]
    [InlineData("RiskyStars.Tests/AgentWireframeComparisons/main-menu-settings.yaml")]
    [InlineData("RiskyStars.Tests/AgentWireframeComparisons/main-menu.yaml")]
    [InlineData("RiskyStars.Tests/AgentWireframeComparisons/multiplayer-lobby.yaml")]
    [InlineData("RiskyStars.Tests/AgentWireframeComparisons/player-dashboard-window.yaml")]
    [InlineData("RiskyStars.Tests/AgentWireframeComparisons/server-status-indicator.yaml")]
    [InlineData("RiskyStars.Tests/AgentWireframeComparisons/settings-window.yaml")]
    [InlineData("RiskyStars.Tests/AgentWireframeComparisons/side-panel-container.yaml")]
    [InlineData("RiskyStars.Tests/AgentWireframeComparisons/single-player-lobby.yaml")]
    [InlineData("RiskyStars.Tests/AgentWireframeComparisons/tutorial-mode-window.yaml")]
    [InlineData("RiskyStars.Tests/AgentWireframeComparisons/ui-scale-window.yaml")]
    public async Task AgentComparisonIntegration_RunsYamlScenarioWhenEnabledOtherwiseRecordsIntentionalPass(string scenarioPath)
    {
        AgentWireframeComparisonOptions options = AgentWireframeComparisonConfiguration.Load(
            AgentWireframeComparisonConfiguration.Build(FindRepositoryFile("RiskyStars.Tests")));
        AgentWireframeComparisonScenario scenario = AgentWireframeComparisonScenarioCatalog.Load(scenarioPath);

        if (!options.Enabled)
        {
            Assert.True(string.IsNullOrWhiteSpace(options.ApiKey));
            return;
        }

        options.ValidateForRun();
        string outputDirectory = AgentWireframeComparisonScenarioCatalog.ResolveRepositoryPath(options.OutputDirectory);
        Directory.CreateDirectory(outputDirectory);

        var runner = new AgentWireframeComparisonRunner();
        AgentWireframeComparisonResult result = await runner.CompareAsync(scenario, options, CancellationToken.None);
        AgentWireframeComparisonValidator.Validate(result, scenario.ScreenId);

        string resultPath = Path.Combine(outputDirectory, $"{scenario.ScreenId}.json");
        await File.WriteAllTextAsync(
            resultPath,
            JsonSerializer.Serialize(result, AgentWireframeComparisonJson.IndentedOptions),
            CancellationToken.None);

        await AgentWireframeComparisonReportWriter.WriteAggregateReportAsync(outputDirectory, CancellationToken.None);

        Assert.NotEmpty(result.CorrectElements);
        Assert.NotEmpty(result.IncorrectElements);
        Assert.False(string.IsNullOrWhiteSpace(result.WireframeSuitability!.Why));
    }

    private static string FindRepositoryFile(string relativePath)
    {
        string normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            string candidate = Path.Combine(directory.FullName, normalized);
            if (File.Exists(candidate) || Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not find repository path {relativePath}.");
    }

    private static string BuildScenarioYamlForTests(
        string? resultSchema = AgentWireframeComparisonJson.SchemaJson,
        bool includeRequirements = true,
        string agentsReadmeFirstPath = "AGENTS-README-FIRST.yaml",
        string actualScreenshotPath = "RiskyStars.Client/Screenshots/Actual/debug-info-window.png")
    {
        var document = new AgentWireframeComparisonScenarioDocument
        {
            ScreenId = "debug-info-window",
            Title = "Debug information window",
            Prompt = "Compare the actual HWND screenshot against the prompt wireframe.",
            AgentsReadmeFirstPath = agentsReadmeFirstPath,
            ActualScreenshotPath = actualScreenshotPath,
            WireframeScreenshotPath = "RiskyStars.Client/Wireframes/PromptWireframes/debug-info-window.png",
            ResultSchema = resultSchema
        };

        if (includeRequirements)
        {
            document.FunctionalRequirements =
            [
                new RequirementReference
                {
                    Id = "FR-UI-AUDIT-001",
                    Text = "Every documented screen must be compared against a prompt-rendered wireframe."
                }
            ];
            document.TechnicalRequirements =
            [
                new RequirementReference
                {
                    Id = "TR-UI-AUDIT-001",
                    Text = "The agent must return strict JSON matching the scenario schema."
                }
            ];
        }

        return AgentWireframeComparisonScenarioYaml.Serialize(document);
    }

    private static string NormalizeNewlines(string value)
    {
        return value.Replace("\r\n", "\n", StringComparison.Ordinal);
    }
}

internal sealed class AgentWireframeComparisonOptions
{
    public string Provider { get; set; } = "Grok";

    public string Endpoint { get; set; } = "https://api.x.ai/v1";

    public string Model { get; set; } = "grok-4";

    public string ApiKey { get; set; } = string.Empty;

    public bool Enabled { get; set; }

    public string OutputDirectory { get; set; } = "RiskyStars.Client/Screenshots/AgentComparisons";

    public float Temperature { get; set; }

    public int TimeoutSeconds { get; set; } = 120;

    public Uri EndpointUri => new(Endpoint, UriKind.Absolute);

    public void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(Provider))
        {
            throw new InvalidOperationException("AgentWireframeComparison:Provider is required.");
        }

        if (string.IsNullOrWhiteSpace(Model))
        {
            throw new InvalidOperationException("AgentWireframeComparison:Model is required.");
        }

        if (!Uri.TryCreate(Endpoint, UriKind.Absolute, out Uri? endpoint) ||
            endpoint.Scheme is not ("http" or "https"))
        {
            throw new InvalidOperationException("AgentWireframeComparison:Endpoint must be an absolute HTTP or HTTPS URI.");
        }

        if (string.IsNullOrWhiteSpace(OutputDirectory))
        {
            throw new InvalidOperationException("AgentWireframeComparison:OutputDirectory is required.");
        }

        if (Temperature < 0 || Temperature > 2)
        {
            throw new InvalidOperationException("AgentWireframeComparison:Temperature must be between 0 and 2.");
        }

        if (TimeoutSeconds <= 0)
        {
            throw new InvalidOperationException("AgentWireframeComparison:TimeoutSeconds must be greater than zero.");
        }
    }

    public void ValidateForRun()
    {
        ValidateConfiguration();
        if (Enabled && string.IsNullOrWhiteSpace(ApiKey))
        {
            throw new InvalidOperationException(
                "AgentWireframeComparison is enabled but no API key was configured. Set RISKYSTARS_AgentWireframeComparison__ApiKey or XAI_API_KEY.");
        }
    }
}

internal static class AgentWireframeComparisonConfiguration
{
    public static IConfiguration Build(string basePath)
    {
        return new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddInMemoryCollection(CreateExternalEnvironmentFallbackConfiguration())
            .AddEnvironmentVariables("RISKYSTARS_")
            .AddEnvironmentVariables()
            .Build();
    }

    public static IConfiguration BuildForTests(IReadOnlyDictionary<string, string?> overrides)
    {
        Dictionary<string, string?> defaults = CreateDefaultConfiguration();
        foreach (KeyValuePair<string, string?> pair in overrides)
        {
            string key = pair.Key.StartsWith("RISKYSTARS_", StringComparison.Ordinal)
                ? pair.Key["RISKYSTARS_".Length..].Replace("__", ":", StringComparison.Ordinal)
                : pair.Key.Replace("__", ":", StringComparison.Ordinal);
            defaults[key] = pair.Value;
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(defaults)
            .Build();
    }

    public static IReadOnlyDictionary<string, string?> CreateExternalEnvironmentFallbackConfiguration(
        Func<string, EnvironmentVariableTarget, string?>? environmentReader = null)
    {
        environmentReader ??= Environment.GetEnvironmentVariable;
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        AddEnvironmentFallbackValue(
            values,
            "AgentWireframeComparison:ApiKey",
            "RISKYSTARS_AgentWireframeComparison__ApiKey",
            environmentReader);
        AddEnvironmentFallbackValue(values, "XAI_API_KEY", "XAI_API_KEY", environmentReader);
        AddEnvironmentFallbackValue(values, "ApiKey", "ApiKey", environmentReader);

        return values;
    }

    public static AgentWireframeComparisonOptions Load(IConfiguration configuration)
    {
        var options = configuration.GetSection("AgentWireframeComparison").Get<AgentWireframeComparisonOptions>()
            ?? new AgentWireframeComparisonOptions();

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            options.ApiKey = configuration["XAI_API_KEY"] ?? configuration["ApiKey"] ?? string.Empty;
        }

        if (!options.Enabled && !string.IsNullOrWhiteSpace(options.ApiKey))
        {
            options.Enabled = true;
        }

        options.ValidateConfiguration();
        return options;
    }

    private static Dictionary<string, string?> CreateDefaultConfiguration()
    {
        return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["AgentWireframeComparison:Provider"] = "Grok",
            ["AgentWireframeComparison:Endpoint"] = "https://api.x.ai/v1",
            ["AgentWireframeComparison:Model"] = "grok-4",
            ["AgentWireframeComparison:ApiKey"] = string.Empty,
            ["AgentWireframeComparison:Enabled"] = "false",
            ["AgentWireframeComparison:OutputDirectory"] = "RiskyStars.Client/Screenshots/AgentComparisons",
            ["AgentWireframeComparison:Temperature"] = "0",
            ["AgentWireframeComparison:TimeoutSeconds"] = "300"
        };
    }

    private static void AddEnvironmentFallbackValue(
        IDictionary<string, string?> values,
        string configurationKey,
        string environmentName,
        Func<string, EnvironmentVariableTarget, string?> environmentReader)
    {
        string? machineValue = environmentReader(environmentName, EnvironmentVariableTarget.Machine);
        if (!string.IsNullOrWhiteSpace(machineValue))
        {
            values[configurationKey] = machineValue;
        }

        string? userValue = environmentReader(environmentName, EnvironmentVariableTarget.User);
        if (!string.IsNullOrWhiteSpace(userValue))
        {
            values[configurationKey] = userValue;
        }
    }
}

internal static class AgentWireframeComparisonCatalog
{
    public static string ResolveRepositoryPath(string path) => AgentWireframeComparisonScenarioCatalog.ResolveRepositoryPath(path);
}

internal static class AgentWireframeComparisonScenarioYaml
{
    private const string GlobalWireframeRequirements =
        "Global wireframe style requirements: RiskyStars spatial UI wireframe at 1536x832. Dark starfield/game-map background, thin green panel borders, semi-transparent black panels, terminal-style labels, simple rectangles for controls, and fixed screen-relative geometry. The output is a precise layout baseline, not a Mermaid flowchart.";

    private static readonly ISerializer Serializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
        .Build();

    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public static AgentWireframeComparisonScenarioDocument CreateScenarioDocument(WireframePromptScreen screen)
    {
        return new AgentWireframeComparisonScenarioDocument
        {
            ScreenId = screen.Id,
            Title = screen.Title,
            Prompt = $"""
                Compare the actual HWND screenshot against the prompt wireframe for screen '{screen.Id}'.
                Screen description: {screen.Prompt}
                {GlobalWireframeRequirements}
                Validate whether the actual UI satisfies the listed FR and TR entries, and whether the wireframe is suitable for those requirements.
                """,
            FunctionalRequirements =
            [
                new RequirementReference
                {
                    Id = "FR-UI-AUDIT-001",
                    Text = "Every documented UI screen must have a direct prompt-rendered wireframe at 1536x832 for screenshot comparison."
                },
                new RequirementReference
                {
                    Id = $"FR-UI-SCREEN-{screen.Id.ToUpperInvariant()}",
                    Text = $"The {screen.Title} screen must visually satisfy the documented screen description and expected user-facing UI behavior."
                },
                new RequirementReference
                {
                    Id = "FR-UI-WIREFRAME-SUITABILITY-001",
                    Text = "The audit must evaluate whether the wireframe itself is suitable for validating the screen requirements."
                }
            ],
            TechnicalRequirements =
            [
                new RequirementReference
                {
                    Id = "TR-UI-AUDIT-YAML-001",
                    Text = "Each AI comparison must be driven by a YAML scenario containing prompt, image paths, schema, FRs, TRs, and AGENTS path."
                },
                new RequirementReference
                {
                    Id = "TR-UI-AUDIT-HWND-001",
                    Text = "The actual screenshot must be captured from the RiskyStars client HWND at 1536x832."
                },
                new RequirementReference
                {
                    Id = "TR-UI-AUDIT-SCHEMA-001",
                    Text = "The model result must conform exactly to the resultSchema JSON schema and invalid JSON is test-fail behavior."
                },
                new RequirementReference
                {
                    Id = "TR-UI-AUDIT-AGENTS-001",
                    Text = "The model must consider AGENTS-README-FIRST.yaml content when validating process and correctness requirements."
                }
            ],
            AgentsReadmeFirstPath = "AGENTS-README-FIRST.yaml",
            ActualScreenshotPath = $"RiskyStars.Client/Screenshots/Actual/{screen.Id}.png",
            WireframeScreenshotPath = $"RiskyStars.Client/Wireframes/PromptWireframes/{screen.Id}.png",
            ResultSchema = AgentWireframeComparisonJson.SchemaJson
        };
    }

    public static string Serialize(AgentWireframeComparisonScenarioDocument document)
    {
        return Serializer.Serialize(document);
    }

    public static AgentWireframeComparisonScenarioDocument Deserialize(string yaml)
    {
        return Deserializer.Deserialize<AgentWireframeComparisonScenarioDocument>(yaml)
            ?? throw new InvalidDataException("Agent wireframe comparison YAML document did not deserialize.");
    }
}

internal sealed class AgentWireframeComparisonScenarioDocument
{
    public string ScreenId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    [YamlMember(ScalarStyle = ScalarStyle.Literal)]
    public string Prompt { get; set; } = string.Empty;

    public List<RequirementReference> FunctionalRequirements { get; set; } = [];

    public List<RequirementReference> TechnicalRequirements { get; set; } = [];

    public string AgentsReadmeFirstPath { get; set; } = string.Empty;

    public string ActualScreenshotPath { get; set; } = string.Empty;

    public string WireframeScreenshotPath { get; set; } = string.Empty;

    [YamlMember(ScalarStyle = ScalarStyle.Literal)]
    public string? ResultSchema { get; set; }

    [YamlMember(ScalarStyle = ScalarStyle.Literal)]
    public string? AgentsReadmeFirstContent { get; set; }
}

internal sealed class WireframePromptScreen
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Prompt { get; set; } = string.Empty;
}

internal sealed class WireframePromptCatalog
{
    public List<WireframePromptScreen> Screens { get; set; } = [];
}

internal static class AgentWireframeComparisonScenarioCatalog
{
    public static readonly string[] ScenarioPaths =
    [
        "RiskyStars.Tests/AgentWireframeComparisons/ai-action-indicator.yaml",
        "RiskyStars.Tests/AgentWireframeComparisons/ai-visualization-window.yaml",
        "RiskyStars.Tests/AgentWireframeComparisons/combat-event-dialog.yaml",
        "RiskyStars.Tests/AgentWireframeComparisons/combat-hud-overlay.yaml",
        "RiskyStars.Tests/AgentWireframeComparisons/combat-screen.yaml",
        "RiskyStars.Tests/AgentWireframeComparisons/connection-screen.yaml",
        "RiskyStars.Tests/AgentWireframeComparisons/context-menu-manager.yaml",
        "RiskyStars.Tests/AgentWireframeComparisons/continent-zoom-window.yaml",
        "RiskyStars.Tests/AgentWireframeComparisons/create-lobby.yaml",
        "RiskyStars.Tests/AgentWireframeComparisons/debug-info-window.yaml",
        "RiskyStars.Tests/AgentWireframeComparisons/dialog-manager.yaml",
        "RiskyStars.Tests/AgentWireframeComparisons/encyclopedia-window.yaml",
        "RiskyStars.Tests/AgentWireframeComparisons/game-mode-selector.yaml",
        "RiskyStars.Tests/AgentWireframeComparisons/gameplay-hud-legend.yaml",
        "RiskyStars.Tests/AgentWireframeComparisons/gameplay-hud-top-bar.yaml",
        "RiskyStars.Tests/AgentWireframeComparisons/legacy-player-dashboard.yaml",
        "RiskyStars.Tests/AgentWireframeComparisons/lobby-browser.yaml",
        "RiskyStars.Tests/AgentWireframeComparisons/main-menu-connecting.yaml",
        "RiskyStars.Tests/AgentWireframeComparisons/main-menu-settings.yaml",
        "RiskyStars.Tests/AgentWireframeComparisons/main-menu.yaml",
        "RiskyStars.Tests/AgentWireframeComparisons/multiplayer-lobby.yaml",
        "RiskyStars.Tests/AgentWireframeComparisons/player-dashboard-window.yaml",
        "RiskyStars.Tests/AgentWireframeComparisons/server-status-indicator.yaml",
        "RiskyStars.Tests/AgentWireframeComparisons/settings-window.yaml",
        "RiskyStars.Tests/AgentWireframeComparisons/side-panel-container.yaml",
        "RiskyStars.Tests/AgentWireframeComparisons/single-player-lobby.yaml",
        "RiskyStars.Tests/AgentWireframeComparisons/tutorial-mode-window.yaml",
        "RiskyStars.Tests/AgentWireframeComparisons/ui-scale-window.yaml"
    ];

    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public static IReadOnlyList<AgentWireframeComparisonScenario> LoadAll()
    {
        return ScenarioPaths.Select(Load).ToArray();
    }

    public static AgentWireframeComparisonScenario Load(string scenarioPath)
    {
        string fullPath = ResolveRepositoryPath(scenarioPath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Could not find agent wireframe comparison scenario {scenarioPath}.", fullPath);
        }

        return LoadFromYaml(File.ReadAllText(fullPath), scenarioPath);
    }

    public static AgentWireframeComparisonScenario LoadFromYamlForTests(string yaml)
    {
        return LoadFromYaml(yaml, "test-scenario.yaml");
    }

    public static IReadOnlyList<string> LoadDocumentedScreenIds()
    {
        return LoadDocumentedScreens()
            .Select(screen => screen.Id)
            .ToArray();
    }

    public static IReadOnlyList<WireframePromptScreen> LoadDocumentedScreens()
    {
        string catalogPath = ResolveRepositoryPath("RiskyStars.Client/Wireframes/PromptWireframes/wireframe-prompts.json");
        WireframePromptCatalog? catalog = JsonSerializer.Deserialize<WireframePromptCatalog>(
            File.ReadAllText(catalogPath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (catalog == null || catalog.Screens.Count == 0)
        {
            throw new InvalidDataException("The prompt wireframe catalog must contain documented screens.");
        }

        return catalog.Screens;
    }

    public static string ResolveRepositoryPath(string path)
    {
        if (Path.IsPathFullyQualified(path))
        {
            return path;
        }

        return Path.Combine(FindRepositoryRoot(), path.Replace('/', Path.DirectorySeparatorChar));
    }

    private static AgentWireframeComparisonScenario LoadFromYaml(string yaml, string sourcePath)
    {
        AgentWireframeComparisonScenario scenario = Deserializer.Deserialize<AgentWireframeComparisonScenario>(yaml)
            ?? throw new InvalidDataException($"Scenario {sourcePath} did not deserialize.");
        scenario.SourcePath = sourcePath;
        scenario.RawYaml = yaml;
        scenario.ActualScreenshotFullPath = ResolveRepositoryPath(scenario.ActualScreenshotPath);
        scenario.WireframeScreenshotFullPath = ResolveRepositoryPath(scenario.WireframeScreenshotPath);
        scenario.AgentsReadmeFirstFullPath = ResolveRepositoryPath(scenario.AgentsReadmeFirstPath);
        scenario.AgentsReadmeFirstContent = File.Exists(scenario.AgentsReadmeFirstFullPath)
            ? File.ReadAllText(scenario.AgentsReadmeFirstFullPath)
            : string.Empty;

        Validate(scenario);
        scenario.ModelPayloadYaml = AppendAgentsReadmeFirstNode(scenario.RawYaml, scenario.AgentsReadmeFirstContent);
        return scenario;
    }

    private static void Validate(AgentWireframeComparisonScenario scenario)
    {
        RequireText(scenario.ScreenId, "screenId");
        RequireText(scenario.Prompt, "prompt");
        RequireText(scenario.ActualScreenshotPath, "actualScreenshotPath");
        RequireText(scenario.WireframeScreenshotPath, "wireframeScreenshotPath");
        RequireText(scenario.ResultSchema, "resultSchema");
        RequireText(scenario.AgentsReadmeFirstPath, "agentsReadmeFirstPath");
        Require(scenario.FunctionalRequirements.Count > 0, "functionalRequirements must contain at least one FR.");
        Require(scenario.TechnicalRequirements.Count > 0, "technicalRequirements must contain at least one TR.");
        Require(scenario.FunctionalRequirements.All(item => item.Id.StartsWith("FR-", StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(item.Text)),
            "functionalRequirements must list explicit FR ids and text.");
        Require(scenario.TechnicalRequirements.All(item => item.Id.StartsWith("TR-", StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(item.Text)),
            "technicalRequirements must list explicit TR ids and text.");
        Require(File.Exists(scenario.ActualScreenshotFullPath), $"actualScreenshotPath does not exist: {scenario.ActualScreenshotPath}");
        Require(File.Exists(scenario.WireframeScreenshotFullPath), $"wireframeScreenshotPath does not exist: {scenario.WireframeScreenshotPath}");
        Require(File.Exists(scenario.AgentsReadmeFirstFullPath), $"agentsReadmeFirstPath does not exist: {scenario.AgentsReadmeFirstPath}");
        RequireText(scenario.AgentsReadmeFirstContent, "agentsReadmeFirstContent");
        using JsonDocument _ = JsonDocument.Parse(scenario.ResultSchema);
    }

    private static string AppendAgentsReadmeFirstNode(string rawYaml, string agentsReadmeFirstContent)
    {
        AgentWireframeComparisonScenarioDocument document = AgentWireframeComparisonScenarioYaml.Deserialize(rawYaml);
        document.AgentsReadmeFirstContent = agentsReadmeFirstContent;
        return AgentWireframeComparisonScenarioYaml.Serialize(document);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidDataException(message);
        }
    }

    private static void RequireText(string? value, string fieldName)
    {
        Require(!string.IsNullOrWhiteSpace(value), $"{fieldName} is required.");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "RiskyStars.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the RiskyStars repository root.");
    }
}

internal sealed class AgentWireframeComparisonScenario
{
    public string SourcePath { get; set; } = string.Empty;

    public string RawYaml { get; set; } = string.Empty;

    public string ModelPayloadYaml { get; set; } = string.Empty;

    public string ScreenId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Prompt { get; set; } = string.Empty;

    public List<RequirementReference> FunctionalRequirements { get; set; } = [];

    public List<RequirementReference> TechnicalRequirements { get; set; } = [];

    public string AgentsReadmeFirstPath { get; set; } = string.Empty;

    public string AgentsReadmeFirstFullPath { get; set; } = string.Empty;

    public string AgentsReadmeFirstContent { get; set; } = string.Empty;

    public string ActualScreenshotPath { get; set; } = string.Empty;

    public string ActualScreenshotFullPath { get; set; } = string.Empty;

    public string WireframeScreenshotPath { get; set; } = string.Empty;

    public string WireframeScreenshotFullPath { get; set; } = string.Empty;

    public string ResultSchema { get; set; } = string.Empty;
}

internal sealed class RequirementReference
{
    public string Id { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;
}

internal static class AgentWireframeComparisonPrompt
{
    public static string BuildInstructions()
    {
        return """
        You are the RiskyStars UI wireframe auditor.
        Compare the actual HWND screenshot against the prompt wireframe PNG using the YAML scenario file content.
        Return only valid JSON.
        Return exactly one valid JSON object matching the resultSchema YAML node. Do not wrap the response in Markdown. Do not add prose before or after the JSON.
        Do not omit any required section: screenId, summary, correctElements, incorrectElements, and wireframeSuitability are all required.
        Use the exact screenId provided by the YAML scenario.
        Use the functionalRequirements and technicalRequirements YAML nodes as the traceable FR/TR basis for the audit.
        Use the agentsReadmeFirstPath and appended agentsReadmeFirstContent YAML nodes to validate repository process requirements when they are relevant.
        correctElements must list visible elements in the actual screenshot that satisfy their stated requirements when compared to the wireframe and screen requirements.
        incorrectElements must list every material mismatch, including bounds, nesting, text, missing controls, extra controls, z-order, DPI/scaling differences, and wireframe elements that are not present in the screenshot. Each incorrect element must include whyIncorrect.
        If the actual screenshot appears to match the wireframe closely, still include at least one incorrectElements item for the most important residual risk or validation limitation; this keeps the audit explicit rather than silently declaring perfection.
        wireframeSuitability must analyze whether the wireframe itself is suitable for validating the requirements it was built for. It must include missingRequirements and recommendedWireframeChanges arrays, even when they are empty.
        Use confidence values from 0 to 1.
        Use exactly one of these lowercase severity values: low, medium, high, critical.
        Use exactly one of these lowercase rating values: suitable, partially_suitable, unsuitable.
        Do not use alternate labels such as partially suitable, partial, adequate, ok, or unsuitable_wireframe.
        The first image is the actual HWND screenshot. The second image is the prompt wireframe.
        """;
    }

    public static string BuildUserPrompt(AgentWireframeComparisonScenario scenario)
    {
        return $$"""
        YAML scenario file content:
        ```yaml
        {{scenario.ModelPayloadYaml}}
        ```

        The actualScreenshotPath and wireframeScreenshotPath values identify the two attached images.
        Evaluate both the screenshot-vs-wireframe comparison and whether the prompt wireframe is suitable for validating the stated UI requirements, FRs, TRs, and repository process requirements.
        Return a single JSON object that conforms to the resultSchema node.
        """;
    }
}

internal sealed class AgentWireframeComparisonRunner
{
    public async Task<AgentWireframeComparisonResult> CompareAsync(
        AgentWireframeComparisonScenario scenario,
        AgentWireframeComparisonOptions options,
        CancellationToken cancellationToken)
    {
        options.ValidateForRun();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.TimeoutSeconds));

        var clientOptions = new OpenAIClientOptions
        {
            Endpoint = options.EndpointUri
        };
        var chatClient = new OpenAI.Chat.ChatClient(
            options.Model,
            new ApiKeyCredential(options.ApiKey),
            clientOptions);
        IChatClient aiChatClient = chatClient.AsIChatClient();
        ChatClientAgent agent = aiChatClient.AsAIAgent(
            name: "RiskyStarsWireframeAuditor",
            description: "Compares RiskyStars wireframes to actual screenshots.",
            instructions: AgentWireframeComparisonPrompt.BuildInstructions());

        var message = new ChatMessage(
            ChatRole.User,
            new List<AIContent>
            {
                new TextContent(AgentWireframeComparisonPrompt.BuildUserPrompt(scenario)),
                new DataContent(await File.ReadAllBytesAsync(scenario.ActualScreenshotFullPath, timeout.Token), "image/png")
                {
                    Name = "actual-hwnd-screenshot.png"
                },
                new DataContent(await File.ReadAllBytesAsync(scenario.WireframeScreenshotFullPath, timeout.Token), "image/png")
                {
                    Name = "prompt-wireframe.png"
                }
            });

        var chatOptions = new ChatOptions
        {
            Temperature = options.Temperature,
            ResponseFormat = AgentWireframeComparisonJson.CreateResponseFormat(scenario.ResultSchema)
        };

        AgentResponse<AgentWireframeComparisonResult> response = await agent.RunAsync<AgentWireframeComparisonResult>(
            message,
            session: null,
            serializerOptions: AgentWireframeComparisonJson.JsonOptions,
            options: new ChatClientAgentRunOptions(chatOptions),
            cancellationToken: timeout.Token);

        AgentWireframeComparisonResult result = response.Result
            ?? throw new InvalidDataException("The agent returned no typed wireframe comparison result.");
        AgentWireframeComparisonValidator.Validate(result, scenario.ScreenId);
        return result;
    }
}

internal static class AgentWireframeComparisonJson
{
    public const string SchemaJson = """
        {
          "$schema": "https://json-schema.org/draft/2020-12/schema",
          "type": "object",
          "additionalProperties": false,
          "required": ["screenId", "summary", "correctElements", "incorrectElements", "wireframeSuitability"],
          "properties": {
            "screenId": { "type": "string", "minLength": 1 },
            "summary": { "type": "string", "minLength": 1 },
            "correctElements": {
              "type": "array",
              "minItems": 1,
              "items": {
                "type": "object",
                "additionalProperties": false,
                "required": ["element", "requirement", "evidence", "confidence"],
                "properties": {
                  "element": { "type": "string", "minLength": 1 },
                  "requirement": { "type": "string", "minLength": 1 },
                  "evidence": { "type": "string", "minLength": 1 },
                  "confidence": { "type": "number", "minimum": 0, "maximum": 1 }
                }
              }
            },
            "incorrectElements": {
              "type": "array",
              "minItems": 1,
              "items": {
                "type": "object",
                "additionalProperties": false,
                "required": ["element", "requirement", "expected", "actual", "whyIncorrect", "severity", "confidence"],
                "properties": {
                  "element": { "type": "string", "minLength": 1 },
                  "requirement": { "type": "string", "minLength": 1 },
                  "expected": { "type": "string", "minLength": 1 },
                  "actual": { "type": "string", "minLength": 1 },
                  "whyIncorrect": { "type": "string", "minLength": 1 },
                  "severity": { "type": "string", "enum": ["low", "medium", "high", "critical"] },
                  "confidence": { "type": "number", "minimum": 0, "maximum": 1 }
                }
              }
            },
            "wireframeSuitability": {
              "type": "object",
              "additionalProperties": false,
              "required": ["rating", "why", "missingRequirements", "recommendedWireframeChanges"],
              "properties": {
                "rating": { "type": "string", "enum": ["suitable", "partially_suitable", "unsuitable"] },
                "why": { "type": "string", "minLength": 1 },
                "missingRequirements": {
                  "type": "array",
                  "items": { "type": "string" }
                },
                "recommendedWireframeChanges": {
                  "type": "array",
                  "items": { "type": "string" }
                }
              }
            }
          }
        }
        """;

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = false,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        AllowTrailingCommas = false
    };

    public static readonly JsonSerializerOptions IndentedOptions = new(JsonOptions)
    {
        WriteIndented = true
    };

    public static ChatResponseFormat CreateResponseFormat(string schemaJson)
    {
        return ChatResponseFormat.ForJsonSchema(
            BuildSchema(schemaJson),
            schemaName: "risky_stars_agent_wireframe_comparison",
            schemaDescription: "Strict RiskyStars screenshot-to-wireframe comparison result.");
    }

    private static JsonElement BuildSchema(string schemaJson)
    {
        using JsonDocument document = JsonDocument.Parse(schemaJson);
        return document.RootElement.Clone();
    }
}

internal static class AgentWireframeComparisonValidator
{
    private static readonly HashSet<string> SuitabilityRatings =
    [
        "suitable",
        "partially_suitable",
        "unsuitable"
    ];

    private static readonly HashSet<string> Severities =
    [
        "low",
        "medium",
        "high",
        "critical"
    ];

    public static AgentWireframeComparisonResult ParseAndValidate(string json, string expectedScreenId)
    {
        AgentWireframeComparisonResult? result = JsonSerializer.Deserialize<AgentWireframeComparisonResult>(
            json,
            AgentWireframeComparisonJson.JsonOptions);
        if (result == null)
        {
            throw new JsonException("Agent wireframe comparison JSON deserialized to null.");
        }

        Validate(result, expectedScreenId);
        return result;
    }

    public static void Validate(AgentWireframeComparisonResult result, string expectedScreenId)
    {
        Require(result.ScreenId == expectedScreenId, $"screenId must be '{expectedScreenId}' but was '{result.ScreenId}'.");
        RequireText(result.Summary, "summary");
        Require(result.CorrectElements.Count > 0, "correctElements must contain at least one element.");
        Require(result.IncorrectElements.Count > 0, "incorrectElements must contain at least one element.");
        WireframeSuitability suitability = result.WireframeSuitability
            ?? throw new InvalidDataException("wireframeSuitability is required.");

        for (int i = 0; i < result.CorrectElements.Count; i++)
        {
            CorrectElementComparison item = result.CorrectElements[i];
            RequireText(item.Element, $"correctElements[{i}].element");
            RequireText(item.Requirement, $"correctElements[{i}].requirement");
            RequireText(item.Evidence, $"correctElements[{i}].evidence");
            RequireConfidence(item.Confidence, $"correctElements[{i}].confidence");
        }

        for (int i = 0; i < result.IncorrectElements.Count; i++)
        {
            IncorrectElementComparison item = result.IncorrectElements[i];
            RequireText(item.Element, $"incorrectElements[{i}].element");
            RequireText(item.Requirement, $"incorrectElements[{i}].requirement");
            RequireText(item.Expected, $"incorrectElements[{i}].expected");
            RequireText(item.Actual, $"incorrectElements[{i}].actual");
            RequireText(item.WhyIncorrect, $"incorrectElements[{i}].whyIncorrect");
            Require(Severities.Contains(item.Severity), $"incorrectElements[{i}].severity must be low, medium, high, or critical.");
            RequireConfidence(item.Confidence, $"incorrectElements[{i}].confidence");
        }

        Require(SuitabilityRatings.Contains(suitability.Rating), "wireframeSuitability.rating must be suitable, partially_suitable, or unsuitable.");
        RequireText(suitability.Why, "wireframeSuitability.why");
        Require(suitability.MissingRequirements != null, "wireframeSuitability.missingRequirements is required.");
        Require(suitability.RecommendedWireframeChanges != null, "wireframeSuitability.recommendedWireframeChanges is required.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidDataException(message);
        }
    }

    private static void RequireText(string? value, string fieldName)
    {
        Require(!string.IsNullOrWhiteSpace(value), $"{fieldName} is required.");
    }

    private static void RequireConfidence(double confidence, string fieldName)
    {
        Require(confidence >= 0 && confidence <= 1, $"{fieldName} must be between 0 and 1.");
    }
}

internal sealed class AgentWireframeComparisonResult
{
    [JsonPropertyName("screenId")]
    public string ScreenId { get; set; } = string.Empty;

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    [JsonPropertyName("correctElements")]
    public List<CorrectElementComparison> CorrectElements { get; set; } = [];

    [JsonPropertyName("incorrectElements")]
    public List<IncorrectElementComparison> IncorrectElements { get; set; } = [];

    [JsonPropertyName("wireframeSuitability")]
    public WireframeSuitability? WireframeSuitability { get; set; }
}

internal sealed class CorrectElementComparison
{
    [JsonPropertyName("element")]
    public string Element { get; set; } = string.Empty;

    [JsonPropertyName("requirement")]
    public string Requirement { get; set; } = string.Empty;

    [JsonPropertyName("evidence")]
    public string Evidence { get; set; } = string.Empty;

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }
}

internal sealed class IncorrectElementComparison
{
    [JsonPropertyName("element")]
    public string Element { get; set; } = string.Empty;

    [JsonPropertyName("requirement")]
    public string Requirement { get; set; } = string.Empty;

    [JsonPropertyName("expected")]
    public string Expected { get; set; } = string.Empty;

    [JsonPropertyName("actual")]
    public string Actual { get; set; } = string.Empty;

    [JsonPropertyName("whyIncorrect")]
    public string WhyIncorrect { get; set; } = string.Empty;

    [JsonPropertyName("severity")]
    public string Severity { get; set; } = string.Empty;

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }
}

internal sealed class WireframeSuitability
{
    [JsonPropertyName("rating")]
    public string Rating { get; set; } = string.Empty;

    [JsonPropertyName("why")]
    public string Why { get; set; } = string.Empty;

    [JsonPropertyName("missingRequirements")]
    public List<string> MissingRequirements { get; set; } = [];

    [JsonPropertyName("recommendedWireframeChanges")]
    public List<string> RecommendedWireframeChanges { get; set; } = [];
}

internal sealed class AgentWireframeComparisonReport(IReadOnlyList<AgentWireframeComparisonResult> results)
{
    [JsonPropertyName("generatedAtUtc")]
    public DateTimeOffset GeneratedAtUtc { get; } = DateTimeOffset.UtcNow;

    [JsonPropertyName("results")]
    public IReadOnlyList<AgentWireframeComparisonResult> Results { get; } = results;
}

internal static class AgentWireframeComparisonReportWriter
{
    private const string AggregateReportFileName = "agent-comparison-report.json";

    public static async Task WriteAggregateReportAsync(string outputDirectory, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(outputDirectory);
        var results = new List<AgentWireframeComparisonResult>();

        foreach (string file in Directory.EnumerateFiles(outputDirectory, "*.json"))
        {
            if (string.Equals(Path.GetFileName(file), AggregateReportFileName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Path.GetFileName(file), "agent-comparison-run-report.json", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            AgentWireframeComparisonResult? result = JsonSerializer.Deserialize<AgentWireframeComparisonResult>(
                await File.ReadAllTextAsync(file, cancellationToken),
                AgentWireframeComparisonJson.JsonOptions);
            if (result != null && !string.IsNullOrWhiteSpace(result.ScreenId))
            {
                results.Add(result);
            }
        }

        var report = new AgentWireframeComparisonReport(
            results.OrderBy(result => result.ScreenId, StringComparer.Ordinal).ToArray());
        string reportPath = Path.Combine(outputDirectory, AggregateReportFileName);
        await File.WriteAllTextAsync(
            reportPath,
            JsonSerializer.Serialize(report, AgentWireframeComparisonJson.IndentedOptions),
            cancellationToken);
    }
}

internal static class PngProbe
{
    public static (int Width, int Height) ReadDimensions(string path)
    {
        using var stream = File.OpenRead(path);
        Span<byte> header = stackalloc byte[24];
        int read = stream.Read(header);
        if (read < 24)
        {
            throw new InvalidDataException($"{path} is not a valid PNG.");
        }

        byte[] signature = header[..8].ToArray();
        byte[] expectedSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        if (!signature.SequenceEqual(expectedSignature))
        {
            throw new InvalidDataException($"{path} is not a PNG file.");
        }

        return (ReadBigEndianInt32(header[16..20]), ReadBigEndianInt32(header[20..24]));
    }

    private static int ReadBigEndianInt32(ReadOnlySpan<byte> bytes)
    {
        return (bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3];
    }
}
