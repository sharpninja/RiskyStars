using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using static RiskyStars.Client.ThemeManager;
using MyraButton = Myra.Graphics2D.UI.Button;

namespace RiskyStars.Client;

/// <summary>
/// Shared builders for all Myra surfaces in the client.
/// </summary>
public static class ThemedUIFactory
{
    public static MyraButton CreateButton(string text, ButtonTheme theme = ButtonTheme.Default)
    {
        return CreateButton(text, Sizes.ButtonMediumWidth, Sizes.ButtonMediumHeight, theme);
    }

    public static MyraButton CreateButton(string text, int width, int height, ButtonTheme theme = ButtonTheme.Default)
    {
        var button = new MyraButton
        {
            Width = width,
            Height = height,
            HorizontalAlignment = HorizontalAlignment.Center,
            Content = new Label
            {
                Text = NormalizeButtonText(text)
            }
        };

        ThemeManager.ApplyButtonTheme(button, theme);
        return button;
    }

    public static MyraButton CreateSmallButton(string text, ButtonTheme theme = ButtonTheme.Default)
    {
        return CreateButton(text, Sizes.ButtonSmallWidth, Sizes.ButtonSmallHeight, theme);
    }

    public static MyraButton CreateLargeButton(string text, ButtonTheme theme = ButtonTheme.Default)
    {
        return CreateButton(text, Sizes.ButtonLargeWidth, Sizes.ButtonLargeHeight, theme);
    }

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

    public static Panel CreateScreenRoot(int width, int height)
    {
        var panel = new Panel
        {
            Width = width,
            Height = height,
            Background = ThemeManager.AssetBrushes.Backdrop
        };
        return panel;
    }

    public static Panel CreateViewportFrame(int width, int? height = null)
    {
        var panel = new Panel
        {
            Width = width,
            Background = ThemeManager.AssetBrushes.ViewportFrame,
            Border = ThemeManager.CreateSolidBrush(ThemeManager.Colors.SteelEdge),
            BorderThickness = new Thickness(ThemeManager.BorderThickness.Thin),
            Padding = ThemeManager.Padding.ViewportFrame
        };

        if (height.HasValue)
        {
            panel.Height = height.Value;
        }

        return panel;
    }

    public static Panel CreateConsolePanel(int? width = null)
    {
        var panel = new Panel
        {
            Background = ThemeManager.AssetBrushes.TerminalPanel,
            Border = ThemeManager.CreateSolidBrush(ThemeManager.Colors.BorderNormal),
            BorderThickness = new Thickness(ThemeManager.BorderThickness.Thin),
            Padding = ThemeManager.Padding.Large
        };

        if (width.HasValue)
        {
            panel.Width = width.Value;
        }

        return panel;
    }

    public static Panel CreateHeaderPlate(string title, string? subtitle = null, int? width = null)
    {
        var panel = new Panel
        {
            Background = ThemeManager.AssetBrushes.HeaderPlate,
            Border = ThemeManager.CreateSolidBrush(ThemeManager.Colors.TextWarning),
            BorderThickness = new Thickness(ThemeManager.BorderThickness.Thin),
            Padding = ThemeManager.Padding.HeaderPlate
        };

        if (width.HasValue)
        {
            panel.Width = width.Value;
        }

        var stack = CreateCompactVerticalStack();
        stack.Spacing = ThemeManager.Spacing.XSmall;

        var titleLabel = CreateTitleLabel(title);
        titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        stack.Widgets.Add(titleLabel);

        if (!string.IsNullOrWhiteSpace(subtitle))
        {
            var subtitleLabel = CreateSubtitleLabel(subtitle);
            subtitleLabel.HorizontalAlignment = HorizontalAlignment.Center;
            subtitleLabel.Wrap = true;
            stack.Widgets.Add(subtitleLabel);
        }

        panel.Widgets.Add(stack);
        return panel;
    }

    public static Panel CreateFieldCard(string title, string description, Widget content, int width)
    {
        var panel = CreateFramePanel();
        panel.Width = width;

        var stack = CreateCompactVerticalStack();
        stack.Spacing = ThemeManager.Spacing.Small;

        var titleLabel = CreateHeadingLabel(title);
        stack.Widgets.Add(titleLabel);

        var descriptionLabel = CreateSmallLabel(description);
        descriptionLabel.TextColor = ThemeManager.Colors.TextPrimary;
        descriptionLabel.Wrap = true;
        stack.Widgets.Add(descriptionLabel);
        stack.Widgets.Add(content);

        panel.Widgets.Add(stack);
        return panel;
    }

