using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RiskyStars.Shared;
using Myra;

namespace RiskyStars.Client;

public enum GameState
{
    MainMenu,
    Lobby,
    InGame
}

public class RiskyStarsGame : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch? _spriteBatch;
    private Settings _settings;
    private ConnectionManager? _connectionManager;
    private GameStateCache? _gameStateCache;
    
    private Camera2D? _camera;
    private MapRenderer? _mapRenderer;
    private RegionRenderer? _regionRenderer;
    private UIRenderer? _uiRenderer;
    private SelectionRenderer? _selectionRenderer;
    private InputController? _inputController;
    private CombatScreen? _combatScreen;
    private PlayerDashboard? _playerDashboard;
    private LobbyManager? _lobbyManager;
    private MainMenu? _mainMenu;
    private AIActionIndicator? _aiActionIndicator;
    private AIActionTracker? _aiActionTracker;
    
    private MapData? _mapData;
    private SpriteFont? _defaultFont;
    
    private string? _currentPlayerId;
    private bool _showDebug = true;
    private KeyboardState _previousKeyState;
    
    private GameState _gameState = GameState.MainMenu;
    private bool _pendingResolutionChange = false;

    public RiskyStarsGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        _settings = Settings.Load();
        ApplySettings();
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        
        // Initialize Myra
        MyraEnvironment.Game = this;
        
        // Initialize Theme Manager
        ThemeManager.Initialize();
    }

    private void ApplySettings()
    {
        _graphics.PreferredBackBufferWidth = _settings.ResolutionWidth;
        _graphics.PreferredBackBufferHeight = _settings.ResolutionHeight;
        _graphics.IsFullScreen = _settings.Fullscreen;
        
        if (_graphics.GraphicsDevice != null)
        {
            _graphics.ApplyChanges();
        }
    }

    protected override void Initialize()
    {
        _gameStateCache = new GameStateCache();
        
        _camera = new Camera2D(_graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
        _mapRenderer = new MapRenderer(GraphicsDevice);
        _regionRenderer = new RegionRenderer(GraphicsDevice);
        _uiRenderer = new UIRenderer(GraphicsDevice, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
        _selectionRenderer = new SelectionRenderer(GraphicsDevice);
        _combatScreen = new CombatScreen(GraphicsDevice, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
        _lobbyManager = new LobbyManager(GraphicsDevice, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
        _mainMenu = new MainMenu(GraphicsDevice, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight, _settings);
        _aiActionIndicator = new AIActionIndicator(GraphicsDevice, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
        
        _mapData = MapLoader.CreateSampleMap();
        
        if (_aiActionIndicator != null && _mapData != null && _gameStateCache != null && _regionRenderer != null)
        {
            _aiActionTracker = new AIActionTracker(_aiActionIndicator, _mapData, _gameStateCache, _regionRenderer);
        }
        
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
            _combatScreen?.LoadContent(_defaultFont);
            _playerDashboard?.LoadContent(_defaultFont);
            _lobbyManager?.LoadContent(_defaultFont);
            _mainMenu?.LoadContent(_defaultFont);
            _aiActionIndicator?.LoadContent(_defaultFont);
        }
    }

    protected override void Update(GameTime gameTime)
    {
        var keyState = Keyboard.GetState();
        
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            Exit();

        switch (_gameState)
        {
            case GameState.MainMenu:
                UpdateMainMenu(gameTime);
                break;

            case GameState.Lobby:
                UpdateLobby(gameTime);
                break;

            case GameState.InGame:
                UpdateInGame(gameTime, keyState);
                break;
        }
        
        _previousKeyState = keyState;

        base.Update(gameTime);
    }

    private void UpdateMainMenu(GameTime gameTime)
    {
        if (_mainMenu == null) return;

        _mainMenu.Update(gameTime);

        if (_mainMenu.ShouldExit)
        {
            Exit();
            return;
        }

        if (_mainMenu.ShouldConnect)
        {
            if (_pendingResolutionChange)
            {
                ApplySettings();
                _pendingResolutionChange = false;
            }

            _connectionManager = new ConnectionManager(_mainMenu.Settings.ServerAddress);
            _gameState = GameState.Lobby;
            _mainMenu.SetState(MainMenuState.Main);
        }

        if (_mainMenu.State == MainMenuState.Settings && 
            (_mainMenu.Settings.ResolutionWidth != _graphics.PreferredBackBufferWidth ||
             _mainMenu.Settings.ResolutionHeight != _graphics.PreferredBackBufferHeight ||
             _mainMenu.Settings.Fullscreen != _graphics.IsFullScreen))
        {
            _pendingResolutionChange = true;
        }
    }

    private void UpdateLobby(GameTime gameTime)
    {
        if (_lobbyManager == null) return;

        _lobbyManager.Update(gameTime);
        
        if (_lobbyManager.IsInGame && _lobbyManager.SessionId != null)
        {
            if (_lobbyManager.SelectedGameMode == GameMode.SinglePlayer)
            {
                InitializeSinglePlayerGame(_lobbyManager.SessionId, _lobbyManager.PlayerName ?? "Player");
            }
            else
            {
                InitializeGame(_lobbyManager.SessionId, _lobbyManager.PlayerName ?? "Player");
            }
            _gameState = GameState.InGame;
        }
    }

    private void UpdateInGame(GameTime gameTime, KeyboardState keyState)
    {
        if (keyState.IsKeyDown(Keys.Escape) && _previousKeyState.IsKeyUp(Keys.Escape))
        {
            ReturnToMainMenu();
            return;
        }

        if (keyState.IsKeyDown(Keys.F1) && _previousKeyState.IsKeyUp(Keys.F1))
            _showDebug = !_showDebug;
        
        if (keyState.IsKeyDown(Keys.F2) && _previousKeyState.IsKeyUp(Keys.F2))
        {
            if (_playerDashboard != null)
                _playerDashboard.IsVisible = !_playerDashboard.IsVisible;
        }

        _connectionManager?.Update();

        if (_connectionManager?.Status == ConnectionStatus.Error && 
            _connectionManager.ReconnectAttempts >= _connectionManager.MaxAttempts)
        {
            ReturnToMainMenu();
            _mainMenu?.ShowError($"Connection lost: {_connectionManager.ErrorMessage}");
            return;
        }

        if (_combatScreen != null && _combatScreen.IsActive)
        {
            _combatScreen.Update(gameTime);
            
            if (_combatScreen.IsComplete)
            {
                _combatScreen.Close();
            }
        }
        else
        {
            _camera?.Update(gameTime);
            _inputController?.Update(gameTime);
            _selectionRenderer?.Update(gameTime);
            
            if (_gameStateCache != null && _mapData != null)
            {
                _playerDashboard?.Update(gameTime, _gameStateCache);
                _aiActionIndicator?.Update(gameTime, _gameStateCache, _mapData, _currentPlayerId);
                
                if (_aiActionIndicator != null && _camera != null)
                {
                    var activeAnimation = _aiActionIndicator.GetFirstMovementAnimation();
                    if (activeAnimation != null)
                    {
                        _aiActionIndicator.TrackArmyMovement(_camera, activeAnimation);
                    }
                }
            }
        }
        
        if (_connectionManager?.IsConnected == true && _connectionManager.GameClient != null)
        {
            var updates = _connectionManager.GameClient.DequeueAllUpdates();
            foreach (var update in updates)
            {
                ProcessGameUpdate(update);
                _aiActionTracker?.ProcessGameUpdate(update, _currentPlayerId);
            }
        }
    }

    private void InitializeGame(string sessionId, string playerName)
    {
        if (_connectionManager == null) return;

        _playerDashboard = new PlayerDashboard(GraphicsDevice, _connectionManager.GameClient, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
        _inputController = new InputController(_connectionManager.GameClient, _gameStateCache, _mapData, _camera);
        
        if (_defaultFont != null)
        {
            _playerDashboard?.LoadContent(_defaultFont);
        }
        
        Task.Run(async () =>
        {
            try
            {
                var success = await _connectionManager.ConnectAsync(playerName, sessionId);
                if (success && _connectionManager.CurrentPlayerId != null)
                {
                    _currentPlayerId = _connectionManager.CurrentPlayerId;
                    _inputController?.SetCurrentPlayer(_currentPlayerId);
                    _playerDashboard?.SetCurrentPlayer(_currentPlayerId);
                }
                else
                {
                    System.Console.WriteLine($"Failed to connect: {_connectionManager.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Failed to connect to game: {ex.Message}");
            }
        });
    }

    private void InitializeSinglePlayerGame(string sessionId, string playerName)
    {
        if (_lobbyManager?.EmbeddedServer == null) return;

        var gameClient = GrpcGameClient.CreateForSinglePlayer(_lobbyManager.EmbeddedServer);
        _connectionManager = new ConnectionManager(gameClient);

        _playerDashboard = new PlayerDashboard(GraphicsDevice, gameClient, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
        _inputController = new InputController(gameClient, _gameStateCache, _mapData, _camera);
        
        if (_defaultFont != null)
        {
            _playerDashboard?.LoadContent(_defaultFont);
        }
        
        Task.Run(async () =>
        {
            try
            {
                var success = await _connectionManager.ConnectAsync(playerName, sessionId);
                if (success && _connectionManager.CurrentPlayerId != null)
                {
                    _currentPlayerId = _connectionManager.CurrentPlayerId;
                    _inputController?.SetCurrentPlayer(_currentPlayerId);
                    _playerDashboard?.SetCurrentPlayer(_currentPlayerId);
                }
                else
                {
                    System.Console.WriteLine($"Failed to connect to embedded server: {_connectionManager.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Failed to initialize single player game: {ex.Message}");
            }
        });
    }

    private void ReturnToMainMenu()
    {
        Task.Run(async () =>
        {
            try
            {
                if (_connectionManager != null)
                {
                    await _connectionManager.DisconnectAsync();
                    _connectionManager.Dispose();
                    _connectionManager = null;
                }

                if (_lobbyManager?.EmbeddedServer != null)
                {
                    try
                    {
                        await _lobbyManager.EmbeddedServer.DisposeAsync();
                    }
                    catch (Exception ex)
                    {
                        System.Console.WriteLine($"Error disposing embedded server: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error during cleanup: {ex.Message}");
            }
        });

        _currentPlayerId = null;
        _inputController = null;
        _playerDashboard = null;
        _gameStateCache = new GameStateCache();
        
        if (_aiActionIndicator != null && _mapData != null && _gameStateCache != null && _regionRenderer != null)
        {
            _aiActionTracker = new AIActionTracker(_aiActionIndicator, _mapData, _gameStateCache, _regionRenderer);
        }
        
        _gameState = GameState.MainMenu;
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(10, 10, 20));

        if (_spriteBatch == null)
            return;

        switch (_gameState)
        {
            case GameState.MainMenu:
                _mainMenu?.Draw(_spriteBatch);
                break;

            case GameState.Lobby:
                DrawLobby(_spriteBatch);
                break;

            case GameState.InGame:
                DrawInGame(_spriteBatch);
                break;
        }

        base.Draw(gameTime);
    }

    private void DrawLobby(SpriteBatch spriteBatch)
    {
        _lobbyManager?.Draw(spriteBatch);
    }

    private void DrawInGame(SpriteBatch spriteBatch)
    {
        if (_mapData == null || _gameStateCache == null)
            return;

        if (_connectionManager?.Status == ConnectionStatus.Reconnecting && _defaultFont != null)
        {
            spriteBatch.Begin();
            var reconnectMsg = $"Reconnecting... (Attempt {_connectionManager.ReconnectAttempts}/{_connectionManager.MaxAttempts})";
            var msgSize = _defaultFont.MeasureString(reconnectMsg);
            var msgPos = new Vector2(
                (_graphics.PreferredBackBufferWidth - msgSize.X) / 2,
                20);
            spriteBatch.DrawString(_defaultFont, reconnectMsg, msgPos, Color.Yellow);
            spriteBatch.End();
        }

        if (_combatScreen != null && _combatScreen.IsActive)
        {
            _combatScreen.Draw(spriteBatch);
        }
        else
        {
            if (_camera != null)
            {
                _mapRenderer?.Draw(spriteBatch, _mapData, _camera);
                _regionRenderer?.Draw(spriteBatch, _mapData, _gameStateCache, _camera);
                
                if (_inputController != null)
                {
                    _selectionRenderer?.Draw(spriteBatch, _mapData, _gameStateCache, _inputController, _camera);
                }
                
                _aiActionIndicator?.Draw(spriteBatch, _camera, _mapData);
            }

            _uiRenderer?.Draw(spriteBatch, _gameStateCache, _currentPlayerId);
            _playerDashboard?.Draw(spriteBatch, _gameStateCache);
            
            if (_inputController != null)
            {
                _uiRenderer?.DrawSelectionInfo(spriteBatch, _inputController.Selection, _gameStateCache);
                _uiRenderer?.DrawKeyboardShortcuts(spriteBatch, _inputController.ShowHelp);
            }

            if (_showDebug && _camera != null)
            {
                _uiRenderer?.DrawDebugInfo(spriteBatch, _camera);
            }
        }
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
                _playerDashboard?.SetCurrentPlayer(_currentPlayerId);
            }
        }
        
        if (update.UpdateCase == GameUpdate.UpdateOneofCase.GameState)
        {
            var gameState = update.GameState;
            if (gameState != null && gameState.CombatEvents.Count > 0)
            {
                foreach (var combatEvent in gameState.CombatEvents)
                {
                    if (_combatScreen != null && !_combatScreen.IsActive)
                    {
                        _combatScreen.StartCombat(combatEvent);
                        break;
                    }
                }
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                if (_connectionManager != null)
                {
                    Task.Run(async () => await _connectionManager.DisconnectAsync()).Wait(TimeSpan.FromSeconds(5));
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error disconnecting: {ex.Message}");
            }

            try
            {
                _connectionManager?.Dispose();
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error disposing connection manager: {ex.Message}");
            }
            
            if (_lobbyManager != null)
            {
                try
                {
                    Task.Run(async () => await _lobbyManager.DisposeAsync()).Wait(TimeSpan.FromSeconds(10));
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"Error disposing lobby manager: {ex.Message}");
                }
            }
        }
        base.Dispose(disposing);
    }
}
