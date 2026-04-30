using System.Text.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace RiskyStars.Tests;

public sealed class InteractionVisualTreeStateExpectationTests
{
    private const string SaveSettingsExpectationPath =
        "RiskyStars.Tests/InteractionStateExpectations/main-menu-settings-save-click.yaml";

    [Fact]
    public void MainMenuSettingsSaveClick_CapturedVisualTreesMatchExpectedInteractionState()
    {
        InteractionTextExpectation expectation = InteractionTextExpectation.Load(SaveSettingsExpectationPath);
        string beforeJson = LoadCapturedVisualTree(expectation.InteractionId, phase: "before");
        string afterJson = LoadCapturedVisualTree(expectation.InteractionId, phase: "after");

        IReadOnlyList<string> failures = InteractionTextExpectationValidator.Validate(expectation, beforeJson, afterJson);

        Assert.Empty(failures);
    }

    [Fact]
    public void MainMenuSettingsSaveClick_UnchangedAfterTreeFailsBadBehaviorValidation()
    {
        InteractionTextExpectation expectation = InteractionTextExpectation.Load(SaveSettingsExpectationPath);
        string beforeJson = LoadCapturedVisualTree(expectation.InteractionId, phase: "before");

        IReadOnlyList<string> failures = InteractionTextExpectationValidator.Validate(expectation, beforeJson, beforeJson);

        Assert.Contains(failures, failure => failure.Contains("after visual tree must not contain 'Command Settings'", StringComparison.Ordinal));
        Assert.Contains(failures, failure => failure.Contains("after visual tree must contain 'Command Actions'", StringComparison.Ordinal));
        Assert.Contains(failures, failure => failure.Contains("bad behavior", StringComparison.OrdinalIgnoreCase));
    }

