# Session Log

## 2026-04-18

### Summary
- Reorganized `RiskyStars.Client` source out of flat root into feature/system folders:
  - `App`
  - `Infrastructure`
  - `Networking`
  - `Lobby`
  - `State`
  - `Rendering`
  - `Gameplay`
  - `UI`
- Updated [AGENTS.md](/F:/GitHub/RiskyStars/AGENTS.md) client structure notes to match new layout.
- Fixed client nullability warnings instead of suppressing them.
- Set [RiskyStars.Client.csproj](/F:/GitHub/RiskyStars/RiskyStars.Client/RiskyStars.Client.csproj) to treat warnings as errors, while ignoring Myra obsolete warnings with `CS0618`.
- Removed unused import from [game.proto](/F:/GitHub/RiskyStars/RiskyStars.Shared/Protos/game.proto) to eliminate shared-project warning.

### Key Code Changes
- Added `EnsureGameClient()` to [ConnectionManager.cs](/F:/GitHub/RiskyStars/RiskyStars.Client/Networking/ConnectionManager.cs) so multiplayer startup has concrete `GrpcGameClient` before async connect.
- Exposed multiplayer server address from [LobbyClient.cs](/F:/GitHub/RiskyStars/RiskyStars.Client/Networking/LobbyClient.cs) and [LobbyManager.cs](/F:/GitHub/RiskyStars/RiskyStars.Client/Lobby/LobbyManager.cs).
- Hardened startup paths in [RiskyStarsGame.cs](/F:/GitHub/RiskyStars/RiskyStars.Client/App/RiskyStarsGame.cs) with explicit non-null guards and stable local references.
- Fixed constructor-init nullability in [ServerStatusIndicator.cs](/F:/GitHub/RiskyStars/RiskyStars.Client/UI/Controls/ServerStatusIndicator.cs).
- Fixed nullable player-id flow in [LobbyScreen.cs](/F:/GitHub/RiskyStars/RiskyStars.Client/UI/Screens/LobbyScreen.cs).

### Verification
- `dotnet build F:\\GitHub\\RiskyStars\\RiskyStars.sln`
- Result: `0 Warning(s), 0 Error(s)`

### Notes
- One stale embedded server process (`dotnet` hosting `RiskyStars.Server.dll`) was locking server output during build. Killed process, then full solution built clean.
- Working tree still has broader in-progress changes beyond this log entry. No commit made in this step.

### Additional Fix
- Investigated blank in-game view after successful connect.
- Root cause: [RiskyStarsGame.cs](/F:/GitHub/RiskyStars/RiskyStars.Client/App/RiskyStarsGame.cs) used Myra `Desktop` for in-game windows, and global desktop theme gave that desktop a full-screen backdrop. That backdrop rendered over map and HUD, leaving only status overlay visible.
- Fix: set in-game desktop background to transparent so world render and classic HUD remain visible under Myra windows.
- Verification: `dotnet build F:\\GitHub\\RiskyStars\\RiskyStars.Client\\RiskyStars.Client.csproj` -> `0 Warning(s), 0 Error(s)`.

### Follow-up Fix
- User reported no change after first transparent-desktop patch.
- Root cause refinement: [SettingsWindow.cs](/F:/GitHub/RiskyStars/RiskyStars.Client/UI/Windows/SettingsWindow.cs) always rendered its own Myra `Desktop`, even while the settings window itself was hidden. That desktop still painted a full-screen backdrop every frame in-game.
- Additional hardening: [PlayerDashboard.cs](/F:/GitHub/RiskyStars/RiskyStars.Client/Gameplay/PlayerDashboard.cs) now also uses a transparent desktop background so it cannot hide the map if its desktop root renders.
- Fixes:
  - `SettingsWindow.Render()` now returns immediately unless the settings window is actually open.
  - `SettingsWindow` desktop background is transparent.
  - `PlayerDashboard` desktop background is transparent.
- Verification: `dotnet build F:\\GitHub\\RiskyStars\\RiskyStars.Client\\RiskyStars.Client.csproj` -> `0 Warning(s), 0 Error(s)`.

