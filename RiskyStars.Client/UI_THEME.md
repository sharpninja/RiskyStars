# UI Theme System

## Overview

RiskyStars uses a comprehensive Myra-based theme system that provides consistent colors, fonts, borders, and spacing across all UI widgets. The theme system eliminates hardcoded visual constants and makes it easy to maintain a cohesive visual style throughout the game.

## Architecture

The theme system consists of three main components:

1. **UITheme.xml** - XML stylesheet defining visual properties for all Myra widgets
2. **ThemeManager.cs** - Centralized manager for theme constants and helper methods
3. **ThemedUIFactory.cs** - Factory class for creating pre-styled UI widgets

## Using the Theme System

### ThemeManager

The `ThemeManager` class provides static access to all theme constants:

```csharp
// Colors
ThemeManager.Colors.PrimaryDark
ThemeManager.Colors.AccentCyan
ThemeManager.Colors.TextPrimary
ThemeManager.Colors.BorderNormal

// Spacing
ThemeManager.Spacing.Small  // 8px
ThemeManager.Spacing.Medium // 12px
ThemeManager.Spacing.Large  // 15px

// Border Thickness
ThemeManager.BorderThickness.Normal // 2px

// Padding
ThemeManager.Padding.Small
ThemeManager.Padding.Medium
ThemeManager.Padding.Button

// Font Scales
ThemeManager.FontScale.Small
ThemeManager.FontScale.Normal
ThemeManager.FontScale.Title

// UI Element Sizes
ThemeManager.Sizes.ButtonMediumWidth
ThemeManager.Sizes.ButtonMediumHeight
ThemeManager.Sizes.InputMediumHeight
```

### ThemedUIFactory

Create pre-styled widgets using the factory:

```csharp
// Buttons
var button = ThemedUIFactory.CreateButton("Click Me", ButtonTheme.Primary);
var smallButton = ThemedUIFactory.CreateSmallButton("OK");
var dangerButton = ThemedUIFactory.CreateButton("Delete", ButtonTheme.Danger);

// Labels
var title = ThemedUIFactory.CreateTitleLabel("Game Title");
var subtitle = ThemedUIFactory.CreateSubtitleLabel("Section Header");
var secondary = ThemedUIFactory.CreateSecondaryLabel("Additional info");

// Panels
var frame = ThemedUIFactory.CreateFramePanel();
var resourcePanel = ThemedUIFactory.CreateResourcePanel();
var heroPanel = ThemedUIFactory.CreateHeroPanel();

// Input Controls
var textBox = ThemedUIFactory.CreateTextBox("Initial text", 300);
var comboBox = ThemedUIFactory.CreateComboBox(200);
var checkBox = ThemedUIFactory.CreateCheckButton(isChecked: true);

// Layout Containers
var grid = ThemedUIFactory.CreateGrid();
var vstack = ThemedUIFactory.CreateVerticalStack();
var hstack = ThemedUIFactory.CreateHorizontalStack();

// Resource Icons
var popIcon = ThemedUIFactory.CreatePopulationIcon();
var metalIcon = ThemedUIFactory.CreateMetalIcon();
var fuelIcon = ThemedUIFactory.CreateFuelIcon();

// Badges
var easyAI = ThemedUIFactory.CreateAIEasyBadge();
var mediumAI = ThemedUIFactory.CreateAIMediumBadge();
var hardAI = ThemedUIFactory.CreateAIHardBadge();
```

### Applying Themes to Existing Widgets

```csharp
// Apply button theme
var button = new TextButton { Text = "My Button" };
ThemeManager.ApplyButtonTheme(button, ButtonTheme.Primary);

// Apply panel theme
var panel = new Panel();
ThemeManager.ApplyPanelTheme(panel, PanelTheme.AccentFrame);

// Apply label theme
var label = new Label { Text = "My Label" };
ThemeManager.ApplyLabelTheme(label, LabelTheme.Title);
```

## Color Palette

