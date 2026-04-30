using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using RiskyStars.Shared;

namespace RiskyStars.Client;

public class InputController
{
    private readonly GrpcGameClient _gameClient;
    private readonly GameStateCache _gameStateCache;
    private readonly MapData _mapData;
    private readonly Camera2D _camera;
    
    private MouseState _previousMouseState;
    private KeyboardState _previousKeyState;
    private Vector2? _rightMouseDownScreenPosition;
    private bool _isRightMouseDragging;
    
    private string? _currentPlayerId;
    private SelectionState _selectionState;
    private ContextMenuManager? _contextMenuManager;

    private const float RightMouseDragThreshold = 6f;
    
    public SelectionState Selection => _selectionState;
    public bool ShowHelp { get; private set; }
    public bool IsPointerInputBlocked { get; set; }
    public event EventHandler<StellarBodyData>? ContinentZoomRequested;
    
    public InputController(GrpcGameClient gameClient, GameStateCache gameStateCache, MapData mapData, Camera2D camera)
    {
        _gameClient = gameClient;
        _gameStateCache = gameStateCache;
        _mapData = mapData;
        _camera = camera;
        _selectionState = new SelectionState();
        _previousMouseState = Mouse.GetState();
        _previousKeyState = Keyboard.GetState();
    }
    
    public void SetContextMenuManager(ContextMenuManager contextMenuManager)
    {
        _contextMenuManager = contextMenuManager;
    }
    
    public void SetCurrentPlayer(string? playerId)
    {
        _currentPlayerId = playerId;
    }

    internal void DebugSetHelpVisible(bool visible)
    {
        ShowHelp = visible;
    }
    
    public void Update(GameTime gameTime)
    {
        var mouseState = Mouse.GetState();
        var keyState = Keyboard.GetState();

        if (IsPointerInputBlocked)
        {
            ResetPointerGesture();
        }
        else
        {
            HandleMouseInput(mouseState);
        }

        HandleKeyboardInput(keyState);
        
        _previousMouseState = mouseState;
        _previousKeyState = keyState;
    }

    private void ResetPointerGesture()
    {
        _rightMouseDownScreenPosition = null;
        _isRightMouseDragging = false;
    }
    
    private void HandleMouseInput(MouseState mouseState)
    {
        HandleRightMouseDrag(mouseState);

        if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
        {
            var screenPosition = new Vector2(mouseState.X, mouseState.Y);
            var worldPosition = _camera.ScreenToWorld(screenPosition);
            
            if (_contextMenuManager != null && _contextMenuManager.IsMenuOpen)
            {
                _contextMenuManager.CloseContextMenu();
            }
            else
            {
                HandleLeftClick(worldPosition);
            }
        }
        
        if (mouseState.RightButton == ButtonState.Released && _previousMouseState.RightButton == ButtonState.Pressed)
        {
            var screenPosition = new Vector2(mouseState.X, mouseState.Y);
            var worldPosition = _camera.ScreenToWorld(screenPosition);

            if (!_isRightMouseDragging)
            {
                if (_contextMenuManager != null)
                {
                    _contextMenuManager.OpenContextMenu(screenPosition, worldPosition, _selectionState);
                }
                else
                {
                    HandleRightClick(worldPosition);
                }
            }

            _rightMouseDownScreenPosition = null;
            _isRightMouseDragging = false;
        }
    }

    private void HandleRightMouseDrag(MouseState mouseState)
    {
        var currentScreenPosition = new Vector2(mouseState.X, mouseState.Y);

        if (mouseState.RightButton == ButtonState.Pressed && _previousMouseState.RightButton == ButtonState.Released)
        {
            _rightMouseDownScreenPosition = currentScreenPosition;
            _isRightMouseDragging = false;
            return;
        }

        if (mouseState.RightButton != ButtonState.Pressed || _previousMouseState.RightButton != ButtonState.Pressed)
        {
            return;
        }

        if (_rightMouseDownScreenPosition.HasValue)
        {
            var dragVector = currentScreenPosition - _rightMouseDownScreenPosition.Value;
            if (!_isRightMouseDragging && dragVector.LengthSquared() >= RightMouseDragThreshold * RightMouseDragThreshold)
            {
                _isRightMouseDragging = true;
                _contextMenuManager?.CloseContextMenu();
            }
        }

        if (_isRightMouseDragging)
        {
            var frameDelta = currentScreenPosition - new Vector2(_previousMouseState.X, _previousMouseState.Y);
            if (frameDelta.LengthSquared() > 0.001f)
            {
                _camera.PanByScreenDelta(frameDelta);
            }
        }
    }
    
