# Sprite Asset Setup Guide

This guide walks you through setting up sprite assets for the RiskyStars client.

## Overview

The game requires 22 PNG sprite files organized in 5 categories:
- **8** Stellar Body sprites
- **2** Army unit sprites  
- **6** UI element sprites
- **2** Hyperspace lane sprites
- **4** Combat effect sprites

## Automated Setup (Recommended)

### Step 1: Run the Placeholder Generator

The easiest way to set up sprites is to use the included placeholder generator:

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

**Or run directly:**
```bash
cd RiskyStars.Client/Tools
dotnet run --project CreatePlaceholders.csproj
```

This will create minimal 1x1 transparent PNG files for all required sprites.

### Step 2: Verify Creation

After running the generator, verify the files exist:

```bash
# Should list 8 PNG files
ls Content/Sprites/StellarBodies/

# Should list 2 PNG files
ls Content/Sprites/Armies/

# Should list 6 PNG files
ls Content/Sprites/UI/

# Should list 2 PNG files
ls Content/Sprites/HyperspaceLanes/

# Should list 4 PNG files
ls Content/Sprites/Combat/
```

### Step 3: Build the Project

```bash
cd RiskyStars.Client
dotnet build
```

The Content Pipeline will process all PNG files and include them in the game build.

## Manual Setup

If the automated generator doesn't work, you can create placeholder files manually.

### Required Files

Create the following PNG files (any size, but 1x1 is sufficient for placeholders):

**StellarBodies/** (8 files)
- GasGiant.png
- GasGiant_Variant1.png
- GasGiant_Variant2.png
- RockyPlanet.png
- RockyPlanet_Variant1.png
- RockyPlanet_Variant2.png
- Planetoid.png
- Comet.png

**Armies/** (2 files)
- Army.png
- Hero.png

**UI/** (6 files)
- ButtonNormal.png
- ButtonHover.png
- ButtonPressed.png
- Panel.png
- IconProduction.png
- IconEnergy.png

**HyperspaceLanes/** (2 files)
- Lane.png
- LaneMouth.png

**Combat/** (4 files)
- Hit.png
- Miss.png
- Explosion.png
- DiceRoll.png

### Using a Script to Create Placeholders

If you have Python 3 with PIL/Pillow installed:

```python
from PIL import Image
import os

base = "RiskyStars.Client/Content/Sprites"
files = {
    "StellarBodies": ["GasGiant.png", "GasGiant_Variant1.png", "GasGiant_Variant2.png",
                      "RockyPlanet.png", "RockyPlanet_Variant1.png", "RockyPlanet_Variant2.png",
                      "Planetoid.png", "Comet.png"],
    "Armies": ["Army.png", "Hero.png"],
    "UI": ["ButtonNormal.png", "ButtonHover.png", "ButtonPressed.png",
           "Panel.png", "IconProduction.png", "IconEnergy.png"],
    "HyperspaceLanes": ["Lane.png", "LaneMouth.png"],
    "Combat": ["Hit.png", "Miss.png", "Explosion.png", "DiceRoll.png"]
}

for dir, filelist in files.items():
    os.makedirs(f"{base}/{dir}", exist_ok=True)
    for file in filelist:
        img = Image.new('RGBA', (1, 1), (0, 0, 0, 0))
        img.save(f"{base}/{dir}/{file}")
        print(f"Created {dir}/{file}")
```

### Using ImageMagick

If you have ImageMagick installed:

```bash
cd RiskyStars.Client/Content/Sprites

# Stellar Bodies
cd StellarBodies
convert -size 1x1 xc:transparent GasGiant.png
convert -size 1x1 xc:transparent GasGiant_Variant1.png
convert -size 1x1 xc:transparent GasGiant_Variant2.png
convert -size 1x1 xc:transparent RockyPlanet.png
convert -size 1x1 xc:transparent RockyPlanet_Variant1.png
convert -size 1x1 xc:transparent RockyPlanet_Variant2.png
convert -size 1x1 xc:transparent Planetoid.png
convert -size 1x1 xc:transparent Comet.png
cd ..

# Armies
cd Armies
convert -size 1x1 xc:transparent Army.png
convert -size 1x1 xc:transparent Hero.png
cd ..

# UI
cd UI
convert -size 1x1 xc:transparent ButtonNormal.png
convert -size 1x1 xc:transparent ButtonHover.png
convert -size 1x1 xc:transparent ButtonPressed.png
convert -size 1x1 xc:transparent Panel.png
convert -size 1x1 xc:transparent IconProduction.png
convert -size 1x1 xc:transparent IconEnergy.png
cd ..

# HyperspaceLanes
cd HyperspaceLanes
convert -size 1x1 xc:transparent Lane.png
convert -size 1x1 xc:transparent LaneMouth.png
cd ..

# Combat
cd Combat
convert -size 1x1 xc:transparent Hit.png
convert -size 1x1 xc:transparent Miss.png
convert -size 1x1 xc:transparent Explosion.png
convert -size 1x1 xc:transparent DiceRoll.png
cd ..
```

## Verification

After creating the files (manually or automatically), verify everything works:

1. **Check file count:**
   ```bash
   # Should output 22
   find RiskyStars.Client/Content/Sprites -name "*.png" | wc -l
   ```

2. **Build the project:**
   ```bash
   dotnet build RiskyStars.Client
   ```
   
   Should complete without errors.

3. **Run the client:**
   ```bash
   dotnet run --project RiskyStars.Client
   ```
   
   The game should start without missing content errors.

## Next Steps

After setting up placeholders:

1. **Replace placeholders** with proper artwork (see `CHECKLIST.md`)
2. **Follow specifications** in `README.md` for dimensions and format
3. **Test in-game** to ensure sprites display correctly
4. **Rebuild** after replacing any sprite files

## Troubleshooting

### "Content file not found" error

The Content Pipeline expects files to be present. Run the placeholder generator or create files manually.

### Build errors

Check that:
- All 22 PNG files exist in the correct directories
- Files have the exact names specified (case-sensitive on Linux/macOS)
- Files are valid PNG format

### Generator fails to run

Requirements:
- .NET 9.0 SDK installed
- Run from `RiskyStars.Client/Tools` directory
- Or use alternative manual methods above

### Files created but not building

Try:
```bash
dotnet clean RiskyStars.Client
dotnet build RiskyStars.Client
```

## Support

For issues:
1. Check file existence and naming
2. Verify .NET SDK version: `dotnet --version`
3. Review build output for specific errors
4. See main documentation in `README.md` and `SPRITES.md`
