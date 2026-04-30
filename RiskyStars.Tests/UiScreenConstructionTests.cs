using System.Collections;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;
using RiskyStars.Client;
using RiskyStars.Shared;

namespace RiskyStars.Tests;

[Collection("Myra UI tests")]
public sealed class UiScreenConstructionTests
{
    private const int ScreenWidth = 1280;
    private const int ScreenHeight = 720;
    private static readonly Game HeadlessMyraGame = CreateHeadlessMyraGame();

    public static IEnumerable<object[]> MenuAndLobbyScreenCases()
    {
        yield return new object[]
        {
            "GameModeSelector",
            new Func<object>(() =>
            {
                var screen = new GameModeSelector(null!, ScreenWidth, ScreenHeight);
                screen.LoadContent(null!);
                return screen;
            }),
            new[] { "Select Game Mode", "Multiplayer", "Single Player", "Continue", "Back" }
        };

        yield return new object[]
        {
            "ConnectionScreen",
            new Func<object>(() =>
            {
                var screen = new ConnectionScreen(null!, ScreenWidth, ScreenHeight);
                screen.LoadContent(null!);
                return screen;
            }),
            new[] { "Multiplayer Uplink", "Commander Identity", "Server Endpoint", "Connect", "Back" }
        };

        yield return new object[]
        {
            "CreateLobbyScreen",
            new Func<object>(() =>
            {
                var screen = new CreateLobbyScreen(null!, ScreenWidth, ScreenHeight);
                screen.LoadContent(null!);
                return screen;
            }),
            new[] { "Create Lobby", "Map Selection", "Maximum Commanders", "Create Lobby", "Cancel" }
        };
    }

    public static IEnumerable<object[]> DockableWindowCases()
    {
        yield return new object[]
        {
            "DebugInfoWindow",
            new Func<DockableWindow>(() => new DebugInfoWindow(new WindowPreferences(), ScreenWidth, ScreenHeight)),
            new[] { "Camera", "Performance", "Game State", "Selection", "UI Audit" }
        };

        yield return new object[]
        {
            "PlayerDashboardWindow",
            new Func<DockableWindow>(() => new PlayerDashboardWindow(null!, new WindowPreferences(), ScreenWidth, ScreenHeight)),
            new[] { "Resources", "Army Purchase", "Hero Assignment", "Buy 1", "Assign to Army" }
        };

        yield return new object[]
        {
            "AIVisualizationWindow",
            new Func<DockableWindow>(() => new AIVisualizationWindow(new WindowPreferences(), ScreenWidth, ScreenHeight)),
            new[] { "AI Status", "Visualization Options", "Activity Log", "Show Movement Animations" }
        };

        yield return new object[]
        {
            "UiScaleWindow",
            new Func<DockableWindow>(() => new UiScaleWindow(new Settings(), new WindowPreferences(), ScreenWidth, ScreenHeight, _ => { })),
            new[] { "Command Deck Scale", "Quick presets", "Apply", "Reset" }
        };
    }