    private void HandleLeftClick(Vector2 worldPosition)
    {
        var clickedArmy = FindArmyAtPosition(worldPosition);
        if (clickedArmy != null && clickedArmy.OwnerId == _currentPlayerId)
        {
            _selectionState.SelectArmy(clickedArmy);
            return;
        }
        
        var clickedBody = ContinentZoomLayout.FindZoomableBodyAtPosition(_mapData, worldPosition);
        if (clickedBody != null)
        {
            _selectionState.SelectStellarBody(clickedBody);
            ContinentZoomRequested?.Invoke(this, clickedBody);
            return;
        }

        var clickedRegion = FindRegionAtPosition(worldPosition);
        if (clickedRegion != null)
        {
            _selectionState.SelectRegion(clickedRegion);
            return;
        }
        
        var clickedMouth = FindHyperspaceLaneMouthAtPosition(worldPosition);
        if (clickedMouth != null)
        {
            _selectionState.SelectHyperspaceLaneMouth(clickedMouth.Value.Item1, clickedMouth.Value.Item2);
            return;
        }
        
        var clickedSingleBody = FindStellarBodyAtPosition(worldPosition);
        if (clickedSingleBody != null)
        {
            _selectionState.SelectStellarBody(clickedSingleBody);
            return;
        }
        
        var clickedSystem = FindStarSystemAtPosition(worldPosition);
        if (clickedSystem != null)
        {
            _selectionState.SelectStarSystem(clickedSystem);
            return;
        }
        
        _selectionState.ClearSelection();
    }
    
    private void HandleRightClick(Vector2 worldPosition)
    {
        if (_selectionState.SelectedArmy == null || _currentPlayerId == null)
        {
            return;
        }

        var targetRegion = FindRegionAtPosition(worldPosition);
        if (targetRegion != null)
        {
            SendMoveArmyCommand(_selectionState.SelectedArmy.ArmyId, targetRegion.Id, LocationType.Region);
            return;
        }
        
        var targetMouth = FindHyperspaceLaneMouthAtPosition(worldPosition);
        if (targetMouth != null)
        {
            SendMoveArmyCommand(_selectionState.SelectedArmy.ArmyId, targetMouth.Value.Item1, LocationType.HyperspaceLaneMouth);
            return;
        }
    }
    
    private void HandleKeyboardInput(KeyboardState keyState)
    {
        if (_currentPlayerId == null || _gameStateCache.GetGameId() == null)
        {
            return;
        }

        if (IsKeyPressed(keyState, Keys.Space))
        {
            SendAdvancePhaseCommand();
        }
        
        if (IsKeyPressed(keyState, Keys.P))
        {
            SendProduceResourcesCommand();
        }
        
        if (IsKeyPressed(keyState, Keys.B))
        {
            SendPurchaseArmiesCommand(1);
        }
        
        if (IsKeyPressed(keyState, Keys.D1) || IsKeyPressed(keyState, Keys.NumPad1))
        {
            SendPurchaseArmiesCommand(1);
        }
        
        if (IsKeyPressed(keyState, Keys.D5) || IsKeyPressed(keyState, Keys.NumPad5))
        {
            SendPurchaseArmiesCommand(5);
        }
        
        if (IsKeyPressed(keyState, Keys.D0) || IsKeyPressed(keyState, Keys.NumPad0))
        {
            SendPurchaseArmiesCommand(10);
        }
        
        if (IsKeyPressed(keyState, Keys.R))
        {
            if (_selectionState.SelectedRegion != null)
            {
                SendReinforceLocationCommand(_selectionState.SelectedRegion.Id, LocationType.Region, 1);
            }
            else if (_selectionState.SelectedHyperspaceLaneMouthId != null)
            {
                SendReinforceLocationCommand(_selectionState.SelectedHyperspaceLaneMouthId, LocationType.HyperspaceLaneMouth, 1);
            }
        }
        
        if (IsKeyPressed(keyState, Keys.Tab))
        {
            CycleSelection();
        }
        
        if (IsKeyPressed(keyState, Keys.C))
        {
            CenterCameraOnSelection();
        }
        
        if (IsKeyPressed(keyState, Keys.Escape))
        {
            _selectionState.ClearSelection();
        }
        
        if (IsKeyPressed(keyState, Keys.H))
        {
            ShowHelp = !ShowHelp;
        }
    }
    
