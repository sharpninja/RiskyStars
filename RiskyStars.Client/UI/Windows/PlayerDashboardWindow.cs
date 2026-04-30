using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI;
using Myra.Graphics2D;
using RiskyStars.Shared;

namespace RiskyStars.Client;

public class PlayerDashboardWindow : DockableWindow
{
    private readonly GrpcGameClient _gameClient;
    
    private Label? _populationStockpileLabel;
    private Label? _populationRateLabel;
    private Label? _metalStockpileLabel;
    private Label? _metalRateLabel;
    private Label? _fuelStockpileLabel;
    private Label? _fuelRateLabel;
    private Label? _territoriesLabel;
    private Label? _armiesLabel;
    
    private Myra.Graphics2D.UI.Button? _buy1Button;
    private Myra.Graphics2D.UI.Button? _buy5Button;
    private Myra.Graphics2D.UI.Button? _buy10Button;
    private Myra.Graphics2D.UI.Button? _buy25Button;
    private Label? _purchasePhaseLabel;
    
    private Myra.Graphics2D.UI.Button? _assignToArmyButton;
    private Myra.Graphics2D.UI.Button? _assignToRegionButton;
    private Myra.Graphics2D.UI.Button? _recallHeroButton;
    private Label? _heroStatusLabel;
    
    private string? _currentPlayerId;
    private string? _lastKnownGameId;
    
    public PlayerDashboardWindow(GrpcGameClient gameClient, WindowPreferences preferences, int screenWidth, int screenHeight)
        : base("player_dashboard", "Player Dashboard", preferences, screenWidth, screenHeight, 340, 600)
    {
        _gameClient = gameClient;
        
        BuildContent();
        
        DockTo(DockPosition.Right);
    }
    
    public void SetCurrentPlayer(string? playerId)
    {
        _currentPlayerId = playerId;
    }
    