    private static string LoadCapturedVisualTree(string interactionId, string phase)
    {
        string path = Path.Combine(
            FindRepositoryRoot(),
            "RiskyStars.Client",
            "Screenshots",
            "Interactions",
            $"{interactionId}-{phase}.visual-tree.json");
        Assert.True(File.Exists(path), $"Missing captured interaction visual tree: {path}");
        return File.ReadAllText(path);
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

internal sealed class InteractionTextExpectation
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public string InteractionId { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string StartScreenId { get; set; } = string.Empty;

    public string ActionText { get; set; } = string.Empty;

    public List<string> BeforeContainsText { get; set; } = [];

    public List<string> BeforeRejectsText { get; set; } = [];

    public List<string> AfterContainsText { get; set; } = [];

    public List<string> AfterRejectsText { get; set; } = [];

    public List<InteractionTextStateChange> StateChanges { get; set; } = [];

    public List<InteractionTextBadBehavior> BadBehaviorValidations { get; set; } = [];

    public static InteractionTextExpectation Load(string relativePath)
    {
        string path = Path.Combine(FindRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(path), $"Missing interaction state expectation: {path}");

        InteractionTextExpectation expectation = Deserializer.Deserialize<InteractionTextExpectation>(File.ReadAllText(path))
            ?? throw new InvalidDataException($"Interaction state expectation did not deserialize: {path}");
        expectation.Validate();
        return expectation;
    }

    private void Validate()
    {
        Assert.False(string.IsNullOrWhiteSpace(InteractionId), "interactionId is required.");
        Assert.False(string.IsNullOrWhiteSpace(StartScreenId), "startScreenId is required.");
        Assert.False(string.IsNullOrWhiteSpace(ActionText), "actionText is required.");
        Assert.NotEmpty(BeforeContainsText);
        Assert.NotEmpty(AfterContainsText);
        Assert.NotEmpty(StateChanges);
        Assert.NotEmpty(BadBehaviorValidations);
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

internal sealed class InteractionTextStateChange
{
    public string Description { get; set; } = string.Empty;

    public string BeforeText { get; set; } = string.Empty;

    public bool BeforePresent { get; set; }

    public string AfterText { get; set; } = string.Empty;

    public bool AfterPresent { get; set; }
}

internal sealed class InteractionTextBadBehavior
{
    public string Text { get; set; } = string.Empty;

    public string Phase { get; set; } = string.Empty;

    public bool RejectedPresent { get; set; }

    public string Reason { get; set; } = string.Empty;
}

internal static class InteractionTextExpectationValidator
{
    public static IReadOnlyList<string> Validate(
        InteractionTextExpectation expectation,
        string beforeVisualTreeJson,
        string afterVisualTreeJson)
    {
        using JsonDocument beforeDocument = JsonDocument.Parse(beforeVisualTreeJson);
        using JsonDocument afterDocument = JsonDocument.Parse(afterVisualTreeJson);
        var failures = new List<string>();

        ValidateContains(expectation.BeforeContainsText, beforeDocument.RootElement, phase: "before", expectedPresent: true, failures);
        ValidateContains(expectation.BeforeRejectsText, beforeDocument.RootElement, phase: "before", expectedPresent: false, failures);
        ValidateContains(expectation.AfterContainsText, afterDocument.RootElement, phase: "after", expectedPresent: true, failures);
        ValidateContains(expectation.AfterRejectsText, afterDocument.RootElement, phase: "after", expectedPresent: false, failures);

        foreach (InteractionTextStateChange change in expectation.StateChanges)
        {
            ValidateStateChange(change, beforeDocument.RootElement, afterDocument.RootElement, failures);
        }

        foreach (InteractionTextBadBehavior badBehavior in expectation.BadBehaviorValidations)
        {
            ValidateBadBehavior(badBehavior, beforeDocument.RootElement, afterDocument.RootElement, failures);
        }

        return failures;
    }

    private static void ValidateContains(
        IEnumerable<string> texts,
        JsonElement root,
        string phase,
        bool expectedPresent,
        ICollection<string> failures)
    {
        foreach (string text in texts)
        {
            bool actualPresent = VisualTreeContainsText(root, text);
            if (actualPresent != expectedPresent)
            {
                string expectation = expectedPresent ? "must contain" : "must not contain";
                failures.Add($"{phase} visual tree {expectation} '{text}'.");
            }
        }
    }

    private static void ValidateStateChange(
        InteractionTextStateChange change,
        JsonElement beforeRoot,
        JsonElement afterRoot,
        ICollection<string> failures)
    {
        bool beforePresent = VisualTreeContainsText(beforeRoot, change.BeforeText);
        bool afterPresent = VisualTreeContainsText(afterRoot, change.AfterText);

        if (beforePresent != change.BeforePresent)
        {
            failures.Add($"state change '{change.Description}' expected before '{change.BeforeText}' present={change.BeforePresent} but was {beforePresent}.");
        }

        if (afterPresent != change.AfterPresent)
        {
            failures.Add($"state change '{change.Description}' expected after '{change.AfterText}' present={change.AfterPresent} but was {afterPresent}.");
        }
    }

    private static void ValidateBadBehavior(
        InteractionTextBadBehavior badBehavior,
        JsonElement beforeRoot,
        JsonElement afterRoot,
        ICollection<string> failures)
    {
        JsonElement root = string.Equals(badBehavior.Phase, "before", StringComparison.OrdinalIgnoreCase)
            ? beforeRoot
            : afterRoot;
        bool actualPresent = VisualTreeContainsText(root, badBehavior.Text);

        if (actualPresent == badBehavior.RejectedPresent)
        {
            failures.Add($"bad behavior validation failed for {badBehavior.Phase} text '{badBehavior.Text}': {badBehavior.Reason}");
        }
    }

    private static bool VisualTreeContainsText(JsonElement root, string text)
    {
        return root
            .GetProperty("elements")
            .EnumerateArray()
            .Any(element =>
                element.TryGetProperty("text", out JsonElement textElement) &&
                string.Equals(textElement.GetString(), text, StringComparison.OrdinalIgnoreCase));
    }
}
