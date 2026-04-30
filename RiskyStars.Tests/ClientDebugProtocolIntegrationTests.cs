using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Myra;
using Myra.Graphics2D.UI.Styles;
using Grpc.Net.Client;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D.UI;
using RiskyStars.Client;
using RiskyStars.Shared;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using MyraButton = Myra.Graphics2D.UI.Button;

namespace RiskyStars.Tests;

[Collection("Myra UI tests")]
public sealed class ClientDebugProtocolIntegrationTests
{
    private static readonly Lazy<Game> HeadlessMyraGame = new(CreateHeadlessMyraGame);

    static ClientDebugProtocolIntegrationTests()
    {
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
    }

    [Fact]
    public async Task ValidationSequence_ExportsEntireVisualTreeJsonThroughGrpc()
    {
        SetupMyra();
        var fixture = new DebugUiFixture();
        await using DebugProtocolHarness harness = await DebugProtocolHarness.StartAsync(fixture);

        DumpVisualTreeResponse response = await harness.InvokeAsync(client =>
            client.DumpVisualTreeAsync(new DumpVisualTreeRequest { IncludeHidden = false }).ResponseAsync);

        Assert.True(response.Result.Success);
        using JsonDocument document = JsonDocument.Parse(response.Json);
        JsonElement root = document.RootElement;
        Assert.False(root.GetProperty("requestedIncludeHidden").GetBoolean());
        AssertCompleteVisualTreeJson(root);

        Assert.True(root.GetProperty("hierarchy").GetProperty("validation").GetProperty("isValid").GetBoolean());
        JsonElement labelElement = FindElement(root, "hud.label");
        Assert.Equal("hud.commands", labelElement.GetProperty("parentId").GetString());
        Assert.Equal(3, labelElement.GetProperty("depth").GetInt32());
        Assert.Equal("Before", labelElement.GetProperty("text").GetString());

        JsonElement hiddenElement = FindElement(root, "hud.hidden");
        Assert.False(hiddenElement.GetProperty("treeVisible").GetBoolean());
        Assert.Equal("hud.root", hiddenElement.GetProperty("parentId").GetString());
        Assert.Contains(
            hiddenElement.GetProperty("warnings").EnumerateArray(),
            warning => warning.GetString() == "hidden");

        Assert.Contains(
            root.GetProperty("hierarchy").GetProperty("roots").EnumerateArray(),
            node => node.GetProperty("id").GetString() == GameUiVisualElementIds.BackBuffer);
        Assert.Contains(
            root.GetProperty("hierarchy").GetProperty("roots").EnumerateArray(),
            node => node.GetProperty("id").GetString() == GameUiVisualElementIds.MyraDesktop);
    }

    [Fact]
    public async Task ValidationSequence_ExercisesEveryUiDebugGrpcFunction()
    {
        SetupMyra();
        var fixture = new DebugUiFixture();
        await using DebugProtocolHarness harness = await DebugProtocolHarness.StartAsync(fixture);

        ElementTextResponse before = await harness.InvokeAsync(client =>
            client.GetElementTextAsync(new ClientDebugElementRequest { ElementId = "hud.label" }).ResponseAsync);
        ClientDebugActionResponse setText = await harness.InvokeAsync(client =>
            client.SetElementTextAsync(new SetElementTextRequest { ElementId = "hud.label", Text = "After" }).ResponseAsync);
        ElementTextResponse after = await harness.InvokeAsync(client =>
            client.GetElementTextAsync(new ClientDebugElementRequest { ElementId = "hud.label" }).ResponseAsync);
        ClientDebugActionResponse setWidth = await harness.InvokeAsync(client =>
            client.SetElementPropertyAsync(new SetElementPropertyRequest
            {
                ElementId = "hud.label",
                PropertyName = "Width",
                Value = "240"
            }).ResponseAsync);
        ClientDebugActionResponse focus = await harness.InvokeAsync(client =>
            client.FocusElementAsync(new FocusElementRequest
            {
                ElementId = GameUiVisualElementIds.MapViewport,
                ShowDebugWindow = true
            }).ResponseAsync);
        ClientDebugActionResponse navigate = await harness.InvokeAsync(client =>
            client.NavigateToScreenAsync(new NavigateToScreenRequest { ScreenId = "main-menu" }).ResponseAsync);
        ClientDebugActionResponse click = await harness.InvokeAsync(client =>
            client.InvokeClickAsync(new ClientDebugElementRequest { ElementId = "hud.button" }).ResponseAsync);

        ElementTextResponse badText = await harness.InvokeAsync(client =>
            client.GetElementTextAsync(new ClientDebugElementRequest { ElementId = GameUiVisualElementIds.MapViewport }).ResponseAsync);
        ClientDebugActionResponse badProperty = await harness.InvokeAsync(client =>
            client.SetElementPropertyAsync(new SetElementPropertyRequest
            {
                ElementId = "hud.label",
                PropertyName = "Parent",
                Value = "bad"
            }).ResponseAsync);
        ClientDebugActionResponse badClick = await harness.InvokeAsync(client =>
            client.InvokeClickAsync(new ClientDebugElementRequest { ElementId = "hud.label" }).ResponseAsync);
        ClientDebugActionResponse badFocus = await harness.InvokeAsync(client =>
            client.FocusElementAsync(new FocusElementRequest { ElementId = "missing.element" }).ResponseAsync);
        ClientDebugActionResponse badNavigate = await harness.InvokeAsync(client =>
            client.NavigateToScreenAsync(new NavigateToScreenRequest { ScreenId = " " }).ResponseAsync);

        Assert.True(before.Result.Success);
        Assert.Equal("Before", before.Text);
        Assert.True(setText.Success);
        Assert.True(after.Result.Success);
        Assert.Equal("After", after.Text);
        Assert.True(setWidth.Success);
        Assert.Equal(240, fixture.Label.Width);
        Assert.True(focus.Success);
        Assert.Equal(GameUiVisualElementIds.MapViewport, fixture.FocusedElementId);
        Assert.True(fixture.FocusedWithDebugWindow);
        Assert.True(navigate.Success);
        Assert.Equal("main-menu", fixture.NavigatedScreenId);
        Assert.True(click.Success);
        Assert.Equal(1, fixture.ClickCount);
        Assert.False(badText.Result.Success);
        Assert.False(badProperty.Success);
        Assert.False(badClick.Success);
        Assert.False(badFocus.Success);
        Assert.False(badNavigate.Success);
    }