### Gameplay UI Pass
- Fixed mouse-wheel zoom in [Camera2D.cs](/F:/GitHub/RiskyStars/RiskyStars.Client/Rendering/Camera2D.cs):
  - zoom now uses wheel delta instead of the absolute wheel counter
  - zoom anchors to cursor position
  - invert zoom setting now actually reaches the camera
- Improved system readability in [MapRenderer.cs](/F:/GitHub/RiskyStars/RiskyStars.Client/Rendering/MapRenderer.cs):
  - systems now show star cores and orbit rings so stellar bodies read as members of a system
- Prevented hyperspace-lane mouth/body overlap in [MapLoader.cs](/F:/GitHub/RiskyStars/RiskyStars.Client/State/MapLoader.cs):
  - lane mouths now use computed safe positions around each system rather than fixed offsets
- Reworked the in-game HUD in [UIRenderer.cs](/F:/GitHub/RiskyStars/RiskyStars.Client/Rendering/UIRenderer.cs):
  - top bar now carries turn/phase, player resources, current message, and panel restore hotkeys
  - added always-visible map legend
  - removed the old left-side game info slab from the main HUD path
- Hid the centered status overlay once the game is live in [RiskyStarsGame.cs](/F:/GitHub/RiskyStars/RiskyStars.Client/App/RiskyStarsGame.cs):
  - gameplay now uses the top bar for status instead
  - embedded server status indicator now hides after the world sync completes and only reappears for connect/reconnect/error states
- Matched in-game Myra windows to gameplay HUD styling:
  - added gameplay-specific window theme in [ThemeManager.cs](/F:/GitHub/RiskyStars/RiskyStars.Client/UI/Theme/ThemeManager.cs)
  - dockable windows now use gameplay theme in [DockableWindow.cs](/F:/GitHub/RiskyStars/RiskyStars.Client/UI/Windows/DockableWindow.cs)
  - inner panels in [PlayerDashboardWindow.cs](/F:/GitHub/RiskyStars/RiskyStars.Client/UI/Windows/PlayerDashboardWindow.cs), [DebugInfoWindow.cs](/F:/GitHub/RiskyStars/RiskyStars.Client/UI/Windows/DebugInfoWindow.cs), and [AIVisualizationWindow.cs](/F:/GitHub/RiskyStars/RiskyStars.Client/UI/Windows/AIVisualizationWindow.cs) now use gameplay panel styling instead of menu chrome
- Verification: `dotnet build F:\\GitHub\\RiskyStars\\RiskyStars.Client\\RiskyStars.Client.csproj` -> `0 Warning(s), 0 Error(s)`.

### Right-Mouse Drag Pan
- Added RTS-style right-mouse drag panning.
- [Camera2D.cs](/F:/GitHub/RiskyStars/RiskyStars.Client/Rendering/Camera2D.cs) now exposes `PanByScreenDelta(...)`.
- [InputController.cs](/F:/GitHub/RiskyStars/RiskyStars.Client/UI/Controls/InputController.cs) now:
  - tracks right-button drag threshold
  - pans the map while the right button is dragged
  - only opens the context menu on right-button release when no drag happened
- Clean verification used alternate output path because the live client/Visual Studio locked the normal build output:
  - `dotnet build F:\\GitHub\\RiskyStars\\RiskyStars.Client\\RiskyStars.Client.csproj -o F:\\GitHub\\RiskyStars\\.codex-build\\client-rmb-pan`
  - Result: `0 Warning(s), 0 Error(s)`

## 2026-04-19

### In-Game UI Scale Panel
- Added a dedicated dockable in-game UI scale panel in [UiScaleWindow.cs](/F:/GitHub/RiskyStars/RiskyStars.Client/UI/Windows/UiScaleWindow.cs).
- New panel behavior:
  - toggled with `F4`
  - uses gameplay chrome instead of menu chrome
  - supports slider + quick presets + apply/reset actions
  - saves the selected `UiScalePercent` back to [settings.json](</F:/GitHub/RiskyStars/settings.json>) through [Settings.cs](/F:/GitHub/RiskyStars/RiskyStars.Client/Infrastructure/Settings.cs)
