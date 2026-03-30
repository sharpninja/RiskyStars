using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RiskyStars.Shared;

namespace RiskyStars.Client;

public class PlayerDashboard
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly GrpcGameClient _gameClient;
    private Texture2D? _pixelTexture;
    private SpriteFont? _font;
    private readonly int _screenWidth;
    private readonly int _screenHeight;
    
    private MouseState _previousMouseState;
    
    private readonly List<DashboardButton> _purchaseButtons;
    private readonly List<DashboardButton> _heroButtons;
    
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
        
        _purchaseButtons = new List<DashboardButton>();
        _heroButtons = new List<DashboardButton>();
        
        CreatePixelTexture();
        InitializePurchaseButtons();
        InitializeHeroButtons();
    }
    
    private void CreatePixelTexture()
    {
        _pixelTexture = new Texture2D(_graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
    }
    
    public void LoadContent(SpriteFont font)
    {
        _font = font;
    }
    
    public void SetCurrentPlayer(string? playerId)
    {
        _currentPlayerId = playerId;
    }
    
    private void InitializePurchaseButtons()
    {
        int buttonWidth = 80;
        int buttonHeight = 30;
        int buttonSpacing = 10;
        int startX = _screenWidth - 320;
        int startY = 360;
        
        _purchaseButtons.Add(new DashboardButton
        {
            Bounds = new Rectangle(startX, startY, buttonWidth, buttonHeight),
            Text = "Buy 1",
            Action = () => PurchaseArmies(1),
            Color = new Color(60, 120, 180),
            HoverColor = new Color(80, 150, 220),
            Cost = GetArmyCost(1)
        });
        
        _purchaseButtons.Add(new DashboardButton
        {
            Bounds = new Rectangle(startX + buttonWidth + buttonSpacing, startY, buttonWidth, buttonHeight),
            Text = "Buy 5",
            Action = () => PurchaseArmies(5),
            Color = new Color(60, 120, 180),
            HoverColor = new Color(80, 150, 220),
            Cost = GetArmyCost(5)
        });
        
        _purchaseButtons.Add(new DashboardButton
        {
            Bounds = new Rectangle(startX, startY + buttonHeight + buttonSpacing, buttonWidth, buttonHeight),
            Text = "Buy 10",
            Action = () => PurchaseArmies(10),
            Color = new Color(60, 120, 180),
            HoverColor = new Color(80, 150, 220),
            Cost = GetArmyCost(10)
        });
        
        _purchaseButtons.Add(new DashboardButton
        {
            Bounds = new Rectangle(startX + buttonWidth + buttonSpacing, startY + buttonHeight + buttonSpacing, buttonWidth, buttonHeight),
            Text = "Buy 25",
            Action = () => PurchaseArmies(25),
            Color = new Color(60, 120, 180),
            HoverColor = new Color(80, 150, 220),
            Cost = GetArmyCost(25)
        });
    }
    
    private void InitializeHeroButtons()
    {
        int buttonWidth = 170;
        int buttonHeight = 30;
        int buttonSpacing = 8;
        int startX = _screenWidth - 320;
        int startY = 550;
        
        _heroButtons.Add(new DashboardButton
        {
            Bounds = new Rectangle(startX, startY, buttonWidth, buttonHeight),
            Text = "Assign to Army",
            Action = () => AssignHeroToArmy(),
            Color = new Color(120, 60, 140),
            HoverColor = new Color(150, 80, 170),
            IsEnabled = false
        });
        
        _heroButtons.Add(new DashboardButton
        {
            Bounds = new Rectangle(startX, startY + buttonHeight + buttonSpacing, buttonWidth, buttonHeight),
            Text = "Assign to Region",
            Action = () => AssignHeroToRegion(),
            Color = new Color(120, 60, 140),
            HoverColor = new Color(150, 80, 170),
            IsEnabled = false
        });
        
        _heroButtons.Add(new DashboardButton
        {
            Bounds = new Rectangle(startX, startY + (buttonHeight + buttonSpacing) * 2, buttonWidth, buttonHeight),
            Text = "Recall Hero",
            Action = () => RecallHero(),
            Color = new Color(140, 60, 60),
            HoverColor = new Color(170, 80, 80),
            IsEnabled = false
        });
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
            return;
        
        _lastKnownGameId = gameStateCache.GetGameId();
        
        var mouseState = Mouse.GetState();
        
        UpdateButtons(mouseState, _purchaseButtons, gameStateCache);
        UpdateButtons(mouseState, _heroButtons, gameStateCache);
        
        _previousMouseState = mouseState;
    }
    
    private void UpdateButtons(MouseState mouseState, List<DashboardButton> buttons, GameStateCache gameStateCache)
    {
        var mousePoint = new Point(mouseState.X, mouseState.Y);
        
        foreach (var button in buttons)
        {
            button.IsHovered = button.Bounds.Contains(mousePoint);
            
            if (button.Cost != null && _currentPlayerId != null)
            {
                var playerState = gameStateCache.GetPlayerState(_currentPlayerId);
                button.IsEnabled = playerState != null && CanAfford(playerState, button.Cost);
            }
            
            if (button.IsHovered && 
                button.IsEnabled && 
                mouseState.LeftButton == ButtonState.Pressed && 
                _previousMouseState.LeftButton == ButtonState.Released)
            {
                button.Action?.Invoke();
            }
        }
    }
    
    private bool CanAfford(PlayerState playerState, ResourceCost cost)
    {
        return playerState.PopulationStockpile >= cost.Population &&
               playerState.MetalStockpile >= cost.Metal &&
               playerState.FuelStockpile >= cost.Fuel;
    }
    
    public void Draw(SpriteBatch spriteBatch, GameStateCache gameStateCache)
    {
        if (!_isVisible || _pixelTexture == null || _font == null || _currentPlayerId == null)
            return;
        
        var playerState = gameStateCache.GetPlayerState(_currentPlayerId);
        if (playerState == null)
            return;
        
        spriteBatch.Begin(
            sortMode: SpriteSortMode.Deferred,
            blendState: BlendState.AlphaBlend,
            samplerState: SamplerState.LinearClamp);
        
        DrawResourceStockpilesPanel(spriteBatch, playerState, gameStateCache);
        DrawArmyPurchasePanel(spriteBatch, playerState, gameStateCache);
        DrawHeroAssignmentPanel(spriteBatch, playerState, gameStateCache);
        
        spriteBatch.End();
    }
    
    private void DrawResourceStockpilesPanel(SpriteBatch spriteBatch, PlayerState playerState, GameStateCache gameStateCache)
    {
        if (_pixelTexture == null || _font == null) return;
        
        int panelWidth = 300;
        int panelHeight = 200;
        int panelX = _screenWidth - panelWidth - 10;
        int panelY = 50;
        
        Rectangle panel = new Rectangle(panelX, panelY, panelWidth, panelHeight);
        spriteBatch.Draw(_pixelTexture, panel, Color.Black * 0.85f);
        DrawRectangleOutline(spriteBatch, panel, new Color(100, 180, 255), 2);
        
        int yOffset = panelY + 10;
        spriteBatch.DrawString(_font, "Resources", new Vector2(panelX + 10, yOffset), new Color(150, 220, 255));
        
        yOffset += 30;
        int populationRate = gameStateCache.GetProductionRate(_currentPlayerId!, "population");
        DrawResourceRow(spriteBatch, panelX + 10, yOffset, "Population", 
            playerState.PopulationStockpile, populationRate, new Color(100, 200, 100));
        
        yOffset += 35;
        int metalRate = gameStateCache.GetProductionRate(_currentPlayerId!, "metal");
        DrawResourceRow(spriteBatch, panelX + 10, yOffset, "Metal", 
            playerState.MetalStockpile, metalRate, new Color(180, 180, 180));
        
        yOffset += 35;
        int fuelRate = gameStateCache.GetProductionRate(_currentPlayerId!, "fuel");
        DrawResourceRow(spriteBatch, panelX + 10, yOffset, "Fuel", 
            playerState.FuelStockpile, fuelRate, new Color(220, 160, 80));
        
        yOffset += 45;
        int regionCount = gameStateCache.GetRegionsOwnedByPlayer(_currentPlayerId!).Count;
        int armyCount = gameStateCache.GetArmiesOwnedByPlayer(_currentPlayerId!).Count;
        
        spriteBatch.DrawString(_font, $"Territories: {regionCount}", 
            new Vector2(panelX + 10, yOffset), Color.White, 0f, Vector2.Zero, 0.75f, SpriteEffects.None, 0f);
        
        yOffset += 25;
        spriteBatch.DrawString(_font, $"Armies: {armyCount}", 
            new Vector2(panelX + 10, yOffset), Color.White, 0f, Vector2.Zero, 0.75f, SpriteEffects.None, 0f);
    }
    
    private void DrawResourceRow(SpriteBatch spriteBatch, int x, int y, string name, int stockpile, int productionRate, Color color)
    {
        if (_font == null || _pixelTexture == null) return;
        
        int iconSize = 18;
        Rectangle iconRect = new Rectangle(x, y + 2, iconSize, iconSize);
        spriteBatch.Draw(_pixelTexture, iconRect, color);
        
        string stockpileText = $"{name}: {stockpile}";
        spriteBatch.DrawString(_font, stockpileText, 
            new Vector2(x + iconSize + 8, y), Color.White, 0f, Vector2.Zero, 0.75f, SpriteEffects.None, 0f);
        
        if (productionRate != 0)
        {
            string rateText = productionRate > 0 ? $"+{productionRate}/turn" : $"{productionRate}/turn";
            Color rateColor = productionRate > 0 ? Color.LightGreen : Color.LightCoral;
            spriteBatch.DrawString(_font, rateText, 
                new Vector2(x + 180, y), rateColor, 0f, Vector2.Zero, 0.65f, SpriteEffects.None, 0f);
        }
    }
    
    private void DrawArmyPurchasePanel(SpriteBatch spriteBatch, PlayerState playerState, GameStateCache gameStateCache)
    {
        if (_pixelTexture == null || _font == null) return;
        
        int panelWidth = 300;
        int panelHeight = 150;
        int panelX = _screenWidth - panelWidth - 10;
        int panelY = 260;
        
        Rectangle panel = new Rectangle(panelX, panelY, panelWidth, panelHeight);
        spriteBatch.Draw(_pixelTexture, panel, Color.Black * 0.85f);
        DrawRectangleOutline(spriteBatch, panel, new Color(100, 180, 255), 2);
        
        int yOffset = panelY + 10;
        spriteBatch.DrawString(_font, "Army Purchase", new Vector2(panelX + 10, yOffset), new Color(150, 220, 255));
        
        yOffset += 28;
        spriteBatch.DrawString(_font, "Cost per Army:", 
            new Vector2(panelX + 10, yOffset), Color.LightGray, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
        
        yOffset += 20;
        spriteBatch.DrawString(_font, "1 Pop, 3 Metal, 1 Fuel", 
            new Vector2(panelX + 10, yOffset), Color.White, 0f, Vector2.Zero, 0.65f, SpriteEffects.None, 0f);
        
        foreach (var button in _purchaseButtons)
        {
            DrawButton(spriteBatch, button, playerState);
        }
        
        var currentPhase = gameStateCache.GetCurrentPhase();
        if (currentPhase != TurnPhase.Purchase)
        {
            yOffset = panelY + panelHeight - 30;
            spriteBatch.DrawString(_font, "Purchase phase only", 
                new Vector2(panelX + 10, yOffset), Color.Orange, 0f, Vector2.Zero, 0.65f, SpriteEffects.None, 0f);
        }
    }
    
    private void DrawHeroAssignmentPanel(SpriteBatch spriteBatch, PlayerState playerState, GameStateCache gameStateCache)
    {
        if (_pixelTexture == null || _font == null) return;
        
        int panelWidth = 300;
        int panelHeight = 180;
        int panelX = _screenWidth - panelWidth - 10;
        int panelY = 420;
        
        Rectangle panel = new Rectangle(panelX, panelY, panelWidth, panelHeight);
        spriteBatch.Draw(_pixelTexture, panel, Color.Black * 0.85f);
        DrawRectangleOutline(spriteBatch, panel, new Color(180, 100, 200), 2);
        
        int yOffset = panelY + 10;
        spriteBatch.DrawString(_font, "Hero Assignment", new Vector2(panelX + 10, yOffset), new Color(200, 140, 220));
        
        yOffset += 30;
        spriteBatch.DrawString(_font, "No heroes available", 
            new Vector2(panelX + 10, yOffset), Color.Gray, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
        
        yOffset += 20;
        spriteBatch.DrawString(_font, "(Coming soon)", 
            new Vector2(panelX + 10, yOffset), Color.DarkGray, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
        
        foreach (var button in _heroButtons)
        {
            DrawButton(spriteBatch, button, playerState);
        }
    }
    
    private void DrawButton(SpriteBatch spriteBatch, DashboardButton button, PlayerState playerState)
    {
        if (_pixelTexture == null || _font == null) return;
        
        Color buttonColor = button.Color;
        if (!button.IsEnabled)
        {
            buttonColor = Color.DarkGray;
        }
        else if (button.IsHovered)
        {
            buttonColor = button.HoverColor;
        }
        
        spriteBatch.Draw(_pixelTexture, button.Bounds, buttonColor * 0.9f);
        DrawRectangleOutline(spriteBatch, button.Bounds, buttonColor, 2);
        
        var textSize = _font.MeasureString(button.Text);
        var textScale = 0.7f;
        var scaledTextSize = textSize * textScale;
        var textPos = new Vector2(
            button.Bounds.X + (button.Bounds.Width - scaledTextSize.X) / 2,
            button.Bounds.Y + (button.Bounds.Height - scaledTextSize.Y) / 2
        );
        
        Color textColor = button.IsEnabled ? Color.White : Color.Gray;
        spriteBatch.DrawString(_font, button.Text, textPos, textColor, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
        
        if (button.Cost != null && button.IsHovered && button.IsEnabled)
        {
            DrawCostTooltip(spriteBatch, button.Bounds, button.Cost, playerState);
        }
    }
    
    private void DrawCostTooltip(SpriteBatch spriteBatch, Rectangle buttonBounds, ResourceCost cost, PlayerState playerState)
    {
        if (_pixelTexture == null || _font == null) return;
        
        int tooltipWidth = 180;
        int tooltipHeight = 80;
        int tooltipX = buttonBounds.X - tooltipWidth - 10;
        int tooltipY = buttonBounds.Y;
        
        Rectangle tooltip = new Rectangle(tooltipX, tooltipY, tooltipWidth, tooltipHeight);
        spriteBatch.Draw(_pixelTexture, tooltip, Color.Black * 0.95f);
        DrawRectangleOutline(spriteBatch, tooltip, Color.Yellow, 2);
        
        int yOffset = tooltipY + 8;
        
        DrawCostLine(spriteBatch, tooltipX + 8, yOffset, "Population", cost.Population, 
            playerState.PopulationStockpile, new Color(100, 200, 100));
        yOffset += 22;
        
        DrawCostLine(spriteBatch, tooltipX + 8, yOffset, "Metal", cost.Metal, 
            playerState.MetalStockpile, new Color(180, 180, 180));
        yOffset += 22;
        
        DrawCostLine(spriteBatch, tooltipX + 8, yOffset, "Fuel", cost.Fuel, 
            playerState.FuelStockpile, new Color(220, 160, 80));
    }
    
    private void DrawCostLine(SpriteBatch spriteBatch, int x, int y, string name, int cost, int available, Color color)
    {
        if (_font == null || _pixelTexture == null) return;
        
        int iconSize = 14;
        Rectangle iconRect = new Rectangle(x, y + 2, iconSize, iconSize);
        spriteBatch.Draw(_pixelTexture, iconRect, color);
        
        bool canAfford = available >= cost;
        Color textColor = canAfford ? Color.White : Color.Red;
        
        string text = $"{name}: {cost} / {available}";
        spriteBatch.DrawString(_font, text, 
            new Vector2(x + iconSize + 6, y), textColor, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
    }
    
    private void DrawRectangleOutline(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness)
    {
        if (_pixelTexture == null) return;
        
        spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(_pixelTexture, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
    }
    
    private void PurchaseArmies(int count)
    {
        if (_currentPlayerId == null || _lastKnownGameId == null)
            return;
        
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

public class DashboardButton
{
    public Rectangle Bounds { get; set; }
    public string Text { get; set; } = "";
    public Action? Action { get; set; }
    public Color Color { get; set; }
    public Color HoverColor { get; set; }
    public bool IsHovered { get; set; }
    public bool IsEnabled { get; set; } = true;
    public ResourceCost? Cost { get; set; }
}

public class ResourceCost
{
    public int Population { get; set; }
    public int Metal { get; set; }
    public int Fuel { get; set; }
}
