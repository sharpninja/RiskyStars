using System.Text.Json.Serialization;

namespace RiskyStars.Server.Entities;

public class AIPlayer : Player
{
    [JsonPropertyName("difficultyLevel")]
    public DifficultyLevel DifficultyLevel { get; set; }

    [JsonPropertyName("aiConfiguration")]
    public AIConfiguration AIConfiguration { get; set; } = new();

    public AIPlayer()
    {
    }

    public AIPlayer(DifficultyLevel difficulty)
    {
        DifficultyLevel = difficulty;
        AIConfiguration = AIConfiguration.CreateForDifficulty(difficulty);
    }
}
