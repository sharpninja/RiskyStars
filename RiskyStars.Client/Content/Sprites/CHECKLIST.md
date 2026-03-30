# Sprite Creation Checklist

Use this checklist when creating or replacing sprite assets.

## Pre-Creation

- [ ] Review sprite specifications in `SPRITES.md` or `README.md`
- [ ] Confirm target dimensions for the sprite
- [ ] Check if transparency is needed (most sprites require it)
- [ ] Review existing sprites for art style consistency

## Creation

### Technical Requirements

- [ ] Format: PNG
- [ ] Color space: sRGB
- [ ] Bit depth: 32-bit RGBA (8 bits per channel)
- [ ] Background: Transparent (alpha channel) or Magenta (255, 0, 255)
- [ ] Size: Matches specification (see table below)

### Dimensions Reference

| Category | Sprite | Size |
|----------|--------|------|
| Stellar Bodies | Gas Giants | 64x64 |
| Stellar Bodies | Rocky Planets | 48x48 |
| Stellar Bodies | Planetoids | 24x24 |
| Stellar Bodies | Comets | 32x32 |
| Armies | Units/Heroes | 32x32 |
| UI | Buttons | 120x40 |
| UI | Panel | 200x150 |
| UI | Icons | 32x32 |
| Hyperspace | Lane | 32x8 |
| Hyperspace | Lane Mouth | 32x32 |
| Combat | Effects | 32x32 or 48x48 |

### Quality Checks

- [ ] Image is crisp and clear at 100% zoom
- [ ] Transparency works correctly (no unwanted artifacts)
- [ ] Colors are vibrant and appropriate for sci-fi space theme
- [ ] Sprite is recognizable at its rendered size
- [ ] No copyrighted or AI-generated content without proper rights

## Integration

### File Management

- [ ] File saved with exact name from specification (case-sensitive)
- [ ] File placed in correct subdirectory under `Content/Sprites/`
- [ ] File size is reasonable (< 100KB for most sprites)
- [ ] Original source file saved separately (PSD, Aseprite, etc.)

### Testing

- [ ] Build the project: `dotnet build RiskyStars.Client`
- [ ] Verify no Content Pipeline errors
- [ ] Run the game and verify sprite appears correctly
- [ ] Check sprite at different zoom levels
- [ ] Verify transparency/blending works as expected

## Post-Integration

### Documentation

- [ ] Update sprite documentation if adding new sprites
- [ ] Note any special considerations (e.g., animation frames)
- [ ] Document source file location for future edits

### Version Control

- [ ] Commit PNG file to repository
- [ ] Include meaningful commit message (e.g., "Add gas giant sprite variant")
- [ ] DO NOT commit source files (PSD, etc.) to main repo unless small

## Sprite-Specific Notes

### Stellar Bodies
- Should look spherical with appropriate planet features
- Consider adding highlights/shadows for depth
- Variants should be visually distinct but similar style

### Army Units
- Should be iconic and easily recognizable at 32x32
- Consider silhouette clarity
- Heroes should be visually distinct from regular armies

### UI Elements
- Buttons need three states (normal, hover, pressed)
- Panel should tile well if used as repeating background
- Icons should be simple and clear at small size

### Combat Effects
- Should be eye-catching but not distracting
- Consider animation frame potential
- Explosion should convey impact

## Common Issues

### Sprite appears blurry
- Ensure PNG is saved without compression artifacts
- Check SamplerState in rendering code (use PointClamp for pixel-perfect)
- Verify sprite dimensions match specification

### Transparency not working
- Ensure PNG has alpha channel or uses magenta color key
- Check Content Pipeline settings (ColorKeyEnabled=True)
- Verify BlendState.AlphaBlend is used in rendering

### Sprite too large/small in game
- Check rendered scale in code
- Verify sprite dimensions match specification
- Consider creating sprite at higher resolution if needed

### Colors look wrong
- Ensure sRGB color space
- Check for premultiplied alpha issues
- Verify no color profile conflicts

## Resources

### Recommended Tools
- **Aseprite**: Pixel art and animation ($19.99)
- **GIMP**: Free, full-featured image editor
- **Krita**: Free painting program
- **Paint.NET**: Simple Windows image editor
- **Photoshop**: Industry standard (subscription)

### Useful Links
- MonoGame Content Pipeline: https://docs.monogame.net/articles/content_pipeline/
- PNG Specification: https://www.w3.org/TR/PNG/
- Color Key Transparency: Magenta RGB(255, 0, 255)

## Quick Reference Commands

```bash
# Generate placeholder sprites (first time)
cd RiskyStars.Client/Tools
dotnet run --project CreatePlaceholders.csproj

# Build client with content
dotnet build RiskyStars.Client

# Run client
dotnet run --project RiskyStars.Client

# Clean and rebuild (if sprite not updating)
dotnet clean RiskyStars.Client
dotnet build RiskyStars.Client
```
