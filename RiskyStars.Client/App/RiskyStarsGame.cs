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
    private Texture2D? _pixelTexture;
    private Settings _settings;
    private WindowPreferences _windowPreferences;
    private ConnectionManager? _connectionManager;
    private GameStateCache? _gameStateCache;
    
    private Camera2D? _camera;
    private MapRenderer? _mapRenderer;
    private RegionRenderer? _regionRenderer;
    private SelectionRenderer? _selectionRenderer;
    private InputController? _inputController;
    private CombatScreen? _combatScreen;
    private PlayerDashboard? _playerDashboard;
    private LobbyManager? _lobbyManager;
    private MainMenu? _mainMenu;
    private AIActionIndicator? _aiActionIndicator;
    private AIActionTracker? _aiActionTracker;
    private ContextMenuManager? _contextMenuManager;
    private GameplayHudOverlay? _gameplayHudOverlay;
    private CombatHudOverlay? _combatHudOverlay;
    private Desktop? _inGameDesktop;
    private DialogManager? _inGameDialogManager;
    private CombatEventDialog? _combatEventDialog;
    
    private PlayerDashboardWindow? _playerDashboardWindow;
    private AIVisualizationWindow? _aiVisualizationWindow;
    private DebugInfoWindow? _debugInfoWindow;
    private UiScaleWindow? _uiScaleWindow;
    private EncyclopediaWindow? _encyclopediaWindow;
    private TutorialWindow? _tutorialWindow;
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
    private bool _pendingGameEntry = false;
    private DateTime _pendingGameEntryStartedAt = DateTime.MinValue;
    private GameFeedbackMessage? _latestFeedback;
    private DateTime _latestFeedbackExpiresAt = DateTime.MinValue;

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
        _camera.InvertScrollZoom = _settings.InvertCameraZoom;
        
        _mapRenderer = new MapRenderer(GraphicsDevice);
        _regionRenderer = new RegionRenderer(GraphicsDevice);
        _selectionRenderer = new SelectionRenderer(GraphicsDevice);
        _combatScreen = new CombatScreen(GraphicsDevice, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
        _lobbyManager = new LobbyManager(GraphicsDevice, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
        _mainMenu = new MainMenu(GraphicsDevice, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight, _settings);
        _aiActionIndicator = new AIActionIndicator(GraphicsDevice, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
        
        _inGameDesktop = new Desktop
        {
            Background = ThemeManager.CreateSolidBrush(Color.Transparent)
        };
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
        _pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
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
            _settingsWindow = new SettingsWindow(_graphics, _settings, OnSettingsApplied, PreviewUiScale);
            _mapRenderer?.LoadContent(_defaultFont);
            _regionRenderer?.LoadContent(_defaultFont);
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
        UpdateFeedbackState();
        
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
                UpdateTransition(gameTime);
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
            if (!_pendingGameEntry)
            {
                StartTransition("Preparing game session");
                GameFeedbackBus.PublishBusy("Preparing game session", "Connecting to the live game stream and waiting for the first world snapshot.");
                _pendingGameEntry = true;
                _pendingGameEntryStartedAt = DateTime.UtcNow;

                if (_lobbyManager.SelectedGameMode == GameMode.SinglePlayer)
                {
                    InitializeSinglePlayerGame(_lobbyManager.SessionId, _lobbyManager.PlayerName ?? "Player", _lobbyManager.PlayerId);
                }
                else
                {
                    InitializeGame(_lobbyManager.SessionId, _lobbyManager.PlayerName ?? "Player", _lobbyManager.PlayerId);
                }
            }
        }
    }

    private void UpdateTransition(GameTime gameTime)
    {
        _connectionManager?.Update();
        _serverStatusIndicator?.Update();
        UpdateServerStatusVisibility();
        DrainPendingGameUpdates();

        if (_pendingGameEntry && HasReceivedInitialGameState())
        {
            _pendingGameEntry = false;
            _gameState = GameState.InGame;
            GameFeedbackBus.PublishSuccess("Game world synchronized", "The command deck is now live.");
            return;
        }

        if (_pendingGameEntry &&
            _connectionManager?.Status == ConnectionStatus.Error &&
            _connectionManager.ReconnectAttempts >= _connectionManager.MaxAttempts)
        {
            var errorMessage = $"Game startup failed: {_connectionManager.ErrorMessage}";
            _pendingGameEntry = false;
            ReturnToMainMenu();
            _mainMenu?.ShowError(errorMessage);
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

        if (keyState.IsKeyDown(Keys.F4) && _previousKeyState.IsKeyUp(Keys.F4))
        {
            _uiScaleWindow?.SyncFromSettings();
            _uiScaleWindow?.Toggle();
        }

        if (keyState.IsKeyDown(Keys.F5) && _previousKeyState.IsKeyUp(Keys.F5))
        {
            _encyclopediaWindow?.Toggle();
        }

        if (keyState.IsKeyDown(Keys.F6) && _previousKeyState.IsKeyUp(Keys.F6))
        {
            _tutorialWindow?.Toggle();
        }

        _connectionManager?.Update();
        _inGameDialogManager?.Update();
        _serverStatusIndicator?.Update();
        UpdateServerStatusVisibility();

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
        
        DrainPendingGameUpdates();
        UpdateTutorialWindow();
        UpdateCombatHud();
        UpdateGameplayHud();
    }

    private void DrainPendingGameUpdates()
    {
        if (_connectionManager?.IsConnected != true || _connectionManager.GameClient == null)
        {
            return;
        }

        foreach (var update in _connectionManager.GameClient.DequeueAllUpdates())
        {
            ProcessGameUpdate(update);
            _aiActionTracker?.ProcessGameUpdate(update, _currentPlayerId);
        }
    }

    private bool HasReceivedInitialGameState()
    {
        return _gameStateCache?.GetLastUpdateTimestamp() > 0;
    }

    private void UpdateServerStatusVisibility()
    {
        if (_serverStatusIndicator?.Container == null)
        {
            return;
        }

        bool shouldShow = !HasReceivedInitialGameState() ||
                          _connectionManager?.Status == ConnectionStatus.Connecting ||
                          _connectionManager?.Status == ConnectionStatus.Reconnecting ||
                          _connectionManager?.Status == ConnectionStatus.Error;

        _serverStatusIndicator.Container.Visible = shouldShow;
    }
    
    private void PreviewUiScale(int uiScalePercent)
    {
        uiScalePercent = Math.Clamp(uiScalePercent, 80, 160);
        if (_settings.UiScalePercent == uiScalePercent)
        {
            return;
        }

        _settings.UiScalePercent = uiScalePercent;
        _settings.Normalize();
        ApplyRuntimeSettings(_settings, preserveUiScaleWindow: true, recreateSettingsWindow: false);
    }

    private void OnSettingsApplied(Settings settings)
    {
        ApplyRuntimeSettings(settings, preserveUiScaleWindow: false, recreateSettingsWindow: true);
    }

    private void ApplyRuntimeSettings(Settings settings, bool preserveUiScaleWindow, bool recreateSettingsWindow)
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
                _camera.InvertScrollZoom = settings.InvertCameraZoom;
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
            _camera.InvertScrollZoom = settings.InvertCameraZoom;
        }

        if (uiScaleChanged && (_gameState == GameState.InGame || _gameState == GameState.Transition))
        {
            RefreshInGameWindows(preserveUiScaleWindow);
        }

        if (uiScaleChanged || resolutionChanged)
        {
            ResizeUiForViewport(_graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
            RebuildMainMenuForCurrentDisplay();
            if (recreateSettingsWindow)
            {
                RecreateSettingsWindow();
            }
        }
    }

    private void RecreateSettingsWindow()
    {
        _settingsWindow = new SettingsWindow(_graphics, _settings, OnSettingsApplied, PreviewUiScale);
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
        _combatScreen?.ResizeViewport(width, height);
        _aiActionIndicator?.ResizeViewport(width, height);
        _playerDashboard?.ResizeViewport(width, height);

        _mainMenu?.ResizeViewport(width, height);
        _lobbyManager?.ResizeViewport(width, height);
        _settingsWindow?.ResizeViewport();
        _gameplayHudOverlay?.ResizeViewport(width, height);
        _combatHudOverlay?.ResizeViewport(width, height);

        _playerDashboardWindow?.ResizeViewport(width, height);
        _aiVisualizationWindow?.ResizeViewport(width, height);
        _debugInfoWindow?.ResizeViewport(width, height);
        _uiScaleWindow?.ResizeViewport(width, height);
        _encyclopediaWindow?.ResizeViewport(width, height);
        _tutorialWindow?.ResizeViewport(width, height);

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
        if (_lobbyManager == null || _gameStateCache == null || _mapData == null || _camera == null)
        {
            GameFeedbackBus.PublishError("Game initialization failed", "Client world services are not ready.");
            return;
        }

        _connectionManager ??= new ConnectionManager(_lobbyManager.MultiplayerServerAddress);

        var connectionManager = _connectionManager;
        var gameClient = connectionManager.EnsureGameClient();
        var gameStateCache = _gameStateCache;
        var mapData = _mapData;
        var camera = _camera;

        _gameStateCache.Clear();
        GameFeedbackBus.PublishBusy("Joining multiplayer session", "Opening the game stream.");

        _playerDashboard = new PlayerDashboard(GraphicsDevice, gameClient, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
        _playerDashboard.IsVisible = false;
        _inputController = new InputController(gameClient, gameStateCache, mapData, camera);
        CreateInGameWindows(gameClient);
        
        if (_inGameDesktop != null)
        {
            _contextMenuManager = new ContextMenuManager(gameClient, gameStateCache, mapData, camera, _inGameDesktop);
            _inputController.SetContextMenuManager(_contextMenuManager);
        }
        AttachInGameWindows();
        
        if (_defaultFont != null)
        {
            _playerDashboard?.LoadContent(_defaultFont);
        }
        
        Task.Run(async () =>
        {
            try
            {
                var success = await connectionManager.ConnectAsync(playerId, playerName, sessionId);
                if (success && connectionManager.CurrentPlayerId != null)
                {
                    _currentPlayerId = connectionManager.CurrentPlayerId;
                    _inputController?.SetCurrentPlayer(_currentPlayerId);
                    _playerDashboard?.SetCurrentPlayer(_currentPlayerId);
                    _playerDashboardWindow?.SetCurrentPlayer(_currentPlayerId);
                    _contextMenuManager?.SetCurrentPlayer(_currentPlayerId);
                    GameFeedbackBus.PublishBusy("Game stream connected", "Waiting for the first world snapshot from the server.");
                }
                else
                {
                    System.Console.WriteLine($"Failed to connect: {connectionManager.ErrorMessage}");
                    GameFeedbackBus.PublishError("Failed to connect to multiplayer game", connectionManager.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Failed to connect to game: {ex.Message}");
                GameFeedbackBus.PublishError("Failed to initialize multiplayer game", ex.Message);
            }
        });
    }

    private void InitializeSinglePlayerGame(string sessionId, string playerName, string playerId)
    {
        if (_lobbyManager?.EmbeddedServer == null || _gameStateCache == null || _mapData == null || _camera == null)
        {
            GameFeedbackBus.PublishError("Game initialization failed", "Single-player world services are not ready.");
            return;
        }

        var gameStateCache = _gameStateCache;
        var mapData = _mapData;
        var camera = _camera;

        _gameStateCache.Clear();
        GameFeedbackBus.PublishBusy("Launching single-player command deck", "Connecting to the embedded game stream.");

        var gameClient = GrpcGameClient.CreateForSinglePlayer(_lobbyManager.EmbeddedServer);
        _connectionManager = new ConnectionManager(gameClient);

        _playerDashboard = new PlayerDashboard(GraphicsDevice, gameClient, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
        _playerDashboard.IsVisible = false;
        _inputController = new InputController(gameClient, gameStateCache, mapData, camera);
        CreateInGameWindows(gameClient);

        _serverStatusIndicator = new ServerStatusIndicator(500);
        _serverStatusIndicator.SetServerHost(_lobbyManager.EmbeddedServer);
        
        if (_inGameDesktop != null)
        {
            _contextMenuManager = new ContextMenuManager(gameClient, gameStateCache, mapData, camera, _inGameDesktop);
            _inputController.SetContextMenuManager(_contextMenuManager);
        }
        AttachInGameWindows();
        
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
                    GameFeedbackBus.PublishBusy("Embedded stream connected", "Waiting for the initial world snapshot.");
                }
                else
                {
                    System.Console.WriteLine($"Failed to connect to embedded server: {_connectionManager.ErrorMessage}");
                    GameFeedbackBus.PublishError("Failed to connect to embedded game stream", _connectionManager.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Failed to initialize single player game: {ex.Message}");
                GameFeedbackBus.PublishError("Failed to initialize single-player game", ex.Message);
            }
        });
    }

    private void ReturnToMainMenu()
    {
        StartTransition("Returning to main menu");
        _pendingGameEntry = false;
        _latestFeedback = null;
        _latestFeedbackExpiresAt = DateTime.MinValue;
        GameFeedbackBus.Clear();
        
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
        _uiScaleWindow = null;
        _encyclopediaWindow = null;
        _tutorialWindow = null;
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

        DrawGlobalStatusOverlay(_spriteBatch);

        base.Draw(gameTime);
    }

    private void DrawLobby(SpriteBatch spriteBatch)
    {
        _lobbyManager?.Draw(spriteBatch);
    }

    private void DrawTransitionScreen(SpriteBatch spriteBatch)
    {
        if (_defaultFont == null || _pixelTexture == null)
            return;
            
        spriteBatch.Begin(sortMode: SpriteSortMode.Deferred, blendState: BlendState.AlphaBlend, samplerState: SamplerState.LinearClamp);
        spriteBatch.Draw(_pixelTexture, new Rectangle(0, 0, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight), Color.Black * 0.78f);

        var elapsed = DateTime.Now - _transitionStartTime;
        var dotCount = (int)(elapsed.TotalSeconds * 2) % 4;
        var dots = new string('.', dotCount);
        var status = BuildVisibleStatus();
        var title = (status?.Title ?? _transitionMessage) + dots;
        var detail = status?.Detail ?? GetTransitionDetail();

        var titleSize = _defaultFont.MeasureString(title);
        var titlePos = new Vector2(
            (_graphics.PreferredBackBufferWidth - titleSize.X) / 2,
            (_graphics.PreferredBackBufferHeight / 2f) - 28f);

        spriteBatch.DrawString(_defaultFont, title, titlePos, status?.Accent ?? Color.LightGray);

        if (!string.IsNullOrWhiteSpace(detail))
        {
            var detailSize = _defaultFont.MeasureString(detail);
            var detailPos = new Vector2(
                (_graphics.PreferredBackBufferWidth - detailSize.X) / 2,
                titlePos.Y + titleSize.Y + 18f);
            spriteBatch.DrawString(_defaultFont, detail, detailPos, Color.Silver, 0f, Vector2.Zero, 0.78f, SpriteEffects.None, 0f);
        }

        spriteBatch.End();
    }

    private void StartTransition(string message)
    {
        _transitionMessage = message;
        _transitionStartTime = DateTime.Now;
        _gameState = GameState.Transition;
    }

    private void UpdateFeedbackState()
    {
        while (GameFeedbackBus.TryDequeue(out var feedback))
        {
            _latestFeedback = feedback;
            _latestFeedbackExpiresAt = DateTime.UtcNow + GetFeedbackDuration(feedback);
        }

        if (_latestFeedback != null && DateTime.UtcNow > _latestFeedbackExpiresAt)
        {
            _latestFeedback = null;
            _latestFeedbackExpiresAt = DateTime.MinValue;
        }
    }

    private static TimeSpan GetFeedbackDuration(GameFeedbackMessage feedback)
    {
        if (feedback.Sticky)
        {
            return feedback.Severity == GameFeedbackSeverity.Error
                ? TimeSpan.FromSeconds(12)
                : TimeSpan.FromSeconds(8);
        }

        return feedback.Severity == GameFeedbackSeverity.Busy
            ? TimeSpan.FromSeconds(6)
            : TimeSpan.FromSeconds(4);
    }

    private (string Title, string? Detail, Color Accent, bool Blocking)? BuildVisibleStatus()
    {
        var derived = BuildDerivedStatus();
        if (derived.HasValue && derived.Value.Blocking)
        {
            return derived;
        }

        if (_latestFeedback != null)
        {
            return (_latestFeedback.Title, _latestFeedback.Detail, GetSeverityColor(_latestFeedback.Severity), false);
        }

        return derived;
    }

    private (string Title, string? Detail, Color Accent, bool Blocking)? BuildDerivedStatus()
    {
        if (_gameState == GameState.Transition && _pendingGameEntry)
        {
            return ("Preparing game session", GetTransitionDetail(), ThemeManager.Colors.TextWarning, true);
        }

        if (_gameState != GameState.InGame)
        {
            return null;
        }

        if (!HasReceivedInitialGameState())
        {
            return ("Synchronizing game world", GetTransitionDetail(), ThemeManager.Colors.TextWarning, true);
        }

        if (_combatScreen?.IsActive == true)
        {
            return ("Combat in progress", "Resolve the battle to continue the strategic turn.", ThemeManager.Colors.TextWarning, false);
        }

        if (_connectionManager?.Status == ConnectionStatus.Reconnecting)
        {
            return (
                "Reconnecting to the game stream",
                $"Retry {_connectionManager.ReconnectAttempts} of {_connectionManager.MaxAttempts}. World view may pause until the uplink returns.",
                ThemeManager.Colors.TextWarning,
                true);
        }

        if (_connectionManager?.Status == ConnectionStatus.Error)
        {
            return (
                "Connection problem detected",
                _connectionManager.ErrorMessage,
                ThemeManager.Colors.TextError,
                true);
        }

        var currentTurnPlayerId = _gameStateCache?.GetCurrentPlayerId();
        var currentPhase = _gameStateCache?.GetCurrentPhase() ?? TurnPhase.Production;
        var phaseInstruction = GetPhaseInstruction(currentPhase);

        if (!string.IsNullOrWhiteSpace(_currentPlayerId) && currentTurnPlayerId == _currentPlayerId)
        {
            return ("Your turn", phaseInstruction, ThemeManager.Colors.TextSuccess, false);
        }

        if (!string.IsNullOrWhiteSpace(currentTurnPlayerId))
        {
            var activePlayerName = _gameStateCache?.GetPlayerState(currentTurnPlayerId)?.PlayerName ?? "another commander";
            return ("Waiting for opponent action", $"{activePlayerName} is resolving {GetPhaseName(currentPhase).ToLowerInvariant()}.", ThemeManager.Colors.TextSecondary, false);
        }

        return null;
    }

    private string GetTransitionDetail()
    {
        if (_connectionManager == null)
        {
            return "Initializing client systems and preparing renderers.";
        }

        return _connectionManager.Status switch
        {
            ConnectionStatus.Connecting => "Opening the game stream.",
            ConnectionStatus.Reconnecting => $"Retrying the stream connection ({_connectionManager.ReconnectAttempts}/{_connectionManager.MaxAttempts}).",
            ConnectionStatus.Connected when !HasReceivedInitialGameState() => "Connected. Waiting for the first world snapshot from the server.",
            ConnectionStatus.Connected => "Game stream connected.",
            ConnectionStatus.Error => _connectionManager.ErrorMessage,
            _ => "Waiting for the game service to respond."
        };
    }

    private static string GetPhaseName(TurnPhase phase)
    {
        return phase switch
        {
            TurnPhase.Production => "Production Phase",
            TurnPhase.Purchase => "Purchase Phase",
            TurnPhase.Reinforcement => "Reinforcement Phase",
            TurnPhase.Movement => "Movement Phase",
            _ => "Unknown Phase"
        };
    }

    private static string GetPhaseInstruction(TurnPhase phase)
    {
        return phase switch
        {
            TurnPhase.Production => "Produce resources or advance to purchasing.",
            TurnPhase.Purchase => "Buy armies or advance to reinforcement.",
            TurnPhase.Reinforcement => "Reinforce owned regions and portals, then advance.",
            TurnPhase.Movement => "Move armies, resolve positioning, or end the turn.",
            _ => "Choose your next strategic action."
        };
    }

    private static Color GetSeverityColor(GameFeedbackSeverity severity)
    {
        return severity switch
        {
            GameFeedbackSeverity.Success => ThemeManager.Colors.TextSuccess,
            GameFeedbackSeverity.Warning => ThemeManager.Colors.TextWarning,
            GameFeedbackSeverity.Error => ThemeManager.Colors.TextError,
            GameFeedbackSeverity.Busy => ThemeManager.Colors.TextAccent,
            _ => ThemeManager.Colors.TextPrimary
        };
    }

    private void DrawGlobalStatusOverlay(SpriteBatch spriteBatch)
    {
        if (_defaultFont == null || _pixelTexture == null || _gameState == GameState.Transition || _gameState == GameState.InGame)
        {
            return;
        }

        var status = BuildVisibleStatus();
        if (!status.HasValue)
        {
            return;
        }

        var title = status.Value.Title;
        var detail = status.Value.Detail;
        var accent = status.Value.Accent;

        var titleSize = _defaultFont.MeasureString(title);
        var detailScale = 0.72f;
        var detailSize = string.IsNullOrWhiteSpace(detail)
            ? Vector2.Zero
            : _defaultFont.MeasureString(detail) * detailScale;

        int paddingX = 18;
        int paddingY = 12;
        int panelWidth = (int)Math.Max(titleSize.X, detailSize.X) + (paddingX * 2);
        int panelHeight = (int)(titleSize.Y + (string.IsNullOrWhiteSpace(detail) ? 0 : detailSize.Y + 10)) + (paddingY * 2);
        int panelX = (_graphics.PreferredBackBufferWidth - panelWidth) / 2;
        int panelY = 18;

        spriteBatch.Begin(sortMode: SpriteSortMode.Deferred, blendState: BlendState.AlphaBlend, samplerState: SamplerState.LinearClamp);
        spriteBatch.Draw(_pixelTexture, new Rectangle(panelX, panelY, panelWidth, panelHeight), Color.Black * 0.72f);
        spriteBatch.Draw(_pixelTexture, new Rectangle(panelX, panelY, panelWidth, 2), accent);
        spriteBatch.Draw(_pixelTexture, new Rectangle(panelX, panelY + panelHeight - 2, panelWidth, 2), accent);
        spriteBatch.Draw(_pixelTexture, new Rectangle(panelX, panelY, 2, panelHeight), accent);
        spriteBatch.Draw(_pixelTexture, new Rectangle(panelX + panelWidth - 2, panelY, 2, panelHeight), accent);

        var titlePos = new Vector2(panelX + paddingX, panelY + paddingY);
        spriteBatch.DrawString(_defaultFont, title, titlePos, accent);

        if (!string.IsNullOrWhiteSpace(detail))
        {
            var detailPos = new Vector2(panelX + paddingX, titlePos.Y + titleSize.Y + 10);
            spriteBatch.DrawString(_defaultFont, detail, detailPos, Color.Silver, 0f, Vector2.Zero, detailScale, SpriteEffects.None, 0f);
        }

        spriteBatch.End();
    }

    private void DrawInGame(SpriteBatch spriteBatch)
    {
        if (_mapData == null || _gameStateCache == null)
        {
            return;
        }

        if (_camera != null)
        {
            _mapRenderer?.Draw(spriteBatch, _mapData, _camera);
            _regionRenderer?.Draw(spriteBatch, _mapData, _gameStateCache, _camera);
            
            if (_inputController != null && !(_combatScreen?.IsActive ?? false))
            {
                _selectionRenderer?.Draw(spriteBatch, _mapData, _gameStateCache, _inputController, _camera);
            }
            
            _aiActionIndicator?.Draw(spriteBatch, _camera, _mapData);
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
                _playerDashboardWindow?.SetCurrentPlayer(_currentPlayerId);
            }
            else if (connStatus.Status == GameConnectionStatus.Types.ConnectionState.Disconnected)
            {
                GameFeedbackBus.PublishWarning("Game stream disconnected", connStatus.Message, sticky: true);
            }
            else if (connStatus.Status == GameConnectionStatus.Types.ConnectionState.Error)
            {
                GameFeedbackBus.PublishError("Game stream error", connStatus.Message);
            }
        }

        if (update.UpdateCase == GameUpdate.UpdateOneofCase.Error)
        {
            var error = update.Error;
            if (error != null)
            {
                GameFeedbackBus.PublishError(error.ErrorCode, error.ErrorMessage);
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

    private void CreateInGameWindows(GrpcGameClient gameClient, bool preserveUiScaleWindow = false)
    {
        DetachInGameWindows();

        _gameplayHudOverlay = new GameplayHudOverlay(_graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
        _combatHudOverlay = new CombatHudOverlay(_graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
        _playerDashboardWindow = new PlayerDashboardWindow(gameClient, _windowPreferences, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
        _aiVisualizationWindow = new AIVisualizationWindow(_windowPreferences, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
        _debugInfoWindow = new DebugInfoWindow(_windowPreferences, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
        if (!preserveUiScaleWindow || _uiScaleWindow == null)
        {
            _uiScaleWindow = new UiScaleWindow(_settings, _windowPreferences, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight, PreviewUiScale);
        }
        else
        {
            _uiScaleWindow.SyncFromSettings();
        }
        _encyclopediaWindow = new EncyclopediaWindow(_windowPreferences, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
        _tutorialWindow = new TutorialWindow(_windowPreferences, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);

        if (!string.IsNullOrWhiteSpace(_currentPlayerId))
        {
            _playerDashboardWindow.SetCurrentPlayer(_currentPlayerId);
        }

        _aiActionTracker?.SetAIVisualizationWindow(_aiVisualizationWindow);
    }

    private void DetachInGameWindows()
    {
        if (_inGameDesktop == null)
        {
            return;
        }

        if (_gameplayHudOverlay != null)
        {
            _inGameDesktop.Widgets.Remove(_gameplayHudOverlay.TopBar);
            _inGameDesktop.Widgets.Remove(_gameplayHudOverlay.LegendPanel);
            _inGameDesktop.Widgets.Remove(_gameplayHudOverlay.AiActivityPanel);
            _inGameDesktop.Widgets.Remove(_gameplayHudOverlay.SelectionPanel);
            _inGameDesktop.Widgets.Remove(_gameplayHudOverlay.HelpPanel);
        }

        if (_combatHudOverlay != null)
        {
            _inGameDesktop.Widgets.Remove(_combatHudOverlay.Backdrop);
            _inGameDesktop.Widgets.Remove(_combatHudOverlay.Window);
        }

        if (_playerDashboardWindow != null)
        {
            _inGameDesktop.Widgets.Remove(_playerDashboardWindow.Window);
        }

        if (_aiVisualizationWindow != null)
        {
            _inGameDesktop.Widgets.Remove(_aiVisualizationWindow.Window);
        }

        if (_debugInfoWindow != null)
        {
            _inGameDesktop.Widgets.Remove(_debugInfoWindow.Window);
        }

        if (_uiScaleWindow != null)
        {
            _inGameDesktop.Widgets.Remove(_uiScaleWindow.Window);
        }

        if (_encyclopediaWindow != null)
        {
            _inGameDesktop.Widgets.Remove(_encyclopediaWindow.Window);
        }

        if (_tutorialWindow != null)
        {
            _inGameDesktop.Widgets.Remove(_tutorialWindow.Window);
        }
    }

    private void AttachInGameWindows()
    {
        if (_inGameDesktop == null)
        {
            return;
        }

        _inGameDesktop.Root = null;

        AttachDesktopWidget(_gameplayHudOverlay?.TopBar);
        AttachDesktopWidget(_gameplayHudOverlay?.LegendPanel);
        AttachDesktopWidget(_gameplayHudOverlay?.AiActivityPanel);
        AttachDesktopWidget(_gameplayHudOverlay?.SelectionPanel);
        AttachDesktopWidget(_gameplayHudOverlay?.HelpPanel);
        AttachDesktopWidget(_playerDashboardWindow?.Window);
        AttachDesktopWidget(_aiVisualizationWindow?.Window);
        AttachDesktopWidget(_debugInfoWindow?.Window);
        AttachDesktopWidget(_uiScaleWindow?.Window);
        AttachDesktopWidget(_encyclopediaWindow?.Window);
        AttachDesktopWidget(_tutorialWindow?.Window);

        if (_serverStatusIndicator != null)
        {
            _serverStatusIndicator.Container.HorizontalAlignment = HorizontalAlignment.Center;
            _serverStatusIndicator.Container.VerticalAlignment = VerticalAlignment.Bottom;
            _serverStatusIndicator.Container.Top = _graphics.PreferredBackBufferHeight - 35;
            AttachDesktopWidget(_serverStatusIndicator.Container);
        }

        AttachDesktopWidget(_combatHudOverlay?.Backdrop);
        AttachDesktopWidget(_combatHudOverlay?.Window);
    }

    private void RefreshInGameWindows(bool preserveUiScaleWindow = false)
    {
        var gameClient = _connectionManager?.GameClient;
        if (gameClient == null)
        {
            return;
        }

        _contextMenuManager?.CloseContextMenu();
        CreateInGameWindows(gameClient, preserveUiScaleWindow);
        AttachInGameWindows();
        UpdateGameplayHud();
    }

    private void AttachDesktopWidget(Widget? widget)
    {
        if (_inGameDesktop == null || widget == null)
        {
            return;
        }

        _inGameDesktop.Widgets.Remove(widget);
        _inGameDesktop.Widgets.Add(widget);
    }

    private void UpdateCombatHud()
    {
        _combatHudOverlay?.Update(_combatScreen?.GetPresentation());
    }

    private void UpdateTutorialWindow()
    {
        _tutorialWindow?.UpdateContent(_gameStateCache, _currentPlayerId, _inputController?.Selection, _combatScreen?.IsActive == true);
    }

    private void UpdateGameplayHud()
    {
        if (_gameplayHudOverlay == null || _gameState != GameState.InGame || _gameStateCache == null)
        {
            return;
        }

        var status = BuildVisibleStatus();
        _gameplayHudOverlay.Update(
            _gameStateCache,
            _currentPlayerId,
            status?.Title,
            status?.Detail,
            status?.Accent ?? ThemeManager.Colors.TextSecondary,
            _inputController?.Selection,
            _aiActionIndicator?.IsAIThinking == true,
            _aiActionIndicator?.ActiveAIPlayerName,
            _aiActionIndicator?.GetRecentLogEntries(),
            _inputController?.ShowHelp ?? false,
            _playerDashboardWindow?.IsVisible == true,
            _aiVisualizationWindow?.IsVisible == true,
            _debugInfoWindow?.IsVisible == true,
            _uiScaleWindow?.IsVisible == true,
            _encyclopediaWindow?.IsVisible == true,
            _tutorialWindow?.IsVisible == true);
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




