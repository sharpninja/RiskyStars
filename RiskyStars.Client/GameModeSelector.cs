using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Myra.Graphics2D.UI;
using MyraButton = Myra.Graphics2D.UI.Button;

namespace RiskyStars.Client;

public class GameModeSelector
{
    private readonly int _screenWidth;
    private readonly int _screenHeight;

    private Desktop? _desktop;
    private Panel? _mainPanel;
    private Panel? _multiplayerPanel;
    private Panel? _singlePlayerPanel;
    private MyraButton? _continueButton;
    private MyraButton? _backButton;
    private bool _isMultiplayerSelected = true;

    public GameMode? SelectedMode { get; private set; }
    public bool ShouldProceed { get; private set; }
    public bool ShouldGoBack { get; private set; }

    public GameModeSelector(GraphicsDevice graphicsDevice, int screenWidth, int screenHeight)
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
        int frameWidth = Math.Min(_screenWidth - 160, 980);
        var frame = ThemedUIFactory.CreateViewportFrame(frameWidth, Math.Min(_screenHeight - 140, 620));
        frame.HorizontalAlignment = HorizontalAlignment.Center;
        frame.VerticalAlignment = VerticalAlignment.Center;

        var layout = ThemedUIFactory.CreateVerticalStack(ThemeManager.Spacing.Large);
        layout.Widgets.Add(ThemedUIFactory.CreateHeaderPlate("Select Game Mode", "Choose the command environment for the next session"));

        var optionsGrid = ThemedUIFactory.CreateGrid(ThemeManager.Spacing.Large, ThemeManager.Spacing.Large);
        optionsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        optionsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Fill));

        _multiplayerPanel = CreateModeCard(
            "Multiplayer",
            "Authenticate with a lobby server and join or create a live session.",
            true);
        _multiplayerPanel.GridColumn = 0;
        _multiplayerPanel.TouchDown += (_, _) => SetModeSelection(true);
        optionsGrid.Widgets.Add(_multiplayerPanel);

        _singlePlayerPanel = CreateModeCard(
            "Single Player",
            "Build an offline lineup and launch directly against AI opponents.",
            false);
        _singlePlayerPanel.GridColumn = 1;
        _singlePlayerPanel.TouchDown += (_, _) => SetModeSelection(false);
        optionsGrid.Widgets.Add(_singlePlayerPanel);

        layout.Widgets.Add(optionsGrid);

        var buttons = ThemedUIFactory.CreateActionBar();
        buttons.HorizontalAlignment = HorizontalAlignment.Center;

        _continueButton = ThemedUIFactory.CreateButton("Continue", 200, ThemeManager.Sizes.ButtonMediumHeight, ThemeManager.ButtonTheme.Primary);
        _continueButton.Click += (_, _) =>
        {
            SelectedMode = _isMultiplayerSelected ? GameMode.Multiplayer : GameMode.SinglePlayer;
            ShouldProceed = true;
        };
        buttons.Widgets.Add(_continueButton);

        _backButton = ThemedUIFactory.CreateButton("Back", 180, ThemeManager.Sizes.ButtonMediumHeight, ThemeManager.ButtonTheme.Default);
        _backButton.Click += (_, _) => ShouldGoBack = true;
        buttons.Widgets.Add(_backButton);
        layout.Widgets.Add(buttons);

        frame.Widgets.Add(layout);

        _mainPanel = ThemedUIFactory.CreateScreenRoot(_screenWidth, _screenHeight);
        _mainPanel.Widgets.Add(frame);

        if (_desktop != null)
        {
            _desktop.Root = _mainPanel;
        }
    }

    private Panel CreateModeCard(string title, string description, bool selected)
    {
        var panel = ThemedUIFactory.CreateListRowPanel(selected);
        panel.Padding = ThemeManager.Padding.Large;

        var stack = ThemedUIFactory.CreateVerticalStack(ThemeManager.Spacing.Small);
        var titleLabel = ThemedUIFactory.CreateHeadingLabel(title);
        stack.Widgets.Add(titleLabel);

        var descriptionLabel = ThemedUIFactory.CreateSecondaryLabel(description);
        descriptionLabel.Wrap = true;
        descriptionLabel.Width = 320;
        descriptionLabel.TextColor = ThemeManager.Colors.TextPrimary;
        stack.Widgets.Add(descriptionLabel);
        panel.Widgets.Add(stack);

        return panel;
    }

    private void SetModeSelection(bool multiplayer)
    {
        _isMultiplayerSelected = multiplayer;
        if (_multiplayerPanel != null)
        {
            _multiplayerPanel.Background = multiplayer ? ThemeManager.AssetBrushes.ListRowSelected : ThemeManager.AssetBrushes.ListRowNormal;
        }

        if (_singlePlayerPanel != null)
        {
            _singlePlayerPanel.Background = multiplayer ? ThemeManager.AssetBrushes.ListRowNormal : ThemeManager.AssetBrushes.ListRowSelected;
        }
    }

    public void Update(GameTime gameTime, MouseState mouseState)
    {
        ShouldProceed = false;
        ShouldGoBack = false;
    }

    public void Reset()
    {
        SelectedMode = null;
        ShouldProceed = false;
        ShouldGoBack = false;
        SetModeSelection(true);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        _desktop?.Render();
    }
}
