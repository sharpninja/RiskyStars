using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RiskyStars.Shared;
using Myra;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using System.Linq;
using MyraButton = Myra.Graphics2D.UI.Button;

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
    private MyraButton? _readyButton;
    private MyraButton? _startGameButton;
    private MyraButton? _leaveLobbyButton;
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
        ThemeManager.ApplyDesktopTheme(_desktop);
        BuildUI();
    }

    private void BuildUI()
    {
        var contentWidth = Math.Max(820, Math.Min(_screenWidth - 160, 1080));
        var frameHeight = Math.Min(_screenHeight - 120, 760);

        var contentStack = ThemedUIFactory.CreateSpaciousVerticalStack();
        contentStack.Width = contentWidth;
        contentStack.HorizontalAlignment = HorizontalAlignment.Center;
        contentStack.VerticalAlignment = VerticalAlignment.Center;
        contentStack.Spacing = ThemeManager.Spacing.Large;

        contentStack.Widgets.Add(ThemedUIFactory.CreateHeaderPlate(
            "Multiplayer Lobby",
            "Coordinate ready state, AI slots, and launch from one shared command deck.",
            contentWidth - 24));

        contentStack.Widgets.Add(BuildInfoPanel());
        contentStack.Widgets.Add(BuildPlayerSlotsPanel());

        _statusLabel = ThemedUIFactory.CreateSecondaryLabel(string.Empty);
        _statusLabel.Font = ThemeManager.UiFonts.Small;
        _statusLabel.HorizontalAlignment = HorizontalAlignment.Center;
        contentStack.Widgets.Add(_statusLabel);

        contentStack.Widgets.Add(BuildButtonsPanel());

        var viewportFrame = ThemedUIFactory.CreateViewportFrame(contentWidth + 96, frameHeight);
        viewportFrame.HorizontalAlignment = HorizontalAlignment.Center;
        viewportFrame.VerticalAlignment = VerticalAlignment.Center;
        viewportFrame.Widgets.Add(ThemedUIFactory.CreateAutoScrollViewer(contentStack, frameHeight - 96));

        _mainPanel = ThemedUIFactory.CreateScreenRoot(_screenWidth, _screenHeight);
        _mainPanel.Widgets.Add(viewportFrame);

        if (_desktop != null)
        {
            _desktop.Root = _mainPanel;
        }
    }

    private Panel BuildInfoPanel()
    {
        var grid = new Grid
        {
            RowSpacing = ThemeManager.Spacing.Small,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        grid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        grid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        grid.RowsProportions.Add(new Proportion(ProportionType.Auto));

#pragma warning disable CS0618 // Type or member is obsolete
        _hostLabel = ThemedUIFactory.CreateSecondaryLabel("Host: Loading...");
        _hostLabel.Font = ThemeManager.UiFonts.Small;
        _hostLabel.TextColor = ThemeManager.Colors.TextWarning;
        _hostLabel.GridRow = 0;
#pragma warning restore CS0618 // Type or member is obsolete
        grid.Widgets.Add(_hostLabel);

#pragma warning disable CS0618 // Type or member is obsolete
        _mapLabel = ThemedUIFactory.CreateLabel("Map: Loading...");
        _mapLabel.Font = ThemeManager.UiFonts.Small;
        _mapLabel.GridRow = 1;
#pragma warning restore CS0618 // Type or member is obsolete
        grid.Widgets.Add(_mapLabel);

#pragma warning disable CS0618 // Type or member is obsolete
        _gameModeLabel = ThemedUIFactory.CreateLabel("Game Mode: Loading...");
        _gameModeLabel.Font = ThemeManager.UiFonts.Small;
        _gameModeLabel.GridRow = 2;
#pragma warning restore CS0618 // Type or member is obsolete
        grid.Widgets.Add(_gameModeLabel);

        var panel = ThemedUIFactory.CreateFramePanel();
        panel.HorizontalAlignment = HorizontalAlignment.Center;
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
        var headerLabel = ThemedUIFactory.CreateHeadingLabel("Lobby Slots");
        headerLabel.GridRow = 0;
        headerLabel.Margin = new Thickness(0, ThemeManager.Spacing.Small, 0, ThemeManager.Spacing.Small);
#pragma warning restore CS0618 // Type or member is obsolete
        outerGrid.Widgets.Add(headerLabel);

#pragma warning disable CS0618 // Type or member is obsolete
        _playerSlotsGrid = new Grid
        {
            RowSpacing = 8,
            ColumnSpacing = 10,
            GridRow = 1,
            Width = 760,
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
            Width = 820,
            MaxHeight = 380,
            GridRow = 1,
            ShowVerticalScrollBar = true
        };
#pragma warning restore CS0618 // Type or member is obsolete
        
        outerGrid.Widgets.Add(scrollViewer);

        var panel = ThemedUIFactory.CreateFramePanel();
        panel.HorizontalAlignment = HorizontalAlignment.Center;
        panel.VerticalAlignment = VerticalAlignment.Stretch;
        panel.Margin = new Thickness(20, 0);
        panel.Widgets.Add(outerGrid);

        return panel;
    }

    private Panel BuildButtonsPanel()
    {
        var grid = new Grid
        {
            ColumnSpacing = ThemeManager.Spacing.Large,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, ThemeManager.Spacing.Small, 0, ThemeManager.Spacing.Medium)
        };

        grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));

        _readyButton = ThemedUIFactory.CreateButton("Ready", 170, ThemeManager.Sizes.ButtonMediumHeight, ThemeManager.ButtonTheme.Primary);
        Grid.SetColumn(_readyButton, 0);
        _readyButton.Click += (s, a) => { ShouldToggleReady = true; };
        grid.Widgets.Add(_readyButton);

        _startGameButton = ThemedUIFactory.CreateButton("Start Game", 190, ThemeManager.Sizes.ButtonMediumHeight, ThemeManager.ButtonTheme.Hero);
        Grid.SetColumn(_startGameButton, 1);
        _startGameButton.Enabled = false;
        _startGameButton.Click += (s, a) => { if (_isHost) { ShouldStartGame = true; } };
        grid.Widgets.Add(_startGameButton);

        _leaveLobbyButton = ThemedUIFactory.CreateButton("Leave Lobby", 180, ThemeManager.Sizes.ButtonMediumHeight, ThemeManager.ButtonTheme.Danger);
        Grid.SetColumn(_leaveLobbyButton, 2);
        _leaveLobbyButton.Click += (s, a) => { ShouldLeaveLobby = true; };
        grid.Widgets.Add(_leaveLobbyButton);

        var panel = new Panel { HorizontalAlignment = HorizontalAlignment.Center };
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
            Font = ThemeManager.UiFonts.Small,
            TextColor = Color.LightGray,
            GridRow = row,
            GridColumn = 0,
            VerticalAlignment = VerticalAlignment.Center
        };
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CS0618 // Type or member is obsolete
        _playerSlotsGrid.Widgets.Add(slotNumLabel);

        // Player name
        Color nameColor = slot.IsAI ? ThemeManager.Colors.TextAccent : (slot.IsReady ? ThemeManager.Colors.TextSuccess : ThemeManager.Colors.TextPrimary);
        string nameText = slot.PlayerName;
        if (!slot.IsAI && slot.IsReady)
        {
            nameText += " [READY]";
        }