    public static Panel CreateListRowPanel(bool selected = false)
    {
        return new Panel
        {
            Background = selected ? ThemeManager.AssetBrushes.ListRowSelected : ThemeManager.AssetBrushes.ListRowNormal,
            Border = ThemeManager.CreateSolidBrush(selected ? ThemeManager.Colors.BorderFocus : ThemeManager.Colors.BorderNormal),
            BorderThickness = new Thickness(ThemeManager.BorderThickness.Thin),
            Padding = ThemeManager.Padding.Medium,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
    }

    public static Panel CreateStatusBadge(Color color, string text, int width = 104)
    {
        var panel = new Panel
        {
            Width = width,
            Height = ThemeManager.Sizes.BadgeHeight,
            Padding = ThemeManager.Padding.Badge,
            Background = ThemeManager.CreateSolidBrush(color * 0.25f),
            Border = ThemeManager.CreateSolidBrush(color),
            BorderThickness = new Thickness(ThemeManager.BorderThickness.Thin),
            HorizontalAlignment = HorizontalAlignment.Left
        };

        var label = CreateSmallLabel(text.ToUpperInvariant());
        label.TextColor = ThemeManager.Colors.TextPrimary;
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.VerticalAlignment = VerticalAlignment.Center;
        panel.Widgets.Add(label);
        return panel;
    }

    public static HorizontalStackPanel CreateActionBar(int spacing = -1)
    {
        return CreateHorizontalStack(spacing >= 0 ? spacing : ThemeManager.Spacing.Large);
    }

    public static TextBox CreateTextBox(string text = "")
    {
        var textBox = new TextBox
        {
            Text = text
        };
        ThemeManager.ApplyTextBoxTheme(textBox);
        return textBox;
    }

    public static TextBox CreateTextBox(string text, int width)
    {
        var textBox = CreateTextBox(text);
        textBox.Width = width;
        return textBox;
    }

    public static ValidatedTextBox CreateValidatedTextBox(int width, string placeholder = "", bool showErrorLabel = false)
    {
        return new ValidatedTextBox(width, placeholder, showErrorLabel);
    }

    public static ValidatedTextBox CreateValidatedPlayerNameBox(int width = 400, bool showErrorLabel = true)
    {
        var validatedBox = new ValidatedTextBox(width, "Commander name", showErrorLabel);
        validatedBox.SetValidator(InputValidator.ValidatePlayerName);
        return validatedBox;
    }

    public static ValidatedTextBox CreateValidatedServerAddressBox(int width = 400, bool showErrorLabel = true)
    {
        var validatedBox = new ValidatedTextBox(width, Settings.Load().ServerAddress, showErrorLabel);
        validatedBox.SetValidator(InputValidator.ValidateServerAddress);
        return validatedBox;
    }

    public static ValidatedTextBox CreateValidatedMapNameBox(int width = 400, bool showErrorLabel = true)
    {
        var validatedBox = new ValidatedTextBox(width, "Map name", showErrorLabel);
        validatedBox.SetValidator(InputValidator.ValidateMapName);
        return validatedBox;
    }

#pragma warning disable CS0618
    public static ComboBox CreateComboBox()
#pragma warning restore CS0618
    {
#pragma warning disable CS0618
        var comboBox = new ComboBox();
#pragma warning restore CS0618
        ThemeManager.ApplyComboBoxTheme(comboBox);
        return comboBox;
    }

#pragma warning disable CS0618
    public static ComboBox CreateComboBox(int width)
#pragma warning restore CS0618
    {
        var comboBox = CreateComboBox();
        comboBox.Width = width;
        return comboBox;
    }

    public static CheckButton CreateCheckButton(bool isChecked = false)
    {
        var checkButton = new CheckButton
        {
            IsPressed = isChecked
        };
        ThemeManager.ApplyCheckButtonTheme(checkButton);
        return checkButton;
    }

    public static ScrollViewer CreateAutoScrollViewer(Widget content, int? height = null)
    {
        var scrollViewer = new ScrollViewer
        {
            Content = content,
            ShowVerticalScrollBar = true,
            ShowHorizontalScrollBar = false,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        if (height.HasValue)
        {
            scrollViewer.Height = height.Value;
        }

        return scrollViewer;
    }

    public static Grid CreateGrid(int rowSpacing = -1, int columnSpacing = -1)
    {
        return new Grid
        {
            RowSpacing = rowSpacing >= 0 ? rowSpacing : ThemeManager.Spacing.Small,
            ColumnSpacing = columnSpacing >= 0 ? columnSpacing : ThemeManager.Spacing.Small
        };
    }

    public static Grid CreateCompactGrid()
    {
        return CreateGrid(ThemeManager.Spacing.XSmall, ThemeManager.Spacing.XSmall);
    }

    public static Grid CreateSpaciousGrid()
    {
        return CreateGrid(ThemeManager.Spacing.Large, ThemeManager.Spacing.Large);
    }

    public static VerticalStackPanel CreateVerticalStack(int spacing = -1)
    {
        return new VerticalStackPanel
        {
            Spacing = spacing >= 0 ? spacing : ThemeManager.Spacing.Small
        };
    }

    public static VerticalStackPanel CreateCompactVerticalStack()
    {
        return CreateVerticalStack(ThemeManager.Spacing.XSmall);
    }

    public static VerticalStackPanel CreateSpaciousVerticalStack()
    {
        return CreateVerticalStack(ThemeManager.Spacing.Large);
    }

    public static HorizontalStackPanel CreateHorizontalStack(int spacing = -1)
    {
        return new HorizontalStackPanel
        {
            Spacing = spacing >= 0 ? spacing : ThemeManager.Spacing.Small
        };
    }

    public static HorizontalStackPanel CreateCompactHorizontalStack()
    {
        return CreateHorizontalStack(ThemeManager.Spacing.XSmall);
    }

    public static HorizontalStackPanel CreateSpaciousHorizontalStack()
    {
        return CreateHorizontalStack(ThemeManager.Spacing.Large);
    }

    public static Panel CreateSpacer(int height)
    {
        return new Panel { Height = height };
    }

    public static Panel CreateResourceIcon(Color color)
    {
        var outer = new Panel
        {
            Width = ThemeManager.Sizes.IconLarge,
            Height = ThemeManager.Sizes.IconLarge,
            Background = ThemeManager.AssetBrushes.WindowFrame,
            Border = ThemeManager.CreateSolidBrush(ThemeManager.Colors.SteelEdge),
            BorderThickness = new Thickness(ThemeManager.BorderThickness.Thin),
            Padding = new Thickness(3)
        };

        var inner = new Panel
        {
            Background = ThemeManager.CreateSolidBrush(color),
            Width = ThemeManager.Sizes.IconMedium,
            Height = ThemeManager.Sizes.IconMedium
        };

        outer.Widgets.Add(inner);
        return outer;
    }

    public static Panel CreatePopulationIcon()
    {
        return CreateResourceIcon(ThemeManager.Colors.PopulationColor);
    }

    public static Panel CreateMetalIcon()
    {
        return CreateResourceIcon(ThemeManager.Colors.MetalColor);
    }

    public static Panel CreateFuelIcon()
    {
        return CreateResourceIcon(ThemeManager.Colors.FuelColor);
    }

    public static Panel CreateSlotPanel(bool isReady = false)
    {
        var panel = CreateListRowPanel(isReady);
        panel.Background = ThemeManager.CreateSolidBrush(isReady ? ThemeManager.Colors.SlotPanelReady : ThemeManager.Colors.SlotPanelNormal);
        return panel;
    }

    public static Panel CreateBadgePanel(Color color, string text)
    {
        return CreateStatusBadge(color, text);
    }

    public static Panel CreateAIEasyBadge()
    {
        return CreateStatusBadge(ThemeManager.Colors.AIEasyColor, "Easy");
    }

    public static Panel CreateAIMediumBadge()
    {
        return CreateStatusBadge(ThemeManager.Colors.AIMediumColor, "Medium");
    }

    public static Panel CreateAIHardBadge()
    {
        return CreateStatusBadge(ThemeManager.Colors.AIHardColor, "Hard");
    }

    public static HorizontalSeparator CreateHorizontalSeparator()
    {
        return new HorizontalSeparator
        {
            Background = ThemeManager.CreateSolidBrush(ThemeManager.Colors.BorderNormal),
            Thickness = ThemeManager.BorderThickness.Thin
        };
    }

    public static VerticalSeparator CreateVerticalSeparator()
    {
        return new VerticalSeparator
        {
            Background = ThemeManager.CreateSolidBrush(ThemeManager.Colors.BorderNormal),
            Thickness = ThemeManager.BorderThickness.Thin
        };
    }

    public static SpinButton CreateSpinButton(int? value = null, int? minimum = null, int? maximum = null)
    {
        var spinButton = new SpinButton
        {
            Value = value,
            Minimum = minimum,
            Maximum = maximum
        };
        ThemeManager.ApplySpinButtonTheme(spinButton);
        return spinButton;
    }

    public static HorizontalStackPanel CreateLabelWithIcon(Panel icon, string text)
    {
        var stack = CreateHorizontalStack();
        stack.Widgets.Add(icon);
        stack.Widgets.Add(CreateLabel(text));
        return stack;
    }

    private static string NormalizeButtonText(string text)
    {
        return text.ToUpperInvariant();
    }
}