    [Fact]
    public void DocumentationMatrix_ListsEveryScreenSurfaceUnderTest()
    {
        var doc = File.ReadAllText(FindRepositoryFile("RiskyStars.Client/UI_SCREEN_TEST_MATRIX.md"));
        var requiredNames = new[]
        {
            "Main menu",
            "Game mode selector",
            "Connection screen",
            "Lobby browser",
            "Create lobby",
            "Multiplayer lobby",
            "Single-player lobby",
            "Gameplay HUD",
            "Side panel container",
            "Settings window",
            "Debug information window",
            "Player dashboard window",
            "AI visualization window",
            "Encyclopedia window",
            "UI scale window",
            "Tutorial mode window",
            "Continent zoom window",
            "Combat HUD overlay",
            "Server status indicator",
            "Dialog manager",
            "Combat event dialog",
            "Context menu manager",
            "Combat screen",
            "Legacy player dashboard",
            "AI action indicator"
        };

        foreach (var requiredName in requiredNames)
        {
            Assert.Contains(requiredName, doc, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void MainMenu_BuildsMainSettingsAndConnectingStates()
    {
        SetupMyra();
        var screen = new MainMenu(null!, ScreenWidth, ScreenHeight, new Settings());
        screen.LoadContent(null!);

        var mainRoot = GetDesktopRoot(screen);
        AssertText(mainRoot, "RiskyStars", "Multiplayer", "Single Player", "Tutorial Mode", "Settings", "Exit");
        AssertHasScrollViewer(mainRoot);

        screen.SetState(MainMenuState.Settings);
        var settingsRoot = GetDesktopRoot(screen);
        AssertText(settingsRoot, "Command Settings", "Server Endpoint", "UI Scale", "Save Settings", "Back");
        AssertHasScrollViewer(settingsRoot);

        screen.SetState(MainMenuState.Connecting);
        var connectingRoot = GetDesktopRoot(screen);
        AssertText(connectingRoot, "Establishing Uplink", "Connecting to the sector network");
        AssertHasScrollViewer(connectingRoot);

        screen.ResizeViewport(0, ScreenHeight);

        Assert.Same(connectingRoot, GetDesktopRoot(screen));
    }

    [Theory]
    [MemberData(nameof(MenuAndLobbyScreenCases))]
    public void MenuAndLobbyScreens_BuildExpectedContent(string screenName, Func<object> createScreen, string[] expectedText)
    {
        SetupMyra();
        var screen = createScreen();

        var root = GetDesktopRoot(screen);

        AssertText(root, expectedText);
        AssertHasScrollViewer(root);
        InvokeResizeViewport(screen, 0, ScreenHeight);
        Assert.Same(root, GetDesktopRoot(screen));
        Assert.NotNull(screenName);
    }

    [Fact]
    public void LobbyBrowser_PopulatesLobbyListAndRejectsEmptySelection()
    {
        SetupMyra();
        var screen = new LobbyBrowserScreen(null!, ScreenWidth, ScreenHeight);
        screen.LoadContent(null!);

        screen.SetLobbies(new List<LobbyInfo>
        {
            new()
            {
                LobbyId = "lobby-1",
                HostPlayerName = "Cadet Host",
                CurrentPlayers = 1,
                MaxPlayers = 4,
                GameMode = "Conquest",
                MapName = "Sirius Gate",
                PlayerNames = { "Cadet Host" }
            }
        });

        var root = GetDesktopRoot(screen);
        AssertText(root, "Game Lobbies", "Available Lobbies: 1", "Cadet Host", "Sirius Gate", "Join Lobby");
        AssertHasScrollViewer(root);

        screen.Reset();

        Assert.Null(screen.SelectedLobbyId);
        Assert.False(screen.ShouldJoinLobby);
    }

    [Fact]
    public void MultiplayerLobby_BuildsSlotsAndStateControls()
    {
        SetupMyra();
        var screen = new LobbyScreen(null!, ScreenWidth, ScreenHeight);
        screen.LoadContent(null!);

        screen.SetLobbyInfo(new LobbyInfo
        {
            LobbyId = "lobby-2",
            HostPlayerName = "Host Commander",
            CurrentPlayers = 2,
            MaxPlayers = 4,
            GameMode = "Conquest",
            MapName = "Rigel March",
            PlayerNames = { "Host Commander", "Wing Two" }
        }, "player-1");

        var root = GetDesktopRoot(screen);
        AssertText(root, "Multiplayer Lobby", "Lobby Slots", "Host Commander", "Rigel March", "Ready", "Start Game", "Leave Lobby");
        AssertHasScrollViewer(root);

        screen.ResizeViewport(0, ScreenHeight);

        Assert.Same(root, GetDesktopRoot(screen));
    }

    [Fact]
    public void LobbyManager_DebugShowState_SeedsScreensForDebugProtocolNavigation()
    {
        SetupMyra();
        var manager = LobbyManager.CreateHeadlessForTests(ScreenWidth, ScreenHeight);
        var browserScreen = new LobbyBrowserScreen(null!, ScreenWidth, ScreenHeight);
        var lobbyScreen = new LobbyScreen(null!, ScreenWidth, ScreenHeight);
        browserScreen.LoadContent(null!);
        lobbyScreen.LoadContent(null!);
        SetPrivateField(manager, "_browserScreen", browserScreen);
        SetPrivateField(manager, "_lobbyScreen", lobbyScreen);

        manager.DebugShowState(LobbyState.Browser);

        Assert.Equal(LobbyState.Browser, manager.State);
        Assert.Equal(GameMode.Multiplayer, manager.SelectedGameMode);
        Assert.Equal("debug-player", manager.PlayerId);
        Assert.Equal("Cadet", manager.PlayerName);
        Assert.Equal("debug-session", manager.SessionId);
        AssertText(GetDesktopRoot(browserScreen), "Game Lobbies", "Available Lobbies: 1", "Host Commander", "Rigel March");

        manager.DebugShowState(LobbyState.InLobby);

        Assert.Equal(LobbyState.InLobby, manager.State);
        AssertText(GetDesktopRoot(lobbyScreen), "Multiplayer Lobby", "Host Commander", "Rigel March", "Ready", "Start Game");

        manager.DebugShowState(LobbyState.SinglePlayerLobby);

        Assert.Equal(LobbyState.SinglePlayerLobby, manager.State);
        Assert.Equal(GameMode.SinglePlayer, manager.SelectedGameMode);
    }

    [Fact]
    public void SinglePlayerLobby_BuildsSetupAndPlayerSlots()
    {
        SetupMyra();
        var screen = new SinglePlayerLobbyScreen(null!, ScreenWidth, ScreenHeight);
        screen.LoadContent(null!);

        var root = GetDesktopRoot(screen);

        AssertText(root, "Single Player Game Setup", "Opponent Lineup", "Start Game", "Back");
        AssertHasScrollViewer(root);
        Assert.True(screen.PlayerSlots.Count >= 2);

        screen.Reset();

        Assert.False(screen.ShouldStartGame);
        Assert.False(screen.ShouldGoBack);
    }

    [Theory]
    [MemberData(nameof(DockableWindowCases))]
    public void DockableWindows_BuildExpectedScrollableContent(string windowName, Func<DockableWindow> createWindow, string[] expectedText)
    {
        SetupMyra();
        using var guard = FileRestoreGuard.CaptureAndDelete("window_preferences.json");
        var dockableWindow = createWindow();

        Assert.NotNull(dockableWindow.Window.Content);
        AssertText(dockableWindow.Window, expectedText);
        AssertHasScrollViewer(dockableWindow.Window);
        dockableWindow.ResizeViewport(0, ScreenHeight);
        Assert.NotNull(dockableWindow.Window.Content);
        Assert.NotNull(windowName);
    }

    [Fact]
    public void SettingsWindow_BuildsAllTabsWithScrollableContent()
    {
        SetupMyra();
        var settingsWindow = new SettingsWindow(null!, new Settings());

        var window = GetPrivateField<Window>(settingsWindow, "_window");
        Assert.NotNull(window.Content);
        AssertText(window, "Settings", "Graphics", "Audio", "Controls", "Server", "Apply", "Cancel");
        Assert.True(CollectWidgets(window).OfType<ScrollViewer>().Count() >= 4);

        settingsWindow.Open();
        Assert.True(settingsWindow.IsOpen);

        settingsWindow.Close();
        settingsWindow.Close();

        Assert.False(settingsWindow.IsOpen);
    }

    [Fact]
    public void EncyclopediaWindow_BuildsNavigationAndArticleContent()
    {
        SetupMyra();
        using var guard = FileRestoreGuard.Capture("window_preferences.json");
        var window = new EncyclopediaWindow(new WindowPreferences(), ScreenWidth, ScreenHeight);

        AssertText(window.Window, "Articles", "Key Points", "Useful Commands");
        Assert.True(CollectWidgets(window.Window).OfType<ScrollViewer>().Count() >= 2);
        Assert.False(window.IsVisible);
    }

    [Fact]
    public void TutorialModeWindow_BuildsScrollableBodyAndStableFooter()
    {
        SetupMyra();
        using var guard = FileRestoreGuard.Capture("window_preferences.json");
        var window = new TutorialModeWindow(new WindowPreferences(), ScreenWidth, ScreenHeight);

        AssertText(window.Window, "Boot the command deck", "Step actions", "Tutorial path", "Back", "Next", "End");
        AssertHasScrollViewer(window.Window);
        Assert.NotEmpty(window.CurrentHighlightTargets);
    }

    [Fact]
    public void TutorialModeWindow_DebugActionGateKeepsBeforeStatePendingUntilProtocolCompletesStep()
    {
        SetupMyra();
        using var guard = FileRestoreGuard.Capture("window_preferences.json");
        var window = new TutorialModeWindow(new WindowPreferences(), ScreenWidth, ScreenHeight);
        var cache = new GameStateCache();
        cache.ApplyUpdate(new GameUpdate
        {
            Timestamp = 123,
            GameState = new TurnBasedGameStateUpdate
            {
                GameId = "debug-game",
                TurnNumber = 1,
                CurrentPhase = TurnPhase.Production,
                CurrentPlayerId = "debug-player"
            }
        });
        var snapshot = new TutorialModeSnapshot(
            cache,
            "debug-player",
            new SelectionState(),
            HelpVisible: false,
            DashboardVisible: false,
            EncyclopediaVisible: false,
            ContextMenuOpen: false,
            CombatActive: false);

        window.DebugReset(requireExplicitActions: true);
        window.UpdateContent(snapshot);

        Assert.True(window.IsCurrentStepObjectiveSatisfied);
        Assert.False(window.IsCurrentStepComplete);
        Assert.Contains("Waiting", window.CurrentStatusText, StringComparison.OrdinalIgnoreCase);

        window.DebugCompleteCurrentStep();

        Assert.True(window.IsCurrentStepComplete);
        Assert.Contains("complete", window.CurrentStatusText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SidePanelContainer_BuildsScrollableContentAndKeepsHeaderOnCollapse()
    {
        SetupMyra();
        var sidePanel = new SidePanelContainer("left", 260, ScreenWidth, ScreenHeight, topOffset: 90);

        AssertHasScrollViewer(sidePanel.Container);
        Assert.True(sidePanel.Container.Visible);
        Assert.False(sidePanel.IsCollapsed);

        sidePanel.ToggleCollapse();

        Assert.True(sidePanel.IsCollapsed);
        Assert.True(sidePanel.Container.Visible);
        Assert.Contains(CollectWidgets(sidePanel.Container), widget => widget.Visible);
    }

    [Fact]
    public void CombatHudOverlay_BuildsScrollableCombatPresentation()
    {
        SetupMyra();
        var overlay = new CombatHudOverlay(ScreenWidth, ScreenHeight);
        var presentation = new CombatPresentation
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

        overlay.Update(presentation);

        Assert.True(overlay.Backdrop.Visible);
        Assert.True(overlay.Window.Visible);
        AssertText(overlay.Window, "Combat initiated", "Attackers", "Defenders", "Pairings", "Survivors");
        AssertHasScrollViewer(overlay.Window);

        overlay.Update(null);

        Assert.False(overlay.Backdrop.Visible);
        Assert.False(overlay.Window.Visible);
    }

    [Fact]
    public void ServerStatusIndicator_BuildsStoppedStateAndRejectsInvalidResize()
    {
        SetupMyra();
        var indicator = new ServerStatusIndicator(320);
        var originalWidth = CollectWidgets(indicator.Container).OfType<Grid>().Single().Width;

        AssertText(indicator.Container, "Server: Stopped");

        indicator.Resize(0);

        Assert.Equal(originalWidth, CollectWidgets(indicator.Container).OfType<Grid>().Single().Width);
    }

    [Fact]
    public void DialogManager_BuildsAndClosesEveryDialogType()
    {
        SetupMyra();
        var desktop = new Desktop();
        var manager = new DialogManager(desktop);

        var cases = new Action[]
        {
            () => manager.ShowInfo("Info Title", "Info Message"),
            () => manager.ShowWarning("Warning Title", "Warning Message"),
            () => manager.ShowError("Error Title", "Error Message"),
            () => manager.ShowSuccess("Success Title", "Success Message"),
            () => manager.ShowQuestion("Question Title", "Question Message"),
            () => manager.ShowConfirmation("Confirm Title", "Confirm Message"),
            () => manager.ShowRetryDialog("Retry Title", "Retry Message"),
            () => manager.ShowCombatEvent("Combat Title", "Combat Message")
        };

        foreach (var showDialog in cases)
        {
            showDialog();
            Assert.True(manager.IsDialogOpen);
            Assert.NotNull(GetPrivateField<Dialog>(manager, "_currentDialog").Content);
            manager.CloseDialog();
            Assert.False(manager.IsDialogOpen);
        }

        manager.CloseDialog();

        Assert.False(manager.IsDialogOpen);
    }

    [Fact]
    public void CombatEventDialog_BuildsAllCombatDialogVariants()
    {
        SetupMyra();
        var dialog = new CombatEventDialog(new Desktop());
        var combatEvent = CreateCombatEvent(CombatEvent.Types.CombatEventType.CombatInitiated);

        dialog.ShowCombatInitiated(combatEvent);
        AssertText(GetPrivateField<Dialog>(dialog, "_currentDialog"), "Combat Initiated", "alpha", "beta");
        dialog.CloseDialog();

        combatEvent.EventType = CombatEvent.Types.CombatEventType.ReinforcementsArrived;
        dialog.ShowReinforcementsArrived(combatEvent);
        AssertText(GetPrivateField<Dialog>(dialog, "_currentDialog"), "Reinforcements Arrived", "Current Forces");
        dialog.CloseDialog();

        combatEvent.EventType = CombatEvent.Types.CombatEventType.CombatEnded;
        combatEvent.ArmyStates[1].UnitCount = 0;
        dialog.ShowCombatEnded(combatEvent);
        AssertText(GetPrivateField<Dialog>(dialog, "_currentDialog"), "Combat Complete", "Victors", "Eliminated");
        dialog.CloseDialog();

        Assert.False(dialog.IsOpen);
    }

    [Fact]
    public void ContextMenuManager_OpensAndClosesRegionMenu()
    {
        SetupMyra();
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

        Assert.True(manager.IsMenuOpen);
        AssertText(desktop, "Terminus", "View Info");

        manager.CloseContextMenu();
        manager.CloseContextMenu();

        Assert.False(manager.IsMenuOpen);
    }

    [Fact]
    public void CombatScreen_CreatesPresentationAndClosesCleanly()
    {
        var combatScreen = new CombatScreen(null, ScreenWidth, ScreenHeight);
        var combatEvent = CreateCombatEvent(CombatEvent.Types.CombatEventType.CombatInitiated);

        combatScreen.StartCombat(combatEvent);

        var presentation = combatScreen.GetPresentation();
        Assert.NotNull(presentation);
        Assert.Equal("Combat initiated", presentation.Title);
        Assert.Contains("alpha", presentation.Attackers[0], StringComparison.Ordinal);
        Assert.Contains("beta", presentation.Defenders[0], StringComparison.Ordinal);

        combatScreen.ResizeViewport(0, ScreenHeight);
        combatScreen.Close();

        Assert.False(combatScreen.IsActive);
        Assert.Null(combatScreen.GetPresentation());
    }

    [Fact]
    public void LegacyXnaScreens_ConstructAndResizeWithoutGraphicsSideEffects()
    {
        SetupMyra();
        var dashboard = new PlayerDashboard(null!, null!, ScreenWidth, ScreenHeight);
        var indicator = new AIActionIndicator(null, ScreenWidth, ScreenHeight);

        dashboard.ResizeViewport(0, ScreenHeight);
        indicator.ResizeViewport(0, ScreenHeight);
        indicator.StartAIThinking("Bot Prime");
        indicator.ShowReinforcement("region-1", LocationType.Region, 3, Color.Red);
        indicator.ShowArmyMovement(Vector2.Zero, Vector2.One, 2, Color.Blue, "army-1");
        indicator.AddLogEntry("AI acted", Color.White);

        Assert.True(dashboard.IsVisible);
        Assert.True(indicator.IsAIThinking);
        Assert.Equal("Bot Prime", indicator.ActiveAIPlayerName);
        Assert.True(indicator.HasActiveMovementAnimations());
        Assert.NotEmpty(indicator.GetRecentLogEntries());
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

    private static void SetPrivateField<T>(object instance, string fieldName, T value)
        where T : class
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field.SetValue(instance, value);
    }

    private static void InvokeResizeViewport(object screen, int width, int height)
    {
        var resizeMethod = screen.GetType().GetMethod("ResizeViewport", BindingFlags.Instance | BindingFlags.Public, [typeof(int), typeof(int)]);
        Assert.NotNull(resizeMethod);
        resizeMethod.Invoke(screen, [width, height]);
    }

    private static void AssertText(object root, params string[] expectedFragments)
    {
        var texts = CollectTexts(root).ToList();
        foreach (var expectedFragment in expectedFragments)
        {
            Assert.Contains(texts, text => text.Contains(expectedFragment, StringComparison.OrdinalIgnoreCase));
        }
    }

    private static void AssertHasScrollViewer(object root)
    {
        Assert.Contains(CollectWidgets(root), widget => widget is ScrollViewer);
    }

    private static IReadOnlyList<Widget> CollectWidgets(object root)
    {
        var widgets = new List<Widget>();
        var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
        CollectWidgetsCore(root, widgets, seen);
        return widgets;
    }

    private static void CollectWidgetsCore(object? current, List<Widget> widgets, HashSet<object> seen)
    {
        if (current == null || !seen.Add(current))
        {
            return;
        }

        if (current is Widget widget)
        {
            widgets.Add(widget);
        }

        foreach (var child in GetChildObjects(current))
        {
            CollectWidgetsCore(child, widgets, seen);
        }
    }

    private static IEnumerable<string> CollectTexts(object root)
    {
        var texts = new List<string>();
        var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
        CollectTextsCore(root, texts, seen);
        return texts.Where(text => !string.IsNullOrWhiteSpace(text));
    }

    private static void CollectTextsCore(object? current, List<string> texts, HashSet<object> seen)
    {
        if (current == null || !seen.Add(current))
        {
            return;
        }

        var textProperty = current.GetType().GetProperty("Text", BindingFlags.Instance | BindingFlags.Public);
        if (textProperty?.GetValue(current) is string text)
        {
            texts.Add(text);
        }

        var titleProperty = current.GetType().GetProperty("Title", BindingFlags.Instance | BindingFlags.Public);
        if (titleProperty?.GetValue(current) is string title)
        {
            texts.Add(title);
        }

        foreach (var child in GetChildObjects(current))
        {
            CollectTextsCore(child, texts, seen);
        }
    }

    private static IEnumerable<object> GetChildObjects(object current)
    {
        foreach (var propertyName in new[] { "Widgets", "Content", "Root", "Items" })
        {
            var property = current.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (property == null || property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            var value = property.GetValue(current);
            if (value == null)
            {
                continue;
            }

            if (value is string)
            {
                continue;
            }

            if (value is IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                {
                    if (item != null)
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

    private static string FindRepositoryFile(string relativePath)
    {
        var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            var candidate = Path.Combine(directory.FullName, normalized);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not find repository file {relativePath}");
    }

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

        public static FileRestoreGuard Capture(string path)
        {
            return new FileRestoreGuard(Path.GetFullPath(path));
        }

        public static FileRestoreGuard CaptureAndDelete(string path)
        {
            var guard = Capture(path);
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
