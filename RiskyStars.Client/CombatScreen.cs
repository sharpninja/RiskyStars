using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RiskyStars.Shared;

namespace RiskyStars.Client;

public class CombatScreen
{
    private readonly GraphicsDevice _graphicsDevice;
    private Texture2D? _pixelTexture;
    private SpriteFont? _font;
    private readonly int _screenWidth;
    private readonly int _screenHeight;

    private CombatEvent? _currentCombatEvent;
    private int _currentRoundIndex;
    private CombatAnimationState _animationState;
    private double _animationTimer;
    
    private List<AnimatedDiceRoll> _animatedAttackerRolls;
    private List<AnimatedDiceRoll> _animatedDefenderRolls;
    private List<AnimatedRollPairing> _animatedPairings;
    private List<AnimatedCasualty> _animatedCasualties;
    private bool _showReinforcementMessage;
    
    private KeyboardState _previousKeyState;
    
    public bool IsActive { get; private set; }
    public bool IsComplete { get; private set; }

    public CombatScreen(GraphicsDevice graphicsDevice, int screenWidth, int screenHeight)
    {
        _graphicsDevice = graphicsDevice;
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
        _animatedAttackerRolls = new List<AnimatedDiceRoll>();
        _animatedDefenderRolls = new List<AnimatedDiceRoll>();
        _animatedPairings = new List<AnimatedRollPairing>();
        _animatedCasualties = new List<AnimatedCasualty>();
        _animationState = CombatAnimationState.Idle;
        CreatePixelTexture();
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

    public void StartCombat(CombatEvent combatEvent)
    {
        _currentCombatEvent = combatEvent;
        _currentRoundIndex = 0;
        IsActive = true;
        IsComplete = false;
        _animationState = CombatAnimationState.ShowingIntro;
        _animationTimer = 0;
        _showReinforcementMessage = combatEvent.EventType == CombatEvent.Types.CombatEventType.ReinforcementsArrived;
        
        PrepareRoundAnimation();
    }

    public void Close()
    {
        IsActive = false;
        _currentCombatEvent = null;
        _animatedAttackerRolls.Clear();
        _animatedDefenderRolls.Clear();
        _animatedPairings.Clear();
        _animatedCasualties.Clear();
    }

    public void Update(GameTime gameTime)
    {
        if (!IsActive || _currentCombatEvent == null)
        {
            return;
        }

        var keyState = Keyboard.GetState();
        
        if (IsKeyPressed(keyState, Keys.Enter) || IsKeyPressed(keyState, Keys.Space))
        {
            AdvanceAnimation();
        }
        
        if (IsKeyPressed(keyState, Keys.Escape))
        {
            Close();
        }
        
        _previousKeyState = keyState;
        
        _animationTimer += gameTime.ElapsedGameTime.TotalSeconds;
        
        UpdateAnimations(gameTime);
        
        if (_animationState == CombatAnimationState.RoundComplete && _animationTimer > 1.0)
        {
            if (_currentRoundIndex < _currentCombatEvent.RoundResults.Count - 1)
            {
                _currentRoundIndex++;
                PrepareRoundAnimation();
            }
            else if (_currentCombatEvent.EventType == CombatEvent.Types.CombatEventType.CombatEnded)
            {
                _animationState = CombatAnimationState.ShowingOutro;
                _animationTimer = 0;
            }
        }
        
        if (_animationState == CombatAnimationState.ShowingOutro && _animationTimer > 2.0)
        {
            IsComplete = true;
        }
    }

    private void AdvanceAnimation()
    {
        switch (_animationState)
        {
            case CombatAnimationState.ShowingIntro:
                _animationState = CombatAnimationState.RollingDice;
                _animationTimer = 0;
                break;
            case CombatAnimationState.RollingDice:
                if (_animationTimer > 0.5)
                {
                    _animationState = CombatAnimationState.ShowingRolls;
                    _animationTimer = 0;
                }
                break;
            case CombatAnimationState.ShowingRolls:
                _animationState = CombatAnimationState.ShowingPairings;
                _animationTimer = 0;
                break;
            case CombatAnimationState.ShowingPairings:
                _animationState = CombatAnimationState.ShowingCasualties;
                _animationTimer = 0;
                break;
            case CombatAnimationState.ShowingCasualties:
                _animationState = CombatAnimationState.RoundComplete;
                _animationTimer = 0;
                break;
            case CombatAnimationState.RoundComplete:
                if (_currentRoundIndex < _currentCombatEvent!.RoundResults.Count - 1)
                {
                    _currentRoundIndex++;
                    PrepareRoundAnimation();
                }
                else
                {
                    _animationState = CombatAnimationState.ShowingOutro;
                    _animationTimer = 0;
                }
                break;
            case CombatAnimationState.ShowingOutro:
                IsComplete = true;
                break;
        }
    }

    private void PrepareRoundAnimation()
    {
        _animationState = CombatAnimationState.ShowingIntro;
        _animationTimer = 0;
        _animatedAttackerRolls.Clear();
        _animatedDefenderRolls.Clear();
        _animatedPairings.Clear();
        _animatedCasualties.Clear();

        if (_currentCombatEvent == null || _currentRoundIndex >= _currentCombatEvent.RoundResults.Count)
        {
            return;
        }

        var round = _currentCombatEvent.RoundResults[_currentRoundIndex];

        for (int i = 0; i < round.AttackerRolls.Count; i++)
        {
            var roll = round.AttackerRolls[i];
            _animatedAttackerRolls.Add(new AnimatedDiceRoll
            {
                Roll = roll,
                DisplayValue = roll.Roll,
                IsRevealed = false,
                RevealDelay = i * 0.1,
                Position = new Vector2(100 + i * 60, 300)
            });
        }

        for (int i = 0; i < round.DefenderRolls.Count; i++)
        {
            var roll = round.DefenderRolls[i];
            _animatedDefenderRolls.Add(new AnimatedDiceRoll
            {
                Roll = roll,
                DisplayValue = roll.Roll,
                IsRevealed = false,
                RevealDelay = i * 0.1,
                Position = new Vector2(_screenWidth - 100 - i * 60, 300)
            });
        }

        foreach (var pairing in round.Pairings)
        {
            _animatedPairings.Add(new AnimatedRollPairing
            {
                Pairing = pairing,
                IsRevealed = false
            });
        }

        foreach (var casualty in round.Casualties)
        {
            _animatedCasualties.Add(new AnimatedCasualty
            {
                Casualty = casualty,
                IsRevealed = false
            });
        }
    }

    private void UpdateAnimations(GameTime gameTime)
    {
        if (_animationState == CombatAnimationState.RollingDice)
        {
            foreach (var roll in _animatedAttackerRolls.Concat(_animatedDefenderRolls))
            {
                if (_animationTimer >= roll.RevealDelay && !roll.IsRevealed)
                {
                    roll.IsRevealed = true;
                }
            }
        }
        else if (_animationState == CombatAnimationState.ShowingPairings)
        {
            int revealedCount = (int)(_animationTimer * 3);
            for (int i = 0; i < Math.Min(revealedCount, _animatedPairings.Count); i++)
            {
                _animatedPairings[i].IsRevealed = true;
            }
        }
        else if (_animationState == CombatAnimationState.ShowingCasualties)
        {
            int revealedCount = (int)(_animationTimer * 2);
            for (int i = 0; i < Math.Min(revealedCount, _animatedCasualties.Count); i++)
            {
                _animatedCasualties[i].IsRevealed = true;
            }
        }
    }

    private bool IsKeyPressed(KeyboardState keyState, Keys key)
    {
        return keyState.IsKeyDown(key) && _previousKeyState.IsKeyUp(key);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (!IsActive || _currentCombatEvent == null || _pixelTexture == null || _font == null)
        {
            return;
        }

        spriteBatch.Begin(
            sortMode: SpriteSortMode.Deferred,
            blendState: BlendState.AlphaBlend,
            samplerState: SamplerState.LinearClamp);

        DrawBackground(spriteBatch);
        DrawCombatTitle(spriteBatch);
        
        if (_showReinforcementMessage)
        {
            DrawReinforcementMessage(spriteBatch);
        }
        
        if (_animationState != CombatAnimationState.Idle)
        {
            DrawRoundInfo(spriteBatch);
            DrawArmyStates(spriteBatch);
            
            if (_animationState >= CombatAnimationState.RollingDice)
            {
                DrawDiceRolls(spriteBatch);
            }
            
            if (_animationState >= CombatAnimationState.ShowingPairings)
            {
                DrawPairings(spriteBatch);
            }
            
            if (_animationState >= CombatAnimationState.ShowingCasualties)
            {
                DrawCasualties(spriteBatch);
            }
            
            if (_animationState == CombatAnimationState.ShowingOutro)
            {
                DrawOutro(spriteBatch);
            }
        }
        
        DrawInstructions(spriteBatch);

        spriteBatch.End();
    }

    private void DrawBackground(SpriteBatch spriteBatch)
    {
        if (_pixelTexture == null)
        {
            return;
        }

        var overlay = new Rectangle(0, 0, _screenWidth, _screenHeight);
        spriteBatch.Draw(_pixelTexture, overlay, Color.Black * 0.9f);
    }

    private void DrawCombatTitle(SpriteBatch spriteBatch)
    {
        if (_font == null || _currentCombatEvent == null)
        {
            return;
        }

        string title = _currentCombatEvent.EventType switch
        {
            CombatEvent.Types.CombatEventType.CombatInitiated => "COMBAT INITIATED",
            CombatEvent.Types.CombatEventType.CombatRoundComplete => "COMBAT IN PROGRESS",
            CombatEvent.Types.CombatEventType.CombatEnded => "COMBAT ENDED",
            CombatEvent.Types.CombatEventType.ReinforcementsArrived => "REINFORCEMENTS ARRIVED",
            _ => "COMBAT EVENT"
        };

        var titleSize = _font.MeasureString(title);
        spriteBatch.DrawString(_font, title,
            new Vector2((_screenWidth - titleSize.X) / 2, 20),
            Color.Red, 0f, Vector2.Zero, 1.2f, SpriteEffects.None, 0f);

        string locationText = $"Location: {_currentCombatEvent.LocationId}";
        var locationSize = _font.MeasureString(locationText);
        spriteBatch.DrawString(_font, locationText,
            new Vector2((_screenWidth - locationSize.X) / 2, 60),
            Color.Yellow, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
    }

    private void DrawReinforcementMessage(SpriteBatch spriteBatch)
    {
        if (_font == null || _pixelTexture == null)
        {
            return;
        }

        int msgWidth = 500;
        int msgHeight = 80;
        int msgX = (_screenWidth - msgWidth) / 2;
        int msgY = 120;

        var msgRect = new Rectangle(msgX, msgY, msgWidth, msgHeight);
        spriteBatch.Draw(_pixelTexture, msgRect, Color.DarkGreen * 0.8f);
        DrawRectangleOutline(spriteBatch, msgRect, Color.Green, 2);

        string message = "New reinforcements have arrived at the battlefield!";
        var msgSize = _font.MeasureString(message);
        spriteBatch.DrawString(_font, message,
            new Vector2(msgX + (msgWidth - msgSize.X) / 2, msgY + (msgHeight - msgSize.Y) / 2),
            Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
    }

    private void DrawRoundInfo(SpriteBatch spriteBatch)
    {
        if (_font == null || _currentCombatEvent == null)
        {
            return;
        }

        string roundText = $"Round {_currentRoundIndex + 1} of {_currentCombatEvent.RoundResults.Count}";
        var roundSize = _font.MeasureString(roundText);
        spriteBatch.DrawString(_font, roundText,
            new Vector2((_screenWidth - roundSize.X) / 2, 100),
            Color.White, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);
    }

    private void DrawArmyStates(SpriteBatch spriteBatch)
    {
        if (_font == null || _pixelTexture == null || _currentCombatEvent == null)
        {
            return;
        }

        var attackers = _currentCombatEvent.ArmyStates.Where(a => a.CombatRole == "Attacker").ToList();
        var defenders = _currentCombatEvent.ArmyStates.Where(a => a.CombatRole == "Defender").ToList();

        int panelWidth = 250;
        int panelHeight = 100 + attackers.Count * 30;
        int leftPanelX = 20;
        int panelY = 140;

        var leftPanel = new Rectangle(leftPanelX, panelY, panelWidth, panelHeight);
        spriteBatch.Draw(_pixelTexture, leftPanel, Color.DarkRed * 0.7f);
        DrawRectangleOutline(spriteBatch, leftPanel, Color.Red, 2);

        int yOffset = panelY + 10;
        spriteBatch.DrawString(_font, "ATTACKERS", new Vector2(leftPanelX + 10, yOffset), Color.Red);
        yOffset += 30;

        foreach (var army in attackers)
        {
            string armyText = $"{army.PlayerId}: {army.UnitCount} units";
            spriteBatch.DrawString(_font, armyText,
                new Vector2(leftPanelX + 10, yOffset), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            yOffset += 25;
        }

        int rightPanelX = _screenWidth - panelWidth - 20;
        panelHeight = 100 + defenders.Count * 30;
        var rightPanel = new Rectangle(rightPanelX, panelY, panelWidth, panelHeight);
        spriteBatch.Draw(_pixelTexture, rightPanel, Color.DarkBlue * 0.7f);
        DrawRectangleOutline(spriteBatch, rightPanel, Color.Blue, 2);

        yOffset = panelY + 10;
        spriteBatch.DrawString(_font, "DEFENDERS", new Vector2(rightPanelX + 10, yOffset), Color.Blue);
        yOffset += 30;

        foreach (var army in defenders)
        {
            string armyText = $"{army.PlayerId}: {army.UnitCount} units";
            spriteBatch.DrawString(_font, armyText,
                new Vector2(rightPanelX + 10, yOffset), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            yOffset += 25;
        }
    }

    private void DrawDiceRolls(SpriteBatch spriteBatch)
    {
        if (_font == null || _pixelTexture == null)
        {
            return;
        }

        spriteBatch.DrawString(_font, "Attacker Rolls:",
            new Vector2(80, 260), Color.Red, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);

        foreach (var roll in _animatedAttackerRolls)
        {
            if (roll.IsRevealed)
            {
                DrawDie(spriteBatch, roll.Position, roll.DisplayValue, Color.Red);
            }
            else
            {
                DrawDie(spriteBatch, roll.Position, 0, Color.Gray);
            }
        }

        var defenderLabelSize = _font.MeasureString("Defender Rolls:");
        spriteBatch.DrawString(_font, "Defender Rolls:",
            new Vector2(_screenWidth - 80 - defenderLabelSize.X, 260), Color.Blue, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);

        foreach (var roll in _animatedDefenderRolls)
        {
            if (roll.IsRevealed)
            {
                DrawDie(spriteBatch, roll.Position, roll.DisplayValue, Color.Blue);
            }
            else
            {
                DrawDie(spriteBatch, roll.Position, 0, Color.Gray);
            }
        }
    }

    private void DrawDie(SpriteBatch spriteBatch, Vector2 position, int value, Color color)
    {
        if (_pixelTexture == null || _font == null)
        {
            return;
        }

        int dieSize = 50;
        var dieRect = new Rectangle((int)position.X - dieSize / 2, (int)position.Y - dieSize / 2, dieSize, dieSize);
        
        spriteBatch.Draw(_pixelTexture, dieRect, color * 0.3f);
        DrawRectangleOutline(spriteBatch, dieRect, color, 2);

        if (value > 0)
        {
            string valueText = value.ToString();
            var valueSize = _font.MeasureString(valueText);
            spriteBatch.DrawString(_font, valueText,
                position - valueSize / 2,
                Color.White, 0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0f);
        }
        else
        {
            string questionText = "?";
            var questionSize = _font.MeasureString(questionText);
            spriteBatch.DrawString(_font, questionText,
                position - questionSize / 2,
                Color.DarkGray, 0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0f);
        }
    }

    private void DrawPairings(SpriteBatch spriteBatch)
    {
        if (_font == null || _pixelTexture == null)
        {
            return;
        }

        int panelWidth = 600;
        int panelHeight = 250;
        int panelX = (_screenWidth - panelWidth) / 2;
        int panelY = 370;

        var panel = new Rectangle(panelX, panelY, panelWidth, panelHeight);
        spriteBatch.Draw(_pixelTexture, panel, Color.Black * 0.8f);
        DrawRectangleOutline(spriteBatch, panel, Color.Yellow, 2);

        int yOffset = panelY + 10;
        spriteBatch.DrawString(_font, "Roll Pairings:", new Vector2(panelX + 10, yOffset), Color.Yellow);
        yOffset += 30;

        for (int i = 0; i < _animatedPairings.Count; i++)
        {
            var pairing = _animatedPairings[i];
            if (!pairing.IsRevealed)
            {
                continue;
            }

            var p = pairing.Pairing;
            string attackRoll = p.AttackerRoll?.Roll.ToString() ?? "N/A";
            string defendRoll = p.DefenderRoll?.Roll.ToString() ?? "N/A";
            string status = p.IsDiscarded ? "[DISCARDED]" : "";
            
            Color lineColor = Color.White;
            if (!p.IsDiscarded)
            {
                lineColor = p.WinnerArmyId == p.AttackerRoll?.ArmyId ? Color.Red : Color.Blue;
            }
            
            string pairingText = $"{i + 1}. Attacker {attackRoll} vs Defender {defendRoll} {status}";
            spriteBatch.DrawString(_font, pairingText,
                new Vector2(panelX + 20, yOffset), lineColor, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            yOffset += 25;

            if (yOffset > panelY + panelHeight - 30)
            {
                break;
            }
        }
    }

    private void DrawCasualties(SpriteBatch spriteBatch)
    {
        if (_font == null || _pixelTexture == null)
        {
            return;
        }

        int panelWidth = 500;
        int panelHeight = 200;
        int panelX = (_screenWidth - panelWidth) / 2;
        int panelY = _screenHeight - panelHeight - 60;

        var panel = new Rectangle(panelX, panelY, panelWidth, panelHeight);
        spriteBatch.Draw(_pixelTexture, panel, Color.DarkRed * 0.8f);
        DrawRectangleOutline(spriteBatch, panel, Color.Red, 2);

        int yOffset = panelY + 10;
        spriteBatch.DrawString(_font, "Casualties:", new Vector2(panelX + 10, yOffset), Color.Red);
        yOffset += 30;

        foreach (var casualty in _animatedCasualties)
        {
            if (!casualty.IsRevealed)
            {
                continue;
            }

            var c = casualty.Casualty;
            string casualtyText = $"{c.PlayerId} ({c.CombatRole}): -{c.Casualties} units ({c.RemainingUnits} remaining)";
            Color textColor = c.Casualties > 0 ? Color.OrangeRed : Color.White;
            
            spriteBatch.DrawString(_font, casualtyText,
                new Vector2(panelX + 20, yOffset), textColor, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            yOffset += 25;
        }
    }

    private void DrawOutro(SpriteBatch spriteBatch)
    {
        if (_font == null || _pixelTexture == null || _currentCombatEvent == null)
        {
            return;
        }

        int panelWidth = 400;
        int panelHeight = 150;
        int panelX = (_screenWidth - panelWidth) / 2;
        int panelY = (_screenHeight - panelHeight) / 2;

        var panel = new Rectangle(panelX, panelY, panelWidth, panelHeight);
        spriteBatch.Draw(_pixelTexture, panel, Color.DarkGreen * 0.9f);
        DrawRectangleOutline(spriteBatch, panel, Color.Green, 3);

        string outroText = "Combat Complete!";
        var outroSize = _font.MeasureString(outroText);
        spriteBatch.DrawString(_font, outroText,
            new Vector2(panelX + (panelWidth - outroSize.X) / 2, panelY + 20),
            Color.Yellow, 0f, Vector2.Zero, 1.0f, SpriteEffects.None, 0f);

        var survivors = _currentCombatEvent.ArmyStates.Where(a => a.UnitCount > 0).ToList();
        if (survivors.Count > 0)
        {
            int yOffset = panelY + 60;
            spriteBatch.DrawString(_font, "Surviving Armies:",
                new Vector2(panelX + 20, yOffset), Color.White, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
            yOffset += 25;

            foreach (var army in survivors)
            {
                string armyText = $"{army.PlayerId}: {army.UnitCount} units";
                spriteBatch.DrawString(_font, armyText,
                    new Vector2(panelX + 30, yOffset), Color.LightGreen, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                yOffset += 20;
            }
        }
    }

    private void DrawInstructions(SpriteBatch spriteBatch)
    {
        if (_font == null)
        {
            return;
        }

        string instructions = _animationState == CombatAnimationState.ShowingOutro
            ? "Press ENTER/SPACE to close | ESC to skip"
            : "Press ENTER/SPACE to advance | ESC to skip";
        
        var instructionsSize = _font.MeasureString(instructions);
        spriteBatch.DrawString(_font, instructions,
            new Vector2((_screenWidth - instructionsSize.X) / 2, _screenHeight - 30),
            Color.Gray, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
    }

    private void DrawRectangleOutline(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness)
    {
        if (_pixelTexture == null)
        {
            return;
        }

        spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(_pixelTexture, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(_pixelTexture, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
    }
}

public enum CombatAnimationState
{
    Idle,
    ShowingIntro,
    RollingDice,
    ShowingRolls,
    ShowingPairings,
    ShowingCasualties,
    RoundComplete,
    ShowingOutro
}

public class AnimatedDiceRoll
{
    public DiceRoll Roll { get; set; } = null!;
    public int DisplayValue { get; set; }
    public bool IsRevealed { get; set; }
    public double RevealDelay { get; set; }
    public Vector2 Position { get; set; }
}

public class AnimatedRollPairing
{
    public RollPairing Pairing { get; set; } = null!;
    public bool IsRevealed { get; set; }
}

public class AnimatedCasualty
{
    public ArmyCasualty Casualty { get; set; } = null!;
    public bool IsRevealed { get; set; }
}
