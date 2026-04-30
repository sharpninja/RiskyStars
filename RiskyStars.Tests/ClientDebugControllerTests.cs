using System.Text.Json;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI;
using RiskyStars.Client;
using MyraButton = Myra.Graphics2D.UI.Button;

namespace RiskyStars.Tests;

[Collection("Myra UI tests")]
public sealed class ClientDebugControllerTests
{
    [Fact]
    public void DumpVisualTree_IncludesVisibleElementsAsJson()
    {
        var tree = new GameUiVisualTree();
        tree.AddXnaElement("xna.map", new Rectangle(10, 20, 300, 200), "MapViewport");
        var controller = CreateController(tree);

        ClientDebugJsonResult result = controller.DumpVisualTree(includeHidden: false);

        Assert.True(result.Success);
        using JsonDocument document = JsonDocument.Parse(result.Json);
        JsonElement elements = document.RootElement.GetProperty("elements");
        JsonElement element = Assert.Single(elements.EnumerateArray());
        Assert.Equal("xna.map", element.GetProperty("id").GetString());
        Assert.Equal(300, element.GetProperty("screenBounds").GetProperty("width").GetInt32());
    }

    [Fact]
    public void DumpVisualTree_IncludesHiddenElementsEvenWhenIncludeHiddenIsFalse()
    {
        var hiddenPanel = new Panel
        {
            Visible = false,
            Width = 100,
            Height = 50
        };
        var tree = new GameUiVisualTree();
        tree.AddMyraElement("hud.hidden", hiddenPanel);
        var controller = CreateController(tree);

        ClientDebugJsonResult result = controller.DumpVisualTree(includeHidden: false);

        Assert.True(result.Success);
        using JsonDocument document = JsonDocument.Parse(result.Json);
        Assert.False(document.RootElement.GetProperty("requestedIncludeHidden").GetBoolean());
        JsonElement element = Assert.Single(document.RootElement.GetProperty("elements").EnumerateArray());
        Assert.Equal("hud.hidden", element.GetProperty("id").GetString());
        Assert.False(element.GetProperty("treeVisible").GetBoolean());
        Assert.Contains(
            element.GetProperty("warnings").EnumerateArray(),
            warning => warning.GetString() == "hidden");
    }

    [Fact]
    public void DumpVisualTree_IncludesHiddenElementsWhenRequested()
    {
        var hiddenPanel = new Panel
        {
            Visible = false,
            Width = 100,
            Height = 50
        };
        var tree = new GameUiVisualTree();
        tree.AddMyraElement("hud.hidden", hiddenPanel);
        var controller = CreateController(tree);

        ClientDebugJsonResult result = controller.DumpVisualTree(includeHidden: true);

        Assert.True(result.Success);
        using JsonDocument document = JsonDocument.Parse(result.Json);
        JsonElement element = Assert.Single(document.RootElement.GetProperty("elements").EnumerateArray());
        Assert.Equal("hud.hidden", element.GetProperty("id").GetString());
        Assert.False(element.GetProperty("treeVisible").GetBoolean());
    }