    [Fact]
    public async Task ValidationSequence_ComparesVisualTreeStateChangeAgainstExpectedInteractionYaml()
    {
        VisualTreeInteractionExpectation expectation = VisualTreeInteractionExpectation.Load(
            "RiskyStars.Tests/InteractionStateExpectations/debug-hud-button-click.yaml");
        SetupMyra();
        var fixture = new DebugUiFixture();
        await using DebugProtocolHarness harness = await DebugProtocolHarness.StartAsync(fixture);

        DumpVisualTreeResponse beforeResponse = await harness.InvokeAsync(client =>
            client.DumpVisualTreeAsync(new DumpVisualTreeRequest { IncludeHidden = true }).ResponseAsync);
        ClientDebugActionResponse clickResponse = await harness.InvokeAsync(client =>
            client.InvokeClickAsync(new ClientDebugElementRequest { ElementId = expectation.Action.ElementId }).ResponseAsync);
        DumpVisualTreeResponse afterResponse = await harness.InvokeAsync(client =>
            client.DumpVisualTreeAsync(new DumpVisualTreeRequest { IncludeHidden = true }).ResponseAsync);

        Assert.True(beforeResponse.Result.Success);
        Assert.True(clickResponse.Success);
        Assert.True(afterResponse.Result.Success);

        using JsonDocument beforeDocument = JsonDocument.Parse(beforeResponse.Json);
        using JsonDocument afterDocument = JsonDocument.Parse(afterResponse.Json);

        expectation.AssertMatches(beforeDocument.RootElement, afterDocument.RootElement);
    }

    [Fact]
    public void ValidationSequence_ExportsScrollerContentForClickableChildren()
    {
        SetupMyra();
        var root = new Panel
        {
            Width = 500,
            Height = 300
        };
        var content = new VerticalStackPanel
        {
            Width = 400,
            Height = 240
        };
        var nestedButton = new MyraButton
        {
            Width = 180,
            Height = 40,
            Content = new Label
            {
                Text = "Nested Action"
            }
        };
        content.Widgets.Add(nestedButton);
        var scrollViewer = new ScrollViewer
        {
            Content = content,
            Width = 420,
            Height = 180
        };
        root.Widgets.Add(scrollViewer);

        var tree = new GameUiVisualTree();
        tree.AddMyraRoot(GameUiVisualElementIds.MyraDesktop, new Rectangle(0, 0, 1280, 720));
        tree.AddMyraTree(GameUiVisualElementIds.MyraDesktop, [root]);
        var controller = new ClientDebugController(
            () => tree,
            () => GameUiScaleContext.Create(1280, 720, 1280, 720, 100, 1f),
            (_, _) => ClientDebugActionResult.Ok("focused"),
            _ => ClientDebugActionResult.Ok("navigated"));

        ClientDebugJsonResult result = controller.DumpVisualTree(includeHidden: true);

        Assert.True(result.Success);
        using JsonDocument document = JsonDocument.Parse(result.Json);
        JsonElement labelElement = Assert.Single(
            document.RootElement.GetProperty("elements").EnumerateArray(),
            element => element.TryGetProperty("text", out JsonElement textElement) &&
                       textElement.GetString() == "Nested Action");
        string parentId = labelElement.GetProperty("parentId").GetString() ?? string.Empty;
        Assert.False(string.IsNullOrWhiteSpace(parentId));
        JsonElement buttonElement = Assert.Single(
            document.RootElement.GetProperty("elements").EnumerateArray(),
            element => element.GetProperty("id").GetString() == parentId);
        Assert.Contains("Button", buttonElement.GetProperty("typeName").GetString(), StringComparison.OrdinalIgnoreCase);
    }

