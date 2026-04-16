using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Myra;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.Brushes;
using System.Linq;

namespace RiskyStars.Client;

public class SinglePlayerLobbyScreen
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly int _screenWidth;
    private readonly int _screenHeight;
    private SpriteFont? _font;

    private Desktop? _desktop;
    private DialogManager? _dialogManager;
    private Panel? _mainPanel;
    private ValidatedTextBox? _playerNameTextBox;
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
    private ComboBox? _mapComboBox;
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CS0618 // Type or member is obsolete
    private Grid? _playerSlotsGrid;
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
    private TextButton? _startGameButton;
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
    private TextButton? _backButton;
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CS0618 // Type or member is obsolete

    private List<PlayerSlot> _playerSlots;
#pragma warning disable CS0618 // Type or member is obsolete
    private List<ComboBox> _playerTypeComboBoxes;
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
    private List<TextButton> _regenerateNameButtons;
#pragma warning restore CS0618 // Type or member is obsolete
    private ServerStatusIndicator? _serverStatusIndicator;
    private EmbeddedServerHost? _embeddedServerHost;

    private KeyboardState _previousKeyState;
    private const int MaxPlayers = 8;
    private const int MaxVisibleSlots = 8;

    public bool ShouldStartGame { get; private set; }
    public bool ShouldGoBack { get; private set; }
    public string PlayerName { get; private set; } = "Player";
    public string SelectedMap { get; private set; } = "Default";
    public List<PlayerSlot> PlayerSlots => _playerSlots;

    public SinglePlayerLobbyScreen(GraphicsDevice graphicsDevice, int screenWidth, int screenHeight)
    {
        _graphicsDevice = graphicsDevice;
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;

        _playerSlots = new List<PlayerSlot>();
#pragma warning disable CS0618 // Type or member is obsolete
        _playerTypeComboBoxes = new List<ComboBox>();
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
        _regenerateNameButtons = new List<TextButton>();
#pragma warning restore CS0618 // Type or member is obsolete

        for (int i = 0; i < MaxPlayers; i++)
        {
            var slot = new PlayerSlot(i + 1);
            slot.IsHost = (i == 0);
            _playerSlots.Add(slot);
        }

        _playerSlots[0].PlayerType = PlayerType.Human;
        
        // Single player defaults: slots 1-4 start as Medium AI opponents, slots 5-7 are empty
        for (int i = 1; i <= 4; i++)
        {
            _playerSlots[i].PlayerType = PlayerType.MediumAI;
            _playerSlots[i].PlayerName = AINameGenerator.GenerateNameWithSeed(i + 1, "1");
        }
        
        // Slots 5-7 start empty
        for (int i = 5; i < MaxPlayers; i++)
        {
            _playerSlots[i].PlayerName = "";
        }
    }

    public void LoadContent(SpriteFont font)
    {
        _font = font;
        _desktop = new Desktop();
        _dialogManager = new DialogManager(_desktop);
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
        rootGrid.RowsProportions.Add(new Proportion(ProportionType.Auto)); // Subtitle
        rootGrid.RowsProportions.Add(new Proportion(ProportionType.Auto)); // Server status
        rootGrid.RowsProportions.Add(new Proportion(ProportionType.Auto)); // Player name
        rootGrid.RowsProportions.Add(new Proportion(ProportionType.Auto)); // Map selection
        rootGrid.RowsProportions.Add(new Proportion(ProportionType.Fill)); // Player slots
        rootGrid.RowsProportions.Add(new Proportion(ProportionType.Auto)); // Buttons

        // Title
#pragma warning disable CS0618 // Type or member is obsolete
        var titleLabel = new Label
        {
            Text = "Single Player Game Setup",
            TextColor = Color.Cyan,
            Scale = new Vector2(1.8f, 1.8f),
            HorizontalAlignment = HorizontalAlignment.Center,
            GridRow = 0,
            Margin = new Thickness(0, 30, 0, 5)
        };
#pragma warning restore CS0618 // Type or member is obsolete
        rootGrid.Widgets.Add(titleLabel);

        // Subtitle
#pragma warning disable CS0618 // Type or member is obsolete
        var subtitleLabel = new Label
        {
            Text = "Configure your game and AI opponents",
            TextColor = Color.White,
            Scale = new Vector2(0.8f, 0.8f),
            HorizontalAlignment = HorizontalAlignment.Center,
            GridRow = 1,
            Margin = new Thickness(0, 0, 0, 10)
        };
#pragma warning restore CS0618 // Type or member is obsolete
        rootGrid.Widgets.Add(subtitleLabel);

        // Server status indicator
        _serverStatusIndicator = new ServerStatusIndicator(600);
#pragma warning disable CS0618 // Type or member is obsolete
        _serverStatusIndicator.Container.GridRow = 2;
#pragma warning restore CS0618 // Type or member is obsolete
        _serverStatusIndicator.Container.HorizontalAlignment = HorizontalAlignment.Center;
        _serverStatusIndicator.Container.Margin = new Thickness(0, 0, 0, 15);
        rootGrid.Widgets.Add(_serverStatusIndicator.Container);

        // Player name input
        var namePanel = BuildPlayerNamePanel();
#pragma warning disable CS0618 // Type or member is obsolete
        namePanel.GridRow = 3;
#pragma warning restore CS0618 // Type or member is obsolete
        rootGrid.Widgets.Add(namePanel);

        // Map selection
        var mapPanel = BuildMapSelectionPanel();
#pragma warning disable CS0618 // Type or member is obsolete
        mapPanel.GridRow = 4;
#pragma warning restore CS0618 // Type or member is obsolete
        rootGrid.Widgets.Add(mapPanel);

        // Player slots
        var slotsPanel = BuildPlayerSlotsPanel();
#pragma warning disable CS0618 // Type or member is obsolete
        slotsPanel.GridRow = 5;
#pragma warning restore CS0618 // Type or member is obsolete
        rootGrid.Widgets.Add(slotsPanel);

        // Buttons
        var buttonsPanel = BuildButtonsPanel();
#pragma warning disable CS0618 // Type or member is obsolete
        buttonsPanel.GridRow = 6;
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

    private Panel BuildPlayerNamePanel()
    {
        var grid = new Grid
        {
            ColumnSpacing = 15,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));

#pragma warning disable CS0618 // Type or member is obsolete
        var label = new Label
        {
            Text = "Your Name:",
            TextColor = Color.White,
            Scale = new Vector2(0.9f, 0.9f),
            VerticalAlignment = VerticalAlignment.Center,
            GridColumn = 0
        };
#pragma warning restore CS0618 // Type or member is obsolete
        grid.Widgets.Add(label);

        _playerNameTextBox = new ValidatedTextBox(400, "Enter your name", showErrorLabel: true);
        _playerNameTextBox.Text = "Player";
        _playerNameTextBox.SetValidator(InputValidator.ValidatePlayerName);
#pragma warning disable CS0618 // Type or member is obsolete
        _playerNameTextBox.Container.GridColumn = 1;
#pragma warning restore CS0618 // Type or member is obsolete
        grid.Widgets.Add(_playerNameTextBox.Container);

        var panel = new Panel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Padding = new Thickness(20, 10),
            Background = new SolidBrush(new Color(0, 0, 0, 180))
        };
        panel.Widgets.Add(grid);

        return panel;
    }

    private Panel BuildMapSelectionPanel()
    {
        var grid = new Grid
        {
            ColumnSpacing = 15,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));

