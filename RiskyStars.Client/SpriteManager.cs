using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace RiskyStars.Client;

public class SpriteManager
{
    private readonly ContentManager _content;
    private readonly Dictionary<string, Texture2D> _textureCache = new();

    public SpriteManager(ContentManager content)
    {
        _content = content;
    }

    public void LoadAllSprites()
    {
        LoadStellarBodies();
        LoadArmies();
        LoadUI();
        LoadHyperspaceLanes();
        LoadCombat();
    }

    private void LoadStellarBodies()
    {
        LoadTexture("Sprites/StellarBodies/GasGiant");
        LoadTexture("Sprites/StellarBodies/GasGiant_Variant1");
        LoadTexture("Sprites/StellarBodies/GasGiant_Variant2");
        LoadTexture("Sprites/StellarBodies/RockyPlanet");
        LoadTexture("Sprites/StellarBodies/RockyPlanet_Variant1");
        LoadTexture("Sprites/StellarBodies/RockyPlanet_Variant2");
        LoadTexture("Sprites/StellarBodies/Planetoid");
        LoadTexture("Sprites/StellarBodies/Comet");
    }

    private void LoadArmies()
    {
        LoadTexture("Sprites/Armies/Army");
        LoadTexture("Sprites/Armies/Hero");
    }

    private void LoadUI()
    {
        LoadTexture("Sprites/UI/ButtonNormal");
        LoadTexture("Sprites/UI/ButtonHover");
        LoadTexture("Sprites/UI/ButtonPressed");
        LoadTexture("Sprites/UI/Panel");
        LoadTexture("Sprites/UI/IconProduction");
        LoadTexture("Sprites/UI/IconEnergy");
    }

    private void LoadHyperspaceLanes()
    {
        LoadTexture("Sprites/HyperspaceLanes/Lane");
        LoadTexture("Sprites/HyperspaceLanes/LaneMouth");
    }

    private void LoadCombat()
    {
        LoadTexture("Sprites/Combat/Hit");
        LoadTexture("Sprites/Combat/Miss");
        LoadTexture("Sprites/Combat/Explosion");
        LoadTexture("Sprites/Combat/DiceRoll");
    }

    private void LoadTexture(string assetName)
    {
        try
        {
            var texture = _content.Load<Texture2D>(assetName);
            _textureCache[assetName] = texture;
        }
        catch (ContentLoadException ex)
        {
            Console.WriteLine($"Warning: Failed to load texture '{assetName}': {ex.Message}");
        }
    }

    public Texture2D? GetTexture(string assetName)
    {
        return _textureCache.TryGetValue(assetName, out var texture) ? texture : null;
    }

    public Texture2D? GetStellarBodyTexture(StellarBodyType bodyType, int variant = 0)
    {
        return bodyType switch
        {
            StellarBodyType.GasGiant when variant == 1 => GetTexture("Sprites/StellarBodies/GasGiant_Variant1"),
            StellarBodyType.GasGiant when variant == 2 => GetTexture("Sprites/StellarBodies/GasGiant_Variant2"),
            StellarBodyType.GasGiant => GetTexture("Sprites/StellarBodies/GasGiant"),
            StellarBodyType.RockyPlanet when variant == 1 => GetTexture("Sprites/StellarBodies/RockyPlanet_Variant1"),
            StellarBodyType.RockyPlanet when variant == 2 => GetTexture("Sprites/StellarBodies/RockyPlanet_Variant2"),
            StellarBodyType.RockyPlanet => GetTexture("Sprites/StellarBodies/RockyPlanet"),
            StellarBodyType.Planetoid => GetTexture("Sprites/StellarBodies/Planetoid"),
            StellarBodyType.Comet => GetTexture("Sprites/StellarBodies/Comet"),
            _ => null
        };
    }

    public Texture2D? GetArmyTexture(bool isHero = false)
    {
        return isHero 
            ? GetTexture("Sprites/Armies/Hero") 
            : GetTexture("Sprites/Armies/Army");
    }

    public Texture2D? GetButtonTexture(UIButtonState state)
    {
        return state switch
        {
            UIButtonState.Normal => GetTexture("Sprites/UI/ButtonNormal"),
            UIButtonState.Hover => GetTexture("Sprites/UI/ButtonHover"),
            UIButtonState.Pressed => GetTexture("Sprites/UI/ButtonPressed"),
            _ => GetTexture("Sprites/UI/ButtonNormal")
        };
    }

    public Texture2D? GetUITexture(UIElement element)
    {
        return element switch
        {
            UIElement.Panel => GetTexture("Sprites/UI/Panel"),
            UIElement.IconProduction => GetTexture("Sprites/UI/IconProduction"),
            UIElement.IconEnergy => GetTexture("Sprites/UI/IconEnergy"),
            _ => null
        };
    }

    public Texture2D? GetHyperspaceLaneTexture()
    {
        return GetTexture("Sprites/HyperspaceLanes/Lane");
    }

    public Texture2D? GetHyperspaceLaneMouthTexture()
    {
        return GetTexture("Sprites/HyperspaceLanes/LaneMouth");
    }

    public Texture2D? GetCombatEffectTexture(CombatEffect effect)
    {
        return effect switch
        {
            CombatEffect.Hit => GetTexture("Sprites/Combat/Hit"),
            CombatEffect.Miss => GetTexture("Sprites/Combat/Miss"),
            CombatEffect.Explosion => GetTexture("Sprites/Combat/Explosion"),
            CombatEffect.DiceRoll => GetTexture("Sprites/Combat/DiceRoll"),
            _ => null
        };
    }

    public void UnloadAll()
    {
        _textureCache.Clear();
    }
}

public enum UIButtonState
{
    Normal,
    Hover,
    Pressed
}

public enum UIElement
{
    Panel,
    IconProduction,
    IconEnergy
}

public enum CombatEffect
{
    Hit,
    Miss,
    Explosion,
    DiceRoll
}
