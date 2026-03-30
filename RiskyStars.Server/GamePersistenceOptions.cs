namespace RiskyStars.Server;

public class GamePersistenceOptions
{
    public string SavePath { get; set; } = "GameSaves";
    public bool AutoSaveEnabled { get; set; } = true;
    public bool AutoRecoveryEnabled { get; set; } = true;
    public int MaxBackupsPerGame { get; set; } = 10;
}
