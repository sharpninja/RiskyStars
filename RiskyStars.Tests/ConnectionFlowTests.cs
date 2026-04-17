using Xunit;
using RiskyStars.Client;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace RiskyStars.Tests;

public class ConnectionFlowTests : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    
    public ConnectionFlowTests()
    {
        var presentationParams = new PresentationParameters();
        _graphicsDevice = new GraphicsDevice(GraphicsAdapter.DefaultAdapter, GraphicsProfile.Reach, presentationParams);
    }

    [Fact]
    public void SetSinglePlayerMode_SetsCorrectState()
    {
        // Arrange
        var lobbyManager = new LobbyManager(_graphicsDevice, 1920, 1080);
        
        // Act
        lobbyManager.SetSinglePlayerMode();
        
        // Assert
        Assert.Equal(GameMode.SinglePlayer, lobbyManager.SelectedGameMode);
        Assert.Equal(LobbyState.SinglePlayerLobby, lobbyManager.State);
        Assert.False(lobbyManager.IsInGame);
    }

    [Fact]
    public void DefaultLobbyManager_IsInModeSelectionState()
    {
        // Arrange
        var lobbyManager = new LobbyManager(_graphicsDevice, 1920, 1080);
        
        // Assert
        Assert.Equal(GameMode.Multiplayer, lobbyManager.SelectedGameMode);
        Assert.Equal(LobbyState.ModeSelection, lobbyManager.State);
    }

    [Fact]
    public void SetSinglePlayerMode_CanBeCalledMultipleTimes()
    {
        // Arrange
        var lobbyManager = new LobbyManager(_graphicsDevice, 1920, 1080);
        
        // Act
        lobbyManager.SetSinglePlayerMode();
        lobbyManager.SetSinglePlayerMode();
        
        // Assert
        Assert.Equal(GameMode.SinglePlayer, lobbyManager.SelectedGameMode);
        Assert.Equal(LobbyState.SinglePlayerLobby, lobbyManager.State);
    }

    public void Dispose()
    {
        _graphicsDevice.Dispose();
    }
}