    private bool IsKeyPressed(KeyboardState keyState, Keys key)
    {
        return keyState.IsKeyDown(key) && _previousKeyState.IsKeyUp(key);
    }
    
    private ArmyState? FindArmyAtPosition(Vector2 worldPosition)
    {
        foreach (var system in _mapData.StarSystems)
        {
            foreach (var body in system.StellarBodies)
            {
                foreach (var region in body.Regions)
                {
                    if (Vector2.Distance(worldPosition, region.Position) < 15f)
                    {
                        var armies = _gameStateCache.GetArmiesAtLocation(region.Id, LocationType.Region);
                        if (armies.Count > 0)
                        {
                            return armies.FirstOrDefault(a => a.OwnerId == _currentPlayerId) ?? armies.First();
                        }
                    }
                }
            }
        }
        
        foreach (var lane in _mapData.HyperspaceLanes)
        {
            if (Vector2.Distance(worldPosition, lane.MouthAPosition) < 15f)
            {
                var armies = _gameStateCache.GetArmiesAtLocation(lane.MouthAId, LocationType.HyperspaceLaneMouth);
                if (armies.Count > 0)
                {
                    return armies.FirstOrDefault(a => a.OwnerId == _currentPlayerId) ?? armies.First();
                }
            }
            
            if (Vector2.Distance(worldPosition, lane.MouthBPosition) < 15f)
            {
                var armies = _gameStateCache.GetArmiesAtLocation(lane.MouthBId, LocationType.HyperspaceLaneMouth);
                if (armies.Count > 0)
                {
                    return armies.FirstOrDefault(a => a.OwnerId == _currentPlayerId) ?? armies.First();
                }
            }
        }
        
        return null;
    }
    
    private RegionData? FindRegionAtPosition(Vector2 worldPosition)
    {
        foreach (var system in _mapData.StarSystems)
        {
            foreach (var body in system.StellarBodies)
            {
                foreach (var region in body.Regions)
                {
                    if (Vector2.Distance(worldPosition, region.Position) < 10f)
                    {
                        return region;
                    }
                }
            }
        }
        
        return null;
    }
    
    private (string, Vector2)? FindHyperspaceLaneMouthAtPosition(Vector2 worldPosition)
    {
        foreach (var lane in _mapData.HyperspaceLanes)
        {
            if (Vector2.Distance(worldPosition, lane.MouthAPosition) < 12f)
            {
                return (lane.MouthAId, lane.MouthAPosition);
            }
            
            if (Vector2.Distance(worldPosition, lane.MouthBPosition) < 12f)
            {
                return (lane.MouthBId, lane.MouthBPosition);
            }
        }
        
        return null;
    }
    
    private StellarBodyData? FindStellarBodyAtPosition(Vector2 worldPosition)
    {
        foreach (var system in _mapData.StarSystems)
        {
            foreach (var body in system.StellarBodies)
            {
                float bodyRadius = body.Type switch
                {
                    StellarBodyType.GasGiant => 20f,
                    StellarBodyType.RockyPlanet => 15f,
                    StellarBodyType.Planetoid => 8f,
                    StellarBodyType.Comet => 6f,
                    _ => 10f
                };
                
                if (Vector2.Distance(worldPosition, body.Position) < bodyRadius)
                {
                    return body;
                }
            }
        }
        
        return null;
    }
    
