using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Text;

namespace RiskyStars.Client;

public class Button
{
    private Rectangle _bounds;
    private string _text;
    private bool _isHovered;
    private bool _wasPressed;

    public bool IsClicked { get; private set; }
    public bool IsEnabled { get; set; } = true;

    public Button(Rectangle bounds, string text)
    {
        _bounds = bounds;
        _text = text;
    }

    public void Update(MouseState mouseState)
    {
        IsClicked = false;
        
        if (!IsEnabled)
        {
            _isHovered = false;
            return;
        }

        _isHovered = _bounds.Contains(mouseState.Position);

        if (_isHovered && mouseState.LeftButton == ButtonState.Pressed)
        {
            _wasPressed = true;
        }
        else if (_wasPressed && mouseState.LeftButton == ButtonState.Released && _isHovered)
        {
            IsClicked = true;
            _wasPressed = false;
        }
        else if (mouseState.LeftButton == ButtonState.Released)
        {
            _wasPressed = false;
        }
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture, SpriteFont font)
    {
        Color backgroundColor = IsEnabled 
            ? (_isHovered ? new Color(50, 100, 150) : new Color(30, 60, 100))
            : new Color(60, 60, 60);
        
        Color borderColor = IsEnabled
            ? (_isHovered ? Color.White : Color.Gray)
            : Color.DarkGray;

        spriteBatch.Draw(pixelTexture, _bounds, backgroundColor);
        
        int thickness = 2;
        spriteBatch.Draw(pixelTexture, new Rectangle(_bounds.X, _bounds.Y, _bounds.Width, thickness), borderColor);
        spriteBatch.Draw(pixelTexture, new Rectangle(_bounds.X, _bounds.Y, thickness, _bounds.Height), borderColor);
        spriteBatch.Draw(pixelTexture, new Rectangle(_bounds.Right - thickness, _bounds.Y, thickness, _bounds.Height), borderColor);
        spriteBatch.Draw(pixelTexture, new Rectangle(_bounds.X, _bounds.Bottom - thickness, _bounds.Width, thickness), borderColor);

        var textSize = font.MeasureString(_text);
        var textPosition = new Vector2(
            _bounds.X + (_bounds.Width - textSize.X) / 2,
            _bounds.Y + (_bounds.Height - textSize.Y) / 2);

        Color textColor = IsEnabled ? Color.White : Color.Gray;
        spriteBatch.DrawString(font, _text, textPosition, textColor, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
    }
}

public class TextInputField
{
    private Rectangle _bounds;
    private string _label;
    private string _text = "";
    private bool _isFocused;
    private int _maxLength;
    private int _cursorPosition;
    private double _cursorBlinkTimer;
    private bool _cursorVisible = true;

    public string Text 
    { 
        get => _text;
        set => _text = value ?? "";
    }

    public bool IsFocused => _isFocused;

    public TextInputField(Rectangle bounds, string label, int maxLength = 50)
    {
        _bounds = bounds;
        _label = label;
        _maxLength = maxLength;
    }

