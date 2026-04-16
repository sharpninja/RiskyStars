# UI Theme System - Quick Start Guide

## What is it?

A comprehensive Myra-based theme system that provides consistent colors, fonts, borders, and spacing for all UI widgets in RiskyStars. No more hardcoded visual constants scattered throughout the code!

## Three Files You Need to Know

1. **UITheme.xml** - The stylesheet (XML) that Myra loads
2. **ThemeManager.cs** - Constants and helper methods
3. **ThemedUIFactory.cs** - Pre-built widget factory

## Quick Examples

### Use Constants Instead of Hardcoded Values

```csharp
// ❌ DON'T DO THIS
Color backgroundColor = new Color(30, 60, 100);
int spacing = 8;
int borderThickness = 2;

// ✅ DO THIS
Color backgroundColor = ThemeManager.Colors.AccentDarkBlue;
int spacing = ThemeManager.Spacing.Small;
int borderThickness = ThemeManager.BorderThickness.Normal;
```

### Create Themed Widgets

```csharp
// ❌ OLD WAY
var button = new TextButton 
{ 
    Text = "Confirm",
    Width = 150,
    Height = 45,
    Background = new SolidBrush(new Color(60, 100, 180)),
    BorderThickness = new Thickness(2),
    // ... many more properties
};

// ✅ NEW WAY
var button = ThemedUIFactory.CreateButton("Confirm", ButtonTheme.Success);
```

### Common Patterns

```csharp
// Buttons
var primaryBtn = ThemedUIFactory.CreateButton("Connect", ButtonTheme.Primary);
var cancelBtn = ThemedUIFactory.CreateButton("Cancel", ButtonTheme.Danger);
var okBtn = ThemedUIFactory.CreateSmallButton("OK");

// Labels
var title = ThemedUIFactory.CreateTitleLabel("Settings");
var subtitle = ThemedUIFactory.CreateSubtitleLabel("Display Options");
var info = ThemedUIFactory.CreateSecondaryLabel("Additional information");

// Panels
var mainPanel = ThemedUIFactory.CreateAccentFramePanel();
var resourcePanel = ThemedUIFactory.CreateResourcePanel();
var heroPanel = ThemedUIFactory.CreateHeroPanel();

// Input controls
var textBox = ThemedUIFactory.CreateTextBox("Enter name", 300);
var combo = ThemedUIFactory.CreateComboBox(200);
var checkbox = ThemedUIFactory.CreateCheckButton(true);

// Layouts
var vstack = ThemedUIFactory.CreateVerticalStack();
var hstack = ThemedUIFactory.CreateHorizontalStack();
var grid = ThemedUIFactory.CreateGrid();

// Icons
var popIcon = ThemedUIFactory.CreatePopulationIcon();
var metalIcon = ThemedUIFactory.CreateMetalIcon();
var fuelIcon = ThemedUIFactory.CreateFuelIcon();
```

## Color Reference (Most Used)

```csharp
// Backgrounds
ThemeManager.Colors.PrimaryDark        // #0A0A14 - Deep background
ThemeManager.Colors.PrimaryMedium      // #1E1E28 - Medium background
ThemeManager.Colors.PrimaryLight       // #28283C - Light background

// Accents
ThemeManager.Colors.AccentCyan         // #64B4FF - Primary accent
ThemeManager.Colors.AccentBlue         // #3C64B4 - Buttons
ThemeManager.Colors.BorderFocus        // Cyan - Focus state

// Text
ThemeManager.Colors.TextPrimary        // White
ThemeManager.Colors.TextSecondary      // #C8C8C8 - Gray
ThemeManager.Colors.TextAccent         // Cyan - Titles

// States
ThemeManager.Colors.HoverColor         // #32648C
ThemeManager.Colors.DisabledColor      // #3C3C3C

// Resources
ThemeManager.Colors.PopulationColor    // Green
ThemeManager.Colors.MetalColor         // Gray
ThemeManager.Colors.FuelColor          // Orange
```

## Spacing Reference

```csharp
ThemeManager.Spacing.XSmall   // 4px
ThemeManager.Spacing.Small    // 8px  ← Most common
ThemeManager.Spacing.Medium   // 12px
ThemeManager.Spacing.Large    // 15px
ThemeManager.Spacing.XLarge   // 20px
```

## Font Scales

