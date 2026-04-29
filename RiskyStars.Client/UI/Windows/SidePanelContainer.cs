using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI;
using Myra.Graphics2D;
using RiskyStars.Client;

namespace RiskyStars.Client;

public class SidePanelContainer
{
    private readonly Panel _containerPanel;
    private readonly VerticalStackPanel _contentPanel;
    private readonly Panel _headerPanel;
    private readonly Widget _collapsePanel;
    private readonly Widget _resizeInPanel;
    private readonly Widget _resizeOutPanel;
    private readonly string _side;

    private int _targetWidth;
    private int _savedExpandedWidth;
    private readonly int _minWidth = 200;
    private readonly int _maxWidth = 500;
    private bool _isCollapsed;
    private readonly int _collapsedWidth = 60; // Wider for clickability
    private int _currentTopOffset;
    private Label? _debugLabel;

    public int Width => _isCollapsed ? _collapsedWidth : _savedExpandedWidth;
    public string Side => _side;
    public Widget Container => _containerPanel;
    public VerticalStackPanel Content => _contentPanel;
    public bool IsCollapsed => _isCollapsed;
    public int CurrentWidth => _targetWidth;
    public int CurrentTopOffset => _currentTopOffset;

    public event EventHandler<int>? WidthChanged;
    public event EventHandler<bool>? CollapseChanged;

    public SidePanelContainer(string side, int defaultWidth, int screenWidth, int screenHeight, int topOffset = 0)
    {
        _side = side;
        _targetWidth = defaultWidth;
        _savedExpandedWidth = defaultWidth;
        _currentTopOffset = topOffset;
        MethodLogger.LogInfo($"[{_side}] Constructor: defaultWidth={defaultWidth}, savedExpanded={_savedExpandedWidth}");

        var stackPanel = new VerticalStackPanel
        {
            Spacing = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top
        };

        _containerPanel = new Panel
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Background = ThemeManager.CreateSolidBrush(new Color(20, 25, 30, 230))
        };
        _containerPanel.Widgets.Add(stackPanel);

        _debugLabel = new Label
        {
            Text = "L",
            TextColor = Microsoft.Xna.Framework.Color.Yellow,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(4)
        };
        _containerPanel.Widgets.Insert(0, _debugLabel);

