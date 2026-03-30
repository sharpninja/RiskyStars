# Sprite Asset System

This document describes the sprite asset system for the RiskyStars MonoGame client.

## Overview

The game uses MonoGame's Content Pipeline to manage sprite assets. All sprites are PNG files located in `Content/Sprites/` and are processed during build time.

## Directory Structure

```
Content/Sprites/
├── StellarBodies/      # Celestial body sprites
├── Armies/             # Unit sprites
├── UI/                 # User interface elements
├── HyperspaceLanes/    # Connection graphics
└── Combat/             # Combat effects and animations
```

## Setup

### Initial Setup

To generate placeholder sprites for development:

**Windows (Command Prompt):**
```cmd
cd RiskyStars.Client\Content\Sprites
Initialize-Sprites.bat
```

**Windows (PowerShell) / Linux / macOS:**
```bash
cd RiskyStars.Client/Content/Sprites
pwsh Initialize-Sprites.ps1
```

**Manual (any platform):**
```bash
cd RiskyStars.Client/Tools
dotnet run --project CreatePlaceholders.csproj
```

This will create minimal 1x1 PNG files for all required sprites. These placeholders allow the game to build and run while you develop or replace them with final artwork.

## Sprite Specifications

### Stellar Bodies

| Sprite | Size | Description |
|--------|------|-------------|
| GasGiant.png | 64x64 | Large gaseous planet (orange/brown) |
| GasGiant_Variant1.png | 64x64 | Yellow gas giant |
| GasGiant_Variant2.png | 64x64 | Reddish gas giant |
| RockyPlanet.png | 48x48 | Earth-like planet (blue/green) |
| RockyPlanet_Variant1.png | 48x48 | Desert/arid world |
| RockyPlanet_Variant2.png | 48x48 | Ice world |
| Planetoid.png | 24x24 | Small rocky body |
| Comet.png | 32x32 | Comet with tail |

### Armies

| Sprite | Size | Description |
|--------|------|-------------|
| Army.png | 32x32 | Generic military unit |
| Hero.png | 32x32 | Hero/special unit |

### UI Elements

| Sprite | Size | Description |
|--------|------|-------------|
| ButtonNormal.png | 120x40 | Button default state |
| ButtonHover.png | 120x40 | Button hover state |
| ButtonPressed.png | 120x40 | Button pressed state |
| Panel.png | 200x150 | UI panel background |
| IconProduction.png | 32x32 | Production resource icon |
| IconEnergy.png | 32x32 | Energy resource icon |

### Hyperspace Lanes

| Sprite | Size | Description |
|--------|------|-------------|
| Lane.png | 32x8 | Tileable lane texture |
| LaneMouth.png | 32x32 | Portal/gate graphic |

### Combat

| Sprite | Size | Description |
|--------|------|-------------|
| Hit.png | 32x32 | Impact effect |
| Miss.png | 32x32 | Miss indicator |
| Explosion.png | 48x48 | Explosion effect |
| DiceRoll.png | 48x48 | Dice face for combat |

## Content Pipeline Configuration

All sprites are configured in `Content/Content.mgcb` with these settings:

- **Importer:** TextureImporter
- **Processor:** TextureProcessor
- **Color Key:** Enabled (Magenta: 255, 0, 255)
- **Premultiply Alpha:** True
- **Texture Format:** Color
- **Generate Mipmaps:** False

## Loading Sprites in Code

Sprites are loaded using MonoGame's ContentManager:

```csharp
// In LoadContent method
Texture2D gasGiant = Content.Load<Texture2D>("Sprites/StellarBodies/GasGiant");
Texture2D armyIcon = Content.Load<Texture2D>("Sprites/Armies/Army");
```

## Creating Custom Sprites

### Requirements

1. **Format:** PNG with transparency
2. **Color Space:** sRGB
3. **Bit Depth:** 32-bit RGBA (8 bits per channel)
4. **Transparency:** Use alpha channel or magenta color key (RGB: 255, 0, 255)

### Best Practices

1. **Use power-of-two dimensions when possible** (16, 32, 64, 128, etc.) for better performance
2. **Keep sprites crisp at their native resolution** - they may be scaled in-game
3. **Use transparency for irregular shapes** rather than rectangular backgrounds
4. **Maintain consistent art style** across all sprites in a category
5. **Test at different zoom levels** - the game camera supports zoom in/out

### Recommended Tools

- **Aseprite** - Pixel art and sprite animation
- **GIMP** - Free image editor with good PNG support
- **Krita** - Free painting program
- **Photoshop** - Professional image editor
- **Paint.NET** - Simple Windows image editor

## Replacing Placeholders

To replace placeholder sprites with final artwork:

1. Create your sprite following the specifications above
2. Save as PNG with the exact filename listed in the specification table
3. Place in the appropriate subdirectory under `Content/Sprites/`
4. Build the project - the Content Pipeline will automatically process the new sprite

Example:
```bash
# Replace gas giant sprite
# 1. Create your 64x64 PNG file
# 2. Save as: RiskyStars.Client/Content/Sprites/StellarBodies/GasGiant.png
# 3. Build
dotnet build RiskyStars.Client
```

## Advanced: High-Quality Placeholder Generation

For better quality placeholders during development, you can use the `GenerateSpritePlaceholders` tool which creates colored shapes:

```bash
cd RiskyStars.Client/Tools
dotnet run --project GenerateSpritePlaceholders.csproj
```

This requires `System.Drawing.Common` package and generates:
- Colored circular planets with bands/features
- Shield-shaped army icons
- Gradient buttons
- Styled UI elements
- Effect graphics

## Troubleshooting

### "Content file not found" error

1. Verify the PNG file exists in the correct location
2. Check that the file is added to `Content.mgcb`
3. Rebuild the content: `dotnet build RiskyStars.Client`

### Sprites appear pixelated or blurry

1. Check the SamplerState used when drawing:
   - Use `SamplerState.PointClamp` for pixel-perfect rendering
   - Use `SamplerState.LinearClamp` for smooth scaling
2. Ensure sprites are drawn at integer positions for pixel-perfect rendering

### Transparency not working

1. Verify PNG has alpha channel or uses magenta (255, 0, 255) for transparency
2. Check Content Pipeline settings have ColorKeyEnabled=True
3. Ensure SpriteBatch uses `BlendState.AlphaBlend`

### Build errors with Content Pipeline

1. Verify all PNG files referenced in `Content.mgcb` exist
2. Check for invalid PNG files (corrupted or wrong format)
3. Clean and rebuild: `dotnet clean && dotnet build`

## Performance Considerations

- **Texture Atlas:** Consider combining small sprites into texture atlases for better performance
- **Mipmaps:** Generally disabled for 2D sprites to avoid blurriness
- **Compression:** Consider DXT compression for large textures on desktop platforms
- **Batch Rendering:** Group sprites by texture to minimize texture switches

## Future Enhancements

Planned improvements to the sprite system:

1. **Sprite Animation Support** - Frame-based animations for combat effects
2. **Texture Atlas Generation** - Automatic packing of sprites
3. **Color Tinting** - Runtime color modification for player colors
4. **Procedural Generation** - Algorithmic planet textures with variations
5. **HD Sprite Support** - Multiple resolution variants for different screen sizes
