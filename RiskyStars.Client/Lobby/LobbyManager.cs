using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RiskyStars.Shared;
using Myra;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.Brushes;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RiskyStars.Client;

public enum LobbyState
{
    ModeSelection,
    SinglePlayerLobby,
    InitializingSinglePlayer,
    Connection,
    Browser,
    CreateLobby,
    InLobby,
    StartingGame,
    InGame
}

public class LobbyManager
{
    private readonly GraphicsDevice _graphicsDevice;
    private int _screenWidth;
    private int _screenHeight;

    private LobbyClient? _lobbyClient;
    private EmbeddedServerHost? _embeddedServerHost;
    private SpriteFont? _font;
    private GameModeSelector _modeSelectorScreen;
    private SinglePlayerLobbyScreen _singlePlayerLobbyScreen;
    private ConnectionScreen _connectionScreen;
    private LobbyBrowserScreen _browserScreen;
    private CreateLobbyScreen _createLobbyScreen;
    private LobbyScreen _lobbyScreen;

    private LobbyState _state = LobbyState.ModeSelection;
    private GameMode _selectedGameMode = GameMode.Multiplayer;
    private bool _isTutorialMode;
    private string? _currentLobbyId;
    private string? _playerId;
    private string? _playerName;
    private string? _sessionId;

    private KeyboardState _previousKeyState;
    private Task? _pendingTask;

    public LobbyState State => _state;
    public GameMode SelectedGameMode => _selectedGameMode;
    public bool IsTutorialMode => _isTutorialMode;
    public string? PlayerId => _playerId;
    public string? SessionId => _sessionId;
    public string? PlayerName => _playerName;
    public string MultiplayerServerAddress => _lobbyClient?.ServerAddress ?? _connectionScreen.ServerAddress;
    public bool IsInGame => _state == LobbyState.InGame;
    public EmbeddedServerHost? EmbeddedServer => _embeddedServerHost;

    public void SetSinglePlayerMode()
    {
        _isTutorialMode = false;
        _selectedGameMode = GameMode.SinglePlayer;
        _state = LobbyState.SinglePlayerLobby;
    }

    public void SetMultiplayerMode()
    {
        _isTutorialMode = false;
        _selectedGameMode = GameMode.Multiplayer;
        _state = LobbyState.Connection;
    }

    public void SetTutorialMode()
    {
        _isTutorialMode = true;
        _selectedGameMode = GameMode.SinglePlayer;

        if (_pendingTask != null)
        {
            return;
        }

        BeginSinglePlayerSession(
            "Cadet",
            "Default",
            CreateTutorialPlayerSlots(),
            "Starting tutorial mode",
            "Booting a guided single-player scenario against one easy AI opponent.",
            resetSinglePlayerScreen: true);
    }

    internal static LobbyManager CreateHeadlessForTests(int screenWidth = 1920, int screenHeight = 1080)
    {
        return new LobbyManager(null, screenWidth, screenHeight, initializeScreens: false);
    }

    public LobbyManager(GraphicsDevice graphicsDevice, int screenWidth, int screenHeight)
        : this(graphicsDevice, screenWidth, screenHeight, initializeScreens: true)
    {
    }

    private LobbyManager(GraphicsDevice? graphicsDevice, int screenWidth, int screenHeight, bool initializeScreens)
    {
        _graphicsDevice = graphicsDevice!;
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;

        if (!initializeScreens)
        {
            _modeSelectorScreen = null!;
            _singlePlayerLobbyScreen = null!;
            _connectionScreen = null!;
            _browserScreen = null!;
            _createLobbyScreen = null!;
            _lobbyScreen = null!;
            return;
        }

        ArgumentNullException.ThrowIfNull(graphicsDevice);
        _modeSelectorScreen = new GameModeSelector(graphicsDevice, screenWidth, screenHeight);
        _singlePlayerLobbyScreen = new SinglePlayerLobbyScreen(graphicsDevice, screenWidth, screenHeight);
        _connectionScreen = new ConnectionScreen(graphicsDevice, screenWidth, screenHeight);
        _browserScreen = new LobbyBrowserScreen(graphicsDevice, screenWidth, screenHeight);
        _createLobbyScreen = new CreateLobbyScreen(graphicsDevice, screenWidth, screenHeight);
        _lobbyScreen = new LobbyScreen(graphicsDevice, screenWidth, screenHeight);
    }

