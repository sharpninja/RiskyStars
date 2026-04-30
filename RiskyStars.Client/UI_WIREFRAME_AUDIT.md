# UI Wireframe Audit

Canonical screenshot-comparison wireframes are direct prompt-rendered PNGs, not diagram renderings. The source catalog is `RiskyStars.Client/Wireframes/PromptWireframes/wireframe-prompts.json`, and the renderer is `RiskyStars.Client/Wireframes/PromptWireframes/render-prompt-wireframes.ps1`.

All prompt wireframes are rendered at the measured maximized laptop client resolution: `1536x832`.

The executable structural audit is `RiskyStars.Tests/UiWireframeAuditTests.cs`. It compares each expected screen contract with the actual constructed Myra visual tree or XNA presentation model, validates the direct prompt-rendered PNG for every screen, and rejects obsolete diagram artifacts as screenshot-comparison baselines.

The executable screenshot audit is `RiskyStars.Tests/ClientDebugGameScreenshotIntegrationTests.cs`. It launches `RiskyStars.Client.exe`, navigates every documented screen through the debug gRPC protocol, and captures actual PNGs from the client window HWND into `RiskyStars.Client/Screenshots/Actual` at `1536x832`.

Prompt render command:

```powershell
RiskyStars.Client/Wireframes/PromptWireframes/render-prompt-wireframes.ps1
```

## Documented Screens

### Main menu

- Source: `RiskyStars.Client/UI/Screens/MainMenu.cs`
- Expected layout: ScreenRoot > ViewportFrame > ScrollViewer > Grid: briefing + command actions + footer
- Prompt wireframe: `RiskyStars.Client/Wireframes/PromptWireframes/main-menu.png`

### Main menu settings

- Source: `RiskyStars.Client/UI/Screens/MainMenu.cs`
- Expected layout: ScreenRoot > ViewportFrame > Grid: header + bounded settings scroller + action bar
- Prompt wireframe: `RiskyStars.Client/Wireframes/PromptWireframes/main-menu-settings.png`

### Main menu connecting

- Source: `RiskyStars.Client/UI/Screens/MainMenu.cs`
- Expected layout: ScreenRoot > ViewportFrame > ScrollViewer > Stack: uplink message
- Prompt wireframe: `RiskyStars.Client/Wireframes/PromptWireframes/main-menu-connecting.png`

### Game mode selector

- Source: `RiskyStars.Client/UI/Screens/GameModeSelector.cs`
- Expected layout: ScreenRoot > ViewportFrame > ScrollViewer > Stack: header + two mode cards + actions
- Prompt wireframe: `RiskyStars.Client/Wireframes/PromptWireframes/game-mode-selector.png`

### Connection screen

- Source: `RiskyStars.Client/UI/Screens/ConnectionScreen.cs`
- Expected layout: ScreenRoot > ViewportFrame > ScrollViewer > Grid: identity/server fields + link-state actions
- Prompt wireframe: `RiskyStars.Client/Wireframes/PromptWireframes/connection-screen.png`

### Lobby browser

- Source: `RiskyStars.Client/UI/Screens/LobbyBrowserScreen.cs`
- Expected layout: ScreenRoot > ViewportFrame > ScrollViewer > Stack: header + meta strip + scrollable lobby list + actions
- Prompt wireframe: `RiskyStars.Client/Wireframes/PromptWireframes/lobby-browser.png`

### Create lobby

- Source: `RiskyStars.Client/UI/Screens/CreateLobbyScreen.cs`
- Expected layout: ScreenRoot > ViewportFrame > ScrollViewer > Stack: header + setup cards + actions
- Prompt wireframe: `RiskyStars.Client/Wireframes/PromptWireframes/create-lobby.png`

### Multiplayer lobby

- Source: `RiskyStars.Client/UI/Screens/LobbyScreen.cs`
- Expected layout: ScreenRoot > ViewportFrame > ScrollViewer > Stack: header + lobby metadata + scrollable slots + actions
- Prompt wireframe: `RiskyStars.Client/Wireframes/PromptWireframes/multiplayer-lobby.png`

### Single-player lobby

- Source: `RiskyStars.Client/UI/Screens/SinglePlayerLobbyScreen.cs`
- Expected layout: ScreenRoot > ViewportFrame > ScrollViewer > Stack: map/name cards + server status + scrollable opponent lineup + actions
- Prompt wireframe: `RiskyStars.Client/Wireframes/PromptWireframes/single-player-lobby.png`

### Gameplay HUD top bar

- Source: `RiskyStars.Client/UI/Controls/GameplayHudOverlay.cs`
- Expected layout: TopBar Panel > Grid: turn/status row + resource chips + shortcut hints
- Prompt wireframe: `RiskyStars.Client/Wireframes/PromptWireframes/gameplay-hud-top-bar.png`

### Gameplay HUD legend

- Source: `RiskyStars.Client/UI/Controls/GameplayHudOverlay.cs`
- Expected layout: Legend Panel > ScrollViewer > Stack: map key rows
- Prompt wireframe: `RiskyStars.Client/Wireframes/PromptWireframes/gameplay-hud-legend.png`

### Side panel container