#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
        var namePanel = new Panel
        {
            Background = ThemeManager.CreateSolidBrush(slot.PlayerType != PlayerType.Human || slot.IsReady
                ? ThemeManager.Colors.SlotPanelReady
                : ThemeManager.Colors.SlotPanelNormal),
            Border = ThemeManager.CreateSolidBrush(slot.PlayerType != PlayerType.Human || slot.IsReady
                ? ThemeManager.Colors.BorderFocus
                : ThemeManager.Colors.BorderNormal),
            BorderThickness = new Thickness(ThemeManager.BorderThickness.Thin),
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
            Font = ThemeManager.UiFonts.Small,
            TextColor = nameColor,
            VerticalAlignment = VerticalAlignment.Center
        };
        namePanel.Widgets.Add(nameLabel);
        _playerSlotsGrid.Widgets.Add(namePanel);

        // AI badge
        if (slot.IsAI)
        {
            Color badgeColor = slot.PlayerType switch
            {
                PlayerType.EasyAI => ThemeManager.Colors.AIEasyColor,
                PlayerType.MediumAI => ThemeManager.Colors.AIMediumColor,
                PlayerType.HardAI => ThemeManager.Colors.AIHardColor,
                _ => ThemeManager.Colors.DisabledColor
            };

            var badgePanel = ThemedUIFactory.CreateStatusBadge(badgeColor, slot.GetDifficultyLevel(), 92);
#pragma warning disable CS0618 // Type or member is obsolete
            badgePanel.GridRow = row;
            badgePanel.GridColumn = 2;
#pragma warning restore CS0618 // Type or member is obsolete
            _playerSlotsGrid.Widgets.Add(badgePanel);
        }

        // Player type dropdown (for host only)
        if (_isHost)
        {
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
            var comboBox = ThemedUIFactory.CreateComboBox(150);
            comboBox.GridRow = row;
            comboBox.GridColumn = 3;
            comboBox.HorizontalAlignment = HorizontalAlignment.Right;
            comboBox.VerticalAlignment = VerticalAlignment.Center;
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
                var removeButton = ThemedUIFactory.CreateButton("Remove", 96, 30, ThemeManager.ButtonTheme.Danger);
                removeButton.GridRow = row;
                removeButton.GridColumn = 4;
                removeButton.HorizontalAlignment = HorizontalAlignment.Right;
                removeButton.VerticalAlignment = VerticalAlignment.Center;
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
            var addButton = ThemedUIFactory.CreateButton("Add", 84, 30, ThemeManager.ButtonTheme.Success);
            addButton.GridRow = row;
            addButton.GridColumn = 4;
            addButton.HorizontalAlignment = HorizontalAlignment.Right;
            addButton.VerticalAlignment = VerticalAlignment.Center;
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
            _statusLabel.Text = "Review the lineup and launch once at least two occupied slots are present.";
            _statusLabel.TextColor = ThemeManager.Colors.TextWarning;
        }
        else
        {
            _statusLabel.Text = "Awaiting host confirmation and match launch.";
            _statusLabel.TextColor = ThemeManager.Colors.TextSecondary;
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
