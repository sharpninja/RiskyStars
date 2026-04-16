using RiskyStars.Server.Entities;

namespace RiskyStars.Server.Services;

public class AIEconomicManager
{
    private readonly GameStateEvaluator _gameStateEvaluator;
    private readonly StellarBodyUpgradeSystem _upgradeSystem;
    private readonly HeroManager _heroManager;
    private readonly ResourceManager _resourceManager;

    public AIEconomicManager(
        GameStateEvaluator gameStateEvaluator,
        StellarBodyUpgradeSystem upgradeSystem,
        HeroManager heroManager,
        ResourceManager resourceManager)
    {
        _gameStateEvaluator = gameStateEvaluator;
        _upgradeSystem = upgradeSystem;
        _heroManager = heroManager;
        _resourceManager = resourceManager;
    }

    public EconomicDecision MakeEconomicDecisions(Game game, AIPlayer aiPlayer)
    {
        var budget = CalculateEconomicBudget(aiPlayer);
        var paybackHorizon = GetPaybackHorizon(aiPlayer.DifficultyLevel);

        var upgradeDecisions = EvaluateUpgradeOpportunities(game, aiPlayer, budget, paybackHorizon);
        var heroAssignments = EvaluateHeroAssignments(game, aiPlayer);

        return new EconomicDecision
        {
            Budget = budget,
            PaybackHorizon = paybackHorizon,
            UpgradeDecisions = upgradeDecisions,
            HeroAssignments = heroAssignments
        };
    }

    private EconomicBudget CalculateEconomicBudget(AIPlayer aiPlayer)
    {
        double budgetPercentage = aiPlayer.DifficultyLevel switch
        {
            DifficultyLevel.Easy => 0.10,
            DifficultyLevel.Medium => 0.20,
            DifficultyLevel.Hard => 0.30,
            _ => 0.15
        };

        int populationBudget = (int)(aiPlayer.PopulationStockpile * budgetPercentage);
        int metalBudget = (int)(aiPlayer.MetalStockpile * budgetPercentage);
        int fuelBudget = (int)(aiPlayer.FuelStockpile * budgetPercentage);

        return new EconomicBudget
        {
            PopulationBudget = populationBudget,
            MetalBudget = metalBudget,
            FuelBudget = fuelBudget,
            BudgetPercentage = budgetPercentage
        };
    }

    private int GetPaybackHorizon(DifficultyLevel difficulty)
    {
        return difficulty switch
        {
            DifficultyLevel.Easy => 12,
            DifficultyLevel.Medium => 8,
            DifficultyLevel.Hard => 5,
            _ => 8
        };
    }

    private List<UpgradeDecision> EvaluateUpgradeOpportunities(
        Game game,
        AIPlayer aiPlayer,
        EconomicBudget budget,
        int paybackHorizon)
    {
        var ownedBodies = game.GetPlayerOwnedBodies(aiPlayer.Id).ToList();
        var upgradeOpportunities = new List<UpgradeEvaluation>();

        foreach (var body in ownedBodies)
        {
            for (int targetLevel = body.UpgradeLevel + 1; targetLevel <= 3; targetLevel++)
            {
                if (_upgradeSystem.CanUpgrade(body, aiPlayer, targetLevel))
                {
                    var evaluation = EvaluateUpgrade(body, aiPlayer, targetLevel, paybackHorizon);
                    upgradeOpportunities.Add(evaluation);
                }
            }
        }

        upgradeOpportunities = upgradeOpportunities
            .OrderByDescending(e => e.ROI)
            .ThenByDescending(e => e.ProductionIncrease)
            .ToList();

        var decisions = new List<UpgradeDecision>();
        var remainingBudget = budget;

        foreach (var opportunity in upgradeOpportunities)
        {
            var cost = _upgradeSystem.GetUpgradeCost(opportunity.StellarBody, opportunity.TargetLevel);

            if (CanAffordWithBudget(cost, remainingBudget))
            {
                decisions.Add(new UpgradeDecision
                {
                    StellarBodyId = opportunity.StellarBody.Id,
                    TargetLevel = opportunity.TargetLevel,
                    Cost = cost,
                    ROI = opportunity.ROI,
                    PaybackPeriod = opportunity.PaybackPeriod,
                    ProductionIncrease = opportunity.ProductionIncrease,
                    Priority = CalculateUpgradePriority(opportunity)
                });

                remainingBudget.PopulationBudget -= cost.population;
                remainingBudget.MetalBudget -= cost.metal;
                remainingBudget.FuelBudget -= cost.fuel;
            }
        }

        return decisions.OrderByDescending(d => d.Priority).ToList();
    }

