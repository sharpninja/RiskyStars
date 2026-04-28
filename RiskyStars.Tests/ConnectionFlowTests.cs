using Xunit;
using RiskyStars.Client;

namespace RiskyStars.Tests;

public class ConnectionFlowTests
{
    [Fact]
    public void SetSinglePlayerMode_SetsCorrectState()
    {
        // Arrange
        var lobbyManager = LobbyManager.CreateHeadlessForTests();
        
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
        var lobbyManager = LobbyManager.CreateHeadlessForTests();
        
        // Assert
        Assert.Equal(GameMode.Multiplayer, lobbyManager.SelectedGameMode);
        Assert.Equal(LobbyState.ModeSelection, lobbyManager.State);
    }

    [Fact]
    public void SetSinglePlayerMode_CanBeCalledMultipleTimes()
    {
        // Arrange
        var lobbyManager = LobbyManager.CreateHeadlessForTests();
        
        // Act
        lobbyManager.SetSinglePlayerMode();
        lobbyManager.SetSinglePlayerMode();
        
        // Assert
        Assert.Equal(GameMode.SinglePlayer, lobbyManager.SelectedGameMode);
        Assert.Equal(LobbyState.SinglePlayerLobby, lobbyManager.State);
    }
}
