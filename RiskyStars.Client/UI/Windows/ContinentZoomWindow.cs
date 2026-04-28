using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace RiskyStars.Client;

public sealed class ContinentZoomWindow
{
    private readonly Label _titleLabel;
    private readonly Label _subtitleLabel;
    private readonly Panel _mapPanel;
    private readonly List<ContinentZoomButtonLayout> _currentLayouts = new();
    private int _screenWidth;
    private int _screenHeight;
    private StellarBodyData? _currentBody;

    public Window Window { get; }
    public bool IsVisible => Window.Visible;
    public event Action<RegionData>? RegionSelected;

    internal IReadOnlyList<ContinentZoomButtonLayout> CurrentLayouts => _currentLayouts;
    internal string TitleText => _titleLabel.Text;
    internal string SubtitleText => _subtitleLabel.Text;

    public ContinentZoomWindow(int screenWidth, int screenHeight)
    {
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;

        Window = new Window
        {
            Title = "Planet Zoom",
            Visible = false
        };
        ThemeManager.ApplyGameplayWindowTheme(Window);

        _titleLabel = ThemedUIFactory.CreateTitleLabel("Planet Zoom");
        _titleLabel.TextColor = ThemeManager.Colors.TextAccent;

        _subtitleLabel = ThemedUIFactory.CreateSecondaryLabel("Select a continent.");
        _subtitleLabel.Wrap = true;

        _mapPanel = ThemedUIFactory.CreateGameplayPanel(ThemeManager.Colors.BorderFocus);
        _mapPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        _mapPanel.VerticalAlignment = VerticalAlignment.Stretch;

        Window.Content = BuildContent();
        ResizeViewport(screenWidth, screenHeight);
    }

    public void Show(StellarBodyData body, StarSystemData? starSystem)
    {
        _currentBody = body;
        Window.Title = $"Planet Zoom: {body.Name}";
        _titleLabel.Text = body.Name;
        _subtitleLabel.Text = starSystem == null
            ? "Select a continent."
            : $"Star: {starSystem.Name} | Select a continent.";
        RebuildRegionButtons(body);
        Window.Visible = true;
    }

    public void Hide()
    {
        Window.Visible = false;
    }

    public void ResizeViewport(int screenWidth, int screenHeight)
    {
        if (screenWidth <= 0 || screenHeight <= 0)
        {
            return;
        }

        _screenWidth = screenWidth;
        _screenHeight = screenHeight;

        int width = Math.Min(ThemeManager.ScalePixels(620), Math.Max(360, screenWidth - ThemeManager.ScalePixels(80)));
        int height = Math.Min(ThemeManager.ScalePixels(520), Math.Max(320, screenHeight - ThemeManager.ScalePixels(120)));

        Window.Width = width;
        Window.Height = height;
        Window.Left = Math.Max(ThemeManager.ScalePixels(20), (screenWidth - width) / 2);
        Window.Top = Math.Max(ThemeManager.ScalePixels(48), (screenHeight - height) / 2);
        _mapPanel.Height = Math.Max(ThemeManager.ScalePixels(240), height - ThemeManager.ScalePixels(150));

        if (_currentBody != null)
        {
            RebuildRegionButtons(_currentBody);
        }
    }

    internal void SelectRegion(RegionData region)
    {
        RegionSelected?.Invoke(region);
        Hide();
    }

    private Widget BuildContent()
    {
        var root = ThemedUIFactory.CreateVerticalStack(ThemeManager.Spacing.Small);
        root.Widgets.Add(_titleLabel);
        root.Widgets.Add(_subtitleLabel);

        var hint = ThemedUIFactory.CreateSmallLabel("The enlarged buttons below are intentionally oversized for dense center continents.");
        hint.TextColor = ThemeManager.Colors.TextWarning;
        hint.Wrap = true;
        root.Widgets.Add(hint);

        root.Widgets.Add(_mapPanel);

        var buttons = ThemedUIFactory.CreateActionBar();
        buttons.HorizontalAlignment = HorizontalAlignment.Right;
        var closeButton = ThemedUIFactory.CreateButton("Close", ThemeManager.Sizes.ButtonSmallWidth, ThemeManager.Sizes.ButtonSmallHeight);
        closeButton.Click += (_, _) => Hide();
        buttons.Widgets.Add(closeButton);
        root.Widgets.Add(buttons);

        return root;
    }

    private void RebuildRegionButtons(StellarBodyData body)
    {
        _mapPanel.Widgets.Clear();
        _currentLayouts.Clear();

        int width = Window.Width.GetValueOrDefault(Math.Max(360, _screenWidth / 2));
        int height = _mapPanel.Height.GetValueOrDefault(Math.Max(240, _screenHeight / 3));
        _currentLayouts.AddRange(ContinentZoomLayout.Build(body, width, height));

        var planetCore = new Panel
        {
            Width = ThemeManager.ScalePixels(96),
            Height = ThemeManager.ScalePixels(96),
            Left = Math.Max(0, (width - ThemeManager.ScalePixels(96)) / 2),
            Top = Math.Max(0, (height - ThemeManager.ScalePixels(96)) / 2),
            Background = ThemeManager.CreateSolidBrush(ThemeManager.Colors.AccentCyan * 0.18f),
            Border = ThemeManager.CreateSolidBrush(ThemeManager.Colors.BorderNormal),
            BorderThickness = new Thickness(ThemeManager.BorderThickness.Thin)
        };
        _mapPanel.Widgets.Add(planetCore);

        foreach (var layout in _currentLayouts)
        {
            var region = layout.Region;
            var button = ThemedUIFactory.CreateButton(
                region.Name,
                layout.Bounds.Width,
                layout.Bounds.Height,
                ThemeManager.ButtonTheme.Primary);
            button.Left = layout.Bounds.Left;
            button.Top = layout.Bounds.Top;
            button.HorizontalAlignment = HorizontalAlignment.Left;
            button.VerticalAlignment = VerticalAlignment.Top;
            button.Click += (_, _) => SelectRegion(region);
            _mapPanel.Widgets.Add(button);
        }
    }
}
