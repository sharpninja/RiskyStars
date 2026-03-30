using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RiskyStars.Shared;

namespace RiskyStars.Client;

public class RiskyStarsGame : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch? _spriteBatch;
    private GrpcGameClient? _gameClient;
    private GameStateCache? _gameStateCache;
    
    private Camera2D? _camera;
    private MapRenderer? _mapRenderer;
    private RegionRenderer? _regionRenderer;
    private UIRenderer? _uiRenderer;
    private SelectionRenderer? _selectionRenderer;
    private InputController? _inputController;
    
    private MapData? _mapData;
    private SpriteFont? _defaultFont;
    
    private string? _currentPlayerId;
    private bool _showDebug = true;
    private KeyboardState _previousKeyState;

    public RiskyStarsGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        _gameClient = new GrpcGameClient("http://localhost:5000");
        _gameStateCache = new GameStateCache();
        
        _camera = new Camera2D(_graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
        _mapRenderer = new MapRenderer(GraphicsDevice);
        _regionRenderer = new RegionRenderer(GraphicsDevice);
        _uiRenderer = new UIRenderer(GraphicsDevice, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
        _selectionRenderer = new SelectionRenderer(GraphicsDevice);
        
        _mapData = MapLoader.CreateSampleMap();
        
        _inputController = new InputController(_gameClient, _gameStateCache, _mapData, _camera);
        
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        
        try
        {
            _defaultFont = Content.Load<SpriteFont>("DefaultFont");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Warning: Could not load font: {ex.Message}");
        }
        
        if (_defaultFont != null)
        {
            _mapRenderer?.LoadContent(_defaultFont);
            _regionRenderer?.LoadContent(_defaultFont);
            _uiRenderer?.LoadContent(_defaultFont);
            _selectionRenderer?.LoadContent(_defaultFont);
        }
    }

    protected override void Update(GameTime gameTime)
    {
        var keyState = Keyboard.GetState();
        
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            Exit();

        if (keyState.IsKeyDown(Keys.F1) && _previousKeyState.IsKeyUp(Keys.F1))
            _showDebug = !_showDebug;

        _camera?.Update(gameTime);
        _inputController?.Update(gameTime);
        _selectionRenderer?.Update(gameTime);
        
        _previousKeyState = keyState;

        if (_gameClient != null && _gameClient.IsConnected)
        {
            var updates = _gameClient.DequeueAllUpdates();
            foreach (var update in updates)
            {
                ProcessGameUpdate(update);
            }
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(10, 10, 20));

        if (_spriteBatch == null || _mapData == null || _gameStateCache == null)
            return;

        if (_camera != null)
        {
            _mapRenderer?.Draw(_spriteBatch, _mapData, _camera);
            _regionRenderer?.Draw(_spriteBatch, _mapData, _gameStateCache, _camera);
            
            if (_inputController != null)
            {
                _selectionRenderer?.Draw(_spriteBatch, _mapData, _gameStateCache, _inputController, _camera);
            }
        }

        _uiRenderer?.Draw(_spriteBatch, _gameStateCache, _currentPlayerId);
        
        if (_inputController != null)
        {
            _uiRenderer?.DrawSelectionInfo(_spriteBatch, _inputController.Selection, _gameStateCache);
            _uiRenderer?.DrawKeyboardShortcuts(_spriteBatch, _inputController.ShowHelp);
        }

        if (_showDebug && _camera != null)
        {
            _uiRenderer?.DrawDebugInfo(_spriteBatch, _camera);
        }

        base.Draw(gameTime);
    }

    private void ProcessGameUpdate(GameUpdate update)
    {
        if (_gameStateCache == null)
        {
            return;
        }

        _gameStateCache.ApplyUpdate(update);
        
        if (update.UpdateCase == GameUpdate.UpdateOneofCase.ConnectionStatus)
        {
            var connStatus = update.ConnectionStatus;
            if (connStatus.Status == GameConnectionStatus.Types.ConnectionState.Connected)
            {
                _currentPlayerId = connStatus.PlayerId;
                _inputController?.SetCurrentPlayer(_currentPlayerId);
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _gameClient?.Dispose();
        }
        base.Dispose(disposing);
    }
}