    public void LoadContent(SpriteFont font)
    {
        _font = font;
        _modeSelectorScreen.LoadContent(font);
        _singlePlayerLobbyScreen.LoadContent(font);
        _connectionScreen.LoadContent(font);
        _browserScreen.LoadContent(font);
        _createLobbyScreen.LoadContent(font);
        _lobbyScreen.LoadContent(font);
    }

    internal void DebugShowState(LobbyState state)
    {
        _pendingTask = null;
        _state = state;
        _selectedGameMode = state == LobbyState.SinglePlayerLobby
            ? GameMode.SinglePlayer
            : GameMode.Multiplayer;
        _isTutorialMode = false;
        _playerId = "debug-player";
        _playerName = "Cadet";
        _sessionId = "debug-session";
        _currentLobbyId = "debug-lobby";

        if (state == LobbyState.Browser)
        {
            _browserScreen.SetLobbies(new List<LobbyInfo> { CreateDebugLobbyInfo() });
        }

        if (state == LobbyState.InLobby || state == LobbyState.StartingGame)
        {
            _lobbyScreen.SetLobbyInfo(CreateDebugLobbyInfo(), _playerId);
            _lobbyScreen.SetReady(true);
        }
    }

    private static LobbyInfo CreateDebugLobbyInfo()
    {
        return new LobbyInfo
        {
            LobbyId = "debug-lobby",
            HostPlayerName = "Host Commander",
            CurrentPlayers = 2,
            MaxPlayers = 4,
            GameMode = "Conquest",
            MapName = "Rigel March",
            PlayerNames = { "Host Commander", "Cadet" }
        };
    }

    public void Update(GameTime gameTime)
    {
        var mouseState = Mouse.GetState();
        var keyState = Keyboard.GetState();

        switch (_state)
        {
            case LobbyState.ModeSelection:
                UpdateModeSelection(gameTime, mouseState);
                break;

            case LobbyState.SinglePlayerLobby:
                UpdateSinglePlayerLobby(gameTime, mouseState, keyState);
                break;

            case LobbyState.InitializingSinglePlayer:
                break;

            case LobbyState.Connection:
                UpdateConnection(gameTime, mouseState, keyState);
                break;

            case LobbyState.Browser:
                UpdateBrowser(gameTime, mouseState);
                break;

            case LobbyState.CreateLobby:
                UpdateCreateLobby(gameTime, mouseState, keyState);
                break;

            case LobbyState.InLobby:
                UpdateInLobby(gameTime, mouseState);
                break;

            case LobbyState.StartingGame:
                break;

            case LobbyState.InGame:
                break;
        }

        _previousKeyState = keyState;
    }

    private void UpdateModeSelection(GameTime gameTime, MouseState mouseState)
    {
        _modeSelectorScreen.Update(gameTime, mouseState);

        if (_modeSelectorScreen.ShouldProceed && _modeSelectorScreen.SelectedMode.HasValue)
        {
            _selectedGameMode = _modeSelectorScreen.SelectedMode.Value;
            _modeSelectorScreen.Reset();

            if (_selectedGameMode == GameMode.Multiplayer)
            {
                _state = LobbyState.Connection;
            }
            else if (_selectedGameMode == GameMode.SinglePlayer)
            {
                _state = LobbyState.SinglePlayerLobby;
            }
        }

        if (_modeSelectorScreen.ShouldGoBack)
        {
            _modeSelectorScreen.Reset();
        }
    }

