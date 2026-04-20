using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Myra.Graphics2D.UI;
using RiskyStars.Shared;
using MyraButton = Myra.Graphics2D.UI.Button;

namespace RiskyStars.Client;

public class CreateLobbyScreen
{
    private int _screenWidth;
    private int _screenHeight;

    private Desktop? _desktop;
    private Panel? _mainPanel;
    private ValidatedTextBox? _mapNameTextBox;
    private SpinButton? _maxPlayersSpinButton;
    private MyraButton? _createButton;
    private MyraButton? _cancelButton;
    private KeyboardState _previousKeyState;

    public bool ShouldCreate { get; private set; }
    public bool ShouldCancel { get; private set; }
    public LobbySettingsProto? LobbySettings { get; private set; }
    private string _mapNameDraft = "Default";
    private int _maxPlayersDraft = 4;

    public CreateLobbyScreen(GraphicsDevice graphicsDevice, int screenWidth, int screenHeight)
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
        int frameWidth = ThemedUIFactory.ResolveResponsiveExtent(_screenWidth, 180, 920);
        int frameHeight = ThemedUIFactory.ResolveResponsiveExtent(_screenHeight, 140, 620);
        var frame = ThemedUIFactory.CreateViewportFrame(frameWidth, frameHeight);
        frame.HorizontalAlignment = HorizontalAlignment.Center;
        frame.VerticalAlignment = VerticalAlignment.Center;

        var layout = ThemedUIFactory.CreateVerticalStack(ThemeManager.Spacing.Large);
        layout.Widgets.Add(ThemedUIFactory.CreateHeaderPlate("Create Lobby", "Define the session shell before inviting commanders"));

        int cardWidth = (frameWidth - 96 - ThemeManager.Spacing.Large) / 2;
        var cards = ThemedUIFactory.CreateGrid(ThemeManager.Spacing.Large, ThemeManager.Spacing.Large);
        cards.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        cards.ColumnsProportions.Add(new Proportion(ProportionType.Fill));

        _mapNameTextBox = new ValidatedTextBox(cardWidth - 40, "Enter map name", showErrorLabel: true);
        _mapNameTextBox.Text = _mapNameDraft;
        _mapNameTextBox.SetValidator(InputValidator.ValidateMapName);
        var mapCard = ThemedUIFactory.CreateFieldCard("Map Selection", "Choose the map preset or scenario name for this lobby.", _mapNameTextBox.Container, cardWidth);
        mapCard.GridColumn = 0;
        cards.Widgets.Add(mapCard);

        _maxPlayersSpinButton = ThemedUIFactory.CreateSpinButton(_maxPlayersDraft, 2, 6);
        _maxPlayersSpinButton.Width = cardWidth - 40;
        var playerCard = ThemedUIFactory.CreateFieldCard("Maximum Commanders", "Set the player capacity for the session. Current implementation supports 2-6.", _maxPlayersSpinButton, cardWidth);
        playerCard.GridColumn = 1;
        cards.Widgets.Add(playerCard);

        layout.Widgets.Add(cards);

        var actionBar = ThemedUIFactory.CreateActionBar();
        actionBar.HorizontalAlignment = HorizontalAlignment.Center;

        _createButton = ThemedUIFactory.CreateButton("Create Lobby", 220, ThemeManager.Sizes.ButtonMediumHeight, ThemeManager.ButtonTheme.Primary);
        _createButton.Click += (_, _) =>
        {
            if (_mapNameTextBox == null || !_mapNameTextBox.ValidateInput().IsValid)
            {
                return;
            }

            if (TryCreateLobbySettings(out var settings))
            {
                LobbySettings = settings;
                ShouldCreate = true;
            }
        };
        actionBar.Widgets.Add(_createButton);

        _cancelButton = ThemedUIFactory.CreateButton("Cancel", 180, ThemeManager.Sizes.ButtonMediumHeight, ThemeManager.ButtonTheme.Default);
        _cancelButton.Click += (_, _) => ShouldCancel = true;
        actionBar.Widgets.Add(_cancelButton);

        layout.Widgets.Add(actionBar);
        frame.Widgets.Add(ThemedUIFactory.CreateAutoScrollViewer(layout, frameHeight - 96));

        _mainPanel = ThemedUIFactory.CreateScreenRoot(_screenWidth, _screenHeight);
        _mainPanel.Widgets.Add(frame);

        if (_desktop != null)
        {
            _desktop.Root = _mainPanel;
        }
    }

    private bool TryCreateLobbySettings(out LobbySettingsProto? settings)
    {
        settings = null;

        if (_mapNameTextBox == null || !_mapNameTextBox.ValidateInput().IsValid)
        {
            return false;
        }

        int maxPlayers = (int)(_maxPlayersSpinButton?.Value ?? 4);
        var playerCountValidation = InputValidator.ValidatePlayerCount(maxPlayers, 2, 6);
        if (!playerCountValidation.IsValid)
        {
            return false;
        }

        settings = new LobbySettingsProto
        {
            MinPlayers = 2,
            MaxPlayers = maxPlayers,
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
        {
            _mapNameTextBox.Text = "Default";
                _mapNameTextBox.ValidateInput();
        }

        if (_maxPlayersSpinButton != null)
        {
            _maxPlayersSpinButton.Value = 4;
        }

        _mapNameDraft = "Default";
        _maxPlayersDraft = 4;
    }

    public void ResizeViewport(int screenWidth, int screenHeight)
    {
        if (screenWidth <= 0 || screenHeight <= 0)
        {
            return;
        }

        _mapNameDraft = _mapNameTextBox?.Text ?? _mapNameDraft;
        _maxPlayersDraft = (int)Math.Round(_maxPlayersSpinButton?.Value ?? _maxPlayersDraft);
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
        BuildUI();
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        _desktop?.Render();
    }
}