- Wired panel into [RiskyStarsGame.cs](/F:/GitHub/RiskyStars/RiskyStars.Client/App/RiskyStarsGame.cs):
  - creates/attaches alongside other dockable in-game windows
  - rebuilds in-game Myra windows after UI scale changes so the new scale actually takes effect live
  - resizes/repositions the new panel on viewport changes
- Updated gameplay HUD and help text in [UIRenderer.cs](/F:/GitHub/RiskyStars/RiskyStars.Client/Rendering/UIRenderer.cs):
  - top bar now shows `F4 Scale`
  - shortcut/help overlay now lists the UI scale panel and right-mouse drag pan
- Updated [SettingsWindow.cs](/F:/GitHub/RiskyStars/RiskyStars.Client/UI/Windows/SettingsWindow.cs) shortcut reference to include `F4` and `Right Mouse Drag`.

### Verification
- `dotnet build F:\\GitHub\\RiskyStars\\RiskyStars.Client\\RiskyStars.Client.csproj -o F:\\GitHub\\RiskyStars\\.codex-build\\client-ui-scale-panel`
- Result: `0 Warning(s), 0 Error(s)`

### Gameplay HUD Scale + Flat Myra Shell
- Extended UI scale so it now affects the classic/XNA gameplay HUD in [UIRenderer.cs](/F:/GitHub/RiskyStars/RiskyStars.Client/Rendering/UIRenderer.cs), not just Myra widgets:
  - top bar sizing and text
  - resource chips
  - map legend
  - selection panel
  - shortcut/help panel
  - debug overlay
- Flattened shared Myra styling in [ThemeManager.cs](/F:/GitHub/RiskyStars/RiskyStars.Client/UI/Theme/ThemeManager.cs) and [ThemedUIFactory.cs](/F:/GitHub/RiskyStars/RiskyStars.Client/UI/Theme/ThemedUIFactory.cs):
  - windows, tabs, panels, text boxes, combo boxes, list rows, and spinners now use the same dark flat shell language as the gameplay HUD
  - removed the heavy asset-backed panel chrome from shared panel/window builders
  - front-end screens inherit the flatter shell automatically because they route through the shared builders/theme

### Verification
- `dotnet build F:\\GitHub\\RiskyStars\\RiskyStars.Client\\RiskyStars.Client.csproj -o F:\\GitHub\\RiskyStars\\.codex-build\\client-scale-theme-pass`
- Result: `0 Warning(s), 0 Error(s)`

### Live UI Scale Preview
- UI scale now applies live while dragging the slider instead of waiting for `Apply`.
- [UiScaleWindow.cs](/F:/GitHub/RiskyStars/RiskyStars.Client/UI/Windows/UiScaleWindow.cs):
  - slider now previews scale immediately on `ValueChanged`
  - `Apply` now only persists the already-previewed scale to disk
- [RiskyStarsGame.cs](/F:/GitHub/RiskyStars/RiskyStars.Client/App/RiskyStarsGame.cs):
  - added `PreviewUiScale(...)` runtime path
  - split runtime settings application from persisted apply flow
  - in-game window refresh can now preserve the live UI-scale window during preview so dragging the slider does not destroy its own panel
- [SettingsWindow.cs](/F:/GitHub/RiskyStars/RiskyStars.Client/UI/Windows/SettingsWindow.cs):
  - graphics-tab UI-scale slider now previews live too
  - `Cancel` reverts any live-previewed scale change from that window before closing

### Verification
- `dotnet build F:\\GitHub\\RiskyStars\\RiskyStars.Client\\RiskyStars.Client.csproj -o F:\\GitHub\\RiskyStars\\.codex-build\\client-live-scale-preview`
- Result: `0 Warning(s), 0 Error(s)`

### Gameplay HUD Migration + Unified Scale Ruler
- Moved gameplay workspace UI off XNA HUD drawing and onto Myra overlay widgets.
- Added [GameplayHudOverlay.cs](/F:/GitHub/RiskyStars/RiskyStars.Client/UI/Controls/GameplayHudOverlay.cs):
  - Myra top status bar
  - Myra resource chips
  - Myra always-visible map key
  - Myra selection panel
  - Myra help/shortcut overlay
