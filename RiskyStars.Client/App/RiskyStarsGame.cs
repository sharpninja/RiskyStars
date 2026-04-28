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
    private ContinentZoomRenderer? _continentZoomRenderer;
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
    private TutorialModeWindow? _tutorialModeWindow;
    private ContinentZoomWindow? _continentZoomWindow;
    private SettingsWindow? _settingsWindow;
    private ServerStatusIndicator? _serverStatusIndicator;

    private SidePanelContainer? _leftSidePanel;
    private SidePanelContainer? _rightSidePanel;
    
    private MapData? _mapData;
    private SpriteFont? _defaultFont;
    
    private string? _currentPlayerId;
    private KeyboardState _previousKeyState;
    private MouseState _previousMouseState;
    
    private GameState _gameState = GameState.MainMenu;
    private bool _pendingResolutionChange = false;
    private bool _handlingClientResize = false;
    private GameWindowMode _appliedWindowMode = GameWindowMode.Normal;
    private string _transitionMessage = "";
    private DateTime _transitionStartTime;
    private bool _pendingGameEntry = false;
    private bool _tutorialModeActive = false;
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
        _settings.Normalize();
        var windowModePlan = GameWindowModeController.CreatePlan(_settings.WindowMode);

        _graphics.PreferredBackBufferWidth = _settings.ResolutionWidth;
        _graphics.PreferredBackBufferHeight = _settings.ResolutionHeight;
        _graphics.IsFullScreen = windowModePlan.IsFullscreen;
        
        if (_graphics.GraphicsDevice != null)
        {
            _graphics.ApplyChanges();
        }

        ApplyDesktopWindowMode(windowModePlan, _settings.WindowMode);
        _appliedWindowMode = _settings.WindowMode;
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
            var currentWindowMode = GetCurrentWindowMode();
            _graphics.PreferredBackBufferWidth = width;
            _graphics.PreferredBackBufferHeight = height;
            _graphics.ApplyChanges();

            _settings.WindowMode = currentWindowMode;
            _settings.Fullscreen = currentWindowMode == GameWindowMode.Full;
            _appliedWindowMode = currentWindowMode;

            if (currentWindowMode == GameWindowMode.Normal)
            {
                _settings.ResolutionWidth = width;
                _settings.ResolutionHeight = height;
            }

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
        
        _camera = CreateConfiguredCamera();
        
        _mapRenderer = new MapRenderer(GraphicsDevice);
        _regionRenderer = new RegionRenderer(GraphicsDevice);
        _selectionRenderer = new SelectionRenderer(GraphicsDevice);
        _continentZoomRenderer = new ContinentZoomRenderer(GraphicsDevice);
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
        ApplyDesktopWindowMode(GameWindowModeController.CreatePlan(_settings.WindowMode), _settings.WindowMode);
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
            _continentZoomRenderer?.LoadContent(_defaultFont);
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
            _mainMenu.ResetNavigationRequests();
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
            _mainMenu.ResetNavigationRequests();
        }

        if (_mainMenu.ShouldStartTutorial)
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

            _lobbyManager.SetTutorialMode();
            _gameState = GameState.Lobby;
            _mainMenu.SetState(MainMenuState.Main);
            _mainMenu.ResetNavigationRequests();
        }

        if (HasDisplaySettingsChanged(_mainMenu.Settings))
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
                    InitializeSinglePlayerGame(_lobbyManager.SessionId, _lobbyManager.PlayerName ?? "Player", _lobbyManager.PlayerId, _lobbyManager.IsTutorialMode);
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

        HandlePanelShortcuts(keyState);

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
            bool isContinentZoomOpen = _continentZoomWindow?.IsVisible == true;
            var mouseState = Mouse.GetState();

            if (isContinentZoomOpen && HandleContinentZoomPointer(mouseState))
            {
                _previousMouseState = mouseState;
                return;
            }

            if (!isContinentZoomOpen)
            {
                _camera?.Update(gameTime);
            }

            if (_inputController != null)
            {
                _inputController.IsPointerInputBlocked = isContinentZoomOpen;
                _inputController.Update(gameTime);
            }

            if (!isContinentZoomOpen)
            {
                _selectionRenderer?.Update(gameTime);
            }

            _previousMouseState = mouseState;
            
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
        
        CaptureCurrentCameraView();
        DrainPendingGameUpdates();
        UpdateTutorialWindow();
        UpdateCombatHud();
        UpdateGameplayHud();
    }

    private void HandlePanelShortcuts(KeyboardState keyState)
    {
        foreach (var toggle in InGameShortcutRouter.PanelToggles)
        {
            if (keyState.IsKeyDown(toggle.Key) && _previousKeyState.IsKeyUp(toggle.Key))
            {
                TogglePanel(toggle.Panel);
            }
        }
    }

    private void TogglePanel(InGamePanelToggle panel)
    {
        switch (panel)
        {
            case InGamePanelToggle.DebugInfo:
                _debugInfoWindow?.Toggle();
                break;
            case InGamePanelToggle.CommandDashboard:
                _playerDashboardWindow?.Toggle();
                break;
            case InGamePanelToggle.AiVisualization:
                _aiVisualizationWindow?.Toggle();
                break;
            case InGamePanelToggle.UiScale:
                _uiScaleWindow?.SyncFromSettings();
                _uiScaleWindow?.Toggle();
                break;
            case InGamePanelToggle.Encyclopedia:
                _encyclopediaWindow?.Toggle();
                break;
            case InGamePanelToggle.GuidedTutorial:
                _tutorialModeWindow?.Toggle();
                AnchorTutorialWindowToMapLeft();
                break;
        }
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
        settings.Normalize();
        bool uiScaleChanged = settings.UiScalePercent != ThemeManager.CurrentUiScalePercent;
        ThemeManager.ApplyThemeSettings(settings);

        bool resolutionChanged = HasDisplaySettingsChanged(settings);
        
        if (resolutionChanged)
        {
            CaptureCurrentCameraView();
            ApplySettings();
            _camera = CreateConfiguredCamera();
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
            ApplyCameraRuntimeSettings(_camera, settings);
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

    private Camera2D CreateConfiguredCamera()
    {
        var camera = new Camera2D(_graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
        ApplyCameraRuntimeSettings(camera, _settings);
        MapCameraPersistence.Restore(_settings.MapCamera, camera);
        return camera;
    }

    private static void ApplyCameraRuntimeSettings(Camera2D camera, Settings settings)
    {
        camera.PanSpeed = settings.CameraPanSpeed;
        camera.ZoomSpeed = settings.CameraZoomSpeed;
        camera.InvertScrollZoom = settings.InvertCameraZoom;
    }

    private void CaptureCurrentCameraView()
    {
        if (_camera == null)
        {
            return;
        }

        MapCameraPersistence.Capture(_settings.MapCamera, _camera);
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
        _tutorialModeWindow?.ResizeViewport(width, height);
        _continentZoomWindow?.ResizeViewport(width, height);

        int topBarHeight = _gameplayHudOverlay?.GetTopBarHeight() ?? ThemeManager.ScalePixels(80);
        int panelTopOffset = topBarHeight + ThemeManager.ScalePixels(12);
        System.Diagnostics.Debug.WriteLine($"[UI] Resizing panels: size={width}x{height}, topBar={topBarHeight}");
        _leftSidePanel?.ResizeViewport(width, height, panelTopOffset);
        _rightSidePanel?.ResizeViewport(width, height, panelTopOffset);
        AnchorTutorialWindowToMapLeft();

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
        _tutorialModeActive = false;

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
        _inputController.ContinentZoomRequested += OnContinentZoomRequested;
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

    private void InitializeSinglePlayerGame(string sessionId, string playerName, string playerId, bool tutorialMode = false)
    {
        _tutorialModeActive = tutorialMode;

        if (_lobbyManager?.EmbeddedServer == null || _gameStateCache == null || _mapData == null || _camera == null)
        {
            GameFeedbackBus.PublishError("Game initialization failed", "Single-player world services are not ready.");
            return;
        }

        var gameStateCache = _gameStateCache;
        var mapData = _mapData;
        var camera = _camera;

        _gameStateCache.Clear();
        GameFeedbackBus.PublishBusy(
            tutorialMode ? "Launching tutorial command deck" : "Launching single-player command deck",
            "Connecting to the embedded game stream.");

        var gameClient = GrpcGameClient.CreateForSinglePlayer(_lobbyManager.EmbeddedServer);
        _connectionManager = new ConnectionManager(gameClient);

        _playerDashboard = new PlayerDashboard(GraphicsDevice, gameClient, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
        _playerDashboard.IsVisible = false;
        _inputController = new InputController(gameClient, gameStateCache, mapData, camera);
        _inputController.ContinentZoomRequested += OnContinentZoomRequested;
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
        PersistWindowDisplaySettings();
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
        _tutorialModeWindow = null;
        _continentZoomWindow = null;
        _serverStatusIndicator = null;
        _contextMenuManager = null;
        _tutorialModeActive = false;
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
        PrepareBackBufferDrawState();
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

        foreach (var pass in WorldRenderPipeline.OrderedPasses)
        {
            switch (pass)
            {
                case WorldRenderPass.OffscreenZoomSurface:
                    UpdateContinentZoomSurface(spriteBatch);
                    break;
                case WorldRenderPass.BackBufferWorld:
                    DrawBackBufferWorld(spriteBatch);
                    break;
                case WorldRenderPass.UiOverlay:
                    DrawInGameUi();
                    break;
            }
        }
    }

    private void DrawBackBufferWorld(SpriteBatch spriteBatch)
    {
        if (_mapData == null || _gameStateCache == null)
        {
            return;
        }

        PrepareBackBufferDrawState();
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
    }

    private void UpdateContinentZoomSurface(SpriteBatch spriteBatch)
    {
        _continentZoomRenderer?.UpdateSurface(
            spriteBatch,
            _continentZoomWindow,
            _gameStateCache,
            _inputController?.Selection,
            _regionRenderer);
    }

    private void DrawInGameUi()
    {
        _inGameDesktop?.Render();
        _settingsWindow?.Render();
    }

    private void PrepareBackBufferDrawState()
    {
        var state = WorldBackBufferDrawState.Create(
            _graphics.PreferredBackBufferWidth,
            _graphics.PreferredBackBufferHeight);

        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Viewport = state.Viewport;
        GraphicsDevice.ScissorRectangle = state.ScissorRectangle;
        GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
        GraphicsDevice.Textures[0] = null;
    }

    private bool HandleContinentZoomPointer(MouseState mouseState)
    {
        if (_continentZoomWindow?.IsVisible != true)
        {
            return false;
        }

        if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
        {
            return _continentZoomWindow.TrySelectRegion(mouseState.Position);
        }

        return false;
    }

    private void ApplyDesktopWindowMode(WindowModePlan plan, GameWindowMode mode)
    {
        if (plan.MaximizeWindow)
        {
            GameWindowModeController.ApplyDesktopMode(Window.Handle, GameWindowMode.Maximized);
        }
        else if (plan.RestoreWindow)
        {
            GameWindowModeController.ApplyDesktopMode(Window.Handle, GameWindowMode.Normal);
        }
    }

    private bool HasDisplaySettingsChanged(Settings settings)
    {
        return WindowDisplaySettings.HasDisplaySettingsChanged(
            settings,
            new AppliedWindowDisplayState(
                _graphics.PreferredBackBufferWidth,
                _graphics.PreferredBackBufferHeight,
                _graphics.IsFullScreen,
                _appliedWindowMode));
    }

    private GameWindowMode GetCurrentWindowMode()
    {
        if (_graphics.IsFullScreen)
        {
            return GameWindowMode.Full;
        }

        if (GameWindowModeController.TryDetectMaximized(Window.Handle, out var isMaximized))
        {
            return GameWindowModeController.DetectMode(false, isMaximized);
        }

        return _appliedWindowMode == GameWindowMode.Maximized ? GameWindowMode.Maximized : GameWindowMode.Normal;
    }

    private void PersistWindowDisplaySettings()
    {
        try
        {
            CaptureCurrentCameraView();
            var currentMode = GetCurrentWindowMode();
            var width = currentMode == GameWindowMode.Full
                ? _graphics.PreferredBackBufferWidth
                : Window.ClientBounds.Width;
            var height = currentMode == GameWindowMode.Full
                ? _graphics.PreferredBackBufferHeight
                : Window.ClientBounds.Height;

            WindowDisplaySettings.CaptureCurrentDisplay(
                _settings,
                new AppliedWindowDisplayState(
                    width,
                    height,
                    currentMode == GameWindowMode.Full,
                    currentMode));

            _settings.Save();
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Failed to persist window display settings: {ex.Message}");
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

        int leftWidth = Math.Max(200, Math.Min(500, _windowPreferences.LeftPanelWidth));
        int rightWidth = Math.Max(200, Math.Min(500, _windowPreferences.RightPanelWidth));
        bool leftCollapsed = _windowPreferences.LeftPanelCollapsed;
        bool rightCollapsed = _windowPreferences.RightPanelCollapsed;

        _gameplayHudOverlay = new GameplayHudOverlay(_graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
        _combatHudOverlay = new CombatHudOverlay(_graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);

        int topBarHeight = _gameplayHudOverlay.GetTopBarHeight();
        int screenWidth = Window.ClientBounds.Width;
        int screenHeight = Window.ClientBounds.Height;

        System.Diagnostics.Debug.WriteLine($"[UI] Creating panels: screen={screenWidth}x{screenHeight}, topBar={topBarHeight}, gfx={_graphics.PreferredBackBufferWidth}x{_graphics.PreferredBackBufferHeight}");

        int panelTopOffset = topBarHeight + ThemeManager.ScalePixels(12);
        _leftSidePanel = new SidePanelContainer("left", leftWidth, screenWidth, screenHeight, panelTopOffset);
        _rightSidePanel = new SidePanelContainer("right", rightWidth, screenWidth, screenHeight, panelTopOffset);
        _leftSidePanel.SetCollapsed(leftCollapsed, false);
        _rightSidePanel.SetCollapsed(rightCollapsed, false);

        _leftSidePanel.WidthChanged += OnSidePanelWidthChanged;
        _leftSidePanel.CollapseChanged += OnSidePanelCollapseChanged;
        _rightSidePanel.WidthChanged += OnSidePanelWidthChanged;
        _rightSidePanel.CollapseChanged += OnSidePanelCollapseChanged;

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
        _tutorialModeWindow = new TutorialModeWindow(_windowPreferences, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
        if (!_tutorialModeActive)
        {
            _tutorialModeWindow.Hide();
        }

        _continentZoomWindow = new ContinentZoomWindow(_graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
        _continentZoomWindow.RegionSelected += OnContinentZoomRegionSelected;

        _tutorialModeWindow.EndRequested += EndTutorialMode;
        AnchorTutorialWindowToMapLeft();

        PopulateSidePanels();

        if (!string.IsNullOrWhiteSpace(_currentPlayerId))
        {
            _playerDashboardWindow.SetCurrentPlayer(_currentPlayerId);
        }

        _aiActionTracker?.SetAIVisualizationWindow(_aiVisualizationWindow);
    }

    private void OnSidePanelWidthChanged(object? sender, int width)
    {
        if (sender == _leftSidePanel)
        {
            _windowPreferences.LeftPanelWidth = width;
        }
        else if (sender == _rightSidePanel)
        {
            _windowPreferences.RightPanelWidth = width;
        }

        AnchorTutorialWindowToMapLeft();
        //_windowPreferences.Save(); // disabled for testing
    }

    private void OnSidePanelCollapseChanged(object? sender, bool isCollapsed)
    {
        if (sender == _leftSidePanel)
        {
            _windowPreferences.LeftPanelCollapsed = isCollapsed;
        }
        else if (sender == _rightSidePanel)
        {
            _windowPreferences.RightPanelCollapsed = isCollapsed;
        }

        AnchorTutorialWindowToMapLeft();
        //_windowPreferences.Save(); // disabled for testing
    }

    private void PopulateSidePanels()
    {
        if (_leftSidePanel == null || _rightSidePanel == null || _gameplayHudOverlay == null)
        {
            return;
        }

        _leftSidePanel.Clear();
        _rightSidePanel.Clear();

        _leftSidePanel.AddWidget(_gameplayHudOverlay.BuildAiActivityContent());
        _rightSidePanel.AddWidget(_gameplayHudOverlay.BuildSelectionContent());
        _rightSidePanel.AddWidget(_gameplayHudOverlay.BuildLegendContent());

        _gameplayHudOverlay.AiActivityPanel.Visible = false;
        _gameplayHudOverlay.SelectionPanel.Visible = false;
        _gameplayHudOverlay.LegendPanel.Visible = false;

        System.Diagnostics.Debug.WriteLine($"[UI] Left panel: {_leftSidePanel.Width}px, Right panel: {_rightSidePanel.Width}px");
    }

    private void DetachInGameWindows()
    {
        if (_inGameDesktop == null)
        {
            return;
        }

        if (_leftSidePanel != null)
        {
            _inGameDesktop.Widgets.Remove(_leftSidePanel.Container);
        }

        if (_rightSidePanel != null)
        {
            _inGameDesktop.Widgets.Remove(_rightSidePanel.Container);
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

        if (_tutorialModeWindow != null)
        {
            _inGameDesktop.Widgets.Remove(_tutorialModeWindow.Window);
        }

        if (_continentZoomWindow != null)
        {
            _inGameDesktop.Widgets.Remove(_continentZoomWindow.Window);
        }
    }

    private void AttachInGameWindows()
    {
        if (_inGameDesktop == null)
        {
            return;
        }

        _inGameDesktop.Root = null;

        int topBarHeight = _gameplayHudOverlay?.GetTopBarHeight() ?? 80;
        int panelTopOffset = topBarHeight + ThemeManager.ScalePixels(12);

        if (_leftSidePanel != null || _rightSidePanel != null)
        {
            AttachDesktopWidget(_leftSidePanel?.Container);
            AttachDesktopWidget(_rightSidePanel?.Container);

            if (_leftSidePanel != null)
            {
                _leftSidePanel.Container.Visible = true;
                _leftSidePanel.UpdatePosition(_graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight, panelTopOffset);
                System.Diagnostics.Debug.WriteLine($"[UI] Left panel attached, top={_leftSidePanel.Container.Top}, visible: {_leftSidePanel.Container.Visible}");
            }
            if (_rightSidePanel != null)
            {
                _rightSidePanel.Container.Visible = true;
                _rightSidePanel.UpdatePosition(_graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight, panelTopOffset);
                System.Diagnostics.Debug.WriteLine($"[UI] Right panel attached at Left={_rightSidePanel.Container.Left}, Top={_rightSidePanel.Container.Top}, visible: {_rightSidePanel.Container.Visible}");
            }

            AnchorTutorialWindowToMapLeft();
            AttachDesktopWidget(_gameplayHudOverlay?.TopBar);
            AttachDesktopWidget(_gameplayHudOverlay?.HelpPanel);
            HideStandardWorkspaceWindows();
            AttachDockableWorkspaceWindows();
            AttachStatusAndCombatOverlays();
            return;
        }

        AttachDesktopWidget(_gameplayHudOverlay?.TopBar);
        AttachDesktopWidget(_gameplayHudOverlay?.HelpPanel);

        if (_gameplayHudOverlay?.AiActivityPanel != null)
        {
            _gameplayHudOverlay.AiActivityPanel.Top = topBarHeight + ThemeManager.ScalePixels(12);
            _gameplayHudOverlay.AiActivityPanel.Left = ThemeManager.ScalePixels(12);
            _gameplayHudOverlay.AiActivityPanel.Visible = true;
        }
        if (_gameplayHudOverlay?.SelectionPanel != null)
        {
            _gameplayHudOverlay.SelectionPanel.Top = topBarHeight + ThemeManager.ScalePixels(12);
            _gameplayHudOverlay.SelectionPanel.Visible = true;
        }
        if (_gameplayHudOverlay?.LegendPanel != null)
        {
            _gameplayHudOverlay.LegendPanel.Top = topBarHeight + ThemeManager.ScalePixels(12);
            _gameplayHudOverlay.LegendPanel.Visible = true;
        }
        AttachDesktopWidget(_gameplayHudOverlay?.AiActivityPanel);
        AttachDesktopWidget(_gameplayHudOverlay?.SelectionPanel);
        AttachDesktopWidget(_gameplayHudOverlay?.LegendPanel);

        if (_playerDashboardWindow != null)
        {
            _playerDashboardWindow.Window.Visible = false;
        }
        if (_aiVisualizationWindow != null)
        {
            _aiVisualizationWindow.Window.Visible = false;
        }
        if (_debugInfoWindow != null)
        {
            _debugInfoWindow.Window.Visible = false;
        }
        if (_uiScaleWindow != null)
        {
            _uiScaleWindow.Window.Visible = false;
        }
        if (_encyclopediaWindow != null)
        {
            _encyclopediaWindow.Window.Visible = false;
        }
        AttachDockableWorkspaceWindows();

        AnchorTutorialWindowToMapLeft();
        AttachStatusAndCombatOverlays();
    }

    private void AnchorTutorialWindowToMapLeft()
    {
        if (_tutorialModeWindow == null)
        {
            return;
        }

        int screenWidth = _graphics.PreferredBackBufferWidth;
        int screenHeight = _graphics.PreferredBackBufferHeight;
        int topBarHeight = _gameplayHudOverlay?.GetTopBarHeight() ?? ThemeManager.ScalePixels(80);
        int mapTop = topBarHeight + ThemeManager.ScalePixels(12);
        int leftDockRight = 0;
        int rightDockLeft = screenWidth;

        if (_leftSidePanel != null)
        {
            leftDockRight = _leftSidePanel.Container.Left + (_leftSidePanel.Container.Width ?? _leftSidePanel.Width);
        }

        if (_rightSidePanel != null)
        {
            rightDockLeft = _rightSidePanel.Container.Left;
        }

        TutorialModeWindowAnchor.Apply(
            _tutorialModeWindow.Window,
            screenWidth,
            screenHeight,
            leftDockRight,
            rightDockLeft,
            mapTop,
            ThemeManager.ScalePixels(520),
            ThemeManager.ScalePixels(640));
    }

    private void AttachDockableWorkspaceWindows()
    {
        AttachDesktopWidget(_playerDashboardWindow?.Window);
        AttachDesktopWidget(_aiVisualizationWindow?.Window);
        AttachDesktopWidget(_debugInfoWindow?.Window);
        AttachDesktopWidget(_uiScaleWindow?.Window);
        AttachDesktopWidget(_encyclopediaWindow?.Window);
        AttachDesktopWidget(_tutorialModeWindow?.Window);
        AttachDesktopWidget(_continentZoomWindow?.Window);
    }

    private void HideStandardWorkspaceWindows()
    {
        if (_playerDashboardWindow != null)
        {
            _playerDashboardWindow.Window.Visible = false;
        }
        if (_aiVisualizationWindow != null)
        {
            _aiVisualizationWindow.Window.Visible = false;
        }
        if (_debugInfoWindow != null)
        {
            _debugInfoWindow.Window.Visible = false;
        }
        if (_uiScaleWindow != null)
        {
            _uiScaleWindow.Window.Visible = false;
        }
        if (_encyclopediaWindow != null)
        {
            _encyclopediaWindow.Window.Visible = false;
        }
    }

    private void AttachStatusAndCombatOverlays()
    {
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
        if (_tutorialModeWindow == null || (!_tutorialModeActive && !_tutorialModeWindow.IsVisible))
        {
            return;
        }

        AnchorTutorialWindowToMapLeft();
        _tutorialModeWindow.UpdateContent(new TutorialModeSnapshot(
            _gameStateCache,
            _currentPlayerId,
            _inputController?.Selection,
            _inputController?.ShowHelp == true,
            _playerDashboardWindow?.IsVisible == true,
            _encyclopediaWindow?.IsVisible == true,
            _contextMenuManager?.IsMenuOpen == true,
            _combatScreen?.IsActive == true));
    }

    private void EndTutorialMode()
    {
        bool wasTutorialModeActive = _tutorialModeActive;
        _tutorialModeActive = false;
        _tutorialModeWindow?.Hide();
        if (wasTutorialModeActive)
        {
            GameFeedbackBus.PublishSuccess("Tutorial mode ended", "Free play continues in the current single-player session.");
        }
    }

    private void OnContinentZoomRequested(object? sender, StellarBodyData body)
    {
        var starSystem = FindStarSystemForBody(body);
        _continentZoomWindow?.Show(body, starSystem);
        GameFeedbackBus.PublishInfo("Planet zoom opened", $"Select a continent on {body.Name}.");
    }

    private void OnContinentZoomRegionSelected(RegionData region)
    {
        _inputController?.Selection.SelectRegion(region);
        GameFeedbackBus.PublishInfo("Continent selected", region.Name);
    }

    private StarSystemData? FindStarSystemForBody(StellarBodyData body)
    {
        return _mapData?.StarSystems.FirstOrDefault(system =>
            system.StellarBodies.Any(candidate => string.Equals(candidate.Id, body.Id, StringComparison.OrdinalIgnoreCase)));
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
            _mapData,
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
            _tutorialModeWindow?.IsVisible == true);
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
            PersistWindowDisplaySettings();

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

            _continentZoomRenderer?.Dispose();
        }
        base.Dispose(disposing);
    }
}




