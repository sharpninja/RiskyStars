using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RiskyStars.Shared;
using Myra;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.Brushes;
using System.Linq;

namespace RiskyStars.Client;

public class LobbyBrowserScreen
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly int _screenWidth;
    private readonly int _screenHeight;
    private SpriteFont? _font;

    private Desktop? _desktop;
    private Panel? _mainPanel;
    private Grid? _lobbiesGrid;
    private TextButton? _createLobbyButton;
    private TextButton? _joinLobbyButton;
    private TextButton? _refreshButton;
    private Label? _countLabel;

    private List<LobbyInfo> _lobbies = new();
    private int _selectedLobbyIndex = -1;
    private double _refreshTimer = 0;
    private const double RefreshInterval = 2000;

    private const int MaxVisibleLobbies = 8;

    public string? SelectedLobbyId { get; private set; }
    public bool ShouldCreateLobby { get; private set; }
    public bool ShouldJoinLobby { get; private set; }
    public bool ShouldRefresh { get; private set; }

    public LobbyBrowserScreen(GraphicsDevice graphicsDevice, int screenWidth, int screenHeight)
    {
        _graphicsDevice = graphicsDevice;
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
    }

    public void LoadContent(SpriteFont font)
    {
        _font = font;
        _desktop = new Desktop();
        BuildUI();
    }

    private void BuildUI()
    {
        var rootGrid = new Grid
        {
            RowSpacing = 15,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Width = _screenWidth,
            Height = _screenHeight
        };

        rootGrid.RowsProportions.Add(new Proportion(ProportionType.Auto)); // Title
        rootGrid.RowsProportions.Add(new Proportion(ProportionType.Auto)); // Count
        rootGrid.RowsProportions.Add(new Proportion(ProportionType.Fill)); // Lobbies list
        rootGrid.RowsProportions.Add(new Proportion(ProportionType.Auto)); // Buttons

        // Title
        var titleLabel = new Label
        {
            Text = "Game Lobbies",
            TextColor = Color.Cyan,
            Scale = new Vector2(1.8f, 1.8f),
            HorizontalAlignment = HorizontalAlignment.Center,
            GridRow = 0,
            Margin = new Thickness(0, 30, 0, 10)
        };
        rootGrid.Widgets.Add(titleLabel);

        // Lobby count
        _countLabel = new Label
        {
            Text = "Available Lobbies: 0",
            TextColor = Color.White,
            Scale = new Vector2(0.9f, 0.9f),
            HorizontalAlignment = HorizontalAlignment.Center,
            GridRow = 1,
            Margin = new Thickness(0, 0, 0, 15)
        };
        rootGrid.Widgets.Add(_countLabel);

        // Lobbies list panel
        var lobbiesPanel = BuildLobbiesPanel();
        lobbiesPanel.GridRow = 2;
        rootGrid.Widgets.Add(lobbiesPanel);

        // Buttons
        var buttonsPanel = BuildButtonsPanel();
        buttonsPanel.GridRow = 3;
        rootGrid.Widgets.Add(buttonsPanel);

        _mainPanel = new Panel
        {
            Width = _screenWidth,
            Height = _screenHeight,
            Background = new SolidBrush(new Color(10, 10, 20))
        };
        _mainPanel.Widgets.Add(rootGrid);

        if (_desktop != null)
        {
            _desktop.Root = _mainPanel;
        }
    }

    private Panel BuildLobbiesPanel()
    {
        var scrollViewer = new ScrollViewer
        {
            Width = _screenWidth - 100,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Stretch,
            ShowHorizontalScrollBar = false,
            ShowVerticalScrollBar = true
        };

        _lobbiesGrid = new Grid
        {
            RowSpacing = 10,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        _lobbiesGrid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));

        scrollViewer.Content = _lobbiesGrid;

        var panel = new Panel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Stretch,
            Padding = new Thickness(20),
            Background = new SolidBrush(new Color(0, 0, 0, 180)),
            Margin = new Thickness(50, 0, 50, 20)
        };
        panel.Widgets.Add(scrollViewer);

        return panel;
    }

    private void RebuildLobbiesList()
    {
        if (_lobbiesGrid == null)
        {
            return;
        }

        _lobbiesGrid.Widgets.Clear();
        _lobbiesGrid.RowsProportions.Clear();

        if (_lobbies.Count == 0)
        {
            _lobbiesGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));

            var emptyLabel = new Label
            {
                Text = "No lobbies available. Create one to start playing!",
                TextColor = Color.Gray,
                Scale = new Vector2(0.9f, 0.9f),
                HorizontalAlignment = HorizontalAlignment.Center,
                GridRow = 0,
                Margin = new Thickness(0, 50, 0, 50)
            };
            _lobbiesGrid.Widgets.Add(emptyLabel);
        }
        else
        {
            for (int i = 0; i < _lobbies.Count; i++)
            {
                _lobbiesGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
                BuildLobbyItem(i);
            }
        }
    }

    private void BuildLobbyItem(int index)
    {
        if (_lobbiesGrid == null)
        {
            return;
        }

        var lobby = _lobbies[index];
        bool isSelected = index == _selectedLobbyIndex;

        var itemPanel = new Panel
        {
            Background = new SolidBrush(isSelected ? new Color(50, 80, 120) : new Color(30, 30, 40)),
            Padding = new Thickness(15, 12),
            GridRow = index,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Border = new SolidBrush(isSelected ? Color.Cyan : Color.Gray),
            BorderThickness = new Thickness(2)
        };

        var grid = new Grid
        {
            RowSpacing = 5,
            ColumnSpacing = 20,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        grid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        grid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        grid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));

        // Host name (row 0, col 0)
        var hostLabel = new Label
        {
            Text = $"Host: {lobby.HostPlayerName}",
            TextColor = Color.White,
            Scale = new Vector2(0.8f, 0.8f),
            GridRow = 0,
            GridColumn = 0
        };
        grid.Widgets.Add(hostLabel);

        // Player count (row 0, col 1)
        Color playersColor = lobby.CurrentPlayers >= lobby.MaxPlayers ? Color.Red : Color.LightGreen;
        var playersLabel = new Label
        {
            Text = $"{lobby.CurrentPlayers}/{lobby.MaxPlayers} Players",
            TextColor = playersColor,
            Scale = new Vector2(0.8f, 0.8f),
            GridRow = 0,
            GridColumn = 1,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        grid.Widgets.Add(playersLabel);

        // Mode and map info (row 1, spanning both columns)
        var infoLabel = new Label
        {
            Text = $"Mode: {lobby.GameMode} | Map: {lobby.MapName}",
            TextColor = Color.LightGray,
            Scale = new Vector2(0.7f, 0.7f),
            GridRow = 1,
            GridColumn = 0,
            GridColumnSpan = 2
        };
        grid.Widgets.Add(infoLabel);

        itemPanel.Widgets.Add(grid);

        // Handle selection
        int capturedIndex = index;
        itemPanel.TouchDown += (s, a) =>
        {
            _selectedLobbyIndex = capturedIndex;
            if (_joinLobbyButton != null)
            {
                _joinLobbyButton.Enabled = true;
            }

            RebuildLobbiesList();
        };

        _lobbiesGrid.Widgets.Add(itemPanel);
    }

    private Panel BuildButtonsPanel()
    {
        var grid = new Grid
        {
            ColumnSpacing = 20,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 10, 0, 30)
        };

        grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));

        _createLobbyButton = new TextButton
        {
            Text = "Create Lobby",
            Width = 160,
            Height = 45,
            GridColumn = 0
        };
        _createLobbyButton.Click += (s, a) => { ShouldCreateLobby = true; };
        grid.Widgets.Add(_createLobbyButton);

        _joinLobbyButton = new TextButton
        {
            Text = "Join Lobby",
            Width = 160,
            Height = 45,
            GridColumn = 1,
            Enabled = false
        };
        _joinLobbyButton.Click += (s, a) =>
        {
            if (_selectedLobbyIndex >= 0 && _selectedLobbyIndex < _lobbies.Count)
            {
                SelectedLobbyId = _lobbies[_selectedLobbyIndex].LobbyId;
                ShouldJoinLobby = true;
            }
        };
        grid.Widgets.Add(_joinLobbyButton);

        _refreshButton = new TextButton
        {
            Text = "Refresh",
            Width = 160,
            Height = 45,
            GridColumn = 2
        };
        _refreshButton.Click += (s, a) =>
        {
            ShouldRefresh = true;
            _refreshTimer = 0;
        };
        grid.Widgets.Add(_refreshButton);

        var panel = new Panel
        {
            HorizontalAlignment = HorizontalAlignment.Center
        };
        panel.Widgets.Add(grid);

        return panel;
    }

    public void Update(GameTime gameTime, MouseState mouseState)
    {
        ShouldCreateLobby = false;
        ShouldJoinLobby = false;
        ShouldRefresh = false;

        _refreshTimer += gameTime.ElapsedGameTime.TotalMilliseconds;
        if (_refreshTimer >= RefreshInterval)
        {
            ShouldRefresh = true;
            _refreshTimer = 0;
        }
    }

    public void SetLobbies(List<LobbyInfo> lobbies)
    {
        _lobbies = lobbies ?? new List<LobbyInfo>();

        if (_countLabel != null)
        {
            _countLabel.Text = $"Available Lobbies: {_lobbies.Count}";
        }

        if (_selectedLobbyIndex >= _lobbies.Count)
        {
            _selectedLobbyIndex = -1;
            if (_joinLobbyButton != null)
            {
                _joinLobbyButton.Enabled = false;
            }
        }

        RebuildLobbiesList();
    }

    public void Reset()
    {
        ShouldCreateLobby = false;
        ShouldJoinLobby = false;
        ShouldRefresh = false;
        SelectedLobbyId = null;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        _desktop?.Render();
    }
}