    private void BuildContent()
    {
        var mainLayout = new VerticalStackPanel
        {
            Spacing = ThemeManager.Spacing.Medium,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        
        mainLayout.Widgets.Add(BuildResourcePanel());
        mainLayout.Widgets.Add(BuildPurchasePanel());
        mainLayout.Widgets.Add(BuildHeroPanel());
        
        SetScrollableContent(mainLayout);
    }
    
    private Widget BuildResourcePanel()
    {
        var panel = ThemedUIFactory.CreateGameplayPanel();
        panel.HorizontalAlignment = HorizontalAlignment.Stretch;
        
        var layout = new VerticalStackPanel
        {
            Spacing = ThemeManager.Spacing.Small
        };
        
        var titleLabel = ThemedUIFactory.CreateLabel("Resources", ThemeManager.LabelTheme.Heading);
        layout.Widgets.Add(titleLabel);
        
        var populationRow = new HorizontalStackPanel { Spacing = ThemeManager.Spacing.Small };
        populationRow.Widgets.Add(ThemedUIFactory.CreatePopulationIcon());
        _populationStockpileLabel = ThemedUIFactory.CreateLabel("Population: 0");
        _populationStockpileLabel.Width = ThemeManager.ScalePixels(140);
        populationRow.Widgets.Add(_populationStockpileLabel);
        _populationRateLabel = ThemedUIFactory.CreateLabel("+0/turn", ThemeManager.LabelTheme.Success);
        _populationRateLabel.Width = ThemeManager.ScalePixels(90);
        populationRow.Widgets.Add(_populationRateLabel);
        layout.Widgets.Add(populationRow);
        
        var metalRow = new HorizontalStackPanel { Spacing = ThemeManager.Spacing.Small };
        metalRow.Widgets.Add(ThemedUIFactory.CreateMetalIcon());
        _metalStockpileLabel = ThemedUIFactory.CreateLabel("Metal: 0");
        _metalStockpileLabel.Width = ThemeManager.ScalePixels(140);
        metalRow.Widgets.Add(_metalStockpileLabel);
        _metalRateLabel = ThemedUIFactory.CreateLabel("+0/turn", ThemeManager.LabelTheme.Success);
        _metalRateLabel.Width = ThemeManager.ScalePixels(90);
        metalRow.Widgets.Add(_metalRateLabel);
        layout.Widgets.Add(metalRow);
        
        var fuelRow = new HorizontalStackPanel { Spacing = ThemeManager.Spacing.Small };
        fuelRow.Widgets.Add(ThemedUIFactory.CreateFuelIcon());
        _fuelStockpileLabel = ThemedUIFactory.CreateLabel("Fuel: 0");
        _fuelStockpileLabel.Width = ThemeManager.ScalePixels(140);
        fuelRow.Widgets.Add(_fuelStockpileLabel);
        _fuelRateLabel = ThemedUIFactory.CreateLabel("+0/turn", ThemeManager.LabelTheme.Success);
        _fuelRateLabel.Width = ThemeManager.ScalePixels(90);
        fuelRow.Widgets.Add(_fuelRateLabel);
        layout.Widgets.Add(fuelRow);
        
        layout.Widgets.Add(ThemedUIFactory.CreateSpacer(ThemeManager.Spacing.Small));
        
        _territoriesLabel = ThemedUIFactory.CreateLabel("Territories: 0");
        layout.Widgets.Add(_territoriesLabel);
        
        _armiesLabel = ThemedUIFactory.CreateLabel("Armies: 0");
        layout.Widgets.Add(_armiesLabel);
        
        panel.Widgets.Add(layout);
        return panel;
    }
    
    private Widget BuildPurchasePanel()
    {
        var panel = ThemedUIFactory.CreateGameplayPanel();
        panel.HorizontalAlignment = HorizontalAlignment.Stretch;
        
        var layout = new VerticalStackPanel
        {
            Spacing = ThemeManager.Spacing.Small
        };
        
        var titleLabel = ThemedUIFactory.CreateLabel("Army Purchase", ThemeManager.LabelTheme.Heading);
        layout.Widgets.Add(titleLabel);
        
        var costLabel = ThemedUIFactory.CreateSecondaryLabel("Cost per Army:");
        layout.Widgets.Add(costLabel);
        
        var costDetailLabel = ThemedUIFactory.CreateLabel("1 Pop, 3 Metal, 1 Fuel");
        layout.Widgets.Add(costDetailLabel);
        
        var buttonGrid = new Grid
        {
            ColumnSpacing = ThemeManager.Spacing.Small,
            RowSpacing = ThemeManager.Spacing.Small
        };
        
        buttonGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        buttonGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        buttonGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        buttonGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        
        _buy1Button = ThemedUIFactory.CreateButton("Buy 1", ThemeManager.Sizes.ButtonSmallWidth, ThemeManager.Sizes.ButtonSmallHeight, ThemeManager.ButtonTheme.Primary);
        Grid.SetColumn(_buy1Button, 0);
        Grid.SetRow(_buy1Button, 0);
        _buy1Button.Click += (s, a) => PurchaseArmies(1);
        buttonGrid.Widgets.Add(_buy1Button);
        
        _buy5Button = ThemedUIFactory.CreateButton("Buy 5", ThemeManager.Sizes.ButtonSmallWidth, ThemeManager.Sizes.ButtonSmallHeight, ThemeManager.ButtonTheme.Primary);
        Grid.SetColumn(_buy5Button, 1);
        Grid.SetRow(_buy5Button, 0);
        _buy5Button.Click += (s, a) => PurchaseArmies(5);
        buttonGrid.Widgets.Add(_buy5Button);
        
        _buy10Button = ThemedUIFactory.CreateButton("Buy 10", ThemeManager.Sizes.ButtonSmallWidth, ThemeManager.Sizes.ButtonSmallHeight, ThemeManager.ButtonTheme.Primary);
        Grid.SetColumn(_buy10Button, 0);
        Grid.SetRow(_buy10Button, 1);
        _buy10Button.Click += (s, a) => PurchaseArmies(10);
        buttonGrid.Widgets.Add(_buy10Button);
        
        _buy25Button = ThemedUIFactory.CreateButton("Buy 25", ThemeManager.Sizes.ButtonSmallWidth, ThemeManager.Sizes.ButtonSmallHeight, ThemeManager.ButtonTheme.Primary);
        Grid.SetColumn(_buy25Button, 1);
        Grid.SetRow(_buy25Button, 1);
        _buy25Button.Click += (s, a) => PurchaseArmies(25);
        buttonGrid.Widgets.Add(_buy25Button);
        
        layout.Widgets.Add(buttonGrid);
        
        _purchasePhaseLabel = ThemedUIFactory.CreateLabel("", ThemeManager.LabelTheme.Warning);
        _purchasePhaseLabel.Visible = false;
        layout.Widgets.Add(_purchasePhaseLabel);
        
        panel.Widgets.Add(layout);
        return panel;
    }
    
    private Widget BuildHeroPanel()
    {
        var panel = ThemedUIFactory.CreateGameplayPanel(ThemeManager.Colors.HeroColor);
        panel.HorizontalAlignment = HorizontalAlignment.Stretch;
        
        var layout = new VerticalStackPanel
        {
            Spacing = ThemeManager.Spacing.Small
        };
        
        var titleLabel = ThemedUIFactory.CreateLabel("Hero Assignment", ThemeManager.LabelTheme.Heading);
        titleLabel.TextColor = ThemeManager.Colors.HeroColor;
        layout.Widgets.Add(titleLabel);
        
        _heroStatusLabel = ThemedUIFactory.CreateSecondaryLabel("No heroes available");
        layout.Widgets.Add(_heroStatusLabel);
        
        var comingSoonLabel = ThemedUIFactory.CreateSmallLabel("(Coming soon)");
        layout.Widgets.Add(comingSoonLabel);
        
        _assignToArmyButton = ThemedUIFactory.CreateButton("Assign to Army", ThemeManager.ScalePixels(170), ThemeManager.Sizes.ButtonSmallHeight, ThemeManager.ButtonTheme.Hero);
        _assignToArmyButton.Enabled = false;
        _assignToArmyButton.Click += (s, a) => AssignHeroToArmy();
        layout.Widgets.Add(_assignToArmyButton);
        
        _assignToRegionButton = ThemedUIFactory.CreateButton("Assign to Region", ThemeManager.ScalePixels(170), ThemeManager.Sizes.ButtonSmallHeight, ThemeManager.ButtonTheme.Hero);
        _assignToRegionButton.Enabled = false;
        _assignToRegionButton.Click += (s, a) => AssignHeroToRegion();
        layout.Widgets.Add(_assignToRegionButton);
        
        _recallHeroButton = ThemedUIFactory.CreateButton("Recall Hero", ThemeManager.ScalePixels(170), ThemeManager.Sizes.ButtonSmallHeight, ThemeManager.ButtonTheme.Danger);
        _recallHeroButton.Enabled = false;
        _recallHeroButton.Click += (s, a) => RecallHero();
        layout.Widgets.Add(_recallHeroButton);
        
        panel.Widgets.Add(layout);
        return panel;
    }
    
    public void UpdateContent(GameStateCache gameStateCache)
    {
        if (!IsVisible || _currentPlayerId == null)
        {
            return;
        }

        _lastKnownGameId = gameStateCache.GetGameId();
        
        UpdateResourceDisplay(gameStateCache);
        UpdatePurchaseButtons(gameStateCache);
    }
    
    private void UpdateResourceDisplay(GameStateCache gameStateCache)
    {
        if (_currentPlayerId == null)
        {
            return;
        }

        var playerState = gameStateCache.GetPlayerState(_currentPlayerId);
        if (playerState == null)
        {
            return;
        }

        if (_populationStockpileLabel != null)
        {
            _populationStockpileLabel.Text = $"Population: {playerState.PopulationStockpile}";
        }

        if (_metalStockpileLabel != null)
        {
            _metalStockpileLabel.Text = $"Metal: {playerState.MetalStockpile}";
        }

        if (_fuelStockpileLabel != null)
        {
            _fuelStockpileLabel.Text = $"Fuel: {playerState.FuelStockpile}";
        }

        int populationRate = gameStateCache.GetProductionRate(_currentPlayerId, "population");
        if (_populationRateLabel != null)
        {
            _populationRateLabel.Text = populationRate > 0 ? $"+{populationRate}/turn" : $"{populationRate}/turn";
            _populationRateLabel.TextColor = populationRate > 0 ? ThemeManager.Colors.TextSuccess : ThemeManager.Colors.TextError;
        }
        
        int metalRate = gameStateCache.GetProductionRate(_currentPlayerId, "metal");
        if (_metalRateLabel != null)
        {
            _metalRateLabel.Text = metalRate > 0 ? $"+{metalRate}/turn" : $"{metalRate}/turn";
            _metalRateLabel.TextColor = metalRate > 0 ? ThemeManager.Colors.TextSuccess : ThemeManager.Colors.TextError;
        }
        
        int fuelRate = gameStateCache.GetProductionRate(_currentPlayerId, "fuel");
        if (_fuelRateLabel != null)
        {
            _fuelRateLabel.Text = fuelRate > 0 ? $"+{fuelRate}/turn" : $"{fuelRate}/turn";
            _fuelRateLabel.TextColor = fuelRate > 0 ? ThemeManager.Colors.TextSuccess : ThemeManager.Colors.TextError;
        }
        
        int regionCount = gameStateCache.GetRegionsOwnedByPlayer(_currentPlayerId).Count;
        if (_territoriesLabel != null)
        {
            _territoriesLabel.Text = $"Territories: {regionCount}";
        }

        int armyCount = gameStateCache.GetArmiesOwnedByPlayer(_currentPlayerId).Count;
        if (_armiesLabel != null)
        {
            _armiesLabel.Text = $"Armies: {armyCount}";
        }
    }
    
    private void UpdatePurchaseButtons(GameStateCache gameStateCache)
    {
        if (_currentPlayerId == null)
        {
            return;
        }

        var playerState = gameStateCache.GetPlayerState(_currentPlayerId);
        if (playerState == null)
        {
            return;
        }

        var currentPhase = gameStateCache.GetCurrentPhase();
        bool isPurchasePhase = currentPhase == TurnPhase.Purchase;
        
        if (_purchasePhaseLabel != null)
        {
            _purchasePhaseLabel.Visible = !isPurchasePhase;
            _purchasePhaseLabel.Text = "Purchase phase only";
        }
        
        UpdatePurchaseButton(_buy1Button, 1, playerState, isPurchasePhase);
        UpdatePurchaseButton(_buy5Button, 5, playerState, isPurchasePhase);
        UpdatePurchaseButton(_buy10Button, 10, playerState, isPurchasePhase);
        UpdatePurchaseButton(_buy25Button, 25, playerState, isPurchasePhase);
    }
    
    private void UpdatePurchaseButton(Myra.Graphics2D.UI.Button? button, int count, PlayerState playerState, bool isPurchasePhase)
    {
        if (button == null)
        {
            return;
        }

        var cost = GetArmyCost(count);
        bool canAfford = CanAfford(playerState, cost);
        
        button.Enabled = isPurchasePhase && canAfford;
    }
    
    private bool CanAfford(PlayerState playerState, ResourceCost cost)
    {
        return playerState.PopulationStockpile >= cost.Population &&
               playerState.MetalStockpile >= cost.Metal &&
               playerState.FuelStockpile >= cost.Fuel;
    }
    
    private ResourceCost GetArmyCost(int count)
    {
        return new ResourceCost
        {
            Population = count * 1,
            Metal = count * 3,
            Fuel = count * 1
        };
    }
    
    private void PurchaseArmies(int count)
    {
        if (_currentPlayerId == null || _lastKnownGameId == null)
        {
            return;
        }

        GameFeedbackBus.PublishBusy("Submitting purchase order", $"Requesting {count} new army unit(s).");

        Task.Run(async () =>
        {
            try
            {
                await _gameClient.SendPurchaseArmiesAsync(_currentPlayerId, _lastKnownGameId, count);
                Console.WriteLine($"Purchased {count} armies");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to purchase armies: {ex.Message}");
                GameFeedbackBus.PublishError("Purchase order failed", ex.Message);
            }
        });
    }
    
    private void AssignHeroToArmy()
    {
        Console.WriteLine("Hero assignment to army - Coming soon!");
        GameFeedbackBus.PublishWarning("Hero assignment unavailable", "Assign hero to army is not implemented yet.");
    }
    
    private void AssignHeroToRegion()
    {
        Console.WriteLine("Hero assignment to region - Coming soon!");
        GameFeedbackBus.PublishWarning("Hero assignment unavailable", "Assign hero to region is not implemented yet.");
    }
    
    private void RecallHero()
    {
        Console.WriteLine("Hero recall - Coming soon!");
        GameFeedbackBus.PublishWarning("Hero recall unavailable", "Hero recall is not implemented yet.");
    }
}
