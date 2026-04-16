using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.Brushes;
using static RiskyStars.Client.ThemeManager;

namespace RiskyStars.Client;

/// <summary>
/// Factory class for creating themed UI widgets using the ThemeManager
/// </summary>
public static class ThemedUIFactory
{
    // Button factory methods
    public static TextButton CreateButton(string text, ButtonTheme theme = ButtonTheme.Default)
    {
        var button = new TextButton
        {
            Text = text,
            Width = Sizes.ButtonMediumWidth,
            Height = Sizes.ButtonMediumHeight,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        
        ThemeManager.ApplyButtonTheme(button, theme);
        return button;
    }

    public static TextButton CreateButton(string text, int width, int height, ButtonTheme theme = ButtonTheme.Default)
    {
        var button = new TextButton
        {
            Text = text,
            Width = width,
            Height = height
        };
        
        ThemeManager.ApplyButtonTheme(button, theme);
        return button;
    }

    public static TextButton CreateSmallButton(string text, ButtonTheme theme = ButtonTheme.Default)
    {
        return CreateButton(text, Sizes.ButtonSmallWidth, Sizes.ButtonSmallHeight, theme);
    }

    public static TextButton CreateLargeButton(string text, ButtonTheme theme = ButtonTheme.Default)
    {
        return CreateButton(text, Sizes.ButtonLargeWidth, Sizes.ButtonLargeHeight, theme);
    }

    // Label factory methods
    public static Label CreateLabel(string text, LabelTheme theme = LabelTheme.Default)
    {
        var label = new Label { Text = text };
        ThemeManager.ApplyLabelTheme(label, theme);
        return label;
    }

    public static Label CreateTitleLabel(string text)
    {
        return CreateLabel(text, LabelTheme.Title);
    }

    public static Label CreateSubtitleLabel(string text)
    {
        return CreateLabel(text, LabelTheme.Subtitle);
    }

    public static Label CreateHeadingLabel(string text)
    {
        return CreateLabel(text, LabelTheme.Heading);
    }

    public static Label CreateSecondaryLabel(string text)
    {
        return CreateLabel(text, LabelTheme.Secondary);
    }

    public static Label CreateSmallLabel(string text)
    {
        return CreateLabel(text, LabelTheme.Small);
    }

    // Panel factory methods
    public static Panel CreatePanel(PanelTheme theme = PanelTheme.Default)
    {
        var panel = new Panel();
        ThemeManager.ApplyPanelTheme(panel, theme);
        return panel;
    }

    public static Panel CreateFramePanel()
    {
        return CreatePanel(PanelTheme.Frame);
    }

    public static Panel CreateAccentFramePanel()
    {
        return CreatePanel(PanelTheme.AccentFrame);
    }

    public static Panel CreateResourcePanel()
    {
        return CreatePanel(PanelTheme.Resource);
    }

    public static Panel CreateHeroPanel()
    {
        return CreatePanel(PanelTheme.Hero);
    }

    public static Panel CreateDarkPanel()
    {
        return CreatePanel(PanelTheme.Dark);
    }

    // TextBox factory methods
    public static TextBox CreateTextBox(string text = "")
    {
        return new TextBox
        {
            Text = text,
            Background = CreateSolidBrush(Colors.PrimaryLight),
            OverBackground = CreateSolidBrush(new Color(50, 50, 70)),
            FocusedBackground = CreateSolidBrush(Colors.PrimaryLight),
            Border = CreateSolidBrush(Colors.BorderNormal),
            OverBorder = CreateSolidBrush(Colors.BorderHover),
            FocusedBorder = CreateSolidBrush(Colors.BorderFocus),
            BorderThickness = new Thickness(BorderThickness.Normal),
            TextColor = Colors.TextPrimary,
            Padding = Padding.Input,
            Height = Sizes.InputMediumHeight
        };
    }

    public static TextBox CreateTextBox(string text, int width)
    {
        var textBox = CreateTextBox(text);
        textBox.Width = width;
        return textBox;
    }

    // ValidatedTextBox factory methods
    public static ValidatedTextBox CreateValidatedTextBox(int width, string placeholder = "", bool showErrorLabel = false)
    {
        return new ValidatedTextBox(width, placeholder, showErrorLabel);
    }

    public static ValidatedTextBox CreateValidatedPlayerNameBox(int width = 400, bool showErrorLabel = true)
    {
        var validatedBox = new ValidatedTextBox(width, "Enter your name", showErrorLabel);
        validatedBox.SetValidator(InputValidator.ValidatePlayerName);
        return validatedBox;
    }

    public static ValidatedTextBox CreateValidatedServerAddressBox(int width = 400, bool showErrorLabel = true)
    {
        var validatedBox = new ValidatedTextBox(width, "http://localhost:5000", showErrorLabel);
        validatedBox.SetValidator(InputValidator.ValidateServerAddress);
        return validatedBox;
    }

    public static ValidatedTextBox CreateValidatedMapNameBox(int width = 400, bool showErrorLabel = true)
    {
        var validatedBox = new ValidatedTextBox(width, "Enter map name", showErrorLabel);
        validatedBox.SetValidator(InputValidator.ValidateMapName);
        return validatedBox;
    }

    // ComboBox factory methods
    public static ComboBox CreateComboBox()
    {
        return new ComboBox
        {
            Background = CreateSolidBrush(Colors.PrimaryLight),
            OverBackground = CreateSolidBrush(new Color(50, 50, 70)),
            FocusedBackground = CreateSolidBrush(Colors.PrimaryLight),
            Border = CreateSolidBrush(Colors.BorderNormal),
            OverBorder = CreateSolidBrush(Colors.BorderHover),
            FocusedBorder = CreateSolidBrush(Colors.BorderFocus),
            BorderThickness = new Thickness(BorderThickness.Normal),
            Padding = Padding.Input
        };
    }

    public static ComboBox CreateComboBox(int width)
    {
        var comboBox = CreateComboBox();
        comboBox.Width = width;
        return comboBox;
    }

    // CheckButton factory methods
    public static CheckButton CreateCheckButton(bool isChecked = false)
    {
        return new CheckButton
        {
            IsPressed = isChecked,
            Background = CreateSolidBrush(Colors.PrimaryLight),
            OverBackground = CreateSolidBrush(new Color(50, 50, 70)),
            PressedBackground = CreateSolidBrush(Colors.AccentCyan),
            Border = CreateSolidBrush(Colors.BorderNormal),
            OverBorder = CreateSolidBrush(Colors.BorderHover),
            BorderThickness = new Thickness(BorderThickness.Normal),
            Width = Sizes.CheckboxSize,
            Height = Sizes.CheckboxSize
        };
    }

    // Grid factory methods
    public static Grid CreateGrid(int rowSpacing = -1, int columnSpacing = -1)
    {
        return new Grid
        {
            RowSpacing = rowSpacing >= 0 ? rowSpacing : Spacing.Small,
            ColumnSpacing = columnSpacing >= 0 ? columnSpacing : Spacing.Small
        };
    }

    public static Grid CreateCompactGrid()
    {
        return CreateGrid(Spacing.XSmall, Spacing.XSmall);
    }

    public static Grid CreateSpaciousGrid()
    {
        return CreateGrid(Spacing.Large, Spacing.Large);
    }

    // StackPanel factory methods
    public static VerticalStackPanel CreateVerticalStack(int spacing = -1)
    {
        return new VerticalStackPanel
        {
            Spacing = spacing >= 0 ? spacing : Spacing.Small
        };
    }

    public static VerticalStackPanel CreateCompactVerticalStack()
    {
        return CreateVerticalStack(Spacing.XSmall);
    }

    public static VerticalStackPanel CreateSpaciousVerticalStack()
    {
        return CreateVerticalStack(Spacing.Large);
    }

    public static HorizontalStackPanel CreateHorizontalStack(int spacing = -1)
    {
        return new HorizontalStackPanel
        {
            Spacing = spacing >= 0 ? spacing : Spacing.Small
        };
    }

    public static HorizontalStackPanel CreateCompactHorizontalStack()
    {
        return CreateHorizontalStack(Spacing.XSmall);
    }

    public static HorizontalStackPanel CreateSpaciousHorizontalStack()
    {
        return CreateHorizontalStack(Spacing.Large);
    }

    // Icon panel factory methods (for resource icons, etc.)
    public static Panel CreateResourceIcon(Color color)
    {
        return new Panel
        {
            Width = Sizes.IconMedium,
            Height = Sizes.IconMedium,
            Background = CreateSolidBrush(color)
        };
    }

    public static Panel CreatePopulationIcon()
    {
        return CreateResourceIcon(Colors.PopulationColor);
    }

    public static Panel CreateMetalIcon()
    {
        return CreateResourceIcon(Colors.MetalColor);
    }

    public static Panel CreateFuelIcon()
    {
        return CreateResourceIcon(Colors.FuelColor);
    }

    // Slot panel factory methods (for lobby player slots)
    public static Panel CreateSlotPanel(bool isReady = false)
    {
        return new Panel
        {
            Background = CreateSolidBrush(isReady ? Colors.SlotPanelReady : Colors.SlotPanelNormal),
            Padding = Padding.Input,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
    }

    // Badge panel factory methods (for AI difficulty badges)
    public static Panel CreateBadgePanel(Color color, string text)
    {
        var panel = new Panel
        {
            Background = CreateSolidBrush(color * 0.8f),
            Width = 70,
            Height = 25,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        var label = new Label
        {
            Text = text.ToUpper(),
            TextColor = Color.White,
            Scale = FontScale.Tiny,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        
        panel.Widgets.Add(label);
        return panel;
    }

    public static Panel CreateAIEasyBadge()
    {
        return CreateBadgePanel(Colors.AIEasyColor, "EASY");
    }

    public static Panel CreateAIMediumBadge()
    {
        return CreateBadgePanel(Colors.AIMediumColor, "MEDIUM");
    }

    public static Panel CreateAIHardBadge()
    {
        return CreateBadgePanel(Colors.AIHardColor, "HARD");
    }

    // Separator factory methods
    public static HorizontalSeparator CreateHorizontalSeparator()
    {
        return new HorizontalSeparator
        {
            Background = CreateSolidBrush(Colors.BorderNormal),
            Thickness = BorderThickness.Thin
        };
    }

    public static VerticalSeparator CreateVerticalSeparator()
    {
        return new VerticalSeparator
        {
            Background = CreateSolidBrush(Colors.BorderNormal),
            Thickness = BorderThickness.Thin
        };
    }

    // SpinButton factory methods
    public static SpinButton CreateSpinButton(int? value = null, int? minimum = null, int? maximum = null)
    {
        return new SpinButton
        {
            Value = value,
            Minimum = minimum,
            Maximum = maximum,
            Background = CreateSolidBrush(Colors.PrimaryLight),
            Border = CreateSolidBrush(Colors.BorderNormal),
            BorderThickness = new Thickness(BorderThickness.Normal),
            Padding = Padding.Input
        };
    }

    // Helper method to create a label with icon
    public static HorizontalStackPanel CreateLabelWithIcon(Panel icon, string text)
    {
        var stack = CreateHorizontalStack();
        stack.Widgets.Add(icon);
        stack.Widgets.Add(CreateLabel(text));
        return stack;
    }
}