    [Fact]
    public void FocusElement_FocusesExistingElementAndRejectsMissingElement()
    {
        var tree = new GameUiVisualTree();
        tree.AddXnaElement("xna.map", new Rectangle(0, 0, 10, 10), "Map");
        string? focusedElement = null;
        bool? showDebugWindow = null;
        var controller = CreateController(
            tree,
            (id, show) =>
            {
                focusedElement = id;
                showDebugWindow = show;
                return ClientDebugActionResult.Ok("focused");
            });

        ClientDebugActionResult success = controller.FocusElement("xna.map", showDebugWindow: true);
        ClientDebugActionResult failure = controller.FocusElement("xna.missing", showDebugWindow: true);

        Assert.True(success.Success);
        Assert.Equal("xna.map", focusedElement);
        Assert.True(showDebugWindow);
        Assert.False(failure.Success);
        Assert.Contains("not present", failure.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FocusElement_RejectsBlankElementId()
    {
        var controller = CreateController(new GameUiVisualTree());

        ClientDebugActionResult result = controller.FocusElement(" ", showDebugWindow: true);
        ClientDebugActionResult nullResult = controller.FocusElement(null!, showDebugWindow: true);

        Assert.False(result.Success);
        Assert.Contains("required", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(nullResult.Success);
    }

    [Fact]
    public void NavigateToScreen_InvokesDelegateAndRejectsBadRequests()
    {
        string? navigatedScreen = null;
        var controller = CreateController(
            new GameUiVisualTree(),
            navigateToScreen: screenId =>
            {
                navigatedScreen = screenId;
                return ClientDebugActionResult.Ok($"navigated {screenId}");
            });
        var missingDelegateController = CreateController(new GameUiVisualTree());

        ClientDebugActionResult success = controller.NavigateToScreen("main-menu");
        ClientDebugActionResult blank = controller.NavigateToScreen(" ");
        ClientDebugActionResult missingDelegate = missingDelegateController.NavigateToScreen("main-menu");

        Assert.True(success.Success);
        Assert.Equal("main-menu", navigatedScreen);
        Assert.False(blank.Success);
        Assert.Contains("required", blank.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(missingDelegate.Success);
        Assert.Contains("not available", missingDelegate.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetAndSetElementText_ReadsAndUpdatesLabelText()
    {
        var label = new Label
        {
            Text = "Before"
        };
        var tree = new GameUiVisualTree();
        tree.AddMyraElement("hud.label", label);
        var controller = CreateController(tree);

        ClientDebugTextResult before = controller.GetElementText("hud.label");
        ClientDebugActionResult set = controller.SetElementText("hud.label", "After");
        ClientDebugTextResult after = controller.GetElementText("hud.label");

        Assert.True(before.Success);
        Assert.Equal("Before", before.Text);
        Assert.True(set.Success);
        Assert.True(after.Success);
        Assert.Equal("After", after.Text);
    }

    [Fact]
    public void GetAndSetElementText_RejectsNonTextWidget()
    {
        var panel = new Panel();
        var tree = new GameUiVisualTree();
        tree.AddMyraElement("hud.panel", panel);
        var controller = CreateController(tree);

        ClientDebugTextResult read = controller.GetElementText("hud.panel");
        ClientDebugActionResult write = controller.SetElementText("hud.panel", "Noop");

        Assert.False(read.Success);
        Assert.False(write.Success);
    }

    [Fact]
    public void GetAndSetElementText_RejectsMissingOrBlankElementIds()
    {
        var tree = new GameUiVisualTree();
        tree.AddXnaElement("xna.map", new Rectangle(0, 0, 10, 10), "Map");
        var controller = CreateController(tree);

        ClientDebugTextResult readMissing = controller.GetElementText("xna.map");
        ClientDebugActionResult writeBlank = controller.SetElementText(" ", "Noop");
        ClientDebugTextResult readNull = controller.GetElementText(null!);

        Assert.False(readMissing.Success);
        Assert.Contains("not a Myra", readMissing.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(writeBlank.Success);
        Assert.Contains("required", writeBlank.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(readNull.Success);
    }

    [Fact]
    public void SetElementProperty_UpdatesAllowedPropertiesAndRejectsDisallowedProperties()
    {
        var panel = new Panel
        {
            Visible = true,
            Width = 100
        };
        var tree = new GameUiVisualTree();
        tree.AddMyraElement("hud.panel", panel);
        var controller = CreateController(tree);

        ClientDebugActionResult visibleResult = controller.SetElementProperty("hud.panel", "Visible", "false");
        ClientDebugActionResult widthResult = controller.SetElementProperty("hud.panel", "Width", "225");
        ClientDebugActionResult unsafeResult = controller.SetElementProperty("hud.panel", "Parent", "anything");

        Assert.True(visibleResult.Success);
        Assert.False(panel.Visible);
        Assert.True(widthResult.Success);
        Assert.Equal(225, panel.Width);
        Assert.False(unsafeResult.Success);
        Assert.Contains("not allowed", unsafeResult.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SetElementProperty_HandlesTextBoolAliasNullableAndMissingWidgetFailures()
    {
        var label = new Label
        {
            Text = "Before"
        };
        var panel = new Panel
        {
            Visible = false,
            Width = 100
        };
        var tree = new GameUiVisualTree();
        tree.AddMyraElement("hud.label", label);
        tree.AddMyraElement("hud.panel", panel);
        tree.AddXnaElement("xna.map", new Rectangle(0, 0, 10, 10), "Map");
        var controller = CreateController(tree);

        ClientDebugActionResult emptyName = controller.SetElementProperty("hud.panel", " ", "ignored");
        ClientDebugActionResult missingWidget = controller.SetElementProperty("xna.map", "Visible", "true");
        ClientDebugActionResult missingProperty = controller.SetElementProperty("hud.panel", "IsEnabled", "true");
        ClientDebugActionResult text = controller.SetElementProperty("hud.label", "Text", "After");
        ClientDebugActionResult boolAlias = controller.SetElementProperty("hud.panel", "Visible", "on");
        ClientDebugActionResult nullable = controller.SetElementProperty("hud.panel", "Width", "");

        Assert.False(emptyName.Success);
        Assert.False(missingWidget.Success);
        Assert.False(missingProperty.Success);
        Assert.True(text.Success);
        Assert.Equal("After", label.Text);
        Assert.True(boolAlias.Success);
        Assert.True(panel.Visible);
        Assert.True(nullable.Success);
        Assert.Null(panel.Width);
    }

    [Fact]
    public void SetElementProperty_ParsesOffBoolAlias()
    {
        var panel = new Panel
        {
            Visible = true
        };
        var tree = new GameUiVisualTree();
        tree.AddMyraElement("hud.panel", panel);
        var controller = CreateController(tree);

        ClientDebugActionResult result = controller.SetElementProperty("hud.panel", "Visible", "off");

        Assert.True(result.Success);
        Assert.False(panel.Visible);
    }

    [Theory]
    [InlineData("1", true)]
    [InlineData("0", false)]
    public void SetElementProperty_ParsesNumericBoolAliases(string value, bool expected)
    {
        var panel = new Panel
        {
            Visible = !expected
        };
        var tree = new GameUiVisualTree();
        tree.AddMyraElement("hud.panel", panel);
        var controller = CreateController(tree);

        ClientDebugActionResult result = controller.SetElementProperty("hud.panel", "Visible", value);

        Assert.True(result.Success);
        Assert.Equal(expected, panel.Visible);
    }

    [Fact]
    public void SetElementProperty_RejectsInvalidConversions()
    {
        var panel = new Panel
        {
            Width = 100
        };
        var tree = new GameUiVisualTree();
        tree.AddMyraElement("hud.panel", panel);
        var controller = CreateController(tree);

        ClientDebugActionResult result = controller.SetElementProperty("hud.panel", "Width", "wide");

        Assert.False(result.Success);
        Assert.Equal(100, panel.Width);
        Assert.Contains("cannot be converted", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void InvokeClick_InvokesButtonClickAndRejectsNonButton()
    {
        int clickCount = 0;
        var button = new MyraButton();
        button.Click += (_, _) => clickCount++;
        var label = new Label
        {
            Text = "Not a button"
        };
        var tree = new GameUiVisualTree();
        tree.AddMyraElement("hud.button", button);
        tree.AddMyraElement("hud.label", label);
        var controller = CreateController(tree);

        ClientDebugActionResult success = controller.InvokeClick("hud.button");
        ClientDebugActionResult failure = controller.InvokeClick("hud.label");

        Assert.True(success.Success);
        Assert.Equal(1, clickCount);
        Assert.False(failure.Success);
    }

    [Fact]
    public void InvokeClick_InvokesClickEventFieldWhenNoClickMethodExists()
    {
        int clickCount = 0;
        var widget = new EventOnlyClickableWidget();
        widget.Click += (_, _) => clickCount++;
        var tree = new GameUiVisualTree();
        tree.AddMyraElement("hud.eventOnly", widget);
        var controller = CreateController(tree);

        ClientDebugActionResult result = controller.InvokeClick("hud.eventOnly");

        Assert.True(result.Success);
        Assert.Equal(1, clickCount);
    }

    [Fact]
    public void InvokeClick_CreatesDefaultArgumentsForNonEventHandlerDelegates()
    {
        int argument = -1;
        var widget = new ValueArgumentClickableWidget();
        widget.Click = value => argument = value;
        var tree = new GameUiVisualTree();
        tree.AddMyraElement("hud.valueClick", widget);
        var controller = CreateController(tree);

        ClientDebugActionResult result = controller.InvokeClick("hud.valueClick");

        Assert.True(result.Success);
        Assert.Equal(0, argument);
    }

    [Fact]
    public void InvokeClick_CreatesNullArgumentsForReferenceTypeDelegateParameters()
    {
        string? argument = "not-null";
        var widget = new ReferenceArgumentClickableWidget();
        widget.Click = value => argument = value;
        var tree = new GameUiVisualTree();
        tree.AddMyraElement("hud.referenceClick", widget);
        var controller = CreateController(tree);

        ClientDebugActionResult result = controller.InvokeClick("hud.referenceClick");

        Assert.True(result.Success);
        Assert.Null(argument);
    }

    [Fact]
    public void InvokeClick_RejectsMissingElement()
    {
        var controller = CreateController(new GameUiVisualTree());

        ClientDebugActionResult result = controller.InvokeClick("missing");

        Assert.False(result.Success);
        Assert.Contains("not a Myra", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CommandQueue_DrainsQueuedCommandsOnCallerThread()
    {
        var tree = new GameUiVisualTree();
        tree.AddXnaElement("xna.map", new Rectangle(0, 0, 10, 10), "Map");
        var controller = CreateController(tree);
        var queue = new ClientDebugCommandQueue();

        Task<ClientDebugJsonResult> pending = queue.InvokeAsync(debug => debug.DumpVisualTree(includeHidden: false));

        Assert.False(pending.IsCompleted);
        Assert.Equal(1, queue.Drain(controller));
        ClientDebugJsonResult result = await pending;
        Assert.Contains("xna.map", result.Json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CommandQueue_RespectsDrainLimit()
    {
        var tree = new GameUiVisualTree();
        tree.AddXnaElement("xna.map", new Rectangle(0, 0, 10, 10), "Map");
        var controller = CreateController(tree);
        var queue = new ClientDebugCommandQueue();
        Task<ClientDebugJsonResult> pending = queue.InvokeAsync(debug => debug.DumpVisualTree(includeHidden: false));

        Assert.Equal(0, queue.Drain(controller, maxCommands: 0));
        Assert.False(pending.IsCompleted);
        Assert.Equal(1, queue.Drain(controller));

        ClientDebugJsonResult result = await pending;
        Assert.True(result.Success);
    }

    [Fact]
    public void CommandQueue_PropagatesBadBehaviorAsFaultedTask()
    {
        var controller = CreateController(new GameUiVisualTree());
        var queue = new ClientDebugCommandQueue();

        Task<ClientDebugActionResult> pending = queue.InvokeAsync<ClientDebugActionResult>(_ => throw new InvalidOperationException("bad"));

        queue.Drain(controller);
        Assert.True(pending.IsFaulted);
        Assert.IsType<InvalidOperationException>(pending.Exception?.InnerException);
    }

    [Fact]
    public async Task CommandQueue_ReturnsCanceledTaskWhenTokenIsAlreadyCanceled()
    {
        var queue = new ClientDebugCommandQueue();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Task<ClientDebugJsonResult> pending = queue.InvokeAsync(
            controller => controller.DumpVisualTree(includeHidden: false),
            cancellation.Token);

        await Assert.ThrowsAsync<TaskCanceledException>(async () => await pending);
    }

    [Fact]
    public async Task CommandQueue_CancelsQueuedCommandWhenTokenIsCanceledBeforeDrain()
    {
        var queue = new ClientDebugCommandQueue();
        var controller = CreateController(new GameUiVisualTree());
        using var cancellation = new CancellationTokenSource();
        Task<ClientDebugJsonResult> pending = queue.InvokeAsync(
            debug => debug.DumpVisualTree(includeHidden: false),
            cancellation.Token);

        cancellation.Cancel();
        Assert.Equal(1, queue.Drain(controller));

        await Assert.ThrowsAsync<TaskCanceledException>(async () => await pending);
    }

    [Fact]
    public void ResolveVisualScreenBounds_PrefersVisualSizeOverInnerActualBounds()
    {
        Rectangle result = GameUiWidgetBoundsResolver.ResolveVisualScreenBounds(
            new Point(210, 109),
            new Rectangle(0, 0, 380, 400),
            new Rectangle(16, 16, 348, 368),
            explicitWidth: 380,
            explicitHeight: 400);

        Assert.Equal(new Rectangle(210, 109, 380, 400), result);
        Assert.NotEqual(new Rectangle(226, 125, 348, 368), result);
    }

    [Fact]
    public void ResolveVisualScreenBounds_FallsBackToActualBoundsOffsetWhenNoVisualSizeExists()
    {
        Rectangle result = GameUiWidgetBoundsResolver.ResolveVisualScreenBounds(
            new Point(210, 109),
            Rectangle.Empty,
            new Rectangle(16, 16, 348, 368),
            explicitWidth: null,
            explicitHeight: null);

        Assert.Equal(new Rectangle(226, 125, 348, 368), result);
    }

    private static ClientDebugController CreateController(
        GameUiVisualTree tree,
        Func<string, bool, ClientDebugActionResult>? focusElement = null,
        Func<string, ClientDebugActionResult>? navigateToScreen = null)
    {
        return new ClientDebugController(
            () => tree,
            () => GameUiScaleContext.Create(1920, 1080, 1920, 1080, 100, 1f),
            focusElement ?? ((id, _) => ClientDebugActionResult.Ok($"focused {id}")),
            navigateToScreen);
    }

    private sealed class EventOnlyClickableWidget : Widget
    {
        public event EventHandler? Click;

        public void RaiseForCompiler()
        {
            Click?.Invoke(this, EventArgs.Empty);
        }
    }

    private sealed class ValueArgumentClickableWidget : Widget
    {
        public Action<int>? Click;
    }

    private sealed class ReferenceArgumentClickableWidget : Widget
    {
        public Action<string?>? Click;
    }
}
