# AGENTS.md - RiskyStars Repository Guide

## Setup
```bash
# Clone the repository
git clone <repository-url>
cd RiskyStars

# Restore packages
dotnet restore RiskyStars.sln
```

## Tech Stack
- **.NET 8.0+** - Core framework
- **ASP.NET Core** - gRPC server
- **gRPC** - Client-server communication
- **MonoGame 3.8.4.1** - Game engine (DesktopGL)
- **Protocol Buffers** - Message serialization
- **Jekyll** - Static site generator for documentation (GitHub Pages theme: jekyll-theme-hacker)
- **Markdown** - Documentation format

## Repository Structure
- `RiskyStars.sln` - Visual Studio solution file
- `RiskyStars.Shared/` - Shared library with proto definitions and generated code
  - `Protos/game.proto` - gRPC service definitions
- `RiskyStars.Server/` - ASP.NET Core gRPC service
  - `Services/GameServiceImpl.cs` - Service implementation
  - `Program.cs` - Server configuration
- `RiskyStars.Client/` - MonoGame game client
  - `RiskyStarsGame.cs` - Main game class
  - `Camera2D.cs` - 2D camera with pan/zoom controls
  - `MapRenderer.cs` - Renders star systems and stellar bodies
  - `RegionRenderer.cs` - Renders ownership and armies
  - `UIRenderer.cs` - Renders HUD and resource displays
  - `SpriteManager.cs` - Sprite asset management
  - `MapData.cs` - Map data structures
  - `MapLoader.cs` - Sample map creation
  - `Content/` - Game assets directory
    - `Sprites/` - PNG sprite assets (placeholders)
  - `Tools/` - Development utilities
    - `CreatePlaceholders.csproj` - Sprite placeholder generator
  - `RENDERING.md` - Rendering system documentation
  - `SPRITES.md` - Sprite asset documentation
- `0.0_Concept/` - Game concept documentation
- `1.0_Rules/` - Gameplay and combat rules
- `2.0_Design/` - Design documentation
- `_config.yml` - Jekyll configuration for GitHub Pages

## Build Commands
```bash
# Build entire solution
dotnet build RiskyStars.sln

# Build specific projects
dotnet build RiskyStars.Shared/RiskyStars.Shared.csproj
dotnet build RiskyStars.Server/RiskyStars.Server.csproj
dotnet build RiskyStars.Client/RiskyStars.Client.csproj

# Generate sprite placeholders (first-time setup)
cd RiskyStars.Client/Tools
dotnet run --project CreatePlaceholders.csproj
```

## Run Commands
```bash
# Run the server (default port 5000)
dotnet run --project RiskyStars.Server

# Run the client (in a separate terminal)
dotnet run --project RiskyStars.Client
```

## Test Commands
No tests currently configured.

## Lint Commands
No linter currently configured.

## Rendering System

The MonoGame client uses a three-renderer architecture:
- **MapRenderer**: Displays static map elements (star systems, stellar bodies, hyperspace lanes)
- **RegionRenderer**: Displays dynamic game state (ownership, armies)
- **UIRenderer**: Displays HUD elements (resources, turn info, player list)

### Camera Controls
- **WASD/Arrow Keys**: Pan camera
- **Shift + Movement**: Fast pan
- **Mouse Wheel**: Zoom
- **Middle Mouse**: Pan by dragging

### Window Management
- **F1**: Toggle Debug Info Window
- **F2**: Toggle Player Dashboard Window
- **F3**: Toggle AI Visualization Window
- All windows are resizable and dockable
- Window positions and sizes are saved automatically

See `RiskyStars.Client/RENDERING.md` for detailed documentation.
See `RiskyStars.Client/DOCKABLE_WINDOWS.md` for window system documentation.

## Sprite Assets

The client uses sprite-based graphics managed through MonoGame's Content Pipeline:

- **Stellar Bodies**: Gas giants (3 variants), rocky planets (3 variants), planetoids, comets
- **Armies**: Generic army units and hero units
- **UI Elements**: Buttons (3 states), panels, resource icons
- **Hyperspace Lanes**: Lane textures and lane mouth portals
- **Combat**: Hit effects, miss indicators, explosions, dice rolls

### First-Time Setup

Generate placeholder sprites before building:
```bash
cd RiskyStars.Client/Content/Sprites
# Windows: Initialize-Sprites.bat
# Linux/macOS: pwsh Initialize-Sprites.ps1
# Or manually:
cd ../../Tools
dotnet run --project CreatePlaceholders.csproj
```

See `RiskyStars.Client/SPRITES.md` for detailed sprite specifications.

## UI Theme System

The client uses a Myra-based theme system for consistent UI styling:

- **UITheme.xml** - XML stylesheet defining colors, fonts, borders, and spacing for all widgets
- **ThemeManager.cs** - Centralized theme constants and helper methods
- **ThemedUIFactory.cs** - Factory for creating pre-styled UI widgets
- **UI_THEME.md** - Complete documentation of the theme system

