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

public class LobbyScreen
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly int _screenWidth;
    private readonly int _screenHeight;
    private SpriteFont? _font;

    private Desktop? _desktop;
    private Panel? _mainPanel;
    private Label? _hostLabel;
    private Label? _mapLabel;
    private Label? _gameModeLabel;
    private Grid? _playerSlotsGrid;
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
    private TextButton? _readyButton;
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
    private TextButton? _startGameButton;
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
    private TextButton? _leaveLobbyButton;
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CS0618 // Type or member is obsolete
    private Label? _statusLabel;

    private LobbyInfo? _lobbyInfo;
    private string? _currentPlayerId;
    private bool _isHost;
    private bool _isReady;
    private double _refreshTimer = 0;
    private const double RefreshInterval = 1000;

    private List<PlayerSlot> _playerSlots;
#pragma warning disable CS0618 // Type or member is obsolete
    private List<ComboBox> _playerTypeComboBoxes;
#pragma warning restore CS0618 // Type or member is obsolete
    private int _maxPlayers = 4;

    public bool ShouldToggleReady { get; private set; }
    public bool ShouldStartGame { get; private set; }
    public bool ShouldLeaveLobby { get; private set; }
    public bool ShouldRefresh { get; private set; }
    public bool GameStarted { get; private set; }
    public string? SessionId { get; private set; }
    public List<PlayerSlot> PlayerSlots => _playerSlots;

    public LobbyScreen(GraphicsDevice graphicsDevice, int screenWidth, int screenHeight)
    {
        _graphicsDevice = graphicsDevice;
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;

        _playerSlots = new List<PlayerSlot>();
#pragma warning disable CS0618 // Type or member is obsolete
        _playerTypeComboBoxes = new List<ComboBox>();
#pragma warning restore CS0618 // Type or member is obsolete

        InitializePlayerSlots();
    }

    private void InitializePlayerSlots()
    {
        _playerSlots.Clear();
        _playerTypeComboBoxes.Clear();

        for (int i = 0; i < 8; i++)
        {
            var slot = new PlayerSlot(i + 1);
            _playerSlots.Add(slot);
        }
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
            ColumnSpacing = 10,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Width = _screenWidth,
            Height = _screenHeight
        };

        rootGrid.RowsProportions.Add(new Proportion(ProportionType.Auto)); // Title
        rootGrid.RowsProportions.Add(new Proportion(ProportionType.Auto)); // Info panel
        rootGrid.RowsProportions.Add(new Proportion(ProportionType.Fill)); // Player slots
        rootGrid.RowsProportions.Add(new Proportion(ProportionType.Auto)); // Status
        rootGrid.RowsProportions.Add(new Proportion(ProportionType.Auto)); // Buttons

        // Title
#pragma warning disable CS0618 // Type or member is obsolete
        var titleLabel = new Label
        {
            Text = "Game Lobby",
            TextColor = Color.Cyan,
            Scale = new Vector2(1.8f, 1.8f),
            HorizontalAlignment = HorizontalAlignment.Center,
            GridRow = 0,
            Margin = new Thickness(0, 20, 0, 10)
        };
#pragma warning restore CS0618 // Type or member is obsolete
        rootGrid.Widgets.Add(titleLabel);

        // Info panel
        var infoPanel = BuildInfoPanel();
#pragma warning disable CS0618 // Type or member is obsolete
        infoPanel.GridRow = 1;
#pragma warning restore CS0618 // Type or member is obsolete
        rootGrid.Widgets.Add(infoPanel);

        // Player slots panel
        var slotsPanel = BuildPlayerSlotsPanel();
#pragma warning disable CS0618 // Type or member is obsolete
        slotsPanel.GridRow = 2;
#pragma warning restore CS0618 // Type or member is obsolete
        rootGrid.Widgets.Add(slotsPanel);

        // Status label
#pragma warning disable CS0618 // Type or member is obsolete
        _statusLabel = new Label
        {
            Text = "",
            TextColor = Color.Yellow,
            HorizontalAlignment = HorizontalAlignment.Center,
            GridRow = 3,
            Scale = new Vector2(0.8f, 0.8f)
        };
#pragma warning restore CS0618 // Type or member is obsolete
        rootGrid.Widgets.Add(_statusLabel);

        // Buttons
        var buttonsPanel = BuildButtonsPanel();