- Source: `RiskyStars.Client/UI/Windows/SidePanelContainer.cs`
- Expected layout: SidePanel Root > Header chrome + bounded ScrollViewer content + resize/collapse affordance
- Prompt wireframe: `RiskyStars.Client/Wireframes/PromptWireframes/side-panel-container.png`

### Settings window

- Source: `RiskyStars.Client/UI/Windows/SettingsWindow.cs`
- Expected layout: Window > Tabs: graphics/audio/controls/server pages each with scrollable tab body + action footer
- Prompt wireframe: `RiskyStars.Client/Wireframes/PromptWireframes/settings-window.png`

### Debug information window

- Source: `RiskyStars.Client/UI/Windows/DebugInfoWindow.cs`
- Expected layout: Dockable Window > ScrollViewer > Stack: camera/performance/state/selection/UI audit panels
- Prompt wireframe: `RiskyStars.Client/Wireframes/PromptWireframes/debug-info-window.png`

### Player dashboard window

- Source: `RiskyStars.Client/UI/Windows/PlayerDashboardWindow.cs`
- Expected layout: Dockable Window > ScrollViewer > Stack: resources + army purchase + hero assignment controls
- Prompt wireframe: `RiskyStars.Client/Wireframes/PromptWireframes/player-dashboard-window.png`

### AI visualization window

- Source: `RiskyStars.Client/UI/Windows/AIVisualizationWindow.cs`
- Expected layout: Dockable Window > ScrollViewer > Stack: AI status + visualization toggles + activity log
- Prompt wireframe: `RiskyStars.Client/Wireframes/PromptWireframes/ai-visualization-window.png`

### Encyclopedia window

- Source: `RiskyStars.Client/UI/Windows/EncyclopediaWindow.cs`
- Expected layout: Dockable Window > Split layout: scrollable article navigation + scrollable article body
- Prompt wireframe: `RiskyStars.Client/Wireframes/PromptWireframes/encyclopedia-window.png`

### UI scale window

- Source: `RiskyStars.Client/UI/Windows/UiScaleWindow.cs`
- Expected layout: Dockable Window > ScrollViewer > Stack: scale slider + presets + apply/reset actions
- Prompt wireframe: `RiskyStars.Client/Wireframes/PromptWireframes/ui-scale-window.png`

### Tutorial mode window

- Source: `RiskyStars.Client/UI/Windows/TutorialModeWindow.cs`
- Expected layout: Dockable Window > Grid: fixed title + bounded scroll body + fixed footer buttons
- Prompt wireframe: `RiskyStars.Client/Wireframes/PromptWireframes/tutorial-mode-window.png`

### Continent zoom window

- Source: `RiskyStars.Client/UI/Windows/ContinentZoomWindow.cs`
- Expected layout: Window > Header labels + Myra-hosted XNA Image surface for planet continents
- Prompt wireframe: `RiskyStars.Client/Wireframes/PromptWireframes/continent-zoom-window.png`

### Combat HUD overlay

- Source: `RiskyStars.Client/UI/Controls/CombatHudOverlay.cs`
- Expected layout: Backdrop + Window Panel > ScrollViewer: combat title, sides, rolls, pairings, casualties, survivors
- Prompt wireframe: `RiskyStars.Client/Wireframes/PromptWireframes/combat-hud-overlay.png`

### Server status indicator

- Source: `RiskyStars.Client/UI/Controls/ServerStatusIndicator.cs`
- Expected layout: Panel > Grid: server state text + metrics lines
- Prompt wireframe: `RiskyStars.Client/Wireframes/PromptWireframes/server-status-indicator.png`

### Dialog manager

- Source: `RiskyStars.Client/UI/Dialogs/DialogManager.cs`
- Expected layout: Dialog > title + message + action buttons
- Prompt wireframe: `RiskyStars.Client/Wireframes/PromptWireframes/dialog-manager.png`

### Combat event dialog

- Source: `RiskyStars.Client/UI/Dialogs/CombatEventDialog.cs`
- Expected layout: Dialog > combat summary panels + close action
- Prompt wireframe: `RiskyStars.Client/Wireframes/PromptWireframes/combat-event-dialog.png`

### Context menu manager

- Source: `RiskyStars.Client/UI/Controls/ContextMenuManager.cs`
- Expected layout: Desktop overlay > context menu rows for selected object actions
- Prompt wireframe: `RiskyStars.Client/Wireframes/PromptWireframes/context-menu-manager.png`

### Combat screen

- Source: `RiskyStars.Client/Gameplay/CombatScreen.cs`
- Expected layout: XNA presentation model: combat title/status + attackers/defenders/rolls/pairings/casualties/survivors
- Prompt wireframe: `RiskyStars.Client/Wireframes/PromptWireframes/combat-screen.png`

### Legacy player dashboard

- Source: `RiskyStars.Client/UI/PlayerDashboard.cs`
- Expected layout: XNA panel model: visible dashboard region with player command controls
- Prompt wireframe: `RiskyStars.Client/Wireframes/PromptWireframes/legacy-player-dashboard.png`

### AI action indicator

- Source: `RiskyStars.Client/UI/AIActionIndicator.cs`
- Expected layout: XNA overlay model: active AI thinking state, movement animations, reinforcement events, activity log
- Prompt wireframe: `RiskyStars.Client/Wireframes/PromptWireframes/ai-action-indicator.png`