#pragma warning disable CS0618 // Type or member is obsolete
        var label = new Label
        {
            Text = "Map:",
            TextColor = Color.White,
            Scale = new Vector2(0.9f, 0.9f),
            VerticalAlignment = VerticalAlignment.Center,
            GridColumn = 0
        };
#pragma warning restore CS0618 // Type or member is obsolete
        grid.Widgets.Add(label);

#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
        _mapComboBox = new ComboBox
        {
            Width = 400,
            GridColumn = 1
        };
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CS0618 // Type or member is obsolete

        var maps = new List<string> { "Default", "Small", "Medium", "Large" };
        foreach (var map in maps)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            _mapComboBox.Items.Add(new ListItem(map));
#pragma warning restore CS0618 // Type or member is obsolete
        }
        _mapComboBox.SelectedIndex = 0;

        grid.Widgets.Add(_mapComboBox);

        var panel = new Panel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Padding = new Thickness(20, 10),
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

        // Header with player count
#pragma warning disable CS0618 // Type or member is obsolete
        var headerGrid = new Grid
        {
            ColumnSpacing = 20,
            GridRow = 0,
            Margin = new Thickness(0, 15, 0, 10),
            Width = 750
        };
#pragma warning restore CS0618 // Type or member is obsolete
        headerGrid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        headerGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));

