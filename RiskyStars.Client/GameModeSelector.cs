using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Myra;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.Brushes;

namespace RiskyStars.Client;

public class GameModeSelector
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly int _screenWidth;
    private readonly int _screenHeight;
    private SpriteFont? _font;

    private Desktop? _desktop;
    private Panel? _mainPanel;
    private TextButton? _multiplayerButton;
    private TextButton? _singlePlayerButton;
    private TextButton? _continueButton;
    private TextButton? _backButton;
    private bool _isMultiplayerSelected = true;

    public GameMode? SelectedMode { get; private set; }
    public bool ShouldProceed { get; private set; }
    public bool ShouldGoBack { get; private set; }

    public GameModeSelector(GraphicsDevice graphicsDevice, int screenWidth, int screenHeight)
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
            VerticalAlignment = VerticalAlignment.Center,
            Width = _screenWidth,
            Height = _screenHeight
        };

        rootGrid.RowsProportions.Add(new Proportion(ProportionType.Auto)); // Title
        rootGrid.RowsProportions.Add(new Proportion(ProportionType.Auto)); // Subtitle
        rootGrid.RowsProportions.Add(new Proportion(ProportionType.Auto)); // Options panel
        rootGrid.RowsProportions.Add(new Proportion(ProportionType.Auto)); // Buttons

        // Title
#pragma warning disable CS0618 // Type or member is obsolete
        var titleLabel = new Label
        {
            Text = "Select Game Mode",
            TextColor = Color.Cyan,
            Scale = new Vector2(2.0f, 2.0f),
            HorizontalAlignment = HorizontalAlignment.Center,
            GridRow = 0,
            Margin = new Thickness(0, 100, 0, 10)
        };
#pragma warning restore CS0618 // Type or member is obsolete
        // Version Label
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        var versionLabel = new Label
        {
            Text = $"v{version.Major}.{version.Minor}.{version.Build}-{version.Revision}",
            TextColor = Color.Gray * 0.7f,
            Scale = new Vector2(0.6f, 0.6f),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(10, 10, 0, 0)
        };
        rootGrid.Widgets.Add(versionLabel);

        rootGrid.Widgets.Add(titleLabel);

        // Subtitle
#pragma warning disable CS0618 // Type or member is obsolete
        var subtitleLabel = new Label
        {
            Text = "Choose how you want to play",
            TextColor = Color.White,
            Scale = new Vector2(0.9f, 0.9f),
            HorizontalAlignment = HorizontalAlignment.Center,
            GridRow = 1,
            Margin = new Thickness(0, 0, 0, 40)
        };
#pragma warning restore CS0618 // Type or member is obsolete
        rootGrid.Widgets.Add(subtitleLabel);

        // Options Panel
        var optionsPanel = new Panel
        {
            Width = 600,
            Padding = new Thickness(40, 30),
            Background = new SolidBrush(new Color(0, 0, 0, 220)),
            Border = new SolidBrush(Color.Cyan),
            BorderThickness = new Thickness(2),
            HorizontalAlignment = HorizontalAlignment.Center,
            GridRow = 2
        };

        var optionsGrid = new Grid
        {
            RowSpacing = 30,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        optionsGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        optionsGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));

        // Multiplayer Button
        _multiplayerButton = new TextButton
        {
            Text = "Multiplayer - Connect to server and play with others",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(10, 15),
            Background = new SolidBrush(Color.Cyan * 0.3f),
            GridRow = 0
        };
        
        // Single Player Button
        _singlePlayerButton = new TextButton
        {
            Text = "Single Player - Play offline against AI",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(10, 15),
            Background = new SolidBrush(Color.Transparent),
            GridRow = 1
        };

        // Selection handlers
        _multiplayerButton.Click += (s, a) =>
        {
            _isMultiplayerSelected = true;
            _multiplayerButton.Background = new SolidBrush(Color.Cyan * 0.3f);
            _singlePlayerButton.Background = new SolidBrush(Color.Transparent);
        };

        _singlePlayerButton.Click += (s, a) =>
        {
            _isMultiplayerSelected = false;
            _singlePlayerButton.Background = new SolidBrush(Color.Cyan * 0.3f);
            _multiplayerButton.Background = new SolidBrush(Color.Transparent);
        };

        optionsGrid.Widgets.Add(_multiplayerButton);
        optionsGrid.Widgets.Add(_singlePlayerButton);
        optionsPanel.Widgets.Add(optionsGrid);
        rootGrid.Widgets.Add(optionsPanel);

        // Buttons Panel
        var buttonsGrid = new Grid
        {
            ColumnSpacing = 20,
            HorizontalAlignment = HorizontalAlignment.Center,
            GridRow = 3,
            Margin = new Thickness(0, 40, 0, 0)
        };

        buttonsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        buttonsGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));

#pragma warning disable CS0618 // Type or member is obsolete
        _continueButton = new TextButton
        {
            Text = "Continue",
            Width = 150,
            Height = 50,
            GridColumn = 0
        };
#pragma warning restore CS0618 // Type or member is obsolete

        _continueButton.Click += (s, a) =>
        {
            SelectedMode = _isMultiplayerSelected ? GameMode.Multiplayer : GameMode.SinglePlayer;
            ShouldProceed = true;
        };

#pragma warning disable CS0618 // Type or member is obsolete
        _backButton = new TextButton
        {
            Text = "Back",
            Width = 150,
            Height = 50,
            GridColumn = 1
        };
#pragma warning restore CS0618 // Type or member is obsolete

        _backButton.Click += (s, a) => 
        { 
            ShouldGoBack = true; 
        };

        buttonsGrid.Widgets.Add(_continueButton);
        buttonsGrid.Widgets.Add(_backButton);
        rootGrid.Widgets.Add(buttonsGrid);

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
        _isMultiplayerSelected = true;
        
        if (_multiplayerButton != null)
            _multiplayerButton.Background = new SolidBrush(Color.Cyan * 0.3f);
        if (_singlePlayerButton != null)
            _singlePlayerButton.Background = new SolidBrush(Color.Transparent);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        _desktop?.Render();
    }
}