- Integrated the overlay into [RiskyStarsGame.cs](/F:/GitHub/RiskyStars/RiskyStars.Client/App/RiskyStarsGame.cs):
  - attached to the in-game Myra desktop
  - refreshed with live game state / selection / panel visibility
  - removed the old XNA HUD draw calls from the in-game render path
- Added a single shared UI geometry scale helper in [ThemeManager.cs](/F:/GitHub/RiskyStars/RiskyStars.Client/UI/Theme/ThemeManager.cs):
  - `ScalePixels(...)`
  - icon and panel size tokens now scale from the same UI scale factor
  - separate padding/frame/font-percent knobs no longer drive runtime geometry; UI scale is the common ruler
- Scaled key fixed-size Myra surfaces with the shared ruler:
  - [DockableWindow.cs](/F:/GitHub/RiskyStars/RiskyStars.Client/UI/Windows/DockableWindow.cs)
  - [ServerStatusIndicator.cs](/F:/GitHub/RiskyStars/RiskyStars.Client/UI/Controls/ServerStatusIndicator.cs)
  - [UiScaleWindow.cs](/F:/GitHub/RiskyStars/RiskyStars.Client/UI/Windows/UiScaleWindow.cs)
  - [PlayerDashboardWindow.cs](/F:/GitHub/RiskyStars/RiskyStars.Client/UI/Windows/PlayerDashboardWindow.cs)
  - [SettingsWindow.cs](/F:/GitHub/RiskyStars/RiskyStars.Client/UI/Windows/SettingsWindow.cs)
- Simplified the main-menu theme settings surface in [MainMenu.cs](/F:/GitHub/RiskyStars/RiskyStars.Client/UI/Screens/MainMenu.cs) so it no longer exposes separate spacing/chrome geometry controls.

### Verification
- `dotnet build F:\\GitHub\\RiskyStars\\RiskyStars.Client\\RiskyStars.Client.csproj -o F:\\GitHub\\RiskyStars\\.codex-build\\client-myra-hud-pass`
- Result: `0 Warning(s), 0 Error(s)`

### AI Activity + Dead HUD Cleanup
- Removed the dead XNA HUD path entirely:
  - deleted [UIRenderer.cs](/F:/GitHub/RiskyStars/RiskyStars.Client/Rendering/UIRenderer.cs)
  - removed all remaining `RiskyStarsGame` references to the old HUD renderer
- Moved AI screen-space status/log overlays into the Myra gameplay HUD:
  - [AIActionIndicator.cs](/F:/GitHub/RiskyStars/RiskyStars.Client/Gameplay/AIActionIndicator.cs) now only draws world-space effects (movement/reinforcement)
  - [GameplayHudOverlay.cs](/F:/GitHub/RiskyStars/RiskyStars.Client/UI/Controls/GameplayHudOverlay.cs) now includes a Myra `AI Activity` panel that shows active AI turn state and recent AI orders
- Updated gameplay/rendering docs to reflect the new single-UI-system reality:
  - [RENDERING.md](/F:/GitHub/RiskyStars/RiskyStars.Client/RENDERING.md)
  - [AGENTS.md](/F:/GitHub/RiskyStars/AGENTS.md)

### Combat Myra Overlay
- Replaced the live combat presentation layer with a Myra overlay while preserving the existing combat-step logic:
  - added [CombatHudOverlay.cs](/F:/GitHub/RiskyStars/RiskyStars.Client/UI/Controls/CombatHudOverlay.cs)
  - [CombatScreen.cs](/F:/GitHub/RiskyStars/RiskyStars.Client/Gameplay/CombatScreen.cs) now exposes `GetPresentation()` for title, round, forces, rolls, pairings, casualties, survivors, and instructions
  - [RiskyStarsGame.cs](/F:/GitHub/RiskyStars/RiskyStars.Client/App/RiskyStarsGame.cs) now updates/renders the combat Myra overlay through the in-game desktop instead of calling the old XNA combat draw path
- Result:
  - map/world rendering stays XNA
  - gameplay workspace UI stays Myra, including combat presentation

