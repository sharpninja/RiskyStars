using System.ClientModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace RiskyStars.Tests;

public sealed class AgentInteractionComparisonTests
{
    [Fact]
    public void InteractionComparisonYamlScenario_ContainsFourImagesSchemaRequirementsAndAgentsContent()
    {
        IReadOnlyList<AgentInteractionComparisonScenario> scenarios = AgentInteractionComparisonScenarioCatalog.LoadAll();

        Assert.Contains(scenarios, scenario => scenario.InteractionId == "main-menu-settings-click");
        Assert.Contains(scenarios, scenario => scenario.InteractionId == "main-menu-settings-back-click");
        Assert.Contains(scenarios, scenario => scenario.InteractionId == "main-menu-settings-save-click");

        foreach (AgentInteractionComparisonScenario scenario in scenarios)
        {
            Assert.Contains("before", scenario.Prompt, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("after", scenario.Prompt, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("\"$schema\"", scenario.ResultSchema, StringComparison.Ordinal);
            Assert.Contains("correctStateChanges", scenario.ResultSchema, StringComparison.Ordinal);
            Assert.Contains("incorrectStateChanges", scenario.ResultSchema, StringComparison.Ordinal);
            Assert.NotEmpty(scenario.FunctionalRequirements);
            Assert.NotEmpty(scenario.TechnicalRequirements);
            Assert.All(scenario.FunctionalRequirements, item => Assert.StartsWith("FR-", item.Id, StringComparison.Ordinal));
            Assert.All(scenario.TechnicalRequirements, item => Assert.StartsWith("TR-", item.Id, StringComparison.Ordinal));
            Assert.Equal("AGENTS-README-FIRST.yaml", scenario.AgentsReadmeFirstPath);
            Assert.Contains("Prioritize correctness over speed", scenario.AgentsReadmeFirstContent, StringComparison.Ordinal);
            Assert.Contains("agentsReadmeFirstContent: |", scenario.ModelPayloadYaml, StringComparison.Ordinal);
            Assert.True(File.Exists(scenario.ExpectedBeforeWireframeFullPath), scenario.ExpectedBeforeWireframePath);
            Assert.True(File.Exists(scenario.ExpectedAfterWireframeFullPath), scenario.ExpectedAfterWireframePath);
            Assert.True(File.Exists(scenario.ActualBeforeScreenshotFullPath), scenario.ActualBeforeScreenshotPath);
            Assert.True(File.Exists(scenario.ActualAfterScreenshotFullPath), scenario.ActualAfterScreenshotPath);
            Assert.Equal((1536, 832), PngProbe.ReadDimensions(scenario.ExpectedBeforeWireframeFullPath));
            Assert.Equal((1536, 832), PngProbe.ReadDimensions(scenario.ExpectedAfterWireframeFullPath));
            Assert.Equal((1536, 832), PngProbe.ReadDimensions(scenario.ActualBeforeScreenshotFullPath));
            Assert.Equal((1536, 832), PngProbe.ReadDimensions(scenario.ActualAfterScreenshotFullPath));

            string userPrompt = AgentInteractionComparisonPrompt.BuildUserPrompt(scenario);
            Assert.Contains("YAML interaction scenario file content", userPrompt, StringComparison.Ordinal);
            Assert.Contains(scenario.ModelPayloadYaml, userPrompt, StringComparison.Ordinal);
        }

        string instructions = AgentInteractionComparisonPrompt.BuildInstructions();
        Assert.Contains("first image is the expected before-state wireframe", instructions, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fourth image is the actual after-state", instructions, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("HWND screenshot", instructions, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Return only valid JSON", instructions, StringComparison.Ordinal);
    }

    [Fact]
    public void InteractionResultParser_AcceptsCompleteStrictJson()
    {
        string json = """
        {
          "interactionId": "main-menu-settings-click",
          "summary": "The Settings click transitions from the main menu action list to the command settings panel.",
          "correctStateChanges": [
            {
              "element": "Settings command",
              "requirement": "Clicking Settings must open Command Settings.",
              "expectedBefore": "Settings button is visible in Command Actions.",
              "expectedAfter": "Command Settings panel is visible.",
              "actualBefore": "Settings button is visible.",
              "actualAfter": "Command Settings panel is visible.",
              "evidence": "The after screenshot contains Command Settings and the before screenshot does not.",
              "confidence": 0.95
            }
          ],
          "incorrectStateChanges": [],
          "wireframeSuitability": {
            "rating": "suitable",
            "why": "The before and after wireframes represent the intended interaction state change.",
            "missingRequirements": [],
            "recommendedWireframeChanges": []
          }
        }
        """;

        AgentInteractionComparisonResult result = AgentInteractionComparisonValidator.ParseAndValidate(
            json,
            "main-menu-settings-click");

        Assert.Equal("main-menu-settings-click", result.InteractionId);
        Assert.Single(result.CorrectStateChanges);
        Assert.Empty(result.IncorrectStateChanges);
        Assert.Equal("suitable", result.WireframeSuitability!.Rating);
    }

    [Fact]
    public void InteractionResultParser_RejectsMissingCorrectChangesAsBadBehavior()
    {
        string json = """
        {
          "interactionId": "main-menu-settings-click",
          "summary": "Incomplete result",
          "correctStateChanges": [],
          "incorrectStateChanges": [],
          "wireframeSuitability": {
            "rating": "suitable",
            "why": "It is adequate.",
            "missingRequirements": [],
            "recommendedWireframeChanges": []
          }
        }
        """;

        InvalidDataException failure = Assert.Throws<InvalidDataException>(() =>
            AgentInteractionComparisonValidator.ParseAndValidate(json, "main-menu-settings-click"));

        Assert.Contains("correctStateChanges", failure.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("RiskyStars.Tests/AgentInteractionComparisons/main-menu-settings-click.yaml")]
    [InlineData("RiskyStars.Tests/AgentInteractionComparisons/main-menu-settings-back-click.yaml")]
    [InlineData("RiskyStars.Tests/AgentInteractionComparisons/main-menu-settings-save-click.yaml")]
    public async Task AgentInteractionComparisonIntegration_RunsYamlScenarioWhenEnabledOtherwiseRecordsIntentionalPass(string scenarioPath)
    {
        AgentWireframeComparisonOptions options = AgentWireframeComparisonConfiguration.Load(
            AgentWireframeComparisonConfiguration.Build(AgentInteractionComparisonScenarioCatalog.ResolveRepositoryPath("RiskyStars.Tests")));
        AgentInteractionComparisonScenario scenario = AgentInteractionComparisonScenarioCatalog.Load(scenarioPath);

        if (!options.Enabled)
        {
            Assert.True(string.IsNullOrWhiteSpace(options.ApiKey));
            return;
        }

        options.ValidateForRun();
        string outputDirectory = AgentInteractionComparisonScenarioCatalog.ResolveRepositoryPath(
            Path.Combine(options.OutputDirectory, "Interactions"));
        Directory.CreateDirectory(outputDirectory);

        var runner = new AgentInteractionComparisonRunner();
        AgentInteractionComparisonResult result = await runner.CompareAsync(scenario, options, CancellationToken.None);
        AgentInteractionComparisonValidator.Validate(result, scenario.InteractionId);

        string resultPath = Path.Combine(outputDirectory, $"{scenario.InteractionId}.json");
        await File.WriteAllTextAsync(
            resultPath,
            JsonSerializer.Serialize(result, AgentWireframeComparisonJson.IndentedOptions),
            CancellationToken.None);

        await AgentInteractionComparisonReportWriter.WriteAggregateReportAsync(outputDirectory, CancellationToken.None);

        Assert.NotEmpty(result.CorrectStateChanges);
        Assert.NotNull(result.IncorrectStateChanges);
        Assert.False(string.IsNullOrWhiteSpace(result.WireframeSuitability!.Why));
    }
}

internal static class AgentInteractionComparisonScenarioYaml
{
    private static readonly ISerializer Serializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
        .Build();

    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public static string Serialize(AgentInteractionComparisonScenarioDocument document)
    {
        return Serializer.Serialize(document);
    }

    public static AgentInteractionComparisonScenarioDocument Deserialize(string yaml)
    {
        return Deserializer.Deserialize<AgentInteractionComparisonScenarioDocument>(yaml)
            ?? throw new InvalidDataException("Agent interaction comparison YAML document did not deserialize.");
    }
}

internal sealed class AgentInteractionComparisonScenarioDocument
{
    public string InteractionId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    [YamlMember(ScalarStyle = ScalarStyle.Literal)]
    public string Prompt { get; set; } = string.Empty;

    public List<RequirementReference> FunctionalRequirements { get; set; } = [];

    public List<RequirementReference> TechnicalRequirements { get; set; } = [];

    public string AgentsReadmeFirstPath { get; set; } = string.Empty;

    public string ExpectedBeforeWireframePath { get; set; } = string.Empty;

    public string ExpectedAfterWireframePath { get; set; } = string.Empty;

    public string ActualBeforeScreenshotPath { get; set; } = string.Empty;

    public string ActualAfterScreenshotPath { get; set; } = string.Empty;

    [YamlMember(ScalarStyle = ScalarStyle.Literal)]
    public string? ResultSchema { get; set; }

    [YamlMember(ScalarStyle = ScalarStyle.Literal)]
    public string? AgentsReadmeFirstContent { get; set; }
}

internal static class AgentInteractionComparisonScenarioCatalog
{
    public static readonly string[] ScenarioPaths =
    [
        "RiskyStars.Tests/AgentInteractionComparisons/main-menu-settings-click.yaml",
        "RiskyStars.Tests/AgentInteractionComparisons/main-menu-settings-back-click.yaml",
        "RiskyStars.Tests/AgentInteractionComparisons/main-menu-settings-save-click.yaml"
    ];

    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public static IReadOnlyList<AgentInteractionComparisonScenario> LoadAll()
    {
        return ScenarioPaths.Select(Load).ToArray();
    }

    public static AgentInteractionComparisonScenario Load(string scenarioPath)
    {
        string fullPath = ResolveRepositoryPath(scenarioPath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Could not find agent interaction comparison scenario {scenarioPath}.", fullPath);
        }

        return LoadFromYaml(File.ReadAllText(fullPath), scenarioPath);
    }

    public static string ResolveRepositoryPath(string path)
    {
        if (Path.IsPathFullyQualified(path))
        {
            return path;
        }

        return Path.Combine(FindRepositoryRoot(), path.Replace('/', Path.DirectorySeparatorChar));
    }

    private static AgentInteractionComparisonScenario LoadFromYaml(string yaml, string sourcePath)
    {
        AgentInteractionComparisonScenario scenario = Deserializer.Deserialize<AgentInteractionComparisonScenario>(yaml)
            ?? throw new InvalidDataException($"Interaction scenario {sourcePath} did not deserialize.");
        scenario.SourcePath = sourcePath;
        scenario.RawYaml = yaml;
        scenario.ExpectedBeforeWireframeFullPath = ResolveRepositoryPath(scenario.ExpectedBeforeWireframePath);
        scenario.ExpectedAfterWireframeFullPath = ResolveRepositoryPath(scenario.ExpectedAfterWireframePath);
        scenario.ActualBeforeScreenshotFullPath = ResolveRepositoryPath(scenario.ActualBeforeScreenshotPath);
        scenario.ActualAfterScreenshotFullPath = ResolveRepositoryPath(scenario.ActualAfterScreenshotPath);
        scenario.AgentsReadmeFirstFullPath = ResolveRepositoryPath(scenario.AgentsReadmeFirstPath);
        scenario.AgentsReadmeFirstContent = File.Exists(scenario.AgentsReadmeFirstFullPath)
            ? File.ReadAllText(scenario.AgentsReadmeFirstFullPath)
            : string.Empty;

        Validate(scenario);
        scenario.ModelPayloadYaml = AppendAgentsReadmeFirstNode(scenario.RawYaml, scenario.AgentsReadmeFirstContent);
        return scenario;
    }

    private static void Validate(AgentInteractionComparisonScenario scenario)
    {
        RequireText(scenario.InteractionId, "interactionId");
        RequireText(scenario.Prompt, "prompt");
        RequireText(scenario.ExpectedBeforeWireframePath, "expectedBeforeWireframePath");
        RequireText(scenario.ExpectedAfterWireframePath, "expectedAfterWireframePath");
        RequireText(scenario.ActualBeforeScreenshotPath, "actualBeforeScreenshotPath");
        RequireText(scenario.ActualAfterScreenshotPath, "actualAfterScreenshotPath");
        RequireText(scenario.ResultSchema, "resultSchema");
        RequireText(scenario.AgentsReadmeFirstPath, "agentsReadmeFirstPath");
        Require(scenario.FunctionalRequirements.Count > 0, "functionalRequirements must contain at least one FR.");
        Require(scenario.TechnicalRequirements.Count > 0, "technicalRequirements must contain at least one TR.");
        Require(scenario.FunctionalRequirements.All(item => item.Id.StartsWith("FR-", StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(item.Text)),
            "functionalRequirements must list explicit FR ids and text.");
        Require(scenario.TechnicalRequirements.All(item => item.Id.StartsWith("TR-", StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(item.Text)),
            "technicalRequirements must list explicit TR ids and text.");
        Require(File.Exists(scenario.ExpectedBeforeWireframeFullPath), $"expectedBeforeWireframePath does not exist: {scenario.ExpectedBeforeWireframePath}");
        Require(File.Exists(scenario.ExpectedAfterWireframeFullPath), $"expectedAfterWireframePath does not exist: {scenario.ExpectedAfterWireframePath}");
        Require(File.Exists(scenario.ActualBeforeScreenshotFullPath), $"actualBeforeScreenshotPath does not exist: {scenario.ActualBeforeScreenshotPath}");
        Require(File.Exists(scenario.ActualAfterScreenshotFullPath), $"actualAfterScreenshotPath does not exist: {scenario.ActualAfterScreenshotPath}");
        Require(File.Exists(scenario.AgentsReadmeFirstFullPath), $"agentsReadmeFirstPath does not exist: {scenario.AgentsReadmeFirstPath}");
        RequireText(scenario.AgentsReadmeFirstContent, "agentsReadmeFirstContent");
        using JsonDocument _ = JsonDocument.Parse(scenario.ResultSchema);
    }

    private static string AppendAgentsReadmeFirstNode(string rawYaml, string agentsReadmeFirstContent)
    {
        AgentInteractionComparisonScenarioDocument document = AgentInteractionComparisonScenarioYaml.Deserialize(rawYaml);
        document.AgentsReadmeFirstContent = agentsReadmeFirstContent;
        return AgentInteractionComparisonScenarioYaml.Serialize(document);
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

internal sealed class AgentInteractionComparisonScenario
{
    public string SourcePath { get; set; } = string.Empty;

    public string RawYaml { get; set; } = string.Empty;

    public string ModelPayloadYaml { get; set; } = string.Empty;

    public string InteractionId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Prompt { get; set; } = string.Empty;

    public List<RequirementReference> FunctionalRequirements { get; set; } = [];

    public List<RequirementReference> TechnicalRequirements { get; set; } = [];

    public string AgentsReadmeFirstPath { get; set; } = string.Empty;

    public string AgentsReadmeFirstFullPath { get; set; } = string.Empty;

    public string AgentsReadmeFirstContent { get; set; } = string.Empty;

    public string ExpectedBeforeWireframePath { get; set; } = string.Empty;

    public string ExpectedBeforeWireframeFullPath { get; set; } = string.Empty;

    public string ExpectedAfterWireframePath { get; set; } = string.Empty;

    public string ExpectedAfterWireframeFullPath { get; set; } = string.Empty;

    public string ActualBeforeScreenshotPath { get; set; } = string.Empty;

    public string ActualBeforeScreenshotFullPath { get; set; } = string.Empty;

    public string ActualAfterScreenshotPath { get; set; } = string.Empty;

    public string ActualAfterScreenshotFullPath { get; set; } = string.Empty;

    public string ResultSchema { get; set; } = string.Empty;
}

internal static class AgentInteractionComparisonPrompt
{
    public static string BuildInstructions()
    {
        return """
        You are the RiskyStars UI interaction auditor.
        Compare the expected before/after wireframe transition against the actual before/after HWND screenshots using the YAML interaction scenario file content.
        Return only valid JSON.
        Return exactly one valid JSON object matching the resultSchema YAML node. Do not wrap the response in Markdown. Do not add prose before or after the JSON.
        Do not omit any required section: interactionId, summary, correctStateChanges, incorrectStateChanges, and wireframeSuitability are all required.
        Use the exact interactionId provided by the YAML scenario.
        Use the functionalRequirements and technicalRequirements YAML nodes as the traceable FR/TR basis for the audit.
        Use the agentsReadmeFirstPath and appended agentsReadmeFirstContent YAML nodes to validate repository process requirements when they are relevant.
        correctStateChanges must list every material UI state transition that appears correct in the actual before/after screenshots compared with the wireframes and requirements.
        incorrectStateChanges must list every material mismatch, including a missing before state, missing after state, unchanged UI after click, wrong panel, wrong text, bounds mismatch, z-order error, DPI/scaling difference, or a wireframe that cannot validate the requirement. Use an empty array only when no mismatch is visible.
        wireframeSuitability must analyze whether the before/after wireframes themselves are suitable for validating the interaction requirements. It must include missingRequirements and recommendedWireframeChanges arrays, even when they are empty.
        Use confidence values from 0 to 1.
        Use exactly one of these lowercase severity values: low, medium, high, critical.
        Use exactly one of these lowercase rating values: suitable, partially_suitable, unsuitable.
        The first image is the expected before-state wireframe.
        The second image is the expected after-state wireframe.
        The third image is the actual before-state HWND screenshot.
        The fourth image is the actual after-state HWND screenshot.
        """;
    }

    public static string BuildUserPrompt(AgentInteractionComparisonScenario scenario)
    {
        return $$"""
        YAML interaction scenario file content:
        ```yaml
        {{scenario.ModelPayloadYaml}}
        ```

        The expectedBeforeWireframePath, expectedAfterWireframePath, actualBeforeScreenshotPath, and actualAfterScreenshotPath values identify the four attached images in that order.
        Compare whether the actual UI state transition satisfies the expected before/after wireframes, stated interaction requirements, FRs, TRs, and repository process requirements.
        Return a single JSON object that conforms to the resultSchema node.
        """;
    }
}

internal sealed class AgentInteractionComparisonRunner
{
    public async Task<AgentInteractionComparisonResult> CompareAsync(
        AgentInteractionComparisonScenario scenario,
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
            name: "RiskyStarsInteractionAuditor",
            description: "Compares RiskyStars expected interaction state changes to actual before/after screenshots.",
            instructions: AgentInteractionComparisonPrompt.BuildInstructions());

        var message = new ChatMessage(
            ChatRole.User,
            new List<AIContent>
            {
                new TextContent(AgentInteractionComparisonPrompt.BuildUserPrompt(scenario)),
                new DataContent(await File.ReadAllBytesAsync(scenario.ExpectedBeforeWireframeFullPath, timeout.Token), "image/png")
                {
                    Name = "expected-before-wireframe.png"
                },
                new DataContent(await File.ReadAllBytesAsync(scenario.ExpectedAfterWireframeFullPath, timeout.Token), "image/png")
                {
                    Name = "expected-after-wireframe.png"
                },
                new DataContent(await File.ReadAllBytesAsync(scenario.ActualBeforeScreenshotFullPath, timeout.Token), "image/png")
                {
                    Name = "actual-before-hwnd-screenshot.png"
                },
                new DataContent(await File.ReadAllBytesAsync(scenario.ActualAfterScreenshotFullPath, timeout.Token), "image/png")
                {
                    Name = "actual-after-hwnd-screenshot.png"
                }
            });

        var chatOptions = new ChatOptions
        {
            Temperature = options.Temperature,
            ResponseFormat = AgentWireframeComparisonJson.CreateResponseFormat(scenario.ResultSchema)
        };

        AgentResponse<AgentInteractionComparisonResult> response = await agent.RunAsync<AgentInteractionComparisonResult>(
            message,
            session: null,
            serializerOptions: AgentWireframeComparisonJson.JsonOptions,
            options: new ChatClientAgentRunOptions(chatOptions),
            cancellationToken: timeout.Token);

        AgentInteractionComparisonResult result = response.Result
            ?? throw new InvalidDataException("The agent returned no typed interaction comparison result.");
        AgentInteractionComparisonValidator.Validate(result, scenario.InteractionId);
        return result;
    }
}

internal static class AgentInteractionComparisonValidator
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

    public static AgentInteractionComparisonResult ParseAndValidate(string json, string expectedInteractionId)
    {
        AgentInteractionComparisonResult? result = JsonSerializer.Deserialize<AgentInteractionComparisonResult>(
            json,
            AgentWireframeComparisonJson.JsonOptions);
        if (result == null)
        {
            throw new JsonException("Agent interaction comparison JSON deserialized to null.");
        }

        Validate(result, expectedInteractionId);
        return result;
    }

    public static void Validate(AgentInteractionComparisonResult result, string expectedInteractionId)
    {
        Require(result.InteractionId == expectedInteractionId, $"interactionId must be '{expectedInteractionId}' but was '{result.InteractionId}'.");
        RequireText(result.Summary, "summary");
        Require(result.CorrectStateChanges.Count > 0, "correctStateChanges must contain at least one element.");
        List<IncorrectStateChangeComparison> incorrectStateChanges = result.IncorrectStateChanges
            ?? throw new InvalidDataException("incorrectStateChanges is required.");
        WireframeSuitability suitability = result.WireframeSuitability
            ?? throw new InvalidDataException("wireframeSuitability is required.");

        for (int i = 0; i < result.CorrectStateChanges.Count; i++)
        {
            CorrectStateChangeComparison item = result.CorrectStateChanges[i];
            RequireText(item.Element, $"correctStateChanges[{i}].element");
            RequireText(item.Requirement, $"correctStateChanges[{i}].requirement");
            RequireText(item.ExpectedBefore, $"correctStateChanges[{i}].expectedBefore");
            RequireText(item.ExpectedAfter, $"correctStateChanges[{i}].expectedAfter");
            RequireText(item.ActualBefore, $"correctStateChanges[{i}].actualBefore");
            RequireText(item.ActualAfter, $"correctStateChanges[{i}].actualAfter");
            RequireText(item.Evidence, $"correctStateChanges[{i}].evidence");
            RequireConfidence(item.Confidence, $"correctStateChanges[{i}].confidence");
        }

        for (int i = 0; i < incorrectStateChanges.Count; i++)
        {
            IncorrectStateChangeComparison item = incorrectStateChanges[i];
            RequireText(item.Element, $"incorrectStateChanges[{i}].element");
            RequireText(item.Requirement, $"incorrectStateChanges[{i}].requirement");
            RequireText(item.Expected, $"incorrectStateChanges[{i}].expected");
            RequireText(item.Actual, $"incorrectStateChanges[{i}].actual");
            RequireText(item.WhyIncorrect, $"incorrectStateChanges[{i}].whyIncorrect");
            Require(Severities.Contains(item.Severity), $"incorrectStateChanges[{i}].severity must be low, medium, high, or critical.");
            RequireConfidence(item.Confidence, $"incorrectStateChanges[{i}].confidence");
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

internal sealed class AgentInteractionComparisonResult
{
    [JsonPropertyName("interactionId")]
    public string InteractionId { get; set; } = string.Empty;

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    [JsonPropertyName("correctStateChanges")]
    public List<CorrectStateChangeComparison> CorrectStateChanges { get; set; } = [];

    [JsonPropertyName("incorrectStateChanges")]
    public List<IncorrectStateChangeComparison> IncorrectStateChanges { get; set; } = [];

    [JsonPropertyName("wireframeSuitability")]
    public WireframeSuitability? WireframeSuitability { get; set; }
}

internal sealed class CorrectStateChangeComparison
{
    [JsonPropertyName("element")]
    public string Element { get; set; } = string.Empty;

    [JsonPropertyName("requirement")]
    public string Requirement { get; set; } = string.Empty;

    [JsonPropertyName("expectedBefore")]
    public string ExpectedBefore { get; set; } = string.Empty;

    [JsonPropertyName("expectedAfter")]
    public string ExpectedAfter { get; set; } = string.Empty;

    [JsonPropertyName("actualBefore")]
    public string ActualBefore { get; set; } = string.Empty;

    [JsonPropertyName("actualAfter")]
    public string ActualAfter { get; set; } = string.Empty;

    [JsonPropertyName("evidence")]
    public string Evidence { get; set; } = string.Empty;

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }
}

internal sealed class IncorrectStateChangeComparison
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

internal sealed class AgentInteractionComparisonReport(IReadOnlyList<AgentInteractionComparisonResult> results)
{
    [JsonPropertyName("generatedAtUtc")]
    public DateTimeOffset GeneratedAtUtc { get; } = DateTimeOffset.UtcNow;

    [JsonPropertyName("results")]
    public IReadOnlyList<AgentInteractionComparisonResult> Results { get; } = results;
}

internal static class AgentInteractionComparisonReportWriter
{
    private const string AggregateReportFileName = "agent-interaction-comparison-report.json";

    public static async Task WriteAggregateReportAsync(string outputDirectory, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(outputDirectory);
        var results = new List<AgentInteractionComparisonResult>();

        foreach (string file in Directory.EnumerateFiles(outputDirectory, "*.json"))
        {
            if (string.Equals(Path.GetFileName(file), AggregateReportFileName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            AgentInteractionComparisonResult? result = JsonSerializer.Deserialize<AgentInteractionComparisonResult>(
                await File.ReadAllTextAsync(file, cancellationToken),
                AgentWireframeComparisonJson.JsonOptions);
            if (result != null && !string.IsNullOrWhiteSpace(result.InteractionId))
            {
                results.Add(result);
            }
        }

        var report = new AgentInteractionComparisonReport(
            results.OrderBy(result => result.InteractionId, StringComparer.Ordinal).ToArray());
        string reportPath = Path.Combine(outputDirectory, AggregateReportFileName);
        await File.WriteAllTextAsync(
            reportPath,
            JsonSerializer.Serialize(report, AgentWireframeComparisonJson.IndentedOptions),
            cancellationToken);
    }
}
