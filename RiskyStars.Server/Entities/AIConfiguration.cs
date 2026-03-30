using System.Text.Json.Serialization;

namespace RiskyStars.Server.Entities;

public class AIConfiguration
{
    [JsonPropertyName("aggressivenessWeight")]
    public double AggressivenessWeight { get; set; }

    [JsonPropertyName("expansionPriority")]
    public double ExpansionPriority { get; set; }

    [JsonPropertyName("defenseThreshold")]
    public double DefenseThreshold { get; set; }

    [JsonPropertyName("upgradeInvestmentRatio")]
    public double UpgradeInvestmentRatio { get; set; }

    [JsonPropertyName("decisionRandomnessFactor")]
    public double DecisionRandomnessFactor { get; set; }

    [JsonPropertyName("planningHorizonTurns")]
    public int PlanningHorizonTurns { get; set; }

    public static AIConfiguration CreateForDifficulty(DifficultyLevel difficulty)
    {
        return difficulty switch
        {
            DifficultyLevel.Easy => new AIConfiguration
            {
                AggressivenessWeight = 0.3,
                ExpansionPriority = 0.5,
                DefenseThreshold = 0.8,
                UpgradeInvestmentRatio = 0.2,
                DecisionRandomnessFactor = 0.4,
                PlanningHorizonTurns = 2
            },
            DifficultyLevel.Medium => new AIConfiguration
            {
                AggressivenessWeight = 0.6,
                ExpansionPriority = 0.7,
                DefenseThreshold = 0.6,
                UpgradeInvestmentRatio = 0.4,
                DecisionRandomnessFactor = 0.2,
                PlanningHorizonTurns = 4
            },
            DifficultyLevel.Hard => new AIConfiguration
            {
                AggressivenessWeight = 0.9,
                ExpansionPriority = 0.9,
                DefenseThreshold = 0.4,
                UpgradeInvestmentRatio = 0.6,
                DecisionRandomnessFactor = 0.05,
                PlanningHorizonTurns = 6
            },
            _ => throw new ArgumentOutOfRangeException(nameof(difficulty))
        };
    }
}
