using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RiskyStars.Shared;
using Myra;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using System.Diagnostics.CodeAnalysis;

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
    private const string ClientDebugLiveProcessCoverageJustification =
        "Exercised by ClientDebugGameScreenshotIntegrationTests through a launched MonoGame process; coverlet does not attach to that child process.";

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
    private ClientDebugCommandQueue? _clientDebugCommandQueue;
    private ClientDebugGrpcHost? _clientDebugGrpcHost;
    
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
        StartClientDebugProtocol();
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
        DrainClientDebugCommands();
        
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

        int panelTopOffset = GetContentTopAfterTopBar(ThemeManager.ScalePixels(12));
        System.Diagnostics.Debug.WriteLine($"[UI] Resizing panels: size={width}x{height}, panelTop={panelTopOffset}");
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
        DrawTutorialHighlights(spriteBatch: _spriteBatch);
        DrawDebugVisualTreeHighlight(spriteBatch: _spriteBatch);
        _settingsWindow?.Render();
    }

    private void DrawTutorialHighlights(SpriteBatch? spriteBatch)
    {
        if (spriteBatch == null ||
            _pixelTexture == null ||
            _tutorialModeWindow?.IsVisible != true)
        {
            return;
        }

        IReadOnlyList<TutorialHighlightBounds> highlights = BuildTutorialHighlightBounds();
        if (highlights.Count == 0)
        {
            return;
        }

        PrepareBackBufferDrawState();
        TutorialHighlightRenderer.Draw(
            spriteBatch,
            _pixelTexture,
            highlights,
            _graphics.PreferredBackBufferWidth,
            _graphics.PreferredBackBufferHeight);
    }

    private void DrawDebugVisualTreeHighlight(SpriteBatch? spriteBatch)
    {
        if (spriteBatch == null ||
            _pixelTexture == null ||
            _debugInfoWindow?.IsVisible != true ||
            string.IsNullOrWhiteSpace(_debugInfoWindow.SelectedVisualElementId))
        {
            return;
        }

        var visualTree = BuildGameUiVisualTree();
        if (!visualTree.TryResolveBounds(_debugInfoWindow.SelectedVisualElementId, out Rectangle bounds))
        {
            return;
        }

        PrepareBackBufferDrawState();
        GameUiDebugHighlightRenderer.Draw(
            spriteBatch,
            _pixelTexture,
            bounds,
            _graphics.PreferredBackBufferWidth,
            _graphics.PreferredBackBufferHeight);
    }

    private IReadOnlyList<TutorialHighlightBounds> BuildTutorialHighlightBounds()
    {
        if (_tutorialModeWindow?.IsVisible != true)
        {
            return [];
        }

        var visualTree = BuildGameUiVisualTree();

        return TutorialHighlightBoundsResolver.Resolve(
            _tutorialModeWindow.CurrentHighlightTargets,
            TutorialHighlightTargets.ResolveVisualBounds(_tutorialModeWindow.CurrentHighlightTargets, visualTree));
    }

    private GameUiVisualTree BuildGameUiVisualTree()
    {
        var visualTree = new GameUiVisualTree();
        visualTree.AddXnaElement(
            GameUiVisualElementIds.BackBuffer,
            new Rectangle(0, 0, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight),
            "BackBuffer");
        visualTree.AddXnaElement(GameUiVisualElementIds.MapViewport, GetMapViewportBounds(), "MapViewport", GameUiVisualElementIds.BackBuffer);
        Desktop? activeDesktop = _gameState == GameState.MainMenu
            ? _mainMenu?.DebugDesktop
            : _inGameDesktop;
        string? myraDesktopParentId = null;
        if (activeDesktop != null)
        {
            visualTree.AddMyraRoot(
                GameUiVisualElementIds.MyraDesktop,
                new Rectangle(0, 0, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight));
            myraDesktopParentId = GameUiVisualElementIds.MyraDesktop;
        }

        visualTree.AddMyraElement(GameUiVisualElementIds.TopBar, _gameplayHudOverlay?.TopBar, myraDesktopParentId);
        visualTree.AddMyraElement(GameUiVisualElementIds.HelpPanel, _gameplayHudOverlay?.HelpPanel, myraDesktopParentId);
        visualTree.AddMyraElement(GameUiVisualElementIds.SelectionPanel, _gameplayHudOverlay?.SelectionPanel, myraDesktopParentId);
        visualTree.AddMyraElement(GameUiVisualElementIds.PlayerDashboard, _playerDashboardWindow?.Window, myraDesktopParentId);
        visualTree.AddMyraElement(GameUiVisualElementIds.EncyclopediaWindow, _encyclopediaWindow?.Window, myraDesktopParentId);
        visualTree.AddMyraElement(GameUiVisualElementIds.ContinentZoomWindow, _continentZoomWindow?.Window, myraDesktopParentId);
        Rectangle mapTargetBounds = BuildMapSelectionTargetBounds();
        visualTree.AddXnaElement(GameUiVisualElementIds.MapSelectionTarget, mapTargetBounds, "MapSelectionTarget", GameUiVisualElementIds.MapViewport);
        if (_continentZoomWindow?.IsVisible == true)
        {
            visualTree.AddXnaElement(
                GameUiVisualElementIds.ContinentZoomSurface,
                _continentZoomWindow.CanvasBounds,
                "ContinentZoomSurface",
                GameUiVisualElementIds.ContinentZoomWindow);
        }

        if (_combatScreen?.IsActive == true)
        {
            visualTree.AddXnaElement(
                GameUiVisualElementIds.CombatOverlay,
                new Rectangle(0, 0, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight),
                "CombatOverlay",
                GameUiVisualElementIds.BackBuffer);
        }

        if (activeDesktop != null)
        {
            IEnumerable<Widget> activeRoots = activeDesktop.Root != null
                ? [activeDesktop.Root]
                : activeDesktop.Widgets;
            visualTree.AddMyraTree(GameUiVisualElementIds.MyraDesktop, activeRoots);
        }

        return visualTree;
    }

    private GameUiScaleContext BuildGameUiScaleContext()
    {
        Rectangle clientBounds = Window.ClientBounds;
        return GameUiScaleContext.Create(
            _graphics.PreferredBackBufferWidth,
            _graphics.PreferredBackBufferHeight,
            clientBounds.Width,
            clientBounds.Height,
            ThemeManager.CurrentUiScalePercent,
            ThemeManager.CurrentUiScaleFactor);
    }

    private void StartClientDebugProtocol()
    {
        _clientDebugCommandQueue = new ClientDebugCommandQueue();
        _clientDebugGrpcHost = new ClientDebugGrpcHost(_clientDebugCommandQueue);

        try
        {
            _clientDebugGrpcHost.StartAsync().GetAwaiter().GetResult();
            System.Console.WriteLine($"Client debug gRPC listening on {_clientDebugGrpcHost.ServerUrl}");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Client debug gRPC disabled: {ex.Message}");
            _clientDebugGrpcHost = null;
        }
    }

    private void DrainClientDebugCommands()
    {
        _clientDebugCommandQueue?.Drain(CreateClientDebugController());
    }

    private ClientDebugController CreateClientDebugController()
    {
        return new ClientDebugController(
            BuildGameUiVisualTree,
            BuildGameUiScaleContext,
            FocusClientDebugElement,
            NavigateClientDebugScreen,
            GetDebugTutorialState,
            SeedDebugTutorialScenario,
            InvokeDebugTutorialAction);
    }

    [ExcludeFromCodeCoverage(Justification = ClientDebugLiveProcessCoverageJustification)]
    private ClientDebugActionResult NavigateClientDebugScreen(string screenId)
    {
        string normalizedScreenId = screenId.Trim().ToLowerInvariant();

        try
        {
            switch (normalizedScreenId)
            {
                case "main-menu":
                    ShowDebugMainMenu(MainMenuState.Main);
                    break;
                case "main-menu-settings":
                    ShowDebugMainMenu(MainMenuState.Settings);
                    break;
                case "main-menu-connecting":
                    ShowDebugMainMenu(MainMenuState.Connecting);
                    break;
                case "game-mode-selector":
                    ShowDebugLobby(LobbyState.ModeSelection);
                    break;
                case "connection-screen":
                    ShowDebugLobby(LobbyState.Connection);
                    break;
                case "lobby-browser":
                    ShowDebugLobby(LobbyState.Browser);
                    break;
                case "create-lobby":
                    ShowDebugLobby(LobbyState.CreateLobby);
                    break;
                case "multiplayer-lobby":
                    ShowDebugLobby(LobbyState.InLobby);
                    break;
                case "single-player-lobby":
                    ShowDebugLobby(LobbyState.SinglePlayerLobby);
                    break;
                case "gameplay-hud-top-bar":
                case "gameplay-hud-legend":
                case "side-panel-container":
                    ShowDebugInGame();
                    break;
                case "settings-window":
                    ShowDebugInGame();
                    _settingsWindow?.Open();
                    break;
                case "debug-info-window":
                    ShowDebugInGame();
                    _debugInfoWindow?.Show();
                    break;
                case "player-dashboard-window":
                case "legacy-player-dashboard":
                    ShowDebugInGame();
                    _playerDashboardWindow?.Show();
                    break;
                case "ai-visualization-window":
                    ShowDebugInGame();
                    _aiVisualizationWindow?.Show();
                    _aiVisualizationWindow?.UpdateAIStatus("Drill Marshal Vega", true);
                    break;
                case "encyclopedia-window":
                    ShowDebugInGame();
                    _encyclopediaWindow?.Show();
                    break;
                case "ui-scale-window":
                    ShowDebugInGame();
                    _uiScaleWindow?.Show();
                    break;
                case "tutorial-mode-window":
                    ShowDebugInGame(tutorialMode: true);
                    _tutorialModeWindow?.Show();
                    AnchorTutorialWindowToMapLeft();
                    UpdateTutorialWindow();
                    break;
                case "continent-zoom-window":
                    ShowDebugInGame();
                    ShowDebugContinentZoom();
                    break;
                case "combat-hud-overlay":
                    ShowDebugInGame();
                    _combatScreen?.StartCombat(CreateDebugCombatEvent());
                    UpdateCombatHud();
                    break;
                case "server-status-indicator":
                    ShowDebugInGame(showServerStatus: true);
                    break;
                case "dialog-manager":
                    ShowDebugInGame();
                    _inGameDialogManager?.ShowConfirmation("Confirm Title", "Confirm Message");
                    break;
                case "combat-event-dialog":
                    ShowDebugInGame();
                    _combatEventDialog?.ShowCombatInitiated(CreateDebugCombatEvent());
                    break;
                case "context-menu-manager":
                    ShowDebugInGame();
                    ShowDebugContextMenu();
                    break;
                case "combat-screen":
                    ShowDebugInGame();
                    _combatScreen?.StartCombat(CreateDebugCombatEvent());
                    break;
                case "ai-action-indicator":
                    ShowDebugInGame();
                    _aiActionIndicator?.StartAIThinking("Drill Marshal Vega");
                    _aiActionIndicator?.ShowArmyMovement(Vector2.Zero, Vector2.One, 2, Color.LightGreen, "debug-army");
                    _aiActionIndicator?.AddLogEntry("AI acted", Color.White);
                    break;
                default:
                    return ClientDebugActionResult.Fail($"Screen '{screenId}' is not registered for debug navigation.");
            }

            return ClientDebugActionResult.Ok($"Navigated to debug screen '{normalizedScreenId}'.");
        }
        catch (Exception ex)
        {
            return ClientDebugActionResult.Fail($"Failed to navigate to debug screen '{screenId}': {ex.Message}");
        }
    }

    [ExcludeFromCodeCoverage(Justification = ClientDebugLiveProcessCoverageJustification)]
    private void ShowDebugMainMenu(MainMenuState state)
    {
        _gameState = GameState.MainMenu;
        _mainMenu ??= new MainMenu(GraphicsDevice, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight, _settings);
        if (_defaultFont != null)
        {
            _mainMenu.LoadContent(_defaultFont);
        }

        _mainMenu.SetState(state);
        _mainMenu.ResetNavigationRequests();
    }

    [ExcludeFromCodeCoverage(Justification = ClientDebugLiveProcessCoverageJustification)]
    private void ShowDebugLobby(LobbyState state)
    {
        _gameState = GameState.Lobby;
        _lobbyManager ??= new LobbyManager(GraphicsDevice, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
        if (_defaultFont != null)
        {
            _lobbyManager.LoadContent(_defaultFont);
        }

        _lobbyManager.DebugShowState(state);
    }

    [ExcludeFromCodeCoverage(Justification = ClientDebugLiveProcessCoverageJustification)]
    private void ShowDebugInGame(bool tutorialMode = false, bool showServerStatus = false)
    {
        _gameState = GameState.InGame;
        _tutorialModeActive = tutorialMode;
        _currentPlayerId = "debug-player";
        _mapData ??= MapLoader.CreateSampleMap();
        _camera ??= CreateConfiguredCamera();
        _camera.SetView(Vector2.Zero, 1f);

        SeedDebugGameState();
        _inputController = new InputController(null!, _gameStateCache!, _mapData, _camera);
        _inputController.ContinentZoomRequested -= OnContinentZoomRequested;
        _inputController.ContinentZoomRequested += OnContinentZoomRequested;
        _inputController.SetCurrentPlayer(_currentPlayerId);

        CreateInGameWindows(null!);
        _serverStatusIndicator = showServerStatus ? new ServerStatusIndicator(500) : null;

        if (_inGameDesktop != null)
        {
            _contextMenuManager = new ContextMenuManager(null!, _gameStateCache!, _mapData, _camera, _inGameDesktop);
            _contextMenuManager.SetCurrentPlayer(_currentPlayerId);
            _inputController.SetContextMenuManager(_contextMenuManager);
        }

        AttachInGameWindows();
        HideDebugInGameSurfaces();

        if (tutorialMode)
        {
            _tutorialModeWindow?.Show();
        }

        if (showServerStatus && _serverStatusIndicator != null)
        {
            _serverStatusIndicator.Container.Visible = true;
            _serverStatusIndicator.Update();
        }

        UpdateGameplayHud();
        UpdateDebugInfo(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0 / 60.0)));
    }

    [ExcludeFromCodeCoverage(Justification = ClientDebugLiveProcessCoverageJustification)]
    private void HideDebugInGameSurfaces()
    {
        _contextMenuManager?.CloseContextMenu();
        _inGameDialogManager?.CloseDialog();
        _combatEventDialog?.CloseDialog();
        _settingsWindow?.Close();
        _combatScreen?.Close();
        _combatHudOverlay?.Update(null);

        _playerDashboardWindow?.Hide();
        _aiVisualizationWindow?.Hide();
        _debugInfoWindow?.Hide();
        _uiScaleWindow?.Hide();
        _encyclopediaWindow?.Hide();
        _tutorialModeWindow?.Hide();
        _continentZoomWindow?.Hide();

        if (_serverStatusIndicator?.Container != null)
        {
            _serverStatusIndicator.Container.Visible = false;
        }
    }

    [ExcludeFromCodeCoverage(Justification = ClientDebugLiveProcessCoverageJustification)]
    private void SeedDebugGameState(
        TurnPhase phase = TurnPhase.Purchase,
        int ownArmyCount = 1,
        bool ownArmyMoved = false)
    {
        _gameStateCache ??= new GameStateCache();
        _gameStateCache.Clear();
        if (_mapData == null)
        {
            return;
        }

        RegionData? firstRegion = _mapData.StarSystems
            .SelectMany(system => system.StellarBodies)
            .SelectMany(body => body.Regions)
            .FirstOrDefault();
        RegionData? secondRegion = _mapData.StarSystems
            .SelectMany(system => system.StellarBodies)
            .SelectMany(body => body.Regions)
            .Skip(1)
            .FirstOrDefault();

        var update = new GameUpdate
        {
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            GameId = "debug-game",
            GameState = new TurnBasedGameStateUpdate
            {
                GameId = "debug-game",
                TurnNumber = 1,
                CurrentPhase = phase,
                CurrentPlayerId = _currentPlayerId ?? "debug-player",
                EventMessage = "Debug protocol screen validation"
            }
        };

        update.GameState.PlayerStates.Add(new PlayerState
        {
            PlayerId = "debug-player",
            PlayerName = "Cadet",
            PopulationStockpile = 100,
            MetalStockpile = 50,
            FuelStockpile = 50,
            TurnOrder = 1
        });
        update.GameState.PlayerStates.Add(new PlayerState
        {
            PlayerId = "debug-ai",
            PlayerName = "Drill Marshal Vega",
            PopulationStockpile = 80,
            MetalStockpile = 35,
            FuelStockpile = 30,
            TurnOrder = 2
        });

        if (firstRegion != null)
        {
            update.GameState.RegionOwnerships.Add(new RegionOwnership
            {
                RegionId = firstRegion.Id,
                OwnerId = "debug-player"
            });
            int safeOwnArmyCount = Math.Max(1, ownArmyCount);
            for (int i = 0; i < safeOwnArmyCount; i++)
            {
                update.GameState.ArmyStates.Add(new ArmyState
                {
                    ArmyId = i == 0 ? "debug-army" : $"debug-army-{i + 1}",
                    OwnerId = "debug-player",
                    UnitCount = 4,
                    LocationId = firstRegion.Id,
                    LocationType = LocationType.Region,
                    HasMovedThisTurn = ownArmyMoved && i == 0
                });
            }
        }

        if (secondRegion != null)
        {
            update.GameState.RegionOwnerships.Add(new RegionOwnership
            {
                RegionId = secondRegion.Id,
                OwnerId = "debug-ai"
            });
            update.GameState.ArmyStates.Add(new ArmyState
            {
                ArmyId = "debug-ai-army",
                OwnerId = "debug-ai",
                UnitCount = 3,
                LocationId = secondRegion.Id,
                LocationType = LocationType.Region
            });
        }

        foreach (var lane in _mapData.HyperspaceLanes)
        {
            update.GameState.HyperspaceLaneMouthOwnerships.Add(new HyperspaceLaneMouthOwnership
            {
                HyperspaceLaneMouthId = lane.MouthAId,
                OwnerId = "debug-player"
            });
            update.GameState.HyperspaceLaneMouthOwnerships.Add(new HyperspaceLaneMouthOwnership
            {
                HyperspaceLaneMouthId = lane.MouthBId,
                OwnerId = "debug-ai"
            });
        }

        _gameStateCache.ApplyUpdate(update);
    }

    [ExcludeFromCodeCoverage(Justification = ClientDebugLiveProcessCoverageJustification)]
    private ClientDebugActionResult SeedDebugTutorialScenario()
    {
        ShowDebugInGame(tutorialMode: true);
        SeedDebugGameState(TurnPhase.Production, ownArmyCount: 1, ownArmyMoved: false);

        _inputController?.Selection.ClearSelection();
        _inputController?.DebugSetHelpVisible(false);
        _contextMenuManager?.CloseContextMenu();
        _playerDashboardWindow?.Hide();
        _encyclopediaWindow?.Hide();
        _tutorialModeWindow?.DebugReset(requireExplicitActions: true);
        _tutorialModeWindow?.Show();
        AnchorTutorialWindowToMapLeft();
        UpdateGameplayHud();
        UpdateTutorialWindow();

        return ClientDebugActionResult.Ok("Seeded deterministic guided tutorial scenario.");
    }

    [ExcludeFromCodeCoverage(Justification = ClientDebugLiveProcessCoverageJustification)]
    private ClientDebugActionResult InvokeDebugTutorialAction(string expectedStepId, string action)
    {
        if (_tutorialModeWindow?.IsVisible != true)
        {
            return ClientDebugActionResult.Fail("Guided tutorial is not visible. Call SeedTutorialScenario first.");
        }

        string currentStepId = _tutorialModeWindow.CurrentStep.Id;
        if (!string.Equals(currentStepId, expectedStepId, StringComparison.OrdinalIgnoreCase))
        {
            return ClientDebugActionResult.Fail(
                $"Expected tutorial step '{expectedStepId}' but current step is '{currentStepId}'.");
        }

        string normalizedAction = NormalizeDebugTutorialAction(action);
        if (normalizedAction == "click-next")
        {
            if (!_tutorialModeWindow.IsCurrentStepComplete &&
                _tutorialModeWindow.CurrentStep.Completion != TutorialStepCompletion.Manual)
            {
                return ClientDebugActionResult.Fail(
                    $"Cannot advance step '{currentStepId}' because its objective is not complete.");
            }

            _tutorialModeWindow.DebugMoveNext();
            UpdateGameplayHud();
            UpdateTutorialWindow();
            return ClientDebugActionResult.Ok($"Advanced from tutorial step '{currentStepId}'.");
        }

        string expectedAction = GetExpectedDebugTutorialAction(currentStepId);
        if (!string.Equals(normalizedAction, expectedAction, StringComparison.OrdinalIgnoreCase))
        {
            return ClientDebugActionResult.Fail(
                $"Action '{action}' is not valid for tutorial step '{currentStepId}'. Expected '{expectedAction}'.");
        }

        ClientDebugActionResult actionResult = ApplyDebugTutorialAction(currentStepId, normalizedAction);
        if (!actionResult.Success)
        {
            return actionResult;
        }

        UpdateGameplayHud();
        UpdateTutorialWindow();

        if (currentStepId != "complete" && !_tutorialModeWindow.IsCurrentStepObjectiveSatisfied)
        {
            return ClientDebugActionResult.Fail(
                $"Action '{normalizedAction}' did not complete tutorial step '{currentStepId}'.");
        }

        if (currentStepId == "complete")
        {
            return ClientDebugActionResult.Ok("Finished guided tutorial and left gameplay active.");
        }

        _tutorialModeWindow.DebugCompleteCurrentStep();
        return ClientDebugActionResult.Ok($"Completed tutorial step '{currentStepId}' with action '{normalizedAction}'.");
    }

    [ExcludeFromCodeCoverage(Justification = ClientDebugLiveProcessCoverageJustification)]
    private ClientDebugActionResult ApplyDebugTutorialAction(string currentStepId, string normalizedAction)
    {
        switch (currentStepId)
        {
            case "sync":
                SeedDebugGameState(TurnPhase.Production, GetOwnDebugArmyCount(), HasMovedDebugArmy());
                return ClientDebugActionResult.Ok("World snapshot is available.");
            case "turn":
                SeedDebugGameState(_gameStateCache?.GetCurrentPhase() ?? TurnPhase.Production, GetOwnDebugArmyCount(), HasMovedDebugArmy());
                return ClientDebugActionResult.Ok("Active player is the tutorial player.");
            case "select":
                return SelectDebugMapTarget();
            case "help":
                _inputController?.DebugSetHelpVisible(true);
                return ClientDebugActionResult.Ok("Shortcut sheet is visible.");
            case "production":
                SeedDebugGameState(TurnPhase.Purchase, GetOwnDebugArmyCount(), HasMovedDebugArmy());
                return ClientDebugActionResult.Ok("Advanced to purchase phase.");
            case "dashboard":
                _playerDashboardWindow?.Show();
                return ClientDebugActionResult.Ok("Player dashboard is visible.");
            case "purchase":
                _playerDashboardWindow?.Show();
                SeedDebugGameState(TurnPhase.Purchase, Math.Max(2, GetOwnDebugArmyCount() + 1), HasMovedDebugArmy());
                return ClientDebugActionResult.Ok("Purchased a starter army.");
            case "reinforcement-phase":
                SeedDebugGameState(TurnPhase.Reinforcement, GetOwnDebugArmyCount(), HasMovedDebugArmy());
                _playerDashboardWindow?.Hide();
                return ClientDebugActionResult.Ok("Advanced to reinforcement phase.");
            case "reinforcement-target":
                return SelectDebugOwnedReinforcementTarget();
            case "movement-phase":
                SeedDebugGameState(TurnPhase.Movement, GetOwnDebugArmyCount(), HasMovedDebugArmy());
                return ClientDebugActionResult.Ok("Advanced to movement phase.");
            case "army":
                return SelectDebugOwnArmy();
            case "movement":
                SeedDebugGameState(TurnPhase.Movement, GetOwnDebugArmyCount(), ownArmyMoved: true);
                return SelectDebugOwnArmy();
            case "reference":
                _encyclopediaWindow?.Show();
                return ClientDebugActionResult.Ok("Encyclopedia is visible.");
            case "complete":
                EndTutorialMode();
                return ClientDebugActionResult.Ok("Guided tutorial closed.");
            default:
                return ClientDebugActionResult.Fail($"Tutorial step '{currentStepId}' has no registered debug action.");
        }
    }

    private static string NormalizeDebugTutorialAction(string action)
    {
        return action.Trim().ToLowerInvariant().Replace('_', '-').Replace(' ', '-');
    }

    internal static string GetExpectedDebugTutorialAction(string stepId)
    {
        return stepId switch
        {
            "sync" => "wait-world-sync",
            "turn" => "confirm-turn-banner",
            "select" => "select-map-target",
            "help" => "toggle-help",
            "production" => "produce-or-advance",
            "dashboard" => "open-dashboard",
            "purchase" => "buy-starter-army",
            "reinforcement-phase" => "advance-reinforcement-phase",
            "reinforcement-target" => "select-reinforcement-target",
            "movement-phase" => "advance-movement-phase",
            "army" => "select-own-army",
            "movement" => "issue-or-inspect-movement",
            "reference" => "open-encyclopedia",
            "complete" => "finish-tutorial",
            _ => string.Empty
        };
    }

    [ExcludeFromCodeCoverage(Justification = ClientDebugLiveProcessCoverageJustification)]
    private ClientDebugActionResult SelectDebugMapTarget()
    {
        RegionData? region = GetFirstDebugRegion();
        if (region == null)
        {
            return ClientDebugActionResult.Fail("No deterministic map region is available.");
        }

        _inputController?.Selection.SelectRegion(region);
        return ClientDebugActionResult.Ok($"Selected map region '{region.Name}'.");
    }

    [ExcludeFromCodeCoverage(Justification = ClientDebugLiveProcessCoverageJustification)]
    private ClientDebugActionResult SelectDebugOwnedReinforcementTarget()
    {
        var ownedMouth = _mapData?.HyperspaceLanes
            .Select(lane => (Id: lane.MouthAId, Position: lane.MouthAPosition))
            .FirstOrDefault(mouth =>
                _gameStateCache?.GetHyperspaceLaneMouthOwnership(mouth.Id)?.OwnerId == (_currentPlayerId ?? "debug-player"));
        if (!string.IsNullOrWhiteSpace(ownedMouth?.Id))
        {
            _inputController?.Selection.SelectHyperspaceLaneMouth(ownedMouth.Value.Id, ownedMouth.Value.Position);
            return ClientDebugActionResult.Ok($"Selected owned reinforcement lane mouth '{ownedMouth.Value.Id}'.");
        }

        RegionData? region = GetFirstDebugRegion();
        if (region == null)
        {
            return ClientDebugActionResult.Fail("No deterministic owned reinforcement target is available.");
        }

        _inputController?.Selection.SelectRegion(region);
        return ClientDebugActionResult.Ok($"Selected owned reinforcement target '{region.Name}'.");
    }

    [ExcludeFromCodeCoverage(Justification = ClientDebugLiveProcessCoverageJustification)]
    private ClientDebugActionResult SelectDebugOwnArmy()
    {
        ArmyState? army = _gameStateCache?
            .GetArmiesOwnedByPlayer(_currentPlayerId ?? "debug-player")
            .FirstOrDefault();
        if (army == null)
        {
            return ClientDebugActionResult.Fail("No deterministic own army is available.");
        }

        _inputController?.Selection.SelectArmy(army);
        return ClientDebugActionResult.Ok($"Selected own army '{army.ArmyId}'.");
    }

    [ExcludeFromCodeCoverage(Justification = ClientDebugLiveProcessCoverageJustification)]
    private RegionData? GetFirstDebugRegion()
    {
        return _mapData?.StarSystems
            .SelectMany(system => system.StellarBodies)
            .SelectMany(body => body.Regions)
            .FirstOrDefault();
    }

    [ExcludeFromCodeCoverage(Justification = ClientDebugLiveProcessCoverageJustification)]
    private int GetOwnDebugArmyCount()
    {
        return _gameStateCache?
            .GetArmiesOwnedByPlayer(_currentPlayerId ?? "debug-player")
            .Count ?? 1;
    }

    [ExcludeFromCodeCoverage(Justification = ClientDebugLiveProcessCoverageJustification)]
    private bool HasMovedDebugArmy()
    {
        return _gameStateCache?
            .GetArmiesOwnedByPlayer(_currentPlayerId ?? "debug-player")
            .Any(army => army.HasMovedThisTurn) == true;
    }

    [ExcludeFromCodeCoverage(Justification = ClientDebugLiveProcessCoverageJustification)]
    private ClientDebugTutorialStateResult GetDebugTutorialState()
    {
        if (_tutorialModeWindow == null)
        {
            return CreateEmptyDebugTutorialState(false, "Guided tutorial window has not been created.");
        }

        UpdateGameplayHud();
        UpdateTutorialWindow();

        TutorialModeStep step = _tutorialModeWindow.CurrentStep;
        IReadOnlyList<TutorialHighlightTarget> targets = _tutorialModeWindow.CurrentHighlightTargets;
        IReadOnlyDictionary<TutorialHighlightTarget, Rectangle> resolvedBounds =
            TutorialHighlightTargets.ResolveVisualBounds(targets, BuildGameUiVisualTree());

        PlayerState? playerState = null;
        if (_gameStateCache != null && !string.IsNullOrWhiteSpace(_currentPlayerId))
        {
            playerState = _gameStateCache.GetPlayerState(_currentPlayerId);
        }

        return new ClientDebugTutorialStateResult(
            Success: true,
            Message: "Guided tutorial state captured.",
            StepIndex: _tutorialModeWindow.CurrentStepIndex,
            StepCount: TutorialModeWindow.AllSteps.Count,
            StepId: step.Id,
            Title: step.Title,
            Objective: step.Objective,
            Status: _tutorialModeWindow.CurrentStatusText,
            NextButtonText: _tutorialModeWindow.NextButtonText,
            IsComplete: _tutorialModeWindow.IsCurrentStepComplete,
            TutorialVisible: _tutorialModeWindow.IsVisible,
            HighlightTargets: targets.Select(target => target.ToString()).ToArray(),
            HighlightBounds: targets
                .Select(target => CreateDebugTutorialHighlightBound(target, resolvedBounds))
                .ToArray(),
            Phase: (_gameStateCache?.GetCurrentPhase() ?? TurnPhase.Production).ToString(),
            CurrentPlayerId: _currentPlayerId,
            SelectionType: (_inputController?.Selection.Type ?? SelectionType.None).ToString(),
            SelectedOwnerId: GetDebugSelectedOwnerId(),
            HelpVisible: _inputController?.ShowHelp == true,
            DashboardVisible: _playerDashboardWindow?.IsVisible == true,
            EncyclopediaVisible: _encyclopediaWindow?.IsVisible == true,
            ContextMenuOpen: _contextMenuManager?.IsMenuOpen == true,
            OwnArmyCount: GetOwnDebugArmyCount(),
            MovedArmyCount: _gameStateCache?
                .GetArmiesOwnedByPlayer(_currentPlayerId ?? "debug-player")
                .Count(army => army.HasMovedThisTurn) ?? 0,
            Population: playerState?.PopulationStockpile ?? 0,
            Metal: playerState?.MetalStockpile ?? 0,
            Fuel: playerState?.FuelStockpile ?? 0);
    }

    private static ClientDebugTutorialHighlightBound CreateDebugTutorialHighlightBound(
        TutorialHighlightTarget target,
        IReadOnlyDictionary<TutorialHighlightTarget, Rectangle> resolvedBounds)
    {
        bool visible = resolvedBounds.TryGetValue(target, out Rectangle bounds) &&
            bounds.Width > 0 &&
            bounds.Height > 0;
        return new ClientDebugTutorialHighlightBound(
            target.ToString(),
            visible ? bounds : Rectangle.Empty,
            visible);
    }

    private static ClientDebugTutorialStateResult CreateEmptyDebugTutorialState(bool success, string message)
    {
        return new ClientDebugTutorialStateResult(
            success,
            message,
            StepIndex: -1,
            StepCount: TutorialModeWindow.AllSteps.Count,
            StepId: string.Empty,
            Title: string.Empty,
            Objective: string.Empty,
            Status: string.Empty,
            NextButtonText: string.Empty,
            IsComplete: false,
            TutorialVisible: false,
            HighlightTargets: [],
            HighlightBounds: [],
            Phase: string.Empty,
            CurrentPlayerId: null,
            SelectionType: SelectionType.None.ToString(),
            SelectedOwnerId: null,
            HelpVisible: false,
            DashboardVisible: false,
            EncyclopediaVisible: false,
            ContextMenuOpen: false,
            OwnArmyCount: 0,
            MovedArmyCount: 0,
            Population: 0,
            Metal: 0,
            Fuel: 0);
    }

    [ExcludeFromCodeCoverage(Justification = ClientDebugLiveProcessCoverageJustification)]
    private string? GetDebugSelectedOwnerId()
    {
        SelectionState? selection = _inputController?.Selection;
        return selection?.Type switch
        {
            SelectionType.Army => selection.SelectedArmy?.OwnerId,
            SelectionType.Region when selection.SelectedRegion != null =>
                _gameStateCache?.GetRegionOwnership(selection.SelectedRegion.Id)?.OwnerId,
            SelectionType.HyperspaceLaneMouth when !string.IsNullOrWhiteSpace(selection.SelectedHyperspaceLaneMouthId) =>
                _gameStateCache?.GetHyperspaceLaneMouthOwnership(selection.SelectedHyperspaceLaneMouthId)?.OwnerId,
            _ => null
        };
    }

    [ExcludeFromCodeCoverage(Justification = ClientDebugLiveProcessCoverageJustification)]
    private void ShowDebugContinentZoom()
    {
        var body = _mapData?.StarSystems
            .SelectMany(system => system.StellarBodies)
            .FirstOrDefault(candidate => candidate.Regions.Count > 1);
        if (body == null)
        {
            return;
        }

        _continentZoomWindow?.Show(body, FindStarSystemForBody(body));
    }

    [ExcludeFromCodeCoverage(Justification = ClientDebugLiveProcessCoverageJustification)]
    private void ShowDebugContextMenu()
    {
        RegionData? region = _mapData?.StarSystems
            .SelectMany(system => system.StellarBodies)
            .SelectMany(body => body.Regions)
            .FirstOrDefault();
        if (region == null || _contextMenuManager == null)
        {
            return;
        }

        var selection = new SelectionState();
        selection.SelectRegion(region);
        _inputController?.Selection.SelectRegion(region);
        _contextMenuManager.OpenContextMenu(new Vector2(360, 240), region.Position, selection);
    }

    [ExcludeFromCodeCoverage(Justification = ClientDebugLiveProcessCoverageJustification)]
    private static CombatEvent CreateDebugCombatEvent()
    {
        return new CombatEvent
        {
            EventId = "debug-combat",
            EventType = CombatEvent.Types.CombatEventType.CombatInitiated,
            LocationId = "debug-region",
            ArmyStates =
            {
                new CombatArmyState
                {
                    ArmyId = "debug-army",
                    PlayerId = "Cadet",
                    CombatRole = "Attacker",
                    UnitCount = 4
                },
                new CombatArmyState
                {
                    ArmyId = "debug-ai-army",
                    PlayerId = "Drill Marshal Vega",
                    CombatRole = "Defender",
                    UnitCount = 3
                }
            },
            RoundResults =
            {
                new CombatRoundResult
                {
                    AttackerRolls =
                    {
                        new DiceRoll { ArmyId = "debug-army", Roll = 6, UnitIndex = 0 }
                    },
                    DefenderRolls =
                    {
                        new DiceRoll { ArmyId = "debug-ai-army", Roll = 4, UnitIndex = 0 }
                    },
                    Pairings =
                    {
                        new RollPairing
                        {
                            AttackerRoll = new DiceRoll { ArmyId = "debug-army", Roll = 6, UnitIndex = 0 },
                            DefenderRoll = new DiceRoll { ArmyId = "debug-ai-army", Roll = 4, UnitIndex = 0 },
                            WinnerArmyId = "debug-army"
                        }
                    },
                    Casualties =
                    {
                        new ArmyCasualty
                        {
                            ArmyId = "debug-ai-army",
                            PlayerId = "debug-ai",
                            CombatRole = "Defender",
                            Casualties = 1,
                            RemainingUnits = 2
                        }
                    }
                }
            }
        };
    }

    private ClientDebugActionResult FocusClientDebugElement(string elementId, bool showDebugWindow)
    {
        GameUiVisualTree visualTree = BuildGameUiVisualTree();
        if (!visualTree.TryResolveBounds(elementId, out _))
        {
            return ClientDebugActionResult.Fail($"Element '{elementId}' is not present in the current visual tree.");
        }

        if (_debugInfoWindow == null)
        {
            return ClientDebugActionResult.Fail("Debug information window is not available in the current game state.");
        }

        if (showDebugWindow)
        {
            _debugInfoWindow.Show();
        }

        _debugInfoWindow.SelectVisualElement(elementId);
        return ClientDebugActionResult.Ok($"Focused visual tree element '{elementId}'.");
    }

    private int GetFallbackTopBarHeight()
    {
        return _gameplayHudOverlay?.GetTopBarHeight() ?? ThemeManager.ScalePixels(80);
    }

    private int GetContentTopAfterTopBar(int gap = 0)
    {
        Rectangle measuredTopBarBounds = Rectangle.Empty;
        if (_gameplayHudOverlay?.TopBar != null)
        {
            GameUiWidgetBoundsResolver.TryGetScreenBounds(_gameplayHudOverlay.TopBar, out measuredTopBarBounds);
        }

        return GameUiLayoutMetrics.ResolveContentTop(measuredTopBarBounds, GetFallbackTopBarHeight(), gap);
    }

    private Rectangle BuildMapSelectionTargetBounds()
    {
        if (_mapData == null || _camera == null)
        {
            return Rectangle.Empty;
        }

        Rectangle viewportBounds = GetMapViewportBounds();
        if (viewportBounds.Width <= 0 || viewportBounds.Height <= 0)
        {
            return Rectangle.Empty;
        }

        Matrix worldToScreen = _camera.GetTransformMatrix();
        int minimumRadius = ThemeManager.ScalePixels(TutorialMapHighlightResolver.MinimumReadableScreenRadius);
        int maximumRadius = ThemeManager.ScalePixels(TutorialMapHighlightResolver.MaximumReadableScreenRadius);
        var candidates = new List<Rectangle>();

        foreach (var system in _mapData.StarSystems)
        {
            AddMapTargetCandidate(candidates, system.Position, 80f, worldToScreen, minimumRadius, maximumRadius);

            foreach (var body in system.StellarBodies)
            {
                AddMapTargetCandidate(candidates, body.Position, GetBodyHitRadius(body), worldToScreen, minimumRadius, maximumRadius);

                foreach (var region in body.Regions)
                {
                    AddMapTargetCandidate(candidates, region.Position, 10f, worldToScreen, minimumRadius, maximumRadius);
                }
            }
        }

        foreach (var lane in _mapData.HyperspaceLanes)
        {
            AddMapTargetCandidate(candidates, lane.MouthAPosition, 12f, worldToScreen, minimumRadius, maximumRadius);
            AddMapTargetCandidate(candidates, lane.MouthBPosition, 12f, worldToScreen, minimumRadius, maximumRadius);
        }

        return TutorialMapHighlightResolver.SelectBestTarget(
            candidates,
            viewportBounds,
            GetTutorialWindowBounds());
    }

    private static void AddMapTargetCandidate(
        List<Rectangle> candidates,
        Vector2 worldPosition,
        float worldRadius,
        Matrix worldToScreen,
        int minimumRadius,
        int maximumRadius)
    {
        candidates.Add(TutorialMapHighlightResolver.ToScreenBounds(
            worldPosition,
            worldRadius,
            worldToScreen,
            minimumRadius,
            maximumRadius));
    }

    private static float GetBodyHitRadius(StellarBodyData body)
    {
        return body.Type switch
        {
            StellarBodyType.GasGiant => 20f,
            StellarBodyType.RockyPlanet => 15f,
            StellarBodyType.Planetoid => 8f,
            StellarBodyType.Comet => 6f,
            _ => 10f
        };
    }

    private Rectangle GetTutorialWindowBounds()
    {
        if (_tutorialModeWindow?.Window.Visible != true)
        {
            return Rectangle.Empty;
        }

        return TutorialHighlightBoundsResolver.PreferExplicitBounds(
            _tutorialModeWindow.Window.ActualBounds,
            _tutorialModeWindow.Window.Left,
            _tutorialModeWindow.Window.Top,
            _tutorialModeWindow.Window.Width,
            _tutorialModeWindow.Window.Height);
    }

    private Rectangle GetMapViewportBounds()
    {
        int screenWidth = _graphics.PreferredBackBufferWidth;
        int screenHeight = _graphics.PreferredBackBufferHeight;
        int top = GetContentTopAfterTopBar();
        int left = 0;
        int right = screenWidth;

        if (_leftSidePanel != null)
        {
            left = Math.Max(left, _leftSidePanel.Container.Left + (_leftSidePanel.Container.Width ?? _leftSidePanel.Width));
        }

        if (_rightSidePanel != null)
        {
            right = Math.Min(right, _rightSidePanel.Container.Left);
        }

        if (right <= left || screenHeight <= top)
        {
            return Rectangle.Empty;
        }

        return new Rectangle(left, top, right - left, screenHeight - top);
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

        if (_debugInfoWindow.IsVisible)
        {
            GameUiAuditReport auditReport = BuildGameUiVisualTree().CreateAuditReport(BuildGameUiScaleContext());
            _debugInfoWindow.UpdateUiAuditInfo(auditReport);
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

        int panelTopOffset = GameUiLayoutMetrics.ResolveContentTop(Rectangle.Empty, topBarHeight, ThemeManager.ScalePixels(12));
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

        int panelTopOffset = GetContentTopAfterTopBar(ThemeManager.ScalePixels(12));

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
            _gameplayHudOverlay.AiActivityPanel.Top = panelTopOffset;
            _gameplayHudOverlay.AiActivityPanel.Left = ThemeManager.ScalePixels(12);
            _gameplayHudOverlay.AiActivityPanel.Visible = true;
        }
        if (_gameplayHudOverlay?.SelectionPanel != null)
        {
            _gameplayHudOverlay.SelectionPanel.Top = panelTopOffset;
            _gameplayHudOverlay.SelectionPanel.Visible = true;
        }
        if (_gameplayHudOverlay?.LegendPanel != null)
        {
            _gameplayHudOverlay.LegendPanel.Top = panelTopOffset;
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
        int mapTop = GetContentTopAfterTopBar(ThemeManager.ScalePixels(12));
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

    private void UpdateTopBarDependentLayout()
    {
        int panelTopOffset = GetContentTopAfterTopBar(ThemeManager.ScalePixels(12));
        int screenWidth = _graphics.PreferredBackBufferWidth;
        int screenHeight = _graphics.PreferredBackBufferHeight;

        if (_leftSidePanel != null && _leftSidePanel.CurrentTopOffset != panelTopOffset)
        {
            _leftSidePanel.UpdatePosition(screenWidth, screenHeight, panelTopOffset);
        }

        if (_rightSidePanel != null && _rightSidePanel.CurrentTopOffset != panelTopOffset)
        {
            _rightSidePanel.UpdatePosition(screenWidth, screenHeight, panelTopOffset);
        }

        if (_leftSidePanel == null && _rightSidePanel == null)
        {
            if (_gameplayHudOverlay?.AiActivityPanel != null)
            {
                _gameplayHudOverlay.AiActivityPanel.Top = panelTopOffset;
            }

            if (_gameplayHudOverlay?.SelectionPanel != null)
            {
                _gameplayHudOverlay.SelectionPanel.Top = panelTopOffset;
            }

            if (_gameplayHudOverlay?.LegendPanel != null)
            {
                _gameplayHudOverlay.LegendPanel.Top = panelTopOffset;
            }
        }

        AnchorTutorialWindowToMapLeft();
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

        UpdateTopBarDependentLayout();
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

            try
            {
                if (_clientDebugGrpcHost != null)
                {
                    Task.Run(async () => await _clientDebugGrpcHost.DisposeAsync()).Wait(TimeSpan.FromSeconds(5));
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error disposing client debug gRPC host: {ex.Message}");
            }
        }
        base.Dispose(disposing);
    }
}