    private UpgradeEvaluation EvaluateUpgrade(
        StellarBody body,
        AIPlayer aiPlayer,
        int targetLevel,
        int paybackHorizon)
    {
        var cost = _upgradeSystem.GetUpgradeCost(body, targetLevel);
        int totalCost = cost.population + cost.metal + cost.fuel;

        int currentProduction = body.CalculateTotalProduction();
        
        int currentLevel = body.UpgradeLevel;
        body.UpgradeLevel = targetLevel;
        int upgradedProduction = body.CalculateTotalProduction();
        body.UpgradeLevel = currentLevel;

        int productionIncrease = upgradedProduction - currentProduction;

        double paybackPeriod = productionIncrease > 0 
            ? (double)totalCost / productionIncrease 
            : double.MaxValue;

        double roi = paybackPeriod <= paybackHorizon && paybackPeriod > 0
            ? (productionIncrease * paybackHorizon - totalCost) / (double)totalCost
            : -1.0;

        return new UpgradeEvaluation
        {
            StellarBody = body,
            TargetLevel = targetLevel,
            Cost = totalCost,
            ProductionIncrease = productionIncrease,
            PaybackPeriod = paybackPeriod,
            ROI = roi
        };
    }

    private double CalculateUpgradePriority(UpgradeEvaluation evaluation)
    {
        double priority = evaluation.ROI * 100;
        
        priority += evaluation.ProductionIncrease * 2;
        
        priority -= evaluation.PaybackPeriod * 10;
        
        int regionCount = evaluation.StellarBody.GetRegionCount();
        priority += regionCount * 5;

        return priority;
    }

    private bool CanAffordWithBudget((int population, int metal, int fuel) cost, EconomicBudget budget)
    {
        return cost.population <= budget.PopulationBudget &&
               cost.metal <= budget.MetalBudget &&
               cost.fuel <= budget.FuelBudget;
    }

    private List<HeroAssignment> EvaluateHeroAssignments(Game game, AIPlayer aiPlayer)
    {
        var assignments = new List<HeroAssignment>();
        var unassignedHeroes = aiPlayer.Heroes.Where(h => h.AssignedStellarBodyId == null).ToList();
        
        if (unassignedHeroes.Count == 0)
        {
            return assignments;
        }

        var ownedBodies = game.GetPlayerOwnedBodies(aiPlayer.Id).ToList();
        var bodyEvaluations = new List<(StellarBody body, double territoryValue, int currentProduction)>();

        foreach (var body in ownedBodies)
        {
            if (body.CanAssignHero(null!))
            {
                var starSystem = game.StarSystems.FirstOrDefault(s => s.Id == body.StarSystemId);
                double territoryValue = 0;

                if (starSystem != null)
                {
                    var regionsInSystem = starSystem.StellarBodies
                        .SelectMany(b => b.Regions)
                        .Where(r => r.OwnerId == aiPlayer.Id)
                        .Count();
                    
                    territoryValue = regionsInSystem * 10;
                    
                    bool isFrontline = IsBodyOnFrontline(game, body, aiPlayer.Id);
                    if (isFrontline)
                    {
                        territoryValue += 50;
                    }

                    bool hasMultipleResourceTypes = starSystem.StellarBodies
                        .Where(b => b.Regions.Any(r => r.OwnerId == aiPlayer.Id))
                        .Select(b => b.ResourceType)
                        .Distinct()
                        .Count() > 1;
                    
                    if (hasMultipleResourceTypes)
                    {
                        territoryValue += 25;
                    }
                }

                int currentProduction = body.CalculateTotalProduction();
                territoryValue += currentProduction * 2;

                bodyEvaluations.Add((body, territoryValue, currentProduction));
            }
        }

        bodyEvaluations = bodyEvaluations
            .OrderByDescending(e => e.currentProduction)
            .ThenByDescending(e => e.territoryValue)
            .ToList();

        foreach (var hero in unassignedHeroes)
        {
            var bestMatch = FindBestBodyForHero(hero, bodyEvaluations, game, aiPlayer);
            
            if (bestMatch.body != null)
            {
                assignments.Add(new HeroAssignment
                {
                    HeroId = hero.Id,
                    StellarBodyId = bestMatch.body.Id,
                    TerritoryValue = bestMatch.territoryValue,
                    ExpectedProductionIncrease = CalculateHeroProductionIncrease(hero, bestMatch.body)
                });

                bodyEvaluations.RemoveAll(e => e.body.Id == bestMatch.body.Id);
            }
        }

        return assignments;
    }