#pragma warning disable CS0618 // Type or member is obsolete
        buttonsPanel.GridRow = 4;
#pragma warning restore CS0618 // Type or member is obsolete
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

    private Panel BuildInfoPanel()
    {
        var grid = new Grid
        {
            RowSpacing = 8,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        grid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        grid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        grid.RowsProportions.Add(new Proportion(ProportionType.Auto));

#pragma warning disable CS0618 // Type or member is obsolete
        _hostLabel = new Label
        {
            Text = "Host: Loading...",
            TextColor = Color.Yellow,
            Scale = new Vector2(0.9f, 0.9f),
            GridRow = 0
        };
#pragma warning restore CS0618 // Type or member is obsolete
        grid.Widgets.Add(_hostLabel);

#pragma warning disable CS0618 // Type or member is obsolete
        _mapLabel = new Label
        {
            Text = "Map: Loading...",
            TextColor = Color.White,
            Scale = new Vector2(0.8f, 0.8f),
            GridRow = 1
        };
#pragma warning restore CS0618 // Type or member is obsolete
        grid.Widgets.Add(_mapLabel);

#pragma warning disable CS0618 // Type or member is obsolete
        _gameModeLabel = new Label
        {
            Text = "Game Mode: Loading...",
            TextColor = Color.White,
            Scale = new Vector2(0.8f, 0.8f),
            GridRow = 2
        };
#pragma warning restore CS0618 // Type or member is obsolete
        grid.Widgets.Add(_gameModeLabel);

        var panel = new Panel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Padding = new Thickness(20, 15),
            Background = new SolidBrush(new Color(0, 0, 0, 180))
        };
        panel.Widgets.Add(grid);

        return panel;
    }

    private Panel BuildPlayerSlotsPanel()
    {
        var outerGrid = new Grid
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top
        };
        outerGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        outerGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));

#pragma warning disable CS0618 // Type or member is obsolete
        var headerLabel = new Label
        {
            Text = "Player Slots:",
            TextColor = Color.Cyan,
            Scale = new Vector2(0.9f, 0.9f),
            GridRow = 0,
            Margin = new Thickness(0, 10, 0, 10)
        };
#pragma warning restore CS0618 // Type or member is obsolete
        outerGrid.Widgets.Add(headerLabel);

#pragma warning disable CS0618 // Type or member is obsolete
        _playerSlotsGrid = new Grid
        {
            RowSpacing = 8,
            ColumnSpacing = 10,
            GridRow = 1,
            Width = 700,
            HorizontalAlignment = HorizontalAlignment.Center
        };
#pragma warning restore CS0618 // Type or member is obsolete

        _playerSlotsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto)); // Slot number
        _playerSlotsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Fill)); // Player name
        _playerSlotsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto)); // AI badge or status
        _playerSlotsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto)); // Player type dropdown
        _playerSlotsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto)); // Remove button

        for (int i = 0; i < 8; i++)
        {
            _playerSlotsGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        }

        // Wrap player slots grid in scrollable container
#pragma warning disable CS0618 // Type or member is obsolete
        var scrollViewer = new ScrollViewer
        {
            Content = _playerSlotsGrid,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Stretch,
            Width = 750,
            MaxHeight = 350,
            GridRow = 1,
            ShowVerticalScrollBar = true
        };
#pragma warning restore CS0618 // Type or member is obsolete
        
        outerGrid.Widgets.Add(scrollViewer);

        var panel = new Panel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Stretch,
            Padding = new Thickness(20),
            Background = new SolidBrush(new Color(0, 0, 0, 180)),
            Margin = new Thickness(50, 0, 50, 0)
        };
        panel.Widgets.Add(outerGrid);

        return panel;
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

#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
        _readyButton = new TextButton
        {
            Text = "Ready",
            Width = 150,
            Height = 45,
            GridColumn = 0
        };
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CS0618 // Type or member is obsolete
        _readyButton.Click += (s, a) => { ShouldToggleReady = true; };
        grid.Widgets.Add(_readyButton);

#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
        _startGameButton = new TextButton
        {
            Text = "Start Game",
            Width = 150,
            Height = 45,
            GridColumn = 1,
            Enabled = false
        };
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CS0618 // Type or member is obsolete
        _startGameButton.Click += (s, a) => { if (_isHost) { ShouldStartGame = true; } };
        grid.Widgets.Add(_startGameButton);

