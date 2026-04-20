using Microsoft.Xna.Framework;

namespace RiskyStars.Client;

public class MapData
{
    public List<StarSystemData> StarSystems { get; set; } = new();
    public List<HyperspaceLaneData> HyperspaceLanes { get; set; } = new();
}

public class StarSystemData
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Vector2 Position { get; set; }
    public StarSystemType Type { get; set; }
    public List<StellarBodyData> StellarBodies { get; set; } = new();
}

public class StellarBodyData
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string StarSystemId { get; set; } = string.Empty;
    public StellarBodyType Type { get; set; }
    public Vector2 Position { get; set; }
    public List<RegionData> Regions { get; set; } = new();
}

public class RegionData
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string StellarBodyId { get; set; } = string.Empty;
    public Vector2 Position { get; set; }
}

public class HyperspaceLaneData
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string StarSystemAId { get; set; } = string.Empty;
    public string StarSystemBId { get; set; } = string.Empty;
    public string MouthAId { get; set; } = string.Empty;
    public string MouthBId { get; set; } = string.Empty;
    public Vector2 MouthAPosition { get; set; }
    public Vector2 MouthBPosition { get; set; }
}

public enum StarSystemType
{
    Home,
    Featured,
    Minor
}

public enum StellarBodyType
{
    GasGiant,
    RockyPlanet,
    Planetoid,
    Comet
}
