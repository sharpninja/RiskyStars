using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Myra;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using System;
using System.Linq;
using MyraButton = Myra.Graphics2D.UI.Button;

namespace RiskyStars.Client;

#pragma warning disable CS0618
public class SinglePlayerLobbyScreen
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly int _screenWidth;
    private readonly int _screenHeight;
    private readonly int _contentWidth;
    private readonly int _settingsCardWidth;

    private SpriteFont? _font;

    private Desktop? _desktop;
    private DialogManager? _dialogManager;
    private Panel? _mainPanel;
    private ValidatedTextBox? _playerNameTextBox;
    private ComboBox? _mapComboBox;
    private Grid? _playerSlotsGrid;
    private MyraButton? _startGameButton;
    private MyraButton? _backButton;
    private Label? _opponentCountLabel;
    private Label? _footerStatusLabel;

    private readonly PlayerSlot[] _playerSlots;
    private readonly Label?[] _slotNameLabels = new Label?[MaxPlayers];
    private readonly Panel?[] _slotNamePanels = new Panel?[MaxPlayers];
    private readonly Panel?[] _slotBadgePanels = new Panel?[MaxPlayers];
    private readonly MyraButton?[] _regenerateNameButtons = new MyraButton?[MaxPlayers];
    private readonly ComboBox?[] _playerTypeComboBoxes = new ComboBox?[MaxPlayers];
    private ServerStatusIndicator? _serverStatusIndicator;
    private EmbeddedServerHost? _embeddedServerHost;

    private KeyboardState _previousKeyState;
    private bool _suppressSlotEvents;

    private const int MaxPlayers = 8;
    private static readonly string[] AvailableMaps = ["Default", "Small", "Medium", "Large"];

    public bool ShouldStartGame { get; private set; }
    public bool ShouldGoBack { get; private set; }
    public string PlayerName { get; private set; } = "Player";
    public string SelectedMap { get; private set; } = "Default";
    public List<PlayerSlot> PlayerSlots => _playerSlots.ToList();

    public SinglePlayerLobbyScreen(GraphicsDevice graphicsDevice, int screenWidth, int screenHeight)
    {
        _graphicsDevice = graphicsDevice;
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
        _contentWidth = Math.Max(780, Math.Min(screenWidth - 160, 1040));
        _settingsCardWidth = (_contentWidth - ThemeManager.Spacing.Large) / 2;

        _playerSlots = new PlayerSlot[MaxPlayers];
        for (int i = 0; i < MaxPlayers; i++)
        {
            _playerSlots[i] = new PlayerSlot(i + 1);
        }

        InitializeDefaultSlots();
    }

    public void LoadContent(SpriteFont font)
    {
        _font = font;
        _desktop = new Desktop();
        _dialogManager = new DialogManager(_desktop);
        BuildUI();
        UpdateAllSlotRows();
    }

    private void InitializeDefaultSlots()
    {
        for (int i = 0; i < MaxPlayers; i++)
        {
            _playerSlots[i].PlayerType = PlayerType.Human;
            _playerSlots[i].PlayerName = i == 0 ? "Player" : string.Empty;
            _playerSlots[i].IsReady = false;
            _playerSlots[i].IsHost = i == 0;
        }

        for (int i = 1; i <= 4; i++)
        {
            _playerSlots[i].PlayerType = PlayerType.MediumAI;
            _playerSlots[i].PlayerName = AINameGenerator.GenerateNameWithSeed(i + 1, "Medium");
            _playerSlots[i].IsReady = true;
        }
    }

    private void BuildUI()
    {
        var rootGrid = ThemedUIFactory.CreateGrid();
        rootGrid.Width = _screenWidth;
        rootGrid.Height = _screenHeight;
        rootGrid.RowsProportions.Add(new Proportion(ProportionType.Fill));
        rootGrid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));

        var contentStack = ThemedUIFactory.CreateSpaciousVerticalStack();
        contentStack.Width = _contentWidth;
        contentStack.HorizontalAlignment = HorizontalAlignment.Center;
        contentStack.VerticalAlignment = VerticalAlignment.Center;
        contentStack.Spacing = ThemeManager.Spacing.Large;

        contentStack.Widgets.Add(BuildHeaderSection());

        _serverStatusIndicator = new ServerStatusIndicator(_contentWidth);
        _serverStatusIndicator.Container.HorizontalAlignment = HorizontalAlignment.Center;
        contentStack.Widgets.Add(_serverStatusIndicator.Container);

        contentStack.Widgets.Add(BuildSetupSection());
        contentStack.Widgets.Add(BuildOpponentSection());
        contentStack.Widgets.Add(BuildButtonsSection());

        var viewportFrame = ThemedUIFactory.CreateViewportFrame(_contentWidth + 96);
        viewportFrame.HorizontalAlignment = HorizontalAlignment.Center;
        viewportFrame.VerticalAlignment = VerticalAlignment.Center;
        viewportFrame.Widgets.Add(contentStack);

        rootGrid.Widgets.Add(viewportFrame);

        _mainPanel = ThemedUIFactory.CreateScreenRoot(_screenWidth, _screenHeight);
        _mainPanel.Widgets.Add(rootGrid);

        if (_desktop != null)
        {
            _desktop.Root = _mainPanel;
        }
    }

    private Widget BuildHeaderSection()
    {
        return ThemedUIFactory.CreateHeaderPlate(
            "Single Player Game Setup",
            "Configure your commander, map, and AI opponents.",
            _contentWidth - 24);
    }

    private Widget BuildSetupSection()
    {
        var grid = ThemedUIFactory.CreateGrid(0, ThemeManager.Spacing.Large);
        grid.Width = _contentWidth;
        grid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        grid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));

        var playerCard = BuildPlayerNameCard();
        playerCard.GridColumn = 0;
        grid.Widgets.Add(playerCard);

        var mapCard = BuildMapCard();
        mapCard.GridColumn = 1;
        grid.Widgets.Add(mapCard);

        return grid;
    }

    private Panel BuildPlayerNameCard()
    {
        _playerNameTextBox = ThemedUIFactory.CreateValidatedPlayerNameBox(_settingsCardWidth - 40, showErrorLabel: true);
        _playerNameTextBox.Text = "Player";
        _playerNameTextBox.TextBox.TextChanged += (_, _) =>
        {
            UpdateHostNameFromInput();
            UpdatePlayerCount();
        };

        return BuildFieldCard(
            "Commander Name",
            _playerNameTextBox.Container,
            "This is the name shown for your human player.");
    }

    private Panel BuildMapCard()
    {
        _mapComboBox = ThemedUIFactory.CreateComboBox(_settingsCardWidth - 40);
        foreach (var map in AvailableMaps)
        {
            _mapComboBox.Items.Add(new ListItem(map));
        }
        _mapComboBox.SelectedIndex = 0;

        return BuildFieldCard(
            "Map Selection",
            _mapComboBox,
            "Choose a starting map size for the match.");
    }

    private Panel BuildFieldCard(string title, Widget content, string description)
    {
        var panel = ThemedUIFactory.CreateFramePanel();
        panel.Width = _settingsCardWidth;

        var stack = ThemedUIFactory.CreateCompactVerticalStack();
        stack.Spacing = ThemeManager.Spacing.Small;

        var titleLabel = ThemedUIFactory.CreateHeadingLabel(title);
        stack.Widgets.Add(titleLabel);

        var descriptionLabel = ThemedUIFactory.CreateSmallLabel(description);
        descriptionLabel.TextColor = ThemeManager.Colors.TextSecondary;
        stack.Widgets.Add(descriptionLabel);

        stack.Widgets.Add(content);
        panel.Widgets.Add(stack);

        return panel;
    }

    private Widget BuildOpponentSection()
    {
        var panel = ThemedUIFactory.CreateFramePanel();
        panel.Width = _contentWidth;

        var outerGrid = ThemedUIFactory.CreateGrid(ThemeManager.Spacing.Small, ThemeManager.Spacing.Small);
        outerGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        outerGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        outerGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        outerGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));

        var headerGrid = ThemedUIFactory.CreateGrid(0, ThemeManager.Spacing.Medium);
        headerGrid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        headerGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        headerGrid.GridRow = 0;

        var titleLabel = ThemedUIFactory.CreateHeadingLabel("Opponent Lineup");
        titleLabel.GridColumn = 0;
        headerGrid.Widgets.Add(titleLabel);

        _opponentCountLabel = ThemedUIFactory.CreateSecondaryLabel(string.Empty);
        _opponentCountLabel.Font = ThemeManager.UiFonts.Small;
        _opponentCountLabel.GridColumn = 1;
        headerGrid.Widgets.Add(_opponentCountLabel);

        outerGrid.Widgets.Add(headerGrid);

        var helperLabel = ThemedUIFactory.CreateSmallLabel("Turn slots off or assign an AI difficulty. Shuffle renames the active AI opponent.");
        helperLabel.TextColor = ThemeManager.Colors.TextSecondary;
        helperLabel.GridRow = 1;
        outerGrid.Widgets.Add(helperLabel);

        var columnHeaderGrid = ThemedUIFactory.CreateGrid(0, ThemeManager.Spacing.Medium);
        columnHeaderGrid.Width = _contentWidth - 70;
        columnHeaderGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        columnHeaderGrid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        columnHeaderGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        columnHeaderGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        columnHeaderGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        columnHeaderGrid.GridRow = 2;
        columnHeaderGrid.Margin = new Thickness(0, ThemeManager.Spacing.Small, 0, 0);

        columnHeaderGrid.Widgets.Add(CreateColumnHeader("Slot", 0, 74));
        columnHeaderGrid.Widgets.Add(CreateColumnHeader("Commander", 1));
        columnHeaderGrid.Widgets.Add(CreateColumnHeader("Role", 2, 96));
        columnHeaderGrid.Widgets.Add(CreateColumnHeader("Shuffle", 3, 100));
        columnHeaderGrid.Widgets.Add(CreateColumnHeader("Assignment", 4, 180));
        outerGrid.Widgets.Add(columnHeaderGrid);

        _playerSlotsGrid = ThemedUIFactory.CreateGrid(ThemeManager.Spacing.Small, ThemeManager.Spacing.Medium);
        _playerSlotsGrid.Width = _contentWidth - 70;
        _playerSlotsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        _playerSlotsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        _playerSlotsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        _playerSlotsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        _playerSlotsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));

        for (int i = 0; i < MaxPlayers; i++)
        {
            _playerSlotsGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
            BuildPlayerSlotRow(i);
        }

        var scrollViewer = new ScrollViewer
        {
            Content = _playerSlotsGrid,
            GridRow = 3,
            Height = 360,
            ShowVerticalScrollBar = true,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top
        };
        outerGrid.Widgets.Add(scrollViewer);

        panel.Widgets.Add(outerGrid);
        return panel;
    }

    private static Label CreateColumnHeader(string text, int column, int? width = null)
    {
        var label = ThemedUIFactory.CreateSmallLabel(text);
        label.TextColor = ThemeManager.Colors.TextSecondary;
        label.GridColumn = column;
        if (width.HasValue)
        {
            label.Width = width.Value;
        }

        return label;
    }

    private void BuildPlayerSlotRow(int slotIndex)
    {
        if (_playerSlotsGrid == null)
        {
            return;
        }

        var slotLabel = ThemedUIFactory.CreateSmallLabel(slotIndex == 0 ? "You" : $"Slot {slotIndex + 1}");
        slotLabel.Width = 74;
        slotLabel.VerticalAlignment = VerticalAlignment.Center;
        slotLabel.GridRow = slotIndex;
        slotLabel.GridColumn = 0;
        _playerSlotsGrid.Widgets.Add(slotLabel);

        var namePanel = ThemedUIFactory.CreateDarkPanel();
        namePanel.Padding = ThemeManager.Padding.Medium;
        namePanel.Height = 42;
        namePanel.GridRow = slotIndex;
        namePanel.GridColumn = 1;
        namePanel.HorizontalAlignment = HorizontalAlignment.Stretch;

        var nameLabel = new Label
        {
            Font = ThemeManager.UiFonts.Body,
            VerticalAlignment = VerticalAlignment.Center,
            Scale = Vector2.One
        };
        namePanel.Widgets.Add(nameLabel);

        _slotNamePanels[slotIndex] = namePanel;
        _slotNameLabels[slotIndex] = nameLabel;
        _playerSlotsGrid.Widgets.Add(namePanel);

        var badgePanel = ThemedUIFactory.CreateBadgePanel(ThemeManager.Colors.BorderNormal, "OPEN");
        badgePanel.Width = 96;
        badgePanel.GridRow = slotIndex;
        badgePanel.GridColumn = 2;
        _slotBadgePanels[slotIndex] = badgePanel;
        _playerSlotsGrid.Widgets.Add(badgePanel);

        var shuffleButton = ThemedUIFactory.CreateButton("Shuffle", 100, 38, ThemeManager.ButtonTheme.Default);
        shuffleButton.GridRow = slotIndex;
        shuffleButton.GridColumn = 3;
        shuffleButton.Visible = slotIndex > 0;
        shuffleButton.Click += (_, _) => RegenerateOpponentName(slotIndex);
        _regenerateNameButtons[slotIndex] = shuffleButton;
        _playerSlotsGrid.Widgets.Add(shuffleButton);

        if (slotIndex == 0)
        {
            var hostRoleWidget = BuildLockedRoleWidget("Human");
            hostRoleWidget.GridRow = slotIndex;
            hostRoleWidget.GridColumn = 4;
            _playerSlotsGrid.Widgets.Add(hostRoleWidget);
        }
        else
        {
            var comboBox = ThemedUIFactory.CreateComboBox(180);
            comboBox.Items.Add(new ListItem("Off"));
            comboBox.Items.Add(new ListItem("Easy AI"));
            comboBox.Items.Add(new ListItem("Medium AI"));
            comboBox.Items.Add(new ListItem("Hard AI"));
            comboBox.GridRow = slotIndex;
            comboBox.GridColumn = 4;
            comboBox.SelectedIndexChanged += (_, _) =>
            {
                if (_suppressSlotEvents)
                {
                    return;
                }

                ApplyOpponentSelection(slotIndex, comboBox.SelectedIndex ?? 0);
            };

            _playerTypeComboBoxes[slotIndex] = comboBox;
            _playerSlotsGrid.Widgets.Add(comboBox);
        }

        UpdatePlayerSlotUI(slotIndex);
    }

    private static Panel BuildLockedRoleWidget(string text)
    {
        var panel = ThemedUIFactory.CreateDarkPanel();
        panel.Width = 180;
        panel.Height = 42;
        panel.Padding = ThemeManager.Padding.Medium;

        var label = ThemedUIFactory.CreateSecondaryLabel(text);
        label.VerticalAlignment = VerticalAlignment.Center;
        label.TextColor = ThemeManager.Colors.TextPrimary;
        panel.Widgets.Add(label);

        return panel;
    }

    private void ApplyOpponentSelection(int slotIndex, int selectionIndex)
    {
        if (slotIndex <= 0 || slotIndex >= MaxPlayers)
        {
            return;
        }

        var slot = _playerSlots[slotIndex];
        var newType = selectionIndex switch
        {
            1 => PlayerType.EasyAI,
            2 => PlayerType.MediumAI,
            3 => PlayerType.HardAI,
            _ => PlayerType.Human
        };

        if (newType == PlayerType.Human)
        {
            slot.PlayerType = PlayerType.Human;
            slot.PlayerName = string.Empty;
            slot.IsReady = false;
        }
        else
        {
            var shouldGenerateName = slot.PlayerType != newType || string.IsNullOrWhiteSpace(slot.PlayerName);
            slot.PlayerType = newType;
            slot.IsReady = true;

            if (shouldGenerateName)
            {
                slot.PlayerName = AINameGenerator.GenerateNameWithSeed(slotIndex + 1, slot.GetDifficultyLevel());
            }
        }

        UpdatePlayerSlotUI(slotIndex);
        UpdatePlayerCount();
    }

    private void RegenerateOpponentName(int slotIndex)
    {
        if (slotIndex <= 0 || slotIndex >= MaxPlayers)
        {
            return;
        }

        var slot = _playerSlots[slotIndex];
        if (!slot.IsAI)
        {
            return;
        }

        slot.PlayerName = AINameGenerator.GenerateName(slotIndex + 1, slot.GetDifficultyLevel());
        UpdatePlayerSlotUI(slotIndex);
    }

    private void UpdateAllSlotRows()
    {
        for (int i = 0; i < MaxPlayers; i++)
        {
            UpdatePlayerSlotUI(i);
        }

        UpdatePlayerCount();
    }

    private void UpdatePlayerSlotUI(int slotIndex)
    {
        var slot = _playerSlots[slotIndex];
        var nameLabel = _slotNameLabels[slotIndex];
        var namePanel = _slotNamePanels[slotIndex];
        var badgePanel = _slotBadgePanels[slotIndex];
        var shuffleButton = _regenerateNameButtons[slotIndex];
        var comboBox = _playerTypeComboBoxes[slotIndex];

        if (nameLabel == null || namePanel == null || badgePanel == null)
        {
            return;
        }

        if (slotIndex == 0)
        {
            var hostName = string.IsNullOrWhiteSpace(_playerNameTextBox?.Text) ? "Player" : _playerNameTextBox!.Text.Trim();
            slot.PlayerName = hostName;

            nameLabel.Text = hostName;
            nameLabel.TextColor = ThemeManager.Colors.TextPrimary;
            namePanel.Background = ThemeManager.AssetBrushes.ListRowSelected;
            namePanel.Border = ThemeManager.CreateSolidBrush(ThemeManager.Colors.BorderFocus);
            namePanel.BorderThickness = new Thickness(ThemeManager.BorderThickness.Normal);
            UpdateBadgePanel(badgePanel, ThemeManager.Colors.TextAccent, "HOST");

            if (shuffleButton != null)
            {
                shuffleButton.Visible = false;
            }

            return;
        }

        if (slot.IsAI)
        {
            nameLabel.Text = slot.PlayerName;
            nameLabel.TextColor = ThemeManager.Colors.TextPrimary;
            namePanel.Background = ThemeManager.CreateSolidBrush(ThemeManager.Colors.SlotPanelReady);
            namePanel.Border = ThemeManager.CreateSolidBrush(ThemeManager.Colors.BorderFocus);
            namePanel.BorderThickness = new Thickness(ThemeManager.BorderThickness.Thin);

            var badgeColor = slot.PlayerType switch
            {
                PlayerType.EasyAI => ThemeManager.Colors.AIEasyColor,
                PlayerType.MediumAI => ThemeManager.Colors.AIMediumColor,
                PlayerType.HardAI => ThemeManager.Colors.AIHardColor,
                _ => ThemeManager.Colors.BorderNormal
            };

            UpdateBadgePanel(badgePanel, badgeColor, slot.GetDifficultyLevel());

            if (shuffleButton != null)
            {
                shuffleButton.Visible = true;
                shuffleButton.Enabled = true;
            }
        }
        else
        {
            nameLabel.Text = "Open slot";
            nameLabel.TextColor = ThemeManager.Colors.TextSecondary;
            namePanel.Background = ThemeManager.CreateSolidBrush(ThemeManager.Colors.SlotPanelNormal);
            namePanel.Border = ThemeManager.CreateSolidBrush(ThemeManager.Colors.BorderNormal);
            namePanel.BorderThickness = new Thickness(ThemeManager.BorderThickness.Thin);
            UpdateBadgePanel(badgePanel, ThemeManager.Colors.DisabledColor, "OFF", ThemeManager.Colors.TextSecondary);

            if (shuffleButton != null)
            {
                shuffleButton.Visible = false;
            }
        }

        if (comboBox != null)
        {
            var desiredIndex = slot.PlayerType switch
            {
                PlayerType.EasyAI => 1,
                PlayerType.MediumAI => 2,
                PlayerType.HardAI => 3,
                _ => 0
            };

            if (comboBox.SelectedIndex != desiredIndex)
            {
                _suppressSlotEvents = true;
                comboBox.SelectedIndex = desiredIndex;
                _suppressSlotEvents = false;
            }
        }
    }

    private static void UpdateBadgePanel(Panel panel, Color color, string text, Color? textColor = null)
    {
        panel.Background = ThemeManager.CreateSolidBrush(color * 0.85f);
        panel.Border = ThemeManager.CreateSolidBrush(color);
        panel.BorderThickness = new Thickness(ThemeManager.BorderThickness.Thin);
        panel.Widgets.Clear();

        var label = new Label
        {
            Text = text.ToUpperInvariant(),
            Font = ThemeManager.UiFonts.Tiny,
            TextColor = textColor ?? ThemeManager.Colors.TextPrimary,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        panel.Widgets.Add(label);
    }

    private void UpdateHostNameFromInput()
    {
        if (_playerNameTextBox == null)
        {
            return;
        }

        var proposedName = string.IsNullOrWhiteSpace(_playerNameTextBox.Text)
            ? "Player"
            : _playerNameTextBox.Text.Trim();

        if (_playerSlots[0].PlayerName == proposedName)
        {
            return;
        }

        _playerSlots[0].PlayerName = proposedName;
        UpdatePlayerSlotUI(0);
    }

    private void UpdatePlayerCount()
    {
        int aiCount = _playerSlots.Skip(1).Count(slot => slot.IsAI);
        int totalPlayers = 1 + aiCount;

        if (_opponentCountLabel != null)
        {
            var opponentLabel = aiCount == 1 ? "1 AI opponent" : $"{aiCount} AI opponents";
            _opponentCountLabel.Text = $"{opponentLabel}  |  {totalPlayers} total players";
        }

        if (_footerStatusLabel != null)
        {
            if (aiCount == 0)
            {
                _footerStatusLabel.Text = "Add at least one AI opponent before starting the game.";
                _footerStatusLabel.TextColor = ThemeManager.Colors.TextWarning;
            }
            else
            {
                _footerStatusLabel.Text = aiCount == 1
                    ? "Ready to launch a duel against one AI opponent."
                    : $"Ready to launch a match with {aiCount} AI opponents.";
                _footerStatusLabel.TextColor = ThemeManager.Colors.TextSuccess;
            }
        }

        if (_startGameButton != null)
        {
            _startGameButton.Enabled = aiCount > 0 && (_playerNameTextBox?.IsValid ?? true);
        }
    }

    private Widget BuildButtonsSection()
    {
        var stack = ThemedUIFactory.CreateCompactVerticalStack();
        stack.HorizontalAlignment = HorizontalAlignment.Center;
        stack.Spacing = ThemeManager.Spacing.Small;

        var buttonsRow = ThemedUIFactory.CreateHorizontalStack(ThemeManager.Spacing.Large);
        buttonsRow.HorizontalAlignment = HorizontalAlignment.Center;

        _startGameButton = ThemedUIFactory.CreateButton("Start Game", 220, 52, ThemeManager.ButtonTheme.Primary);
        _startGameButton.Click += (_, _) =>
        {
            if (_playerNameTextBox == null || !_playerNameTextBox.IsValid)
            {
                _dialogManager?.ShowError("Validation Error", "Enter a valid commander name before starting.");
                return;
            }

            int aiCount = _playerSlots.Skip(1).Count(slot => slot.IsAI);
            if (aiCount == 0)
            {
                _dialogManager?.ShowError("Lineup Incomplete", "Add at least one AI opponent before starting.");
                return;
            }

            PlayerName = _playerNameTextBox.Text.Trim();
            SelectedMap = _mapComboBox?.SelectedItem?.Text ?? "Default";
            _playerSlots[0].PlayerName = PlayerName;
            ShouldStartGame = true;
        };
        buttonsRow.Widgets.Add(_startGameButton);

        _backButton = ThemedUIFactory.CreateButton("Back", 180, 52, ThemeManager.ButtonTheme.Default);
        _backButton.Click += (_, _) => ShouldGoBack = true;
        buttonsRow.Widgets.Add(_backButton);

        stack.Widgets.Add(buttonsRow);

        _footerStatusLabel = ThemedUIFactory.CreateSmallLabel(string.Empty);
        _footerStatusLabel.HorizontalAlignment = HorizontalAlignment.Center;
        stack.Widgets.Add(_footerStatusLabel);

        return stack;
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

        UpdateHostNameFromInput();
        UpdatePlayerCount();
        _serverStatusIndicator?.Update();

        _previousKeyState = keyState;
    }

    public void SetEmbeddedServerHost(EmbeddedServerHost? serverHost)
    {
        _embeddedServerHost = serverHost;
        if (_serverStatusIndicator == null)
        {
            return;
        }

        if (serverHost != null)
        {
            _serverStatusIndicator.SetServerHost(serverHost);
        }
        else
        {
            _serverStatusIndicator.Update();
        }
    }

    public void Reset()
    {
        ShouldStartGame = false;
        ShouldGoBack = false;
        PlayerName = "Player";
        SelectedMap = "Default";

        InitializeDefaultSlots();

        if (_playerNameTextBox != null)
        {
            _playerNameTextBox.Text = "Player";
            _playerNameTextBox.ValidateInput();
        }

        if (_mapComboBox != null)
        {
            _mapComboBox.SelectedIndex = 0;
        }

        UpdateAllSlotRows();
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
#pragma warning restore CS0618