### Usage Examples
```csharp
// Use ThemeManager constants
ThemeManager.Colors.AccentCyan
ThemeManager.Spacing.Medium
ThemeManager.FontScale.Title

// Create themed widgets
var button = ThemedUIFactory.CreateButton("OK", ButtonTheme.Primary);
var panel = ThemedUIFactory.CreateResourcePanel();
var label = ThemedUIFactory.CreateTitleLabel("Game Title");

// Apply themes to existing widgets
ThemeManager.ApplyButtonTheme(button, ButtonTheme.Success);
```

See `RiskyStars.Client/UI_THEME.md` for complete documentation.

## Dockable Window System

The client uses resizable and dockable UI panels for game information:

- **WindowPreferences.cs** - User preference persistence for window states
- **DockableWindow.cs** - Base class for all dockable windows
- **PlayerDashboardWindow.cs** - Resource management and army purchasing
- **AIVisualizationWindow.cs** - AI action tracking and visualization controls
- **DebugInfoWindow.cs** - Performance metrics and debug information
- **DOCKABLE_WINDOWS.md** - Complete documentation of the window system

### Features
- Resizable windows with drag handles
- Dockable to screen edges and corners
- Automatic state persistence (position, size, visibility)
- Keyboard shortcuts (F1-F3) for quick access
- Themed styling using ThemeManager
- Integration with AI action tracking

## Dialog System

The client uses Myra's Dialog system for modal notifications and user prompts:

- **DialogManager.cs** - Centralized dialog system for errors, warnings, confirmations, and questions
- **CombatEventDialog.cs** - Specialized dialog for combat event notifications
- **DIALOG_SYSTEM.md** - Complete documentation of the dialog system

### Features
- Error, warning, info, and success dialogs with themed styling
- Confirmation and question dialogs with callbacks
- Combat event notifications with formatted battle information
- Replaces custom modal overlays with consistent Myra dialogs

### Usage Examples
```csharp
// Initialize
_desktop = new Desktop();
_dialogManager = new DialogManager(_desktop);

// Show error dialog
_dialogManager.ShowError("Error", "Something went wrong");

// Show confirmation with callback
_dialogManager.ShowConfirmation("Delete?", "Are you sure?", (result) =>
{
    if (result == DialogResult.OK)
    {
        // User confirmed
    }
});

// Show combat event
_combatEventDialog.ShowCombatInitiated(combatEvent, () =>
{
    _combatScreen.StartCombat(combatEvent);
});
```

See `RiskyStars.Client/DIALOG_SYSTEM.md` for complete documentation.

## Input Validation System

The client implements comprehensive input validation with visual error feedback:

- **InputValidator.cs** - Static validation methods for all input types
- **ValidatedTextBox.cs** - Myra TextBox wrapper with validation and error display
- **ValidatedTextInputField.cs** - Custom TextInputField wrapper with validation
- **INPUT_VALIDATION.md** - Complete documentation of the validation system

### Features
- Real-time validation on text input
- Visual error indicators (red borders, error messages)
- Myra tooltip error feedback on hover
- Optional inline error labels
- Pre-configured validators for common fields (player name, server address, map name)

### Usage Examples
```csharp
// Create validated text box for player name
var playerNameBox = ThemedUIFactory.CreateValidatedPlayerNameBox();
playerNameBox.Text = "Player";
grid.Widgets.Add(playerNameBox.Container);

// Validate before submitting
if (!playerNameBox.IsValid)
{
    _dialogManager.ShowError("Validation Error", playerNameBox.ErrorMessage);
    return;
}

// Custom validation
var customBox = new ValidatedTextBox(400, "Enter value", showErrorLabel: true);
customBox.SetValidator(text => 
{
    if (text.Length < 5)
        return new ValidationResult(false, "Must be at least 5 characters");
    return new ValidationResult(true, "Valid");
});
```

See `RiskyStars.Client/INPUT_VALIDATION.md` for complete documentation.

## Conventions
### Code
- C# projects follow standard .NET conventions
- Proto files use proto3 syntax
- gRPC services defined in `RiskyStars.Shared/Protos/`
- Service implementations in `RiskyStars.Server/Services/`
- MonoGame content in `RiskyStars.Client/Content/`
- Sprite assets in `RiskyStars.Client/Content/Sprites/`
- **UI styling uses ThemeManager constants and ThemedUIFactory - no hardcoded colors/spacing**
- **Input validation uses InputValidator and ValidatedTextBox/ValidatedTextInputField - validate all user inputs**
- **Dockable windows extend DockableWindow base class and use WindowPreferences for persistence**

### Documentation
- Documentation files use Markdown format with `.md` extension
- Numbering system for organization (e.g., `0.0.0_Game_Concept.md`, `1.0.00_Gameplay.md`)
- Each directory contains a `.gitkeep` file to preserve empty directories
- Best viewed with [NotesHub](https://www.noteshub.app)

## Viewing Documentation
Access via GitHub Pages or view locally with any Markdown reader.