#pragma warning disable CS0618 // Type or member is obsolete
        var headerLabel = new Label
        {
            Text = "Player Slots",
            TextColor = Color.Yellow,
            Scale = new Vector2(0.9f, 0.9f),
            GridColumn = 0,
            HorizontalAlignment = HorizontalAlignment.Left
        };
#pragma warning restore CS0618 // Type or member is obsolete
        headerGrid.Widgets.Add(headerLabel);

#pragma warning disable CS0618 // Type or member is obsolete
        var countLabel = new Label
        {
            Text = "AI: 0 | Total: 1/8",
            TextColor = Color.LightGray,
            Scale = new Vector2(0.8f, 0.8f),
            GridColumn = 1,
            HorizontalAlignment = HorizontalAlignment.Right,
            Id = "CountLabel"
        };
#pragma warning restore CS0618 // Type or member is obsolete
        headerGrid.Widgets.Add(countLabel);

        outerGrid.Widgets.Add(headerGrid);

        // Player slots grid
#pragma warning disable CS0618 // Type or member is obsolete
        _playerSlotsGrid = new Grid
        {
            RowSpacing = 8,
            ColumnSpacing = 10,
            GridRow = 1,
            Width = 750,
            HorizontalAlignment = HorizontalAlignment.Center
        };