### Primary Colors
- **PrimaryDark** (#0A0A14) - Deep background color
- **PrimaryMedium** (#1E1E28) - Medium background color
- **PrimaryLight** (#28283C) - Light background color

### Accent Colors
- **AccentCyan** (#64B4FF) - Primary accent, used for highlights and focus
- **AccentBlue** (#3C64B4) - Button backgrounds
- **AccentDarkBlue** (#1E3C64) - Button pressed state

### UI State Colors
- **HoverColor** (#32648C) - Hover state
- **PressedColor** (#1E3C5A) - Pressed state
- **DisabledColor** (#3C3C3C) - Disabled elements

### Text Colors
- **TextPrimary** (White) - Primary text
- **TextSecondary** (#C8C8C8) - Secondary text
- **TextDisabled** (Gray) - Disabled text
- **TextAccent** (Cyan) - Accent text (titles, highlights)
- **TextWarning** (Yellow) - Warnings
- **TextError** (Red) - Errors
- **TextSuccess** (LightGreen) - Success messages

### Border Colors
- **BorderNormal** (Gray) - Default borders
- **BorderFocus** (Cyan) - Focused elements
- **BorderHover** (#64B4FF) - Hovered elements

### Resource Colors
- **PopulationColor** (#64C864) - Population resource
- **MetalColor** (#B4B4B4) - Metal resource
- **FuelColor** (#DCA050) - Fuel resource

### AI/Hero Colors
- **HeroColor** (#B464DC) - Hero-related UI
- **AIEasyColor** (#64B464) - Easy AI
- **AIMediumColor** (#C8B464) - Medium AI
- **AIHardColor** (#C86464) - Hard AI

## Theme Variants

### Button Themes
- **Default** - Standard button (blue)
- **Primary** - Primary action button (bright blue)
- **Danger** - Destructive action (red)
- **Success** - Positive action (green)
- **Hero** - Hero-related actions (purple)

### Panel Themes
- **Default** - Transparent panel
- **Frame** - Panel with border
- **AccentFrame** - Panel with cyan border
- **Resource** - Resource display panel (cyan border)
- **Hero** - Hero panel (purple border)
- **Dark** - Dark semi-transparent panel

### Label Themes
- **Default** - Standard white text
- **Title** - Large cyan title (1.8x scale)
- **Subtitle** - Medium cyan subtitle (1.2x scale)
- **Heading** - Section heading (1.5x scale)
- **Secondary** - Secondary text (gray)
- **Small** - Small text (0.8x scale)
- **Warning** - Warning text (yellow)
- **Error** - Error text (red)
- **Success** - Success text (green)

## Spacing System

The theme uses a consistent spacing scale:

- **XSmall**: 4px - Very tight spacing
- **Small**: 8px - Standard spacing for most layouts
- **Medium**: 12px - Comfortable spacing for button padding
- **Large**: 15px - Generous spacing for sections
- **XLarge**: 20px - Large gaps between major sections
- **XXLarge**: 30px - Extra large spacing for distinct areas

## Font Scaling

Font scales are provided as Vector2 values:

- **Tiny**: 0.6x - Very small labels (badges)
- **Small**: 0.7x - Small labels
- **SmallMedium**: 0.8x - Slightly small text
- **Normal**: 0.9x - Standard body text
- **Medium**: 1.0x - Medium text
- **Large**: 1.2x - Large text (subtitles)
- **XLarge**: 1.5x - Extra large (headings)
- **XXLarge**: 1.8x - Very large (titles)
- **Title**: 2.5x - Main menu title

## Customizing the Theme

### Modifying UITheme.xml

The XML stylesheet can be edited to change colors, spacing, and other properties:

```xml
<Colors>
  <Color Id="AccentCyan">#64B4FF</Color>
</Colors>

<TextButtonStyle Id="default">
  <Background>AccentBlue</Background>
  <BorderThickness>2</BorderThickness>
  <Padding>12, 10</Padding>
</TextButtonStyle>
```

### Adding New Color Constants

Add new colors to ThemeManager.Colors:

```csharp
public static readonly Color CustomColor = new Color(100, 150, 200);
```

### Creating Custom Button Themes

Extend ButtonTheme enum and add case to ApplyButtonTheme:

```csharp
public enum ButtonTheme
{
    Default,
    Primary,
    Custom  // New theme
}

// In ApplyButtonTheme method
case ButtonTheme.Custom:
    button.Background = CreateSolidBrush(Colors.CustomColor);
    // ... other properties
    break;
```

## Migration Guide

To migrate existing hardcoded UI to use the theme system:

1. Replace hardcoded colors with ThemeManager.Colors:
   ```csharp
   // Before
   Color.Cyan
   new Color(30, 60, 100)
   
   // After
   ThemeManager.Colors.TextAccent
   ThemeManager.Colors.AccentDarkBlue
   ```

2. Replace hardcoded spacing with ThemeManager.Spacing:
   ```csharp
   // Before
   RowSpacing = 8
   
   // After
   RowSpacing = ThemeManager.Spacing.Small
   ```

3. Use ThemedUIFactory for new widgets:
   ```csharp
   // Before
   var button = new TextButton { Text = "OK", Width = 150, Height = 45 };
   
   // After
   var button = ThemedUIFactory.CreateButton("OK");
   ```

4. Apply themes to Myra widgets:
   ```csharp
   // For existing widgets
   ThemeManager.ApplyButtonTheme(existingButton, ButtonTheme.Primary);
   ```

## Best Practices

1. **Always use ThemeManager constants** - Never hardcode colors, spacing, or sizes
2. **Use ThemedUIFactory** - For new widgets, prefer factory methods
3. **Choose appropriate themes** - Use semantic button themes (Danger for delete, Success for confirm)
4. **Consistent spacing** - Use the spacing system for all layouts
5. **Font scaling** - Use predefined scales for consistency
6. **Color semantics** - Use colors consistently (cyan for accents, red for errors, etc.)

## Examples

### Creating a Dialog

```csharp
var panel = ThemedUIFactory.CreateAccentFramePanel();
panel.Width = 400;
panel.Height = 300;

var layout = ThemedUIFactory.CreateVerticalStack();

var title = ThemedUIFactory.CreateTitleLabel("Confirm Action");
layout.Widgets.Add(title);

var message = ThemedUIFactory.CreateLabel("Are you sure?");
layout.Widgets.Add(message);

var buttonRow = ThemedUIFactory.CreateHorizontalStack();
buttonRow.Widgets.Add(ThemedUIFactory.CreateButton("Yes", ButtonTheme.Success));
buttonRow.Widgets.Add(ThemedUIFactory.CreateButton("No", ButtonTheme.Danger));
layout.Widgets.Add(buttonRow);

panel.Widgets.Add(layout);
```

### Creating a Resource Display

```csharp
var resourceRow = ThemedUIFactory.CreateHorizontalStack();
resourceRow.Widgets.Add(ThemedUIFactory.CreatePopulationIcon());
resourceRow.Widgets.Add(ThemedUIFactory.CreateLabel("Population: 1000"));

var panel = ThemedUIFactory.CreateResourcePanel();
panel.Widgets.Add(resourceRow);
```

## Legacy Custom Controls

The custom controls in `UIControls.cs` (Button, TextInputField, etc.) have been updated to use ThemeManager constants. These are gradually being replaced with Myra widgets throughout the codebase, but remain available for compatibility.

## Performance Considerations

- Theme initialization happens once at startup
- Color and constant lookups are direct property accesses (very fast)
- Factory methods create minimal overhead
- XML stylesheet is loaded once and cached

## Future Enhancements

Potential improvements to the theme system:

- Runtime theme switching (dark/light modes)
- Per-player color customization
- Animated theme transitions
- Additional pre-built widget templates
- Theme preview tool
