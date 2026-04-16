namespace RiskyStars.Server;

public class GamePersistenceOptions
{
    public string SavePath { get; set; } = "GameSaves";
    public bool AutoSaveEnabled { get; set; } = true;
    public bool AutoRecoveryEnabled { get; set; } = true;
    public int MaxBackupsPerGame { get; set; } = 10;
}

public class SessionManagementOptions
{
    public int SessionTimeoutMinutes { get; set; } = 30;
    public int CleanupIntervalMinutes { get; set; } = 5;
    public int MaxActiveGames { get; set; } = 50;
    public int MaxPlayersPerGame { get; set; } = 8;
}

public class GrpcOptions
{
    public int MaxReceiveMessageSize { get; set; } = 16 * 1024 * 1024;
    public int MaxSendMessageSize { get; set; } = 16 * 1024 * 1024;
    public bool EnableDetailedErrors { get; set; } = false;
    public string[] CompressionProviders { get; set; } = new[] { "gzip" };
}

public class ServerOptions
{
    public int Port { get; set; } = 5000;
    public bool UseHttps { get; set; } = false;
    public int HttpsPort { get; set; } = 5001;
}