#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
        _leaveLobbyButton = new TextButton
        {
            Text = "Leave Lobby",
            Width = 150,
            Height = 45,
            GridColumn = 2
        };
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CS0618 // Type or member is obsolete
        _leaveLobbyButton.Click += (s, a) => { ShouldLeaveLobby = true; };
        grid.Widgets.Add(_leaveLobbyButton);

        var panel = new Panel
        {
            HorizontalAlignment = HorizontalAlignment.Center
        };
        panel.Widgets.Add(grid);

        return panel;
    }

    private void UpdatePlayerSlotUI(int slotIndex)
    {
        if (_playerSlotsGrid == null || slotIndex >= _maxPlayers)
        {
            return;
        }

        var slot = _playerSlots[slotIndex];
        int row = slotIndex;

        // Clear existing widgets for this row
#pragma warning disable CS0618 // Type or member is obsolete
        var widgetsToRemove = _playerSlotsGrid.Widgets.Where(w => w.GridRow == row).ToList();
#pragma warning restore CS0618 // Type or member is obsolete
        foreach (var widget in widgetsToRemove)
        {
            _playerSlotsGrid.Widgets.Remove(widget);
        }

        // Slot number
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
        var slotNumLabel = new Label
        {
            Text = $"{slotIndex + 1}.",
            TextColor = Color.LightGray,
            Scale = new Vector2(0.8f, 0.8f),
            GridRow = row,
            GridColumn = 0,
            VerticalAlignment = VerticalAlignment.Center
        };
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CS0618 // Type or member is obsolete
        _playerSlotsGrid.Widgets.Add(slotNumLabel);

        // Player name
        Color nameColor = slot.IsAI ? Color.LightBlue : (slot.IsReady ? Color.LightGreen : Color.White);
        string nameText = slot.PlayerName;
        if (!slot.IsAI && slot.IsReady)
        {
            nameText += " [READY]";
        }

#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
        var namePanel = new Panel
        {
            Background = new SolidBrush(slot.PlayerType != PlayerType.Human || slot.IsReady ? new Color(0, 60, 0, 100) : new Color(40, 40, 40, 100)),
            Padding = new Thickness(10, 8),
            GridRow = row,
            GridColumn = 1,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CS0618 // Type or member is obsolete

        var nameLabel = new Label
        {
            Text = nameText,
            TextColor = nameColor,
            Scale = new Vector2(0.8f, 0.8f),
            VerticalAlignment = VerticalAlignment.Center
        };
        namePanel.Widgets.Add(nameLabel);
        _playerSlotsGrid.Widgets.Add(namePanel);

        // AI badge
        if (slot.IsAI)
        {
            Color badgeColor = slot.PlayerType switch
            {
                PlayerType.EasyAI => new Color(100, 180, 100),
                PlayerType.MediumAI => new Color(200, 180, 100),
                PlayerType.HardAI => new Color(200, 100, 100),
                _ => Color.Gray
            };

#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
            var badgePanel = new Panel
            {
                Background = new SolidBrush(badgeColor * 0.8f),
                Width = 70,
                Height = 25,
                GridRow = row,
                GridColumn = 2,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CS0618 // Type or member is obsolete

            var badgeLabel = new Label
            {
                Text = slot.GetDifficultyLevel().ToUpper(),
                TextColor = Color.White,
                Scale = new Vector2(0.6f, 0.6f),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            badgePanel.Widgets.Add(badgeLabel);
            _playerSlotsGrid.Widgets.Add(badgePanel);
        }

        // Player type dropdown (for host only)
        if (_isHost)
        {
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
            var comboBox = new ComboBox
            {
                Width = 150,
                GridRow = row,
                GridColumn = 3,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CS0618 // Type or member is obsolete

#pragma warning disable CS0618 // Type or member is obsolete
            comboBox.Items.Add(new ListItem("Human"));
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
            comboBox.Items.Add(new ListItem("Easy AI"));
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
            comboBox.Items.Add(new ListItem("Medium AI"));
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
            comboBox.Items.Add(new ListItem("Hard AI"));
#pragma warning restore CS0618 // Type or member is obsolete

            comboBox.SelectedIndex = slot.PlayerType switch
            {
                PlayerType.EasyAI => 1,
                PlayerType.MediumAI => 2,
                PlayerType.HardAI => 3,
                _ => 0
            };

            int capturedIndex = slotIndex;
            comboBox.SelectedIndexChanged += (s, a) =>
            {
                if (comboBox.SelectedIndex == null)
                {
                    return;
                }

                var newPlayerType = comboBox.SelectedIndex switch
                {
                    1 => PlayerType.EasyAI,
                    2 => PlayerType.MediumAI,
                    3 => PlayerType.HardAI,
                    _ => PlayerType.Human
                };

                if (_playerSlots[capturedIndex].PlayerType != newPlayerType)
                {
                    var oldType = _playerSlots[capturedIndex].PlayerType;
                    _playerSlots[capturedIndex].PlayerType = newPlayerType;

                    if (oldType == PlayerType.Human && newPlayerType != PlayerType.Human)
                    {
                        _playerSlots[capturedIndex].PlayerName = AINameGenerator.GenerateNameWithSeed(
                            capturedIndex + 1,
                            _playerSlots[capturedIndex].GetDifficultyLevel());
                        _playerSlots[capturedIndex].IsReady = true;
                    }
                    else if (oldType != PlayerType.Human && newPlayerType != PlayerType.Human)
                    {
                        _playerSlots[capturedIndex].PlayerName = AINameGenerator.GenerateNameWithSeed(
                            capturedIndex + 1,
                            _playerSlots[capturedIndex].GetDifficultyLevel());
                    }
                    else if (newPlayerType == PlayerType.Human)
                    {
                        _playerSlots[capturedIndex].PlayerName = $"Player {capturedIndex + 1}";
                        _playerSlots[capturedIndex].IsReady = false;
                    }

                    UpdatePlayerSlotUI(capturedIndex);
                }
            };

            while (_playerTypeComboBoxes.Count <= slotIndex)
            {
                _playerTypeComboBoxes.Add(null!);
            }
            _playerTypeComboBoxes[slotIndex] = comboBox;

            _playerSlotsGrid.Widgets.Add(comboBox);
            
            // Add remove button for host (only for occupied slots that are not host)
            if (_isHost && (slot.PlayerType != PlayerType.Human || (slot.PlayerName != GetPlayerName(_currentPlayerId) && !string.IsNullOrEmpty(slot.PlayerName))))
            {
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
                var removeButton = new TextButton
                {
                    Text = "✕",
                    Width = 35,
                    Height = 30,
                    GridRow = row,
                    GridColumn = 4,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center,
                    Background = new SolidBrush(new Color(120, 30, 30)),
                    Padding = new Thickness(0)
                };
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CS0618 // Type or member is obsolete

            int capturedRemoveIndex = slotIndex;
            removeButton.Click += (s, a) =>
            {
                _playerSlots[capturedRemoveIndex].PlayerType = PlayerType.Human;
                _playerSlots[capturedRemoveIndex].PlayerName = $"Player {capturedRemoveIndex + 1}";
                _playerSlots[capturedRemoveIndex].IsReady = false;
                _playerSlots[capturedRemoveIndex].IsHost = false;
                
                UpdatePlayerSlotUI(capturedRemoveIndex);
                UpdateStartButton();
            };

            _playerSlotsGrid.Widgets.Add(removeButton);
        }
        // Add "+ Add Player" button for empty slots when host
        else if (_isHost && slot.PlayerType == PlayerType.Human && slot.PlayerName == $"Player {slotIndex + 1}" && !slot.IsReady)
        {
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
            var addButton = new TextButton
            {
                Text = "+",
                Width = 35,
                Height = 30,
                GridRow = row,
                GridColumn = 4,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Background = new SolidBrush(new Color(30, 80, 30)),
                Padding = new Thickness(0)
            };
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CS0618 // Type or member is obsolete

            int capturedAddIndex = slotIndex;
            addButton.Click += (s, a) =>
            {
                _playerSlots[capturedAddIndex].PlayerType = PlayerType.EasyAI;
                _playerSlots[capturedAddIndex].PlayerName = AINameGenerator.GenerateNameWithSeed(
                    capturedAddIndex + 1,
                    _playerSlots[capturedAddIndex].GetDifficultyLevel());
                _playerSlots[capturedAddIndex].IsReady = true;
                
                UpdatePlayerSlotUI(capturedAddIndex);
                UpdateStartButton();
            };

            _playerSlotsGrid.Widgets.Add(addButton);
        }
    }
}

    public void SetLobbyInfo(LobbyInfo lobbyInfo, string playerId)
    {
        _lobbyInfo = lobbyInfo;
        _currentPlayerId = playerId;
        _maxPlayers = lobbyInfo.MaxPlayers;

        if (_lobbyInfo != null)
        {
            _isHost = _lobbyInfo.HostPlayerName == GetPlayerName(playerId);

            if (_hostLabel != null)
            {
                _hostLabel.Text = $"Host: {_lobbyInfo.HostPlayerName}";
            }

            if (_mapLabel != null)
            {
                _mapLabel.Text = $"Map: {_lobbyInfo.MapName}";
            }

            if (_gameModeLabel != null)
            {
                _gameModeLabel.Text = $"Game Mode: {_lobbyInfo.GameMode}";
            }

            UpdateStatusLabel();
            UpdateAllPlayerSlots();
            UpdateStartButton();
        }
    }

    private void UpdateAllPlayerSlots()
    {
        for (int i = 0; i < Math.Min(_maxPlayers, _playerSlots.Count); i++)
        {
            UpdatePlayerSlotUI(i);
        }
    }

    private void UpdateStatusLabel()
    {
        if (_statusLabel == null)
        {
            return;
        }

        if (_isHost)
        {
            _statusLabel.Text = "Configure player slots and start when ready!";
            _statusLabel.TextColor = Color.Yellow;
        }
        else
        {
            _statusLabel.Text = "Waiting for host to start the game...";
            _statusLabel.TextColor = Color.Gray;
        }
    }

    private void UpdateStartButton()
    {
        if (_startGameButton == null)
        {
            return;
        }

        int occupiedSlots = _playerSlots.Count(s => s.PlayerType == PlayerType.Human || s.IsAI);
        _startGameButton.Enabled = _isHost && occupiedSlots >= 2;
    }

    public void SetCurrentPlayer(string playerId)
    {
        _currentPlayerId = playerId;
    }

    public void SetReady(bool ready)
    {
        _isReady = ready;
        if (_readyButton != null)
        {
            _readyButton.Enabled = !_isReady;
        }
    }

    public void OnGameStarted(string sessionId)
    {
        SessionId = sessionId;
        GameStarted = true;
    }

    private bool AllPlayersReady()
    {
        if (_lobbyInfo == null)
        {
            return false;
        }

        int occupiedSlots = _playerSlots.Count(s => s.PlayerType == PlayerType.Human || s.IsAI);
        return occupiedSlots >= 2;
    }

    private string GetPlayerName(string playerId)
    {
        if (_lobbyInfo == null || string.IsNullOrEmpty(playerId))
        {
            return "Unknown";
        }

        return _lobbyInfo.PlayerNames.FirstOrDefault() ?? "Unknown";
    }

    public void Update(GameTime gameTime, MouseState mouseState)
    {
        ShouldToggleReady = false;
        ShouldStartGame = false;
        ShouldLeaveLobby = false;
        ShouldRefresh = false;

        _refreshTimer += gameTime.ElapsedGameTime.TotalMilliseconds;
        if (_refreshTimer >= RefreshInterval)
        {
            ShouldRefresh = true;
            _refreshTimer = 0;
        }

        if (_isHost)
        {
            UpdateStartButton();
        }
    }

    public void Reset()
    {
        ShouldToggleReady = false;
        ShouldStartGame = false;
        ShouldLeaveLobby = false;
        ShouldRefresh = false;
        GameStarted = false;
        SessionId = null;
        _isReady = false;

        if (_readyButton != null)
        {
            _readyButton.Enabled = true;
        }

        for (int i = 0; i < _playerSlots.Count; i++)
        {
            _playerSlots[i].PlayerType = PlayerType.Human;
            _playerSlots[i].PlayerName = $"Player {i + 1}";
            _playerSlots[i].IsReady = false;
            _playerSlots[i].IsHost = false;
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        _desktop?.Render();
    }
}
