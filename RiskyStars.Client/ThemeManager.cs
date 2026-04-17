using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;
using System;
using System.IO;
using MyraButton = Myra.Graphics2D.UI.Button;
using MyraComboBox = Myra.Graphics2D.UI.ComboBox;

namespace RiskyStars.Client;

public static class ThemeManager
{
    private const string FontFileName = "Oxanium.ttf";
    private const string MyraAssetRoot = "Sprites/UI/Myra/";

    private static bool _isInitialized;
    private static bool _contentLoaded;
    private static FontSystem? _fontSystem;
    private static LoadedAssets? _assets;
    private static ThemeProfile _themeProfile = ThemeProfile.FromSettings(new UiThemeSettings());

    public static Stylesheet Stylesheet
    {
        get
        {
            return Myra.Graphics2D.UI.Styles.Stylesheet.Current;
        }
    }

    public static bool IsContentLoaded => _contentLoaded;

    public static void ApplyThemeSettings(UiThemeSettings? themeSettings)
    {
        _themeProfile = ThemeProfile.FromSettings(themeSettings);

        if (_contentLoaded)
        {
            ApplyGlobalStylesheet();
        }
    }

    public static void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
    }

    public static void LoadContent(ContentManager content)
    {
        Initialize();

        if (_contentLoaded)
        {
            return;
        }

        _assets = new LoadedAssets
        {
            Backdrop = CreateTextureBrush(content.Load<Texture2D>($"{MyraAssetRoot}Backdrop")),
            ViewportFrame = CreateNinePatchBrush(content.Load<Texture2D>($"{MyraAssetRoot}ViewportFrame"), 22),
            WindowFrame = CreateNinePatchBrush(content.Load<Texture2D>($"{MyraAssetRoot}WindowFrame"), 16),
            TerminalPanel = CreateNinePatchBrush(content.Load<Texture2D>($"{MyraAssetRoot}TerminalPanel"), 10),
            HeaderPlate = CreateNinePatchBrush(content.Load<Texture2D>($"{MyraAssetRoot}HeaderPlate"), 12),
            ButtonPrimaryNormal = CreateNinePatchBrush(content.Load<Texture2D>($"{MyraAssetRoot}ButtonPrimaryNormal"), 8),
            ButtonPrimaryHover = CreateNinePatchBrush(content.Load<Texture2D>($"{MyraAssetRoot}ButtonPrimaryHover"), 8),
            ButtonPrimaryPressed = CreateNinePatchBrush(content.Load<Texture2D>($"{MyraAssetRoot}ButtonPrimaryPressed"), 8),
            ButtonPrimaryDisabled = CreateNinePatchBrush(content.Load<Texture2D>($"{MyraAssetRoot}ButtonPrimaryDisabled"), 8),
            ButtonSecondaryNormal = CreateNinePatchBrush(content.Load<Texture2D>($"{MyraAssetRoot}ButtonSecondaryNormal"), 8),
            ButtonSecondaryHover = CreateNinePatchBrush(content.Load<Texture2D>($"{MyraAssetRoot}ButtonSecondaryHover"), 8),
            ButtonSecondaryPressed = CreateNinePatchBrush(content.Load<Texture2D>($"{MyraAssetRoot}ButtonSecondaryPressed"), 8),
            ButtonSecondaryDisabled = CreateNinePatchBrush(content.Load<Texture2D>($"{MyraAssetRoot}ButtonSecondaryDisabled"), 8),
            ButtonDangerNormal = CreateNinePatchBrush(content.Load<Texture2D>($"{MyraAssetRoot}ButtonDangerNormal"), 8),
            ButtonDangerHover = CreateNinePatchBrush(content.Load<Texture2D>($"{MyraAssetRoot}ButtonDangerHover"), 8),
            ButtonDangerPressed = CreateNinePatchBrush(content.Load<Texture2D>($"{MyraAssetRoot}ButtonDangerPressed"), 8),
            ButtonDangerDisabled = CreateNinePatchBrush(content.Load<Texture2D>($"{MyraAssetRoot}ButtonDangerDisabled"), 8),
            TabNormal = CreateNinePatchBrush(content.Load<Texture2D>($"{MyraAssetRoot}TabNormal"), 6),
            TabSelected = CreateNinePatchBrush(content.Load<Texture2D>($"{MyraAssetRoot}TabSelected"), 6),
            ListRowNormal = CreateNinePatchBrush(content.Load<Texture2D>($"{MyraAssetRoot}ListRowNormal"), 4),
            ListRowSelected = CreateNinePatchBrush(content.Load<Texture2D>($"{MyraAssetRoot}ListRowSelected"), 4)
        };

        LoadUiFont();
        ApplyGlobalStylesheet();
        _contentLoaded = true;
    }

    private static void LoadUiFont()
    {
        var fontPath = Path.Combine(AppContext.BaseDirectory, "Content", "Fonts", FontFileName);
        if (!File.Exists(fontPath))
        {
            return;
        }

        _fontSystem = new FontSystem();
        _fontSystem.AddFont(File.ReadAllBytes(fontPath));
    }

    private static void ApplyGlobalStylesheet()
    {
        var sheet = Stylesheet;

        sheet.LabelStyle ??= new LabelStyle();
        sheet.TooltipStyle ??= new LabelStyle();
        sheet.ButtonStyle ??= new ButtonStyle();
        sheet.TextBoxStyle ??= new TextBoxStyle();
        sheet.ComboBoxStyle ??= new ComboBoxStyle();
        sheet.ListBoxStyle ??= new ListBoxStyle();
        sheet.TabControlStyle ??= new TabControlStyle();
        sheet.WindowStyle ??= new WindowStyle();
        sheet.DesktopStyle ??= new DesktopStyle();
        sheet.ScrollViewerStyle ??= new ScrollViewerStyle();
        sheet.SpinButtonStyle ??= new SpinButtonStyle();
        sheet.CheckBoxStyle ??= new ImageTextButtonStyle();
        sheet.RadioButtonStyle ??= new ImageTextButtonStyle();
        sheet.HorizontalSeparatorStyle ??= new SeparatorStyle();
        sheet.VerticalSeparatorStyle ??= new SeparatorStyle();

        sheet.Fonts["UI-Small"] = UiFonts.Small;
        sheet.Fonts["UI-Body"] = UiFonts.Body;
        sheet.Fonts["UI-Heading"] = UiFonts.Heading;
        sheet.Fonts["UI-Title"] = UiFonts.Title;
        sheet.Fonts["UI-Hero"] = UiFonts.Hero;
        sheet.Fonts["UI-Tiny"] = UiFonts.Tiny;

        ConfigureLabelStyle(sheet.LabelStyle, Colors.TextPrimary, UiFonts.Body);
        ConfigureLabelStyle(sheet.TooltipStyle, Colors.TextPrimary, UiFonts.Small);
        sheet.TooltipStyle.Background = CreateSolidBrush(Colors.Gunmetal);
        sheet.TooltipStyle.Border = CreateSolidBrush(Colors.SteelEdge);
        sheet.TooltipStyle.BorderThickness = new Thickness(BorderThickness.Normal);
        sheet.TooltipStyle.Padding = Padding.Small;

        ConfigureButtonStyle(sheet.ButtonStyle, ButtonTheme.Default);
        ConfigureTextBoxStyle(sheet.TextBoxStyle);
        ConfigureComboBoxStyle(sheet.ComboBoxStyle);
        ConfigureListBoxStyle(sheet.ListBoxStyle);
        ConfigureTabControlStyle(sheet.TabControlStyle);
        ConfigureWindowStyle(sheet.WindowStyle);
        ConfigureDesktopStyle(sheet.DesktopStyle);
        ConfigureScrollViewerStyle(sheet.ScrollViewerStyle);
        ConfigureSpinButtonStyle(sheet.SpinButtonStyle);
        ConfigureImageTextButtonStyle(sheet.CheckBoxStyle, ButtonTheme.Primary);
        ConfigureImageTextButtonStyle(sheet.RadioButtonStyle, ButtonTheme.Primary);
        ConfigureSeparatorStyle(sheet.HorizontalSeparatorStyle);
        ConfigureSeparatorStyle(sheet.VerticalSeparatorStyle);
    }

    private static void ConfigureLabelStyle(LabelStyle style, Color textColor, SpriteFontBase font)
    {
        style.Font = font;
        style.TextColor = textColor;
        style.DisabledTextColor = Colors.TextDisabled;
        style.OverTextColor = textColor;
        style.PressedTextColor = textColor;
    }

    private static void ConfigureButtonStyle(ButtonStyle style, ButtonTheme theme)
    {
        var visuals = GetButtonVisuals(theme);
        style.Background = visuals.Background;
        style.OverBackground = visuals.OverBackground;
        style.PressedBackground = visuals.PressedBackground;
        style.DisabledBackground = visuals.DisabledBackground;
        style.Border = CreateSolidBrush(Colors.SteelEdge);
        style.OverBorder = CreateSolidBrush(Colors.BorderFocus);
        style.FocusedBorder = CreateSolidBrush(Colors.BorderFocus);
        style.DisabledBorder = CreateSolidBrush(Colors.DisabledColor);
        style.BorderThickness = new Thickness(BorderThickness.Thin);
        style.Padding = Padding.Button;

        style.LabelStyle ??= new LabelStyle();
        ConfigureLabelStyle(style.LabelStyle, visuals.TextColor, UiFonts.Button);
        style.LabelStyle.OverTextColor = visuals.OverTextColor;
        style.LabelStyle.PressedTextColor = visuals.PressedTextColor;
        style.LabelStyle.DisabledTextColor = visuals.DisabledTextColor;
    }

    private static void ConfigureImageTextButtonStyle(ImageTextButtonStyle style, ButtonTheme theme)
    {
        var visuals = GetButtonVisuals(theme);
        style.Background = visuals.Background;
        style.OverBackground = visuals.OverBackground;
        style.PressedBackground = visuals.PressedBackground;
        style.DisabledBackground = visuals.DisabledBackground;
        style.Border = CreateSolidBrush(Colors.SteelEdge);
        style.OverBorder = CreateSolidBrush(Colors.BorderFocus);
        style.FocusedBorder = CreateSolidBrush(Colors.BorderFocus);
        style.DisabledBorder = CreateSolidBrush(Colors.DisabledColor);
        style.BorderThickness = new Thickness(BorderThickness.Thin);
        style.Padding = Padding.Medium;
        style.ImageTextSpacing = Spacing.Small;

        style.LabelStyle ??= new LabelStyle();
        ConfigureLabelStyle(style.LabelStyle, visuals.TextColor, UiFonts.Button);
        style.LabelStyle.OverTextColor = visuals.OverTextColor;
        style.LabelStyle.PressedTextColor = visuals.PressedTextColor;
        style.LabelStyle.DisabledTextColor = visuals.DisabledTextColor;
    }

    private static void ConfigureTextBoxStyle(TextBoxStyle style)
    {
        style.Background = AssetBrushes.TerminalPanel;
        style.OverBackground = AssetBrushes.TerminalPanel;
        style.FocusedBackground = AssetBrushes.TerminalPanel;
        style.Border = CreateSolidBrush(Colors.BorderNormal);
        style.OverBorder = CreateSolidBrush(Colors.BorderFocus);
        style.FocusedBorder = CreateSolidBrush(Colors.BorderFocus);
        style.DisabledBorder = CreateSolidBrush(Colors.DisabledColor);
        style.BorderThickness = new Thickness(BorderThickness.Thin);
        style.Padding = Padding.Input;
        style.Font = UiFonts.Body;
        style.MessageFont = UiFonts.Small;
        style.TextColor = Colors.TextPrimary;
        style.FocusedTextColor = Colors.TextPrimary;
        style.DisabledTextColor = Colors.TextDisabled;
        style.Selection = CreateSolidBrush(Colors.Selection);
        style.Height = Sizes.InputMediumHeight;
    }

    private static void ConfigureComboBoxStyle(ComboBoxStyle style)
    {
        style.Background = AssetBrushes.TerminalPanel;
        style.OverBackground = AssetBrushes.TerminalPanel;
        style.PressedBackground = AssetBrushes.TerminalPanel;
        style.FocusedBackground = AssetBrushes.TerminalPanel;
        style.Border = CreateSolidBrush(Colors.BorderNormal);
        style.OverBorder = CreateSolidBrush(Colors.BorderFocus);
        style.FocusedBorder = CreateSolidBrush(Colors.BorderFocus);
        style.DisabledBorder = CreateSolidBrush(Colors.DisabledColor);
        style.BorderThickness = new Thickness(BorderThickness.Thin);
        style.Padding = Padding.Input;
        style.Height = Sizes.InputMediumHeight;

        style.LabelStyle ??= new LabelStyle();
        ConfigureLabelStyle(style.LabelStyle, Colors.TextPrimary, UiFonts.Body);

        style.ListBoxStyle ??= new ListBoxStyle();
        ConfigureListBoxStyle(style.ListBoxStyle);
    }

    private static void ConfigureListBoxStyle(ListBoxStyle style)
    {
        style.Background = AssetBrushes.TerminalPanel;
        style.Border = CreateSolidBrush(Colors.BorderNormal);
        style.OverBorder = CreateSolidBrush(Colors.BorderFocus);
        style.FocusedBorder = CreateSolidBrush(Colors.BorderFocus);
        style.BorderThickness = new Thickness(BorderThickness.Thin);
        style.Padding = Padding.Medium;

        style.ListItemStyle ??= new ImageTextButtonStyle();
        style.ListItemStyle.Background = AssetBrushes.ListRowNormal;
        style.ListItemStyle.OverBackground = AssetBrushes.ListRowSelected;
        style.ListItemStyle.PressedBackground = AssetBrushes.ListRowSelected;
        style.ListItemStyle.Border = CreateSolidBrush(Colors.SteelEdge);
        style.ListItemStyle.OverBorder = CreateSolidBrush(Colors.BorderFocus);
        style.ListItemStyle.FocusedBorder = CreateSolidBrush(Colors.BorderFocus);
        style.ListItemStyle.DisabledBorder = CreateSolidBrush(Colors.DisabledColor);
        style.ListItemStyle.BorderThickness = new Thickness(BorderThickness.Thin);
        style.ListItemStyle.Padding = Padding.Medium;
        style.ListItemStyle.ImageTextSpacing = Spacing.Small;

        style.ListItemStyle.LabelStyle ??= new LabelStyle();
        ConfigureLabelStyle(style.ListItemStyle.LabelStyle, Colors.TextPrimary, UiFonts.Body);
        style.ListItemStyle.LabelStyle.OverTextColor = Colors.TextAccent;
        style.ListItemStyle.LabelStyle.PressedTextColor = Colors.TextAccent;
    }

    private static void ConfigureTabControlStyle(TabControlStyle style)
    {
        style.Background = AssetBrushes.TerminalPanel;
        style.Border = CreateSolidBrush(Colors.SteelEdge);
        style.BorderThickness = new Thickness(BorderThickness.Thin);
        style.Padding = Padding.Medium;
        style.HeaderSpacing = Spacing.Medium;
        style.ButtonSpacing = Spacing.Medium;

        style.ContentStyle ??= new WidgetStyle();
        style.ContentStyle.Background = AssetBrushes.WindowFrame;
        style.ContentStyle.Border = CreateSolidBrush(Colors.SteelEdge);
        style.ContentStyle.BorderThickness = new Thickness(BorderThickness.Thin);
        style.ContentStyle.Padding = Padding.Medium;

        style.TabItemStyle ??= new ImageTextButtonStyle();
        style.TabItemStyle.Background = AssetBrushes.TabNormal;
        style.TabItemStyle.OverBackground = AssetBrushes.TabSelected;
        style.TabItemStyle.PressedBackground = AssetBrushes.TabSelected;
        style.TabItemStyle.Border = CreateSolidBrush(Colors.SteelEdge);
        style.TabItemStyle.OverBorder = CreateSolidBrush(Colors.BorderFocus);
        style.TabItemStyle.FocusedBorder = CreateSolidBrush(Colors.BorderFocus);
        style.TabItemStyle.DisabledBorder = CreateSolidBrush(Colors.DisabledColor);
        style.TabItemStyle.BorderThickness = new Thickness(BorderThickness.Thin);
        style.TabItemStyle.Padding = new Thickness(18, 10);
        style.TabItemStyle.ImageTextSpacing = Spacing.Small;

        style.TabItemStyle.LabelStyle ??= new LabelStyle();
        ConfigureLabelStyle(style.TabItemStyle.LabelStyle, Colors.TextPrimary, UiFonts.Body);
        style.TabItemStyle.LabelStyle.OverTextColor = Colors.TextAccent;
        style.TabItemStyle.LabelStyle.PressedTextColor = Colors.TextAccent;
    }

    private static void ConfigureWindowStyle(WindowStyle style)
    {
        style.Background = AssetBrushes.WindowFrame;
        style.Border = CreateSolidBrush(Colors.SteelEdge);
        style.OverBorder = CreateSolidBrush(Colors.BorderFocus);
        style.FocusedBorder = CreateSolidBrush(Colors.BorderFocus);
        style.BorderThickness = new Thickness(BorderThickness.Thin);
        style.Padding = Padding.Large;

        style.TitleStyle ??= new LabelStyle();
        ConfigureLabelStyle(style.TitleStyle, Colors.TextWarning, UiFonts.Heading);
    }

    private static void ConfigureDesktopStyle(DesktopStyle style)
    {
        style.Background = AssetBrushes.Backdrop;
    }

    private static void ConfigureScrollViewerStyle(ScrollViewerStyle style)
    {
        style.Background = AssetBrushes.TerminalPanel;
        style.Border = CreateSolidBrush(Colors.BorderNormal);
        style.BorderThickness = new Thickness(BorderThickness.Thin);
        style.Padding = Padding.Medium;
    }

    private static void ConfigureSpinButtonStyle(SpinButtonStyle style)
    {
        style.Background = AssetBrushes.TerminalPanel;
        style.Border = CreateSolidBrush(Colors.BorderNormal);
        style.OverBorder = CreateSolidBrush(Colors.BorderFocus);
        style.FocusedBorder = CreateSolidBrush(Colors.BorderFocus);
        style.BorderThickness = new Thickness(BorderThickness.Thin);
        style.Padding = Padding.Input;
        style.TextBoxStyle ??= new TextBoxStyle();
        ConfigureTextBoxStyle(style.TextBoxStyle);
    }

    private static void ConfigureSeparatorStyle(SeparatorStyle style)
    {
        style.Background = CreateSolidBrush(Colors.BorderNormal);
        style.Thickness = BorderThickness.Thin;
    }

    private static IBrush CreateTextureBrush(Texture2D texture)
    {
        return new TextureRegion(texture);
    }

    private static IBrush CreateNinePatchBrush(Texture2D texture, int border)
    {
        return new NinePatchRegion(texture, texture.Bounds, new Thickness(border));
    }

    public static SolidBrush CreateSolidBrush(Color color)
    {
        return new SolidBrush(color);
    }

    public static void ApplyDesktopTheme(Desktop desktop)
    {
        desktop.Background = AssetBrushes.Backdrop;
    }

    public static void ApplyWindowTheme(Window window, bool highlighted = false)
    {
        window.Background = AssetBrushes.WindowFrame;
        window.Border = CreateSolidBrush(highlighted ? Colors.BorderFocus : Colors.SteelEdge);
        window.BorderThickness = new Thickness(BorderThickness.Thin);
        window.Padding = Padding.Large;
        window.TitleTextColor = Colors.TextWarning;
        window.TitleFont = UiFonts.Heading;
    }

    public static void ApplyButtonTheme(MyraButton button, ButtonTheme theme = ButtonTheme.Default)
    {
        var visuals = GetButtonVisuals(theme);
        button.Background = visuals.Background;
        button.OverBackground = visuals.OverBackground;
        button.PressedBackground = visuals.PressedBackground;
        button.DisabledBackground = visuals.DisabledBackground;
        button.Border = CreateSolidBrush(Colors.SteelEdge);
        button.OverBorder = CreateSolidBrush(Colors.BorderFocus);
        button.FocusedBorder = CreateSolidBrush(Colors.BorderFocus);
        button.BorderThickness = new Thickness(BorderThickness.Thin);
        button.Padding = Padding.Button;
        ApplyButtonContentTheme(button.Content, visuals);
    }

    public static void ApplyPanelTheme(Panel panel, PanelTheme theme = PanelTheme.Default)
    {
        switch (theme)
        {
            case PanelTheme.Frame:
                panel.Background = AssetBrushes.WindowFrame;
                panel.Border = CreateSolidBrush(Colors.SteelEdge);
                panel.BorderThickness = new Thickness(BorderThickness.Thin);
                panel.Padding = Padding.XLarge;
                break;

            case PanelTheme.AccentFrame:
                panel.Background = AssetBrushes.WindowFrame;
                panel.Border = CreateSolidBrush(Colors.BorderFocus);
                panel.BorderThickness = new Thickness(BorderThickness.Thin);
                panel.Padding = Padding.XLarge;
                break;

            case PanelTheme.Resource:
                panel.Background = AssetBrushes.TerminalPanel;
                panel.Border = CreateSolidBrush(Colors.BorderFocus);
                panel.BorderThickness = new Thickness(BorderThickness.Thin);
                panel.Padding = Padding.Large;
                break;

            case PanelTheme.Hero:
                panel.Background = AssetBrushes.HeaderPlate;
                panel.Border = CreateSolidBrush(Colors.TextWarning);
                panel.BorderThickness = new Thickness(BorderThickness.Thin);
                panel.Padding = Padding.Large;
                break;

            case PanelTheme.Dark:
                panel.Background = AssetBrushes.TerminalPanel;
                panel.Border = CreateSolidBrush(Colors.BorderNormal);
                panel.BorderThickness = new Thickness(BorderThickness.Thin);
                panel.Padding = Padding.Large;
                break;

            default:
                panel.Background = AssetBrushes.TerminalPanel;
                panel.Border = CreateSolidBrush(Colors.BorderNormal);
                panel.BorderThickness = new Thickness(BorderThickness.Thin);
                panel.Padding = Padding.Large;
                break;
        }
    }

    public static void ApplyLabelTheme(Label label, LabelTheme theme = LabelTheme.Default)
    {
        switch (theme)
        {
            case LabelTheme.Title:
                label.Font = UiFonts.Title;
                label.TextColor = Colors.TextAccent;
                label.Scale = FontScale.Normal;
                break;

            case LabelTheme.Subtitle:
                label.Font = UiFonts.Heading;
                label.TextColor = Colors.TextPrimary;
                label.Scale = FontScale.Normal;
                break;

            case LabelTheme.Heading:
                label.Font = UiFonts.Heading;
                label.TextColor = Colors.TextWarning;
                label.Scale = FontScale.Normal;
                break;

            case LabelTheme.Secondary:
                label.Font = UiFonts.Body;
                label.TextColor = Colors.TextSecondary;
                label.Scale = FontScale.Normal;
                break;

            case LabelTheme.Small:
                label.Font = UiFonts.Small;
                label.TextColor = Colors.TextSecondary;
                label.Scale = FontScale.Normal;
                break;

            case LabelTheme.Warning:
                label.Font = UiFonts.Body;
                label.TextColor = Colors.TextWarning;
                label.Scale = FontScale.Normal;
                break;

            case LabelTheme.Error:
                label.Font = UiFonts.Body;
                label.TextColor = Colors.TextError;
                label.Scale = FontScale.Normal;
                break;

            case LabelTheme.Success:
                label.Font = UiFonts.Body;
                label.TextColor = Colors.TextSuccess;
                label.Scale = FontScale.Normal;
                break;

            default:
                label.Font = UiFonts.Body;
                label.TextColor = Colors.TextPrimary;
                label.Scale = FontScale.Normal;
                break;
        }
    }

    public static void ApplyTextBoxTheme(TextBox textBox)
    {
        textBox.Background = AssetBrushes.TerminalPanel;
        textBox.OverBackground = AssetBrushes.TerminalPanel;
        textBox.FocusedBackground = AssetBrushes.TerminalPanel;
        textBox.Border = CreateSolidBrush(Colors.BorderNormal);
        textBox.OverBorder = CreateSolidBrush(Colors.BorderFocus);
        textBox.FocusedBorder = CreateSolidBrush(Colors.BorderFocus);
        textBox.BorderThickness = new Thickness(BorderThickness.Thin);
        textBox.TextColor = Colors.TextPrimary;
        textBox.Font = UiFonts.Body;
        textBox.Padding = Padding.Input;
        textBox.Height = Sizes.InputMediumHeight;
    }

    public static void ApplyComboBoxTheme(MyraComboBox comboBox)
    {
        comboBox.Background = AssetBrushes.TerminalPanel;
        comboBox.OverBackground = AssetBrushes.TerminalPanel;
        comboBox.FocusedBackground = AssetBrushes.TerminalPanel;
        comboBox.Border = CreateSolidBrush(Colors.BorderNormal);
        comboBox.OverBorder = CreateSolidBrush(Colors.BorderFocus);
        comboBox.FocusedBorder = CreateSolidBrush(Colors.BorderFocus);
        comboBox.BorderThickness = new Thickness(BorderThickness.Thin);
        comboBox.Padding = Padding.Input;
        comboBox.Height = Sizes.InputMediumHeight;
    }

    public static void ApplyCheckButtonTheme(CheckButton checkButton)
    {
        ApplyCheckButtonBaseTheme(checkButton, ButtonTheme.Primary);
        checkButton.Width = Sizes.CheckboxSize;
        checkButton.Height = Sizes.CheckboxSize;
        checkButton.Padding = new Thickness(0);
    }

    private static void ApplyCheckButtonBaseTheme(CheckButton button, ButtonTheme theme)
    {
        var visuals = GetButtonVisuals(theme);
        button.Background = visuals.Background;
        button.OverBackground = visuals.OverBackground;
        button.PressedBackground = visuals.PressedBackground;
        button.DisabledBackground = visuals.DisabledBackground;
        button.Border = CreateSolidBrush(Colors.SteelEdge);
        button.OverBorder = CreateSolidBrush(Colors.BorderFocus);
        button.FocusedBorder = CreateSolidBrush(Colors.BorderFocus);
        button.BorderThickness = new Thickness(BorderThickness.Thin);
    }

    public static void ApplySpinButtonTheme(SpinButton spinButton)
    {
        spinButton.Background = AssetBrushes.TerminalPanel;
        spinButton.Border = CreateSolidBrush(Colors.BorderNormal);
        spinButton.OverBorder = CreateSolidBrush(Colors.BorderFocus);
        spinButton.FocusedBorder = CreateSolidBrush(Colors.BorderFocus);
        spinButton.BorderThickness = new Thickness(BorderThickness.Thin);
        spinButton.Padding = Padding.Input;
    }

    private static void ApplyButtonContentTheme(Widget? content, ButtonVisuals visuals)
    {
        if (content is Label label)
        {
            label.Font = UiFonts.Button;
            label.TextColor = visuals.TextColor;
            label.OverTextColor = visuals.OverTextColor;
            label.DisabledTextColor = visuals.DisabledTextColor;
            label.Scale = Vector2.One;
            label.HorizontalAlignment = HorizontalAlignment.Center;
            label.VerticalAlignment = VerticalAlignment.Center;
        }
    }

    private static ButtonVisuals GetButtonVisuals(ButtonTheme theme)
    {
        return theme switch
        {
            ButtonTheme.Primary => new ButtonVisuals(
                AssetBrushes.ButtonPrimaryNormal,
                AssetBrushes.ButtonPrimaryHover,
                AssetBrushes.ButtonPrimaryPressed,
                AssetBrushes.ButtonPrimaryDisabled,
                Colors.TextPrimary,
                Colors.TextAccent,
                Colors.TextPrimary,
                Colors.TextDisabled),
            ButtonTheme.Danger => new ButtonVisuals(
                AssetBrushes.ButtonDangerNormal,
                AssetBrushes.ButtonDangerHover,
                AssetBrushes.ButtonDangerPressed,
                AssetBrushes.ButtonDangerDisabled,
                Colors.TextPrimary,
                Colors.TextWarning,
                Colors.TextPrimary,
                Colors.TextDisabled),
            ButtonTheme.Success => new ButtonVisuals(
                AssetBrushes.ButtonPrimaryNormal,
                AssetBrushes.ButtonPrimaryHover,
                AssetBrushes.ButtonPrimaryPressed,
                AssetBrushes.ButtonPrimaryDisabled,
                Colors.TextPrimary,
                Colors.TextAccent,
                Colors.TextPrimary,
                Colors.TextDisabled),
            ButtonTheme.Hero => new ButtonVisuals(
                AssetBrushes.ButtonSecondaryNormal,
                AssetBrushes.ButtonSecondaryHover,
                AssetBrushes.ButtonSecondaryPressed,
                AssetBrushes.ButtonSecondaryDisabled,
                Colors.TextWarning,
                Colors.TextAccent,
                Colors.TextWarning,
                Colors.TextDisabled),
            _ => new ButtonVisuals(
                AssetBrushes.ButtonSecondaryNormal,
                AssetBrushes.ButtonSecondaryHover,
                AssetBrushes.ButtonSecondaryPressed,
                AssetBrushes.ButtonSecondaryDisabled,
                Colors.TextPrimary,
                Colors.TextAccent,
                Colors.TextPrimary,
                Colors.TextDisabled)
        };
    }

    private static IBrush ResolveBrush(Func<LoadedAssets, IBrush> selector, Color fallbackColor)
    {
        return _assets != null ? selector(_assets) : CreateSolidBrush(fallbackColor);
    }

    private static SpriteFontBase ResolveFont(float size)
    {
        return _fontSystem?.GetFont(size) ?? Stylesheet.LabelStyle.Font;
    }

    private static int ScaleMetric(int value, float factor, int minimum = 1)
    {
        return Math.Max(minimum, (int)MathF.Round(value * factor));
    }

    private static Thickness CreateThickness(int uniform)
    {
        return new Thickness(uniform);
    }

    private static Thickness CreateThickness(int horizontal, int vertical)
    {
        return new Thickness(horizontal, vertical);
    }

    private static Color MultiplyRgb(Color color, float factor)
    {
        return new Color(
            (byte)Math.Clamp((int)MathF.Round(color.R * factor), 0, 255),
            (byte)Math.Clamp((int)MathF.Round(color.G * factor), 0, 255),
            (byte)Math.Clamp((int)MathF.Round(color.B * factor), 0, 255),
            color.A);
    }

    private static Color Blend(Color from, Color to, float amount, byte? alpha = null)
    {
        return new Color(
            (byte)Math.Clamp((int)MathF.Round(MathHelper.Lerp(from.R, to.R, amount)), 0, 255),
            (byte)Math.Clamp((int)MathF.Round(MathHelper.Lerp(from.G, to.G, amount)), 0, 255),
            (byte)Math.Clamp((int)MathF.Round(MathHelper.Lerp(from.B, to.B, amount)), 0, 255),
            alpha ?? (byte)Math.Clamp((int)MathF.Round(MathHelper.Lerp(from.A, to.A, amount)), 0, 255));
    }

    public static class AssetBrushes
    {
        public static IBrush Backdrop => ResolveBrush(a => a.Backdrop, Colors.PrimaryDark);
        public static IBrush ViewportFrame => ResolveBrush(a => a.ViewportFrame, Colors.PrimaryMedium);
        public static IBrush WindowFrame => ResolveBrush(a => a.WindowFrame, Colors.PrimaryMedium);
        public static IBrush TerminalPanel => ResolveBrush(a => a.TerminalPanel, Colors.PrimaryMedium);
        public static IBrush HeaderPlate => ResolveBrush(a => a.HeaderPlate, Colors.PrimaryLight);
        public static IBrush ButtonPrimaryNormal => ResolveBrush(a => a.ButtonPrimaryNormal, Colors.AccentBlue);
        public static IBrush ButtonPrimaryHover => ResolveBrush(a => a.ButtonPrimaryHover, Colors.HoverColor);
        public static IBrush ButtonPrimaryPressed => ResolveBrush(a => a.ButtonPrimaryPressed, Colors.PressedColor);
        public static IBrush ButtonPrimaryDisabled => ResolveBrush(a => a.ButtonPrimaryDisabled, Colors.DisabledColor);
        public static IBrush ButtonSecondaryNormal => ResolveBrush(a => a.ButtonSecondaryNormal, Colors.PrimaryLight);
        public static IBrush ButtonSecondaryHover => ResolveBrush(a => a.ButtonSecondaryHover, Colors.PrimaryLight);
        public static IBrush ButtonSecondaryPressed => ResolveBrush(a => a.ButtonSecondaryPressed, Colors.PrimaryMedium);
        public static IBrush ButtonSecondaryDisabled => ResolveBrush(a => a.ButtonSecondaryDisabled, Colors.DisabledColor);
        public static IBrush ButtonDangerNormal => ResolveBrush(a => a.ButtonDangerNormal, Colors.TextError);
        public static IBrush ButtonDangerHover => ResolveBrush(a => a.ButtonDangerHover, Colors.TextError);
        public static IBrush ButtonDangerPressed => ResolveBrush(a => a.ButtonDangerPressed, Colors.TextError);
        public static IBrush ButtonDangerDisabled => ResolveBrush(a => a.ButtonDangerDisabled, Colors.DisabledColor);
        public static IBrush TabNormal => ResolveBrush(a => a.TabNormal, Colors.PrimaryMedium);
        public static IBrush TabSelected => ResolveBrush(a => a.TabSelected, Colors.AccentBlue);
        public static IBrush ListRowNormal => ResolveBrush(a => a.ListRowNormal, Colors.PrimaryMedium);
        public static IBrush ListRowSelected => ResolveBrush(a => a.ListRowSelected, Colors.AccentBlue);
    }

    public static class UiFonts
    {
        public static SpriteFontBase Tiny => ResolveFont(_themeProfile.TinyFontSize);
        public static SpriteFontBase Small => ResolveFont(_themeProfile.SmallFontSize);
        public static SpriteFontBase Body => ResolveFont(_themeProfile.BodyFontSize);
        public static SpriteFontBase Button => ResolveFont(_themeProfile.ButtonFontSize);
        public static SpriteFontBase Heading => ResolveFont(_themeProfile.HeadingFontSize);
        public static SpriteFontBase Title => ResolveFont(_themeProfile.TitleFontSize);
        public static SpriteFontBase Hero => ResolveFont(_themeProfile.HeroFontSize);
    }

    public static class Colors
    {
        public static Color HullBlack => _themeProfile.HullBlack;
        public static Color Gunmetal => _themeProfile.Gunmetal;
        public static Color GunmetalRaised => _themeProfile.GunmetalRaised;
        public static Color SteelEdge => _themeProfile.SteelEdge;
        public static Color SteelShadow => _themeProfile.SteelShadow;
        public static Color TerminalBlack => _themeProfile.TerminalBlack;
        public static Color CRTGreenDim => _themeProfile.CrtGreenDim;
        public static Color PhosphorGreen => _themeProfile.PhosphorGreen;
        public static Color PhosphorGreenBright => _themeProfile.PhosphorGreenBright;
        public static Color AmberData => _themeProfile.WarningBright;
        public static Color AmberMuted => _themeProfile.WarningMuted;
        public static Color RedAlert => _themeProfile.RedAlert;
        public static Color NeutralSilver => _themeProfile.NeutralSilver;
        public static Color NeutralSlate => _themeProfile.NeutralSlate;
        public static Color Selection => _themeProfile.Selection;

        public static Color PrimaryDark => HullBlack;
        public static Color PrimaryMedium => Gunmetal;
        public static Color PrimaryLight => GunmetalRaised;

        public static Color AccentCyan => PhosphorGreen;
        public static Color AccentBlue => CRTGreenDim;
        public static Color AccentDarkBlue => TerminalBlack;

        public static Color HoverColor => _themeProfile.HoverColor;
        public static Color PressedColor => _themeProfile.PressedColor;
        public static Color DisabledColor => _themeProfile.DisabledColor;

        public static Color TextPrimary => _themeProfile.TextPrimary;
        public static Color TextSecondary => _themeProfile.TextSecondary;
        public static Color TextDisabled => _themeProfile.TextDisabled;
        public static Color TextAccent => _themeProfile.TextAccent;
        public static Color TextWarning => _themeProfile.TextWarning;
        public static Color TextError => RedAlert;
        public static Color TextSuccess => _themeProfile.TextSuccess;

        public static Color BorderNormal => _themeProfile.BorderNormal;
        public static Color BorderFocus => _themeProfile.BorderFocus;
        public static Color BorderHover => _themeProfile.BorderHover;

        public static Color BackgroundDark => _themeProfile.BackgroundDark;
        public static Color BackgroundMedium => _themeProfile.BackgroundMedium;
        public static Color BackgroundLight => _themeProfile.BackgroundLight;

        public static Color PopulationColor => PhosphorGreenBright;
        public static Color MetalColor => _themeProfile.MetalColor;
        public static Color FuelColor => AmberData;

        public static Color HeroColor => _themeProfile.HeroColor;
        public static Color AIEasyColor => _themeProfile.AiEasyColor;
        public static Color AIMediumColor => _themeProfile.AiMediumColor;
        public static Color AIHardColor => _themeProfile.AiHardColor;

        public static Color DropdownOptionHover => _themeProfile.DropdownOptionHover;
        public static Color DropdownBackground => _themeProfile.DropdownBackground;

        public static Color SlotPanelNormal => _themeProfile.SlotPanelNormal;
        public static Color SlotPanelReady => _themeProfile.SlotPanelReady;
    }

    public static class Spacing
    {
        public static int XSmall => _themeProfile.XSmallSpacing;
        public static int Small => _themeProfile.SmallSpacing;
        public static int Medium => _themeProfile.MediumSpacing;
        public static int Large => _themeProfile.LargeSpacing;
        public static int XLarge => _themeProfile.XLargeSpacing;
        public static int XXLarge => _themeProfile.XXLargeSpacing;
    }

    public static class BorderThickness
    {
        public const int Thin = 1;
        public const int Normal = 2;
        public const int Thick = 3;
    }

    public static class Padding
    {
        public static Thickness Small => _themeProfile.SmallPadding;
        public static Thickness Medium => _themeProfile.MediumPadding;
        public static Thickness Large => _themeProfile.LargePadding;
        public static Thickness XLarge => _themeProfile.XLargePadding;

        public static Thickness SmallVertical => _themeProfile.SmallVerticalPadding;
        public static Thickness MediumVertical => _themeProfile.MediumVerticalPadding;
        public static Thickness SmallHorizontal => _themeProfile.SmallHorizontalPadding;
        public static Thickness MediumHorizontal => _themeProfile.MediumHorizontalPadding;
        public static Thickness Button => _themeProfile.ButtonPadding;
        public static Thickness Input => _themeProfile.InputPadding;
        public static Thickness Panel => _themeProfile.PanelPadding;
        public static Thickness ViewportFrame => _themeProfile.ViewportFramePadding;
        public static Thickness HeaderPlate => _themeProfile.HeaderPlatePadding;
        public static Thickness Badge => _themeProfile.BadgePadding;
    }

    public static class FontScale
    {
        public static Vector2 Tiny => Vector2.One;
        public static Vector2 Small => Vector2.One;
        public static Vector2 SmallMedium => Vector2.One;
        public static Vector2 Normal => Vector2.One;
        public static Vector2 Medium => Vector2.One;
        public static Vector2 Large => Vector2.One;
        public static Vector2 XLarge => Vector2.One;
        public static Vector2 XXLarge => Vector2.One;
        public static Vector2 Title => Vector2.One;
    }

    public static class Sizes
    {
        public static int ButtonSmallWidth => 96;
        public static int ButtonSmallHeight => _themeProfile.ButtonSmallHeight;
        public static int ButtonMediumWidth => 170;
        public static int ButtonMediumHeight => _themeProfile.ButtonMediumHeight;
        public static int ButtonLargeWidth => 260;
        public static int ButtonLargeHeight => _themeProfile.ButtonLargeHeight;

        public static int InputSmallHeight => _themeProfile.InputSmallHeight;
        public static int InputMediumHeight => _themeProfile.InputMediumHeight;
        public static int InputLargeHeight => _themeProfile.InputLargeHeight;

        public static int IconSmall => 16;
        public static int IconMedium => 18;
        public static int IconLarge => 24;

        public static int PanelSmallWidth => 300;
        public static int PanelMediumWidth => 420;
        public static int PanelLargeWidth => 560;
        public static int PanelXLargeWidth => 820;

        public static int CheckboxSize => _themeProfile.CheckboxSize;
        public static int BadgeHeight => _themeProfile.BadgeHeight;
    }

    public static class Timing
    {
        public const double CursorBlinkInterval = 500.0;
        public const double TooltipDelay = 1000.0;
        public const double AnimationShort = 0.15;
        public const double AnimationMedium = 0.3;
        public const double AnimationLong = 0.5;
    }

    public enum ButtonTheme
    {
        Default,
        Primary,
        Danger,
        Success,
        Hero
    }

    public enum PanelTheme
    {
        Default,
        Frame,
        AccentFrame,
        Resource,
        Hero,
        Dark
    }

    public enum LabelTheme
    {
        Default,
        Title,
        Subtitle,
        Heading,
        Secondary,
        Small,
        Warning,
        Error,
        Success
    }

    private sealed class ThemeProfile
    {
        public required UiThemeSettings Source { get; init; }
        public required float TinyFontSize { get; init; }
        public required float SmallFontSize { get; init; }
        public required float BodyFontSize { get; init; }
        public required float ButtonFontSize { get; init; }
        public required float HeadingFontSize { get; init; }
        public required float TitleFontSize { get; init; }
        public required float HeroFontSize { get; init; }

        public required int XSmallSpacing { get; init; }
        public required int SmallSpacing { get; init; }
        public required int MediumSpacing { get; init; }
        public required int LargeSpacing { get; init; }
        public required int XLargeSpacing { get; init; }
        public required int XXLargeSpacing { get; init; }

        public required Thickness SmallPadding { get; init; }
        public required Thickness MediumPadding { get; init; }
        public required Thickness LargePadding { get; init; }
        public required Thickness XLargePadding { get; init; }
        public required Thickness SmallVerticalPadding { get; init; }
        public required Thickness MediumVerticalPadding { get; init; }
        public required Thickness SmallHorizontalPadding { get; init; }
        public required Thickness MediumHorizontalPadding { get; init; }
        public required Thickness ButtonPadding { get; init; }
        public required Thickness InputPadding { get; init; }
        public required Thickness PanelPadding { get; init; }
        public required Thickness ViewportFramePadding { get; init; }
        public required Thickness HeaderPlatePadding { get; init; }
        public required Thickness BadgePadding { get; init; }

        public required int ButtonSmallHeight { get; init; }
        public required int ButtonMediumHeight { get; init; }
        public required int ButtonLargeHeight { get; init; }
        public required int InputSmallHeight { get; init; }
        public required int InputMediumHeight { get; init; }
        public required int InputLargeHeight { get; init; }
        public required int CheckboxSize { get; init; }
        public required int BadgeHeight { get; init; }

        public required Color HullBlack { get; init; }
        public required Color Gunmetal { get; init; }
        public required Color GunmetalRaised { get; init; }
        public required Color SteelEdge { get; init; }
        public required Color SteelShadow { get; init; }
        public required Color TerminalBlack { get; init; }
        public required Color CrtGreenDim { get; init; }
        public required Color PhosphorGreen { get; init; }
        public required Color PhosphorGreenBright { get; init; }
        public required Color WarningBright { get; init; }
        public required Color WarningMuted { get; init; }
        public required Color RedAlert { get; init; }
        public required Color NeutralSilver { get; init; }
        public required Color NeutralSlate { get; init; }
        public required Color Selection { get; init; }
        public required Color HoverColor { get; init; }
        public required Color PressedColor { get; init; }
        public required Color DisabledColor { get; init; }
        public required Color TextPrimary { get; init; }
        public required Color TextSecondary { get; init; }
        public required Color TextDisabled { get; init; }
        public required Color TextAccent { get; init; }
        public required Color TextWarning { get; init; }
        public required Color TextSuccess { get; init; }
        public required Color BorderNormal { get; init; }
        public required Color BorderFocus { get; init; }
        public required Color BorderHover { get; init; }
        public required Color BackgroundDark { get; init; }
        public required Color BackgroundMedium { get; init; }
        public required Color BackgroundLight { get; init; }
        public required Color MetalColor { get; init; }
        public required Color HeroColor { get; init; }
        public required Color AiEasyColor { get; init; }
        public required Color AiMediumColor { get; init; }
        public required Color AiHardColor { get; init; }
        public required Color DropdownOptionHover { get; init; }
        public required Color DropdownBackground { get; init; }
        public required Color SlotPanelNormal { get; init; }
        public required Color SlotPanelReady { get; init; }

        public static ThemeProfile FromSettings(UiThemeSettings? rawSettings)
        {
            var settings = rawSettings?.Clone() ?? new UiThemeSettings();
            settings.Normalize();

            var accent = ResolveAccentPalette(settings.AccentColor);
            var warning = ResolveWarningColor(settings.WarningColor);
            var fontProfile = ResolveFontProfile(settings.FontStyle);

            float fontScale = settings.FontScalePercent / 100f;
            float paddingScale = settings.PaddingScalePercent / 100f;
            float frameScale = settings.FramePaddingPercent / 100f;
            float contrastScale = settings.ContrastPercent / 100f;
            float sizeScale = MathF.Max(paddingScale, fontScale * 0.94f);

            var hullBlack = new Color(6, 8, 12);
            var gunmetal = new Color(22, 24, 27);
            var gunmetalRaised = new Color(40, 44, 48);
            var steelEdge = Blend(new Color(124, 126, 128), accent.Base, 0.18f);
            var steelShadow = new Color(10, 12, 14);
            var terminalBlack = new Color(10, 14, 10);
            var neutralSilver = MultiplyRgb(new Color(244, 246, 238), contrastScale);
            var neutralSlate = MultiplyRgb(new Color(168, 174, 166), 0.95f + ((contrastScale - 1f) * 0.55f));
            var disabled = Blend(gunmetalRaised, new Color(92, 96, 92), 0.55f);
            var textSecondary = MultiplyRgb(Blend(warning, new Color(184, 176, 156), 0.42f), 0.95f + ((contrastScale - 1f) * 0.7f));
            var textWarning = MultiplyRgb(warning, contrastScale);
            var textAccent = MultiplyRgb(accent.Bright, contrastScale);
            var borderFocus = Blend(accent.Bright, Color.White, 0.12f);
            var backgroundDark = new Color(0, 0, 0, 224);
            var backgroundMedium = Blend(new Color(10, 14, 16), terminalBlack, 0.18f, 216);
            var backgroundLight = Blend(new Color(20, 26, 28), gunmetalRaised, 0.24f, 180);

            return new ThemeProfile
            {
                Source = settings,
                TinyFontSize = 12f * fontScale * fontProfile.Tiny,
                SmallFontSize = 15f * fontScale * fontProfile.Small,
                BodyFontSize = 18f * fontScale * fontProfile.Body,
                ButtonFontSize = 20f * fontScale * fontProfile.Button,
                HeadingFontSize = 24f * fontScale * fontProfile.Heading,
                TitleFontSize = 34f * fontScale * fontProfile.Title,
                HeroFontSize = 46f * fontScale * fontProfile.Hero,

                XSmallSpacing = ScaleMetric(4, paddingScale),
                SmallSpacing = ScaleMetric(10, paddingScale),
                MediumSpacing = ScaleMetric(16, paddingScale),
                LargeSpacing = ScaleMetric(24, paddingScale),
                XLargeSpacing = ScaleMetric(32, paddingScale),
                XXLargeSpacing = ScaleMetric(40, paddingScale),

                SmallPadding = CreateThickness(ScaleMetric(8, paddingScale)),
                MediumPadding = CreateThickness(ScaleMetric(14, paddingScale)),
                LargePadding = CreateThickness(ScaleMetric(22, paddingScale)),
                XLargePadding = CreateThickness(ScaleMetric(30, paddingScale)),
                SmallVerticalPadding = CreateThickness(0, ScaleMetric(8, paddingScale)),
                MediumVerticalPadding = CreateThickness(0, ScaleMetric(14, paddingScale)),
                SmallHorizontalPadding = CreateThickness(ScaleMetric(8, paddingScale), 0),
                MediumHorizontalPadding = CreateThickness(ScaleMetric(14, paddingScale), 0),
                ButtonPadding = CreateThickness(ScaleMetric(24, paddingScale), ScaleMetric(16, paddingScale)),
                InputPadding = CreateThickness(ScaleMetric(16, paddingScale), ScaleMetric(12, paddingScale)),
                PanelPadding = CreateThickness(ScaleMetric(24, paddingScale)),
                ViewportFramePadding = CreateThickness(ScaleMetric(28, frameScale), ScaleMetric(24, frameScale)),
                HeaderPlatePadding = CreateThickness(ScaleMetric(26, frameScale), ScaleMetric(22, frameScale)),
                BadgePadding = CreateThickness(ScaleMetric(12, paddingScale), ScaleMetric(8, paddingScale)),

                ButtonSmallHeight = ScaleMetric(40, sizeScale),
                ButtonMediumHeight = ScaleMetric(56, sizeScale),
                ButtonLargeHeight = ScaleMetric(66, sizeScale),
                InputSmallHeight = ScaleMetric(36, sizeScale),
                InputMediumHeight = ScaleMetric(50, sizeScale),
                InputLargeHeight = ScaleMetric(58, sizeScale),
                CheckboxSize = ScaleMetric(22, paddingScale),
                BadgeHeight = ScaleMetric(40, sizeScale),

                HullBlack = hullBlack,
                Gunmetal = gunmetal,
                GunmetalRaised = gunmetalRaised,
                SteelEdge = steelEdge,
                SteelShadow = steelShadow,
                TerminalBlack = terminalBlack,
                CrtGreenDim = accent.Dim,
                PhosphorGreen = accent.Base,
                PhosphorGreenBright = accent.Bright,
                WarningBright = warning,
                WarningMuted = Blend(warning, new Color(160, 150, 132), 0.46f),
                RedAlert = new Color(206, 92, 76),
                NeutralSilver = neutralSilver,
                NeutralSlate = neutralSlate,
                Selection = new Color(accent.Dim.R, accent.Dim.G, accent.Dim.B, (byte)170),
                HoverColor = Blend(accent.Dim, accent.Bright, 0.30f),
                PressedColor = Blend(terminalBlack, accent.Dim, 0.34f),
                DisabledColor = disabled,
                TextPrimary = neutralSilver,
                TextSecondary = textSecondary,
                TextDisabled = neutralSlate,
                TextAccent = textAccent,
                TextWarning = textWarning,
                TextSuccess = textAccent,
                BorderNormal = MultiplyRgb(steelEdge, 0.96f + ((contrastScale - 1f) * 0.35f)),
                BorderFocus = borderFocus,
                BorderHover = accent.Base,
                BackgroundDark = backgroundDark,
                BackgroundMedium = backgroundMedium,
                BackgroundLight = backgroundLight,
                MetalColor = Blend(new Color(184, 188, 184), steelEdge, 0.22f),
                HeroColor = Blend(warning, new Color(198, 170, 90), 0.35f),
                AiEasyColor = Blend(accent.Bright, new Color(110, 188, 86), 0.35f),
                AiMediumColor = warning,
                AiHardColor = new Color(206, 92, 76),
                DropdownOptionHover = Blend(terminalBlack, accent.Dim, 0.45f),
                DropdownBackground = Blend(terminalBlack, gunmetal, 0.18f),
                SlotPanelNormal = Blend(new Color(18, 22, 22), gunmetalRaised, 0.15f, 210),
                SlotPanelReady = Blend(terminalBlack, accent.Dim, 0.36f, 230)
            };
        }

        private static AccentPalette ResolveAccentPalette(string accentColor)
        {
            return accentColor switch
            {
                "Ice Cyan" => new AccentPalette(new Color(48, 96, 118), new Color(92, 172, 210), new Color(168, 228, 244)),
                "Amber Gold" => new AccentPalette(new Color(92, 72, 28), new Color(188, 156, 68), new Color(238, 208, 112)),
                "Signal Red" => new AccentPalette(new Color(92, 42, 36), new Color(182, 78, 70), new Color(232, 150, 132)),
                _ => new AccentPalette(new Color(68, 108, 48), new Color(128, 190, 82), new Color(188, 238, 122))
            };
        }

        private static Color ResolveWarningColor(string warningColor)
        {
            return warningColor switch
            {
                "Ivory" => new Color(230, 230, 216),
                "Crimson" => new Color(214, 104, 88),
                "Cyan" => new Color(154, 218, 236),
                _ => new Color(232, 184, 104)
            };
        }

        private static FontProfileScale ResolveFontProfile(string fontStyle)
        {
            return fontStyle switch
            {
                "Compact" => new FontProfileScale(0.94f, 0.96f, 0.95f, 0.95f, 0.93f, 0.91f, 0.89f),
                "Command" => new FontProfileScale(1.00f, 1.05f, 1.04f, 1.08f, 1.14f, 1.20f, 1.26f),
                _ => new FontProfileScale(1.00f, 1.00f, 1.00f, 1.00f, 1.00f, 1.00f, 1.00f)
            };
        }
    }

    private sealed class LoadedAssets
    {
        public required IBrush Backdrop { get; init; }
        public required IBrush ViewportFrame { get; init; }
        public required IBrush WindowFrame { get; init; }
        public required IBrush TerminalPanel { get; init; }
        public required IBrush HeaderPlate { get; init; }
        public required IBrush ButtonPrimaryNormal { get; init; }
        public required IBrush ButtonPrimaryHover { get; init; }
        public required IBrush ButtonPrimaryPressed { get; init; }
        public required IBrush ButtonPrimaryDisabled { get; init; }
        public required IBrush ButtonSecondaryNormal { get; init; }
        public required IBrush ButtonSecondaryHover { get; init; }
        public required IBrush ButtonSecondaryPressed { get; init; }
        public required IBrush ButtonSecondaryDisabled { get; init; }
        public required IBrush ButtonDangerNormal { get; init; }
        public required IBrush ButtonDangerHover { get; init; }
        public required IBrush ButtonDangerPressed { get; init; }
        public required IBrush ButtonDangerDisabled { get; init; }
        public required IBrush TabNormal { get; init; }
        public required IBrush TabSelected { get; init; }
        public required IBrush ListRowNormal { get; init; }
        public required IBrush ListRowSelected { get; init; }
    }

    private readonly record struct ButtonVisuals(
        IBrush Background,
        IBrush OverBackground,
        IBrush PressedBackground,
        IBrush DisabledBackground,
        Color TextColor,
        Color OverTextColor,
        Color PressedTextColor,
        Color DisabledTextColor);

    private readonly record struct AccentPalette(Color Dim, Color Base, Color Bright);
    private readonly record struct FontProfileScale(float Tiny, float Small, float Body, float Button, float Heading, float Title, float Hero);
}
