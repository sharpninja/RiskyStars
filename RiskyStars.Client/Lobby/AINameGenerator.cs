namespace RiskyStars.Client;

public static class AINameGenerator
{
    private static readonly string[] Prefixes = new[]
    {
        "Commander", "Admiral", "Captain", "General", "Colonel",
        "Marshal", "Warlord", "Duke", "Baron", "Lord",
        "Emperor", "Regent", "Overlord", "Archon", "Praetor"
    };

    private static readonly string[] Names = new[]
    {
        "Nexus", "Vortex", "Omega", "Sigma", "Alpha",
        "Delta", "Gamma", "Zeta", "Phoenix", "Titan",
        "Atlas", "Orion", "Nova", "Stellar", "Quantum",
        "Vector", "Matrix", "Apex", "Zenith", "Prime",
        "Eclipse", "Nebula", "Pulsar", "Quasar", "Void",
        "Cipher", "Ghost", "Shadow", "Storm", "Fury",
        "Havoc", "Chaos", "Raven", "Falcon", "Eagle",
        "Drake", "Wyrm", "Hydra", "Basilisk", "Kraken"
    };

    private static readonly string[] Suffixes = new[]
    {
        "the Bold", "the Wise", "the Swift", "the Strong", "the Cunning",
        "the Fierce", "the Ruthless", "the Merciless", "the Unstoppable", "the Eternal",
        "VII", "IX", "XII", "XIV", "XVI",
        "Prime", "Supreme", "Ultimate", "Elite", "Apex"
    };

    private static readonly Random Random = new Random();

    public static string GenerateName(int slotIndex, string difficulty)
    {
        int style = Random.Next(0, 3);

        return style switch
        {
            0 => $"{Prefixes[Random.Next(Prefixes.Length)]} {Names[Random.Next(Names.Length)]}",
            1 => $"{Names[Random.Next(Names.Length)]} {Suffixes[Random.Next(Suffixes.Length)]}",
            _ => Names[Random.Next(Names.Length)]
        };
    }

    public static string GenerateNameWithSeed(int slotIndex, string difficulty)
    {
        var seededRandom = new Random(slotIndex * 1000 + difficulty.GetHashCode());
        int style = seededRandom.Next(0, 3);

        return style switch
        {
            0 => $"{Prefixes[seededRandom.Next(Prefixes.Length)]} {Names[seededRandom.Next(Names.Length)]}",
            1 => $"{Names[seededRandom.Next(Names.Length)]} {Suffixes[seededRandom.Next(Suffixes.Length)]}",
            _ => Names[seededRandom.Next(Names.Length)]
        };
    }
}
