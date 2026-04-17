using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Myra.Graphics2D.UI;
using RiskyStars.Shared;
using MyraButton = Myra.Graphics2D.UI.Button;

namespace RiskyStars.Client;

public class LobbyBrowserScreen
{
    private readonly int _screenWidth;
    private readonly int _screenHeight;

    private Desktop? _desktop;
    private Panel? _mainPanel;
    private Grid? _lobbiesGrid;
    private MyraButton? _createLobbyButton;
    private MyraButton? _joinLobbyButton;
    private MyraButton? _refreshButton;
    private Label? _countLabel;
    private Label? _statusLabel;

    private List<LobbyInfo> _lobbies = new();
    private int _selectedLobbyIndex = -1;
    private double _refreshTimer;
    private const double RefreshInterval = 2000;

    public string? SelectedLobbyId { get; private set; }
    public bool ShouldCreateLobby { get; private set; }
    public bool ShouldJoinLobby { get; private set; }
    public bool ShouldRefresh { get; private set; }

    public LobbyBrowserScreen(GraphicsDevice graphicsDevice, int screenWidth, int screenHeight)
    {
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
    }

    public void LoadContent(SpriteFont font)
    {
        _desktop = new Desktop();
        ThemeManager.ApplyDesktopTheme(_desktop);
        BuildUI();
    }

