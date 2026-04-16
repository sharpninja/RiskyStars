# Input Validation System

This document describes the comprehensive input validation and error feedback system for RiskyStars UI forms.

## Overview

The input validation system provides real-time validation with visual error indicators and helpful error messages using Myra's tooltip system. It ensures data integrity and improves user experience by providing immediate feedback on invalid inputs.

## Components

### 1. InputValidator

**File**: `InputValidator.cs`

A static class providing validation methods for all input types.

#### Validation Methods

##### Server Address Validation
```csharp
ValidationResult ValidateServerAddress(string address)
```
- Checks for empty/whitespace
- Validates URL format (http:// or https://)
- Ensures valid host is present
- Returns detailed error messages for each validation failure

**Rules**:
- Required field
- Must be a valid URL with http:// or https:// scheme
- Must include a host name

**Example**:
```csharp
var result = InputValidator.ValidateServerAddress("http://localhost:5000");
if (!result.IsValid)
{
    Console.WriteLine(result.Message);
}
```

##### Player Name Validation
```csharp
ValidationResult ValidatePlayerName(string name)
```
- Checks for empty/whitespace
- Validates length (2-20 characters)
- Validates allowed characters (alphanumeric, spaces, underscores, hyphens)
- Ensures at least one letter or number is present

**Rules**:
- Required field
- Length: 2-20 characters
- Allowed characters: a-z, A-Z, 0-9, space, underscore (_), hyphen (-)
- Must contain at least one letter or number

##### Map Name Validation
```csharp
ValidationResult ValidateMapName(string mapName)
```
- Checks for empty/whitespace
- Validates length (2-50 characters)
- Validates allowed characters

**Rules**:
- Required field
- Length: 2-50 characters
- Allowed characters: a-z, A-Z, 0-9, space, underscore (_), hyphen (-)

##### Player Count Validation
```csharp
ValidationResult ValidatePlayerCount(int count, int min, int max)
```
- Validates numeric range
- Returns specific error messages for min/max violations

##### Generic Validators
```csharp
ValidationResult ValidateNumericRange(int value, int min, int max, string fieldName)
ValidationResult ValidateRequired(string value, string fieldName)
```

### 2. ValidationResult

**File**: `InputValidator.cs`

Result object returned by validation methods.

**Properties**:
- `bool IsValid` - Whether the validation passed
- `string Message` - Descriptive error message or success message

### 3. ValidatedTextBox (Myra-based)

**File**: `ValidatedTextBox.cs`

A wrapper around Myra's TextBox that provides real-time validation and visual error feedback.

**Features**:
- Real-time validation on text change
- Red error border when invalid
- Error message tooltip on hover
- Optional inline error label below the field
- Themed styling using ThemeManager

**Usage**:
```csharp
// Create validated text box
var textBox = new ValidatedTextBox(400, "Enter your name", showErrorLabel: true);
textBox.SetValidator(InputValidator.ValidatePlayerName);
textBox.Text = "Player";

// Add to Myra UI
grid.Widgets.Add(textBox.Container);

// Check validation before saving
if (!textBox.IsValid)
{
    ShowError(textBox.ErrorMessage);
    return;
}
```

**Properties**:
- `TextBox TextBox` - Access to underlying Myra TextBox
- `Panel Container` - Container panel to add to UI
- `bool IsValid` - Current validation state
- `string? ErrorMessage` - Current error message if invalid
- `string Text` - Get/set text value

**Methods**:
- `SetValidator(Func<string, ValidationResult> validator)` - Set validation function
- `ValidateInput()` - Manually trigger validation
- `ClearValidation()` - Clear any validation errors

### 4. ValidatedTextInputField (Custom UI)

**File**: `ValidatedTextInputField.cs`

A wrapper around the custom TextInputField for non-Myra screens (like ConnectionScreen).

**Features**:
- Real-time validation
- Red error border overlay
- Error message displayed below field
- Integrates with custom UI rendering

**Usage**:
```csharp
var field = new ValidatedTextInputField(
    new Rectangle(x, y, width, height),
    "Player Name",
    maxLength: 20);
field.SetValidator(InputValidator.ValidatePlayerName);

// Update
field.Update(mouseState, keyState, previousKeyState);

// Draw
field.Draw(spriteBatch, pixelTexture, font);

// Validate before use
if (!field.IsValid)
{
    statusMessage = field.ErrorMessage;
    return;
}
```

### 5. ThemedUIFactory Extensions

**File**: `ThemedUIFactory.cs`

Factory methods for creating pre-configured validated text boxes.

**Methods**:
```csharp
// Generic validated text box
ValidatedTextBox CreateValidatedTextBox(int width, string placeholder, bool showErrorLabel)

// Pre-configured for specific use cases
ValidatedTextBox CreateValidatedPlayerNameBox(int width = 400, bool showErrorLabel = true)
ValidatedTextBox CreateValidatedServerAddressBox(int width = 400, bool showErrorLabel = true)
ValidatedTextBox CreateValidatedMapNameBox(int width = 400, bool showErrorLabel = true)
```

**Example**:
```csharp
var playerNameBox = ThemedUIFactory.CreateValidatedPlayerNameBox();
playerNameBox.Text = "Player";
grid.Widgets.Add(playerNameBox.Container);
```

## Implementation in UI Screens

### MainMenu (Settings Screen)

**File**: `MainMenu.cs`

**Validated Fields**:
- Server Address (ValidatedTextBox)

**Implementation**:
```csharp
_serverAddressTextBox = new ValidatedTextBox(500, "http://localhost:5000", showErrorLabel: true);
_serverAddressTextBox.Text = _settings.ServerAddress;
_serverAddressTextBox.SetValidator(InputValidator.ValidateServerAddress);

// In save handler
if (!_serverAddressTextBox.IsValid)
{
    _dialogManager?.ShowError("Validation Error", "Please fix the server address before saving.");
    return;
}
```

### SinglePlayerLobbyScreen

**File**: `SinglePlayerLobbyScreen.cs`

**Validated Fields**:
- Player Name (ValidatedTextBox)

**Implementation**:
```csharp
_playerNameTextBox = new ValidatedTextBox(400, "Enter your name", showErrorLabel: true);
_playerNameTextBox.Text = "Player";
_playerNameTextBox.SetValidator(InputValidator.ValidatePlayerName);

// In start game handler
if (_playerNameTextBox == null || !_playerNameTextBox.IsValid)
{
    _dialogManager?.ShowError("Validation Error", 
        "Please enter a valid player name (2-20 characters, letters and numbers only).");
    return;
}
```

### CreateLobbyScreen

**File**: `CreateLobbyScreen.cs`

**Validated Fields**:
- Map Name (ValidatedTextBox)
- Max Players (validated in TryCreateLobbySettings)

**Implementation**:
```csharp
_mapNameTextBox = new ValidatedTextBox(450, "Enter map name", showErrorLabel: true);
_mapNameTextBox.Text = "Default";
_mapNameTextBox.SetValidator(InputValidator.ValidateMapName);

// In create handler
if (_mapNameTextBox == null || !_mapNameTextBox.IsValid)
{
    return; // Error already shown in UI
}
```

### ConnectionScreen

**File**: `ConnectionScreen.cs`

**Validated Fields**:
- Player Name (ValidatedTextInputField)
- Server Address (ValidatedTextInputField)

**Implementation**:
```csharp
_playerNameField = new ValidatedTextInputField(bounds, "Player Name", 20);
_playerNameField.SetValidator(InputValidator.ValidatePlayerName);

_serverAddressField = new ValidatedTextInputField(bounds, "Server Address", 100);
_serverAddressField.SetValidator(InputValidator.ValidateServerAddress);

// In connect handler
var nameValidation = _playerNameField.ValidateInput();
var serverValidation = _serverAddressField.ValidateInput();

if (!nameValidation.IsValid)
{
    _statusMessage = nameValidation.Message;
    return;
}
```

## Visual Error Indicators

### Myra-based Screens

**Error States**:
1. **Border Color**: Changes from normal (gray) → error (red)
2. **Tooltip**: Displays error message on hover
3. **Error Label**: Optional inline error message below field (red text, small font)

**Colors** (from ThemeManager):
- Normal Border: `ThemeManager.Colors.BorderNormal` (Gray)
- Hover Border: `ThemeManager.Colors.BorderHover` (Light Blue)
- Focus Border: `ThemeManager.Colors.BorderFocus` (Cyan)
- Error Border: `ThemeManager.Colors.TextError` (Red)
- Error Text: `ThemeManager.Colors.TextError` (Red)

### Custom UI Screens

**Error States**:
1. **Border Overlay**: Thick red border drawn over the field
2. **Error Message**: Displayed below the field in red text (0.6 scale)

**Rendering**:
- Error border thickness: `ThemeManager.BorderThickness.Thick` (3px)
- Error message offset: 5px below field
- Error message scale: 0.6f

## Best Practices

### 1. Real-time Validation
All validated fields automatically validate on text change, providing immediate feedback to users.

### 2. Submit-time Validation
Always validate again before performing actions:
```csharp
if (!_validatedField.IsValid)
{
    ShowError(_validatedField.ErrorMessage);
    return;
}
```

### 3. Error Messages
- Keep messages concise and actionable
- Specify what's wrong and how to fix it
- Use friendly, non-technical language where possible

### 4. Visual Consistency
- Use `ThemeManager.Colors` for all colors
- Use `showErrorLabel: true` for important fields
- Use tooltips for secondary validation feedback

### 5. Accessibility
- Error messages are both visual (color) and textual (message)
- Tooltips provide additional context without cluttering the UI
- Inline error labels for screen readers and users who need persistent error messages

## Extending the System

### Adding New Validators

1. Add validation method to `InputValidator`:
```csharp
public static ValidationResult ValidateEmail(string email)
{
    if (string.IsNullOrWhiteSpace(email))
        return new ValidationResult(false, "Email is required");
    
    if (!email.Contains("@"))
        return new ValidationResult(false, "Invalid email format");
    
    return new ValidationResult(true, "Valid email");
}
```

2. Add factory method to `ThemedUIFactory`:
```csharp
public static ValidatedTextBox CreateValidatedEmailBox(int width = 400, bool showErrorLabel = true)
{
    var validatedBox = new ValidatedTextBox(width, "Enter email", showErrorLabel);
    validatedBox.SetValidator(InputValidator.ValidateEmail);
    return validatedBox;
}
```

3. Use in UI screens:
```csharp
_emailTextBox = ThemedUIFactory.CreateValidatedEmailBox();
grid.Widgets.Add(_emailTextBox.Container);
```

### Custom Validation Functions

You can also use custom validation functions inline:
```csharp
_textBox.SetValidator(text =>
{
    if (text.Length < 5)
        return new ValidationResult(false, "Must be at least 5 characters");
    return new ValidationResult(true, "Valid");
});
```

## Testing Validation

### Manual Testing
1. Test empty/whitespace inputs
2. Test boundary values (min/max lengths)
3. Test invalid characters
4. Test valid inputs
5. Test tooltip display on hover
6. Test error label visibility

### Validation Rules Summary

| Field Type | Min Length | Max Length | Allowed Characters | Special Rules |
|------------|-----------|------------|-------------------|---------------|
| Player Name | 2 | 20 | a-z, A-Z, 0-9, space, _, - | Must have ≥1 letter/number |
| Map Name | 2 | 50 | a-z, A-Z, 0-9, space, _, - | - |
| Server Address | 1 | - | URL format | Must be http:// or https:// |
| Player Count | 2 | 6 | Numbers only | Range validation |

## Troubleshooting

### Validation not triggering
- Ensure `SetValidator()` is called before adding to UI
- Check that TextChanged event is firing
- Verify validator function is not null

### Error border not showing
- Ensure `UpdateVisualState()` is being called
- Check that ThemeManager colors are initialized
- Verify border brushes are being applied correctly

### Tooltips not appearing
- Myra tooltips require mouse hover over the control
- Ensure `TooltipText` property is being set
- Check Desktop rendering is working correctly

### Error labels not visible
- Ensure `showErrorLabel: true` was passed to constructor
- Check that Container panel is being added, not just TextBox
- Verify Grid row proportions allow space for error label
