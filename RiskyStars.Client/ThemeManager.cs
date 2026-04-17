using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;
using System;
using System.IO;

namespace RiskyStars.Client;

public static class ThemeManager
{
    private static Stylesheet? _stylesheet;
    private static bool _isInitialized = false;

    public static Stylesheet Stylesheet
    {
        get
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("ThemeManager has not been initialized. Call Initialize() first.");
            }
            // Return default if stylesheet loading failed
            return _stylesheet ?? Stylesheet.Current;
        }
    }

    public static void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        try
        {
            // Delay stylesheet loading until GraphicsDevice is guaranteed to be available
            // Myra.DefaultAssets.get_DefaultStylesheet() requires a valid GraphicsDevice
            // We set the flag first and let Stylesheet property handle lazy loading if needed
            _isInitialized = true;
            
            string themeFilePath = "UITheme.xml";
            if (File.Exists(themeFilePath))
            {
                Console.WriteLine($"UITheme.xml found. Theme constants from ThemeManager.Colors/Spacing/etc are available.");
                // Future: Load custom stylesheet if Myra API supports it after device is ready
            }
            else
            {
                Console.WriteLine("UITheme.xml not found. Using ThemeManager constants for consistent styling.");
            }
            
            Console.WriteLine("ThemeManager initialized successfully (stylesheet deferred).");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing theme: {ex.Message}");
            _isInitialized = true;
            _stylesheet = null;
        }
    }

    // Color Constants - extracted from hardcoded values across the codebase
    public static class Colors
    {
        // Primary palette
        public static readonly Color PrimaryDark = new Color(10, 10, 20);
        public static readonly Color PrimaryMedium = new Color(30, 30, 40);
        public static readonly Color PrimaryLight = new Color(40, 40, 60);

        // Accent colors
        public static readonly Color AccentCyan = new Color(100, 180, 255);
        public static readonly Color AccentBlue = new Color(60, 100, 180);
        public static readonly Color AccentDarkBlue = new Color(30, 60, 100);

        // UI state colors
        public static readonly Color HoverColor = new Color(50, 100, 150);
        public static readonly Color PressedColor = new Color(30, 60, 90);
        public static readonly Color DisabledColor = new Color(60, 60, 60);

        // Text colors
        public static readonly Color TextPrimary = Color.White;
        public static readonly Color TextSecondary = new Color(200, 200, 200);
        public static readonly Color TextDisabled = Color.Gray;
        public static readonly Color TextAccent = Color.Cyan;
        public static readonly Color TextWarning = Color.Yellow;
        public static readonly Color TextError = Color.Red;
        public static readonly Color TextSuccess = Color.LightGreen;

        // Border colors
        public static readonly Color BorderNormal = Color.Gray;
        public static readonly Color BorderFocus = Color.Cyan;
        public static readonly Color BorderHover = new Color(100, 180, 255);

        // Background colors
        public static readonly Color BackgroundDark = new Color(0, 0, 0, 217); // 85% opacity
        public static readonly Color BackgroundMedium = new Color(30, 30, 40, 180); // 70% opacity
        public static readonly Color BackgroundLight = new Color(40, 40, 60, 128); // 50% opacity

        // Resource colors
        public static readonly Color PopulationColor = new Color(100, 200, 100);
        public static readonly Color MetalColor = new Color(180, 180, 180);
        public static readonly Color FuelColor = new Color(220, 160, 80);

        // Player/AI colors
        public static readonly Color HeroColor = new Color(180, 100, 220);
        public static readonly Color AIEasyColor = new Color(100, 180, 100);
        public static readonly Color AIMediumColor = new Color(200, 180, 100);
        public static readonly Color AIHardColor = new Color(200, 100, 100);

        // Dropdown/ComboBox colors
        public static readonly Color DropdownOptionHover = new Color(50, 80, 120);
        public static readonly Color DropdownBackground = new Color(25, 25, 35);

        // Slot panel colors
        public static readonly Color SlotPanelNormal = new Color(40, 40, 40, 100);
        public static readonly Color SlotPanelReady = new Color(0, 60, 0, 100);
    }

    // Spacing Constants
    public static class Spacing
    {
        public const int XSmall = 4;
        public const int Small = 8;
        public const int Medium = 12;
        public const int Large = 15;
        public const int XLarge = 20;
        public const int XXLarge = 30;
    }

    // Border Thickness Constants
    public static class BorderThickness
    {
        public const int Thin = 1;
        public const int Normal = 2;
        public const int Thick = 3;
    }

    // Padding Constants
    public static class Padding
    {
        public static readonly Thickness Small = new Thickness(5);
        public static readonly Thickness Medium = new Thickness(10);
        public static readonly Thickness Large = new Thickness(15);
        public static readonly Thickness XLarge = new Thickness(20);
        
        public static readonly Thickness SmallVertical = new Thickness(0, 5);
        public static readonly Thickness MediumVertical = new Thickness(0, 10);
        
        public static readonly Thickness SmallHorizontal = new Thickness(5, 0);
        public static readonly Thickness MediumHorizontal = new Thickness(10, 0);
        
        public static readonly Thickness Button = new Thickness(12, 10);
        public static readonly Thickness Input = new Thickness(10, 8);
        public static readonly Thickness Panel = new Thickness(15);
    }

    // Font Scale Constants
    public static class FontScale
    {
        public static readonly Vector2 Tiny = new Vector2(0.6f, 0.6f);
        public static readonly Vector2 Small = new Vector2(0.7f, 0.7f);
        public static readonly Vector2 SmallMedium = new Vector2(0.8f, 0.8f);
        public static readonly Vector2 Normal = new Vector2(0.9f, 0.9f);
        public static readonly Vector2 Medium = new Vector2(1.0f, 1.0f);
        public static readonly Vector2 Large = new Vector2(1.2f, 1.2f);
        public static readonly Vector2 XLarge = new Vector2(1.5f, 1.5f);
        public static readonly Vector2 XXLarge = new Vector2(1.8f, 1.8f);
        public static readonly Vector2 Title = new Vector2(2.5f, 2.5f);
    }

    // Common UI Element Sizes
    public static class Sizes
    {
        // Button sizes
        public const int ButtonSmallWidth = 80;
        public const int ButtonSmallHeight = 30;
        public const int ButtonMediumWidth = 150;
        public const int ButtonMediumHeight = 45;
        public const int ButtonLargeWidth = 250;
        public const int ButtonLargeHeight = 50;

        // Input field sizes
        public const int InputSmallHeight = 30;
        public const int InputMediumHeight = 40;
        public const int InputLargeHeight = 50;

        // Icon sizes
        public const int IconSmall = 16;
        public const int IconMedium = 18;
        public const int IconLarge = 24;

        // Panel widths
        public const int PanelSmallWidth = 300;
        public const int PanelMediumWidth = 400;
        public const int PanelLargeWidth = 500;
        public const int PanelXLargeWidth = 700;

        // CheckBox/Radio button size
        public const int CheckboxSize = 20;
    }

    // Animation Timing Constants
    public static class Timing
    {
        public const double CursorBlinkInterval = 500.0;
        public const double TooltipDelay = 1000.0;
        public const double AnimationShort = 0.15;
        public const double AnimationMedium = 0.3;
        public const double AnimationLong = 0.5;
    }

    // Helper methods for common styling patterns
    public static Myra.Graphics2D.Brushes.SolidBrush CreateSolidBrush(Color color)
    {
        return new Myra.Graphics2D.Brushes.SolidBrush(color);
    }

    public static void ApplyButtonTheme(Myra.Graphics2D.UI.Button button, ButtonTheme theme = ButtonTheme.Default)
    {
        switch (theme)
        {
            case ButtonTheme.Primary:
                button.Background = CreateSolidBrush(Colors.AccentBlue);
                button.OverBackground = CreateSolidBrush(Colors.AccentCyan);
                button.PressedBackground = CreateSolidBrush(Colors.AccentDarkBlue);
                button.Border = CreateSolidBrush(Colors.BorderHover);
                break;
            case ButtonTheme.Danger:
                button.Background = CreateSolidBrush(new Color(150, 60, 60));
                button.OverBackground = CreateSolidBrush(new Color(200, 80, 80));
                button.PressedBackground = CreateSolidBrush(new Color(120, 40, 40));
                button.Border = CreateSolidBrush(Colors.TextError);
                break;
            case ButtonTheme.Success:
                button.Background = CreateSolidBrush(new Color(60, 150, 60));
                button.OverBackground = CreateSolidBrush(new Color(80, 200, 80));
                button.PressedBackground = CreateSolidBrush(new Color(40, 120, 40));
                button.Border = CreateSolidBrush(Colors.TextSuccess);
                break;
            case ButtonTheme.Hero:
                button.Background = CreateSolidBrush(new Color(120, 60, 140));
                button.OverBackground = CreateSolidBrush(new Color(150, 80, 170));
                button.PressedBackground = CreateSolidBrush(new Color(90, 40, 120));
                button.Border = CreateSolidBrush(Colors.HeroColor);
                break;
            default:
                button.Background = CreateSolidBrush(Colors.AccentBlue);
                button.OverBackground = CreateSolidBrush(Colors.HoverColor);
                button.PressedBackground = CreateSolidBrush(Colors.PressedColor);
                button.Border = CreateSolidBrush(Colors.BorderNormal);
                break;
        }
        button.BorderThickness = new Thickness(BorderThickness.Normal);
        button.Padding = Padding.Button;
    }

    public static void ApplyPanelTheme(Myra.Graphics2D.UI.Panel panel, PanelTheme theme = PanelTheme.Default)
    {
        switch (theme)
        {
            case PanelTheme.Frame:
                panel.Background = CreateSolidBrush(Colors.BackgroundDark);
                panel.Border = CreateSolidBrush(Colors.BorderNormal);
                panel.BorderThickness = new Thickness(BorderThickness.Normal);
                panel.Padding = Padding.Large;
                break;
            case PanelTheme.AccentFrame:
                panel.Background = CreateSolidBrush(Colors.BackgroundDark);
                panel.Border = CreateSolidBrush(Colors.AccentCyan);
                panel.BorderThickness = new Thickness(BorderThickness.Normal);
                panel.Padding = Padding.Large;
                break;
            case PanelTheme.Resource:
                panel.Background = CreateSolidBrush(Colors.BackgroundDark);
                panel.Border = CreateSolidBrush(Colors.AccentCyan);
                panel.BorderThickness = new Thickness(BorderThickness.Normal);
                panel.Padding = Padding.Medium;
                break;
            case PanelTheme.Hero:
                panel.Background = CreateSolidBrush(Colors.BackgroundDark);
                panel.Border = CreateSolidBrush(Colors.HeroColor);
                panel.BorderThickness = new Thickness(BorderThickness.Normal);
                panel.Padding = Padding.Medium;
                break;
            case PanelTheme.Dark:
                panel.Background = CreateSolidBrush(Colors.BackgroundMedium);
                panel.BorderThickness = new Thickness(0);
                panel.Padding = Padding.Medium;
                break;
            default:
                panel.Background = CreateSolidBrush(Colors.BackgroundDark);
                panel.BorderThickness = new Thickness(0);
                break;
        }
    }

    public static void ApplyLabelTheme(Myra.Graphics2D.UI.Label label, LabelTheme theme = LabelTheme.Default)
    {
        switch (theme)
        {
            case LabelTheme.Title:
                label.TextColor = Colors.TextAccent;
                label.Scale = FontScale.XXLarge;
                break;
            case LabelTheme.Subtitle:
                label.TextColor = Colors.TextAccent;
                label.Scale = FontScale.Large;
                break;
            case LabelTheme.Heading:
                label.TextColor = Colors.TextAccent;
                label.Scale = FontScale.XLarge;
                break;
            case LabelTheme.Secondary:
                label.TextColor = Colors.TextSecondary;
                label.Scale = FontScale.Normal;
                break;
            case LabelTheme.Small:
                label.TextColor = Colors.TextSecondary;
                label.Scale = FontScale.SmallMedium;
                break;
            case LabelTheme.Warning:
                label.TextColor = Colors.TextWarning;
                break;
            case LabelTheme.Error:
                label.TextColor = Colors.TextError;
                break;
            case LabelTheme.Success:
                label.TextColor = Colors.TextSuccess;
                break;
            default:
                label.TextColor = Colors.TextPrimary;
                break;
        }
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
}

