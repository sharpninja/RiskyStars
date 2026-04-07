using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RiskyStars.Shared;
using Myra;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.Brushes;

namespace RiskyStars.Client;

public class CreateLobbyScreen
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly int _screenWidth;
    private readonly int _screenHeight;
    private SpriteFont? _font;

    private Desktop? _desktop;
    private Panel? _mainPanel;
    private TextBox? _mapNameTextBox;
    private SpinButton? _maxPlayersSpinButton;
    private TextButton? _createButton;
    private TextButton? _cancelButton;

    private KeyboardState _previousKeyState;

    public bool ShouldCreate { get; private set; }
    public bool ShouldCancel { get; private set; }
    public LobbySettingsProto? LobbySettings { get; private set; }

    public CreateLobbyScreen(GraphicsDevice graphicsDevice, int screenWidth, int screenHeight)
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
            RowSpacing = 20,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        rootGrid.RowsProportions.Add(new Proportion(ProportionType.Auto)); // Title
        rootGrid.RowsProportions.Add(new Proportion(ProportionType.Auto)); // Map name
        rootGrid.RowsProportions.Add(new Proportion(ProportionType.Auto)); // Max players
        rootGrid.RowsProportions.Add(new Proportion(ProportionType.Auto)); // Buttons

        // Title
        var titleLabel = new Label
        {
            Text = "Create Lobby",
            TextColor = Color.Cyan,
            Scale = new Vector2(1.5f, 1.5f),
            HorizontalAlignment = HorizontalAlignment.Center,
            GridRow = 0,
            Margin = new Thickness(0, 0, 0, 20)
        };
        rootGrid.Widgets.Add(titleLabel);

        // Map name field
        var mapNamePanel = BuildMapNameField();
        mapNamePanel.GridRow = 1;
        rootGrid.Widgets.Add(mapNamePanel);

        // Max players field
        var maxPlayersPanel = BuildMaxPlayersField();
        maxPlayersPanel.GridRow = 2;
        rootGrid.Widgets.Add(maxPlayersPanel);

        // Buttons
        var buttonsPanel = BuildButtonsPanel();
        buttonsPanel.GridRow = 3;
        rootGrid.Widgets.Add(buttonsPanel);

        // Main container
        var containerPanel = new Panel
        {
            Width = 550,
            Padding = new Thickness(40, 30),
            Background = new SolidBrush(new Color(0, 0, 0, 220)),
            Border = new SolidBrush(Color.Cyan),
            BorderThickness = new Thickness(3),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        containerPanel.Widgets.Add(rootGrid);

        _mainPanel = new Panel
        {
            Width = _screenWidth,
            Height = _screenHeight,
            Background = new SolidBrush(new Color(10, 10, 20))
        };
        _mainPanel.Widgets.Add(containerPanel);

        if (_desktop != null)
            _desktop.Root = _mainPanel;
    }

    private Panel BuildMapNameField()
    {
        var grid = new Grid
        {
            RowSpacing = 8,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        grid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        grid.RowsProportions.Add(new Proportion(ProportionType.Auto));

        var label = new Label
        {
            Text = "Map Name",
            TextColor = Color.White,
            Scale = new Vector2(0.9f, 0.9f),
            GridRow = 0
        };
        grid.Widgets.Add(label);

        _mapNameTextBox = new TextBox
        {
            Text = "Default",
            Width = 450,
            GridRow = 1,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        grid.Widgets.Add(_mapNameTextBox);

        var panel = new Panel
        {
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        panel.Widgets.Add(grid);

        return panel;
    }

    private Panel BuildMaxPlayersField()
    {
        var grid = new Grid
        {
            RowSpacing = 8,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        grid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        grid.RowsProportions.Add(new Proportion(ProportionType.Auto));

        var label = new Label
        {
            Text = "Max Players (2-6)",
            TextColor = Color.White,
            Scale = new Vector2(0.9f, 0.9f),
            GridRow = 0
        };
        grid.Widgets.Add(label);

        _maxPlayersSpinButton = new SpinButton
        {
            Width = 450,
            GridRow = 1,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Minimum = 2,
            Maximum = 6,
            Value = 4
        };
        grid.Widgets.Add(_maxPlayersSpinButton);

        var panel = new Panel
        {
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        panel.Widgets.Add(grid);

        return panel;
    }

    private Panel BuildButtonsPanel()
    {
        var grid = new Grid
        {
            ColumnSpacing = 20,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 20, 0, 0)
        };

        grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        grid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));

        _createButton = new TextButton
        {
            Text = "Create",
            Width = 180,
            Height = 50,
            GridColumn = 0
        };
        _createButton.Click += (s, a) =>
        {
            if (TryCreateLobbySettings(out var settings))
            {
                LobbySettings = settings;
                ShouldCreate = true;
            }
        };
        grid.Widgets.Add(_createButton);

        _cancelButton = new TextButton
        {
            Text = "Cancel",
            Width = 180,
            Height = 50,
            GridColumn = 1
        };
        _cancelButton.Click += (s, a) => { ShouldCancel = true; };
        grid.Widgets.Add(_cancelButton);

        var panel = new Panel
        {
            HorizontalAlignment = HorizontalAlignment.Center
        };
        panel.Widgets.Add(grid);

        return panel;
    }

    private bool TryCreateLobbySettings(out LobbySettingsProto? settings)
    {
        settings = null;

        if (string.IsNullOrWhiteSpace(_mapNameTextBox?.Text))
            return false;

        settings = new LobbySettingsProto
        {
            MinPlayers = 2,
            MaxPlayers = (int)(_maxPlayersSpinButton?.Value ?? 4),
            GameMode = "Standard",
            MapName = _mapNameTextBox.Text.Trim(),
            StartingPopulation = 100,
            StartingMetal = 50,
            StartingFuel = 50,
            AllowSpectators = false,
            TurnTimeLimit = 300
        };

        return true;
    }

    public void Update(GameTime gameTime, MouseState mouseState, KeyboardState keyState)
    {
        ShouldCreate = false;
        ShouldCancel = false;

        if (keyState.IsKeyDown(Keys.Escape) && _previousKeyState.IsKeyUp(Keys.Escape))
        {
            ShouldCancel = true;
        }

        _previousKeyState = keyState;
    }

    public void Reset()
    {
        ShouldCreate = false;
        ShouldCancel = false;
        LobbySettings = null;

        if (_mapNameTextBox != null)
            _mapNameTextBox.Text = "Default";
        if (_maxPlayersSpinButton != null)
            _maxPlayersSpinButton.Value = 4;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        _desktop?.Render();
    }
}