    private static void AssertCompleteVisualTreeJson(JsonElement root)
    {
        Dictionary<string, JsonElement> elementsById = root
            .GetProperty("elements")
            .EnumerateArray()
            .ToDictionary(
                element => element.GetProperty("id").GetString() ?? string.Empty,
                element => element,
                StringComparer.Ordinal);
        Assert.DoesNotContain(string.Empty, elementsById.Keys);

        var nestedIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (JsonElement node in root.GetProperty("hierarchy").GetProperty("roots").EnumerateArray())
        {
            VisitNode(node, nestedIds);
        }

        Assert.Equal(elementsById.Keys.Order(StringComparer.Ordinal), nestedIds.Order(StringComparer.Ordinal));
        foreach (KeyValuePair<string, JsonElement> pair in elementsById)
        {
            string id = pair.Key;
            JsonElement element = pair.Value;
            string? parentId = element.TryGetProperty("parentId", out JsonElement parentValue)
                ? parentValue.GetString()
                : null;

            if (!string.IsNullOrWhiteSpace(parentId))
            {
                Assert.True(elementsById.ContainsKey(parentId), $"Parent '{parentId}' for '{id}' was not exported.");
                Assert.Contains(
                    id,
                    elementsById[parentId].GetProperty("childIds").EnumerateArray().Select(child => child.GetString()));
            }

            foreach (string childId in element.GetProperty("childIds").EnumerateArray().Select(child => child.GetString() ?? string.Empty))
            {
                Assert.True(elementsById.TryGetValue(childId, out JsonElement child), $"Child '{childId}' for '{id}' was not exported.");
                Assert.Equal(id, child.GetProperty("parentId").GetString());
            }
        }
    }

    private static void VisitNode(JsonElement node, ISet<string> ids)
    {
        string id = node.GetProperty("id").GetString() ?? string.Empty;
        Assert.False(string.IsNullOrWhiteSpace(id));
        Assert.True(ids.Add(id), $"Nested visual tree duplicated '{id}'.");
        foreach (JsonElement child in node.GetProperty("children").EnumerateArray())
        {
            Assert.Equal(id, child.GetProperty("parentId").GetString());
            VisitNode(child, ids);
        }
    }

    private static JsonElement FindElement(JsonElement root, string id)
    {
        return Assert.Single(
            root.GetProperty("elements").EnumerateArray(),
            element => element.GetProperty("id").GetString() == id);
    }

    private static void SetupMyra()
    {
        MyraEnvironment.Game = HeadlessMyraGame.Value;
        Stylesheet.Current = DefaultAssets.DefaultStylesheet;
        ThemeManager.Initialize();
        ThemeManager.ApplyThemeSettings(new RiskyStars.Client.Settings());
    }

    private static Game CreateHeadlessMyraGame()
    {
        var game = new Game();
        var graphics = new GraphicsDeviceManager(game)
        {
            PreferredBackBufferWidth = 1,
            PreferredBackBufferHeight = 1
        };
        graphics.ApplyChanges();
        game.RunOneFrame();
        return game;
    }

    private sealed class DebugUiFixture
    {
        private readonly Panel _root = new()
        {
            Left = 10,
            Top = 20,
            Width = 500,
            Height = 360
        };

        private readonly Panel _commands = new()
        {
            Left = 30,
            Top = 40,
            Width = 320,
            Height = 220
        };

        private readonly Panel _hidden = new()
        {
            Left = 360,
            Top = 48,
            Width = 80,
            Height = 50,
            Visible = false
        };

        private readonly MyraButton _button = new()
        {
            Left = 8,
            Top = 56,
            Width = 120,
            Height = 40
        };

