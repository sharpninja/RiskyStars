# UI Screen Documentation And Test Matrix

This matrix is the authoritative inventory of RiskyStars UI surfaces. Every entry is expected to be represented in the visual tree where applicable and covered by automated tests for construction, core content, and the bad behavior the screen must prevent.

## Test Rules

- Myra screens must construct a non-null root/window/container and expose the expected labels or buttons.
- Content-heavy Myra screens must place growing content behind a constrained `ScrollViewer` so UI scale and text wrapping do not expand past the panel.
- XNA gameplay overlays must expose their presentation model or Myra-hosted overlay widgets so the behavior can be validated without a graphics device.
- Dialog and context surfaces must be tested for open/close behavior and for not leaving stale widgets behind.
- The debug visual tree must be able to export the entire UI hierarchy as JSON and preserve parent/child nesting.
- `UI_WIREFRAME_AUDIT.md` is the authoritative wireframe inventory and must list the direct prompt-rendered baseline for every screen.
- Direct prompt-rendered spatial screenshot baselines must exist as `Wireframes/PromptWireframes/*.png` at the laptop maximized work-area resolution, `1536x832`.
- Diagram artifacts are not canonical screenshot baselines; the audit must reject them for wireframe-vs-actual comparison.
- `UiWireframeAuditTests` must compare each documented wireframe contract against the actual constructed Myra tree or XNA presentation model.
- `ClientDebugGameScreenshotIntegrationTests` must launch the real client, navigate every screen through the debug gRPC protocol, and capture `Screenshots/Actual/*.png` from the client window HWND at `1536x832`.
- Screenshot-vs-wireframe comparison artifacts must exist for every documented screen in `Screenshots/Comparisons`, and the divergence report must enumerate every screen in `Screenshots/WIREFRAME_SCREENSHOT_DIVERGENCE.md`.

## Menu And Lobby Screens

| Screen | Source | Required structure | Automated coverage |
| --- | --- | --- | --- |
| Main menu | `RiskyStars.Client/UI/Screens/MainMenu.cs` | Root screen panel, viewport frame, scrollable command deck, Multiplayer, Single Player, Tutorial Mode, Settings, Exit | `UiScreenConstructionTests.MainMenu_BuildsMainSettingsAndConnectingStates` |
| Game mode selector | `RiskyStars.Client/UI/Screens/GameModeSelector.cs` | Root screen panel, scrollable mode cards, Continue, Back | `UiScreenConstructionTests.MenuAndLobbyScreens_BuildExpectedContent` |
| Connection screen | `RiskyStars.Client/UI/Screens/ConnectionScreen.cs` | Root screen panel, scrollable identity/server form, Connect, Back | `UiScreenConstructionTests.MenuAndLobbyScreens_BuildExpectedContent` |
| Lobby browser | `RiskyStars.Client/UI/Screens/LobbyBrowserScreen.cs` | Root screen panel, scrollable lobby list, Create Lobby, Join Lobby, Refresh | `UiScreenConstructionTests.LobbyBrowser_PopulatesLobbyListAndRejectsEmptySelection` |
| Create lobby | `RiskyStars.Client/UI/Screens/CreateLobbyScreen.cs` | Root screen panel, scrollable setup form, map selection, maximum commander count, Create, Cancel | `UiScreenConstructionTests.MenuAndLobbyScreens_BuildExpectedContent` |
| Multiplayer lobby | `RiskyStars.Client/UI/Screens/LobbyScreen.cs` | Root screen panel, scrollable lobby info, scrollable player slots, Ready, Start Game, Leave Lobby | `UiScreenConstructionTests.MultiplayerLobby_BuildsSlotsAndStateControls` |
| Single-player lobby | `RiskyStars.Client/UI/Screens/SinglePlayerLobbyScreen.cs` | Root screen panel, scrollable setup, scrollable AI slot list, Start Game, Back | `UiScreenConstructionTests.SinglePlayerLobby_BuildsSetupAndPlayerSlots` |

## In-Game Myra Surfaces