#pragma warning restore CS0618 // Type or member is obsolete

        _playerSlotsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto)); // Slot label
        _playerSlotsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Fill)); // Player name
        _playerSlotsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto)); // AI badge
        _playerSlotsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto)); // Regenerate button
        _playerSlotsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto)); // Player type dropdown
        _playerSlotsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto)); // Remove button

        for (int i = 0; i < MaxVisibleSlots; i++)
        {
            _playerSlotsGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
            BuildPlayerSlotRow(i);
        }

        // Wrap player slots in scroll viewer
        var playerSlotsScrollViewer = new ScrollViewer
        {
            Content = _playerSlotsGrid,
            Height = 350,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top,
            Background = new SolidBrush(new Color(0,0,0,0))
        };
        
        outerGrid.Widgets.Add(playerSlotsScrollViewer);

        var panel = new Panel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Padding = new Thickness(20),
            Background = new SolidBrush(new Color(0, 0, 0, 180)),
            Margin = new Thickness(50, 0, 50, 0)
        };
        panel.Widgets.Add(outerGrid);

        return panel;
    }

    private void BuildPlayerSlotRow(int slotIndex)
    {
        if (_playerSlotsGrid == null)
        {
            return;
        }

        var slot = _playerSlots[slotIndex];
        int row = slotIndex;

        // Slot label
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
        var slotLabel = new Label
        {
            Text = slotIndex == 0 ? "YOU:" : $"Slot {slotIndex + 1}:",
            TextColor = Color.LightGray,
            Scale = new Vector2(0.7f, 0.7f),
            GridRow = row,
            GridColumn = 0,
            VerticalAlignment = VerticalAlignment.Center,
            Width = 70
        };
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CS0618 // Type or member is obsolete
        _playerSlotsGrid.Widgets.Add(slotLabel);

        // Player name panel
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
        var namePanel = new Panel
        {
            Background = new SolidBrush(slot.IsAI ? new Color(40, 60, 80) : new Color(30, 40, 50)),
            Padding = new Thickness(10, 10),
            GridRow = row,
            GridColumn = 1,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Height = 40
        };
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CS0618 // Type or member is obsolete

        var nameLabel = new Label
        {
            Text = slotIndex == 0 ? "Player" : slot.PlayerName,
            TextColor = slot.IsAI ? Color.LightBlue : Color.White,
            Scale = new Vector2(0.8f, 0.8f),
            VerticalAlignment = VerticalAlignment.Center,
            Id = $"SlotName_{slotIndex}"
        };
        namePanel.Widgets.Add(nameLabel);
        _playerSlotsGrid.Widgets.Add(namePanel);

        // AI badge (placeholder, updated dynamically)
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
        var badgePanel = new Panel
        {
            Width = 70,
            Height = 25,
            GridRow = row,
            GridColumn = 2,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Visible = false,
            Id = $"Badge_{slotIndex}"
        };
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CS0618 // Type or member is obsolete
        _playerSlotsGrid.Widgets.Add(badgePanel);

        // Regenerate name button
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
        var regenButton = new TextButton
        {
            Text = "↻",
            Width = 40,
            Height = 40,
            GridRow = row,
            GridColumn = 3,
            Visible = false,
            Id = $"Regen_{slotIndex}"
        };
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CS0618 // Type or member is obsolete

        int capturedIndex = slotIndex;
        regenButton.Click += (s, a) =>
        {
            if (_playerSlots[capturedIndex].IsAI)
            {
                _playerSlots[capturedIndex].PlayerName = AINameGenerator.GenerateName(
                    capturedIndex + 1,
                    _playerSlots[capturedIndex].GetDifficultyLevel());
                UpdatePlayerSlotUI(capturedIndex);
            }
        };

        _regenerateNameButtons.Add(regenButton);
        _playerSlotsGrid.Widgets.Add(regenButton);

        // Player type dropdown
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
        var comboBox = new ComboBox
        {
            Width = 150,
            GridRow = row,
            GridColumn = 4,
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
        // Set correct selected index from slot state
        comboBox.SelectedIndex = slot.IsAI ? (int)slot.PlayerType : 0;
        
        // Disable Human selection for all slots except slot 0 in single player mode
        if (slotIndex > 0)
        {
            comboBox.Items.RemoveAt(0);
        }

        comboBox.SelectedIndexChanged += (s, a) =>
        {
            if (comboBox.SelectedIndex == null)
            {
                return;
            }

            var newPlayerType = comboBox.SelectedIndex switch
            {
                0 => PlayerType.EasyAI,
                1 => PlayerType.MediumAI,
                2 => PlayerType.HardAI,
                _ => PlayerType.EasyAI
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
                }
                else if (oldType != PlayerType.Human && newPlayerType != PlayerType.Human)
                {
                    _playerSlots[capturedIndex].PlayerName = AINameGenerator.GenerateNameWithSeed(
                        capturedIndex + 1,
                        _playerSlots[capturedIndex].GetDifficultyLevel());
                }
                else if (newPlayerType == PlayerType.Human && capturedIndex > 0)
                {
                    _playerSlots[capturedIndex].PlayerName = $"Player {capturedIndex + 1}";
                }

                UpdatePlayerSlotUI(capturedIndex);
                UpdatePlayerCount();
            }
        };

        _playerTypeComboBoxes.Add(comboBox);
        _playerSlotsGrid.Widgets.Add(comboBox);
        
        // Remove player button
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
        var removeButton = new TextButton
        {
            Text = "✕",
            Width = 40,
            Height = 40,
            GridRow = row,
            GridColumn = 5,
            Visible = slotIndex > 0 && slot.PlayerType != PlayerType.Human,
            Id = $"Remove_{slotIndex}"
        };
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CS0618 // Type or member is obsolete
        
        removeButton.Click += (s, a) =>
        {
            _playerSlots[capturedIndex].PlayerName = "";
            _playerTypeComboBoxes[capturedIndex].SelectedIndex = 0;
            UpdatePlayerSlotUI(capturedIndex);
            UpdatePlayerCount();
        };
        
        _playerSlotsGrid.Widgets.Add(removeButton);
    }

    private void UpdatePlayerSlotUI(int slotIndex)
    {
        if (_playerSlotsGrid == null)
        {
            return;
        }

        var slot = _playerSlots[slotIndex];

        // Update name label
        var nameLabel = _playerSlotsGrid.Widgets.FirstOrDefault(w => w.Id == $"SlotName_{slotIndex}") as Label;
        if (nameLabel != null)
        {
            nameLabel.Text = slotIndex == 0 ? (_playerNameTextBox?.Text ?? "Player") : slot.PlayerName;
            nameLabel.TextColor = slot.IsAI ? Color.LightBlue : Color.White;
        }

        // Update player name in real-time for slot 0
        if (slotIndex == 0 && _playerNameTextBox != null && !string.IsNullOrWhiteSpace(_playerNameTextBox.Text))
        {
            slot.PlayerName = _playerNameTextBox.Text.Trim();
        }

        // Update badge visibility and content
        var badgePanel = _playerSlotsGrid.Widgets.FirstOrDefault(w => w.Id == $"Badge_{slotIndex}") as Panel;
        if (badgePanel != null)
        {
            badgePanel.Visible = slot.IsAI;
            if (slot.IsAI)
            {
                Color badgeColor = slot.PlayerType switch
                {
                    PlayerType.EasyAI => new Color(100, 180, 100),
                    PlayerType.MediumAI => new Color(200, 180, 100),
                    PlayerType.HardAI => new Color(200, 100, 100),
                    _ => Color.Gray
                };

                badgePanel.Background = new SolidBrush(badgeColor * 0.8f);
                badgePanel.Widgets.Clear();

                var badgeLabel = new Label
                {
                    Text = slot.GetDifficultyLevel().ToUpper(),
                    TextColor = Color.White,
                    Scale = new Vector2(0.6f, 0.6f),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                badgePanel.Widgets.Add(badgeLabel);
            }
        }

        // Update regenerate button visibility
#pragma warning disable CS0618 // Type or member is obsolete
        var regenButton = _playerSlotsGrid.Widgets.FirstOrDefault(w => w.Id == $"Regen_{slotIndex}") as TextButton;
#pragma warning restore CS0618 // Type or member is obsolete
        if (regenButton != null)
        {
            regenButton.Visible = slot.IsAI;
        }

        // Update remove button visibility
#pragma warning disable CS0618 // Type or member is obsolete
        var removeButton = _playerSlotsGrid.Widgets.FirstOrDefault(w => w.Id == $"Remove_{slotIndex}") as TextButton;
#pragma warning restore CS0618 // Type or member is obsolete
        if (removeButton != null)
        {
            removeButton.Visible = slotIndex > 0 && slot.IsAI;
        }
    }

    private void UpdatePlayerCount()
    {
        int aiCount = _playerSlots.Count(s => s.IsAI);
        int totalPlayers = _playerSlots.Count(s => s.PlayerType != PlayerType.Human || s.IsHost);

        // Find count label in the widget tree
        if (_mainPanel != null)
        {
            var countLabel = FindWidgetById(_mainPanel, "CountLabel") as Label;
            if (countLabel != null)
            {
                countLabel.Text = $"AI: {aiCount} | Total: {totalPlayers}/{MaxPlayers}";
            }
        }
    }

    private Widget? FindWidgetById(Widget parent, string id)
    {
        if (parent.Id == id)
        {
            return parent;
        }

        if (parent is Panel panel)
        {
            foreach (var child in panel.Widgets)
            {
                var found = FindWidgetById(child, id);
                if (found != null)
                {
                    return found;
                }
            }
        }
        else if (parent is Grid grid)
        {
            foreach (var child in grid.Widgets)
            {
                var found = FindWidgetById(child, id);
                if (found != null)
                {
                    return found;
                }
            }
        }

        return null;
    }

    private Panel BuildButtonsPanel()
    {
        var grid = new Grid
        {
            ColumnSpacing = 20,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 15, 0, 30)
        };

        grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));

