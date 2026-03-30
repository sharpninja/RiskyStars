using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RiskyStars.Shared;
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
    private readonly int _screenWidth;
    private readonly int _screenHeight;

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
    private string? _currentLobbyId;
    private string? _playerName;
    private string? _sessionId;

    private KeyboardState _previousKeyState;
    private Task? _pendingTask;

    public LobbyState State => _state;
    public GameMode SelectedGameMode => _selectedGameMode;
    public string? SessionId => _sessionId;
    public string? PlayerName => _playerName;
    public bool IsInGame => _state == LobbyState.InGame;
    public EmbeddedServerHost? EmbeddedServer => _embeddedServerHost;

    public LobbyManager(GraphicsDevice graphicsDevice, int screenWidth, int screenHeight)
    {
        _graphicsDevice = graphicsDevice;
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;

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
            _playerName = _singlePlayerLobbyScreen.PlayerName;
            var selectedMap = _singlePlayerLobbyScreen.SelectedMap;
            var playerSlots = _singlePlayerLobbyScreen.PlayerSlots;
            
            int humanPlayerCount = playerSlots.Count(s => s.PlayerType == PlayerType.Human);
            int aiPlayerCount = playerSlots.Count(s => s.IsAI);
            
            System.Console.WriteLine($"Starting single player game: {_playerName}");
            System.Console.WriteLine($"Map: {selectedMap}");
            System.Console.WriteLine($"Human Players: {humanPlayerCount}, AI Players: {aiPlayerCount}");
            
            foreach (var slot in playerSlots.Where(s => s.IsAI))
            {
                System.Console.WriteLine($"  - {slot.PlayerName} ({slot.GetDifficultyLevel()} AI)");
            }
            
            _singlePlayerLobbyScreen.Reset();
            _state = LobbyState.InitializingSinglePlayer;

            _pendingTask = Task.Run(async () =>
            {
                try
                {
                    _embeddedServerHost = new EmbeddedServerHost();
                    bool success = await _embeddedServerHost.StartAsync();

                    if (success)
                    {
                        _sessionId = Guid.NewGuid().ToString();
                        _state = LobbyState.InGame;
                    }
                    else
                    {
                        string errorMessage = _embeddedServerHost.LastError ?? "Unknown error occurred";
                        System.Console.WriteLine($"Failed to start embedded server: {errorMessage}");
                        _singlePlayerLobbyScreen.SetError($"Failed to start game server: {errorMessage}");
                        
                        await _embeddedServerHost.DisposeAsync();
                        _embeddedServerHost = null;
                        _state = LobbyState.SinglePlayerLobby;
                    }
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"Failed to start embedded server: {ex.Message}");
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
                    }
                    
                    _state = LobbyState.SinglePlayerLobby;
                }
                finally
                {
                    _pendingTask = null;
                }
            });
        }

        if (_singlePlayerLobbyScreen.ShouldGoBack)
        {
            _singlePlayerLobbyScreen.Reset();
            _state = LobbyState.ModeSelection;
        }
    }

    private void UpdateConnection(GameTime gameTime, MouseState mouseState, KeyboardState keyState)
    {
        _connectionScreen.Update(gameTime, mouseState, keyState);

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
                    }
                }
                catch
                {
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
                        }
                    }
                    catch
                    {
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
                        }
                    }
                    catch
                    {
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
                    }
                }
                catch
                {
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
                    }
                }
                catch
                {
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
                            _state = LobbyState.StartingGame;
                            await Task.Delay(1000);
                            _state = LobbyState.InGame;
                        }
                    }
                }
                catch
                {
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
                catch
                {
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

    private void DrawInitializingScreen(SpriteBatch spriteBatch)
    {
        if (_font == null) return;

        spriteBatch.Begin();

        var loadingText = "Initializing single player game...";
        var textSize = _font.MeasureString(loadingText);
        var textPosition = new Vector2(
            (_screenWidth - textSize.X) / 2,
            (_screenHeight - textSize.Y) / 2);

        spriteBatch.DrawString(_font, loadingText, textPosition, Color.White);

        spriteBatch.End();
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
}