| Screen | Source | Required structure | Automated coverage |
| --- | --- | --- | --- |
| Gameplay HUD | `RiskyStars.Client/UI/Controls/GameplayHudOverlay.cs` | Desktop root with top bar, resource chips, map key, selection panel, help panel, side-panel containers | `GameplayHudOverlayTests`, `ClientDebugProtocolIntegrationTests` |
| Side panel container | `RiskyStars.Client/UI/Windows/SidePanelContainer.cs` | Persistent container with header controls and scrollable remaining content region | `UiScreenConstructionTests.SidePanelContainer_BuildsScrollableContentAndKeepsHeaderOnCollapse` |
| Settings window | `RiskyStars.Client/UI/Windows/SettingsWindow.cs` | Modal window with Graphics, Audio, Controls, Server tabs, each tab scrollable, Apply, Cancel | `UiScreenConstructionTests.SettingsWindow_BuildsAllTabsWithScrollableContent` |
| Debug information window | `RiskyStars.Client/UI/Windows/DebugInfoWindow.cs` | Dockable window, scrollable camera/performance/state/selection/UI audit panels, selectable visual tree | `UiScreenConstructionTests.DockableWindows_BuildExpectedScrollableContent`, `TutorialHighlightTargetsTests`, `ClientDebugControllerTests` |
| Player dashboard window | `RiskyStars.Client/UI/Windows/PlayerDashboardWindow.cs` | Dockable window, scrollable resources, purchase controls, hero controls | `UiScreenConstructionTests.DockableWindows_BuildExpectedScrollableContent` |
| AI visualization window | `RiskyStars.Client/UI/Windows/AIVisualizationWindow.cs` | Dockable window, scrollable status/options/activity log, checkboxes | `UiScreenConstructionTests.DockableWindows_BuildExpectedScrollableContent` |
| Encyclopedia window | `RiskyStars.Client/UI/Windows/EncyclopediaWindow.cs` | Dockable window, article navigation scroller, article content scroller | `UiScreenConstructionTests.EncyclopediaWindow_BuildsNavigationAndArticleContent` |
| UI scale window | `RiskyStars.Client/UI/Windows/UiScaleWindow.cs` | Dockable window, scrollable scale controls, presets, Apply, Reset | `UiScreenConstructionTests.DockableWindows_BuildExpectedScrollableContent` |
| Tutorial mode window | `RiskyStars.Client/UI/Windows/TutorialModeWindow.cs` | Dockable window, scrollable body, fixed footer buttons, highlight target model | `TutorialModeWindowAnchorTests`, `TutorialHighlightTargetsTests`, `UiScreenConstructionTests.TutorialModeWindow_BuildsScrollableBodyAndStableFooter` |
| Continent zoom window | `RiskyStars.Client/UI/Windows/ContinentZoomWindow.cs` | Window with Myra-hosted XNA image surface, header scroller, close button, persistent zoom while selecting | `ContinentZoomWindowTests`, `ContinentZoomLayoutTests`, `ContinentZoomRenderModelTests`, `ContinentZoomGraphicsStateTests` |
| Combat HUD overlay | `RiskyStars.Client/UI/Controls/CombatHudOverlay.cs` | Myra backdrop/window with scrollable combat sections and resize-constrained viewport | `UiScreenConstructionTests.CombatHudOverlay_BuildsScrollableCombatPresentation` |
| Server status indicator | `RiskyStars.Client/UI/Controls/ServerStatusIndicator.cs` | Compact status panel with dot, state label, detail label | `UiScreenConstructionTests.ServerStatusIndicator_BuildsStoppedStateAndRejectsInvalidResize` |
| Dialog manager | `RiskyStars.Client/UI/Dialogs/DialogManager.cs` | Modal frame with typed title color, wrapped message, configured buttons, clean close | `UiScreenConstructionTests.DialogManager_BuildsAndClosesEveryDialogType` |
| Combat event dialog | `RiskyStars.Client/UI/Dialogs/CombatEventDialog.cs` | Combat modal with summary text and action button for initiated, reinforcement, and completion events | `UiScreenConstructionTests.CombatEventDialog_BuildsAllCombatDialogVariants` |
| Context menu manager | `RiskyStars.Client/UI/Controls/ContextMenuManager.cs` | Transient Myra menu hosted by the desktop and removed cleanly when closed | `UiScreenConstructionTests.ContextMenuManager_OpensAndClosesRegionMenu` |

## XNA Gameplay Surfaces

| Screen | Source | Required structure | Automated coverage |
| --- | --- | --- | --- |
| Combat screen | `RiskyStars.Client/Gameplay/CombatScreen.cs` | XNA combat state machine with presentation projection consumed by `CombatHudOverlay` | `UiScreenConstructionTests.CombatScreen_CreatesPresentationAndClosesCleanly` |
| Legacy player dashboard | `RiskyStars.Client/Gameplay/PlayerDashboard.cs` | Older XNA dashboard wrapper retained for compatibility until removed | `UiScreenConstructionTests.LegacyXnaScreens_ConstructAndResizeWithoutGraphicsSideEffects` |
| AI action indicator | `RiskyStars.Client/Gameplay/AIActionIndicator.cs` | XNA AI activity/log/animation model, no Myra root | `UiScreenConstructionTests.LegacyXnaScreens_ConstructAndResizeWithoutGraphicsSideEffects` |

## Exclusions

Themed factories, validators, input controllers, map renderers, and pure data/layout helpers are not screens. They remain covered by their own unit tests or by the screen tests that consume them.
