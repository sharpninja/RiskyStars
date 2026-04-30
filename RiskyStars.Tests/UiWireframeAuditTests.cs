using System.Collections;
using System.Reflection;
using Microsoft.Xna.Framework;
using Myra;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;
using RiskyStars.Client;
using RiskyStars.Shared;
using MyraButton = Myra.Graphics2D.UI.Button;

namespace RiskyStars.Tests;

[Collection("Myra UI tests")]
public sealed class UiWireframeAuditTests
{
    private const int MaximizedWireframeWidth = 1536;
    private const int MaximizedWireframeHeight = 832;
    private const int ScreenWidth = MaximizedWireframeWidth;
    private const int ScreenHeight = MaximizedWireframeHeight;
    private static readonly Game HeadlessMyraGame = CreateHeadlessMyraGame();

    public static IEnumerable<object[]> WireframeCases()
    {
        foreach (var spec in BuildSpecs())
        {
            yield return new object[] { spec };
        }
    }

    [Theory]
    [MemberData(nameof(WireframeCases))]
    public void DocumentedWireframe_MatchesActualUiTree(UiWireframeSpec spec)
    {
        SetupMyra();

        UiActualSnapshot actual = spec.CaptureActual();

        AssertWireframeMatchesActual(spec, actual);
    }