    public void Update(MouseState mouseState, KeyboardState keyState, KeyboardState previousKeyState)
    {
        if (mouseState.LeftButton == ButtonState.Pressed)
        {
            _isFocused = _bounds.Contains(mouseState.Position);
        }

        if (!_isFocused)
            return;

        _cursorBlinkTimer += 16.67;
        if (_cursorBlinkTimer >= 500)
        {
            _cursorVisible = !_cursorVisible;
            _cursorBlinkTimer = 0;
        }

        var pressedKeys = keyState.GetPressedKeys();
        foreach (var key in pressedKeys)
        {
            if (previousKeyState.IsKeyUp(key))
            {
                if (key == Keys.Back && _text.Length > 0)
                {
                    _text = _text.Substring(0, _text.Length - 1);
                    _cursorPosition = _text.Length;
                }
                else if (key == Keys.Space && _text.Length < _maxLength)
                {
                    _text += " ";
                    _cursorPosition = _text.Length;
                }
                else if (_text.Length < _maxLength)
                {
                    string character = GetCharacterFromKey(key, keyState.IsKeyDown(Keys.LeftShift) || keyState.IsKeyDown(Keys.RightShift));
                    if (!string.IsNullOrEmpty(character))
                    {
                        _text += character;
                        _cursorPosition = _text.Length;
                    }
                }
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture, SpriteFont font)
    {
        Color backgroundColor = _isFocused ? new Color(40, 40, 60) : new Color(30, 30, 40);
        Color borderColor = _isFocused ? Color.Cyan : Color.Gray;

        spriteBatch.Draw(pixelTexture, _bounds, backgroundColor);

        int thickness = 2;
        spriteBatch.Draw(pixelTexture, new Rectangle(_bounds.X, _bounds.Y, _bounds.Width, thickness), borderColor);
        spriteBatch.Draw(pixelTexture, new Rectangle(_bounds.X, _bounds.Y, thickness, _bounds.Height), borderColor);
        spriteBatch.Draw(pixelTexture, new Rectangle(_bounds.Right - thickness, _bounds.Y, thickness, _bounds.Height), borderColor);
        spriteBatch.Draw(pixelTexture, new Rectangle(_bounds.X, _bounds.Bottom - thickness, _bounds.Width, thickness), borderColor);

        var labelSize = font.MeasureString(_label);
        spriteBatch.DrawString(font, _label,
            new Vector2(_bounds.X, _bounds.Y - labelSize.Y - 5),
            Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);

        string displayText = string.IsNullOrEmpty(_text) ? "" : _text;
        var textSize = font.MeasureString(displayText);
        var textPosition = new Vector2(_bounds.X + 10, _bounds.Y + (_bounds.Height - textSize.Y) / 2);

        spriteBatch.DrawString(font, displayText, textPosition, Color.White, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);

        if (_isFocused && _cursorVisible)
        {
            var cursorX = textPosition.X + (string.IsNullOrEmpty(_text) ? 0 : font.MeasureString(_text).X * 0.8f);
            var cursorRect = new Rectangle((int)cursorX, (int)textPosition.Y, 2, (int)(textSize.Y * 0.8f));
            spriteBatch.Draw(pixelTexture, cursorRect, Color.White);
        }
    }

    private string GetCharacterFromKey(Keys key, bool shift)
    {
        if (key >= Keys.A && key <= Keys.Z)
        {
            char c = (char)('a' + (key - Keys.A));
            return shift ? c.ToString().ToUpper() : c.ToString();
        }
        
        if (key >= Keys.D0 && key <= Keys.D9)
        {
            if (!shift)
                return ((char)('0' + (key - Keys.D0))).ToString();
            
            return key switch
            {
                Keys.D1 => "!",
                Keys.D2 => "@",
                Keys.D3 => "#",
                Keys.D4 => "$",
                Keys.D5 => "%",
                Keys.D6 => "^",
                Keys.D7 => "&",
                Keys.D8 => "*",
                Keys.D9 => "(",
                Keys.D0 => ")",
                _ => ""
            };
        }

        return key switch
        {
            Keys.OemPeriod => shift ? ">" : ".",
            Keys.OemComma => shift ? "<" : ",",
            Keys.OemQuestion => shift ? "?" : "/",
            Keys.OemSemicolon => shift ? ":" : ";",
            Keys.OemQuotes => shift ? "\"" : "'",
            Keys.OemOpenBrackets => shift ? "{" : "[",
            Keys.OemCloseBrackets => shift ? "}" : "]",
            Keys.OemPipe => shift ? "|" : "\\",
            Keys.OemMinus => shift ? "_" : "-",
            Keys.OemPlus => shift ? "+" : "=",
            _ => ""
        };
    }
}

public class NumericInputField
{
    private Rectangle _bounds;
    private string _label;
    private int _value;
    private int _minValue;
    private int _maxValue;
    private bool _isFocused;

    private Button _decrementButton;
    private Button _incrementButton;

    public int Value => _value;

    public NumericInputField(Rectangle bounds, string label, int initialValue, int minValue, int maxValue)
    {
        _bounds = bounds;
        _label = label;
        _value = Math.Clamp(initialValue, minValue, maxValue);
        _minValue = minValue;
        _maxValue = maxValue;

        int buttonSize = bounds.Height;
        _decrementButton = new Button(
            new Rectangle(bounds.X, bounds.Y, buttonSize, buttonSize),
            "-");
        _incrementButton = new Button(
            new Rectangle(bounds.Right - buttonSize, bounds.Y, buttonSize, buttonSize),
            "+");
    }

    public void Update(MouseState mouseState)
    {
        _decrementButton.Update(mouseState);
        _incrementButton.Update(mouseState);

        if (_decrementButton.IsClicked && _value > _minValue)
        {
            _value--;
        }

        if (_incrementButton.IsClicked && _value < _maxValue)
        {
            _value++;
        }

        var valueBounds = new Rectangle(
            _bounds.X + _bounds.Height + 5,
            _bounds.Y,
            _bounds.Width - _bounds.Height * 2 - 10,
            _bounds.Height);
        
        _isFocused = valueBounds.Contains(mouseState.Position);
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture, SpriteFont font)
    {
        var labelSize = font.MeasureString(_label);
        spriteBatch.DrawString(font, _label,
            new Vector2(_bounds.X, _bounds.Y - labelSize.Y - 5),
            Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);

        _decrementButton.Draw(spriteBatch, pixelTexture, font);
        _incrementButton.Draw(spriteBatch, pixelTexture, font);

        var valueBounds = new Rectangle(
            _bounds.X + _bounds.Height + 5,
            _bounds.Y,
            _bounds.Width - _bounds.Height * 2 - 10,
            _bounds.Height);

        Color backgroundColor = _isFocused ? new Color(40, 40, 60) : new Color(30, 30, 40);
        Color borderColor = _isFocused ? Color.Cyan : Color.Gray;

        spriteBatch.Draw(pixelTexture, valueBounds, backgroundColor);

        int thickness = 2;
        spriteBatch.Draw(pixelTexture, new Rectangle(valueBounds.X, valueBounds.Y, valueBounds.Width, thickness), borderColor);
        spriteBatch.Draw(pixelTexture, new Rectangle(valueBounds.X, valueBounds.Y, thickness, valueBounds.Height), borderColor);
        spriteBatch.Draw(pixelTexture, new Rectangle(valueBounds.Right - thickness, valueBounds.Y, thickness, valueBounds.Height), borderColor);
        spriteBatch.Draw(pixelTexture, new Rectangle(valueBounds.X, valueBounds.Bottom - thickness, valueBounds.Width, thickness), borderColor);

        var valueText = _value.ToString();
        var valueSize = font.MeasureString(valueText);
        var textPosition = new Vector2(
            valueBounds.X + (valueBounds.Width - valueSize.X) / 2,
            valueBounds.Y + (valueBounds.Height - valueSize.Y) / 2);

        spriteBatch.DrawString(font, valueText, textPosition, Color.White, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);
    }
}