#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
        _startGameButton = new TextButton
        {
            Text = "Start Game",
            Width = 180,
            Height = 50,
            GridColumn = 0
        };
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CS0618 // Type or member is obsolete
        _startGameButton.Click += (s, a) =>
        {
            // Validate player name before starting
            if (_playerNameTextBox == null || !_playerNameTextBox.IsValid)
            {
                _dialogManager?.ShowError("Validation Error", "Please enter a valid player name (2-20 characters, letters and numbers only).");
                return;
            }

            PlayerName = _playerNameTextBox.Text.Trim();
            SelectedMap = _mapComboBox?.SelectedItem?.Text ?? "Default";
            _playerSlots[0].PlayerName = PlayerName;
            ShouldStartGame = true;
        };
        grid.Widgets.Add(_startGameButton);

#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
        _backButton = new TextButton
        {
            Text = "Back",
            Width = 180,
            Height = 50,
            GridColumn = 1
        };
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CS0618 // Type or member is obsolete
        _backButton.Click += (s, a) => { ShouldGoBack = true; };
        grid.Widgets.Add(_backButton);

        var panel = new Panel
        {
            HorizontalAlignment = HorizontalAlignment.Center
        };
        panel.Widgets.Add(grid);

        return panel;
    }

    public void Update(GameTime gameTime, MouseState mouseState, KeyboardState keyState)
    {
        ShouldStartGame = false;
        ShouldGoBack = false;
        _dialogManager?.Update();

        if (keyState.IsKeyDown(Keys.Escape) && _previousKeyState.IsKeyUp(Keys.Escape))
        {
            ShouldGoBack = true;
        }

        _serverStatusIndicator?.Update();

        _previousKeyState = keyState;
    }
    
    public void SetEmbeddedServerHost(EmbeddedServerHost? serverHost)
    {
        _embeddedServerHost = serverHost;
        if (_serverStatusIndicator != null && serverHost != null)
        {
            _serverStatusIndicator.SetServerHost(serverHost);
        }
    }

    public void Reset()
    {
        ShouldStartGame = false;
        ShouldGoBack = false;

        _playerSlots[0].PlayerType = PlayerType.Human;
        _playerSlots[0].PlayerName = "Player";
        if (_playerTypeComboBoxes.Count > 0)
        {
            _playerTypeComboBoxes[0].SelectedIndex = 0;
        }
        
        // Single player defaults: slots 1-4 start as Medium AI opponents, slots 5-7 are empty
        for (int i = 1; i <= 4; i++)
        {
            _playerSlots[i].PlayerType = PlayerType.MediumAI;
            _playerSlots[i].PlayerName = AINameGenerator.GenerateNameWithSeed(i + 1, "1");
            if (_playerTypeComboBoxes.Count > i)
            {
                _playerTypeComboBoxes[i].SelectedIndex = (int)_playerSlots[i].PlayerType;
            }
        }
        
        // Slots 5-7 start empty
        for (int i = 5; i < MaxPlayers; i++)
        {
            _playerSlots[i].PlayerName = "";
        }

        for (int i = 0; i < MaxPlayers; i++)
        {
            UpdatePlayerSlotUI(i);
        }

        UpdatePlayerCount();
    }

    public void SetError(string errorMessage)
    {
        _dialogManager?.ShowError("Game Setup Error", errorMessage);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        _desktop?.Render();
    }
}