    [Fact]
    public void Documentation_ContainsEveryExecutableWireframe()
    {
        string doc = File.ReadAllText(FindRepositoryFile("RiskyStars.Client/UI_WIREFRAME_AUDIT.md"));

        Assert.Contains("PromptWireframes/render-prompt-wireframes.ps1", doc, StringComparison.Ordinal);
        Assert.Contains("PromptWireframes/wireframe-prompts.json", doc, StringComparison.Ordinal);
        Assert.DoesNotContain("Mermaid", doc, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".mmd", doc, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("render-wireframes.ps1", doc, StringComparison.Ordinal);

        foreach (var spec in BuildSpecs())
        {
            Assert.Contains($"### {spec.DisplayName}", doc, StringComparison.Ordinal);
            Assert.Contains(spec.SourcePath, doc, StringComparison.Ordinal);
            Assert.Contains(spec.ExpectedLayout, doc, StringComparison.Ordinal);
            Assert.Contains(spec.PromptPngPath, doc, StringComparison.Ordinal);
        }
    }

    [Theory]
    [MemberData(nameof(WireframeCases))]
    public void PromptWireframe_HasRenderedDirectPng(UiWireframeSpec spec)
    {
        string promptCatalog = File.ReadAllText(FindRepositoryFile("RiskyStars.Client/Wireframes/PromptWireframes/wireframe-prompts.json"));
        string promptPngPath = FindRepositoryFile(spec.PromptPngPath);

        Assert.Contains($"\"id\": \"{spec.Id}\"", promptCatalog, StringComparison.Ordinal);
        Assert.Contains("\"prompt\":", promptCatalog, StringComparison.Ordinal);
        Assert.Contains("not a Mermaid flowchart", promptCatalog, StringComparison.Ordinal);

        byte[] pngHeader = File.ReadAllBytes(promptPngPath).Take(8).ToArray();
        Assert.Equal([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A], pngHeader);
        Assert.True(new FileInfo(promptPngPath).Length > 4096, $"{spec.PromptPngPath} is too small to be a direct prompt-rendered wireframe.");

        (int width, int height) = ReadPngDimensions(promptPngPath);
        Assert.Equal(MaximizedWireframeWidth, width);
        Assert.Equal(MaximizedWireframeHeight, height);
    }

    [Fact]
    public void ScreenMatrix_LinksCanonicalWireframeAudit()
    {
        string doc = File.ReadAllText(FindRepositoryFile("RiskyStars.Client/UI_SCREEN_TEST_MATRIX.md"));

        Assert.Contains("UI_WIREFRAME_AUDIT.md", doc, StringComparison.Ordinal);
        Assert.Contains("UiWireframeAuditTests", doc, StringComparison.Ordinal);
        Assert.Contains("ClientDebugGameScreenshotIntegrationTests", doc, StringComparison.Ordinal);
        Assert.Contains("Screenshots/Actual/*.png", doc, StringComparison.Ordinal);
        Assert.Contains("Screenshots/Comparisons", doc, StringComparison.Ordinal);
        Assert.Contains("WIREFRAME_SCREENSHOT_DIVERGENCE.md", doc, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(WireframeCases))]
    public void PromptWireframe_HasRenderedSpatialPng(UiWireframeSpec spec)
    {
        string promptCatalog = File.ReadAllText(FindRepositoryFile("RiskyStars.Client/Wireframes/PromptWireframes/wireframe-prompts.json"));
        string promptPngPath = FindRepositoryFile(spec.PromptPngPath);

        Assert.Contains($"\"id\": \"{spec.Id}\"", promptCatalog, StringComparison.Ordinal);
        Assert.Contains("\"prompt\":", promptCatalog, StringComparison.Ordinal);
        Assert.Equal((MaximizedWireframeWidth, MaximizedWireframeHeight), ReadPngDimensions(promptPngPath));
        Assert.True(new FileInfo(promptPngPath).Length > 4096, $"{spec.PromptPngPath} is too small to be a rendered prompt wireframe.");
    }

    [Fact]
    public void ScreenshotComparisonArtifacts_CoverEveryDocumentedScreen()
    {
        string metrics = File.ReadAllText(FindRepositoryFile("RiskyStars.Client/Screenshots/Comparisons/comparison-metrics.csv"));
        string contactSheet = FindRepositoryFile("RiskyStars.Client/Screenshots/Comparisons/overview-contact-sheet.png");
        Assert.True(new FileInfo(contactSheet).Length > 4096, "The comparison contact sheet must be a real PNG artifact.");

        foreach (var spec in BuildSpecs())
        {
            string actualPath = FindRepositoryFile($"RiskyStars.Client/Screenshots/Actual/{spec.Id}.png");
            string wireframePath = FindRepositoryFile(spec.PromptPngPath);
            string comparisonPath = FindRepositoryFile($"RiskyStars.Client/Screenshots/Comparisons/{spec.Id}-compare.png");

            Assert.Equal((MaximizedWireframeWidth, MaximizedWireframeHeight), ReadPngDimensions(actualPath));
            Assert.Equal((MaximizedWireframeWidth, MaximizedWireframeHeight), ReadPngDimensions(wireframePath));
            Assert.True(new FileInfo(comparisonPath).Length > 4096, $"{spec.Id} comparison artifact is missing or too small.");
            Assert.Contains(spec.Id, metrics, StringComparison.Ordinal);
            Assert.Contains("prompt_wireframe_png", metrics, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void DivergenceReport_ContainsEveryScreenComparison()
    {
        string report = File.ReadAllText(FindRepositoryFile("RiskyStars.Client/Screenshots/WIREFRAME_SCREENSHOT_DIVERGENCE.md"));

        Assert.Contains("direct prompt-rendered spatial wireframes", report, StringComparison.Ordinal);
        Assert.Contains("PromptWireframes", report, StringComparison.Ordinal);
        Assert.Contains("overview-contact-sheet.png", report, StringComparison.Ordinal);
        AssertDivergenceReportCoversScreens(report, BuildSpecs().Select(spec => spec.Id));
    }

    [Fact]
    public void DivergenceReport_ReportsMissingScreenAsBadBehavior()
    {
        var failure = Assert.Throws<WireframeAuditException>(() =>
            AssertDivergenceReportCoversScreens("### main-menu", ["main-menu", "settings-window"]));

        Assert.Contains("settings-window", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void WireframeRenderScript_DefaultsToMaximizedLaptopResolution()
    {
        string promptScript = File.ReadAllText(FindRepositoryFile("RiskyStars.Client/Wireframes/PromptWireframes/render-prompt-wireframes.ps1"));
        string promptCatalog = File.ReadAllText(FindRepositoryFile("RiskyStars.Client/Wireframes/PromptWireframes/wireframe-prompts.json"));

        Assert.Contains("\"width\": 1536", promptCatalog, StringComparison.Ordinal);
        Assert.Contains("\"height\": 832", promptCatalog, StringComparison.Ordinal);
        Assert.Contains("wireframe-prompts.json", promptScript, StringComparison.Ordinal);
        Assert.DoesNotContain("mmdc", promptScript, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("mermaid", promptScript, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("System.Drawing", promptScript, StringComparison.Ordinal);
    }

    [Fact]
    public void WireframeAudit_RejectsMermaidAsCanonicalBaseline()
    {
        var spec = BuildSpecs().First();
        string obsoleteMermaidPath = $"RiskyStars.Client/Wireframes/{spec.Id}.mmd";

        var failure = Assert.Throws<WireframeAuditException>(() =>
            AssertPromptBaselinePathIsCanonical(obsoleteMermaidPath));

        Assert.Contains("PromptWireframes", failure.Message, StringComparison.Ordinal);
        Assert.Contains("Mermaid", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void WireframeAudit_ReportsMissingTextAsBadBehavior()
    {
        SetupMyra();
        var spec = BuildSpecs().First(item => item.Id == "main-menu")
            with { RequiredText = ["RiskyStars", "THIS TEXT MUST NOT EXIST"] };
        UiActualSnapshot actual = spec.CaptureActual();

        var failure = Assert.Throws<WireframeAuditException>(() => AssertWireframeMatchesActual(spec, actual));

        Assert.Contains("THIS TEXT MUST NOT EXIST", failure.Message, StringComparison.Ordinal);
        Assert.Contains("text", failure.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WireframeAudit_ReportsMissingScrollerAsBadBehavior()
    {
        SetupMyra();
        var root = new Panel();
        root.Widgets.Add(new VerticalStackPanel());
        var spec = new UiWireframeSpec(
            "bad-no-scroll",
            "Bad no-scroll panel",
            "test",
            "Panel > ScrollViewer > Stack",
            () => SnapshotFromRoot(root),
            RequiredText: [],
            RequiredWidgetTypes: ["Panel", "VerticalStackPanel"],
            MinimumScrollViewers: 1,
            MinimumButtons: 0,
            MinimumPanels: 1,
            MinimumGrids: 0,
            MinimumTreeDepth: 2);

        var failure = Assert.Throws<WireframeAuditException>(() => AssertWireframeMatchesActual(spec, spec.CaptureActual()));

        Assert.Contains("ScrollViewer", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void WireframeAudit_ReportsFlatTreeAsBadBehavior()
    {
        SetupMyra();
        var root = new Panel();
        root.Widgets.Add(new Label { Text = "Flat" });
        var spec = new UiWireframeSpec(
            "bad-flat",
            "Bad flat panel",
            "test",
            "Panel > Frame > ScrollViewer > Stack > Content",
            () => SnapshotFromRoot(root),
            RequiredText: ["Flat"],
            RequiredWidgetTypes: ["Panel", "Label"],
            MinimumScrollViewers: 0,
            MinimumButtons: 0,
            MinimumPanels: 1,
            MinimumGrids: 0,
            MinimumTreeDepth: 5);

        var failure = Assert.Throws<WireframeAuditException>(() => AssertWireframeMatchesActual(spec, spec.CaptureActual()));

        Assert.Contains("depth", failure.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<UiWireframeSpec> BuildSpecs()
    {
        return
        [
            new(
                "main-menu",
                "Main menu",
                "RiskyStars.Client/UI/Screens/MainMenu.cs",
                "ScreenRoot > ViewportFrame > ScrollViewer > Grid: briefing + command actions + footer",
                () => CaptureMainMenu(MainMenuState.Main),
                ["RiskyStars", "Command Actions", "Multiplayer", "Single Player", "Tutorial Mode", "Settings", "Exit"],
                ["Panel", "Grid", "ScrollViewer", "Button", "Label"],
                MinimumScrollViewers: 1,
                MinimumButtons: 5,
                MinimumPanels: 3,
                MinimumGrids: 2,
                MinimumTreeDepth: 5),
            new(
                "main-menu-settings",
                "Main menu settings",
                "RiskyStars.Client/UI/Screens/MainMenu.cs",
                "ScreenRoot > ViewportFrame > Grid: header + bounded settings scroller + action bar",
                () => CaptureMainMenu(MainMenuState.Settings),
                ["Command Settings", "Server Endpoint", "Display Profile", "Visual Palette", "Save Settings", "Back"],
                ["Panel", "Grid", "ScrollViewer", "Button", "ComboBox"],
                MinimumScrollViewers: 1,
                MinimumButtons: 2,
                MinimumPanels: 4,
                MinimumGrids: 4,
                MinimumTreeDepth: 5),
            new(
                "main-menu-connecting",
                "Main menu connecting",
                "RiskyStars.Client/UI/Screens/MainMenu.cs",
                "ScreenRoot > ViewportFrame > ScrollViewer > Stack: uplink message",
                () => CaptureMainMenu(MainMenuState.Connecting),
                ["Establishing Uplink", "Connecting to the sector network"],
                ["Panel", "ScrollViewer", "VerticalStackPanel", "Label"],
                MinimumScrollViewers: 1,
                MinimumButtons: 0,
                MinimumPanels: 2,
                MinimumGrids: 0,
                MinimumTreeDepth: 4),
            new(
                "game-mode-selector",
                "Game mode selector",
                "RiskyStars.Client/UI/Screens/GameModeSelector.cs",
                "ScreenRoot > ViewportFrame > ScrollViewer > Stack: header + two mode cards + actions",
                () => CaptureScreen(new GameModeSelector(null!, ScreenWidth, ScreenHeight)),
                ["Select Game Mode", "Multiplayer", "Single Player", "Continue", "Back"],
                ["Panel", "Grid", "ScrollViewer", "Button", "Label"],
                MinimumScrollViewers: 1,
                MinimumButtons: 2,
                MinimumPanels: 4,
                MinimumGrids: 1,
                MinimumTreeDepth: 5),
            new(
                "connection-screen",
                "Connection screen",
                "RiskyStars.Client/UI/Screens/ConnectionScreen.cs",
                "ScreenRoot > ViewportFrame > ScrollViewer > Grid: identity/server fields + link-state actions",
                () => CaptureScreen(new ConnectionScreen(null!, ScreenWidth, ScreenHeight)),
                ["Multiplayer Uplink", "Commander Identity", "Server Endpoint", "Connect", "Back"],
                ["Panel", "Grid", "ScrollViewer", "Button", "TextBox"],
                MinimumScrollViewers: 1,
                MinimumButtons: 2,
                MinimumPanels: 4,
                MinimumGrids: 2,
                MinimumTreeDepth: 5),
            new(
                "lobby-browser",
                "Lobby browser",
                "RiskyStars.Client/UI/Screens/LobbyBrowserScreen.cs",
                "ScreenRoot > ViewportFrame > ScrollViewer > Stack: header + meta strip + scrollable lobby list + actions",
                CaptureLobbyBrowser,
                ["Game Lobbies", "Available Lobbies: 1", "Cadet Host", "Sirius Gate", "Create Lobby", "Join Lobby", "Refresh"],
                ["Panel", "Grid", "ScrollViewer", "Button", "Label"],
                MinimumScrollViewers: 2,
                MinimumButtons: 3,
                MinimumPanels: 5,
                MinimumGrids: 3,
                MinimumTreeDepth: 5),
            new(
                "create-lobby",
                "Create lobby",
                "RiskyStars.Client/UI/Screens/CreateLobbyScreen.cs",
                "ScreenRoot > ViewportFrame > ScrollViewer > Stack: header + setup cards + actions",
                () => CaptureScreen(new CreateLobbyScreen(null!, ScreenWidth, ScreenHeight)),
                ["Create Lobby", "Map Selection", "Maximum Commanders", "Cancel"],
                ["Panel", "Grid", "ScrollViewer", "Button", "TextBox"],
                MinimumScrollViewers: 1,
                MinimumButtons: 2,
                MinimumPanels: 4,
                MinimumGrids: 1,
                MinimumTreeDepth: 5),
            new(
                "multiplayer-lobby",
                "Multiplayer lobby",
                "RiskyStars.Client/UI/Screens/LobbyScreen.cs",
                "ScreenRoot > ViewportFrame > ScrollViewer > Stack: header + lobby metadata + scrollable slots + actions",
                CaptureMultiplayerLobby,
                ["Multiplayer Lobby", "Lobby Slots", "Host Commander", "Rigel March", "Ready", "Start Game", "Leave Lobby"],
                ["Panel", "Grid", "ScrollViewer", "Button", "Label"],
                MinimumScrollViewers: 2,
                MinimumButtons: 3,
                MinimumPanels: 6,
                MinimumGrids: 4,
                MinimumTreeDepth: 5),
            new(
                "single-player-lobby",
                "Single-player lobby",
                "RiskyStars.Client/UI/Screens/SinglePlayerLobbyScreen.cs",
                "ScreenRoot > ViewportFrame > ScrollViewer > Stack: map/name cards + server status + scrollable opponent lineup + actions",
                () => CaptureScreen(new SinglePlayerLobbyScreen(null!, ScreenWidth, ScreenHeight)),
                ["Single Player Game Setup", "Map Selection", "Commander Name", "Opponent Lineup", "Start Game", "Back"],
                ["Panel", "Grid", "ScrollViewer", "Button", "ComboBox"],
                MinimumScrollViewers: 2,
                MinimumButtons: 4,
                MinimumPanels: 8,
                MinimumGrids: 4,
                MinimumTreeDepth: 5),
            new(
                "gameplay-hud-top-bar",
                "Gameplay HUD top bar",
                "RiskyStars.Client/UI/Controls/GameplayHudOverlay.cs",
                "TopBar Panel > Grid: turn/status row + resource chips + shortcut hints",
                CaptureGameplayHudTopBar,
                ["POP", "MET", "FUEL"],
                ["Panel", "Grid", "Label"],
                MinimumScrollViewers: 0,
                MinimumButtons: 0,
                MinimumPanels: 4,
                MinimumGrids: 1,
                MinimumTreeDepth: 3),
            new(
                "gameplay-hud-legend",
                "Gameplay HUD legend",
                "RiskyStars.Client/UI/Controls/GameplayHudOverlay.cs",
                "Legend Panel > ScrollViewer > Stack: map key rows",
                CaptureGameplayHudLegend,
                ["Map Key", "System orbit", "Stellar body", "Region marker", "Lane mouth"],
                ["Panel", "ScrollViewer", "VerticalStackPanel", "Label"],
                MinimumScrollViewers: 1,
                MinimumButtons: 0,
                MinimumPanels: 2,
                MinimumGrids: 0,
                MinimumTreeDepth: 4),
            new(
                "side-panel-container",
                "Side panel container",
                "RiskyStars.Client/UI/Windows/SidePanelContainer.cs",
                "SidePanel Root > Header chrome + bounded ScrollViewer content + resize/collapse affordance",
                CaptureSidePanel,
                [],
                ["Panel", "ScrollViewer", "Button"],
                MinimumScrollViewers: 1,
                MinimumButtons: 1,
                MinimumPanels: 3,
                MinimumGrids: 1,
                MinimumTreeDepth: 4),
            new(
                "settings-window",
                "Settings window",
                "RiskyStars.Client/UI/Windows/SettingsWindow.cs",
                "Window > Tabs: graphics/audio/controls/server pages each with scrollable tab body + action footer",
                CaptureSettingsWindow,
                ["Settings", "Graphics", "Audio", "Controls", "Server", "Apply", "Cancel"],
                ["Window", "Panel", "Grid", "ScrollViewer", "Button"],
                MinimumScrollViewers: 4,
                MinimumButtons: 2,
                MinimumPanels: 6,
                MinimumGrids: 4,
                MinimumTreeDepth: 5),
            new(
                "debug-info-window",
                "Debug information window",
                "RiskyStars.Client/UI/Windows/DebugInfoWindow.cs",
                "Dockable Window > ScrollViewer > Stack: camera/performance/state/selection/UI audit panels",
                () => CaptureDockable(new DebugInfoWindow(new WindowPreferences(), ScreenWidth, ScreenHeight)),
                ["Debug Information", "Camera", "Performance", "Game State", "Selection", "UI Audit", "Visual Tree"],
                ["Window", "Panel", "ScrollViewer", "VerticalStackPanel", "Label"],
                MinimumScrollViewers: 2,
                MinimumButtons: 0,
                MinimumPanels: 6,
                MinimumGrids: 0,
                MinimumTreeDepth: 5),
            new(
                "player-dashboard-window",
                "Player dashboard window",
                "RiskyStars.Client/UI/Windows/PlayerDashboardWindow.cs",
                "Dockable Window > ScrollViewer > Stack: resources + army purchase + hero assignment controls",
                () => CaptureDockable(new PlayerDashboardWindow(null!, new WindowPreferences(), ScreenWidth, ScreenHeight)),
                ["Player Dashboard", "Resources", "Army Purchase", "Hero Assignment", "Buy 1", "Assign to Army"],
                ["Window", "Panel", "ScrollViewer", "Button", "Label"],
                MinimumScrollViewers: 1,
                MinimumButtons: 3,
                MinimumPanels: 4,
                MinimumGrids: 1,
                MinimumTreeDepth: 5),
            new(
                "ai-visualization-window",
                "AI visualization window",
                "RiskyStars.Client/UI/Windows/AIVisualizationWindow.cs",
                "Dockable Window > ScrollViewer > Stack: AI status + visualization toggles + activity log",
                () => CaptureDockable(new AIVisualizationWindow(new WindowPreferences(), ScreenWidth, ScreenHeight)),
                ["AI Visualization", "AI Status", "Visualization Options", "Activity Log", "Show Movement Animations"],
                ["Window", "Panel", "ScrollViewer", "CheckButton", "Label"],
                MinimumScrollViewers: 1,
                MinimumButtons: 1,
                MinimumPanels: 4,
                MinimumGrids: 0,
                MinimumTreeDepth: 5),
            new(
                "encyclopedia-window",
                "Encyclopedia window",
                "RiskyStars.Client/UI/Windows/EncyclopediaWindow.cs",
                "Dockable Window > Split layout: scrollable article navigation + scrollable article body",
                () => CaptureDockable(new EncyclopediaWindow(new WindowPreferences(), ScreenWidth, ScreenHeight)),
                ["Encyclopedia", "Articles", "Key Points", "Useful Commands"],
                ["Window", "Panel", "Grid", "ScrollViewer", "Button"],
                MinimumScrollViewers: 2,
                MinimumButtons: 4,
                MinimumPanels: 4,
                MinimumGrids: 1,
                MinimumTreeDepth: 5),
            new(
                "ui-scale-window",
                "UI scale window",
                "RiskyStars.Client/UI/Windows/UiScaleWindow.cs",
                "Dockable Window > ScrollViewer > Stack: scale slider + presets + apply/reset actions",
                () => CaptureDockable(new UiScaleWindow(new Settings(), new WindowPreferences(), ScreenWidth, ScreenHeight, _ => { })),
                ["UI Scale", "Command Deck Scale", "Quick presets", "Apply", "Reset"],
                ["Window", "Panel", "ScrollViewer", "Button", "HorizontalSlider"],
                MinimumScrollViewers: 1,
                MinimumButtons: 5,
                MinimumPanels: 3,
                MinimumGrids: 1,
                MinimumTreeDepth: 5),
            new(
                "tutorial-mode-window",
                "Tutorial mode window",
                "RiskyStars.Client/UI/Windows/TutorialModeWindow.cs",
                "Dockable Window > Grid: fixed title + bounded scroll body + fixed footer buttons",
                () => CaptureDockable(new TutorialModeWindow(new WindowPreferences(), ScreenWidth, ScreenHeight)),
                ["Tutorial Mode", "Boot the command deck", "Step actions", "Tutorial path", "Back", "Next", "End"],
                ["Window", "Panel", "Grid", "ScrollViewer", "Button"],
                MinimumScrollViewers: 1,
                MinimumButtons: 3,
                MinimumPanels: 4,
                MinimumGrids: 2,
                MinimumTreeDepth: 5),
            new(
                "continent-zoom-window",
                "Continent zoom window",
                "RiskyStars.Client/UI/Windows/ContinentZoomWindow.cs",
                "Window > Header labels + Myra-hosted XNA Image surface for planet continents",
                CaptureContinentZoom,
                ["Planet Zoom", "Sirius b", "Sirius", "Select a continent", "Surface: 4 continent layouts"],
                ["Window", "Panel", "Image", "Label"],
                MinimumScrollViewers: 0,
                MinimumButtons: 0,
                MinimumPanels: 3,
                MinimumGrids: 1,
                MinimumTreeDepth: 4),
            new(
                "combat-hud-overlay",
                "Combat HUD overlay",
                "RiskyStars.Client/UI/Controls/CombatHudOverlay.cs",
                "Backdrop + Window Panel > ScrollViewer: combat title, sides, rolls, pairings, casualties, survivors",
                CaptureCombatHudOverlay,
                ["Combat initiated", "Attackers", "Defenders", "Pairings", "Survivors"],
                ["Panel", "ScrollViewer", "Label"],
                MinimumScrollViewers: 1,
                MinimumButtons: 0,
                MinimumPanels: 4,
                MinimumGrids: 2,
                MinimumTreeDepth: 5),
            new(
                "server-status-indicator",
                "Server status indicator",
                "RiskyStars.Client/UI/Controls/ServerStatusIndicator.cs",
                "Panel > Grid: server state text + metrics lines",
                () => SnapshotFromRoot(new ServerStatusIndicator(320).Container),
                ["Server: Stopped"],
                ["Panel", "Grid", "Label"],
                MinimumScrollViewers: 0,
                MinimumButtons: 0,
                MinimumPanels: 1,
                MinimumGrids: 1,
                MinimumTreeDepth: 2),
            new(
                "dialog-manager",
                "Dialog manager",
                "RiskyStars.Client/UI/Dialogs/DialogManager.cs",
                "Dialog > title + message + action buttons",
                CaptureDialogManager,
                ["Confirm Title", "Confirm Message"],
                ["Dialog", "Panel", "Button", "Label"],
                MinimumScrollViewers: 0,
                MinimumButtons: 2,
                MinimumPanels: 2,
                MinimumGrids: 1,
                MinimumTreeDepth: 4),
            new(
                "combat-event-dialog",
                "Combat event dialog",
                "RiskyStars.Client/UI/Dialogs/CombatEventDialog.cs",
                "Dialog > combat summary panels + close action",
                CaptureCombatEventDialog,
                ["Combat Initiated", "alpha", "beta"],
                ["Dialog", "Panel", "Button", "Label"],
                MinimumScrollViewers: 0,
                MinimumButtons: 1,
                MinimumPanels: 1,
                MinimumGrids: 1,
                MinimumTreeDepth: 4),
            new(
                "context-menu-manager",
                "Context menu manager",
                "RiskyStars.Client/UI/Controls/ContextMenuManager.cs",
                "Desktop overlay > context menu rows for selected object actions",
                CaptureContextMenu,
                ["Terminus", "View Info"],
                ["Desktop", "VerticalStackPanel", "Label"],
                MinimumScrollViewers: 0,
                MinimumButtons: 0,
                MinimumPanels: 0,
                MinimumGrids: 0,
                MinimumTreeDepth: 2),
            new(
                "combat-screen",
                "Combat screen",
                "RiskyStars.Client/Gameplay/CombatScreen.cs",
                "XNA presentation model: combat title/status + attackers/defenders/rolls/pairings/casualties/survivors",
                CaptureCombatScreen,
                ["Combat initiated", "alpha", "beta", "Press ENTER or SPACE"],
                ["XnaPresentation"],
                MinimumScrollViewers: 0,
                MinimumButtons: 0,
                MinimumPanels: 0,
                MinimumGrids: 0,
                MinimumTreeDepth: 1),
            new(
                "legacy-player-dashboard",
                "Legacy player dashboard",
                "RiskyStars.Client/UI/PlayerDashboard.cs",
                "XNA panel model: visible dashboard region with player command controls",
                CaptureLegacyDashboard,
                ["Legacy XNA PlayerDashboard", "Visible: True"],
                ["XnaPanel"],
                MinimumScrollViewers: 0,
                MinimumButtons: 0,
                MinimumPanels: 0,
                MinimumGrids: 0,
                MinimumTreeDepth: 1),
            new(
                "ai-action-indicator",
                "AI action indicator",
                "RiskyStars.Client/UI/AIActionIndicator.cs",
                "XNA overlay model: active AI thinking state, movement animations, reinforcement events, activity log",
                CaptureAiActionIndicator,
                ["AIActionIndicator", "Bot Prime", "AI acted", "MovementAnimations: True"],
                ["XnaPanel"],
                MinimumScrollViewers: 0,
                MinimumButtons: 0,
                MinimumPanels: 0,
                MinimumGrids: 0,
                MinimumTreeDepth: 1)
        ];
    }

    private static UiActualSnapshot CaptureScreen(object screen)
    {
        InvokeLoadContent(screen);
        return SnapshotFromRoot(GetDesktopRoot(screen));
    }

    private static UiActualSnapshot CaptureMainMenu(MainMenuState state)
    {
        var screen = new MainMenu(null!, ScreenWidth, ScreenHeight, new Settings());
        screen.LoadContent(null!);
        screen.SetState(state);
        return SnapshotFromRoot(GetDesktopRoot(screen));
    }

    private static UiActualSnapshot CaptureLobbyBrowser()
    {
        var screen = new LobbyBrowserScreen(null!, ScreenWidth, ScreenHeight);
        screen.LoadContent(null!);
        screen.SetLobbies(
        [
            new LobbyInfo
            {
                LobbyId = "lobby-1",
                HostPlayerName = "Cadet Host",
                CurrentPlayers = 1,
                MaxPlayers = 4,
                GameMode = "Conquest",
                MapName = "Sirius Gate",
                PlayerNames = { "Cadet Host" }
            }
        ]);

        return SnapshotFromRoot(GetDesktopRoot(screen));
    }

    private static UiActualSnapshot CaptureMultiplayerLobby()
    {
        var screen = new LobbyScreen(null!, ScreenWidth, ScreenHeight);
        screen.LoadContent(null!);
        screen.SetLobbyInfo(
            new LobbyInfo
            {
                LobbyId = "lobby-2",
                HostPlayerName = "Host Commander",
                CurrentPlayers = 2,
                MaxPlayers = 4,
                GameMode = "Conquest",
                MapName = "Rigel March",
                PlayerNames = { "Host Commander", "Wing Two" }
            },
            "player-1");
        return SnapshotFromRoot(GetDesktopRoot(screen));
    }

    private static UiActualSnapshot CaptureGameplayHudTopBar()
    {
        var overlay = new GameplayHudOverlay(ScreenWidth, ScreenHeight);
        return SnapshotFromRoot(overlay.TopBar);
    }

    private static UiActualSnapshot CaptureGameplayHudLegend()
    {
        var overlay = new GameplayHudOverlay(ScreenWidth, ScreenHeight);
        return SnapshotFromRoot(overlay.BuildLegendContent());
    }

    private static UiActualSnapshot CaptureSidePanel()
    {
        var sidePanel = new SidePanelContainer("left", 260, ScreenWidth, ScreenHeight, topOffset: 90);
        sidePanel.AddWidget(ThemedUIFactory.CreateHeadingLabel("Injected content"));
        return SnapshotFromRoot(sidePanel.Container);
    }

    private static UiActualSnapshot CaptureSettingsWindow()
    {
        var window = new SettingsWindow(null!, new Settings());
        return SnapshotFromRoot(GetPrivateField<Window>(window, "_window"));
    }

    private static UiActualSnapshot CaptureDockable(DockableWindow window)
    {
        using var guard = FileRestoreGuard.CaptureAndDelete("window_preferences.json");
        return SnapshotFromRoot(window.Window);
    }

    private static UiActualSnapshot CaptureContinentZoom()
    {
        var window = new ContinentZoomWindow(ScreenWidth, ScreenHeight);
        var body = CreateBody("Sirius b", 4);
        window.Show(body, CreateStarSystem(body));
        return SnapshotFromRoot(window.Window, $"Surface: {window.CurrentLayouts.Count} continent layouts");
    }

    private static UiActualSnapshot CaptureCombatHudOverlay()
    {
        var overlay = new CombatHudOverlay(ScreenWidth, ScreenHeight);
        overlay.Update(CreateCombatPresentation());
        return SnapshotFromRoot(overlay.Window);
    }

    private static UiActualSnapshot CaptureDialogManager()
    {
        var desktop = new Desktop();
        var manager = new DialogManager(desktop);
        manager.ShowConfirmation("Confirm Title", "Confirm Message");
        return SnapshotFromRoot(GetPrivateField<Dialog>(manager, "_currentDialog"));
    }

    private static UiActualSnapshot CaptureCombatEventDialog()
    {
        var dialog = new CombatEventDialog(new Desktop());
        dialog.ShowCombatInitiated(CreateCombatEvent(CombatEvent.Types.CombatEventType.CombatInitiated));
        return SnapshotFromRoot(GetPrivateField<Dialog>(dialog, "_currentDialog"));
    }

    private static UiActualSnapshot CaptureContextMenu()
    {
        var desktop = new Desktop();
        var manager = new ContextMenuManager(
            null!,
            new GameStateCache(),
            new MapData(),
            new Camera2D(ScreenWidth, ScreenHeight),
            desktop);
        var selection = new SelectionState();
        selection.SelectRegion(new RegionData { Id = "region-1", Name = "Terminus", StellarBodyId = "body-1" });
        manager.OpenContextMenu(new Vector2(200, 180), Vector2.Zero, selection);
        return SnapshotFromRoot(desktop);
    }

    private static UiActualSnapshot CaptureCombatScreen()
    {
        var screen = new CombatScreen(null, ScreenWidth, ScreenHeight);
        screen.StartCombat(CreateCombatEvent(CombatEvent.Types.CombatEventType.CombatInitiated));
        CombatPresentation presentation = Assert.IsType<CombatPresentation>(screen.GetPresentation());
        var texts = new List<string>
        {
            presentation.Title,
            presentation.Status,
            presentation.Instructions
        };
        texts.AddRange(presentation.Attackers);
        texts.AddRange(presentation.Defenders);
        texts.AddRange(presentation.Pairings);
        texts.AddRange(presentation.Casualties);
        texts.AddRange(presentation.Survivors);
        return UiActualSnapshot.FromPresentation("CombatScreen", ["XnaPresentation"], texts);
    }

    private static UiActualSnapshot CaptureLegacyDashboard()
    {
        var dashboard = new PlayerDashboard(null!, null!, ScreenWidth, ScreenHeight);
        return UiActualSnapshot.FromPresentation(
            "LegacyPlayerDashboard",
            ["XnaPanel"],
            ["Legacy XNA PlayerDashboard", $"Visible: {dashboard.IsVisible}"]);
    }

    private static UiActualSnapshot CaptureAiActionIndicator()
    {
        var indicator = new AIActionIndicator(null, ScreenWidth, ScreenHeight);
        indicator.StartAIThinking("Bot Prime");
        indicator.ShowArmyMovement(Vector2.Zero, Vector2.One, 2, Color.Blue, "army-1");
        indicator.AddLogEntry("AI acted", Color.White);
        return UiActualSnapshot.FromPresentation(
            "AIActionIndicator",
            ["XnaPanel"],
            [
                "AIActionIndicator",
                indicator.ActiveAIPlayerName ?? string.Empty,
                "AI acted",
                $"MovementAnimations: {indicator.HasActiveMovementAnimations()}"
            ]);
    }

    private static void AssertWireframeMatchesActual(UiWireframeSpec spec, UiActualSnapshot actual)
    {
        foreach (string expectedText in spec.RequiredText)
        {
            if (!actual.Texts.Any(text => text.Contains(expectedText, StringComparison.OrdinalIgnoreCase)))
            {
                throw new WireframeAuditException(
                    $"{spec.DisplayName} is missing expected text '{expectedText}'. Actual text: {string.Join(" | ", actual.Texts)}");
            }
        }

        foreach (string expectedType in spec.RequiredWidgetTypes)
        {
            if (!actual.WidgetTypes.Any(type => string.Equals(type, expectedType, StringComparison.Ordinal) || type.Contains(expectedType, StringComparison.Ordinal)))
            {
                throw new WireframeAuditException(
                    $"{spec.DisplayName} is missing expected widget type '{expectedType}'. Actual types: {string.Join(", ", actual.WidgetTypes.Distinct().Order(StringComparer.Ordinal))}");
            }
        }

        if (actual.ScrollViewerCount < spec.MinimumScrollViewers)
        {
            throw new WireframeAuditException(
                $"{spec.DisplayName} expected at least {spec.MinimumScrollViewers} ScrollViewer widgets but found {actual.ScrollViewerCount}.");
        }

        if (actual.ButtonCount < spec.MinimumButtons)
        {
            throw new WireframeAuditException(
                $"{spec.DisplayName} expected at least {spec.MinimumButtons} buttons but found {actual.ButtonCount}.");
        }

        if (actual.PanelCount < spec.MinimumPanels)
        {
            throw new WireframeAuditException(
                $"{spec.DisplayName} expected at least {spec.MinimumPanels} panels but found {actual.PanelCount}.");
        }

        if (actual.GridCount < spec.MinimumGrids)
        {
            throw new WireframeAuditException(
                $"{spec.DisplayName} expected at least {spec.MinimumGrids} grids but found {actual.GridCount}.");
        }

        if (actual.MaxDepth < spec.MinimumTreeDepth)
        {
            throw new WireframeAuditException(
                $"{spec.DisplayName} expected a nested wireframe depth of at least {spec.MinimumTreeDepth} but actual depth was {actual.MaxDepth}.");
        }
    }

    private static void AssertDivergenceReportCoversScreens(string report, IEnumerable<string> screenIds)
    {
        var missing = screenIds
            .Where(screenId => !report.Contains($"### {screenId}", StringComparison.Ordinal))
            .ToArray();

        if (missing.Length > 0)
        {
            throw new WireframeAuditException(
                $"The screenshot divergence report is missing screen sections: {string.Join(", ", missing)}.");
        }
    }

    private static void AssertPromptBaselinePathIsCanonical(string relativePath)
    {
        if (!relativePath.Contains("RiskyStars.Client/Wireframes/PromptWireframes/", StringComparison.Ordinal) ||
            !relativePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
        {
            throw new WireframeAuditException(
                $"Canonical wireframe screenshot baselines must be direct prompt-rendered PNGs under RiskyStars.Client/Wireframes/PromptWireframes. Mermaid artifacts are not valid baselines: {relativePath}");
        }
    }

    private static UiActualSnapshot SnapshotFromRoot(object root, params string[] extraTexts)
    {
        var nodes = new List<UiNodeSnapshot>();
        var texts = new List<string>(extraTexts);
        var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
        CollectActual(root, depth: 0, nodes, texts, seen);
        return new UiActualSnapshot(root.GetType().Name, nodes, texts);
    }

    private static void CollectActual(
        object? current,
        int depth,
        List<UiNodeSnapshot> nodes,
        List<string> texts,
        HashSet<object> seen)
    {
        if (current == null || !seen.Add(current))
        {
            return;
        }

        if (current is Widget widget)
        {
            nodes.Add(new UiNodeSnapshot(widget.GetType().Name, depth));
        }
        else if (current is Desktop)
        {
            nodes.Add(new UiNodeSnapshot("Desktop", depth));
        }

        var textProperty = current.GetType().GetProperty("Text", BindingFlags.Instance | BindingFlags.Public);
        if (textProperty?.GetValue(current) is string text && !string.IsNullOrWhiteSpace(text))
        {
            texts.Add(text);
        }

        var titleProperty = current.GetType().GetProperty("Title", BindingFlags.Instance | BindingFlags.Public);
        if (titleProperty?.GetValue(current) is string title && !string.IsNullOrWhiteSpace(title))
        {
            texts.Add(title);
        }

        foreach (object child in GetChildObjects(current))
        {
            CollectActual(child, depth + 1, nodes, texts, seen);
        }
    }

    private static IEnumerable<object> GetChildObjects(object current)
    {
        foreach (string propertyName in new[] { "Widgets", "Content", "Root", "Items" })
        {
            var property = current.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (property == null || property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            object? value = property.GetValue(current);
            if (value == null || value is string)
            {
                continue;
            }

            if (value is IEnumerable enumerable)
            {
                foreach (object? item in enumerable)
                {
                    if (item != null && item is not string)
                    {
                        yield return item;
                    }
                }
            }
            else
            {
                yield return value;
            }
        }
    }

    private static Widget GetDesktopRoot(object screen)
    {
        var desktop = GetPrivateField<Desktop>(screen, "_desktop");
        if (desktop.Root != null)
        {
            return desktop.Root;
        }

        return Assert.Single(desktop.Widgets);
    }

    private static T GetPrivateField<T>(object instance, string fieldName)
        where T : class
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return Assert.IsType<T>(field.GetValue(instance));
    }

    private static void InvokeLoadContent(object screen)
    {
        var loadContent = screen.GetType().GetMethod("LoadContent", BindingFlags.Instance | BindingFlags.Public, [typeof(Microsoft.Xna.Framework.Graphics.SpriteFont)]);
        Assert.NotNull(loadContent);
        loadContent.Invoke(screen, [null]);
    }

    private static void SetupMyra()
    {
        MyraEnvironment.Game = HeadlessMyraGame;
        Stylesheet.Current = DefaultAssets.DefaultStylesheet;
        ThemeManager.Initialize();
        ThemeManager.ApplyThemeSettings(new Settings());
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

    private static CombatPresentation CreateCombatPresentation()
    {
        return new CombatPresentation
        {
            Title = "Combat initiated",
            Location = "region-1",
            Round = "Round 1 of 1",
            Status = "Resolving dice",
            Instructions = "Press ENTER or SPACE to advance.",
            Attackers = ["p1: 3 units"],
            Defenders = ["p2: 2 units"],
            AttackerRolls = ["A1: 6"],
            DefenderRolls = ["D1: 4"],
            Pairings = ["1. Attacker 6 vs Defender 4"],
            Casualties = ["p2: -1 units"],
            Survivors = ["p1: 3 units"]
        };
    }

    private static CombatEvent CreateCombatEvent(CombatEvent.Types.CombatEventType eventType)
    {
        return new CombatEvent
        {
            EventId = "combat-1",
            EventType = eventType,
            LocationId = "region-1",
            ArmyStates =
            {
                new CombatArmyState
                {
                    ArmyId = "army-alpha",
                    PlayerId = "alpha",
                    CombatRole = "Attacker",
                    UnitCount = 3
                },
                new CombatArmyState
                {
                    ArmyId = "army-beta",
                    PlayerId = "beta",
                    CombatRole = "Defender",
                    UnitCount = 2
                }
            },
            RoundResults =
            {
                new CombatRoundResult
                {
                    AttackerRolls =
                    {
                        new DiceRoll { ArmyId = "army-alpha", Roll = 6, UnitIndex = 0 }
                    },
                    DefenderRolls =
                    {
                        new DiceRoll { ArmyId = "army-beta", Roll = 4, UnitIndex = 0 }
                    },
                    Pairings =
                    {
                        new RollPairing
                        {
                            AttackerRoll = new DiceRoll { ArmyId = "army-alpha", Roll = 6, UnitIndex = 0 },
                            DefenderRoll = new DiceRoll { ArmyId = "army-beta", Roll = 4, UnitIndex = 0 },
                            WinnerArmyId = "army-alpha"
                        }
                    },
                    Casualties =
                    {
                        new ArmyCasualty
                        {
                            ArmyId = "army-beta",
                            PlayerId = "beta",
                            CombatRole = "Defender",
                            Casualties = 1,
                            RemainingUnits = 1
                        }
                    }
                }
            }
        };
    }

    private static StarSystemData CreateStarSystem(StellarBodyData body)
    {
        return new StarSystemData
        {
            Id = "star-1",
            Name = "Sirius",
            Position = Vector2.Zero,
            Type = StarSystemType.Home,
            StellarBodies = { body }
        };
    }

    private static StellarBodyData CreateBody(string name, int regionCount)
    {
        var body = new StellarBodyData
        {
            Id = "body-1",
            Name = name,
            StarSystemId = "star-1",
            Type = StellarBodyType.RockyPlanet,
            Position = new Vector2(100, 100)
        };

        for (int i = 0; i < regionCount; i++)
        {
            body.Regions.Add(new RegionData
            {
                Id = $"region-{i:D2}",
                Name = $"Continent {i:D2}",
                StellarBodyId = body.Id,
                Position = body.Position
            });
        }

        return body;
    }

    private static string FindRepositoryFile(string relativePath)
    {
        string normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            string candidate = Path.Combine(directory.FullName, normalized);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not find repository file {relativePath}");
    }

    private static (int Width, int Height) ReadPngDimensions(string path)
    {
        using var stream = File.OpenRead(path);
        Span<byte> header = stackalloc byte[24];
        int read = stream.Read(header);
        Assert.True(read >= 24, $"{path} is not a valid PNG.");
        int width = ReadBigEndianInt32(header[16..20]);
        int height = ReadBigEndianInt32(header[20..24]);
        return (width, height);
    }

    private static int ReadBigEndianInt32(ReadOnlySpan<byte> bytes)
    {
        return (bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3];
    }

    public sealed record UiWireframeSpec(
        string Id,
        string DisplayName,
        string SourcePath,
        string ExpectedLayout,
        Func<UiActualSnapshot> CaptureActual,
        IReadOnlyList<string> RequiredText,
        IReadOnlyList<string> RequiredWidgetTypes,
        int MinimumScrollViewers,
        int MinimumButtons,
        int MinimumPanels,
        int MinimumGrids,
        int MinimumTreeDepth)
    {
        public string PromptPngPath => $"RiskyStars.Client/Wireframes/PromptWireframes/{Id}.png";

        public override string ToString() => DisplayName;
    }

    public sealed record UiActualSnapshot(
        string RootType,
        IReadOnlyList<UiNodeSnapshot> Nodes,
        IReadOnlyList<string> Texts)
    {
        public IReadOnlyList<string> WidgetTypes => Nodes.Select(node => node.Type).ToArray();

        public int ScrollViewerCount => Nodes.Count(node => node.Type.Contains("ScrollViewer", StringComparison.Ordinal));

        public int ButtonCount => Nodes.Count(node => node.Type.Contains("Button", StringComparison.Ordinal) || node.Type.Contains("MenuItem", StringComparison.Ordinal));

        public int PanelCount => Nodes.Count(node => node.Type.Contains("Panel", StringComparison.Ordinal));

        public int GridCount => Nodes.Count(node => node.Type.Contains("Grid", StringComparison.Ordinal));

        public int MaxDepth => Nodes.Count == 0 ? 0 : Nodes.Max(node => node.Depth);

        public static UiActualSnapshot FromPresentation(string rootType, IReadOnlyList<string> types, IReadOnlyList<string> texts)
        {
            return new UiActualSnapshot(
                rootType,
                types.Select(type => new UiNodeSnapshot(type, 1)).ToArray(),
                texts);
        }
    }

    public sealed record UiNodeSnapshot(string Type, int Depth);

    private sealed class WireframeAuditException(string message) : Exception(message);

    private sealed class FileRestoreGuard : IDisposable
    {
        private readonly string _path;
        private readonly string? _originalContents;
        private readonly bool _existed;

        private FileRestoreGuard(string path)
        {
            _path = path;
            _existed = File.Exists(path);
            _originalContents = _existed ? File.ReadAllText(path) : null;
        }

        public static FileRestoreGuard CaptureAndDelete(string path)
        {
            var guard = new FileRestoreGuard(Path.GetFullPath(path));
            if (File.Exists(guard._path))
            {
                File.Delete(guard._path);
            }

            return guard;
        }

        public void Dispose()
        {
            if (_existed)
            {
                File.WriteAllText(_path, _originalContents);
            }
            else if (File.Exists(_path))
            {
                File.Delete(_path);
            }
        }
    }
}
