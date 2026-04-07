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
    private Panel? _mainPanel;
    private TextBox? _playerNameTextBox;
    private ComboBox? _mapComboBox;
    private Grid? _playerSlotsGrid;
    private TextButton? _startGameButton;
    private TextButton? _backButton;
    private Label? _errorLabel;

    private List<PlayerSlot> _playerSlots;
    private List<ComboBox> _playerTypeComboBoxes;
    private List<TextButton> _regenerateNameButtons;

    private KeyboardState _previousKeyState;
    private string? _errorMessage;
    private float _errorDisplayTime;
    private const float ErrorDisplayDuration = 5.0f;
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
        _playerTypeComboBoxes = new List<ComboBox>();
        _regenerateNameButtons = new List<TextButton>();

        for (int i = 0; i < MaxPlayers; i++)
        {
            var slot = new PlayerSlot(i + 1);
            slot.IsHost = (i == 0);
            _playerSlots.Add(slot);
        }

        _playerSlots[0].PlayerType = PlayerType.Human;
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
        rootGrid.RowsProportions.Add(new Proportion(ProportionType.Auto)); // Subtitle
        rootGrid.RowsProportions.Add(new Proportion(ProportionType.Auto)); // Player name
        rootGrid.RowsProportions.Add(new Proportion(ProportionType.Auto)); // Map selection
        rootGrid.RowsProportions.Add(new Proportion(ProportionType.Fill)); // Player slots
        rootGrid.RowsProportions.Add(new Proportion(ProportionType.Auto)); // Error message
        rootGrid.RowsProportions.Add(new Proportion(ProportionType.Auto)); // Buttons

        // Title
        var titleLabel = new Label
        {
            Text = "Single Player Game Setup",
            TextColor = Color.Cyan,
            Scale = new Vector2(1.8f, 1.8f),
            HorizontalAlignment = HorizontalAlignment.Center,
            GridRow = 0,
            Margin = new Thickness(0, 30, 0, 5)
        };
        rootGrid.Widgets.Add(titleLabel);

        // Subtitle
        var subtitleLabel = new Label
        {
            Text = "Configure your game and AI opponents",
            TextColor = Color.White,
            Scale = new Vector2(0.8f, 0.8f),
            HorizontalAlignment = HorizontalAlignment.Center,
            GridRow = 1,
            Margin = new Thickness(0, 0, 0, 15)
        };
        rootGrid.Widgets.Add(subtitleLabel);

        // Player name input
        var namePanel = BuildPlayerNamePanel();
        namePanel.GridRow = 2;
        rootGrid.Widgets.Add(namePanel);

        // Map selection
        var mapPanel = BuildMapSelectionPanel();
        mapPanel.GridRow = 3;
        rootGrid.Widgets.Add(mapPanel);

        // Player slots
        var slotsPanel = BuildPlayerSlotsPanel();
        slotsPanel.GridRow = 4;
        rootGrid.Widgets.Add(slotsPanel);

        // Error message
        _errorLabel = new Label
        {
            Text = "",
            TextColor = Color.Yellow,
            HorizontalAlignment = HorizontalAlignment.Center,
            GridRow = 5,
            Scale = new Vector2(0.8f, 0.8f),
            Visible = false,
            Wrap = true,
            Width = 600
        };
        rootGrid.Widgets.Add(_errorLabel);

        // Buttons
        var buttonsPanel = BuildButtonsPanel();
        buttonsPanel.GridRow = 6;
        rootGrid.Widgets.Add(buttonsPanel);

        _mainPanel = new Panel
        {
            Width = _screenWidth,
            Height = _screenHeight,
            Background = new SolidBrush(new Color(10, 10, 20))
        };
        _mainPanel.Widgets.Add(rootGrid);

        if (_desktop != null)
            _desktop.Root = _mainPanel;
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

        var label = new Label
        {
            Text = "Your Name:",
            TextColor = Color.White,
            Scale = new Vector2(0.9f, 0.9f),
            VerticalAlignment = VerticalAlignment.Center,
            GridColumn = 0
        };
        grid.Widgets.Add(label);

        _playerNameTextBox = new TextBox
        {
            Text = "Player",
            Width = 400,
            GridColumn = 1
        };
        grid.Widgets.Add(_playerNameTextBox);

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

        var label = new Label
        {
            Text = "Map:",
            TextColor = Color.White,
            Scale = new Vector2(0.9f, 0.9f),
            VerticalAlignment = VerticalAlignment.Center,
            GridColumn = 0
        };
        grid.Widgets.Add(label);

        _mapComboBox = new ComboBox
        {
            Width = 400,
            GridColumn = 1
        };

        var maps = new List<string> { "Default", "Small", "Medium", "Large" };
        foreach (var map in maps)
        {
            _mapComboBox.Items.Add(new ListItem(map));
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
        var headerGrid = new Grid
        {
            ColumnSpacing = 20,
            GridRow = 0,
            Margin = new Thickness(0, 15, 0, 10),
            Width = 750
        };
        headerGrid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        headerGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));

        var headerLabel = new Label
        {
            Text = "Player Slots",
            TextColor = Color.Yellow,
            Scale = new Vector2(0.9f, 0.9f),
            GridColumn = 0,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        headerGrid.Widgets.Add(headerLabel);

        var countLabel = new Label
        {
            Text = "AI: 0 | Total: 1/8",
            TextColor = Color.LightGray,
            Scale = new Vector2(0.8f, 0.8f),
            GridColumn = 1,
            HorizontalAlignment = HorizontalAlignment.Right,
            Id = "CountLabel"
        };
        headerGrid.Widgets.Add(countLabel);

        outerGrid.Widgets.Add(headerGrid);

        // Player slots grid
        _playerSlotsGrid = new Grid
        {
            RowSpacing = 8,
            ColumnSpacing = 10,
            GridRow = 1,
            Width = 750,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        _playerSlotsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto)); // Slot label
        _playerSlotsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Fill)); // Player name
        _playerSlotsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto)); // AI badge
        _playerSlotsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto)); // Regenerate button
        _playerSlotsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto)); // Player type dropdown

        for (int i = 0; i < MaxVisibleSlots; i++)
        {
            _playerSlotsGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
            BuildPlayerSlotRow(i);
        }

        outerGrid.Widgets.Add(_playerSlotsGrid);

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
        if (_playerSlotsGrid == null) return;

        var slot = _playerSlots[slotIndex];
        int row = slotIndex;

        // Slot label
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
        _playerSlotsGrid.Widgets.Add(slotLabel);

        // Player name panel
        var namePanel = new Panel
        {
            Background = new SolidBrush(slot.IsAI ? new Color(40, 60, 80) : new Color(30, 40, 50)),
            Padding = new Thickness(10, 10),
            GridRow = row,
            GridColumn = 1,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Height = 40
        };

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
        _playerSlotsGrid.Widgets.Add(badgePanel);

        // Regenerate name button
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
        var comboBox = new ComboBox
        {
            Width = 150,
            GridRow = row,
            GridColumn = 4,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };

        comboBox.Items.Add(new ListItem("Human"));
        comboBox.Items.Add(new ListItem("Easy AI"));
        comboBox.Items.Add(new ListItem("Medium AI"));
        comboBox.Items.Add(new ListItem("Hard AI"));
        comboBox.SelectedIndex = 0;

        comboBox.SelectedIndexChanged += (s, a) =>
        {
            if (comboBox.SelectedIndex == null) return;

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
    }

    private void UpdatePlayerSlotUI(int slotIndex)
    {
        if (_playerSlotsGrid == null) return;

        var slot = _playerSlots[slotIndex];

        // Update name label
        var nameLabel = _playerSlotsGrid.Widgets.FirstOrDefault(w => w.Id == $"SlotName_{slotIndex}") as Label;
        if (nameLabel != null)
        {
            nameLabel.Text = slotIndex == 0 ? (_playerNameTextBox?.Text ?? "Player") : slot.PlayerName;
            nameLabel.TextColor = slot.IsAI ? Color.LightBlue : Color.White;
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
        var regenButton = _playerSlotsGrid.Widgets.FirstOrDefault(w => w.Id == $"Regen_{slotIndex}") as TextButton;
        if (regenButton != null)
        {
            regenButton.Visible = slot.IsAI;
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
            return parent;

        if (parent is Panel panel)
        {
            foreach (var child in panel.Widgets)
            {
                var found = FindWidgetById(child, id);
                if (found != null)
                    return found;
            }
        }
        else if (parent is Grid grid)
        {
            foreach (var child in grid.Widgets)
            {
                var found = FindWidgetById(child, id);
                if (found != null)
                    return found;
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

        _startGameButton = new TextButton
        {
            Text = "Start Game",
            Width = 180,
            Height = 50,
            GridColumn = 0
        };
        _startGameButton.Click += (s, a) =>
        {
            if (!string.IsNullOrWhiteSpace(_playerNameTextBox?.Text))
            {
                PlayerName = _playerNameTextBox.Text.Trim();
                SelectedMap = _mapComboBox?.SelectedItem?.Text ?? "Default";
                _playerSlots[0].PlayerName = PlayerName;
                ShouldStartGame = true;
            }
        };
        grid.Widgets.Add(_startGameButton);

        _backButton = new TextButton
        {
            Text = "Back",
            Width = 180,
            Height = 50,
            GridColumn = 1
        };
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

        if (_errorDisplayTime > 0)
        {
            _errorDisplayTime -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_errorDisplayTime <= 0)
            {
                _errorMessage = null;
                if (_errorLabel != null)
                    _errorLabel.Visible = false;
            }
        }

        if (keyState.IsKeyDown(Keys.Escape) && _previousKeyState.IsKeyUp(Keys.Escape))
        {
            ShouldGoBack = true;
        }

        _previousKeyState = keyState;
    }

    public void Reset()
    {
        ShouldStartGame = false;
        ShouldGoBack = false;
        _errorMessage = null;
        _errorDisplayTime = 0;

        if (_errorLabel != null)
            _errorLabel.Visible = false;

        for (int i = 0; i < _playerSlots.Count; i++)
        {
            if (i == 0)
            {
                _playerSlots[i].PlayerType = PlayerType.Human;
                _playerSlots[i].PlayerName = "Player";
                if (_playerTypeComboBoxes.Count > i)
                    _playerTypeComboBoxes[i].SelectedIndex = 0;
            }
            else
            {
                _playerSlots[i].PlayerType = PlayerType.Human;
                _playerSlots[i].PlayerName = $"Player {i + 1}";
                if (_playerTypeComboBoxes.Count > i)
                    _playerTypeComboBoxes[i].SelectedIndex = 0;
            }

            UpdatePlayerSlotUI(i);
        }

        UpdatePlayerCount();
    }

    public void SetError(string errorMessage)
    {
        _errorMessage = errorMessage;
        _errorDisplayTime = ErrorDisplayDuration;

        if (_errorLabel != null)
        {
            _errorLabel.Text = errorMessage;
            _errorLabel.Visible = true;
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        _desktop?.Render();
    }
}
