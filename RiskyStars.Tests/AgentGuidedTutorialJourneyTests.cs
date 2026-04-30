using System.ClientModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Text.Json.Serialization;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using RiskyStars.Client;
using RiskyStars.Shared;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace RiskyStars.Tests;

[SupportedOSPlatform("windows6.1")]
public sealed class AgentGuidedTutorialJourneyTests
{
    private static readonly SemaphoreSlim GuidedTutorialAgentComparisonRunLock = new(1, 1);
    private static readonly GuidedTutorialAgentComparisonRateLimitCircuitBreaker GuidedTutorialAgentComparisonCircuitBreaker = new();

    [Fact]
    public void TutorialJourneyManifest_ExportsAllFourteenStepsInOrder()
    {
        IReadOnlyList<GuidedTutorialStepDefinition> steps = GuidedTutorialJourneyDefinition.Steps;

        Assert.Equal(14, steps.Count);
        Assert.Equal(
            [
                "sync",
                "turn",
                "select",
                "help",
                "production",
                "dashboard",
                "purchase",
                "reinforcement-phase",
                "reinforcement-target",
                "movement-phase",
                "army",
                "movement",
                "reference",
                "complete"
            ],
            steps.Select(step => step.StepId).ToArray());
        Assert.All(steps, step => Assert.Equal(RiskyStars.Client.RiskyStarsGame.GetExpectedDebugTutorialAction(step.StepId), step.Action));
    }

    [Fact]
    public void TutorialJourneyHighlightTargets_MatchTutorialWindowCompletionMapping()
    {
        foreach (GuidedTutorialStepDefinition step in GuidedTutorialJourneyDefinition.Steps)
        {
            string[] expected = TutorialHighlightTargets
                .ForCompletion(step.Completion)
                .Select(target => target.ToString())
                .ToArray();

            Assert.Equal(expected, step.ExpectedHighlightTargets);
        }
    }