        _headerPanel = new Panel
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top,
            Height = ThemeManager.ScalePixels(40),
            Margin = new Thickness(ThemeManager.ScalePixels(4), ThemeManager.ScalePixels(8), ThemeManager.ScalePixels(4), ThemeManager.ScalePixels(4))
        };

        _collapsePanel = CreateButtonPanel("<", w => OnCollapseClick(null!));
        _resizeInPanel = CreateButtonPanel("-", w => OnResizeInClick(null!));
        _resizeOutPanel = CreateButtonPanel("+", w => OnResizeOutClick(null!));
        _collapsePanel.Visible = true;
        _resizeInPanel.Visible = true;
        _resizeOutPanel.Visible = true;

        var contentWrapper = new Panel
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(ThemeManager.ScalePixels(8))
        };

        _contentPanel = new VerticalStackPanel
        {
            Spacing = ThemeManager.Spacing.Medium,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top
        };
        contentWrapper.Widgets.Add(_contentPanel);

        RebuildHeader();
        stackPanel.Widgets.Add(_headerPanel);
        stackPanel.Widgets.Add(contentWrapper);
        System.Diagnostics.Debug.WriteLine($"[SidePanelCtor] {_side}: screen={screenWidth}x{screenHeight}, topOff={topOffset}");
        UpdatePosition(screenWidth, screenHeight, topOffset);
    }

    private Widget CreateButtonPanel(string text, Action<Widget> onClick)
    {
        var isLeft = _side == "left";
        var iconText = isLeft ? (text.Contains("EXPAND") ? ">" : "<") : (text.Contains("EXPAND") ? "<" : ">");
        
        var button = new TextButton
        {
            Width = ThemeManager.ScalePixels(32),
            Height = ThemeManager.ScalePixels(32),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Text = iconText,
            TextColor = Microsoft.Xna.Framework.Color.Yellow
        };

        button.Click += (sender, args) => onClick(button);
        return button;
    }

    private void RebuildHeader()
    {
        _headerPanel.Widgets.Clear();

        var layout = new HorizontalStackPanel
        {
            Spacing = ThemeManager.Spacing.Small,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        if (_side == "left")
        {
            layout.Widgets.Add(_resizeInPanel);
            layout.Widgets.Add(_resizeOutPanel);
            layout.Widgets.Add(_collapsePanel);
        }
        else
        {
            layout.Widgets.Add(_collapsePanel);
            layout.Widgets.Add(_resizeInPanel);
            layout.Widgets.Add(_resizeOutPanel);
        }

        _headerPanel.Widgets.Add(layout);
    }

    private void OnCollapseClick(Widget widget)
    {
        string clickMsg = $"[{_side}] OnCollapseClick: _isCollapsed={_isCollapsed}, will set to {!_isCollapsed}, headerVisible={_headerPanel.Visible}, containerW={_containerPanel.Width}";
        MethodLogger.LogInfo(clickMsg);
        System.Diagnostics.Debug.WriteLine(clickMsg);
        // FORCE header visible before expand
        _headerPanel.Visible = true;
        SetCollapsed(!_isCollapsed, true);
        // Update icon
        if (widget is TextButton tb)
        {
            bool isLeft = _side == "left";
            tb.Text = _isCollapsed ? (isLeft ? ">" : "<") : (isLeft ? "<" : ">");
            tb.TextColor = Microsoft.Xna.Framework.Color.Yellow;
        }
    }

    private void OnResizeInClick(Widget widget)
    {
        if (_targetWidth > _minWidth)
        {
            _targetWidth = Math.Max(_minWidth, _targetWidth - 50);
            if (!_isCollapsed)
                _savedExpandedWidth = _targetWidth;
            _containerPanel.Width = _targetWidth;
            WidthChanged?.Invoke(this, _targetWidth);
        }
    }

    private void OnResizeOutClick(Widget widget)
    {
        if (_targetWidth < _maxWidth)
        {
            _targetWidth = Math.Min(_maxWidth, _targetWidth + 50);
            if (!_isCollapsed)
                _savedExpandedWidth = _targetWidth;
            _containerPanel.Width = _targetWidth;
            WidthChanged?.Invoke(this, _targetWidth);
        }
    }

    public void UpdatePosition(int screenWidth, int screenHeight, int topOffset = 0)
    {
        System.Diagnostics.Debug.WriteLine($"[SidePanel] UpdatePosition: side={_side}, topOffset={topOffset}, isCollapsed={_isCollapsed}, collapsedWidth={_collapsedWidth}");
        _currentTopOffset = topOffset;
        if (_debugLabel != null)
            _debugLabel.Text = $"top={topOffset}";
        _containerPanel.Width = Width;
        _containerPanel.Height = screenHeight - topOffset;
        _containerPanel.HorizontalAlignment = HorizontalAlignment.Left;
        _containerPanel.VerticalAlignment = VerticalAlignment.Top;
        _containerPanel.Top = topOffset;

        if (_side == "left")
        {
            _containerPanel.Left = 0;
        }
        else
        {
            _containerPanel.Left = screenWidth - Width;
        }
        _containerPanel.InvalidateMeasure();

        System.Diagnostics.Debug.WriteLine($"[SidePanel] {_side}: screen={screenWidth}x{screenHeight}, topOff={topOffset}, size={Width}x{_containerPanel.Height}, top={_containerPanel.Top}");

        // Keep header visible ALWAYS so collapse button is clickable
        bool showContent = !_isCollapsed;
        _contentPanel.Visible = showContent;
        _headerPanel.Visible = true; // Always show header for collapse/expand button
        _containerPanel.Visible = true; // keep container visible
    }

    public void ResizeViewport(int screenWidth, int screenHeight, int topOffset = 0)
    {
        UpdatePosition(screenWidth, screenHeight, topOffset);
    }

    public void ToggleCollapse()
    {
        _isCollapsed = !_isCollapsed;
        UpdateHeaderText();
        UpdatePosition(_containerPanel.Width ?? _targetWidth, _containerPanel.Height ?? 400);
        CollapseChanged?.Invoke(this, _isCollapsed);
    }

    private void UpdateHeaderText()
    {
        foreach (var widget in _headerPanel.Widgets)
        {
            if (widget is HorizontalStackPanel stack)
            {
                var panels = stack.Widgets.ToList();
                int collapseIndex = _side == "left" ? 2 : 0;
                if (collapseIndex < panels.Count && panels[collapseIndex] is Panel collapseBtn)
                {
                    foreach (var child in collapseBtn.Widgets)
                    {
                        if (child is Label lbl)
                        {
                            lbl.Text = _isCollapsed ? (_side == "left" ? "<" : ">") : (_side == "left" ? ">" : "<");
                        }
                    }
                }
            }
        }
    }

    public void SetWidth(int width, bool notify = true)
    {
        _targetWidth = Math.Clamp(width, _minWidth, _maxWidth);
        UpdatePosition(_containerPanel.Width ?? _targetWidth, _containerPanel.Height ?? 400);
        if (notify)
        {
            WidthChanged?.Invoke(this, _targetWidth);
        }
    }

public void SetCollapsed(bool collapsed, bool notify = true)
    {
        string enterMsg = $"[{_side}] SetCollapsed ENTER: collapsed={collapsed}, current={_isCollapsed}";
        MethodLogger.LogInfo(enterMsg);
        System.Diagnostics.Debug.WriteLine(enterMsg);
        if (_isCollapsed == collapsed)
        {
            MethodLogger.LogInfo($"[{_side}] SetCollapsed EARLY RETURN - same state");
            return;
        }

        // Save expanded width when collapsing (but not if already collapsed to 40)
        if (collapsed && _targetWidth > _collapsedWidth)
        {
            _savedExpandedWidth = _targetWidth;
            string saveMsg = $"[{_side}] SAVED width: {_savedExpandedWidth}";
            MethodLogger.LogInfo(saveMsg);
            System.Diagnostics.Debug.WriteLine(saveMsg);
        }

        _isCollapsed = collapsed;
        MethodLogger.LogInfo($"[{_side}] SetCollapsed: collapsed={collapsed}, savedExpanded={_savedExpandedWidth}, target={_targetWidth}");

        // Save current Top and Left before any width changes
        int savedTop = _currentTopOffset;
        int currentLeft = _containerPanel.Left;
        int fullWidth = _containerPanel.Width ?? _targetWidth;

        // Use savedExpandedWidth when expanding, collapsedWidth when collapsing
        // Guard: never expand to less than minWidth
        int expandWidth = Math.Max(_minWidth, _savedExpandedWidth);
        int newWidth = collapsed ? _collapsedWidth : expandWidth;

        // CRITICAL: Update _targetWidth when expanding so +/- buttons work correctly
        if (!collapsed)
        {
            _targetWidth = newWidth;
        }
        string logMsg = $"[{_side}] SetCollapsed: newWidth={newWidth}, expandWidth={expandWidth}, collapsed={collapsed}, target={_targetWidth}, saved={_savedExpandedWidth}";
        MethodLogger.LogInfo(logMsg);
        System.Diagnostics.Debug.WriteLine(logMsg);

        // Hide content only, keep header with collapse button visible
        _contentPanel.Visible = !collapsed;
        _resizeInPanel.Visible = !collapsed;
        _resizeOutPanel.Visible = !collapsed;
        MethodLogger.LogInfo($"[{_side}] content.Visible={!collapsed}, resizeBtns={!collapsed}");

        // Set width (triggers layout)
        _containerPanel.Width = newWidth;
        MethodLogger.LogInfo($"[{_side}] Set Width to {newWidth}");

        // Restore Top AFTER width change - this is the key fix!
        _containerPanel.Top = savedTop;
        _containerPanel.HorizontalAlignment = HorizontalAlignment.Left;
        _containerPanel.VerticalAlignment = VerticalAlignment.Top;
        MethodLogger.LogInfo($"[{_side}] Set Top={savedTop}, Left");

        // For right panel, calculate position based on where it was before
        if (_side == "left")
        {
            _containerPanel.Left = 0;
            MethodLogger.LogInfo($"[{_side}] Left=0");
        }
        else
        {
            _containerPanel.Left = currentLeft + (fullWidth - newWidth);
        }

        MethodLogger.LogInfo($"[{_side}] FINAL - Width={_containerPanel.Width}, Top={_containerPanel.Top}, Left={_containerPanel.Left}");

        if (notify)
        {
            CollapseChanged?.Invoke(this, _isCollapsed);
        }
    }

    public int MinWidth => _minWidth;
    public int MaxWidth => _maxWidth;

    public void AddWidget(Widget widget)
    {
        _contentPanel.Widgets.Add(widget);
    }

    public void RemoveWidget(Widget widget)
    {
        _contentPanel.Widgets.Remove(widget);
    }

    public void Clear()
    {
        while (_contentPanel.Widgets.Count > 0)
        {
            _contentPanel.Widgets.RemoveAt(0);
        }
    }
}
