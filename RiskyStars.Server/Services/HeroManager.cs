using RiskyStars.Server.Entities;

namespace RiskyStars.Server.Services;

public class HeroManager
{
    public Hero CreateHero(string id, string name, HeroClass heroClass, int fixedResourceAmount = 0)
    {
        return new Hero
        {
            Id = id,
            Name = name,
            Class = heroClass,
            FixedResourceAmount = fixedResourceAmount,
            AssignedStellarBodyId = null
        };
    }

    public Hero CreateClassIHero(string id, string name, int fixedResourceAmount)
    {
        return CreateHero(id, name, HeroClass.ClassI, fixedResourceAmount);
    }

    public Hero CreateClassIIHero(string id, string name)
    {
        return CreateHero(id, name, HeroClass.ClassII);
    }

    public Hero CreateClassIIIHero(string id, string name, int fixedResourceAmount)
    {
        return CreateHero(id, name, HeroClass.ClassIII, fixedResourceAmount);
    }

    public bool CanAssignHeroToBody(Hero hero, StellarBody stellarBody, Player player)
    {
        if (hero.AssignedStellarBodyId != null)
        {
            return false;
        }

        if (!player.Heroes.Contains(hero))
        {
            return false;
        }

        if (!stellarBody.Regions.Any(r => r.OwnerId == player.Id))
        {
            return false;
        }

        if (stellarBody.Heroes.Count >= 3)
        {
            return false;
        }

        if (stellarBody.Heroes.Count >= stellarBody.GetRegionCount())
        {
            return false;
        }

        return true;
    }

    public void AssignHeroToBody(Hero hero, StellarBody stellarBody, Player player)
    {
        if (!CanAssignHeroToBody(hero, stellarBody, player))
        {
            throw new InvalidOperationException("Cannot assign hero to stellar body");
        }

        hero.AssignedStellarBodyId = stellarBody.Id;
        stellarBody.Heroes.Add(hero);
    }

    public void UnassignHeroFromBody(Hero hero, StellarBody stellarBody)
    {
        if (hero.AssignedStellarBodyId != stellarBody.Id)
        {
            throw new InvalidOperationException("Hero is not assigned to this stellar body");
        }

        hero.AssignedStellarBodyId = null;
        stellarBody.Heroes.Remove(hero);
    }

    public void RemoveHeroesFromBody(StellarBody stellarBody, Player previousOwner)
    {
        var heroesToRemove = stellarBody.Heroes.ToList();

        foreach (var hero in heroesToRemove)
        {
            hero.AssignedStellarBodyId = null;
            previousOwner.Heroes.Remove(hero);
        }

        stellarBody.Heroes.Clear();
    }

    public void HandleBodyCapture(StellarBody stellarBody, Player previousOwner, Player newOwner)
    {
        RemoveHeroesFromBody(stellarBody, previousOwner);
    }

    public List<Hero> GetHeroesOnBody(StellarBody stellarBody)
    {
        return stellarBody.Heroes.ToList();
    }

    public Hero? GetHeroById(Player player, string heroId)
    {
        return player.Heroes.FirstOrDefault(h => h.Id == heroId);
    }

    public void AddHeroToPlayer(Player player, Hero hero)
    {
        if (!player.Heroes.Contains(hero))
        {
            player.Heroes.Add(hero);
        }
    }

    public void RemoveHeroFromPlayer(Player player, Hero hero)
    {
        if (hero.AssignedStellarBodyId != null)
        {
            throw new InvalidOperationException("Cannot remove hero that is currently assigned to a stellar body");
        }

        player.Heroes.Remove(hero);
    }
}