    private (StellarBody? body, double territoryValue) FindBestBodyForHero(
        Hero hero,
        List<(StellarBody body, double territoryValue, int currentProduction)> bodyEvaluations,
        Game game,
        AIPlayer aiPlayer)
    {
        foreach (var evaluation in bodyEvaluations)
        {
            if (_heroManager.CanAssignHeroToBody(hero, evaluation.body, aiPlayer))
            {
                double adjustedValue = evaluation.territoryValue;
                
                if (hero.Class == HeroClass.ClassI || hero.Class == HeroClass.ClassIII)
                {
                    adjustedValue += hero.FixedResourceAmount * 5;
                }
                
                if (hero.Class == HeroClass.ClassII || hero.Class == HeroClass.ClassIII)
                {
                    adjustedValue += evaluation.currentProduction * 0.25 * 3;
                }

                return (evaluation.body, adjustedValue);
            }
        }

        return (null, 0);
    }

    private bool IsBodyOnFrontline(Game game, StellarBody body, string playerId)
    {
        foreach (var region in body.Regions)
        {
            if (region.OwnerId != playerId)
            {
                continue;
            }

            bool hasEnemyNeighbor = body.Regions.Any(r => r.OwnerId != playerId && r.OwnerId != null);
            if (hasEnemyNeighbor)
            {
                return true;
            }
        }

        var starSystem = game.StarSystems.FirstOrDefault(s => s.Id == body.StarSystemId);
        if (starSystem != null)
        {
            foreach (var lane in starSystem.HyperspaceLanes)
            {
                var oppositeSystemId = lane.GetOppositeStarSystemId(starSystem.Id);
                var oppositeSystem = game.StarSystems.FirstOrDefault(s => s.Id == oppositeSystemId);

                if (oppositeSystem != null)
                {
                    bool hasEnemyInOppositeSystem = oppositeSystem.StellarBodies
                        .SelectMany(b => b.Regions)
                        .Any(r => r.OwnerId != playerId && r.OwnerId != null);

                    if (hasEnemyInOppositeSystem)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private int CalculateHeroProductionIncrease(Hero hero, StellarBody body)
    {
        int currentProduction = body.CalculateTotalProduction();
        int increase = 0;

        if (hero.Class == HeroClass.ClassI || hero.Class == HeroClass.ClassIII)
        {
            increase += hero.FixedResourceAmount;
        }

        if (hero.Class == HeroClass.ClassII || hero.Class == HeroClass.ClassIII)
        {
            increase += (int)(currentProduction * 0.25);
        }

        return increase;
    }

    public void ExecuteEconomicDecisions(Game game, AIPlayer aiPlayer, EconomicDecision decision)
    {
        foreach (var upgrade in decision.UpgradeDecisions)
        {
            var body = game.StarSystems
                .SelectMany(s => s.StellarBodies)
                .FirstOrDefault(b => b.Id == upgrade.StellarBodyId);

            if (body != null && _upgradeSystem.CanUpgrade(body, aiPlayer, upgrade.TargetLevel))
            {
                _upgradeSystem.ApplyUpgrade(body, aiPlayer, upgrade.TargetLevel);
            }
        }

        foreach (var assignment in decision.HeroAssignments)
        {
            var hero = _heroManager.GetHeroById(aiPlayer, assignment.HeroId);
            var body = game.StarSystems
                .SelectMany(s => s.StellarBodies)
                .FirstOrDefault(b => b.Id == assignment.StellarBodyId);

            if (hero != null && body != null && _heroManager.CanAssignHeroToBody(hero, body, aiPlayer))
            {
                _heroManager.AssignHeroToBody(hero, body, aiPlayer);
            }
        }
    }
}

public class EconomicDecision
{
    public EconomicBudget Budget { get; set; } = new();
    public int PaybackHorizon { get; set; }
    public List<UpgradeDecision> UpgradeDecisions { get; set; } = new();
    public List<HeroAssignment> HeroAssignments { get; set; } = new();
}

public class EconomicBudget
{
    public int PopulationBudget { get; set; }
    public int MetalBudget { get; set; }
    public int FuelBudget { get; set; }
    public double BudgetPercentage { get; set; }
}

public class UpgradeDecision
{
    public string StellarBodyId { get; set; } = string.Empty;
    public int TargetLevel { get; set; }
    public (int population, int metal, int fuel) Cost { get; set; }
    public double ROI { get; set; }
    public double PaybackPeriod { get; set; }
    public int ProductionIncrease { get; set; }
    public double Priority { get; set; }
}

public class UpgradeEvaluation
{
    public StellarBody StellarBody { get; set; } = null!;
    public int TargetLevel { get; set; }
    public int Cost { get; set; }
    public int ProductionIncrease { get; set; }
    public double PaybackPeriod { get; set; }
    public double ROI { get; set; }
}

public class HeroAssignment
{
    public string HeroId { get; set; } = string.Empty;
    public string StellarBodyId { get; set; } = string.Empty;
    public double TerritoryValue { get; set; }
    public int ExpectedProductionIncrease { get; set; }
}
