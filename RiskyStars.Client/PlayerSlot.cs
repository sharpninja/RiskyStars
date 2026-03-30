namespace RiskyStars.Client;

public enum PlayerType
{
    Human,
    EasyAI,
    MediumAI,
    HardAI
}

public class PlayerSlot
{
    public int SlotIndex { get; set; }
    public PlayerType PlayerType { get; set; }
    public string PlayerName { get; set; }
    public bool IsReady { get; set; }
    public bool IsHost { get; set; }

    public PlayerSlot(int slotIndex)
    {
        SlotIndex = slotIndex;
        PlayerType = PlayerType.Human;
        PlayerName = $"Player {slotIndex}";
        IsReady = false;
        IsHost = false;
    }

    public bool IsAI => PlayerType != PlayerType.Human;

    public string GetDifficultyLevel()
    {
        return PlayerType switch
        {
            PlayerType.EasyAI => "Easy",
            PlayerType.MediumAI => "Medium",
            PlayerType.HardAI => "Hard",
            _ => ""
        };
    }

    public string GetDisplayText()
    {
        if (IsAI)
        {
            return $"{PlayerName} [AI - {GetDifficultyLevel()}]";
        }
        return PlayerName;
    }
}
