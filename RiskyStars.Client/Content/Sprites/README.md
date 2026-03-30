# Sprite Placeholders

This directory contains placeholder sprite assets for the RiskyStars MonoGame client.

## Quick Start

**First time setup** - Generate placeholder PNG files:
```bash
# From this directory:
Initialize-Sprites.bat          # Windows
pwsh Initialize-Sprites.ps1     # Linux/macOS

# Or manually:
cd ../../Tools
dotnet run --project CreatePlaceholders.csproj
```

This creates minimal 1x1 PNG files that allow the game to build and run. Replace them with proper artwork as needed.

## Directory Structure

```
Sprites/
├── StellarBodies/      # Planet and celestial body sprites
│   ├── GasGiant.png
│   ├── GasGiant_Variant1.png
│   ├── GasGiant_Variant2.png
│   ├── RockyPlanet.png
│   ├── RockyPlanet_Variant1.png
│   ├── RockyPlanet_Variant2.png
│   ├── Planetoid.png
│   └── Comet.png
├── Armies/             # Army and hero unit sprites
│   ├── Army.png
│   └── Hero.png
├── UI/                 # User interface elements
│   ├── ButtonNormal.png
│   ├── ButtonHover.png
│   ├── ButtonPressed.png
│   ├── Panel.png
│   ├── IconProduction.png
│   └── IconEnergy.png
├── HyperspaceLanes/    # Hyperspace lane graphics
│   ├── Lane.png
│   └── LaneMouth.png
└── Combat/             # Combat animation effects
    ├── Hit.png
    ├── Miss.png
    ├── Explosion.png
    └── DiceRoll.png
```

## Generating Placeholders

### Option 1: Using the C# Generator Tool

```bash
cd RiskyStars.Client/Tools
dotnet run --project CreatePlaceholders.csproj
```

This will create minimal valid PNG files for all required sprites.

### Option 2: Manual Creation

You can create your own placeholder images using any image editor. Recommended dimensions:

- **Gas Giants**: 64x64 pixels
- **Rocky Planets**: 48x48 pixels
- **Planetoids**: 24x24 pixels
- **Comets**: 32x32 pixels
- **Army/Hero**: 32x32 pixels
- **UI Buttons**: 120x40 pixels
- **UI Panel**: 200x150 pixels
- **UI Icons**: 32x32 pixels
- **Hyperspace Lane**: 32x8 pixels (tileable)
- **Lane Mouth**: 32x32 pixels
- **Combat Effects**: 32x32 to 48x48 pixels

### Option 3: Using System.Drawing Generator

For higher quality placeholders, you can use the `GenerateSpritePlaceholders` tool:

```bash
cd RiskyStars.Client/Tools
dotnet run --project GenerateSpritePlaceholders.csproj
```

This requires `System.Drawing.Common` and generates colored placeholder sprites with basic shapes.

## Sprite Descriptions

### Stellar Bodies

- **GasGiant.png**: Large orange/brown planet with bands (64x64)
- **GasGiant_Variant1.png**: Yellow gas giant variant (64x64)
- **GasGiant_Variant2.png**: Reddish gas giant variant (64x64)
- **RockyPlanet.png**: Earth-like blue/green planet (48x48)
- **RockyPlanet_Variant1.png**: Desert/arid planet (48x48)
- **RockyPlanet_Variant2.png**: Ice world (48x48)
- **Planetoid.png**: Small gray rocky body with craters (24x24)
- **Comet.png**: Small body with glowing tail (32x32)

### Armies

- **Army.png**: Generic military unit icon (shield shape) (32x32)
- **Hero.png**: Special hero unit with crown/star emblem (32x32)

### UI Elements

- **ButtonNormal.png**: Default button state (120x40)
- **ButtonHover.png**: Button hover state (120x40)
- **ButtonPressed.png**: Button pressed state (120x40)
- **Panel.png**: Background panel for UI sections (200x150)
- **IconProduction.png**: Production resource icon (32x32)
- **IconEnergy.png**: Energy resource icon (32x32)

### Hyperspace Lanes

- **Lane.png**: Tileable texture for hyperspace connections (32x8)
- **LaneMouth.png**: Portal/gate graphic for lane endpoints (32x32)

### Combat

- **Hit.png**: Impact/hit effect animation frame (32x32)
- **Miss.png**: Miss indicator (X or slash mark) (32x32)
- **Explosion.png**: Explosion burst effect (48x48)
- **DiceRoll.png**: Dice face for combat resolution (48x48)

## Content Pipeline Integration

All sprites are registered in `Content.mgcb` with the following settings:

- **Importer**: TextureImporter
- **Processor**: TextureProcessor
- **Color Key**: Magenta (255, 0, 255) for transparency
- **Premultiply Alpha**: True
- **Texture Format**: Color

The sprites are loaded in the game using MonoGame's Content.Load<Texture2D>() method.

## Replacing Placeholders

To replace these placeholders with final artwork:

1. Create your sprite at the recommended dimensions
2. Save as PNG with transparency
3. Use magenta (RGB: 255, 0, 255) for areas that should be transparent if not using alpha channel
4. Replace the placeholder file with the same filename
5. Rebuild the content pipeline (the build system will detect changes automatically)

## Notes

- All placeholder images are simple geometric shapes or single-pixel PNGs
- They are meant to be replaced with proper artwork during game development
- The Content Pipeline will automatically process these files during build
- Transparent backgrounds are recommended for all sprites except UI panels