```csharp
ThemeManager.FontScale.Small        // 0.7x
ThemeManager.FontScale.SmallMedium  // 0.8x
ThemeManager.FontScale.Normal       // 0.9x
ThemeManager.FontScale.Large        // 1.2x
ThemeManager.FontScale.XLarge       // 1.5x
ThemeManager.FontScale.XXLarge      // 1.8x
ThemeManager.FontScale.Title        // 2.5x
```

## Common Sizes

```csharp
// Buttons
ThemeManager.Sizes.ButtonSmallWidth   // 80
ThemeManager.Sizes.ButtonSmallHeight  // 30
ThemeManager.Sizes.ButtonMediumWidth  // 150
ThemeManager.Sizes.ButtonMediumHeight // 45
ThemeManager.Sizes.ButtonLargeWidth   // 250
ThemeManager.Sizes.ButtonLargeHeight  // 50

// Inputs
ThemeManager.Sizes.InputMediumHeight  // 40

// Icons
ThemeManager.Sizes.IconMedium         // 18
```

## Button Themes

```csharp
ButtonTheme.Default   // Standard blue button
ButtonTheme.Primary   // Bright blue (main actions)
ButtonTheme.Danger    // Red (delete, cancel)
ButtonTheme.Success   // Green (confirm, save)
ButtonTheme.Hero      // Purple (hero actions)
```

## Panel Themes

```csharp
PanelTheme.Default       // Transparent
PanelTheme.Frame         // With border
PanelTheme.AccentFrame   // Cyan border
PanelTheme.Resource      // Resource display
PanelTheme.Hero          // Hero panel (purple)
PanelTheme.Dark          // Dark semi-transparent
```

## Label Themes

```csharp
LabelTheme.Default    // White text
LabelTheme.Title      // Large cyan (1.8x)
LabelTheme.Subtitle   // Medium cyan (1.2x)
LabelTheme.Secondary  // Gray
LabelTheme.Small      // Small (0.8x)
LabelTheme.Warning    // Yellow
LabelTheme.Error      // Red
LabelTheme.Success    // Green
```

## Applying Themes to Existing Widgets

```csharp
var existingButton = new TextButton { Text = "My Button" };
ThemeManager.ApplyButtonTheme(existingButton, ButtonTheme.Primary);

var existingPanel = new Panel();
ThemeManager.ApplyPanelTheme(existingPanel, PanelTheme.AccentFrame);

var existingLabel = new Label { Text = "Title" };
ThemeManager.ApplyLabelTheme(existingLabel, LabelTheme.Title);
```

## Complete Example: Settings Dialog

```csharp
// Create main panel
var panel = ThemedUIFactory.CreateAccentFramePanel();
panel.Width = 500;
panel.Height = 400;

// Create layout
var layout = ThemedUIFactory.CreateVerticalStack();

// Add title
layout.Widgets.Add(ThemedUIFactory.CreateTitleLabel("Settings"));

// Add input field with label
var nameLabel = ThemedUIFactory.CreateSecondaryLabel("Player Name");
layout.Widgets.Add(nameLabel);

var nameBox = ThemedUIFactory.CreateTextBox("", 400);
layout.Widgets.Add(nameBox);

// Add separator
layout.Widgets.Add(new Panel { Height = ThemeManager.Spacing.Medium });

// Add buttons
var buttonRow = ThemedUIFactory.CreateHorizontalStack();
buttonRow.Widgets.Add(ThemedUIFactory.CreateButton("Save", ButtonTheme.Success));
buttonRow.Widgets.Add(ThemedUIFactory.CreateButton("Cancel", ButtonTheme.Danger));
layout.Widgets.Add(buttonRow);

panel.Widgets.Add(layout);
```

## Migration Checklist

When updating existing UI code:

- [ ] Replace `new Color(r, g, b)` with `ThemeManager.Colors.*`
- [ ] Replace hardcoded spacing numbers with `ThemeManager.Spacing.*`
- [ ] Replace hardcoded sizes with `ThemeManager.Sizes.*`
- [ ] Replace `new Thickness(n)` with `ThemeManager.Padding.*`
- [ ] Use `ThemedUIFactory` for new widgets
- [ ] Apply themes to existing widgets with `ThemeManager.Apply*Theme()`

## Need More Info?

See **UI_THEME.md** for complete documentation including:
- Full color palette
- All available constants
- Customization guide
- Best practices
- Advanced examples

## Legacy Custom Controls

The old custom controls in `UIControls.cs` (Button, TextInputField, etc.) now use ThemeManager constants internally. They still work but are being gradually replaced with Myra widgets.