    private void BuildUI()
    {
        int frameWidth = Math.Min(_screenWidth - 120, 1120);
        int frameHeight = Math.Min(_screenHeight - 120, 760);
        var frame = ThemedUIFactory.CreateViewportFrame(frameWidth, frameHeight);
        frame.HorizontalAlignment = HorizontalAlignment.Center;
        frame.VerticalAlignment = VerticalAlignment.Center;

        var layout = ThemedUIFactory.CreateVerticalStack(ThemeManager.Spacing.Large);
        layout.Widgets.Add(ThemedUIFactory.CreateHeaderPlate("Game Lobbies", "Review active multiplayer sessions and join a command room"));

        var metaStrip = ThemedUIFactory.CreateConsolePanel();
        var metaGrid = ThemedUIFactory.CreateGrid(0, ThemeManager.Spacing.Large);
        metaGrid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        metaGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));

        _countLabel = ThemedUIFactory.CreateSecondaryLabel("Available Lobbies: 0");
        _countLabel.TextColor = ThemeManager.Colors.TextWarning;
        _countLabel.GridColumn = 0;
        metaGrid.Widgets.Add(_countLabel);

        _statusLabel = ThemedUIFactory.CreateSmallLabel("Refreshes automatically every 2 seconds.");
        _statusLabel.TextColor = ThemeManager.Colors.TextSecondary;
        _statusLabel.GridColumn = 1;
        metaGrid.Widgets.Add(_statusLabel);

        metaStrip.Widgets.Add(metaGrid);
        layout.Widgets.Add(metaStrip);

        var listPanel = ThemedUIFactory.CreateFramePanel();
        var scrollViewer = ThemedUIFactory.CreateAutoScrollViewer(new Panel(), 420);

        _lobbiesGrid = ThemedUIFactory.CreateGrid(ThemeManager.Spacing.Small, ThemeManager.Spacing.Small);
        _lobbiesGrid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        scrollViewer.Content = _lobbiesGrid;
        listPanel.Widgets.Add(scrollViewer);
        layout.Widgets.Add(listPanel);

        var buttons = ThemedUIFactory.CreateActionBar();
        buttons.HorizontalAlignment = HorizontalAlignment.Center;

        _createLobbyButton = ThemedUIFactory.CreateButton("Create Lobby", 200, ThemeManager.Sizes.ButtonMediumHeight, ThemeManager.ButtonTheme.Primary);
        _createLobbyButton.Click += (_, _) => ShouldCreateLobby = true;
        buttons.Widgets.Add(_createLobbyButton);

        _joinLobbyButton = ThemedUIFactory.CreateButton("Join Lobby", 200, ThemeManager.Sizes.ButtonMediumHeight, ThemeManager.ButtonTheme.Primary);
        _joinLobbyButton.Enabled = false;
        _joinLobbyButton.Click += (_, _) =>
        {
            if (_selectedLobbyIndex >= 0 && _selectedLobbyIndex < _lobbies.Count)
            {
                SelectedLobbyId = _lobbies[_selectedLobbyIndex].LobbyId;
                ShouldJoinLobby = true;
            }
        };
        buttons.Widgets.Add(_joinLobbyButton);

        _refreshButton = ThemedUIFactory.CreateButton("Refresh", 180, ThemeManager.Sizes.ButtonMediumHeight, ThemeManager.ButtonTheme.Default);
        _refreshButton.Click += (_, _) =>
        {
            ShouldRefresh = true;
            _refreshTimer = 0;
            SetStatus("Refreshing lobby manifest...");
        };
        buttons.Widgets.Add(_refreshButton);

        layout.Widgets.Add(buttons);
        frame.Widgets.Add(ThemedUIFactory.CreateAutoScrollViewer(layout, frameHeight - 96));

        _mainPanel = ThemedUIFactory.CreateScreenRoot(_screenWidth, _screenHeight);
        _mainPanel.Widgets.Add(frame);

        if (_desktop != null)
        {
            _desktop.Root = _mainPanel;
        }

        RebuildLobbiesList();
    }

    private void SetStatus(string text)
    {
        if (_statusLabel != null)
        {
            _statusLabel.Text = text;
        }
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
            var emptyPanel = ThemedUIFactory.CreateConsolePanel();
            emptyPanel.GridRow = 0;

            var emptyLabel = ThemedUIFactory.CreateSecondaryLabel("No active lobbies. Create one to open the first command room.");
            emptyLabel.Wrap = true;
            emptyLabel.TextColor = ThemeManager.Colors.TextSecondary;
            emptyLabel.Width = 720;
            emptyPanel.Widgets.Add(emptyLabel);
            _lobbiesGrid.Widgets.Add(emptyPanel);
            return;
        }

        for (int i = 0; i < _lobbies.Count; i++)
        {
            _lobbiesGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
            BuildLobbyItem(i);
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

        var row = ThemedUIFactory.CreateListRowPanel(isSelected);
        row.GridRow = index;

        var grid = ThemedUIFactory.CreateGrid(ThemeManager.Spacing.XSmall, ThemeManager.Spacing.Medium);
        grid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        grid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        grid.RowsProportions.Add(new Proportion(ProportionType.Auto));

        var hostLabel = ThemedUIFactory.CreateHeadingLabel(lobby.HostPlayerName);
        hostLabel.Scale = ThemeManager.FontScale.Medium;
        hostLabel.GridRow = 0;
        hostLabel.GridColumn = 0;
        grid.Widgets.Add(hostLabel);

        var availabilityColor = lobby.CurrentPlayers >= lobby.MaxPlayers
            ? ThemeManager.Colors.TextError
            : ThemeManager.Colors.TextSuccess;
        var playersBadge = ThemedUIFactory.CreateStatusBadge(availabilityColor, $"{lobby.CurrentPlayers}/{lobby.MaxPlayers}");
        playersBadge.GridRow = 0;
        playersBadge.GridColumn = 1;
        grid.Widgets.Add(playersBadge);

        var infoLabel = ThemedUIFactory.CreateSecondaryLabel($"Mode: {lobby.GameMode}   |   Map: {lobby.MapName}");
        infoLabel.TextColor = ThemeManager.Colors.TextPrimary;
        infoLabel.GridRow = 1;
        infoLabel.GridColumn = 0;
        grid.Widgets.Add(infoLabel);

        row.Widgets.Add(grid);

        int capturedIndex = index;
        row.TouchDown += (_, _) =>
        {
            _selectedLobbyIndex = capturedIndex;
            if (_joinLobbyButton != null)
            {
                _joinLobbyButton.Enabled = true;
            }

            SetStatus($"Selected lobby hosted by {lobby.HostPlayerName}.");
            RebuildLobbiesList();
        };

        _lobbiesGrid.Widgets.Add(row);
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

        SetStatus(_lobbies.Count == 0 ? "No sessions discovered on the uplink." : $"Showing {_lobbies.Count} active multiplayer sessions.");
        RebuildLobbiesList();
    }

    public void Reset()
    {
        ShouldCreateLobby = false;
        ShouldJoinLobby = false;
        ShouldRefresh = false;
        SelectedLobbyId = null;
        _selectedLobbyIndex = -1;
        _refreshTimer = 0;

        if (_joinLobbyButton != null)
        {
            _joinLobbyButton.Enabled = false;
        }

        SetStatus("Refreshes automatically every 2 seconds.");
        RebuildLobbiesList();
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        _desktop?.Render();
    }
}
