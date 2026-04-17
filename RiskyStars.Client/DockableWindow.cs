using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI;
using Myra.Graphics2D;

namespace RiskyStars.Client;

public class DockableWindow
{
    protected Window _window;
    protected readonly string _windowId;
    protected readonly WindowPreferences _preferences;
    protected readonly int _screenWidth;
    protected readonly int _screenHeight;
    
    private DockPosition _currentDockPosition = DockPosition.None;
    
    protected const int DockThreshold = 50;
    protected const int TitleBarHeight = 30;
    
    public Window Window => _window;
    public bool IsVisible
    {
        get => _window.Visible;
        set => _window.Visible = value;
    }
    
    public DockableWindow(string windowId, string title, WindowPreferences preferences, int screenWidth, int screenHeight, int defaultWidth, int defaultHeight)
    {
        _windowId = windowId;
        _preferences = preferences;
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
        
        _window = new Window
        {
            Title = title
        };
        ThemeManager.ApplyWindowTheme(_window);
        
        var savedState = _preferences.GetWindowState(_windowId);
        if (savedState != null)
        {
            _window.Left = savedState.X;
            _window.Top = savedState.Y;
            _window.Width = savedState.Width;
            _window.Height = savedState.Height;
            _window.Visible = savedState.IsVisible;
            _currentDockPosition = savedState.DockPosition;
            
            if (_currentDockPosition != DockPosition.None)
            {
                ApplyDockPosition(_currentDockPosition);
            }
        }
        else
        {
            _window.Width = defaultWidth;
            _window.Height = defaultHeight;
            _window.Left = (screenWidth - defaultWidth) / 2;
            _window.Top = (screenHeight - defaultHeight) / 2;
        }
        
        SetupEventHandlers();
    }
    
    protected virtual void SetupEventHandlers()
    {
        _window.MouseEntered += (s, a) => UpdateWindowStyle(true);
        _window.MouseLeft += (s, a) => UpdateWindowStyle(false);
    }
    
    protected virtual void UpdateWindowStyle(bool isHovered)
    {
        ThemeManager.ApplyWindowTheme(_window, isHovered);
    }
    
    public void Toggle()
    {
        IsVisible = !IsVisible;
        SaveState();
    }
    
    public void Show()
    {
        IsVisible = true;
        SaveState();
    }
    
    public void Hide()
    {
        IsVisible = false;
        SaveState();
    }
    
    public void DockTo(DockPosition position)
    {
        _currentDockPosition = position;
        ApplyDockPosition(position);
        SaveState();
    }
    
    protected virtual void ApplyDockPosition(DockPosition position)
    {
        const int margin = 10;
        int width = _window.Width ?? 300;
        int height = _window.Height ?? 400;
        
        switch (position)
        {
            case DockPosition.Left:
                _window.Left = margin;
                _window.Top = TitleBarHeight + margin;
                _window.Height = _screenHeight - TitleBarHeight - margin * 2;
                break;
                
            case DockPosition.Right:
                _window.Left = _screenWidth - width - margin;
                _window.Top = TitleBarHeight + margin;
                _window.Height = _screenHeight - TitleBarHeight - margin * 2;
                break;
                
            case DockPosition.Top:
                _window.Left = (_screenWidth - width) / 2;
                _window.Top = TitleBarHeight + margin;
                break;
                
            case DockPosition.Bottom:
                _window.Left = (_screenWidth - width) / 2;
                _window.Top = _screenHeight - height - margin;
                break;
                
            case DockPosition.TopLeft:
                _window.Left = margin;
                _window.Top = TitleBarHeight + margin;
                break;
                
            case DockPosition.TopRight:
                _window.Left = _screenWidth - width - margin;
                _window.Top = TitleBarHeight + margin;
                break;
                
            case DockPosition.BottomLeft:
                _window.Left = margin;
                _window.Top = _screenHeight - height - margin;
                break;
                
            case DockPosition.BottomRight:
                _window.Left = _screenWidth - width - margin;
                _window.Top = _screenHeight - height - margin;
                break;
                
            case DockPosition.None:
            default:
                break;
        }
    }
    
    public DockPosition GetSuggestedDockPosition()
    {
        int x = _window.Left;
        int y = _window.Top;
        int width = _window.Width ?? 0;
        int height = _window.Height ?? 0;
        
        bool nearLeft = x < DockThreshold;
        bool nearRight = x > _screenWidth - width - DockThreshold;
        bool nearTop = y < TitleBarHeight + DockThreshold;
        bool nearBottom = y > _screenHeight - height - DockThreshold;
        
        if (nearLeft && nearTop)
        {
            return DockPosition.TopLeft;
        }

        if (nearRight && nearTop)
        {
            return DockPosition.TopRight;
        }

        if (nearLeft && nearBottom)
        {
            return DockPosition.BottomLeft;
        }

        if (nearRight && nearBottom)
        {
            return DockPosition.BottomRight;
        }

        if (nearLeft)
        {
            return DockPosition.Left;
        }

        if (nearRight)
        {
            return DockPosition.Right;
        }

        if (nearTop)
        {
            return DockPosition.Top;
        }

        if (nearBottom)
        {
            return DockPosition.Bottom;
        }

        return DockPosition.None;
    }
    
    public void SaveState()
    {
        var state = new WindowState
        {
            X = _window.Left,
            Y = _window.Top,
            Width = _window.Width ?? 300,
            Height = _window.Height ?? 400,
            IsVisible = _window.Visible,
            DockPosition = _currentDockPosition
        };
        
        _preferences.SetWindowState(_windowId, state);
        _preferences.Save();
    }
    
    public virtual void Update(Microsoft.Xna.Framework.GameTime gameTime)
    {
    }
}