    private void UpdateSinglePlayerLobby(GameTime gameTime, MouseState mouseState, KeyboardState keyState)
    {
        _singlePlayerLobbyScreen.Update(gameTime, mouseState, keyState);

        if (_singlePlayerLobbyScreen.ShouldStartGame && _pendingTask == null)
        {
            _isTutorialMode = false;
            var playerName = _singlePlayerLobbyScreen.PlayerName;
            var selectedMap = _singlePlayerLobbyScreen.SelectedMap;
            var playerSlots = ClonePlayerSlots(_singlePlayerLobbyScreen.PlayerSlots);

            BeginSinglePlayerSession(
                playerName,
                selectedMap,
                playerSlots,
                "Starting single-player game",
                "Booting embedded server and creating the first turn.",
                resetSinglePlayerScreen: true);
        }

        if (_singlePlayerLobbyScreen.ShouldGoBack)
        {
            _isTutorialMode = false;
            if (_embeddedServerHost != null)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        await _embeddedServerHost.DisposeAsync();
                    }
                    catch (Exception ex)
                    {
                        System.Console.WriteLine($"Error disposing embedded server: {ex.Message}");
                    }
                }).Wait(TimeSpan.FromSeconds(5));
                
                _embeddedServerHost = null;
                _singlePlayerLobbyScreen.SetEmbeddedServerHost(null);
            }
            
            _playerId = null;
            _sessionId = null;
            _singlePlayerLobbyScreen.Reset();
            _state = LobbyState.ModeSelection;
        }
    }

    private void BeginSinglePlayerSession(
        string playerName,
        string selectedMap,
        IReadOnlyList<PlayerSlot> playerSlots,
        string busyTitle,
        string busyDetail,
        bool resetSinglePlayerScreen)
    {
        if (_pendingTask != null)
        {
            return;
        }

        _playerName = string.IsNullOrWhiteSpace(playerName) ? "Player" : playerName;

        int humanPlayerCount = playerSlots.Count(s => s.PlayerType == PlayerType.Human);
        int aiPlayerCount = playerSlots.Count(s => s.IsAI);

        System.Console.WriteLine($"Starting single player game: {_playerName}");
        System.Console.WriteLine($"Map: {selectedMap}");
        System.Console.WriteLine($"Human Players: {humanPlayerCount}, AI Players: {aiPlayerCount}");

        foreach (var slot in playerSlots.Where(s => s.IsAI))
        {
            System.Console.WriteLine($"  - {slot.PlayerName} ({slot.GetDifficultyLevel()} AI)");
        }

        GameFeedbackBus.PublishBusy(busyTitle, busyDetail);
        if (resetSinglePlayerScreen)
        {
            _singlePlayerLobbyScreen.Reset();
        }

        _state = LobbyState.InitializingSinglePlayer;

        _pendingTask = Task.Run(async () =>
        {
            try
            {
                _embeddedServerHost = new EmbeddedServerHost();
                _singlePlayerLobbyScreen.SetEmbeddedServerHost(_embeddedServerHost);

                bool success = await _embeddedServerHost.StartAsync();

                if (success)
                {
                    GameFeedbackBus.PublishBusy("Embedded server online", "Authenticating commander profile.");
                    using var embeddedLobbyClient = new LobbyClient(_embeddedServerHost.ServerUrl);
                    var authResponse = await embeddedLobbyClient.AuthenticateAsync(_playerName ?? "Player");
                    if (!authResponse.Success)
                    {
                        throw new InvalidOperationException(authResponse.Message);
                    }

                    GameFeedbackBus.PublishBusy("Commander authenticated", "Creating single-player session.");
                    var startResponse = await embeddedLobbyClient.StartSinglePlayerGameAsync(
                        _playerName ?? "Player",
                        selectedMap,
                        playerSlots.Where(slot => slot.IsAI));

                    if (!startResponse.Success || string.IsNullOrWhiteSpace(startResponse.SessionId))
                    {
                        throw new InvalidOperationException(startResponse.Message);
                    }

                    _playerId = startResponse.PlayerId;
                    _sessionId = startResponse.SessionId;
                    GameFeedbackBus.PublishSuccess(
                        _isTutorialMode ? "Tutorial scenario ready" : "Single-player session ready",
                        "Opening the live game stream.");
                    _state = LobbyState.InGame;
                }
                else
                {
                    string errorMessage = _embeddedServerHost.LastError ?? "Unknown error occurred";
                    System.Console.WriteLine($"Failed to start embedded server: {errorMessage}");
                    GameFeedbackBus.PublishError("Failed to start embedded server", errorMessage);
                    _singlePlayerLobbyScreen.SetError($"Failed to start game server: {errorMessage}");

                    await _embeddedServerHost.DisposeAsync();
                    _embeddedServerHost = null;
                    _singlePlayerLobbyScreen.SetEmbeddedServerHost(null);
                    _state = LobbyState.SinglePlayerLobby;
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Failed to start embedded server: {ex.Message}");
                GameFeedbackBus.PublishError("Single-player startup failed", ex.Message);
                _singlePlayerLobbyScreen.SetError($"Failed to start game server: {ex.Message}");

                if (_embeddedServerHost != null)
                {
                    try
                    {
                        await _embeddedServerHost.DisposeAsync();
                    }
                    catch (Exception disposeEx)
                    {
                        System.Console.WriteLine($"Error disposing server after failure: {disposeEx.Message}");
                    }

                    _embeddedServerHost = null;
                    _singlePlayerLobbyScreen.SetEmbeddedServerHost(null);
                }

                _state = LobbyState.SinglePlayerLobby;
            }
            finally
            {
                _pendingTask = null;
            }
        });
    }

    private static IReadOnlyList<PlayerSlot> CreateTutorialPlayerSlots()
    {
        var slots = new List<PlayerSlot>();
        for (int i = 0; i < 8; i++)
        {
            var slot = new PlayerSlot(i + 1)
            {
                PlayerType = PlayerType.Human,
                PlayerName = i == 0 ? "Cadet" : string.Empty,
                IsReady = i == 0,
                IsHost = i == 0
            };
            slots.Add(slot);
        }

        slots[1].PlayerType = PlayerType.EasyAI;
        slots[1].PlayerName = "Drill Marshal Vega";
        slots[1].IsReady = true;

        return slots;
    }

    private static IReadOnlyList<PlayerSlot> ClonePlayerSlots(IEnumerable<PlayerSlot> slots)
    {
        return slots
            .Select(slot => new PlayerSlot(slot.SlotIndex)
            {
                PlayerType = slot.PlayerType,
                PlayerName = slot.PlayerName,
                IsReady = slot.IsReady,
                IsHost = slot.IsHost
            })
            .ToList();
    }

    private void UpdateConnection(GameTime gameTime, MouseState mouseState, KeyboardState keyState)
    {
        _connectionScreen.Update(gameTime, mouseState, keyState);

        if (_connectionScreen.ShouldGoBack())
        {
            _state = LobbyState.ModeSelection;
            return;
        }

        if (_connectionScreen.ShouldAttemptConnection() && _pendingTask == null)
        {
            _pendingTask = Task.Run(async () =>
            {
                try
                {
                    _lobbyClient = new LobbyClient(_connectionScreen.ServerAddress);
                    var response = await _lobbyClient.AuthenticateAsync(_connectionScreen.PlayerName);

                    if (response.Success)
                    {
                        _playerId = _lobbyClient.PlayerId;
                        _playerName = _connectionScreen.PlayerName;
                        _connectionScreen.SetConnectionResult(true, "Connected successfully!");
                        await Task.Delay(500);
                        _state = LobbyState.Browser;
                    }
                    else
                    {
                        _connectionScreen.SetConnectionResult(false, response.Message);
                    }
                }
                catch (Exception ex)
                {
                    _connectionScreen.SetConnectionResult(false, $"Connection failed: {ex.Message}");
                }
                finally
                {
                    _pendingTask = null;
                }
            });
        }
    }

    private void UpdateBrowser(GameTime gameTime, MouseState mouseState)
    {
        _browserScreen.Update(gameTime, mouseState);

        if (_browserScreen.ShouldRefresh && _pendingTask == null)
        {
            _pendingTask = Task.Run(async () =>
            {
                try
                {
                    if (_lobbyClient != null)
                    {
                        var response = await _lobbyClient.ListLobbiesAsync();
                        if (response.Success)
                        {
                            _browserScreen.SetLobbies(response.Lobbies.ToList());
                        }
                        else
                        {
                            GameFeedbackBus.PublishWarning("Lobby refresh failed", response.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    GameFeedbackBus.PublishError("Lobby refresh failed", ex.Message);
                }
                finally
                {
                    _pendingTask = null;
                }
            });
        }

        if (_browserScreen.ShouldCreateLobby)
        {
            _browserScreen.Reset();
            _state = LobbyState.CreateLobby;
        }

        if (_browserScreen.ShouldJoinLobby && _pendingTask == null)
        {
            var lobbyId = _browserScreen.SelectedLobbyId;
            if (lobbyId != null)
            {
                _pendingTask = Task.Run(async () =>
                {
                    try
                    {
                        if (_lobbyClient != null && _playerName != null)
                        {
                            var response = await _lobbyClient.JoinLobbyAsync(lobbyId, _playerName);
                            if (response.Success)
                            {
                                _currentLobbyId = lobbyId;
                                _state = LobbyState.InLobby;
                                _browserScreen.Reset();
                            }
                            else
                            {
                                GameFeedbackBus.PublishWarning("Join lobby failed", response.Message);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        GameFeedbackBus.PublishError("Join lobby failed", ex.Message);
                    }
                    finally
                    {
                        _pendingTask = null;
                    }
                });
            }
        }
    }

    private void UpdateCreateLobby(GameTime gameTime, MouseState mouseState, KeyboardState keyState)
    {
        _createLobbyScreen.Update(gameTime, mouseState, keyState);

        if (_createLobbyScreen.ShouldCreate && _pendingTask == null)
        {
            var settings = _createLobbyScreen.LobbySettings;
            if (settings != null)
            {
                _pendingTask = Task.Run(async () =>
                {
                    try
                    {
                        if (_lobbyClient != null && _playerName != null)
                        {
                            var response = await _lobbyClient.CreateLobbyAsync(_playerName, settings);
                            if (response.Success)
                            {
                                _currentLobbyId = response.LobbyId;
                                _state = LobbyState.InLobby;
                                _createLobbyScreen.Reset();
                            }
                            else
                            {
                                GameFeedbackBus.PublishWarning("Create lobby failed", response.Message);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        GameFeedbackBus.PublishError("Create lobby failed", ex.Message);
                    }
                    finally
                    {
                        _pendingTask = null;
                    }
                });
            }
        }

        if (_createLobbyScreen.ShouldCancel)
        {
            _createLobbyScreen.Reset();
            _state = LobbyState.Browser;
        }
    }

    private void UpdateInLobby(GameTime gameTime, MouseState mouseState)
    {
        _lobbyScreen.Update(gameTime, mouseState);

        if (_lobbyScreen.ShouldRefresh && _pendingTask == null && _currentLobbyId != null)
        {
            _pendingTask = Task.Run(async () =>
            {
                    try
                    {
                        if (_lobbyClient != null)
                        {
                            var response = await _lobbyClient.GetLobbyAsync(_currentLobbyId);
                            if (response.Success && response.Lobby != null)
                            {
                                _lobbyScreen.SetLobbyInfo(response.Lobby, _lobbyClient.PlayerId ?? "");
                            }
                            else
                            {
                                GameFeedbackBus.PublishWarning("Lobby refresh failed", response.Message);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        GameFeedbackBus.PublishError("Lobby refresh failed", ex.Message);
                    }
                finally
                {
                    _pendingTask = null;
                }
            });
        }

        if (_lobbyScreen.ShouldToggleReady && _pendingTask == null && _currentLobbyId != null)
        {
            _pendingTask = Task.Run(async () =>
            {
                    try
                    {
                        if (_lobbyClient != null)
                        {
                            var response = await _lobbyClient.SetReadyAsync(_currentLobbyId, true);
                            if (response.Success)
                            {
                                _lobbyScreen.SetReady(true);
                            }
                            else
                            {
                                GameFeedbackBus.PublishWarning("Ready-state update failed", response.Message);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        GameFeedbackBus.PublishError("Ready-state update failed", ex.Message);
                    }
                finally
                {
                    _pendingTask = null;
                }
            });
        }

        if (_lobbyScreen.ShouldStartGame && _pendingTask == null && _currentLobbyId != null)
        {
            _pendingTask = Task.Run(async () =>
            {
                    try
                    {
                        if (_lobbyClient != null)
                        {
                            var response = await _lobbyClient.StartGameAsync(_currentLobbyId);
                            if (response.Success && !string.IsNullOrEmpty(response.SessionId))
                            {
                                _sessionId = response.SessionId;
                                _lobbyScreen.OnGameStarted(response.SessionId);
                                GameFeedbackBus.PublishBusy("Lobby launch confirmed", "Starting the shared game session.");
                                _state = LobbyState.StartingGame;
                                await Task.Delay(1000);
                                _state = LobbyState.InGame;
                            }
                            else
                            {
                                GameFeedbackBus.PublishWarning("Start game failed", response.Message);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        GameFeedbackBus.PublishError("Start game failed", ex.Message);
                    }
                finally
                {
                    _pendingTask = null;
                }
            });
        }

        if (_lobbyScreen.ShouldLeaveLobby && _pendingTask == null && _currentLobbyId != null)
        {
            _pendingTask = Task.Run(async () =>
            {
                    try
                    {
                        if (_lobbyClient != null)
                        {
                            await _lobbyClient.LeaveLobbyAsync(_currentLobbyId);
                            _currentLobbyId = null;
                            _lobbyScreen.Reset();
                            _state = LobbyState.Browser;
                        }
                    }
                    catch (Exception ex)
                    {
                        GameFeedbackBus.PublishError("Leave lobby failed", ex.Message);
                    }
                finally
                {
                    _pendingTask = null;
                }
            });
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        switch (_state)
        {
            case LobbyState.ModeSelection:
                _modeSelectorScreen.Draw(spriteBatch);
                break;

            case LobbyState.SinglePlayerLobby:
                _singlePlayerLobbyScreen.Draw(spriteBatch);
                break;

            case LobbyState.InitializingSinglePlayer:
                DrawInitializingScreen(spriteBatch);
                break;

            case LobbyState.Connection:
                _connectionScreen.Draw(spriteBatch);
                break;

            case LobbyState.Browser:
                _browserScreen.Draw(spriteBatch);
                break;

            case LobbyState.CreateLobby:
                _createLobbyScreen.Draw(spriteBatch);
                break;

            case LobbyState.InLobby:
            case LobbyState.StartingGame:
                _lobbyScreen.Draw(spriteBatch);
                break;

            case LobbyState.InGame:
                break;
        }
    }

    private Desktop? _initializingDesktop;
    private Panel? _initializingMainPanel;
#pragma warning disable CS0618 // Type or member is obsolete
    private Label? _initializingLoadingLabel;
    private Label? _initializingStatusLabel;
#pragma warning restore CS0618 // Type or member is obsolete

    private void BuildInitializingScreen()
    {
        if (_initializingDesktop != null)
            return;

        _initializingDesktop = new Desktop();

        var rootGrid = new Grid
        {
            RowSpacing = 20,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Width = _screenWidth,
            Height = _screenHeight
        };

#pragma warning disable CS0618 // Type or member is obsolete
        _initializingLoadingLabel = new Label
        {
            Text = "Initializing single player game...",
            Font = ThemeManager.UiFonts.Heading,
            TextColor = Color.White,
            HorizontalAlignment = HorizontalAlignment.Center
        };
#pragma warning restore CS0618 // Type or member is obsolete

#pragma warning disable CS0618 // Type or member is obsolete
        _initializingStatusLabel = new Label
        {
            Text = "",
            Font = ThemeManager.UiFonts.Small,
            TextColor = Color.Yellow,
            HorizontalAlignment = HorizontalAlignment.Center
        };
#pragma warning restore CS0618 // Type or member is obsolete

        rootGrid.Widgets.Add(_initializingLoadingLabel);
        rootGrid.Widgets.Add(_initializingStatusLabel);

        _initializingMainPanel = new Panel
        {
            Width = _screenWidth,
            Height = _screenHeight,
            Background = new SolidBrush(new Color(10, 10, 20))
        };

        _initializingMainPanel.Widgets.Add(rootGrid);
        _initializingDesktop.Root = _initializingMainPanel;
    }

    private void DrawInitializingScreen(SpriteBatch spriteBatch)
    {
        BuildInitializingScreen();

        if (_initializingStatusLabel != null && _embeddedServerHost != null)
        {
            var statusText = _embeddedServerHost.Status switch
            {
                ServerStatus.Starting => "Starting server...",
                ServerStatus.Running => "Server ready!",
                ServerStatus.Error => $"Error: {_embeddedServerHost.LastError}",
                _ => ""
            };

            _initializingStatusLabel.Text = statusText;
            _initializingStatusLabel.TextColor = _embeddedServerHost.Status switch
            {
                ServerStatus.Starting => Color.Yellow,
                ServerStatus.Running => Color.LimeGreen,
                ServerStatus.Error => Color.Red,
                _ => Color.White
            };
        }

        _initializingDesktop?.Render();
    }

    public async Task DisposeAsync()
    {
        _lobbyClient?.Dispose();
        
        if (_embeddedServerHost != null)
        {
            try
            {
                await _embeddedServerHost.DisposeAsync();
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error disposing embedded server: {ex.Message}");
            }
            finally
            {
                _embeddedServerHost = null;
            }
        }

        if (_pendingTask != null)
        {
            try
            {
                await _pendingTask;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error waiting for pending task: {ex.Message}");
            }
        }
    }

    public void Dispose()
    {
        try
        {
            Task.Run(async () => await DisposeAsync()).Wait(TimeSpan.FromSeconds(10));
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error disposing LobbyManager: {ex.Message}");
        }
    }

    public void ResizeViewport(int screenWidth, int screenHeight)
    {
        if (screenWidth <= 0 || screenHeight <= 0)
        {
            return;
        }

        _screenWidth = screenWidth;
        _screenHeight = screenHeight;

        _modeSelectorScreen.ResizeViewport(screenWidth, screenHeight);
        _singlePlayerLobbyScreen.ResizeViewport(screenWidth, screenHeight);
        _connectionScreen.ResizeViewport(screenWidth, screenHeight);
        _browserScreen.ResizeViewport(screenWidth, screenHeight);
        _createLobbyScreen.ResizeViewport(screenWidth, screenHeight);
        _lobbyScreen.ResizeViewport(screenWidth, screenHeight);

        _initializingDesktop = null;
        _initializingMainPanel = null;
        _initializingLoadingLabel = null;
        _initializingStatusLabel = null;
    }
}
