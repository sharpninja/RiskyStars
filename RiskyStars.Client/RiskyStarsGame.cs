using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RiskyStars.Shared;
using Myra;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;

namespace RiskyStars.Client;

public enum GameState
{
    MainMenu,
    Lobby,
    InGame,
    Transition
}

public class RiskyStarsGame : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch? _spriteBatch;
    private Settings _settings;
    private WindowPreferences _windowPreferences;
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
    private ContextMenuManager? _contextMenuManager;
    private Desktop? _inGameDesktop;
    private DialogManager? _inGameDialogManager;
    private CombatEventDialog? _combatEventDialog;
    
    private PlayerDashboardWindow? _playerDashboardWindow;
    private AIVisualizationWindow? _aiVisualizationWindow;
    private DebugInfoWindow? _debugInfoWindow;
    private SettingsWindow? _settingsWindow;
    private ServerStatusIndicator? _serverStatusIndicator;
    
    private MapData? _mapData;
    private SpriteFont? _defaultFont;
    
    private string? _currentPlayerId;
    private KeyboardState _previousKeyState;
    
    private GameState _gameState = GameState.MainMenu;
    private bool _pendingResolutionChange = false;
    private bool _handlingClientResize = false;
    private string _transitionMessage = "";
    private DateTime _transitionStartTime;

    public RiskyStarsGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        _settings = Settings.Load();
        _windowPreferences = WindowPreferences.Load();
        ApplySettings();
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
        Window.ClientSizeChanged += OnClientSizeChanged;
        
        // Initialize Myra
        MyraEnvironment.Game = this;
        
        // Initialize Theme Manager
        ThemeManager.Initialize();
        
        // Configure logging level - change to LogLevel.Verbose for maximum debugging
        MethodLogger.SetLevel(MethodLogger.LogLevel.Verbose);
        MethodLogger.LogInfo("Application started with detailed logging enabled");
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

    private void OnClientSizeChanged(object? sender, EventArgs e)
    {
        if (_handlingClientResize || _graphics.IsFullScreen)
        {
            return;
        }

        int width = Window.ClientBounds.Width;
        int height = Window.ClientBounds.Height;
        if (width <= 0 || height <= 0)
        {
            return;
        }

        if (width == _graphics.PreferredBackBufferWidth && height == _graphics.PreferredBackBufferHeight)
        {
            ResizeUiForViewport(width, height);
            return;
        }

        _handlingClientResize = true;
        try
        {
            _graphics.PreferredBackBufferWidth = width;
            _graphics.PreferredBackBufferHeight = height;
            _graphics.ApplyChanges();

            _settings.ResolutionWidth = width;
            _settings.ResolutionHeight = height;

            ResizeUiForViewport(width, height);
        }
        finally
        {
            _handlingClientResize = false;
        }
    }

    protected override void Initialize()
    {
        _gameStateCache = new GameStateCache();
        
        _camera = new Camera2D(_graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
        _camera.PanSpeed = _settings.CameraPanSpeed;
        _camera.ZoomSpeed = _settings.CameraZoomSpeed;
        
        _mapRenderer = new MapRenderer(GraphicsDevice);
        _regionRenderer = new RegionRenderer(GraphicsDevice);
        _uiRenderer = new UIRenderer(GraphicsDevice, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
        _selectionRenderer = new SelectionRenderer(GraphicsDevice);
        _combatScreen = new CombatScreen(GraphicsDevice, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
        _lobbyManager = new LobbyManager(GraphicsDevice, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
        _mainMenu = new MainMenu(GraphicsDevice, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight, _settings);
        _aiActionIndicator = new AIActionIndicator(GraphicsDevice, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
        
        _inGameDesktop = new Desktop();
        _inGameDialogManager = new DialogManager(_inGameDesktop);
        _combatEventDialog = new CombatEventDialog(_inGameDesktop);
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
        ThemeManager.LoadContent(Content);
        ThemeManager.ApplyThemeSettings(_settings);
        
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
            _settingsWindow = new SettingsWindow(_graphics, _settings, OnSettingsApplied);
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
        {
            Exit();
        }

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
                
            case GameState.Transition:
                // Do nothing while transitioning, just show loading message
                break;
        }
        
        _previousKeyState = keyState;

        base.Update(gameTime);
    }

    private void UpdateMainMenu(GameTime gameTime)
    {
        if (_mainMenu == null)
        {
            return;
        }

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
                ResizeUiForViewport(_graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
                _pendingResolutionChange = false;
            }

            _lobbyManager = new LobbyManager(GraphicsDevice, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
            if (_defaultFont != null)
            {
                _lobbyManager.LoadContent(_defaultFont);
            }
            _lobbyManager.SetMultiplayerMode();
            _gameState = GameState.Lobby;
            _mainMenu.SetState(MainMenuState.Main);
        }

        if (_mainMenu.ShouldStartSinglePlayer)
        {
            if (_pendingResolutionChange)
            {
                ApplySettings();
                ResizeUiForViewport(_graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
                _pendingResolutionChange = false;
            }

            // Go directly to lobby manager in single player mode
            _lobbyManager = new LobbyManager(GraphicsDevice, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
            if (_defaultFont != null)
            {
                _lobbyManager.LoadContent(_defaultFont);
            }
            _lobbyManager.SetSinglePlayerMode();
            _gameState = GameState.Lobby;
            _mainMenu.SetState(MainMenuState.Main);
        }

        if (_mainMenu.Settings.ResolutionWidth != _graphics.PreferredBackBufferWidth ||
            _mainMenu.Settings.ResolutionHeight != _graphics.PreferredBackBufferHeight ||
            _mainMenu.Settings.Fullscreen != _graphics.IsFullScreen)
        {
            _pendingResolutionChange = true;
        }

        if (_pendingResolutionChange && _mainMenu.State == MainMenuState.Main)
        {
            ApplySettings();
            ResizeUiForViewport(_graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
            _pendingResolutionChange = false;
        }
    }

    private void UpdateLobby(GameTime gameTime)
    {
        if (_lobbyManager == null)
        {
            return;
        }

        _lobbyManager.Update(gameTime);
        
        if (_lobbyManager.IsInGame && _lobbyManager.SessionId != null && _lobbyManager.PlayerId != null)
        {
            StartTransition("Loading game world");
            
            if (_lobbyManager.SelectedGameMode == GameMode.SinglePlayer)
            {
                InitializeSinglePlayerGame(_lobbyManager.SessionId, _lobbyManager.PlayerName ?? "Player", _lobbyManager.PlayerId);
            }
            else
            {
                InitializeGame(_lobbyManager.SessionId, _lobbyManager.PlayerName ?? "Player", _lobbyManager.PlayerId);
            }
            _gameState = GameState.InGame;
        }
    }

    private void UpdateInGame(GameTime gameTime, KeyboardState keyState)
    {
        if (keyState.IsKeyDown(Keys.Escape) && _previousKeyState.IsKeyUp(Keys.Escape))
        {
            if (_contextMenuManager != null && _contextMenuManager.IsMenuOpen)
            {
                _contextMenuManager.CloseContextMenu();
            }
            else
            {
                ReturnToMainMenu();
            }
            return;
        }

        if (keyState.IsKeyDown(Keys.F1) && _previousKeyState.IsKeyUp(Keys.F1))
        {
            _debugInfoWindow?.Toggle();
        }
        
        if (keyState.IsKeyDown(Keys.F2) && _previousKeyState.IsKeyUp(Keys.F2))
        {
            _playerDashboardWindow?.Toggle();
        }
        
        if (keyState.IsKeyDown(Keys.F3) && _previousKeyState.IsKeyUp(Keys.F3))
        {
            _aiVisualizationWindow?.Toggle();
        }

        _connectionManager?.Update();
        _inGameDialogManager?.Update();
        _serverStatusIndicator?.Update();

        if (_connectionManager?.Status == ConnectionStatus.Error && 
            _connectionManager.ReconnectAttempts >= _connectionManager.MaxAttempts)
        {
            var errorMessage = $"Connection lost: {_connectionManager.ErrorMessage}";
            ReturnToMainMenu();
            _mainMenu?.ShowError(errorMessage);
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
        else if (!(_combatEventDialog?.IsOpen ?? false) && !(_settingsWindow?.IsOpen ?? false))
        {
            _camera?.Update(gameTime);
            _inputController?.Update(gameTime);
            _selectionRenderer?.Update(gameTime);
            
            if (_gameStateCache != null && _mapData != null)
            {
                _playerDashboard?.Update(gameTime, _gameStateCache);
                _playerDashboardWindow?.UpdateContent(_gameStateCache);
                
                _aiActionIndicator?.Update(gameTime, _gameStateCache, _mapData, _currentPlayerId);
                
                if (_aiActionIndicator != null && _camera != null)
                {
                    var activeAnimation = _aiActionIndicator.GetFirstMovementAnimation();
                    if (activeAnimation != null)
                    {
                        if (_aiVisualizationWindow?.AutoFollowAIActions ?? false)
                        {
                            _aiActionIndicator.TrackArmyMovement(_camera, activeAnimation);
                        }
                    }
                }
                
                UpdateDebugInfo(gameTime);
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
    
    private void OnSettingsApplied(Settings settings)
    {
        bool uiScaleChanged = settings.UiScalePercent != ThemeManager.CurrentUiScalePercent;
        ThemeManager.ApplyThemeSettings(settings);

        bool resolutionChanged = settings.ResolutionWidth != _graphics.PreferredBackBufferWidth ||
                                settings.ResolutionHeight != _graphics.PreferredBackBufferHeight ||
                                settings.Fullscreen != _graphics.IsFullScreen;
        
        if (resolutionChanged)
        {
            ApplySettings();
            
            if (_camera != null)
            {
                _camera = new Camera2D(_graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
                _camera.PanSpeed = settings.CameraPanSpeed;
                _camera.ZoomSpeed = settings.CameraZoomSpeed;
            }
        }
        
        if (_graphics.SynchronizeWithVerticalRetrace != settings.VSync)
        {
            _graphics.SynchronizeWithVerticalRetrace = settings.VSync;
            _graphics.ApplyChanges();
        }
        
        if (settings.TargetFrameRate > 0)
        {
            TargetElapsedTime = TimeSpan.FromSeconds(1.0 / settings.TargetFrameRate);
            IsFixedTimeStep = true;
        }
        else
        {
            IsFixedTimeStep = false;
        }
        
        if (_camera != null)
        {
            _camera.PanSpeed = settings.CameraPanSpeed;
            _camera.ZoomSpeed = settings.CameraZoomSpeed;
        }

        if (uiScaleChanged || resolutionChanged)
        {
            ResizeUiForViewport(_graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
            RecreateSettingsWindow();
        }
    }

    private void RecreateSettingsWindow()
    {
        _settingsWindow = new SettingsWindow(_graphics, _settings, OnSettingsApplied);
    }

    private void RebuildMainMenuForCurrentDisplay()
    {
        _mainMenu?.ResizeViewport(_graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
    }

    private void ResizeUiForViewport(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        _camera?.ResizeViewport(width, height);
        _uiRenderer?.ResizeViewport(width, height);
        _combatScreen?.ResizeViewport(width, height);
        _aiActionIndicator?.ResizeViewport(width, height);
        _playerDashboard?.ResizeViewport(width, height);

        _mainMenu?.ResizeViewport(width, height);
        _lobbyManager?.ResizeViewport(width, height);
        _settingsWindow?.ResizeViewport();

        _playerDashboardWindow?.ResizeViewport(width, height);
        _aiVisualizationWindow?.ResizeViewport(width, height);
        _debugInfoWindow?.ResizeViewport(width, height);

        if (_serverStatusIndicator != null)
        {
            _serverStatusIndicator.Resize(Math.Min(500, Math.Max(280, width - 80)));
            _serverStatusIndicator.Container.HorizontalAlignment = HorizontalAlignment.Center;
            _serverStatusIndicator.Container.VerticalAlignment = VerticalAlignment.Bottom;
            _serverStatusIndicator.Container.Top = height - 35;
        }
    }

    private void InitializeGame(string sessionId, string playerName, string playerId)
    {
        if (_connectionManager == null)
        {
            return;
        }

        _playerDashboard = new PlayerDashboard(GraphicsDevice, _connectionManager.GameClient, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
        _inputController = new InputController(_connectionManager.GameClient, _gameStateCache, _mapData, _camera);
        
        _playerDashboardWindow = new PlayerDashboardWindow(_connectionManager.GameClient, _windowPreferences, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
        _aiVisualizationWindow = new AIVisualizationWindow(_windowPreferences, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
        _debugInfoWindow = new DebugInfoWindow(_windowPreferences, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
        
        if (_inGameDesktop != null && _mapData != null && _gameStateCache != null && _camera != null)
        {
            _contextMenuManager = new ContextMenuManager(_connectionManager.GameClient, _gameStateCache, _mapData, _camera, _inGameDesktop);
            _inputController.SetContextMenuManager(_contextMenuManager);
        }
        
        if (_aiActionTracker != null)
        {
            _aiActionTracker.SetAIVisualizationWindow(_aiVisualizationWindow);
        }
        
        if (_inGameDesktop != null)
        {
            _inGameDesktop.Root = null;
            _inGameDesktop.Widgets.Add(_playerDashboardWindow.Window);
            _inGameDesktop.Widgets.Add(_aiVisualizationWindow.Window);
            _inGameDesktop.Widgets.Add(_debugInfoWindow.Window);
        }
        
        if (_defaultFont != null)
        {
            _playerDashboard?.LoadContent(_defaultFont);
        }
        
        Task.Run(async () =>
        {
            try
            {
                var success = await _connectionManager.ConnectAsync(playerId, playerName, sessionId);
                if (success && _connectionManager.CurrentPlayerId != null)
                {
                    _currentPlayerId = _connectionManager.CurrentPlayerId;
                    _inputController?.SetCurrentPlayer(_currentPlayerId);
                    _playerDashboard?.SetCurrentPlayer(_currentPlayerId);
                    _playerDashboardWindow?.SetCurrentPlayer(_currentPlayerId);
                    _contextMenuManager?.SetCurrentPlayer(_currentPlayerId);
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

    private void InitializeSinglePlayerGame(string sessionId, string playerName, string playerId)
    {
        if (_lobbyManager?.EmbeddedServer == null)
        {
            return;
        }

        var gameClient = GrpcGameClient.CreateForSinglePlayer(_lobbyManager.EmbeddedServer);
        _connectionManager = new ConnectionManager(gameClient);

        _playerDashboard = new PlayerDashboard(GraphicsDevice, gameClient, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
        _inputController = new InputController(gameClient, _gameStateCache, _mapData, _camera);
        
        _playerDashboardWindow = new PlayerDashboardWindow(gameClient, _windowPreferences, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
        _aiVisualizationWindow = new AIVisualizationWindow(_windowPreferences, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
        _debugInfoWindow = new DebugInfoWindow(_windowPreferences, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
        
        _serverStatusIndicator = new ServerStatusIndicator(500);
        _serverStatusIndicator.SetServerHost(_lobbyManager.EmbeddedServer);
        
        if (_inGameDesktop != null && _mapData != null && _gameStateCache != null && _camera != null)
        {
            _contextMenuManager = new ContextMenuManager(gameClient, _gameStateCache, _mapData, _camera, _inGameDesktop);
            _inputController.SetContextMenuManager(_contextMenuManager);
        }
        
        if (_aiActionTracker != null)
        {
            _aiActionTracker.SetAIVisualizationWindow(_aiVisualizationWindow);
        }
        
        if (_inGameDesktop != null)
        {
            _inGameDesktop.Root = null;
            _inGameDesktop.Widgets.Add(_playerDashboardWindow.Window);
            _inGameDesktop.Widgets.Add(_aiVisualizationWindow.Window);
            _inGameDesktop.Widgets.Add(_debugInfoWindow.Window);
            
            if (_serverStatusIndicator != null)
            {
                _serverStatusIndicator.Container.HorizontalAlignment = HorizontalAlignment.Center;
                _serverStatusIndicator.Container.VerticalAlignment = VerticalAlignment.Bottom;
                _serverStatusIndicator.Container.Top = _graphics.PreferredBackBufferHeight - 35;
                _inGameDesktop.Widgets.Add(_serverStatusIndicator.Container);
            }
        }
        
        if (_defaultFont != null)
        {
            _playerDashboard?.LoadContent(_defaultFont);
        }
        
        Task.Run(async () =>
        {
            try
            {
                var success = await _connectionManager.ConnectAsync(playerId, playerName, sessionId);
                if (success && _connectionManager.CurrentPlayerId != null)
                {
                    _currentPlayerId = _connectionManager.CurrentPlayerId;
                    _inputController?.SetCurrentPlayer(_currentPlayerId);
                    _playerDashboard?.SetCurrentPlayer(_currentPlayerId);
                    _playerDashboardWindow?.SetCurrentPlayer(_currentPlayerId);
                    _contextMenuManager?.SetCurrentPlayer(_currentPlayerId);
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
        StartTransition("Returning to main menu");
        
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
        _playerDashboardWindow = null;
        _aiVisualizationWindow = null;
        _debugInfoWindow = null;
        _serverStatusIndicator = null;
        _contextMenuManager = null;
        _gameStateCache = new GameStateCache();
        
        if (_inGameDesktop != null)
        {
            _inGameDesktop.Root = null;
            _inGameDesktop.Widgets.Clear();
        }
        
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
        {
            return;
        }

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
                
            case GameState.Transition:
                DrawTransitionScreen(_spriteBatch);
                break;
        }

        base.Draw(gameTime);
    }

    private void DrawLobby(SpriteBatch spriteBatch)
    {
        _lobbyManager?.Draw(spriteBatch);
    }

    private void DrawTransitionScreen(SpriteBatch spriteBatch)
    {
        if (_defaultFont == null)
            return;
            
        spriteBatch.Begin();
        
        // Draw loading dots animation
        var elapsed = DateTime.Now - _transitionStartTime;
        var dotCount = (int)(elapsed.TotalSeconds * 2) % 4;
        var dots = new string('.', dotCount);
        var fullMessage = _transitionMessage + dots;
        
        var msgSize = _defaultFont.MeasureString(fullMessage);
        var msgPos = new Vector2(
            (_graphics.PreferredBackBufferWidth - msgSize.X) / 2,
            (_graphics.PreferredBackBufferHeight - msgSize.Y) / 2);
            
        spriteBatch.DrawString(_defaultFont, fullMessage, msgPos, Color.LightGray);
        spriteBatch.End();
    }

    private void StartTransition(string message)
    {
        _transitionMessage = message;
        _transitionStartTime = DateTime.Now;
        _gameState = GameState.Transition;
    }

    private void DrawInGame(SpriteBatch spriteBatch)
    {
        if (_mapData == null || _gameStateCache == null)
        {
            return;
        }

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
        }

        _inGameDesktop?.Render();
        _settingsWindow?.Render();
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
                    if (_combatScreen != null && !_combatScreen.IsActive && !(_combatEventDialog?.IsOpen ?? false))
                    {
                        ShowCombatEventNotification(combatEvent);
                        break;
                    }
                }
            }
        }
    }

    private void UpdateDebugInfo(GameTime gameTime)
    {
        if (_debugInfoWindow == null)
        {
            return;
        }

        _debugInfoWindow.Update(gameTime);
        
        if (_camera != null)
        {
            _debugInfoWindow.UpdateCameraInfo(_camera);
        }
        
        if (_gameStateCache != null)
        {
            _debugInfoWindow.UpdateGameStateInfo(_gameStateCache, _connectionManager);
        }
        
        if (_inputController != null)
        {
            _debugInfoWindow.UpdateSelectionInfo(_inputController.Selection);
        }
        
        if (_aiVisualizationWindow != null && _gameStateCache != null)
        {
            var currentPlayerId = _gameStateCache.GetCurrentPlayerId();
            if (currentPlayerId != _currentPlayerId && !string.IsNullOrEmpty(currentPlayerId))
            {
                var playerState = _gameStateCache.GetPlayerState(currentPlayerId);
                if (playerState != null)
                {
                    _aiVisualizationWindow.UpdateAIStatus(playerState.PlayerName, true);
                }
            }
            else
            {
                _aiVisualizationWindow.UpdateAIStatus("", false);
            }
        }
    }
    
    private void ShowCombatEventNotification(CombatEvent combatEvent)
    {
        if (_combatEventDialog == null)
        {
            return;
        }

        switch (combatEvent.EventType)
        {
            case CombatEvent.Types.CombatEventType.CombatInitiated:
                _combatEventDialog.ShowCombatInitiated(combatEvent, () =>
                {
                    _combatScreen?.StartCombat(combatEvent);
                });
                break;

            case CombatEvent.Types.CombatEventType.ReinforcementsArrived:
                _combatEventDialog.ShowReinforcementsArrived(combatEvent, () =>
                {
                    _combatScreen?.StartCombat(combatEvent);
                });
                break;

            case CombatEvent.Types.CombatEventType.CombatEnded:
                _combatEventDialog.ShowCombatEnded(combatEvent);
                break;

            case CombatEvent.Types.CombatEventType.CombatRoundComplete:
                _combatScreen?.StartCombat(combatEvent);
                break;
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