        public DebugUiFixture()
        {
            Label = new Label
            {
                Left = 8,
                Top = 10,
                Width = 160,
                Height = 30,
                Text = "Before"
            };
            _button.Click += (_, _) =>
            {
                ClickCount++;
                Label.Text = "After Click";
                _hidden.Visible = true;
            };
            _commands.Widgets.Add(Label);
            _commands.Widgets.Add(_button);
            _root.Widgets.Add(_commands);
            _root.Widgets.Add(_hidden);
        }

        public Label Label { get; }

        public int ClickCount { get; private set; }

        public string? FocusedElementId { get; private set; }

        public string? NavigatedScreenId { get; private set; }

        public bool FocusedWithDebugWindow { get; private set; }

        public GameUiVisualTree BuildTree()
        {
            var tree = new GameUiVisualTree();
            tree.AddXnaElement(GameUiVisualElementIds.BackBuffer, new Rectangle(0, 0, 1280, 720), "BackBuffer");
            tree.AddXnaElement(
                GameUiVisualElementIds.MapViewport,
                new Rectangle(100, 80, 900, 560),
                "MapViewport",
                GameUiVisualElementIds.BackBuffer);
            tree.AddMyraRoot(GameUiVisualElementIds.MyraDesktop, new Rectangle(0, 0, 1280, 720));
            tree.AddMyraElement("hud.root", _root, GameUiVisualElementIds.MyraDesktop);
            tree.AddMyraElement("hud.commands", _commands);
            tree.AddMyraElement("hud.label", Label);
            tree.AddMyraElement("hud.button", _button);
            tree.AddMyraElement("hud.hidden", _hidden);
            tree.AddMyraTree(GameUiVisualElementIds.MyraDesktop, new[] { _root });
            return tree;
        }

        public ClientDebugController CreateController()
        {
            return new ClientDebugController(
                BuildTree,
                () => GameUiScaleContext.Create(1280, 720, 1280, 720, 100, 1f),
                FocusElement,
                NavigateToScreen);
        }

        private ClientDebugActionResult FocusElement(string elementId, bool showDebugWindow)
        {
            FocusedElementId = elementId;
            FocusedWithDebugWindow = showDebugWindow;
            return ClientDebugActionResult.Ok($"Focused {elementId}.");
        }

        private ClientDebugActionResult NavigateToScreen(string screenId)
        {
            NavigatedScreenId = screenId;
            return ClientDebugActionResult.Ok($"Navigated to {screenId}.");
        }
    }

    private sealed class DebugProtocolHarness : IAsyncDisposable
    {
        private readonly ClientDebugCommandQueue _queue;
        private readonly ClientDebugController _controller;
        private readonly ClientDebugGrpcHost _host;
        private readonly GrpcChannel _channel;

        private DebugProtocolHarness(
            ClientDebugCommandQueue queue,
            ClientDebugController controller,
            ClientDebugGrpcHost host,
            GrpcChannel channel)
        {
            _queue = queue;
            _controller = controller;
            _host = host;
            _channel = channel;
            Client = new ClientDebugProtocol.ClientDebugProtocolClient(channel);
        }

        public ClientDebugProtocol.ClientDebugProtocolClient Client { get; }

        public static async Task<DebugProtocolHarness> StartAsync(DebugUiFixture fixture)
        {
            var queue = new ClientDebugCommandQueue();
            var host = new ClientDebugGrpcHost(queue, FindFreePort());
            await host.StartAsync();
            GrpcChannel channel = GrpcChannel.ForAddress(host.ServerUrl!);
            return new DebugProtocolHarness(queue, fixture.CreateController(), host, channel);
        }

        public async Task<TResponse> InvokeAsync<TResponse>(
            Func<ClientDebugProtocol.ClientDebugProtocolClient, Task<TResponse>> call)
        {
            Task<TResponse> responseTask = call(Client);
            for (int attempt = 0; attempt < 500 && !responseTask.IsCompleted; attempt++)
            {
                _queue.Drain(_controller);
                await Task.Delay(5);
            }

            _queue.Drain(_controller);
            if (!responseTask.IsCompleted)
            {
                throw new TimeoutException("Timed out waiting for the client debug gRPC command to drain.");
            }

            return await responseTask;
        }

        public async ValueTask DisposeAsync()
        {
            _channel.Dispose();
            await _host.DisposeAsync();
        }

