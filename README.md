# RiskyStars

Best viewed with [NotesHub](https://www.noteshub.app/notebooks/github/sharpninja%2FRiskyStars)

## Solution Structure

This repository contains a .NET solution with three projects:

### RiskyStars.Shared
A class library containing:
- Protocol Buffer definitions (`Protos/game.proto`)
- Shared models and generated gRPC code
- **Packages**: Grpc.Tools, Google.Protobuf, Grpc.Net.Client

### RiskyStars.Server
An ASP.NET Core gRPC service:
- Game server implementation
- gRPC service endpoints
- **Packages**: Grpc.AspNetCore
- **References**: RiskyStars.Shared

### RiskyStars.Client
A MonoGame-based game client:
- Game rendering and logic
- gRPC client for server communication
- **Packages**: MonoGame.Framework.DesktopGL, Grpc.Net.Client
- **References**: RiskyStars.Shared

## Building and Running

```bash
# Build the entire solution
dotnet build RiskyStars.sln

# Run the server
dotnet run --project RiskyStars.Server

# Run the client (in a separate terminal)
dotnet run --project RiskyStars.Client
```

## Documentation

### [0.0 Concept](0.0_Concept/0.0.0_Game_Concept.md)
### [1.0 Rules - Gameplay](1.0_Rules/1.0.00_Gameplay.md)
### [1.1 Rules - Combat](1.0_Rules/1.1.00_Combat.md)
### [2.0 Design](0.0_Design)