    [Fact]
    public void TutorialJourneyScenarioYaml_ContainsSchemaRequirementsAgentsAndImagePaths()
    {
        GuidedTutorialJourneyArtifactGenerator.WriteScenarioFiles();
        IReadOnlyList<GuidedTutorialAgentComparisonScenario> scenarios =
            GuidedTutorialAgentComparisonScenarioCatalog.LoadAll(requireArtifacts: false);

        Assert.Equal(14, scenarios.Count);
        foreach (GuidedTutorialAgentComparisonScenario scenario in scenarios)
        {
            Assert.Contains("\"$schema\"", scenario.ResultSchema, StringComparison.Ordinal);
            Assert.Contains("highlightValidation", scenario.ResultSchema, StringComparison.Ordinal);
            Assert.Contains("incorrectBehavior", scenario.ResultSchema, StringComparison.Ordinal);
            Assert.Contains("wireframeSuitability", scenario.ResultSchema, StringComparison.Ordinal);
            Assert.NotEmpty(scenario.FunctionalRequirements);
            Assert.NotEmpty(scenario.TechnicalRequirements);
            Assert.Equal("AGENTS-README-FIRST.yaml", scenario.AgentsReadmeFirstPath);
            Assert.Contains("agentsReadmeFirstContent: |", scenario.ModelPayloadYaml, StringComparison.Ordinal);
            Assert.Contains("Prioritize correctness over speed", scenario.ModelPayloadYaml, StringComparison.Ordinal);
            Assert.Contains(scenario.BeforeWireframePath, scenario.ModelPayloadYaml, StringComparison.Ordinal);
            Assert.Contains(scenario.AfterWireframePath, scenario.ModelPayloadYaml, StringComparison.Ordinal);
            Assert.Contains(scenario.BeforeScreenshotPath, scenario.ModelPayloadYaml, StringComparison.Ordinal);
            Assert.Contains(scenario.AfterScreenshotPath, scenario.ModelPayloadYaml, StringComparison.Ordinal);
            string prompt = GuidedTutorialAgentComparisonPrompt.BuildUserPrompt(scenario);
            Assert.Contains("visual-tree JSON text blocks", GuidedTutorialAgentComparisonPrompt.BuildInstructions(), StringComparison.Ordinal);
            Assert.Contains("visual tree JSON text blocks", prompt, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void TutorialJourneyWireframeRenderer_WritesDirectPromptPngsAtLaptopResolution()
    {
        GuidedTutorialJourneyArtifactGenerator.WriteScenarioFiles();
        GuidedTutorialStepDefinition step = GuidedTutorialJourneyDefinition.Steps[2];
        GuidedTutorialCapturedState before = GuidedTutorialCapturedState.ForExpectedStep(step, isComplete: false);
        GuidedTutorialCapturedState after = before with { IsComplete = true, Status = "Status: Objective complete. Press Next." };

        string repositoryRoot = GuidedTutorialAgentComparisonScenarioCatalog.FindRepositoryRoot();
        string beforePath = Path.Combine(repositoryRoot, step.BeforeWireframePath.Replace('/', Path.DirectorySeparatorChar));
        string afterPath = Path.Combine(repositoryRoot, step.AfterWireframePath.Replace('/', Path.DirectorySeparatorChar));

        GuidedTutorialWireframeRenderer.Render(step, before, beforePath, "before");
        GuidedTutorialWireframeRenderer.Render(step, after, afterPath, "after");

        Assert.Equal((1536, 832), PngProbe.ReadDimensions(beforePath));
        Assert.Equal((1536, 832), PngProbe.ReadDimensions(afterPath));
    }

    [Fact]
    public void TutorialVisualTreeSnapshot_IncludesTutorialStateAndRawVisualTree()
    {
        GuidedTutorialStepDefinition step = GuidedTutorialJourneyDefinition.Steps.Single(item => item.StepId == "select");
        GuidedTutorialCapturedState state = GuidedTutorialCapturedState.ForExpectedStep(step, isComplete: false);

        string snapshotJson = GuidedTutorialVisualTreeSnapshot.CreateJson(state, """{"elements":[{"id":"hud.topBar"}]}""");

        using JsonDocument document = JsonDocument.Parse(snapshotJson);
        Assert.Equal("select", document.RootElement.GetProperty("tutorialState").GetProperty("stepId").GetString());
        Assert.Equal("hud.topBar", document.RootElement.GetProperty("visualTree").GetProperty("elements")[0].GetProperty("id").GetString());
    }

    [Fact]
    public void TutorialDeterministicValidator_RejectsMissingHighlightAsBadBehavior()
    {
        GuidedTutorialStepDefinition step = GuidedTutorialJourneyDefinition.Steps.Single(item => item.StepId == "select");
        GuidedTutorialCapturedState before = GuidedTutorialCapturedState.ForExpectedStep(step, isComplete: false);
        GuidedTutorialCapturedState after = before with
        {
            IsComplete = true,
            HighlightTargets = ["TopBar"],
            HighlightBounds = []
        };

        InvalidDataException failure = Assert.Throws<InvalidDataException>(() =>
            GuidedTutorialDeterministicValidator.ValidateAfterState(step, before, after));

        Assert.Contains("highlight", failure.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TutorialDeterministicValidator_RejectsUnchangedGameStateAsBadBehavior()
    {
        GuidedTutorialStepDefinition step = GuidedTutorialJourneyDefinition.Steps.Single(item => item.StepId == "purchase");
        GuidedTutorialCapturedState before = GuidedTutorialCapturedState.ForExpectedStep(step, isComplete: false) with
        {
            OwnArmyCount = 1
        };
        GuidedTutorialCapturedState after = before with
        {
            IsComplete = true,
            OwnArmyCount = 1
        };

        InvalidDataException failure = Assert.Throws<InvalidDataException>(() =>
            GuidedTutorialDeterministicValidator.ValidateAfterState(step, before, after));

        Assert.Contains("army", failure.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TutorialDebugController_RejectsMissingAndInvalidTutorialActions()
    {
        var controller = new ClientDebugController(
            () => new GameUiVisualTree(),
            () => GameUiScaleContext.Create(1536, 832, 1536, 832, 100, 1f),
            (_, _) => ClientDebugActionResult.Ok("focused"),
            invokeTutorialAction: (_, _) => ClientDebugActionResult.Fail("invalid"));

        Assert.False(controller.InvokeTutorialAction("sync", string.Empty).Success);
        Assert.False(controller.InvokeTutorialAction(string.Empty, "wait-world-sync").Success);
        Assert.False(controller.InvokeTutorialAction("sync", "bad-action").Success);
    }

    [Fact]
    public void TutorialAgentResultParser_RejectsIncorrectBehaviorAndUnsuitableWireframe()
    {
        string json = """
        {
          "stepId": "sync",
          "summary": "The UI has a defect.",
          "correctElements": [
            {
              "element": "Tutorial panel",
              "requirement": "The tutorial panel is visible",
              "evidence": "The screenshot shows the panel",
              "confidence": 0.9
            }
          ],
          "incorrectElements": [],
          "correctBehavior": [
            {
              "behavior": "World sync",
              "requirement": "The step should complete after a world snapshot",
              "evidence": "The state changed",
              "confidence": 0.9
            }
          ],
          "incorrectBehavior": [
            {
              "behavior": "Highlight",
              "requirement": "No stale highlights",
              "expected": "TopBar only",
              "actual": "Stale map target",
              "whyIncorrect": "The highlight is stale",
              "severity": "high",
              "confidence": 0.9
            }
          ],
          "highlightValidation": {
            "expectedTargets": ["TopBar"],
            "visibleTargets": ["TopBar"],
            "allExpectedTargetsVisible": true,
            "boundsCorrect": true,
            "notHiddenBehindTutorialPanel": true,
            "noStaleHighlights": true,
            "why": "Highlight is visible"
          },
          "wireframeSuitability": {
            "rating": "unsuitable",
            "why": "It lacks required elements",
            "missingRequirements": ["highlight"],
            "recommendedWireframeChanges": ["add highlight"]
          }
        }
        """;

        InvalidDataException failure = Assert.Throws<InvalidDataException>(() =>
            GuidedTutorialAgentComparisonValidator.ParseAndValidate(json, "sync"));

        Assert.Contains("incorrectBehavior", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TutorialAgentComparisonReportWriter_IncludesModelErrorsWithoutTreatingThemAsResults()
    {
        string outputDirectory = Path.Combine(Path.GetTempPath(), "RiskyStarsGuidedTutorialReport", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outputDirectory);
        GuidedTutorialStepDefinition step = GuidedTutorialJourneyDefinition.Steps[0];
        await File.WriteAllTextAsync(
            Path.Combine(outputDirectory, "01-sync.error.json"),
            JsonSerializer.Serialize(
                new GuidedTutorialAgentComparisonError
                {
                    StepId = step.StepId,
                    StepIndex = step.StepIndex,
                    ScenarioPath = step.ScenarioPath,
                    ErrorType = "ClientResultException",
                    Message = "Status: 503 (Service Unavailable)"
                },
                AgentWireframeComparisonJson.IndentedOptions));

        await GuidedTutorialAgentComparisonReportWriter.WriteAggregateReportAsync(outputDirectory, CancellationToken.None);

        string reportJson = await File.ReadAllTextAsync(Path.Combine(outputDirectory, "guided-tutorial-agent-comparison-report.json"));
        GuidedTutorialAgentComparisonReport? report = JsonSerializer.Deserialize<GuidedTutorialAgentComparisonReport>(
            reportJson,
            AgentWireframeComparisonJson.JsonOptions);

        Assert.NotNull(report);
        Assert.Empty(report.Results);
        GuidedTutorialAgentComparisonError error = Assert.Single(report.Errors);
        Assert.Equal("sync", error.StepId);
        Assert.Contains("503", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TutorialAgentComparisonOutputFiles_ClearStaleScenarioArtifactsBeforeRun()
    {
        string outputDirectory = Path.Combine(Path.GetTempPath(), "RiskyStarsGuidedTutorialOutput", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outputDirectory);
        string resultPath = Path.Combine(outputDirectory, "02-select.json");
        string errorPath = Path.Combine(outputDirectory, "02-select.error.json");
        File.WriteAllText(resultPath, "{}");
        File.WriteAllText(errorPath, "{}");

        GuidedTutorialAgentComparisonOutputFiles.ClearScenarioArtifacts(resultPath, errorPath);

        Assert.False(File.Exists(resultPath));
        Assert.False(File.Exists(errorPath));
    }

    [Fact]
    public async Task TutorialAgentComparisonRetryPolicy_RetriesRateLimitFailures()
    {
        int attempts = 0;
        int delayCount = 0;

        string result = await GuidedTutorialAgentComparisonRetryPolicy.ExecuteAsync(
            _ =>
            {
                attempts++;
                if (attempts == 1)
                {
                    throw new InvalidOperationException("Status: 429 (Too Many Requests)");
                }

                return Task.FromResult("ok");
            },
            CancellationToken.None,
            maxAttempts: 2,
            delayProvider: _ => TimeSpan.Zero,
            delayAsync: (_, _) =>
            {
                delayCount++;
                return Task.CompletedTask;
            });

        Assert.Equal("ok", result);
        Assert.Equal(2, attempts);
        Assert.Equal(1, delayCount);
    }

    [Fact]
    public async Task TutorialAgentComparisonRetryPolicy_DoesNotRetryNonTransientFailures()
    {
        int attempts = 0;

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            GuidedTutorialAgentComparisonRetryPolicy.ExecuteAsync<string>(
                _ =>
                {
                    attempts++;
                    throw new InvalidDataException("schema failure");
                },
                CancellationToken.None,
                maxAttempts: 3,
                delayProvider: _ => TimeSpan.Zero,
                delayAsync: (_, _) => Task.CompletedTask));

        Assert.Equal(1, attempts);
    }

    [Fact]
    public void TutorialAgentComparisonRateLimitCircuitBreaker_ShortCircuitsAfterExhaustion()
    {
        var circuitBreaker = new GuidedTutorialAgentComparisonRateLimitCircuitBreaker();

        Assert.False(circuitBreaker.TryGetBlockedFailure(out _));

        circuitBreaker.MarkBlocked(new InvalidOperationException("Status: 429 (Too Many Requests)"));

        Assert.True(circuitBreaker.TryGetBlockedFailure(out InvalidOperationException? blockedFailure));
        Assert.NotNull(blockedFailure);
        Assert.Contains("prior scenario exhausted rate-limit retries", blockedFailure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TutorialScreenshotValidation_RejectsBlankBlackCaptureAsBadBehavior()
    {
        string outputPath = Path.Combine(Path.GetTempPath(), "RiskyStarsGuidedTutorialBlack", Guid.NewGuid().ToString("N") + ".png");
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        using (var bitmap = new Bitmap(1536, 832, PixelFormat.Format32bppArgb))
        {
            using Graphics graphics = Graphics.FromImage(bitmap);
            graphics.Clear(Color.Black);
            bitmap.Save(outputPath, ImageFormat.Png);
        }

        Assert.False(GuidedTutorialJourneyScreenshotIntegrationTests.ScreenshotHasRenderableContent(outputPath));
    }

    [Theory]
    [MemberData(nameof(GuidedTutorialScenarioPaths))]
    public async Task AgentGuidedTutorialComparisonIntegration_RunsYamlScenarioWhenEnabledOtherwiseRecordsIntentionalPass(string scenarioPath)
    {
        GuidedTutorialJourneyArtifactGenerator.WriteScenarioFiles();
        AgentWireframeComparisonOptions options = AgentWireframeComparisonConfiguration.Load(
            AgentWireframeComparisonConfiguration.Build(GuidedTutorialAgentComparisonScenarioCatalog.ResolveRepositoryPath("RiskyStars.Tests")));
        GuidedTutorialAgentComparisonScenario scenario = GuidedTutorialAgentComparisonScenarioCatalog.Load(scenarioPath);

        if (!options.Enabled)
        {
            Assert.True(string.IsNullOrWhiteSpace(options.ApiKey));
            return;
        }

        bool enteredRunLock = await GuidedTutorialAgentComparisonRunLock.WaitAsync(TimeSpan.FromMinutes(45), CancellationToken.None);
        Assert.True(enteredRunLock, "Timed out waiting to serialize guided tutorial AI comparison model calls.");
        try
        {
            options.ValidateForRun();
            string outputDirectory = GuidedTutorialAgentComparisonScenarioCatalog.ResolveRepositoryPath(
                Path.Combine(options.OutputDirectory, "GuidedTutorial"));
            Directory.CreateDirectory(outputDirectory);

            var runner = new GuidedTutorialAgentComparisonRunner();
            string resultPath = Path.Combine(outputDirectory, $"{scenario.StepIndex:D2}-{scenario.StepId}.json");
            string errorPath = Path.Combine(outputDirectory, $"{scenario.StepIndex:D2}-{scenario.StepId}.error.json");
            GuidedTutorialAgentComparisonOutputFiles.ClearScenarioArtifacts(resultPath, errorPath);

            GuidedTutorialAgentComparisonResult result;
            try
            {
                if (GuidedTutorialAgentComparisonCircuitBreaker.TryGetBlockedFailure(out InvalidOperationException? blockedFailure))
                {
                    throw blockedFailure!;
                }

                result = await GuidedTutorialAgentComparisonRetryPolicy.ExecuteAsync(
                    cancellationToken => runner.CompareAsync(scenario, options, cancellationToken),
                    CancellationToken.None);
            }
            catch (Exception exception)
            {
                if (GuidedTutorialAgentComparisonRetryPolicy.IsTransientRateLimit(exception))
                {
                    GuidedTutorialAgentComparisonCircuitBreaker.MarkBlocked(exception);
                }

                if (File.Exists(resultPath))
                {
                    File.Delete(resultPath);
                }

                await File.WriteAllTextAsync(
                    errorPath,
                    JsonSerializer.Serialize(
                        GuidedTutorialAgentComparisonError.FromException(scenario, exception),
                        AgentWireframeComparisonJson.IndentedOptions),
                    CancellationToken.None);
                await GuidedTutorialAgentComparisonReportWriter.WriteAggregateReportAsync(outputDirectory, CancellationToken.None);
                throw;
            }

            await File.WriteAllTextAsync(
                resultPath,
                JsonSerializer.Serialize(result, AgentWireframeComparisonJson.IndentedOptions),
                CancellationToken.None);
            if (File.Exists(errorPath))
            {
                File.Delete(errorPath);
            }

            await GuidedTutorialAgentComparisonReportWriter.WriteAggregateReportAsync(outputDirectory, CancellationToken.None);
            GuidedTutorialAgentComparisonValidator.Validate(result, scenario.StepId);

            Assert.Empty(result.IncorrectElements);
            Assert.Empty(result.IncorrectBehavior);
            Assert.Equal("suitable", result.WireframeSuitability!.Rating);
        }
        finally
        {
            GuidedTutorialAgentComparisonRunLock.Release();
        }
    }

    public static IEnumerable<object[]> GuidedTutorialScenarioPaths()
    {
        GuidedTutorialJourneyArtifactGenerator.WriteScenarioFiles();
        return GuidedTutorialAgentComparisonScenarioCatalog.ScenarioPaths.Select(path => new object[] { path });
    }
}

[SupportedOSPlatform("windows6.1")]
[Collection("Client debug screenshot tests")]
public sealed class GuidedTutorialJourneyScreenshotIntegrationTests
{
    private const int ScreenshotWidth = 1536;
    private const int ScreenshotHeight = 832;

    static GuidedTutorialJourneyScreenshotIntegrationTests()
    {
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
    }

    [Fact]
    public async Task ValidationSequence_RunsFullGuidedTutorialAndCapturesWireframesScreenshotsAndVisualTrees()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        GuidedTutorialJourneyArtifactGenerator.WriteScenarioFiles();
        string repositoryRoot = GuidedTutorialAgentComparisonScenarioCatalog.FindRepositoryRoot();
        string clientOutputDirectory = Path.Combine(repositoryRoot, "RiskyStars.Client", "bin", "Debug", "net9.0");
        string clientExecutable = Path.Combine(clientOutputDirectory, "RiskyStars.Client.exe");
        Assert.True(File.Exists(clientExecutable), $"Build the client before running tutorial validation. Missing: {clientExecutable}");

        string screenshotDirectory = Path.Combine(repositoryRoot, "RiskyStars.Client", "Screenshots", "TutorialJourney");
        Directory.CreateDirectory(screenshotDirectory);

        int debugPort = GetAvailableTcpPort();
        string settingsPath = Path.Combine(clientOutputDirectory, "settings.json");
        string preferencesPath = Path.Combine(clientOutputDirectory, "window_preferences.json");
        string? originalSettings = ReadExistingFile(settingsPath);
        string? originalPreferences = ReadExistingFile(preferencesPath);

            Process? process = null;
        try
        {
            WriteDeterministicClientSettings(settingsPath);
            File.WriteAllText(preferencesPath, "{}");

            process = StartClient(clientExecutable, clientOutputDirectory, debugPort);
            Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
            Task<string> stderrTask = process.StandardError.ReadToEndAsync();

            IntPtr hwnd = await WaitForMainWindowHandleAsync(process, TimeSpan.FromSeconds(30));
            MoveClientAreaToMonitorWorkArea(hwnd);
            ActivateClientWindowForRendering(hwnd);
            await Task.Delay(TimeSpan.FromMilliseconds(500));

            using var channel = GrpcChannel.ForAddress(
                $"http://localhost:{debugPort.ToString(CultureInfo.InvariantCulture)}",
                new GrpcChannelOptions
                {
                    HttpHandler = new SocketsHttpHandler
                    {
                        EnableMultipleHttp2Connections = true
                    }
                });
            var client = new ClientDebugProtocol.ClientDebugProtocolClient(channel);
            await WaitForDebugProtocolAsync(client, TimeSpan.FromSeconds(30));

            ClientDebugActionResponse seed = await client.SeedTutorialScenarioAsync(new SeedTutorialScenarioRequest());
            Assert.True(seed.Success, seed.Message);

            foreach (GuidedTutorialStepDefinition step in GuidedTutorialJourneyDefinition.Steps)
            {
                GuidedTutorialCapturedState beforeState = GuidedTutorialCapturedState.From(
                    await client.GetTutorialStateAsync(new TutorialStateRequest()));
                Assert.Equal(step.StepId, beforeState.StepId);

                string beforeTree = (await client.DumpVisualTreeAsync(new DumpVisualTreeRequest { IncludeHidden = true })).Json;
                string beforeTreePath = Path.Combine(repositoryRoot, step.BeforeVisualTreePath.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(beforeTreePath)!);
                await File.WriteAllTextAsync(beforeTreePath, GuidedTutorialVisualTreeSnapshot.CreateJson(beforeState, beforeTree));

                string beforeWireframePath = Path.Combine(repositoryRoot, step.BeforeWireframePath.Replace('/', Path.DirectorySeparatorChar));
                GuidedTutorialWireframeRenderer.Render(step, beforeState, beforeWireframePath, "before");

                string beforeScreenshotPath = Path.Combine(repositoryRoot, step.BeforeScreenshotPath.Replace('/', Path.DirectorySeparatorChar));
                CaptureClientArea(hwnd, beforeScreenshotPath);

                ClientDebugActionResponse action = await client.InvokeTutorialActionAsync(new InvokeTutorialActionRequest
                {
                    ExpectedStepId = step.StepId,
                    Action = step.Action
                });
                Assert.True(action.Success, $"{step.StepId}: {action.Message}");

                GuidedTutorialCapturedState afterState = GuidedTutorialCapturedState.From(
                    await client.GetTutorialStateAsync(new TutorialStateRequest()));
                GuidedTutorialDeterministicValidator.ValidateAfterState(step, beforeState, afterState);

                string afterTree = (await client.DumpVisualTreeAsync(new DumpVisualTreeRequest { IncludeHidden = true })).Json;
                string afterTreePath = Path.Combine(repositoryRoot, step.AfterVisualTreePath.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(afterTreePath)!);
                await File.WriteAllTextAsync(afterTreePath, GuidedTutorialVisualTreeSnapshot.CreateJson(afterState, afterTree));

                string afterWireframePath = Path.Combine(repositoryRoot, step.AfterWireframePath.Replace('/', Path.DirectorySeparatorChar));
                GuidedTutorialWireframeRenderer.Render(step, afterState, afterWireframePath, "after");

                string afterScreenshotPath = Path.Combine(repositoryRoot, step.AfterScreenshotPath.Replace('/', Path.DirectorySeparatorChar));
                CaptureClientArea(hwnd, afterScreenshotPath);

                AssertScreenshotPng(beforeWireframePath, ScreenshotWidth, ScreenshotHeight);
                AssertScreenshotPng(afterWireframePath, ScreenshotWidth, ScreenshotHeight);
                AssertScreenshotPng(beforeScreenshotPath, ScreenshotWidth, ScreenshotHeight);
                AssertScreenshotPng(afterScreenshotPath, ScreenshotWidth, ScreenshotHeight);
                AssertDoesNotIncludeNativeWindowChrome(beforeScreenshotPath);
                AssertDoesNotIncludeNativeWindowChrome(afterScreenshotPath);

                if (step.StepId != "complete")
                {
                    ClientDebugActionResponse next = await client.InvokeTutorialActionAsync(new InvokeTutorialActionRequest
                    {
                        ExpectedStepId = step.StepId,
                        Action = "click-next"
                    });
                    Assert.True(next.Success, $"{step.StepId} next: {next.Message}");
                }
            }

            if (process.HasExited)
            {
                string stdout = await stdoutTask;
                string stderr = await stderrTask;
                Assert.Fail($"RiskyStars.Client exited during guided tutorial validation. stdout: {stdout} stderr: {stderr}");
            }
        }
        finally
        {
            if (process != null)
            {
                StopClient(process);
            }

            RestoreFile(settingsPath, originalSettings);
            RestoreFile(preferencesPath, originalPreferences);
        }
    }

    private static Process StartClient(string clientExecutable, string workingDirectory, int debugPort)
    {
        var startInfo = new ProcessStartInfo(clientExecutable)
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        startInfo.Environment["RISKYSTARS_CLIENT_DEBUG_PORT"] = debugPort.ToString(CultureInfo.InvariantCulture);

        Process? process = Process.Start(startInfo);
        Assert.NotNull(process);
        return process;
    }

    private static async Task WaitForDebugProtocolAsync(
        ClientDebugProtocol.ClientDebugProtocolClient client,
        TimeSpan timeout)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < timeout)
        {
            try
            {
                DumpVisualTreeResponse response = await client.DumpVisualTreeAsync(
                    new DumpVisualTreeRequest { IncludeHidden = true },
                    deadline: DateTime.UtcNow.AddSeconds(1)).ResponseAsync;
                if (response.Result.Success)
                {
                    return;
                }
            }
            catch (RpcException exception)
                when (exception.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded or StatusCode.Cancelled)
            {
            }
            catch (IOException)
            {
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }

        throw new TimeoutException("Client debug gRPC protocol did not respond before the timeout.");
    }

    private static async Task<IntPtr> WaitForMainWindowHandleAsync(Process process, TimeSpan timeout)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < timeout)
        {
            process.Refresh();
            if (process.HasExited)
            {
                throw new InvalidOperationException($"RiskyStars.Client exited before creating a window. Exit code: {process.ExitCode}");
            }

            if (process.MainWindowHandle != IntPtr.Zero)
            {
                return process.MainWindowHandle;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }

        throw new TimeoutException("RiskyStars.Client did not expose a main window handle.");
    }

    [SupportedOSPlatform("windows")]
    private static void CaptureClientArea(IntPtr hwnd, string outputPath)
    {
        Exception? lastFailure = null;
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        for (int attempt = 1; attempt <= 20; attempt++)
        {
            BringWindowToFront(hwnd);
            CaptureClientAreaOnce(hwnd, outputPath);

            if (ScreenshotHasRenderableContent(outputPath))
            {
                return;
            }

            lastFailure = new InvalidDataException(
                $"{outputPath} did not contain enough rendered UI content on capture attempt {attempt}.");
            Thread.Sleep(TimeSpan.FromMilliseconds(150));
        }

        throw new InvalidDataException(
            $"Failed to capture a renderable RiskyStars.Client HWND screenshot after 20 attempts: {outputPath}",
            lastFailure);
    }

    [SupportedOSPlatform("windows")]
    private static void BringWindowToFront(IntPtr hwnd)
    {
        ShowWindow(hwnd, ShowWindowCommands.Restore);
        SetWindowPos(
            hwnd,
            HwndTopMost,
            0,
            0,
            0,
            0,
            SetWindowPositionFlags.NoMove | SetWindowPositionFlags.NoSize | SetWindowPositionFlags.ShowWindow);
        SetForegroundWindow(hwnd);
        Thread.Sleep(TimeSpan.FromMilliseconds(100));
        SetWindowPos(
            hwnd,
            HwndNoTopMost,
            0,
            0,
            0,
            0,
            SetWindowPositionFlags.NoMove | SetWindowPositionFlags.NoSize | SetWindowPositionFlags.ShowWindow);
    }

    [SupportedOSPlatform("windows")]
    private static void ActivateClientWindowForRendering(IntPtr hwnd)
    {
        BringWindowToFront(hwnd);
        var activationPoint = ResolveSafeClientActivationPoint(hwnd);
        SetCursorPos(activationPoint.X, activationPoint.Y);
        MouseEvent(MouseEventFlags.LeftDown, 0, 0, 0, UIntPtr.Zero);
        Thread.Sleep(TimeSpan.FromMilliseconds(50));
        MouseEvent(MouseEventFlags.LeftUp, 0, 0, 0, UIntPtr.Zero);
    }

    [SupportedOSPlatform("windows")]
    private static NativePoint ResolveSafeClientActivationPoint(IntPtr hwnd)
    {
        if (!GetClientRect(hwnd, out NativeRect clientRect))
        {
            throw new InvalidOperationException("GetClientRect failed while resolving the activation point.");
        }

        var origin = new NativePoint { X = 0, Y = 0 };
        if (!ClientToScreen(hwnd, ref origin))
        {
            throw new InvalidOperationException("ClientToScreen failed while resolving the activation point.");
        }

        int width = clientRect.Right - clientRect.Left;
        int height = clientRect.Bottom - clientRect.Top;
        return new NativePoint
        {
            X = origin.X + Math.Min(Math.Max(12, width / 2), Math.Max(12, width - 12)),
            Y = origin.Y + Math.Min(12, Math.Max(0, height - 1))
        };
    }

    [SupportedOSPlatform("windows")]
    private static void CaptureClientAreaOnce(IntPtr hwnd, string outputPath)
    {
        IntPtr previousDpiContext = SetThreadDpiAwarenessContext(DpiAwarenessContextPerMonitorAwareV2);
        try
        {
            if (!GetClientRect(hwnd, out NativeRect clientRect))
            {
                throw new InvalidOperationException("GetClientRect failed for the RiskyStars.Client window handle.");
            }

            int width = clientRect.Right - clientRect.Left;
            int height = clientRect.Bottom - clientRect.Top;
            var origin = new NativePoint { X = 0, Y = 0 };
            if (!ClientToScreen(hwnd, ref origin))
            {
                throw new InvalidOperationException("ClientToScreen failed for the RiskyStars.Client window handle.");
            }

            var captureRect = new NativeRect
            {
                Left = origin.X,
                Top = origin.Y,
                Right = origin.X + width,
                Bottom = origin.Y + height
            };
            AssertScreenRectFitsWorkArea(captureRect, GetMonitorWorkArea(hwnd));

            using var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(origin.X, origin.Y, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
            }

            if (width == ScreenshotWidth && height == ScreenshotHeight)
            {
                bitmap.Save(outputPath, ImageFormat.Png);
                return;
            }

            using var normalized = new Bitmap(bitmap, new Size(ScreenshotWidth, ScreenshotHeight));
            normalized.Save(outputPath, ImageFormat.Png);
        }
        finally
        {
            if (previousDpiContext != IntPtr.Zero)
            {
                SetThreadDpiAwarenessContext(previousDpiContext);
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private static void MoveClientAreaToMonitorWorkArea(IntPtr hwnd)
    {
        ShowWindow(hwnd, ShowWindowCommands.Restore);
        SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, SetWindowPositionFlags.NoZOrder | SetWindowPositionFlags.NoSize | SetWindowPositionFlags.ShowWindow);

        if (!GetWindowRect(hwnd, out NativeRect windowRect))
        {
            throw new InvalidOperationException("GetWindowRect failed while positioning the RiskyStars.Client window.");
        }

        NativeRect workArea = GetMonitorWorkArea(hwnd);
        var clientOrigin = new NativePoint { X = 0, Y = 0 };
        if (!ClientToScreen(hwnd, ref clientOrigin))
        {
            throw new InvalidOperationException("ClientToScreen failed while positioning the RiskyStars.Client window.");
        }

        int targetWindowX = windowRect.Left + workArea.Left - clientOrigin.X;
        int targetWindowY = windowRect.Top + workArea.Top - clientOrigin.Y;
        SetWindowPos(hwnd, IntPtr.Zero, targetWindowX, targetWindowY, 0, 0, SetWindowPositionFlags.NoZOrder | SetWindowPositionFlags.NoSize | SetWindowPositionFlags.ShowWindow);
        SetForegroundWindow(hwnd);
    }

    private static void AssertScreenshotPng(string screenshotPath, int expectedWidth, int expectedHeight)
    {
        Assert.True(File.Exists(screenshotPath), $"Screenshot was not written: {screenshotPath}");
        var fileInfo = new FileInfo(screenshotPath);
        Assert.True(fileInfo.Length > 1024, $"{screenshotPath} is too small to be a real PNG capture.");

        byte[] bytes = File.ReadAllBytes(screenshotPath);
        Assert.Equal([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A], bytes.Take(8).ToArray());
        Assert.Equal(expectedWidth, ReadBigEndianInt32(bytes, 16));
        Assert.Equal(expectedHeight, ReadBigEndianInt32(bytes, 20));
        Assert.True(ScreenshotHasRenderableContent(screenshotPath), $"{screenshotPath} appears to be a blank or non-rendered HWND capture.");
    }

    internal static bool ScreenshotHasRenderableContent(string screenshotPath)
    {
        using var bitmap = new Bitmap(screenshotPath);
        int left = Math.Min(8, bitmap.Width / 8);
        int top = Math.Min(8, bitmap.Height / 8);
        int right = Math.Max(left + 1, bitmap.Width - left);
        int bottom = Math.Max(top + 1, bitmap.Height - top);
        int stepX = Math.Max(1, (right - left) / 160);
        int stepY = Math.Max(1, (bottom - top) / 100);
        int signalSamples = 0;
        var colorBuckets = new HashSet<int>();

        for (int y = top; y < bottom; y += stepY)
        {
            for (int x = left; x < right; x += stepX)
            {
                Color color = bitmap.GetPixel(x, y);
                int max = Math.Max(color.R, Math.Max(color.G, color.B));
                int min = Math.Min(color.R, Math.Min(color.G, color.B));
                int spread = max - min;
                if (max > 45 || spread > 20)
                {
                    signalSamples++;
                    int bucket = ((color.R / 16) << 8) | ((color.G / 16) << 4) | (color.B / 16);
                    colorBuckets.Add(bucket);
                }
            }
        }

        return signalSamples > 200 && colorBuckets.Count > 8;
    }

    [SupportedOSPlatform("windows")]
    private static void AssertDoesNotIncludeNativeWindowChrome(string screenshotPath)
    {
        using var bitmap = new Bitmap(screenshotPath);
        int sampledRows = Math.Min(24, bitmap.Height);
        long red = 0;
        long green = 0;
        long blue = 0;
        for (int y = 0; y < sampledRows; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                Color color = bitmap.GetPixel(x, y);
                red += color.R;
                green += color.G;
                blue += color.B;
            }
        }

        double count = sampledRows * bitmap.Width;
        double averageRed = red / count;
        double averageGreen = green / count;
        double averageBlue = blue / count;
        double averageBrightness = (averageRed + averageGreen + averageBlue) / 3.0;
        double channelSpread = Math.Max(averageRed, Math.Max(averageGreen, averageBlue)) - Math.Min(averageRed, Math.Min(averageGreen, averageBlue));

        bool looksLikeNativeChrome = averageBrightness > 45 && averageBrightness < 95 && channelSpread < 12;
        Assert.False(looksLikeNativeChrome, $"{screenshotPath} appears to include the native Windows title bar.");
    }

    private static void AssertScreenRectFitsWorkArea(NativeRect captureRect, NativeRect workArea)
    {
        if (captureRect.Left < workArea.Left ||
            captureRect.Top < workArea.Top ||
            captureRect.Right > workArea.Right ||
            captureRect.Bottom > workArea.Bottom)
        {
            throw new InvalidOperationException("The HWND client capture rectangle extends outside the monitor work area.");
        }
    }

    private static int ReadBigEndianInt32(byte[] bytes, int offset)
    {
        return (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];
    }

    private static int GetAvailableTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static void WriteDeterministicClientSettings(string settingsPath)
    {
        File.WriteAllText(
            settingsPath,
            $$"""
            {
              "Theme": {
                "AccentColor": "Classic Green",
                "WarningColor": "Ivory",
                "FontStyle": "Standard",
                "FontScalePercent": 100,
                "PaddingScalePercent": 100,
                "FramePaddingPercent": 100,
                "ContrastPercent": 100
              },
              "ServerAddress": "http://localhost:5000",
              "ResolutionWidth": {{ScreenshotWidth.ToString(CultureInfo.InvariantCulture)}},
              "ResolutionHeight": {{ScreenshotHeight.ToString(CultureInfo.InvariantCulture)}},
              "UiScalePercent": 100,
              "WindowMode": "Normal",
              "Fullscreen": false,
              "VSync": true,
              "TargetFrameRate": 60,
              "MasterVolume": 1,
              "MusicVolume": 0.7,
              "SfxVolume": 0.8,
              "CameraPanSpeed": 5,
              "CameraZoomSpeed": 0.1,
              "InvertCameraZoom": false,
              "ShowDebugInfo": false,
              "ShowFPS": true,
              "MapCamera": {
                "HasSavedView": false,
                "PositionX": 0,
                "PositionY": 0,
                "Zoom": 1
              }
            }
            """);
    }

    private static string? ReadExistingFile(string path)
    {
        return File.Exists(path) ? File.ReadAllText(path) : null;
    }

    private static void RestoreFile(string path, string? originalContent)
    {
        if (originalContent == null)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            return;
        }

        File.WriteAllText(path, originalContent);
    }

    private static void StopClient(Process process)
    {
        try
        {
            if (process.HasExited)
            {
                return;
            }

            process.CloseMainWindow();
            if (!process.WaitForExit(5000))
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(5000);
            }
        }
        finally
        {
            process.Dispose();
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetClientRect(IntPtr hWnd, out NativeRect lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ClientToScreen(IntPtr hWnd, ref NativePoint lpPoint);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(IntPtr hWnd, out NativeRect lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref NativeMonitorInfo monitorInfo);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll", EntryPoint = "mouse_event")]
    private static extern void MouseEvent(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern IntPtr SetThreadDpiAwarenessContext(IntPtr dpiContext);

    private static readonly IntPtr DpiAwarenessContextPerMonitorAwareV2 = new(-4);
    private static readonly IntPtr HwndTopMost = new(-1);
    private static readonly IntPtr HwndNoTopMost = new(-2);

    private static class ShowWindowCommands
    {
        public const int Restore = 9;
    }

    private static class SetWindowPositionFlags
    {
        public const uint NoSize = 0x0001;
        public const uint NoMove = 0x0002;
        public const uint NoZOrder = 0x0004;
        public const uint ShowWindow = 0x0040;
    }

    private static class MouseEventFlags
    {
        public const uint LeftDown = 0x0002;
        public const uint LeftUp = 0x0004;
    }

    private static class MonitorDefaults
    {
        public const uint MonitorDefaultToNearest = 0x00000002;
    }

    [SupportedOSPlatform("windows")]
    private static NativeRect GetMonitorWorkArea(IntPtr hwnd)
    {
        IntPtr monitor = MonitorFromWindow(hwnd, MonitorDefaults.MonitorDefaultToNearest);
        if (monitor == IntPtr.Zero)
        {
            throw new InvalidOperationException("MonitorFromWindow failed for the RiskyStars.Client window.");
        }

        var info = new NativeMonitorInfo
        {
            Size = Marshal.SizeOf<NativeMonitorInfo>()
        };
        if (!GetMonitorInfo(monitor, ref info))
        {
            throw new InvalidOperationException("GetMonitorInfo failed for the RiskyStars.Client window monitor.");
        }

        return info.WorkArea;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct NativeMonitorInfo
    {
        public int Size;
        public NativeRect MonitorArea;
        public NativeRect WorkArea;
        public uint Flags;
    }
}

internal sealed record GuidedTutorialStepDefinition(
    int StepIndex,
    string StepId,
    string Title,
    string Objective,
    TutorialStepCompletion Completion,
    string Action,
    IReadOnlyList<string> ExpectedHighlightTargets)
{
    public string Prefix => $"{StepIndex + 1:D2}-{StepId}";

    public string BeforeWireframePath => $"RiskyStars.Client/Wireframes/TutorialJourney/{Prefix}-before.png";

    public string AfterWireframePath => $"RiskyStars.Client/Wireframes/TutorialJourney/{Prefix}-after.png";

    public string BeforeScreenshotPath => $"RiskyStars.Client/Screenshots/TutorialJourney/{Prefix}-before.png";

    public string AfterScreenshotPath => $"RiskyStars.Client/Screenshots/TutorialJourney/{Prefix}-after.png";

    public string BeforeVisualTreePath => $"RiskyStars.Client/Screenshots/TutorialJourney/{Prefix}-before.visual-tree.json";

    public string AfterVisualTreePath => $"RiskyStars.Client/Screenshots/TutorialJourney/{Prefix}-after.visual-tree.json";

    public string ScenarioPath => $"RiskyStars.Tests/AgentGuidedTutorialComparisons/{Prefix}.yaml";
}

internal static class GuidedTutorialJourneyDefinition
{
    public static IReadOnlyList<GuidedTutorialStepDefinition> Steps { get; } =
        TutorialModeWindow.AllSteps
            .Select((step, index) => new GuidedTutorialStepDefinition(
                index,
                step.Id,
                step.Title,
                step.Objective,
                step.Completion,
                RiskyStars.Client.RiskyStarsGame.GetExpectedDebugTutorialAction(step.Id),
                TutorialHighlightTargets.ForCompletion(step.Completion).Select(target => target.ToString()).ToArray()))
            .ToArray();
}

internal static class GuidedTutorialJourneyArtifactGenerator
{
    public static void WriteScenarioFiles()
    {
        string root = GuidedTutorialAgentComparisonScenarioCatalog.FindRepositoryRoot();
        foreach (GuidedTutorialStepDefinition step in GuidedTutorialJourneyDefinition.Steps)
        {
            string path = Path.Combine(root, step.ScenarioPath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var document = GuidedTutorialAgentComparisonScenarioDocument.FromStep(step);
            File.WriteAllText(path, GuidedTutorialAgentComparisonScenarioYaml.Serialize(document));
        }
    }
}

internal static class GuidedTutorialVisualTreeSnapshot
{
    private static readonly JsonSerializerOptions SnapshotJsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static string CreateJson(GuidedTutorialCapturedState state, string visualTreeJson)
    {
        using JsonDocument visualTree = JsonDocument.Parse(visualTreeJson);
        return JsonSerializer.Serialize(
            new
            {
                tutorialState = state,
                visualTree = visualTree.RootElement.Clone()
            },
            SnapshotJsonOptions);
    }
}

[SupportedOSPlatform("windows6.1")]
internal static class GuidedTutorialWireframeRenderer
{
    private const int Width = 1536;
    private const int Height = 832;

    public static void Render(GuidedTutorialStepDefinition step, GuidedTutorialCapturedState state, string outputPath, string phase)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        using var bitmap = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        graphics.Clear(Color.FromArgb(8, 8, 14));

        using var panelBrush = new SolidBrush(Color.FromArgb(235, 0, 0, 0));
        using var sideBrush = new SolidBrush(Color.FromArgb(24, 31, 38));
        using var mapBrush = new SolidBrush(Color.FromArgb(12, 12, 20));
        using var accentPen = new Pen(Color.FromArgb(174, 222, 116), 2);
        using var highlightPen = new Pen(Color.White, 3);
        using var textBrush = new SolidBrush(Color.FromArgb(220, 220, 220));
        using var accentBrush = new SolidBrush(Color.FromArgb(174, 222, 116));
        using var dimBrush = new SolidBrush(Color.FromArgb(120, 140, 120));
        using var titleFont = new Font("Consolas", 24, FontStyle.Regular, GraphicsUnit.Pixel);
        using var bodyFont = new Font("Consolas", 16, FontStyle.Regular, GraphicsUnit.Pixel);
        using var smallFont = new Font("Consolas", 13, FontStyle.Regular, GraphicsUnit.Pixel);

        var topBar = new Rectangle(0, 0, Width, 92);
        var leftDock = new Rectangle(0, 92, 200, Height - 92);
        var rightDock = new Rectangle(Width - 190, 92, 190, Height - 92);
        var mapViewport = new Rectangle(200, 92, Width - 390, Height - 92);

        graphics.FillRectangle(sideBrush, leftDock);
        graphics.FillRectangle(sideBrush, rightDock);
        graphics.FillRectangle(panelBrush, topBar);
        graphics.DrawRectangle(accentPen, 0, 0, Width - 1, topBar.Height);
        graphics.FillRectangle(mapBrush, mapViewport);

        DrawTopBar(graphics, topBar, state, accentPen, textBrush, accentBrush, bodyFont, smallFont);
        DrawDockSlots(graphics, leftDock, smallFont, dimBrush);
        DrawDockSlots(graphics, rightDock, smallFont, dimBrush);
        DrawMap(graphics, state, mapViewport, bodyFont, smallFont);
        DrawMapKey(graphics, Width - 180, 165, accentPen, textBrush, titleFont, smallFont);

        if (state.SelectionType != "None")
        {
            DrawSelectionPanel(graphics, state, Width - 180, 352, accentPen, panelBrush, textBrush, titleFont, smallFont);
        }

        if (state.HelpVisible)
        {
            DrawHelpPanel(graphics, panelBrush, accentPen, textBrush, titleFont, smallFont);
        }

        if (state.DashboardVisible)
        {
            DrawDashboard(graphics, state, panelBrush, accentPen, textBrush, accentBrush, titleFont, smallFont);
        }

        if (state.EncyclopediaVisible)
        {
            DrawEncyclopedia(graphics, panelBrush, accentPen, textBrush, accentBrush, titleFont, smallFont);
        }

        if (state.ContextMenuOpen)
        {
            DrawContextMenu(graphics, panelBrush, accentPen, textBrush, smallFont);
        }

        if (state.TutorialVisible)
        {
            DrawTutorialWindow(graphics, step, state, phase, panelBrush, accentPen, textBrush, accentBrush, dimBrush, titleFont, bodyFont, smallFont);
        }

        DrawHighlightBounds(graphics, state, highlightPen, smallFont);

        bitmap.Save(outputPath, ImageFormat.Png);
    }

    private static void DrawTopBar(
        Graphics graphics,
        Rectangle bounds,
        GuidedTutorialCapturedState state,
        Pen accentPen,
        Brush textBrush,
        Brush accentBrush,
        Font bodyFont,
        Font smallFont)
    {
        graphics.DrawString("Turn 1 | " + state.Phase + " Phase | Active: Cadet", bodyFont, textBrush, bounds.Left + 14, bounds.Top + 18);
        graphics.DrawString("Your turn: Produce resources or advance to purchasing.", smallFont, accentBrush, bounds.Right - 385, bounds.Top + 20);
        DrawResourceBox(graphics, "POP " + state.Population + " (+2)", bounds.Left + 14, bounds.Top + 56, accentPen, textBrush, smallFont);
        DrawResourceBox(graphics, "MET " + state.Metal + " (+1)", bounds.Left + 136, bounds.Top + 56, accentPen, textBrush, smallFont);
        DrawResourceBox(graphics, "FUEL " + state.Fuel + " (+1)", bounds.Left + 258, bounds.Top + 56, accentPen, textBrush, smallFont);
        graphics.DrawString(
            "Panels F1 Dbg:Off F2 Cmd:" + (state.DashboardVisible ? "On" : "Off")
            + " F5 Ref:" + (state.EncyclopediaVisible ? "On" : "Off")
            + " F6 Tut:" + (state.TutorialVisible ? "On" : "Off")
            + " H Help",
            smallFont,
            accentBrush,
            bounds.Right - 520,
            bounds.Top + 56);
    }

    private static void DrawResourceBox(Graphics graphics, string text, int x, int y, Pen accentPen, Brush textBrush, Font font)
    {
        var bounds = new Rectangle(x, y, 110, 34);
        graphics.DrawRectangle(accentPen, bounds);
        graphics.DrawString(text, font, textBrush, bounds.Left + 9, bounds.Top + 9);
    }

    private static void DrawDockSlots(Graphics graphics, Rectangle dock, Font font, Brush dimBrush)
    {
        using var slotPen = new Pen(Color.FromArgb(60, 90, 100, 105), 1);
        for (int i = 0; i < 3; i++)
        {
            graphics.DrawRectangle(slotPen, dock.Left + 8 + i * 42, dock.Top + 8, 30, 28);
        }

        graphics.DrawString("dock", font, dimBrush, dock.Left + 8, dock.Bottom - 26);
    }

    private static void DrawMap(Graphics graphics, GuidedTutorialCapturedState state, Rectangle mapViewport, Font bodyFont, Font smallFont)
    {
        using var lanePen = new Pen(Color.FromArgb(150, 170, 170, 170), 2);
        using var viewportPen = new Pen(Color.FromArgb(75, 174, 222, 116), 1);
        graphics.DrawRectangle(viewportPen, mapViewport);
        graphics.DrawLine(lanePen, 800, 385, 1120, 585);
        DrawSystem(graphics, 790, 355, "Arcturus", bodyFont, smallFont);
        DrawSystem(graphics, 1170, 610, "Canopus", bodyFont, smallFont);
        graphics.FillRectangle(Brushes.White, 818, 385, 7, 7);
        graphics.FillRectangle(Brushes.White, 1210, 632, 7, 7);

        if (state.SelectionType != "None")
        {
            using var selectionPen = new Pen(Color.White, 2);
            graphics.DrawRectangle(selectionPen, 760, 430, 92, 78);
            graphics.DrawString("selected " + state.SelectionType, smallFont, Brushes.White, 760, 512);
        }
    }

    private static void DrawMapKey(Graphics graphics, int x, int y, Pen accentPen, Brush textBrush, Font titleFont, Font smallFont)
    {
        var bounds = new Rectangle(x, y, 165, 176);
        graphics.DrawRectangle(accentPen, bounds);
        graphics.DrawString("Map Key", titleFont, textBrush, bounds.Left + 15, bounds.Top + 14);
        using var keyLinePen = new Pen(Color.FromArgb(150, 174, 222, 116), 2);
        graphics.DrawLine(keyLinePen, bounds.Left + 16, bounds.Top + 72, bounds.Left + 34, bounds.Top + 72);
        graphics.DrawString("System orbit", smallFont, textBrush, bounds.Left + 46, bounds.Top + 63);
        graphics.FillRectangle(Brushes.LightSteelBlue, bounds.Left + 16, bounds.Top + 94, 10, 10);
        graphics.DrawString("Stellar body", smallFont, textBrush, bounds.Left + 46, bounds.Top + 87);
        graphics.FillRectangle(Brushes.White, bounds.Left + 16, bounds.Top + 121, 8, 8);
        graphics.DrawString("Region marker", smallFont, textBrush, bounds.Left + 46, bounds.Top + 113);
        graphics.DrawRectangle(Pens.LightGreen, bounds.Left + 16, bounds.Top + 147, 10, 10);
        graphics.DrawString("Lane mouth", smallFont, textBrush, bounds.Left + 46, bounds.Top + 139);
    }

    private static void DrawSelectionPanel(
        Graphics graphics,
        GuidedTutorialCapturedState state,
        int x,
        int y,
        Pen accentPen,
        Brush panelBrush,
        Brush textBrush,
        Font titleFont,
        Font smallFont)
    {
        var bounds = new Rectangle(x, y, 165, 166);
        graphics.FillRectangle(panelBrush, bounds);
        graphics.DrawRectangle(accentPen, bounds);
        string title = state.SelectionType switch
        {
            "Army" => "Selected Army",
            "Region" => "Selected Region",
            "HyperspaceLaneMouth" => "Selected Lane",
            "StellarBody" => "Selected Body",
            _ => "Selected Item"
        };
        graphics.DrawString(title, titleFont, textBrush, new RectangleF(bounds.Left + 12, bounds.Top + 12, 145, 34));
        graphics.DrawString("Name: " + DisplaySelectionName(state), smallFont, textBrush, bounds.Left + 12, bounds.Top + 60);
        graphics.DrawString("Owner: " + DisplayOwner(state), smallFont, textBrush, bounds.Left + 12, bounds.Top + 88);
        graphics.DrawString("Type: " + state.SelectionType, smallFont, textBrush, bounds.Left + 12, bounds.Top + 116);
    }

    private static string DisplaySelectionName(GuidedTutorialCapturedState state)
    {
        return state.SelectionType switch
        {
            "Army" => "Cadet Army",
            "Region" => "Selected region",
            "HyperspaceLaneMouth" => "Selected lane mouth",
            "StellarBody" => "Canopus b",
            _ => "None"
        };
    }

    private static string DisplayOwner(GuidedTutorialCapturedState state)
    {
        return state.SelectedOwnerId == state.CurrentPlayerId ? "Cadet" : "Unowned";
    }

    private static void DrawHelpPanel(Graphics graphics, Brush panelBrush, Pen accentPen, Brush textBrush, Font titleFont, Font smallFont)
    {
        var bounds = new Rectangle(980, 118, 340, 230);
        graphics.FillRectangle(panelBrush, bounds);
        graphics.DrawRectangle(accentPen, bounds);
        graphics.DrawString("Shortcut Sheet", titleFont, textBrush, bounds.Left + 14, bounds.Top + 14);
        graphics.DrawString("F1 Debug\nF2 Command Dashboard\nF5 Encyclopedia\nF6 Guided Tutorial\nMouse wheel zoom\nRight-drag pan", smallFont, textBrush, bounds.Left + 18, bounds.Top + 62);
    }

    private static void DrawDashboard(
        Graphics graphics,
        GuidedTutorialCapturedState state,
        Brush panelBrush,
        Pen accentPen,
        Brush textBrush,
        Brush accentBrush,
        Font titleFont,
        Font smallFont)
    {
        var bounds = new Rectangle(800, 168, 360, 360);
        graphics.FillRectangle(panelBrush, bounds);
        graphics.DrawRectangle(accentPen, bounds);
        graphics.DrawString("Command Dashboard", titleFont, textBrush, bounds.Left + 18, bounds.Top + 18);
        graphics.DrawString("Resources", smallFont, accentBrush, bounds.Left + 22, bounds.Top + 70);
        graphics.DrawString($"Population {state.Population}\nMetal {state.Metal}\nFuel {state.Fuel}\nOwn armies {state.OwnArmyCount}", smallFont, textBrush, bounds.Left + 22, bounds.Top + 100);
        DrawButton(graphics, "Buy starter army", bounds.Left + 22, bounds.Top + 230, 210, 38, accentPen, textBrush, smallFont);
        DrawButton(graphics, "Advance phase", bounds.Left + 22, bounds.Top + 280, 210, 38, accentPen, textBrush, smallFont);
    }

    private static void DrawEncyclopedia(
        Graphics graphics,
        Brush panelBrush,
        Pen accentPen,
        Brush textBrush,
        Brush accentBrush,
        Font titleFont,
        Font smallFont)
    {
        var bounds = new Rectangle(805, 120, 410, 460);
        graphics.FillRectangle(panelBrush, bounds);
        graphics.DrawRectangle(accentPen, bounds);
        graphics.DrawString("In-Game Encyclopedia", titleFont, textBrush, bounds.Left + 18, bounds.Top + 18);
        graphics.DrawString("Turn Flow", smallFont, accentBrush, bounds.Left + 22, bounds.Top + 76);
        graphics.DrawString("Production -> Purchase -> Reinforcement -> Movement\n\nUse this reference while following the guided tutorial.", smallFont, textBrush, new RectangleF(bounds.Left + 22, bounds.Top + 105, 360, 180));
    }

    private static void DrawContextMenu(Graphics graphics, Brush panelBrush, Pen accentPen, Brush textBrush, Font smallFont)
    {
        var bounds = new Rectangle(790, 470, 180, 110);
        graphics.FillRectangle(panelBrush, bounds);
        graphics.DrawRectangle(accentPen, bounds);
        graphics.DrawString("Move army\nInspect route\nCancel", smallFont, textBrush, bounds.Left + 14, bounds.Top + 14);
    }

    private static void DrawTutorialWindow(
        Graphics graphics,
        GuidedTutorialStepDefinition step,
        GuidedTutorialCapturedState state,
        string phase,
        Brush panelBrush,
        Pen accentPen,
        Brush textBrush,
        Brush accentBrush,
        Brush dimBrush,
        Font titleFont,
        Font bodyFont,
        Font smallFont)
    {
        var bounds = new Rectangle(215, 116, 520, 645);
        graphics.FillRectangle(panelBrush, bounds);
        graphics.DrawRectangle(accentPen, bounds);
        graphics.DrawString("Tutorial Mode", titleFont, textBrush, bounds.Left + 16, bounds.Top + 16);
        graphics.DrawString("x", bodyFont, textBrush, bounds.Right - 34, bounds.Top + 16);

        var objectivePanel = new Rectangle(bounds.Left + 16, bounds.Top + 72, bounds.Width - 32, 220);
        graphics.DrawRectangle(accentPen, objectivePanel);
        graphics.DrawString($"Step {state.StepIndex + 1} of 14 ({phase})", smallFont, dimBrush, objectivePanel.Left + 14, objectivePanel.Top + 14);
        graphics.DrawString(step.Title, titleFont, accentBrush, objectivePanel.Left + 14, objectivePanel.Top + 48);
        graphics.DrawString("Objective: " + step.Objective, smallFont, textBrush, new RectangleF(objectivePanel.Left + 14, objectivePanel.Top + 96, objectivePanel.Width - 28, 48));
        graphics.DrawString(state.Status, smallFont, state.IsComplete ? accentBrush : dimBrush, objectivePanel.Left + 14, objectivePanel.Bottom - 38);

        var actionPanel = new Rectangle(bounds.Left + 16, bounds.Top + 315, bounds.Width - 32, 92);
        graphics.DrawRectangle(accentPen, actionPanel);
        graphics.DrawString("Step actions", smallFont, textBrush, actionPanel.Left + 14, actionPanel.Top + 14);
        graphics.DrawString("- " + step.Action + "\n- Verify highlighted targets", smallFont, textBrush, actionPanel.Left + 14, actionPanel.Top + 38);

        var pathPanel = new Rectangle(bounds.Left + 16, bounds.Top + 430, bounds.Width - 32, 150);
        graphics.DrawRectangle(accentPen, pathPanel);
        graphics.DrawString("Tutorial path", smallFont, textBrush, pathPanel.Left + 14, pathPanel.Top + 14);
        graphics.DrawString("NOW   " + step.Title + "\nNEXT  " + (state.StepIndex < 13 ? GuidedTutorialJourneyDefinition.Steps[state.StepIndex + 1].Title : "Complete"), smallFont, textBrush, pathPanel.Left + 14, pathPanel.Top + 42);
        graphics.DrawString("Expected highlights: " + string.Join(", ", step.ExpectedHighlightTargets), smallFont, dimBrush, new RectangleF(pathPanel.Left + 14, pathPanel.Top + 92, pathPanel.Width - 28, 42));

        DrawButton(graphics, "Back", bounds.Left + 80, bounds.Bottom - 58, 110, 36, accentPen, textBrush, smallFont);
        DrawButton(graphics, state.NextButtonText, bounds.Left + 210, bounds.Bottom - 58, 130, 36, accentPen, textBrush, smallFont);
        DrawButton(graphics, "End", bounds.Left + 360, bounds.Bottom - 58, 100, 36, accentPen, textBrush, smallFont);
    }

    private static void DrawButton(Graphics graphics, string label, int x, int y, int width, int height, Pen accentPen, Brush textBrush, Font font)
    {
        using var fill = new SolidBrush(Color.FromArgb(32, 44, 48, 54));
        var bounds = new Rectangle(x, y, width, height);
        graphics.FillRectangle(fill, bounds);
        graphics.DrawRectangle(accentPen, bounds);
        graphics.DrawString(label, font, textBrush, bounds.Left + 12, bounds.Top + 10);
    }

    private static void DrawHighlightBounds(Graphics graphics, GuidedTutorialCapturedState state, Pen highlightPen, Font smallFont)
    {
        foreach (GuidedTutorialHighlightState highlight in state.HighlightBounds.Where(item => item.Visible))
        {
            graphics.DrawRectangle(highlightPen, highlight.X, highlight.Y, highlight.Width, highlight.Height);
            graphics.DrawString(highlight.Target, smallFont, Brushes.White, highlight.X + 4, Math.Max(0, highlight.Y - 18));
        }
    }

    private static void DrawSystem(Graphics graphics, int x, int y, string name, Font bodyFont, Font smallFont)
    {
        using var orbitPen = new Pen(Color.FromArgb(95, 174, 222, 116), 2);
        using var starBrush = new SolidBrush(Color.Yellow);
        using var bodyBrush = new SolidBrush(Color.FromArgb(210, 170, 120, 70));
        using var blueBrush = new SolidBrush(Color.FromArgb(120, 170, 220));

        graphics.DrawEllipse(orbitPen, x - 80, y - 80, 160, 160);
        graphics.DrawEllipse(orbitPen, x - 42, y - 42, 84, 84);
        graphics.FillEllipse(starBrush, x - 8, y - 8, 16, 16);
        graphics.FillEllipse(blueBrush, x - 52, y - 38, 32, 32);
        graphics.FillEllipse(bodyBrush, x + 38, y + 18, 42, 42);
        graphics.DrawString(name, bodyFont, Brushes.White, x - 32, y + 94);
        graphics.DrawString("rocky body", smallFont, Brushes.White, x + 36, y + 64);
    }
}

internal sealed record GuidedTutorialCapturedState(
    int StepIndex,
    string StepId,
    string Title,
    string Objective,
    string Status,
    string NextButtonText,
    bool IsComplete,
    bool TutorialVisible,
    IReadOnlyList<string> HighlightTargets,
    IReadOnlyList<GuidedTutorialHighlightState> HighlightBounds,
    string Phase,
    string CurrentPlayerId,
    string SelectionType,
    string SelectedOwnerId,
    bool HelpVisible,
    bool DashboardVisible,
    bool EncyclopediaVisible,
    bool ContextMenuOpen,
    int OwnArmyCount,
    int MovedArmyCount,
    int Population,
    int Metal,
    int Fuel)
{
    public static GuidedTutorialCapturedState From(TutorialStateResponse response)
    {
        Assert.True(response.Result.Success, response.Result.Message);
        return new GuidedTutorialCapturedState(
            response.StepIndex,
            response.StepId,
            response.Title,
            response.Objective,
            response.Status,
            response.NextButtonText,
            response.IsComplete,
            response.TutorialVisible,
            response.HighlightTargets.ToArray(),
            response.HighlightBounds.Select(item => new GuidedTutorialHighlightState(
                item.Target,
                item.X,
                item.Y,
                item.Width,
                item.Height,
                item.Visible)).ToArray(),
            response.Phase,
            response.CurrentPlayerId,
            response.SelectionType,
            response.SelectedOwnerId,
            response.HelpVisible,
            response.DashboardVisible,
            response.EncyclopediaVisible,
            response.ContextMenuOpen,
            response.OwnArmyCount,
            response.MovedArmyCount,
            response.Population,
            response.Metal,
            response.Fuel);
    }

    public static GuidedTutorialCapturedState ForExpectedStep(GuidedTutorialStepDefinition step, bool isComplete)
    {
        return new GuidedTutorialCapturedState(
            step.StepIndex,
            step.StepId,
            step.Title,
            step.Objective,
            isComplete ? "Status: Objective complete. Press Next." : "Status: Waiting for the objective.",
            isComplete ? "Next" : "Skip",
            isComplete,
            true,
            step.ExpectedHighlightTargets,
            step.ExpectedHighlightTargets.Select((target, index) => new GuidedTutorialHighlightState(target, 220 + index * 140, 120, 120, 90, true)).ToArray(),
            "Production",
            "debug-player",
            "None",
            string.Empty,
            false,
            false,
            false,
            false,
            1,
            0,
            100,
            50,
            50);
    }
}

internal sealed record GuidedTutorialHighlightState(
    string Target,
    int X,
    int Y,
    int Width,
    int Height,
    bool Visible);

internal static class GuidedTutorialDeterministicValidator
{
    public static void ValidateAfterState(
        GuidedTutorialStepDefinition step,
        GuidedTutorialCapturedState before,
        GuidedTutorialCapturedState after)
    {
        if (step.StepId == "complete")
        {
            Require(!after.TutorialVisible, "complete must close the guided tutorial window.");
            return;
        }

        Require(after.StepId == step.StepId, $"After state stepId must remain {step.StepId} until Next is invoked.");
        Require(after.IsComplete, $"Step {step.StepId} must be complete after action {step.Action}.");
        Require(step.ExpectedHighlightTargets.SequenceEqual(after.HighlightTargets), $"Step {step.StepId} highlight targets are stale or missing.");
        foreach (string target in step.ExpectedHighlightTargets)
        {
            Require(after.HighlightBounds.Any(bound => bound.Target == target && bound.Visible && bound.Width > 0 && bound.Height > 0),
                $"Step {step.StepId} expected visible highlight target {target}.");
        }

        switch (step.StepId)
        {
            case "help":
                Require(after.HelpVisible, "help step must show the shortcut sheet.");
                break;
            case "production":
                Require(after.Phase == "Purchase", "production step must advance to Purchase phase.");
                break;
            case "dashboard":
                Require(after.DashboardVisible, "dashboard step must show the player dashboard.");
                break;
            case "purchase":
                Require(after.OwnArmyCount > before.OwnArmyCount, "purchase step must increase own army count.");
                break;
            case "reinforcement-phase":
                Require(after.Phase == "Reinforcement", "reinforcement-phase step must advance to Reinforcement phase.");
                break;
            case "reinforcement-target":
                Require(after.SelectionType is "Region" or "HyperspaceLaneMouth", "reinforcement-target step must select an owned target.");
                Require(after.SelectedOwnerId == after.CurrentPlayerId, "reinforcement-target selection must be owned by the current player.");
                break;
            case "movement-phase":
                Require(after.Phase == "Movement", "movement-phase step must advance to Movement phase.");
                break;
            case "army":
                Require(after.SelectionType == "Army", "army step must select an army.");
                Require(after.SelectedOwnerId == after.CurrentPlayerId, "army step must select the current player's army.");
                break;
            case "movement":
                Require(after.MovedArmyCount > before.MovedArmyCount || after.ContextMenuOpen, "movement step must move an army or open movement context.");
                break;
            case "reference":
                Require(after.EncyclopediaVisible, "reference step must show the encyclopedia.");
                break;
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidDataException(message);
        }
    }
}

internal static class GuidedTutorialAgentComparisonScenarioYaml
{
    private static readonly ISerializer Serializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
        .Build();

    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public static string Serialize(GuidedTutorialAgentComparisonScenarioDocument document)
    {
        return Serializer.Serialize(document);
    }

    public static GuidedTutorialAgentComparisonScenarioDocument Deserialize(string yaml)
    {
        return Deserializer.Deserialize<GuidedTutorialAgentComparisonScenarioDocument>(yaml)
            ?? throw new InvalidDataException("Guided tutorial comparison YAML document did not deserialize.");
    }
}

internal sealed class GuidedTutorialAgentComparisonScenarioDocument
{
    public string StepId { get; set; } = string.Empty;

    public int StepIndex { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Objective { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public List<string> ExpectedHighlightTargets { get; set; } = [];

    public List<string> ExpectedGameStateDelta { get; set; } = [];

    public List<string> ExpectedVisibleText { get; set; } = [];

    [YamlMember(ScalarStyle = ScalarStyle.Literal)]
    public string Prompt { get; set; } = string.Empty;

    public List<RequirementReference> FunctionalRequirements { get; set; } = [];

    public List<RequirementReference> TechnicalRequirements { get; set; } = [];

    public string AgentsReadmeFirstPath { get; set; } = string.Empty;

    public string BeforeWireframePath { get; set; } = string.Empty;

    public string AfterWireframePath { get; set; } = string.Empty;

    public string BeforeScreenshotPath { get; set; } = string.Empty;

    public string AfterScreenshotPath { get; set; } = string.Empty;

    public string BeforeVisualTreePath { get; set; } = string.Empty;

    public string AfterVisualTreePath { get; set; } = string.Empty;

    [YamlMember(ScalarStyle = ScalarStyle.Literal)]
    public string ResultSchema { get; set; } = string.Empty;

    [YamlMember(ScalarStyle = ScalarStyle.Literal)]
    public string? AgentsReadmeFirstContent { get; set; }

    public static GuidedTutorialAgentComparisonScenarioDocument FromStep(GuidedTutorialStepDefinition step)
    {
        return new GuidedTutorialAgentComparisonScenarioDocument
        {
            StepId = step.StepId,
            StepIndex = step.StepIndex,
            Title = step.Title,
            Objective = step.Objective,
            Action = step.Action,
            ExpectedHighlightTargets = step.ExpectedHighlightTargets.ToList(),
            ExpectedGameStateDelta = GetExpectedGameStateDelta(step).ToList(),
            ExpectedVisibleText =
            [
                "Tutorial Mode",
                step.Title,
                step.Objective
            ],
            Prompt = BuildPrompt(step),
            FunctionalRequirements =
            [
                new RequirementReference
                {
                    Id = "FR-GUIDED-TUTORIAL-001",
                    Text = "The guided tutorial must lead the player through the full 14-step strategic loop."
                },
                new RequirementReference
                {
                    Id = $"FR-GUIDED-TUTORIAL-{step.StepIndex + 1:D2}",
                    Text = $"Step '{step.StepId}' must perform action '{step.Action}' and satisfy objective '{step.Objective}'."
                },
                new RequirementReference
                {
                    Id = "FR-UI-HIGHLIGHT-001",
                    Text = "Tutorial steps must visually highlight the UI elements the user is expected to inspect or use."
                }
            ],
            TechnicalRequirements =
            [
                new RequirementReference
                {
                    Id = "TR-GUIDED-TUTORIAL-DEBUG-PROTOCOL-001",
                    Text = "The tutorial journey must be driven through deterministic debug protocol actions instead of raw flaky input."
                },
                new RequirementReference
                {
                    Id = "TR-GUIDED-TUTORIAL-HWND-001",
                    Text = "Actual before and after screenshots must be captured from the RiskyStars client HWND at 1536x832."
                },
                new RequirementReference
                {
                    Id = "TR-GUIDED-TUTORIAL-AI-SCHEMA-001",
                    Text = "The AI comparison result must conform exactly to the strict resultSchema JSON schema."
                },
                new RequirementReference
                {
                    Id = "TR-UI-AUDIT-AGENTS-001",
                    Text = "The model must consider AGENTS-README-FIRST.yaml content when validating process and correctness requirements."
                }
            ],
            AgentsReadmeFirstPath = "AGENTS-README-FIRST.yaml",
            BeforeWireframePath = step.BeforeWireframePath,
            AfterWireframePath = step.AfterWireframePath,
            BeforeScreenshotPath = step.BeforeScreenshotPath,
            AfterScreenshotPath = step.AfterScreenshotPath,
            BeforeVisualTreePath = step.BeforeVisualTreePath,
            AfterVisualTreePath = step.AfterVisualTreePath,
            ResultSchema = GuidedTutorialAgentComparisonJson.SchemaJson
        };
    }

    private static IEnumerable<string> GetExpectedGameStateDelta(GuidedTutorialStepDefinition step)
    {
        return step.StepId switch
        {
            "sync" => ["World snapshot timestamp exists and step status becomes complete."],
            "turn" => ["Active player is the current tutorial player."],
            "select" => ["Selection panel shows the deterministic selected map entity."],
            "help" => ["Shortcut help panel is visible."],
            "production" => ["Current phase reaches Purchase."],
            "dashboard" => ["Player dashboard is visible."],
            "purchase" => ["Own army count or reserve increases."],
            "reinforcement-phase" => ["Current phase reaches Reinforcement."],
            "reinforcement-target" => ["Selected target owner is the current player."],
            "movement-phase" => ["Current phase reaches Movement."],
            "army" => ["Selected army owner is the current player."],
            "movement" => ["Moved army count increases or movement context is open."],
            "reference" => ["Encyclopedia window is visible."],
            "complete" => ["Tutorial window closes and gameplay remains active."],
            _ => []
        };
    }

    private static string BuildPrompt(GuidedTutorialStepDefinition step)
    {
        return $$"""
        Compare the guided tutorial step '{{step.StepId}}' expected before/after wireframes against the actual before/after HWND screenshots.
        Validate the full game viewport, tutorial panel, top bar, map, side panels, highlighted UI targets, objective status text, and gameplay state change.
        The required action is '{{step.Action}}'.
        Required highlight targets: {{string.Join(", ", step.ExpectedHighlightTargets)}}.
        The after state must show the objective complete for every step except 'complete', which must close the tutorial window while gameplay remains active.
        Report any missing, stale, wrongly bounded, hidden, or occluded highlight as incorrect behavior.
        Treat decorative map names, exact resource values not listed in expectedGameStateDelta, and extra tutorial explanatory prose as acceptable unless they block the required user flow or contradict the visual-tree tutorialState.
        Also judge whether the wireframes are suitable for validating this step's stated functional and technical requirements.
        """;
    }
}

internal sealed class GuidedTutorialAgentComparisonScenario
{
    public string SourcePath { get; set; } = string.Empty;

    public string RawYaml { get; set; } = string.Empty;

    public string ModelPayloadYaml { get; set; } = string.Empty;

    public string StepId { get; set; } = string.Empty;

    public int StepIndex { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Objective { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public List<string> ExpectedHighlightTargets { get; set; } = [];

    public List<string> ExpectedGameStateDelta { get; set; } = [];

    public List<string> ExpectedVisibleText { get; set; } = [];

    public string Prompt { get; set; } = string.Empty;

    public List<RequirementReference> FunctionalRequirements { get; set; } = [];

    public List<RequirementReference> TechnicalRequirements { get; set; } = [];

    public string AgentsReadmeFirstPath { get; set; } = string.Empty;

    public string AgentsReadmeFirstFullPath { get; set; } = string.Empty;

    public string AgentsReadmeFirstContent { get; set; } = string.Empty;

    public string BeforeWireframePath { get; set; } = string.Empty;

    public string BeforeWireframeFullPath { get; set; } = string.Empty;

    public string AfterWireframePath { get; set; } = string.Empty;

    public string AfterWireframeFullPath { get; set; } = string.Empty;

    public string BeforeScreenshotPath { get; set; } = string.Empty;

    public string BeforeScreenshotFullPath { get; set; } = string.Empty;

    public string AfterScreenshotPath { get; set; } = string.Empty;

    public string AfterScreenshotFullPath { get; set; } = string.Empty;

    public string BeforeVisualTreePath { get; set; } = string.Empty;

    public string BeforeVisualTreeFullPath { get; set; } = string.Empty;

    public string AfterVisualTreePath { get; set; } = string.Empty;

    public string AfterVisualTreeFullPath { get; set; } = string.Empty;

    public string ResultSchema { get; set; } = string.Empty;
}

internal static class GuidedTutorialAgentComparisonScenarioCatalog
{
    public static IReadOnlyList<string> ScenarioPaths =>
        GuidedTutorialJourneyDefinition.Steps.Select(step => step.ScenarioPath).ToArray();

    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public static IReadOnlyList<GuidedTutorialAgentComparisonScenario> LoadAll(bool requireArtifacts = true)
    {
        return ScenarioPaths.Select(path => Load(path, requireArtifacts)).ToArray();
    }

    public static GuidedTutorialAgentComparisonScenario Load(string scenarioPath, bool requireArtifacts = true)
    {
        string fullPath = ResolveRepositoryPath(scenarioPath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Could not find guided tutorial agent comparison scenario {scenarioPath}.", fullPath);
        }

        GuidedTutorialAgentComparisonScenario scenario = Deserializer.Deserialize<GuidedTutorialAgentComparisonScenario>(File.ReadAllText(fullPath))
            ?? throw new InvalidDataException($"Guided tutorial scenario {scenarioPath} did not deserialize.");
        scenario.SourcePath = scenarioPath;
        scenario.RawYaml = File.ReadAllText(fullPath);
        scenario.BeforeWireframeFullPath = ResolveRepositoryPath(scenario.BeforeWireframePath);
        scenario.AfterWireframeFullPath = ResolveRepositoryPath(scenario.AfterWireframePath);
        scenario.BeforeScreenshotFullPath = ResolveRepositoryPath(scenario.BeforeScreenshotPath);
        scenario.AfterScreenshotFullPath = ResolveRepositoryPath(scenario.AfterScreenshotPath);
        scenario.BeforeVisualTreeFullPath = ResolveRepositoryPath(scenario.BeforeVisualTreePath);
        scenario.AfterVisualTreeFullPath = ResolveRepositoryPath(scenario.AfterVisualTreePath);
        scenario.AgentsReadmeFirstFullPath = ResolveRepositoryPath(scenario.AgentsReadmeFirstPath);
        scenario.AgentsReadmeFirstContent = File.Exists(scenario.AgentsReadmeFirstFullPath)
            ? File.ReadAllText(scenario.AgentsReadmeFirstFullPath)
            : string.Empty;

        Validate(scenario, requireArtifacts);
        scenario.ModelPayloadYaml = AppendAgentsReadmeFirstNode(scenario.RawYaml, scenario.AgentsReadmeFirstContent);
        return scenario;
    }

    public static string ResolveRepositoryPath(string path)
    {
        if (Path.IsPathFullyQualified(path))
        {
            return path;
        }

        return Path.Combine(FindRepositoryRoot(), path.Replace('/', Path.DirectorySeparatorChar));
    }

    public static string FindRepositoryRoot()
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

    private static void Validate(GuidedTutorialAgentComparisonScenario scenario, bool requireArtifacts)
    {
        RequireText(scenario.StepId, "stepId");
        RequireText(scenario.Title, "title");
        RequireText(scenario.Objective, "objective");
        RequireText(scenario.Action, "action");
        RequireText(scenario.Prompt, "prompt");
        RequireText(scenario.ResultSchema, "resultSchema");
        RequireText(scenario.AgentsReadmeFirstPath, "agentsReadmeFirstPath");
        Require(scenario.FunctionalRequirements.Count > 0, "functionalRequirements must contain at least one FR.");
        Require(scenario.TechnicalRequirements.Count > 0, "technicalRequirements must contain at least one TR.");
        Require(File.Exists(scenario.AgentsReadmeFirstFullPath), $"agentsReadmeFirstPath does not exist: {scenario.AgentsReadmeFirstPath}");
        RequireText(scenario.AgentsReadmeFirstContent, "agentsReadmeFirstContent");
        using JsonDocument _ = JsonDocument.Parse(scenario.ResultSchema);

        RequireText(scenario.BeforeWireframePath, "beforeWireframePath");
        RequireText(scenario.AfterWireframePath, "afterWireframePath");
        RequireText(scenario.BeforeScreenshotPath, "beforeScreenshotPath");
        RequireText(scenario.AfterScreenshotPath, "afterScreenshotPath");
        RequireText(scenario.BeforeVisualTreePath, "beforeVisualTreePath");
        RequireText(scenario.AfterVisualTreePath, "afterVisualTreePath");

        if (!requireArtifacts)
        {
            return;
        }

        Require(File.Exists(scenario.BeforeWireframeFullPath), $"beforeWireframePath does not exist: {scenario.BeforeWireframePath}");
        Require(File.Exists(scenario.AfterWireframeFullPath), $"afterWireframePath does not exist: {scenario.AfterWireframePath}");
        Require(File.Exists(scenario.BeforeScreenshotFullPath), $"beforeScreenshotPath does not exist: {scenario.BeforeScreenshotPath}");
        Require(File.Exists(scenario.AfterScreenshotFullPath), $"afterScreenshotPath does not exist: {scenario.AfterScreenshotPath}");
        Require(File.Exists(scenario.BeforeVisualTreeFullPath), $"beforeVisualTreePath does not exist: {scenario.BeforeVisualTreePath}");
        Require(File.Exists(scenario.AfterVisualTreeFullPath), $"afterVisualTreePath does not exist: {scenario.AfterVisualTreePath}");
    }

    private static string AppendAgentsReadmeFirstNode(string rawYaml, string agentsReadmeFirstContent)
    {
        GuidedTutorialAgentComparisonScenarioDocument document = GuidedTutorialAgentComparisonScenarioYaml.Deserialize(rawYaml);
        document.AgentsReadmeFirstContent = agentsReadmeFirstContent;
        return GuidedTutorialAgentComparisonScenarioYaml.Serialize(document);
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
}

internal static class GuidedTutorialAgentComparisonPrompt
{
    public static string BuildInstructions()
    {
        return """
        You are the RiskyStars guided tutorial UI auditor.
        Compare the expected before/after wireframes against the actual before/after HWND screenshots using the YAML guided tutorial scenario file content.
        Return only valid JSON.
        Return exactly one valid JSON object matching the resultSchema YAML node. Do not wrap the response in Markdown. Do not add prose before or after the JSON.
        Required top-level fields are stepId, summary, correctElements, incorrectElements, correctBehavior, incorrectBehavior, highlightValidation, and wireframeSuitability.
        incorrectElements and incorrectBehavior must be empty only when there are no actual defects.
        highlightValidation must explicitly confirm every expected target is visible, correctly bounded, not hidden behind the tutorial panel, and not replaced by stale highlights.
        wireframeSuitability.rating must be suitable only when the wireframes can validate the stated step requirements; otherwise use partially_suitable or unsuitable.
        A wireframe may be suitable without being pixel-perfect when it shows the required viewport, tutorial panel, panel/window presence, expected status, and highlight targets.
        Do not mark as incorrect solely because screenshots include normal RiskyStars HUD panel shortcut text, dock collapse/resize controls, or additional tutorial explanatory prose not listed in expectedVisibleText.
        Do not require exact map body names or exact selected entity display names unless the scenario explicitly lists that text in expectedVisibleText or expectedGameStateDelta.
        Use the functionalRequirements, technicalRequirements, agentsReadmeFirstPath, and appended agentsReadmeFirstContent YAML nodes as traceable audit context.
        Use the attached before and after visual-tree JSON text blocks as authoritative state data for nesting, visibility, tutorial state, highlight bounds, selection, phase, resources, and open windows.
        Use confidence values from 0 to 1.
        Use exactly one of these lowercase severity values: low, medium, high, critical.
        Use exactly one of these lowercase rating values: suitable, partially_suitable, unsuitable.
        The first image is the expected before-state wireframe; the second image is the expected after-state wireframe.
        The third image is the actual before-state HWND screenshot; the fourth image is the actual after-state HWND screenshot.
        MapViewport means the resolved clickable map target inside the map viewport, not necessarily the whole map rectangle.
        """;
    }

    public static string BuildUserPrompt(GuidedTutorialAgentComparisonScenario scenario)
    {
        return $$"""
        YAML guided tutorial scenario file content:
        ```yaml
        {{scenario.ModelPayloadYaml}}
        ```

        The beforeWireframePath, afterWireframePath, beforeScreenshotPath, and afterScreenshotPath values identify the four attached images in that order.
        The beforeVisualTreePath and afterVisualTreePath values identify the attached visual tree JSON text blocks captured at the same states. Each visual tree JSON has a tutorialState node and a raw visualTree node.
        Compare whether the actual guided tutorial step satisfies the expected before/after wireframes, the stated tutorial action, highlight targets, gameplay state delta, visible text, FRs, TRs, and repository process requirements.
        Treat decorative map labels, exact resource values not listed in expectedGameStateDelta, and extra tutorial explanatory prose as acceptable unless they block the required user flow or contradict the visual-tree tutorialState.
        Return a single JSON object that conforms to the resultSchema node.
        """;
    }
}

internal sealed class GuidedTutorialAgentComparisonRunner
{
    public async Task<GuidedTutorialAgentComparisonResult> CompareAsync(
        GuidedTutorialAgentComparisonScenario scenario,
        AgentWireframeComparisonOptions options,
        CancellationToken cancellationToken)
    {
        options.ValidateForRun();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.TimeoutSeconds));

        var clientOptions = new OpenAIClientOptions
        {
            Endpoint = options.EndpointUri,
            NetworkTimeout = TimeSpan.FromSeconds(options.TimeoutSeconds)
        };
        var chatClient = new OpenAI.Chat.ChatClient(
            options.Model,
            new ApiKeyCredential(options.ApiKey),
            clientOptions);
        IChatClient aiChatClient = chatClient.AsIChatClient();
        ChatClientAgent agent = aiChatClient.AsAIAgent(
            name: "RiskyStarsGuidedTutorialAuditor",
            description: "Compares RiskyStars guided tutorial expected step wireframes to actual before/after screenshots.",
            instructions: GuidedTutorialAgentComparisonPrompt.BuildInstructions());

        var message = new ChatMessage(
            ChatRole.User,
            new List<AIContent>
            {
                new TextContent(GuidedTutorialAgentComparisonPrompt.BuildUserPrompt(scenario)),
                new TextContent("Before visual tree JSON:\n```json\n" + await File.ReadAllTextAsync(scenario.BeforeVisualTreeFullPath, timeout.Token) + "\n```"),
                new TextContent("After visual tree JSON:\n```json\n" + await File.ReadAllTextAsync(scenario.AfterVisualTreeFullPath, timeout.Token) + "\n```"),
                new DataContent(await File.ReadAllBytesAsync(scenario.BeforeWireframeFullPath, timeout.Token), "image/png")
                {
                    Name = "expected-before-wireframe.png"
                },
                new DataContent(await File.ReadAllBytesAsync(scenario.AfterWireframeFullPath, timeout.Token), "image/png")
                {
                    Name = "expected-after-wireframe.png"
                },
                new DataContent(await File.ReadAllBytesAsync(scenario.BeforeScreenshotFullPath, timeout.Token), "image/png")
                {
                    Name = "actual-before-hwnd-screenshot.png"
                },
                new DataContent(await File.ReadAllBytesAsync(scenario.AfterScreenshotFullPath, timeout.Token), "image/png")
                {
                    Name = "actual-after-hwnd-screenshot.png"
                }
            });

        var chatOptions = new ChatOptions
        {
            Temperature = options.Temperature,
            ResponseFormat = AgentWireframeComparisonJson.CreateResponseFormat(scenario.ResultSchema)
        };

        AgentResponse<GuidedTutorialAgentComparisonResult> response = await agent.RunAsync<GuidedTutorialAgentComparisonResult>(
            message,
            session: null,
            serializerOptions: AgentWireframeComparisonJson.JsonOptions,
            options: new ChatClientAgentRunOptions(chatOptions),
            cancellationToken: timeout.Token);

        GuidedTutorialAgentComparisonResult result = response.Result
            ?? throw new InvalidDataException("The agent returned no typed guided tutorial comparison result.");
        return result;
    }
}

internal static class GuidedTutorialAgentComparisonRetryPolicy
{
    public static async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken,
        int maxAttempts = 2,
        Func<int, TimeSpan>? delayProvider = null,
        Func<TimeSpan, CancellationToken, Task>? delayAsync = null)
    {
        if (maxAttempts < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxAttempts), "Retry attempts must be at least one.");
        }

        delayProvider ??= DefaultDelayForAttempt;
        delayAsync ??= Task.Delay;

        for (int attempt = 1; ; attempt++)
        {
            try
            {
                return await operation(cancellationToken);
            }
            catch (Exception exception) when (attempt < maxAttempts && IsTransientRateLimit(exception))
            {
                await delayAsync(delayProvider(attempt), cancellationToken);
            }
        }
    }

    public static bool IsTransientRateLimit(Exception exception)
    {
        string message = exception.Message;
        return message.Contains("Status: 429", StringComparison.OrdinalIgnoreCase)
            || message.Contains("Too Many Requests", StringComparison.OrdinalIgnoreCase);
    }

    private static TimeSpan DefaultDelayForAttempt(int attempt)
    {
        return attempt switch
        {
            1 => TimeSpan.FromSeconds(60),
            2 => TimeSpan.FromSeconds(120),
            _ => TimeSpan.FromSeconds(180)
        };
    }
}

internal sealed class GuidedTutorialAgentComparisonRateLimitCircuitBreaker
{
    private string? _blockedMessage;

    public bool TryGetBlockedFailure(out InvalidOperationException? exception)
    {
        if (string.IsNullOrWhiteSpace(_blockedMessage))
        {
            exception = null;
            return false;
        }

        exception = new InvalidOperationException(
            "Guided tutorial AI comparison stopped because a prior scenario exhausted rate-limit retries: "
            + _blockedMessage);
        return true;
    }

    public void MarkBlocked(Exception exception)
    {
        if (!GuidedTutorialAgentComparisonRetryPolicy.IsTransientRateLimit(exception))
        {
            return;
        }

        _blockedMessage ??= exception.Message;
    }
}

internal static class GuidedTutorialAgentComparisonOutputFiles
{
    public static void ClearScenarioArtifacts(string resultPath, string errorPath)
    {
        DeleteIfExists(resultPath);
        DeleteIfExists(errorPath);
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}

internal static class GuidedTutorialAgentComparisonJson
{
    public const string SchemaJson = """
        {
          "$schema": "https://json-schema.org/draft/2020-12/schema",
          "type": "object",
          "additionalProperties": false,
          "required": ["stepId", "summary", "correctElements", "incorrectElements", "correctBehavior", "incorrectBehavior", "highlightValidation", "wireframeSuitability"],
          "properties": {
            "stepId": { "type": "string", "minLength": 1 },
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
              "items": { "$ref": "#/$defs/incorrectItem" }
            },
            "correctBehavior": {
              "type": "array",
              "minItems": 1,
              "items": {
                "type": "object",
                "additionalProperties": false,
                "required": ["behavior", "requirement", "evidence", "confidence"],
                "properties": {
                  "behavior": { "type": "string", "minLength": 1 },
                  "requirement": { "type": "string", "minLength": 1 },
                  "evidence": { "type": "string", "minLength": 1 },
                  "confidence": { "type": "number", "minimum": 0, "maximum": 1 }
                }
              }
            },
            "incorrectBehavior": {
              "type": "array",
              "items": { "$ref": "#/$defs/incorrectBehaviorItem" }
            },
            "highlightValidation": {
              "type": "object",
              "additionalProperties": false,
              "required": ["expectedTargets", "visibleTargets", "allExpectedTargetsVisible", "boundsCorrect", "notHiddenBehindTutorialPanel", "noStaleHighlights", "why"],
              "properties": {
                "expectedTargets": { "type": "array", "items": { "type": "string" } },
                "visibleTargets": { "type": "array", "items": { "type": "string" } },
                "allExpectedTargetsVisible": { "type": "boolean" },
                "boundsCorrect": { "type": "boolean" },
                "notHiddenBehindTutorialPanel": { "type": "boolean" },
                "noStaleHighlights": { "type": "boolean" },
                "why": { "type": "string", "minLength": 1 }
              }
            },
            "wireframeSuitability": {
              "type": "object",
              "additionalProperties": false,
              "required": ["rating", "why", "missingRequirements", "recommendedWireframeChanges"],
              "properties": {
                "rating": { "type": "string", "enum": ["suitable", "partially_suitable", "unsuitable"] },
                "why": { "type": "string", "minLength": 1 },
                "missingRequirements": { "type": "array", "items": { "type": "string" } },
                "recommendedWireframeChanges": { "type": "array", "items": { "type": "string" } }
              }
            }
          },
          "$defs": {
            "incorrectItem": {
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
            },
            "incorrectBehaviorItem": {
              "type": "object",
              "additionalProperties": false,
              "required": ["behavior", "requirement", "expected", "actual", "whyIncorrect", "severity", "confidence"],
              "properties": {
                "behavior": { "type": "string", "minLength": 1 },
                "requirement": { "type": "string", "minLength": 1 },
                "expected": { "type": "string", "minLength": 1 },
                "actual": { "type": "string", "minLength": 1 },
                "whyIncorrect": { "type": "string", "minLength": 1 },
                "severity": { "type": "string", "enum": ["low", "medium", "high", "critical"] },
                "confidence": { "type": "number", "minimum": 0, "maximum": 1 }
              }
            }
          }
        }
        """;
}

internal static class GuidedTutorialAgentComparisonValidator
{
    private static readonly HashSet<string> Severities = ["low", "medium", "high", "critical"];

    public static GuidedTutorialAgentComparisonResult ParseAndValidate(string json, string expectedStepId)
    {
        GuidedTutorialAgentComparisonResult? result = JsonSerializer.Deserialize<GuidedTutorialAgentComparisonResult>(
            json,
            AgentWireframeComparisonJson.JsonOptions);
        if (result == null)
        {
            throw new JsonException("Agent guided tutorial comparison JSON deserialized to null.");
        }

        Validate(result, expectedStepId);
        return result;
    }

    public static void Validate(GuidedTutorialAgentComparisonResult result, string expectedStepId)
    {
        Require(result.StepId == expectedStepId, $"stepId must be '{expectedStepId}' but was '{result.StepId}'.");
        RequireText(result.Summary, "summary");
        Require(result.CorrectElements.Count > 0, "correctElements must contain at least one element.");
        Require(result.CorrectBehavior.Count > 0, "correctBehavior must contain at least one element.");
        Require(result.IncorrectElements.Count == 0, "incorrectElements must be empty for guided tutorial AI integration pass.");
        Require(result.IncorrectBehavior.Count == 0, "incorrectBehavior must be empty for guided tutorial AI integration pass.");
        GuidedTutorialHighlightValidation highlightValidation =
            result.HighlightValidation ?? throw new InvalidDataException("highlightValidation is required.");
        Require(highlightValidation.AllExpectedTargetsVisible, "highlightValidation.allExpectedTargetsVisible must be true.");
        Require(highlightValidation.BoundsCorrect, "highlightValidation.boundsCorrect must be true.");
        Require(highlightValidation.NotHiddenBehindTutorialPanel, "highlightValidation.notHiddenBehindTutorialPanel must be true.");
        Require(highlightValidation.NoStaleHighlights, "highlightValidation.noStaleHighlights must be true.");
        RequireText(highlightValidation.Why, "highlightValidation.why");
        WireframeSuitability wireframeSuitability =
            result.WireframeSuitability ?? throw new InvalidDataException("wireframeSuitability is required.");
        Require(wireframeSuitability.Rating == "suitable", "wireframeSuitability.rating must be suitable.");
        RequireText(wireframeSuitability.Why, "wireframeSuitability.why");

        foreach (CorrectElementComparison item in result.CorrectElements)
        {
            RequireText(item.Element, "correctElements.element");
            RequireText(item.Requirement, "correctElements.requirement");
            RequireText(item.Evidence, "correctElements.evidence");
            RequireConfidence(item.Confidence, "correctElements.confidence");
        }

        foreach (CorrectGuidedTutorialBehaviorComparison item in result.CorrectBehavior)
        {
            RequireText(item.Behavior, "correctBehavior.behavior");
            RequireText(item.Requirement, "correctBehavior.requirement");
            RequireText(item.Evidence, "correctBehavior.evidence");
            RequireConfidence(item.Confidence, "correctBehavior.confidence");
        }

        foreach (IncorrectElementComparison item in result.IncorrectElements)
        {
            Require(Severities.Contains(item.Severity), "incorrectElements.severity must be low, medium, high, or critical.");
        }

        foreach (IncorrectGuidedTutorialBehaviorComparison item in result.IncorrectBehavior)
        {
            Require(Severities.Contains(item.Severity), "incorrectBehavior.severity must be low, medium, high, or critical.");
        }
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

internal sealed class GuidedTutorialAgentComparisonResult
{
    [JsonPropertyName("stepId")]
    public string StepId { get; set; } = string.Empty;

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    [JsonPropertyName("correctElements")]
    public List<CorrectElementComparison> CorrectElements { get; set; } = [];

    [JsonPropertyName("incorrectElements")]
    public List<IncorrectElementComparison> IncorrectElements { get; set; } = [];

    [JsonPropertyName("correctBehavior")]
    public List<CorrectGuidedTutorialBehaviorComparison> CorrectBehavior { get; set; } = [];

    [JsonPropertyName("incorrectBehavior")]
    public List<IncorrectGuidedTutorialBehaviorComparison> IncorrectBehavior { get; set; } = [];

    [JsonPropertyName("highlightValidation")]
    public GuidedTutorialHighlightValidation? HighlightValidation { get; set; }

    [JsonPropertyName("wireframeSuitability")]
    public WireframeSuitability? WireframeSuitability { get; set; }
}

internal sealed class CorrectGuidedTutorialBehaviorComparison
{
    [JsonPropertyName("behavior")]
    public string Behavior { get; set; } = string.Empty;

    [JsonPropertyName("requirement")]
    public string Requirement { get; set; } = string.Empty;

    [JsonPropertyName("evidence")]
    public string Evidence { get; set; } = string.Empty;

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }
}

internal sealed class IncorrectGuidedTutorialBehaviorComparison
{
    [JsonPropertyName("behavior")]
    public string Behavior { get; set; } = string.Empty;

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

internal sealed class GuidedTutorialHighlightValidation
{
    [JsonPropertyName("expectedTargets")]
    public List<string> ExpectedTargets { get; set; } = [];

    [JsonPropertyName("visibleTargets")]
    public List<string> VisibleTargets { get; set; } = [];

    [JsonPropertyName("allExpectedTargetsVisible")]
    public bool AllExpectedTargetsVisible { get; set; }

    [JsonPropertyName("boundsCorrect")]
    public bool BoundsCorrect { get; set; }

    [JsonPropertyName("notHiddenBehindTutorialPanel")]
    public bool NotHiddenBehindTutorialPanel { get; set; }

    [JsonPropertyName("noStaleHighlights")]
    public bool NoStaleHighlights { get; set; }

    [JsonPropertyName("why")]
    public string Why { get; set; } = string.Empty;
}

internal sealed class GuidedTutorialAgentComparisonError
{
    [JsonPropertyName("stepId")]
    public string StepId { get; set; } = string.Empty;

    [JsonPropertyName("stepIndex")]
    public int StepIndex { get; set; }

    [JsonPropertyName("scenarioPath")]
    public string ScenarioPath { get; set; } = string.Empty;

    [JsonPropertyName("errorType")]
    public string ErrorType { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    public static GuidedTutorialAgentComparisonError FromException(
        GuidedTutorialAgentComparisonScenario scenario,
        Exception exception)
    {
        return new GuidedTutorialAgentComparisonError
        {
            StepId = scenario.StepId,
            StepIndex = scenario.StepIndex,
            ScenarioPath = scenario.SourcePath,
            ErrorType = exception.GetType().FullName ?? exception.GetType().Name,
            Message = exception.Message
        };
    }
}

internal sealed class GuidedTutorialAgentComparisonReport
{
    [JsonPropertyName("generatedAtUtc")]
    public DateTimeOffset GeneratedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    [JsonPropertyName("results")]
    public IReadOnlyList<GuidedTutorialAgentComparisonResult> Results { get; set; } = [];

    [JsonPropertyName("errors")]
    public IReadOnlyList<GuidedTutorialAgentComparisonError> Errors { get; set; } = [];
}

internal static class GuidedTutorialAgentComparisonReportWriter
{
    private const string AggregateReportFileName = "guided-tutorial-agent-comparison-report.json";

    public static async Task WriteAggregateReportAsync(string outputDirectory, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(outputDirectory);
        var results = new List<GuidedTutorialAgentComparisonResult>();
        var errors = new List<GuidedTutorialAgentComparisonError>();

        foreach (string file in Directory.EnumerateFiles(outputDirectory, "*.json"))
        {
            if (string.Equals(Path.GetFileName(file), AggregateReportFileName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (file.EndsWith(".error.json", StringComparison.OrdinalIgnoreCase))
            {
                GuidedTutorialAgentComparisonError? error = JsonSerializer.Deserialize<GuidedTutorialAgentComparisonError>(
                    await File.ReadAllTextAsync(file, cancellationToken),
                    AgentWireframeComparisonJson.JsonOptions);
                if (error != null && !string.IsNullOrWhiteSpace(error.StepId))
                {
                    errors.Add(error);
                }

                continue;
            }

            GuidedTutorialAgentComparisonResult? result = JsonSerializer.Deserialize<GuidedTutorialAgentComparisonResult>(
                await File.ReadAllTextAsync(file, cancellationToken),
                AgentWireframeComparisonJson.JsonOptions);
            if (result != null && !string.IsNullOrWhiteSpace(result.StepId))
            {
                results.Add(result);
            }
        }

        var report = new GuidedTutorialAgentComparisonReport
        {
            Results = results.OrderBy(result => result.StepId, StringComparer.Ordinal).ToArray(),
            Errors = errors.OrderBy(error => error.StepId, StringComparer.Ordinal).ToArray()
        };
        await File.WriteAllTextAsync(
            Path.Combine(outputDirectory, AggregateReportFileName),
            JsonSerializer.Serialize(report, AgentWireframeComparisonJson.IndentedOptions),
            cancellationToken);
    }
}