### Input / Doc Truth Pass
- Updated stale control docs to match actual input behavior:
  - [RENDERING.md](/F:/GitHub/RiskyStars/RiskyStars.Client/RENDERING.md)
  - [SETTINGS_WINDOW.md](/F:/GitHub/RiskyStars/RiskyStars.Client/SETTINGS_WINDOW.md)
  - [INPUT.md](/F:/GitHub/RiskyStars/RiskyStars.Client/INPUT.md)
  - [AGENTS.md](/F:/GitHub/RiskyStars/AGENTS.md)
- Docs now describe:
  - right-mouse drag pans the map
  - right-click release opens the context menu when no drag occurred

### Verification
- `dotnet build F:\\GitHub\\RiskyStars\\RiskyStars.Client\\RiskyStars.Client.csproj -o F:\\GitHub\\RiskyStars\\.codex-build\\client-myra-audit-pass`
- Result: `0 Warning(s), 0 Error(s)`
- `dotnet build F:\\GitHub\\RiskyStars\\RiskyStars.Client\\RiskyStars.Client.csproj -o F:\\GitHub\\RiskyStars\\.codex-build\\client-myra-combat-pass`
- Result: `0 Warning(s), 0 Error(s)`

## 2026-04-20

### In-Game Encyclopedia
- Added shared reference content in [GameReferenceData.cs](/F:/GitHub/RiskyStars/RiskyStars.Client/Infrastructure/GameReferenceData.cs):
  - encyclopedia articles for turn flow, resources, armies, regions, lane mouths, combat, controls, and panels
- Added [EncyclopediaWindow.cs](/F:/GitHub/RiskyStars/RiskyStars.Client/UI/Windows/EncyclopediaWindow.cs):
  - Myra dockable in-game encyclopedia window
  - article navigator on the left
  - scrollable article detail view on the right
  - hidden by default on first run, then persisted through window preferences
- Wired into [RiskyStarsGame.cs](/F:/GitHub/RiskyStars/RiskyStars.Client/App/RiskyStarsGame.cs):
  - created with other in-game windows
  - attached to the in-game desktop
  - hotkey: `F5`
  - viewport resize handling included

### In-Game Tutorial
- Added tutorial lessons and live context generation in [GameReferenceData.cs](/F:/GitHub/RiskyStars/RiskyStars.Client/Infrastructure/GameReferenceData.cs):
  - first turn
  - economy loop
  - reinforcement
  - movement
  - combat readout
  - workspace/panel recovery
- Added [TutorialWindow.cs](/F:/GitHub/RiskyStars/RiskyStars.Client/UI/Windows/TutorialWindow.cs):
  - Myra dockable tutorial window
  - `Current Guidance` panel driven by phase, active turn, selection, and combat state
  - lesson browser and detail view
  - `Open Suggested Lesson` action to jump to the phase-relevant tutorial page
  - hidden by default on first run, then persisted through window preferences
- Wired into [RiskyStarsGame.cs](/F:/GitHub/RiskyStars/RiskyStars.Client/App/RiskyStarsGame.cs):
  - hotkey: `F6`
  - live updates every in-game frame from current game state and selection
  - visibility included in gameplay HUD panel hints

### HUD / Shortcut Updates
- Updated [GameplayHudOverlay.cs](/F:/GitHub/RiskyStars/RiskyStars.Client/UI/Controls/GameplayHudOverlay.cs):
  - top-bar panel hint now includes `F5 Ref` and `F6 Tut`
  - help overlay now lists encyclopedia and tutorial shortcuts
- Updated shortcut docs:
  - [SETTINGS_WINDOW.md](/F:/GitHub/RiskyStars/RiskyStars.Client/SETTINGS_WINDOW.md)
  - [INPUT.md](/F:/GitHub/RiskyStars/RiskyStars.Client/INPUT.md)
  - [AGENTS.md](/F:/GitHub/RiskyStars/AGENTS.md)

### Verification
- `dotnet build F:\\GitHub\\RiskyStars\\RiskyStars.Client\\RiskyStars.Client.csproj -o F:\\GitHub\\RiskyStars\\.codex-build\\client-encyclopedia-tutorial`
- Result: `0 Warning(s), 0 Error(s)`
