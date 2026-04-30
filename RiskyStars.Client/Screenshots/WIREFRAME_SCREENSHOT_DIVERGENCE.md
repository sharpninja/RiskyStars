# Prompt Wireframe vs Actual Screenshot Divergence Audit

Resolution: `1536x832`

Generated artifact roots:

- Actual screenshots: `RiskyStars.Client/Screenshots/Actual/*.png`
- Prompt source catalog: `RiskyStars.Client/Wireframes/PromptWireframes/wireframe-prompts.json`
- Prompt-authored spatial wireframes: `RiskyStars.Client/Wireframes/PromptWireframes/*.png`
- Side-by-side comparisons: `RiskyStars.Client/Screenshots/Comparisons/*-compare.png`
- Contact sheet: `RiskyStars.Client/Screenshots/Comparisons/overview-contact-sheet.png`
- Pixel metrics: `RiskyStars.Client/Screenshots/Comparisons/comparison-metrics.csv`

The actual screenshots were captured from the live `RiskyStars.Client` HWND after debug gRPC navigation. The capture path validates that the client rectangle fits inside the monitor work area, so screenshots do not include the native title bar or Windows taskbar.

The screenshot baseline is direct prompt-rendered spatial wireframes generated from `PromptWireframes/wireframe-prompts.json`. Diagram artifacts are not accepted as wireframe-vs-actual comparison baselines.

## Remaining Global Divergences

- Prompt baselines now describe screen-relative spatial geometry, but they are still intentionally low-fidelity wireframes. They do not reproduce exact starfield dots, antialiasing, text wrapping, or every Myra internal border.
- Runtime screenshots include generated map state, selected objects, saved/default docking, scroll offsets, and live debug state. Prompt wireframes represent the expected layout intent, not a pixel clone.
- Component-focused screens are captured inside the full game context when that is how the UI is rendered at runtime. The prompt wireframes include the same context category, but some focus panels intentionally simplify internal content.
- The largest remaining geometry gaps are menu/lobby form internals, planet zoom continent rendering, combat details, and dockable window body content density.

## Screen Findings

### ai-action-indicator

- Match: both show the gameplay shell, map systems, and an AI action focus region.
- Divergence: actual AI animation/log state is subtle in the live screenshot; prompt baseline uses an explicit map-object highlight box.

### ai-visualization-window

- Match: both show a gameplay map with a dockable AI Visualization window.
- Divergence: prompt window body is simplified and does not match exact checkbox/list row spacing.

### combat-event-dialog

- Match: both show a modal dialog over dimmed gameplay context.
- Divergence: actual dialog fill is much brighter and has exact Myra button placement; prompt uses dark modal styling and a summary placeholder.

### combat-hud-overlay

- Match: both show combat content over the game context.
- Divergence: actual combat panel is darker and wider left; prompt approximates the panel and section structure but not exact text placement.

### combat-screen

- Match: both show a red-bordered combat presentation panel over a dark game context.
- Divergence: prompt body uses generic combat sections; actual XNA content includes specific attacker/defender columns and status text.

### connection-screen

- Match: both show a framed command-deck connection form.
- Divergence: actual form is wider and left-aligned with a right connection-actions column; prompt baseline currently uses a centered form panel.

### context-menu-manager

- Match: both show a context menu anchored near a map selection.
- Divergence: actual context menu is wider and begins farther left; prompt rows and item labels are simplified.

### continent-zoom-window

- Match: both show a gameplay map with a planet zoom window and circular planet surface.
- Divergence: prompt planet surface uses simplified quadrant lines; actual surface uses colored continent regions and a tighter window layout.

### create-lobby

- Match: both show a command-deck form for creating a lobby.
- Divergence: actual layout contains specific map selection controls and action buttons; prompt uses a generic wide setup form.

### debug-info-window

- Match: both show a gameplay map with Debug Information window.
- Divergence: actual debug sections and scroll bar density are more detailed than the prompt body placeholder.

### dialog-manager

- Match: both show a centered modal dialog with action buttons.
- Divergence: actual dialog button colors, size, and backdrop intensity differ from prompt baseline.

### encyclopedia-window

- Match: both show a gameplay map with an Encyclopedia window.
- Divergence: actual has a split navigation/article layout; prompt only sketches the dockable window shell and generic body.

### game-mode-selector

- Match: both show command-deck framing and a central selection panel.
- Divergence: actual mode card positions and widths differ; prompt uses a simpler centered panel.

### gameplay-hud-legend

- Match: both show gameplay shell and right-side Map Key/legend area.
- Divergence: prompt map-system positions are approximate and the legend rows are not detailed.

### gameplay-hud-top-bar

- Match: both show full-width top bar, resource chips, shortcut hints, side rails, and map below.
- Divergence: prompt resource and shortcut text geometry is approximate; actual top-bar content has exact Myra text wrapping and spacing.

### legacy-player-dashboard

- Match: both show a gameplay map with a narrow dashboard window.
- Divergence: prompt dashboard body is a placeholder and does not match the actual legacy XNA control list.

### lobby-browser

- Match: both show command-deck lobby browsing.
- Divergence: actual lobby table spans nearly the whole inner frame; prompt uses a narrower generic wide panel.

### main-menu

- Match: both show starfield command deck, title, left briefing, and right command action stack.
- Divergence: prompt text wrapping and button vertical spacing are close but not exact; footer/build details are simplified.

### main-menu-connecting

- Match: both show the command-deck frame with a centered connection/uplink panel.
- Divergence: prompt panel text is generic and less wide than actual.

### main-menu-settings

- Match: both show command-deck settings inside a framed viewport.
- Divergence: actual settings content is left-biased with large input groups; prompt uses a centered settings panel.

### multiplayer-lobby

- Match: both show multiplayer lobby in a command-deck frame.
- Divergence: actual slot table and right-side ready controls are more detailed and positioned lower than the prompt.

### player-dashboard-window

- Match: both show gameplay map plus Player Dashboard window.
- Divergence: actual dashboard is taller and more content-dense; prompt footer/buttons do not represent all dashboard controls.

### server-status-indicator

- Match: both show gameplay HUD shell and left rail focus.
- Divergence: actual server status indicator is only one small element in the HUD; prompt highlights the left rail region more broadly.

### settings-window

- Match: both show gameplay map with Settings window.
- Divergence: actual tabs and body controls are more detailed; prompt body is simplified and slightly smaller.

### side-panel-container

- Match: both show gameplay shell with side rail emphasis.
- Divergence: actual right map-key panel content and side-panel controls are more detailed than prompt outlines.

### single-player-lobby

- Match: both show a command-deck single-player setup screen.
- Divergence: actual screen has a multi-column setup header and large AI lineup table; prompt uses a generic setup panel and should be refined next.

### tutorial-mode-window

- Match: both place Tutorial Mode as a large docked window on the left side of the map content.
- Divergence: actual window is shorter/narrower and has detailed section borders, scroll bar, and footer buttons; prompt body content is simplified.

### ui-scale-window

- Match: both show gameplay map with a compact UI Scale window.
- Divergence: actual slider/body controls are smaller and more tightly packed than prompt controls.

## Required Follow-Up

The next improvement should refine the prompt catalog screen by screen, prioritizing the largest current geometry gaps:

- `single-player-lobby`
- `connection-screen`
- `main-menu-settings`
- `lobby-browser`
- `continent-zoom-window`
- `tutorial-mode-window`
- `combat-event-dialog`
