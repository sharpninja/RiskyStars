using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RiskyStars.Shared;
using Myra;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.Brushes;

namespace RiskyStars.Client;

public class PlayerDashboard
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly GrpcGameClient _gameClient;
    private int _screenWidth;
    private int _screenHeight;
    
    private Desktop? _desktop;
    private Panel? _mainPanel;
    
    // Resource display widgets
    private Label? _populationStockpileLabel;
    private Label? _populationRateLabel;
    private Label? _metalStockpileLabel;
    private Label? _metalRateLabel;
    private Label? _fuelStockpileLabel;
    private Label? _fuelRateLabel;
    private Label? _territoriesLabel;
    private Label? _armiesLabel;
    
    // Purchase button widgets
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
    private TextButton? _buy1Button;
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
    private TextButton? _buy5Button;
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
    private TextButton? _buy10Button;
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
    private TextButton? _buy25Button;
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CS0618 // Type or member is obsolete
    private Label? _purchasePhaseLabel;
    
    // Hero assignment widgets
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
    private TextButton? _assignToArmyButton;
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
    private TextButton? _assignToRegionButton;
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
    private TextButton? _recallHeroButton;
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CS0618 // Type or member is obsolete
    private Label? _heroStatusLabel;
    
    private string? _currentPlayerId;
    private string? _lastKnownGameId;
    private bool _isVisible;
    
    public bool IsVisible
    {
        get => _isVisible;
        set => _isVisible = value;
    }
    
    public PlayerDashboard(GraphicsDevice graphicsDevice, GrpcGameClient gameClient, int screenWidth, int screenHeight)
    {
        _graphicsDevice = graphicsDevice;
        _gameClient = gameClient;
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
        _isVisible = true;
        
        BuildUI();
    }
    
    public void LoadContent(SpriteFont font)
    {
        // Myra doesn't require explicit font loading in this way
        // The UI is already built in BuildUI
    }

    public void ResizeViewport(int screenWidth, int screenHeight)
    {
        if (screenWidth <= 0 || screenHeight <= 0)
        {
            return;
        }

        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
        BuildUI();
    }
    
    public void SetCurrentPlayer(string? playerId)
    {
        _currentPlayerId = playerId;
    }
    
    private void BuildUI()
    {
        _desktop = new Desktop();
        
        // Main container panel positioned on the right side
        _mainPanel = new Panel
        {
            Width = 320,
            Height = _screenHeight,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Stretch,
            Background = null // Transparent background
        };
        
        // Main vertical layout for all panels
        var mainLayout = new VerticalStackPanel
        {
            Spacing = 10,
            Width = 310
        };
        
        // Build the three main sections
        mainLayout.Widgets.Add(BuildResourcePanel());
        mainLayout.Widgets.Add(BuildPurchasePanel());
        mainLayout.Widgets.Add(BuildHeroPanel());
        
        _mainPanel.Widgets.Add(mainLayout);
        _desktop.Root = _mainPanel;
    }
    
    private Panel BuildResourcePanel()
    {
        var panel = new Panel
        {
            Width = 300,
            Height = 200,
            Background = new SolidBrush(Color.Black * 0.85f),
            Border = new SolidBrush(new Color(100, 180, 255)),
            BorderThickness = new Thickness(2),
            Padding = new Thickness(10)
        };
        
        var layout = new VerticalStackPanel
        {
            Spacing = 8
        };
        
        // Title
        var titleLabel = new Label
        {
            Text = "Resources",
            TextColor = new Color(150, 220, 255)
        };
        layout.Widgets.Add(titleLabel);
        
        // Population resource row
        var populationRow = new HorizontalStackPanel
        {
            Spacing = 8
        };
        
        var populationIcon = new Panel
        {
            Width = 18,
            Height = 18,
            Background = new SolidBrush(new Color(100, 200, 100))
        };
        populationRow.Widgets.Add(populationIcon);
        
        _populationStockpileLabel = new Label
        {
            Text = "Population: 0",
            TextColor = Color.White,
            Width = 140
        };
        populationRow.Widgets.Add(_populationStockpileLabel);
        
        _populationRateLabel = new Label
        {
            Text = "+0/turn",
            TextColor = Color.LightGreen,
            Width = 90
        };
        populationRow.Widgets.Add(_populationRateLabel);
        
        layout.Widgets.Add(populationRow);
        
        // Metal resource row
        var metalRow = new HorizontalStackPanel
        {
            Spacing = 8
        };
        
        var metalIcon = new Panel
        {
            Width = 18,
            Height = 18,
            Background = new SolidBrush(new Color(180, 180, 180))
        };
        metalRow.Widgets.Add(metalIcon);
        
        _metalStockpileLabel = new Label
        {
            Text = "Metal: 0",
            TextColor = Color.White,
            Width = 140
        };
        metalRow.Widgets.Add(_metalStockpileLabel);
        
        _metalRateLabel = new Label
        {
            Text = "+0/turn",
            TextColor = Color.LightGreen,
            Width = 90
        };
        metalRow.Widgets.Add(_metalRateLabel);
        
        layout.Widgets.Add(metalRow);
        
        // Fuel resource row
        var fuelRow = new HorizontalStackPanel
        {
            Spacing = 8
        };
        
        var fuelIcon = new Panel
        {
            Width = 18,
            Height = 18,
            Background = new SolidBrush(new Color(220, 160, 80))
        };
        fuelRow.Widgets.Add(fuelIcon);
        
        _fuelStockpileLabel = new Label
        {
            Text = "Fuel: 0",
            TextColor = Color.White,
            Width = 140
        };
        fuelRow.Widgets.Add(_fuelStockpileLabel);
        
        _fuelRateLabel = new Label
        {
            Text = "+0/turn",
            TextColor = Color.LightGreen,
            Width = 90
        };
        fuelRow.Widgets.Add(_fuelRateLabel);
        
        layout.Widgets.Add(fuelRow);
        
        // Spacer
        layout.Widgets.Add(new Panel { Height = 10 });
        
        // Territories count
        _territoriesLabel = new Label
        {
            Text = "Territories: 0",
            TextColor = Color.White
        };
        layout.Widgets.Add(_territoriesLabel);
        
        // Armies count
        _armiesLabel = new Label
        {
            Text = "Armies: 0",
            TextColor = Color.White
        };
        layout.Widgets.Add(_armiesLabel);
        
        panel.Widgets.Add(layout);
        return panel;
    }
    
    private Panel BuildPurchasePanel()
    {
        var panel = new Panel
        {
            Width = 300,
            Height = 150,
            Background = new SolidBrush(Color.Black * 0.85f),
            Border = new SolidBrush(new Color(100, 180, 255)),
            BorderThickness = new Thickness(2),
            Padding = new Thickness(10)
        };
        
        var layout = new VerticalStackPanel
        {
            Spacing = 8
        };
        
        // Title
        var titleLabel = new Label
        {
            Text = "Army Purchase",
            TextColor = new Color(150, 220, 255)
        };
        layout.Widgets.Add(titleLabel);
        
        // Cost info
        var costLabel = new Label
        {
            Text = "Cost per Army:",
            TextColor = Color.LightGray
        };
        layout.Widgets.Add(costLabel);
        
        var costDetailLabel = new Label
        {
            Text = "1 Pop, 3 Metal, 1 Fuel",
            TextColor = Color.White
        };
        layout.Widgets.Add(costDetailLabel);
        
        // Button grid - 2x2
        var buttonGrid = new Grid
        {
            ColumnSpacing = 10,
            RowSpacing = 10
        };
        
        buttonGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        buttonGrid.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        buttonGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        buttonGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        
        // Buy 1 button
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
        _buy1Button = new TextButton
        {
            Text = "Buy 1",
            Width = 80,
            Height = 30,
            Background = new SolidBrush(new Color(60, 120, 180)),
            GridColumn = 0,
            GridRow = 0
        };
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CS0618 // Type or member is obsolete
        _buy1Button.Click += (s, a) => PurchaseArmies(1);
        buttonGrid.Widgets.Add(_buy1Button);
        
        // Buy 5 button
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
        _buy5Button = new TextButton
        {
            Text = "Buy 5",
            Width = 80,
            Height = 30,
            Background = new SolidBrush(new Color(60, 120, 180)),
            GridColumn = 1,
            GridRow = 0
        };
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CS0618 // Type or member is obsolete
        _buy5Button.Click += (s, a) => PurchaseArmies(5);
        buttonGrid.Widgets.Add(_buy5Button);
        
        // Buy 10 button
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
        _buy10Button = new TextButton
        {
            Text = "Buy 10",
            Width = 80,
            Height = 30,
            Background = new SolidBrush(new Color(60, 120, 180)),
            GridColumn = 0,
            GridRow = 1
        };
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CS0618 // Type or member is obsolete
        _buy10Button.Click += (s, a) => PurchaseArmies(10);
        buttonGrid.Widgets.Add(_buy10Button);
        
        // Buy 25 button
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
        _buy25Button = new TextButton
        {
            Text = "Buy 25",
            Width = 80,
            Height = 30,
            Background = new SolidBrush(new Color(60, 120, 180)),
            GridColumn = 1,
            GridRow = 1
        };
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CS0618 // Type or member is obsolete
        _buy25Button.Click += (s, a) => PurchaseArmies(25);
        buttonGrid.Widgets.Add(_buy25Button);
        
        layout.Widgets.Add(buttonGrid);
        
        // Purchase phase warning
        _purchasePhaseLabel = new Label
        {
            Text = "",
            TextColor = Color.Orange,
            Visible = false
        };
        layout.Widgets.Add(_purchasePhaseLabel);
        
        panel.Widgets.Add(layout);
        return panel;
    }
    
    private Panel BuildHeroPanel()
    {
        var panel = new Panel
        {
            Width = 300,
            Height = 180,
            Background = new SolidBrush(Color.Black * 0.85f),
            Border = new SolidBrush(new Color(180, 100, 200)),
            BorderThickness = new Thickness(2),
            Padding = new Thickness(10)
        };
        
        var layout = new VerticalStackPanel
        {
            Spacing = 8
        };
        
        // Title
        var titleLabel = new Label
        {
            Text = "Hero Assignment",
            TextColor = new Color(200, 140, 220)
        };
        layout.Widgets.Add(titleLabel);
        
        // Hero status
        _heroStatusLabel = new Label
        {
            Text = "No heroes available",
            TextColor = Color.Gray
        };
        layout.Widgets.Add(_heroStatusLabel);
        
        var comingSoonLabel = new Label
        {
            Text = "(Coming soon)",
            TextColor = Color.DarkGray
        };
        layout.Widgets.Add(comingSoonLabel);
        
        // Assign to Army button
#pragma warning disable CS0618 // Type or member is obsolete
        _assignToArmyButton = new TextButton
        {
            Text = "Assign to Army",
            Width = 170,
            Height = 30,
            Background = new SolidBrush(new Color(120, 60, 140)),
            Enabled = false
        };
#pragma warning restore CS0618 // Type or member is obsolete
        _assignToArmyButton.Click += (s, a) => AssignHeroToArmy();
        layout.Widgets.Add(_assignToArmyButton);
        
        // Assign to Region button
#pragma warning disable CS0618 // Type or member is obsolete
        _assignToRegionButton = new TextButton
        {
            Text = "Assign to Region",
            Width = 170,
            Height = 30,
            Background = new SolidBrush(new Color(120, 60, 140)),
            Enabled = false
        };
#pragma warning restore CS0618 // Type or member is obsolete
        _assignToRegionButton.Click += (s, a) => AssignHeroToRegion();
        layout.Widgets.Add(_assignToRegionButton);
        
        // Recall Hero button
#pragma warning disable CS0618 // Type or member is obsolete
        _recallHeroButton = new TextButton
        {
            Text = "Recall Hero",
            Width = 170,
            Height = 30,
            Background = new SolidBrush(new Color(140, 60, 60)),
            Enabled = false
        };
#pragma warning restore CS0618 // Type or member is obsolete
        _recallHeroButton.Click += (s, a) => RecallHero();
        layout.Widgets.Add(_recallHeroButton);
        
        panel.Widgets.Add(layout);
        return panel;
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
    
    public void Update(GameTime gameTime, GameStateCache gameStateCache)
    {
        if (!_isVisible)
        {
            return;
        }

        _lastKnownGameId = gameStateCache.GetGameId();
        
        UpdateResourceDisplay(gameStateCache);
        UpdatePurchaseButtons(gameStateCache);
        UpdateHeroButtons(gameStateCache);
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

        // Update resource stockpiles
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

        // Update production rates
        int populationRate = gameStateCache.GetProductionRate(_currentPlayerId, "population");
        if (_populationRateLabel != null)
        {
            _populationRateLabel.Text = populationRate > 0 ? $"+{populationRate}/turn" : $"{populationRate}/turn";
            _populationRateLabel.TextColor = populationRate > 0 ? Color.LightGreen : Color.LightCoral;
        }
        
        int metalRate = gameStateCache.GetProductionRate(_currentPlayerId, "metal");
        if (_metalRateLabel != null)
        {
            _metalRateLabel.Text = metalRate > 0 ? $"+{metalRate}/turn" : $"{metalRate}/turn";
            _metalRateLabel.TextColor = metalRate > 0 ? Color.LightGreen : Color.LightCoral;
        }
        
        int fuelRate = gameStateCache.GetProductionRate(_currentPlayerId, "fuel");
        if (_fuelRateLabel != null)
        {
            _fuelRateLabel.Text = fuelRate > 0 ? $"+{fuelRate}/turn" : $"{fuelRate}/turn";
            _fuelRateLabel.TextColor = fuelRate > 0 ? Color.LightGreen : Color.LightCoral;
        }
        
        // Update territory and army counts
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
        
        // Update phase warning
        if (_purchasePhaseLabel != null)
        {
            _purchasePhaseLabel.Visible = !isPurchasePhase;
            _purchasePhaseLabel.Text = "Purchase phase only";
        }
        
        // Update button states based on affordability and phase
        UpdatePurchaseButton(_buy1Button, 1, playerState, isPurchasePhase);
        UpdatePurchaseButton(_buy5Button, 5, playerState, isPurchasePhase);
        UpdatePurchaseButton(_buy10Button, 10, playerState, isPurchasePhase);
        UpdatePurchaseButton(_buy25Button, 25, playerState, isPurchasePhase);
    }
    
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
    private void UpdatePurchaseButton(TextButton? button, int count, PlayerState playerState, bool isPurchasePhase)
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CS0618 // Type or member is obsolete
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
    
    private void UpdateHeroButtons(GameStateCache gameStateCache)
    {
        // Hero functionality is not yet implemented
        // Buttons remain disabled
    }
    
    public void Draw(SpriteBatch spriteBatch, GameStateCache gameStateCache)
    {
        if (!_isVisible || _currentPlayerId == null)
        {
            return;
        }

        var playerState = gameStateCache.GetPlayerState(_currentPlayerId);
        if (playerState == null)
        {
            return;
        }

        // Render Myra UI
        _desktop?.Render();
    }
    
    private void PurchaseArmies(int count)
    {
        if (_currentPlayerId == null || _lastKnownGameId == null)
        {
            return;
        }

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
            }
        });
    }
    
    private void AssignHeroToArmy()
    {
        Console.WriteLine("Hero assignment to army - Coming soon!");
    }
    
    private void AssignHeroToRegion()
    {
        Console.WriteLine("Hero assignment to region - Coming soon!");
    }
    
    private void RecallHero()
    {
        Console.WriteLine("Hero recall - Coming soon!");
    }
}

public class ResourceCost
{
    public int Population { get; set; }
    public int Metal { get; set; }
    public int Fuel { get; set; }
}