        private static int FindFreePort()
        {
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
    }
}

internal sealed class VisualTreeInteractionExpectation
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public string InteractionId { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public VisualTreeInteractionAction Action { get; set; } = new();

    public List<VisualTreeExpectedElement> Before { get; set; } = [];

    public List<VisualTreeExpectedElement> After { get; set; } = [];

    public List<VisualTreeExpectedChange> StateChanges { get; set; } = [];

    public List<VisualTreeBadBehaviorValidation> BadBehaviorValidations { get; set; } = [];

    public static VisualTreeInteractionExpectation Load(string relativePath)
    {
        string fullPath = AgentWireframeComparisonScenarioCatalog.ResolveRepositoryPath(relativePath);
        VisualTreeInteractionExpectation? expectation = Deserializer.Deserialize<VisualTreeInteractionExpectation>(
            File.ReadAllText(fullPath));
        if (expectation == null)
        {
            throw new InvalidDataException($"Could not deserialize visual tree interaction expectation {relativePath}.");
        }

        expectation.Validate();
        return expectation;
    }

    public void AssertMatches(JsonElement beforeRoot, JsonElement afterRoot)
    {
        foreach (VisualTreeExpectedElement expected in Before)
        {
            expected.AssertMatches(beforeRoot, phase: "before");
        }

        foreach (VisualTreeExpectedElement expected in After)
        {
            expected.AssertMatches(afterRoot, phase: "after");
        }

        foreach (VisualTreeExpectedChange change in StateChanges)
        {
            JsonElement beforeElement = FindElement(beforeRoot, change.ElementId);
            JsonElement afterElement = FindElement(afterRoot, change.ElementId);
            Assert.Equal(change.Before, ReadPropertyAsString(beforeElement, change.Property));
            Assert.Equal(change.After, ReadPropertyAsString(afterElement, change.Property));
        }

        foreach (VisualTreeBadBehaviorValidation badBehavior in BadBehaviorValidations)
        {
            JsonElement afterElement = FindElement(afterRoot, badBehavior.ElementId);
            Assert.NotEqual(badBehavior.RejectedValue, ReadPropertyAsString(afterElement, badBehavior.Property));
        }
    }

    private void Validate()
    {
        Assert.False(string.IsNullOrWhiteSpace(InteractionId), "interactionId is required.");
        Assert.False(string.IsNullOrWhiteSpace(Action.ElementId), "action.elementId is required.");
        Assert.NotEmpty(Before);
        Assert.NotEmpty(After);
        Assert.NotEmpty(StateChanges);
        Assert.NotEmpty(BadBehaviorValidations);
    }

    private static JsonElement FindElement(JsonElement root, string elementId)
    {
        return Assert.Single(
            root.GetProperty("elements").EnumerateArray(),
            element => element.GetProperty("id").GetString() == elementId);
    }

    private static string ReadPropertyAsString(JsonElement element, string property)
    {
        if (string.Equals(property, "text", StringComparison.Ordinal))
        {
            return element.TryGetProperty("text", out JsonElement textElement) && textElement.ValueKind != JsonValueKind.Null
                ? textElement.GetString() ?? string.Empty
                : string.Empty;
        }

        if (string.Equals(property, "treeVisible", StringComparison.Ordinal))
        {
            return element.GetProperty("treeVisible").GetBoolean().ToString().ToLowerInvariant();
        }

        if (string.Equals(property, "visible", StringComparison.Ordinal))
        {
            return element.GetProperty("visible").GetBoolean().ToString().ToLowerInvariant();
        }

        throw new InvalidDataException($"Unsupported visual tree expectation property '{property}'.");
    }
}

internal sealed class VisualTreeInteractionAction
{
    public string ElementId { get; set; } = string.Empty;

    public string Invoke { get; set; } = string.Empty;
}

internal sealed class VisualTreeExpectedElement
{
    public string ElementId { get; set; } = string.Empty;

    public string? Text { get; set; }

    public bool? TreeVisible { get; set; }

    public void AssertMatches(JsonElement root, string phase)
    {
        JsonElement element = Assert.Single(
            root.GetProperty("elements").EnumerateArray(),
            candidate => candidate.GetProperty("id").GetString() == ElementId);

        if (Text != null)
        {
            Assert.Equal(Text, element.GetProperty("text").GetString());
        }

        if (TreeVisible.HasValue)
        {
            Assert.Equal(TreeVisible.Value, element.GetProperty("treeVisible").GetBoolean());
        }
    }
}

internal sealed class VisualTreeExpectedChange
{
    public string ElementId { get; set; } = string.Empty;

    public string Property { get; set; } = string.Empty;

    public string Before { get; set; } = string.Empty;

    public string After { get; set; } = string.Empty;
}

internal sealed class VisualTreeBadBehaviorValidation
{
    public string ElementId { get; set; } = string.Empty;

    public string Property { get; set; } = string.Empty;

    public string RejectedValue { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;
}
