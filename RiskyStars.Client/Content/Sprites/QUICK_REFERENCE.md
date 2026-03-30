# Sprite System Quick Reference

## Setup (First Time Only)

```bash
cd RiskyStars.Client/Content/Sprites
./Initialize-Sprites.bat  # Windows
# or
pwsh Initialize-Sprites.ps1  # Linux/macOS
```

## Loading Sprites

```csharp
// In LoadContent()
var spriteManager = new SpriteManager(Content);
spriteManager.LoadAllSprites();
```

## Getting Sprites

```csharp
// Stellar bodies
var planet = spriteManager.GetStellarBodyTexture(StellarBodyType.GasGiant, variant: 1);

// Armies
var army = spriteManager.GetArmyTexture(isHero: false);
var hero = spriteManager.GetArmyTexture(isHero: true);

// UI
var button = spriteManager.GetButtonTexture(ButtonState.Hover);
var panel = spriteManager.GetUITexture(UIElement.Panel);

// Hyperspace
var lane = spriteManager.GetHyperspaceLaneTexture();
var mouth = spriteManager.GetHyperspaceLaneMouthTexture();

// Combat
var hit = spriteManager.GetCombatEffectTexture(CombatEffect.Hit);
```

## Sprite Specifications

| Category | File | Size |
|----------|------|------|
| Gas Giants | GasGiant.png (3 variants) | 64x64 |
| Rocky Planets | RockyPlanet.png (3 variants) | 48x48 |
| Planetoids | Planetoid.png | 24x24 |
| Comets | Comet.png | 32x32 |
| Armies | Army.png, Hero.png | 32x32 |
| Buttons | ButtonNormal/Hover/Pressed.png | 120x40 |
| Panels | Panel.png | 200x150 |
| Icons | IconProduction/Energy.png | 32x32 |
| Lanes | Lane.png | 32x8 |
| Lane Mouths | LaneMouth.png | 32x32 |
| Combat | Hit/Miss/Explosion/DiceRoll.png | 32-48px |

## Adding New Sprites

1. Create PNG file (transparency via alpha or magenta color key)
2. Place in appropriate subdirectory
3. Add to `Content/Content.mgcb`:
   ```
   #begin Sprites/Category/FileName.png
   /importer:TextureImporter
   /processor:TextureProcessor
   /processorParam:ColorKeyColor=255,0,255,255
   /processorParam:ColorKeyEnabled=True
   /processorParam:GenerateMipmaps=False
   /processorParam:PremultiplyAlpha=True
   /processorParam:ResizeToPowerOfTwo=False
   /processorParam:MakeSquare=False
   /processorParam:TextureFormat=Color
   /build:Sprites/Category/FileName.png
   ```
4. Rebuild: `dotnet build RiskyStars.Client`

## Common Commands

```bash
# Build with content
dotnet build RiskyStars.Client

# Clean rebuild
dotnet clean RiskyStars.Client
dotnet build RiskyStars.Client

# Run game
dotnet run --project RiskyStars.Client

# Generate placeholders
cd RiskyStars.Client/Tools
dotnet run --project CreatePlaceholders.csproj
```

## Troubleshooting

| Problem | Solution |
|---------|----------|
| "Content file not found" | Run placeholder generator |
| Sprite not updating | Clean and rebuild |
| Build error | Verify file exists and is valid PNG |
| Transparency not working | Check alpha channel or use magenta (255,0,255) |

## File Locations

- **Sprites:** `RiskyStars.Client/Content/Sprites/`
- **SpriteManager:** `RiskyStars.Client/SpriteManager.cs`
- **Content Pipeline:** `RiskyStars.Client/Content/Content.mgcb`
- **Documentation:** `RiskyStars.Client/SPRITES.md`
- **Tools:** `RiskyStars.Client/Tools/`

## Documentation

- **Full Documentation:** `SPRITES.md`
- **Setup Guide:** `Content/Sprites/SETUP.md`
- **Creation Checklist:** `Content/Sprites/CHECKLIST.md`
- **Implementation Details:** `Content/Sprites/IMPLEMENTATION_SUMMARY.md`
