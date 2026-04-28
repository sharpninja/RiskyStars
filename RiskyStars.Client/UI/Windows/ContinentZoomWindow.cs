using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace RiskyStars.Client;

public sealed class ContinentZoomWindow
{
    private const int CanvasHorizontalInset = 24;
    private const int CanvasHeaderOffset = 140;
    private const int CanvasFooterInset = 58;

    private readonly Label _titleLabel;
    private readonly Label _subtitleLabel;
    private readonly Panel _mapPanel;
    private readonly Image _mapImage;
    private readonly List<ContinentZoomButtonLayout> _currentLayouts = new();
    private int _screenWidth;
    private int _screenHeight;
    private StellarBodyData? _currentBody;

    public Window Window { get; }
    public bool IsVisible => Window.Visible;
    public StellarBodyData? CurrentBody => _currentBody;
    public Rectangle CanvasBounds { get; private set; }
    public event Action<RegionData>? RegionSelected;

    internal IReadOnlyList<ContinentZoomButtonLayout> CurrentLayouts => _currentLayouts;
    internal string TitleText => _titleLabel.Text;
    internal string SubtitleText => _subtitleLabel.Text;
    internal Widget RenderSurfaceWidget => _mapImage;
    internal IImage? RenderSurface => _mapImage.Renderable;

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

        _mapImage = new Image
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            ResizeMode = ImageResizeMode.Stretch
        };
        _mapPanel.Widgets.Add(_mapImage);

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
        RebuildRegionLayouts(body);
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
        UpdateCanvasBounds();

        if (_currentBody != null)
        {
            RebuildRegionLayouts(_currentBody);
        }
    }

    public bool TrySelectRegion(Point screenPoint)
    {
        var selectedRegion = ContinentZoomRenderModel.HitTest(_currentBody, CanvasBounds, screenPoint);
        if (selectedRegion == null)
        {
            return false;
        }

        SelectRegion(selectedRegion);
        return true;
    }

    internal void SelectRegion(RegionData region)
    {
        RegionSelected?.Invoke(region);
    }

    internal void SetRenderedSurface(IImage? renderSurface)
    {
        _mapImage.Renderable = renderSurface;
    }

    private Widget BuildContent()
    {
        var root = ThemedUIFactory.CreateVerticalStack(ThemeManager.Spacing.Small);
        root.Widgets.Add(_titleLabel);
        root.Widgets.Add(_subtitleLabel);

        var hint = ThemedUIFactory.CreateSmallLabel("The XNA zoom surface keeps the planet layout, fills owned regions by player color, and highlights the selected boundary.");
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

    private void RebuildRegionLayouts(StellarBodyData body)
    {
        _currentLayouts.Clear();

        int width = CanvasBounds.Width > 0 ? CanvasBounds.Width : Math.Max(360, _screenWidth / 2);
        int height = CanvasBounds.Height > 0 ? CanvasBounds.Height : Math.Max(240, _screenHeight / 3);
        _currentLayouts.AddRange(ContinentZoomLayout.Build(body, width, height));
    }

    private void UpdateCanvasBounds()
    {
        int windowLeft = Window.Left;
        int windowTop = Window.Top;
        int windowWidth = Window.Width.GetValueOrDefault(Math.Min(ThemeManager.ScalePixels(620), Math.Max(360, _screenWidth - ThemeManager.ScalePixels(80))));
        int windowHeight = Window.Height.GetValueOrDefault(Math.Min(ThemeManager.ScalePixels(520), Math.Max(320, _screenHeight - ThemeManager.ScalePixels(120))));
        int inset = ThemeManager.ScalePixels(CanvasHorizontalInset);
        int top = windowTop + ThemeManager.ScalePixels(CanvasHeaderOffset);
        int height = Math.Max(
            ThemeManager.ScalePixels(210),
            windowHeight - ThemeManager.ScalePixels(CanvasHeaderOffset + CanvasFooterInset));

        CanvasBounds = new Rectangle(
            windowLeft + inset,
            top,
            Math.Max(ThemeManager.ScalePixels(260), windowWidth - inset * 2),
            height);
    }
}
