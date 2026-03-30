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
  - `MapData.cs` - Map data structures
  - `MapLoader.cs` - Sample map creation
  - `Content/` - Game assets directory
  - `RENDERING.md` - Rendering system documentation
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
- **F1**: Toggle debug info

See `RiskyStars.Client/RENDERING.md` for detailed documentation.

## Conventions
### Code
- C# projects follow standard .NET conventions
- Proto files use proto3 syntax
- gRPC services defined in `RiskyStars.Shared/Protos/`
- Service implementations in `RiskyStars.Server/Services/`
- MonoGame content in `RiskyStars.Client/Content/`

### Documentation
- Documentation files use Markdown format with `.md` extension
- Numbering system for organization (e.g., `0.0.0_Game_Concept.md`, `1.0.00_Gameplay.md`)
- Each directory contains a `.gitkeep` file to preserve empty directories
- Best viewed with [NotesHub](https://www.noteshub.app)

## Viewing Documentation
Access via GitHub Pages or view locally with any Markdown reader.