    private StarSystemData? FindStarSystemAtPosition(Vector2 worldPosition)
    {
        foreach (var system in _mapData.StarSystems)
        {
            if (Vector2.Distance(worldPosition, system.Position) < 80f)
            {
                return system;
            }
        }
        
        return null;
    }
    
    private void SendMoveArmyCommand(string armyId, string targetLocationId, LocationType targetLocationType)
    {
        if (_currentPlayerId == null)
        {
            return;
        }

        GameFeedbackBus.PublishBusy("Submitting move order", "Routing an army to the selected destination.");

        Task.Run(async () =>
        {
            try
            {
                await _gameClient.SendMoveArmyAsync(_currentPlayerId, armyId, targetLocationId, targetLocationType);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send move army command: {ex.Message}");
                GameFeedbackBus.PublishError("Move order failed", ex.Message);
            }
        });
    }
    
    private void SendAdvancePhaseCommand()
    {
        if (_currentPlayerId == null)
        {
            return;
        }

        var gameId = _gameStateCache.GetGameId();
        if (gameId == null)
        {
            return;
        }

        GameFeedbackBus.PublishBusy("Advancing phase", "Sending the end-of-phase command to the server.");

        Task.Run(async () =>
        {
            try
            {
                await _gameClient.SendAdvancePhaseAsync(_currentPlayerId, gameId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send advance phase command: {ex.Message}");
                GameFeedbackBus.PublishError("Advance phase failed", ex.Message);
            }
        });
    }
    
    private void SendProduceResourcesCommand()
    {
        if (_currentPlayerId == null)
        {
            return;
        }

        var gameId = _gameStateCache.GetGameId();
        if (gameId == null)
        {
            return;
        }

        GameFeedbackBus.PublishBusy("Producing resources", "Requesting production for the current turn.");

        Task.Run(async () =>
        {
            try
            {
                await _gameClient.SendProduceResourcesAsync(_currentPlayerId, gameId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send produce resources command: {ex.Message}");
                GameFeedbackBus.PublishError("Produce resources failed", ex.Message);
            }
        });
    }
    
    private void SendPurchaseArmiesCommand(int count)
    {
        if (_currentPlayerId == null)
        {
            return;
        }

        var gameId = _gameStateCache.GetGameId();
        if (gameId == null)
        {
            return;
        }

        GameFeedbackBus.PublishBusy("Submitting purchase order", $"Requesting {count} new army unit(s).");

        Task.Run(async () =>
        {
            try
            {
                await _gameClient.SendPurchaseArmiesAsync(_currentPlayerId, gameId, count);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send purchase armies command: {ex.Message}");
                GameFeedbackBus.PublishError("Purchase order failed", ex.Message);
            }
        });
    }
    
    private void SendReinforceLocationCommand(string locationId, LocationType locationType, int unitCount)
    {
        if (_currentPlayerId == null)
        {
            return;
        }

        var gameId = _gameStateCache.GetGameId();
        if (gameId == null)
        {
            return;
        }

        GameFeedbackBus.PublishBusy("Submitting reinforcement order", $"Reinforcing the selected location with {unitCount} unit(s).");

        Task.Run(async () =>
        {
            try
            {
                await _gameClient.SendReinforceLocationAsync(_currentPlayerId, gameId, locationId, locationType, unitCount);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send reinforce location command: {ex.Message}");
                GameFeedbackBus.PublishError("Reinforcement failed", ex.Message);
            }
        });
    }
    
    private void CycleSelection()
    {
        if (_currentPlayerId == null)
        {
            return;
        }

        var playerArmies = _gameStateCache.GetArmiesOwnedByPlayer(_currentPlayerId);
        if (playerArmies.Count == 0)
        {
            return;
        }

        if (_selectionState.SelectedArmy == null)
        {
            _selectionState.SelectArmy(playerArmies.First());
            return;
        }
        
        var currentIndex = playerArmies.ToList().FindIndex(a => a.ArmyId == _selectionState.SelectedArmy.ArmyId);
        var nextIndex = (currentIndex + 1) % playerArmies.Count;
        _selectionState.SelectArmy(playerArmies.ToList()[nextIndex]);
    }
    
    private void CenterCameraOnSelection()
    {
        Vector2? targetPosition = null;
        
        if (_selectionState.SelectedArmy != null)
        {
            targetPosition = GetArmyPosition(_selectionState.SelectedArmy);
        }
        else if (_selectionState.SelectedRegion != null)
        {
            targetPosition = _selectionState.SelectedRegion.Position;
        }
        else if (_selectionState.SelectedHyperspaceLaneMouthPosition != null)
        {
            targetPosition = _selectionState.SelectedHyperspaceLaneMouthPosition;
        }
        else if (_selectionState.SelectedStellarBody != null)
        {
            targetPosition = _selectionState.SelectedStellarBody.Position;
        }
        else if (_selectionState.SelectedStarSystem != null)
        {
            targetPosition = _selectionState.SelectedStarSystem.Position;
        }
        
        if (targetPosition.HasValue)
        {
            _camera.CenterOn(targetPosition.Value);
        }
    }
    
    private Vector2? GetArmyPosition(ArmyState army)
    {
        if (army.LocationType == LocationType.Region)
        {
            foreach (var system in _mapData.StarSystems)
            {
                foreach (var body in system.StellarBodies)
                {
                    foreach (var region in body.Regions)
                    {
                        if (region.Id == army.LocationId)
                        {
                            return region.Position;
                        }
                    }
                }
            }
        }
        else if (army.LocationType == LocationType.HyperspaceLaneMouth)
        {
            foreach (var lane in _mapData.HyperspaceLanes)
            {
                if (lane.MouthAId == army.LocationId)
                {
                    return lane.MouthAPosition;
                }
                if (lane.MouthBId == army.LocationId)
                {
                    return lane.MouthBPosition;
                }
            }
        }
        
        return null;
    }
}

public class SelectionState
{
    public ArmyState? SelectedArmy { get; private set; }
    public RegionData? SelectedRegion { get; private set; }
    public string? SelectedHyperspaceLaneMouthId { get; private set; }
    public Vector2? SelectedHyperspaceLaneMouthPosition { get; private set; }
    public StellarBodyData? SelectedStellarBody { get; private set; }
    public StarSystemData? SelectedStarSystem { get; private set; }
    
    public SelectionType Type { get; private set; }
    
    public SelectionState()
    {
        Type = SelectionType.None;
    }
    
    public void SelectArmy(ArmyState army)
    {
        ClearSelection();
        SelectedArmy = army;
        Type = SelectionType.Army;
    }
    
    public void SelectRegion(RegionData region)
    {
        ClearSelection();
        SelectedRegion = region;
        Type = SelectionType.Region;
    }
    
    public void SelectHyperspaceLaneMouth(string mouthId, Vector2 position)
    {
        ClearSelection();
        SelectedHyperspaceLaneMouthId = mouthId;
        SelectedHyperspaceLaneMouthPosition = position;
        Type = SelectionType.HyperspaceLaneMouth;
    }
    
    public void SelectStellarBody(StellarBodyData body)
    {
        ClearSelection();
        SelectedStellarBody = body;
        Type = SelectionType.StellarBody;
    }
    
    public void SelectStarSystem(StarSystemData system)
    {
        ClearSelection();
        SelectedStarSystem = system;
        Type = SelectionType.StarSystem;
    }
    
    public void ClearSelection()
    {
        SelectedArmy = null;
        SelectedRegion = null;
        SelectedHyperspaceLaneMouthId = null;
        SelectedHyperspaceLaneMouthPosition = null;
        SelectedStellarBody = null;
        SelectedStarSystem = null;
        Type = SelectionType.None;
    }
}

public enum SelectionType
{
    None,
    Army,
    Region,
    HyperspaceLaneMouth,
    StellarBody,
    StarSystem
}
