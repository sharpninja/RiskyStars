using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using RiskyStars.Server.Entities;

namespace RiskyStars.Server.Services;

public class GameRepository
{
    private readonly string _savePath;
    private readonly int _maxBackups;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<GameRepository> _logger;

    public GameRepository(ILogger<GameRepository> logger, IOptions<GamePersistenceOptions> options)
    {
        _logger = logger;
        var opts = options.Value;
        _savePath = Path.IsPathRooted(opts.SavePath) 
            ? opts.SavePath 
            : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, opts.SavePath);
        _maxBackups = opts.MaxBackupsPerGame;
        
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        EnsureSaveDirectoryExists();
    }

    private void EnsureSaveDirectoryExists()
    {
        if (!Directory.Exists(_savePath))
        {
            Directory.CreateDirectory(_savePath);
            _logger.LogInformation("Created game save directory at: {SavePath}", _savePath);
        }
    }

    public async Task<bool> SaveGameStateAsync(Game game, CombatManager? combatManager = null)
    {
        try
        {
            var combatSnapshots = new List<CombatSessionSnapshot>();
            
            if (combatManager != null)
            {
                var activeCombatLocations = combatManager.GetActiveCombatLocations();
                foreach (var location in activeCombatLocations)
                {
                    var session = combatManager.GetCombatSession(location);
                    if (session != null)
                    {
                        combatSnapshots.Add(CombatSessionSnapshot.FromCombatSession(session));
                    }
                }
            }

            var snapshot = GameStateSnapshot.FromGame(game, combatSnapshots);
            var fileName = GetGameFileName(game.Id);
            var filePath = Path.Combine(_savePath, fileName);

            var json = JsonSerializer.Serialize(snapshot, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json);

            _logger.LogInformation("Saved game {GameId} at turn {TurnNumber} to {FilePath}", 
                game.Id, game.TurnNumber, filePath);

            await CreateBackupAsync(game.Id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save game state for game {GameId}", game.Id);
            return false;
        }
    }

    public async Task<(Game? Game, List<CombatSessionSnapshot> CombatSessions)?> LoadGameStateAsync(string gameId)
    {
        try
        {
            var fileName = GetGameFileName(gameId);
            var filePath = Path.Combine(_savePath, fileName);

            if (!File.Exists(filePath))
            {
                _logger.LogWarning("No saved game file found for game {GameId} at {FilePath}", gameId, filePath);
                return null;
            }

            var json = await File.ReadAllTextAsync(filePath);
            var snapshot = JsonSerializer.Deserialize<GameStateSnapshot>(json, _jsonOptions);

            if (snapshot == null)
            {
                _logger.LogError("Failed to deserialize game state for game {GameId}", gameId);
                return null;
            }

            if (snapshot.Version != 1)
            {
                _logger.LogWarning("Game state version mismatch for game {GameId}. Expected 1, got {Version}", 
                    gameId, snapshot.Version);
            }

            var game = snapshot.ToGame();
            _logger.LogInformation("Loaded game {GameId} at turn {TurnNumber} from {FilePath}", 
                gameId, game.TurnNumber, filePath);

            return (game, snapshot.ActiveCombats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load game state for game {GameId}", gameId);
            return null;
        }
    }

    public Task<List<string>> GetAllSavedGameIdsAsync()
    {
        try
        {
            var files = Directory.GetFiles(_savePath, "game_*.json");
            var gameIds = new List<string>();

            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                if (fileName.StartsWith("game_") && fileName.EndsWith(".json"))
                {
                    var gameId = fileName.Substring(5, fileName.Length - 10);
                    gameIds.Add(gameId);
                }
            }

            return Task.FromResult(gameIds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get saved game IDs");
            return Task.FromResult(new List<string>());
        }
    }

    public Task<bool> DeleteGameStateAsync(string gameId)
    {
        try
        {
            var fileName = GetGameFileName(gameId);
            var filePath = Path.Combine(_savePath, fileName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("Deleted saved game {GameId}", gameId);

                var backupFiles = Directory.GetFiles(_savePath, $"game_{gameId}_backup_*.json");
                foreach (var backupFile in backupFiles)
                {
                    File.Delete(backupFile);
                }

                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete game state for game {GameId}", gameId);
            return Task.FromResult(false);
        }
    }

    public Task<bool> RestoreFromBackupAsync(string gameId, DateTime? backupTime = null)
    {
        try
        {
            var backupFiles = Directory.GetFiles(_savePath, $"game_{gameId}_backup_*.json")
                .OrderByDescending(f => File.GetLastWriteTimeUtc(f))
                .ToList();

            if (!backupFiles.Any())
            {
                _logger.LogWarning("No backup files found for game {GameId}", gameId);
                return Task.FromResult(false);
            }

            string backupFile;
            if (backupTime.HasValue)
            {
                backupFile = backupFiles.FirstOrDefault(f => 
                    Math.Abs((File.GetLastWriteTimeUtc(f) - backupTime.Value).TotalSeconds) < 60) 
                    ?? backupFiles.First();
            }
            else
            {
                backupFile = backupFiles.First();
            }

            var fileName = GetGameFileName(gameId);
            var filePath = Path.Combine(_savePath, fileName);

            File.Copy(backupFile, filePath, overwrite: true);

            _logger.LogInformation("Restored game {GameId} from backup {BackupFile}", gameId, backupFile);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore game {GameId} from backup", gameId);
            return Task.FromResult(false);
        }
    }

    private Task CreateBackupAsync(string gameId)
    {
        try
        {
            var fileName = GetGameFileName(gameId);
            var filePath = Path.Combine(_savePath, fileName);

            if (!File.Exists(filePath))
                return Task.CompletedTask;

            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var backupFileName = $"game_{gameId}_backup_{timestamp}.json";
            var backupFilePath = Path.Combine(_savePath, backupFileName);

            File.Copy(filePath, backupFilePath, overwrite: false);

            CleanupOldBackups(gameId);
            
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create backup for game {GameId}", gameId);
            return Task.CompletedTask;
        }
    }

    private void CleanupOldBackups(string gameId)
    {
        try
        {
            var backupFiles = Directory.GetFiles(_savePath, $"game_{gameId}_backup_*.json")
                .OrderByDescending(f => File.GetLastWriteTimeUtc(f))
                .ToList();

            if (backupFiles.Count > _maxBackups)
            {
                var filesToDelete = backupFiles.Skip(_maxBackups);
                foreach (var file in filesToDelete)
                {
                    File.Delete(file);
                }

                _logger.LogDebug("Cleaned up {Count} old backup files for game {GameId}", 
                    backupFiles.Count - _maxBackups, gameId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cleanup old backups for game {GameId}", gameId);
        }
    }

    private string GetGameFileName(string gameId)
    {
        return $"game_{gameId}.json";
    }

    public async Task<GameStateMetadata?> GetGameMetadataAsync(string gameId)
    {
        try
        {
            var fileName = GetGameFileName(gameId);
            var filePath = Path.Combine(_savePath, fileName);

            if (!File.Exists(filePath))
                return null;

            var json = await File.ReadAllTextAsync(filePath);
            var snapshot = JsonSerializer.Deserialize<GameStateSnapshot>(json, _jsonOptions);

            if (snapshot == null)
                return null;

            var fileInfo = new FileInfo(filePath);

            return new GameStateMetadata
            {
                GameId = snapshot.GameId,
                TurnNumber = snapshot.TurnNumber,
                CurrentPhase = snapshot.CurrentPhase,
                PlayerCount = snapshot.Players.Count,
                SavedAt = snapshot.SavedAt,
                FileSize = fileInfo.Length
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get metadata for game {GameId}", gameId);
            return null;
        }
    }

    public async Task<List<GameStateMetadata>> GetAllGameMetadataAsync()
    {
        var gameIds = await GetAllSavedGameIdsAsync();
        var metadata = new List<GameStateMetadata>();

        foreach (var gameId in gameIds)
        {
            var meta = await GetGameMetadataAsync(gameId);
            if (meta != null)
            {
                metadata.Add(meta);
            }
        }

        return metadata.OrderByDescending(m => m.SavedAt).ToList();
    }
}

public class GameStateMetadata
{
    public string GameId { get; set; } = string.Empty;
    public int TurnNumber { get; set; }
    public TurnPhase CurrentPhase { get; set; }
    public int PlayerCount { get; set; }
    public DateTime SavedAt { get; set; }
    public long FileSize { get; set; }
}
