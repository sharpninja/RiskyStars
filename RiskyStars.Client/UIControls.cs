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

public class CheckboxField
{
    private Rectangle _bounds;
    private string _label;
    private bool _isChecked;
    private bool _isHovered;
    private bool _wasPressed;

    public bool IsChecked
    {
        get => _isChecked;
        set => _isChecked = value;
    }

    public CheckboxField(Rectangle bounds, string label)
    {
        _bounds = bounds;
        _label = label;
    }

    public void Update(MouseState mouseState)
    {
        var checkboxBounds = new Rectangle(_bounds.X, _bounds.Y, _bounds.Height, _bounds.Height);
        _isHovered = checkboxBounds.Contains(mouseState.Position);

        if (_isHovered && mouseState.LeftButton == ButtonState.Pressed)
        {
            _wasPressed = true;
        }
        else if (_wasPressed && mouseState.LeftButton == ButtonState.Released && _isHovered)
        {
            _isChecked = !_isChecked;
            _wasPressed = false;
        }
        else if (mouseState.LeftButton == ButtonState.Released)
        {
            _wasPressed = false;
        }
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture, SpriteFont font)
    {
        var labelSize = font.MeasureString(_label);
        spriteBatch.DrawString(font, _label,
            new Vector2(_bounds.X, _bounds.Y - labelSize.Y - 5),
            Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);

        var checkboxBounds = new Rectangle(_bounds.X, _bounds.Y, _bounds.Height, _bounds.Height);
        Color backgroundColor = _isHovered ? new Color(40, 40, 60) : new Color(30, 30, 40);
        Color borderColor = _isHovered ? Color.Cyan : Color.Gray;

        spriteBatch.Draw(pixelTexture, checkboxBounds, backgroundColor);

        int thickness = 2;
        spriteBatch.Draw(pixelTexture, new Rectangle(checkboxBounds.X, checkboxBounds.Y, checkboxBounds.Width, thickness), borderColor);
        spriteBatch.Draw(pixelTexture, new Rectangle(checkboxBounds.X, checkboxBounds.Y, thickness, checkboxBounds.Height), borderColor);
        spriteBatch.Draw(pixelTexture, new Rectangle(checkboxBounds.Right - thickness, checkboxBounds.Y, thickness, checkboxBounds.Height), borderColor);
        spriteBatch.Draw(pixelTexture, new Rectangle(checkboxBounds.X, checkboxBounds.Bottom - thickness, checkboxBounds.Width, thickness), borderColor);

        if (_isChecked)
        {
            int padding = 6;
            var checkBounds = new Rectangle(
                checkboxBounds.X + padding,
                checkboxBounds.Y + padding,
                checkboxBounds.Width - padding * 2,
                checkboxBounds.Height - padding * 2);
            spriteBatch.Draw(pixelTexture, checkBounds, Color.Cyan);
        }

        var labelPosition = new Vector2(_bounds.X + _bounds.Height + 15, _bounds.Y + (_bounds.Height - labelSize.Y) / 2);
        spriteBatch.DrawString(font, _label, labelPosition, Color.White, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
    }
}

public class DropdownField
{
    private Rectangle _bounds;
    private string _label;
    private List<string> _options;
    private int _selectedIndex;
    private bool _isExpanded;
    private bool _isHovered;
    private int _hoveredOptionIndex = -1;
    private bool _wasPressed;

    public List<string> Options => _options;
    public int SelectedIndex
    {
        get => _selectedIndex;
        set => _selectedIndex = Math.Clamp(value, 0, _options.Count - 1);
    }

    public string SelectedValue => _selectedIndex >= 0 && _selectedIndex < _options.Count ? _options[_selectedIndex] : "";

    public DropdownField(Rectangle bounds, string label, List<string> options)
    {
        _bounds = bounds;
        _label = label;
        _options = options;
        _selectedIndex = 0;
    }

    public void Update(MouseState mouseState)
    {
        _isHovered = _bounds.Contains(mouseState.Position);
        _hoveredOptionIndex = -1;

        if (_isExpanded)
        {
            for (int i = 0; i < _options.Count; i++)
            {
                var optionBounds = new Rectangle(
                    _bounds.X,
                    _bounds.Y + _bounds.Height * (i + 1),
                    _bounds.Width,
                    _bounds.Height);

                if (optionBounds.Contains(mouseState.Position))
                {
                    _hoveredOptionIndex = i;
                    break;
                }
            }
        }

        if (mouseState.LeftButton == ButtonState.Pressed)
        {
            _wasPressed = true;
        }
        else if (_wasPressed && mouseState.LeftButton == ButtonState.Released)
        {
            if (_isExpanded)
            {
                if (_hoveredOptionIndex >= 0)
                {
                    _selectedIndex = _hoveredOptionIndex;
                }
                _isExpanded = false;
            }
            else if (_isHovered)
            {
                _isExpanded = true;
            }
            _wasPressed = false;
        }
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture, SpriteFont font)
    {
        var labelSize = font.MeasureString(_label);
        spriteBatch.DrawString(font, _label,
            new Vector2(_bounds.X, _bounds.Y - labelSize.Y - 5),
            Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);

        Color backgroundColor = _isHovered || _isExpanded ? new Color(40, 40, 60) : new Color(30, 30, 40);
        Color borderColor = _isHovered || _isExpanded ? Color.Cyan : Color.Gray;

        spriteBatch.Draw(pixelTexture, _bounds, backgroundColor);

        int thickness = 2;
        spriteBatch.Draw(pixelTexture, new Rectangle(_bounds.X, _bounds.Y, _bounds.Width, thickness), borderColor);
        spriteBatch.Draw(pixelTexture, new Rectangle(_bounds.X, _bounds.Y, thickness, _bounds.Height), borderColor);
        spriteBatch.Draw(pixelTexture, new Rectangle(_bounds.Right - thickness, _bounds.Y, thickness, _bounds.Height), borderColor);
        spriteBatch.Draw(pixelTexture, new Rectangle(_bounds.X, _bounds.Bottom - thickness, _bounds.Width, thickness), borderColor);

        var selectedText = SelectedValue;
        var textSize = font.MeasureString(selectedText);
        var textPosition = new Vector2(_bounds.X + 10, _bounds.Y + (_bounds.Height - textSize.Y) / 2);
        spriteBatch.DrawString(font, selectedText, textPosition, Color.White, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);

        var arrowText = _isExpanded ? "▲" : "▼";
        var arrowSize = font.MeasureString(arrowText);
        var arrowPosition = new Vector2(_bounds.Right - arrowSize.X - 10, _bounds.Y + (_bounds.Height - arrowSize.Y) / 2);
        spriteBatch.DrawString(font, arrowText, arrowPosition, Color.White, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);

        if (_isExpanded)
        {
            for (int i = 0; i < _options.Count; i++)
            {
                var optionBounds = new Rectangle(
                    _bounds.X,
                    _bounds.Y + _bounds.Height * (i + 1),
                    _bounds.Width,
                    _bounds.Height);

                Color optionBgColor = i == _hoveredOptionIndex ? new Color(50, 80, 120) : new Color(25, 25, 35);
                spriteBatch.Draw(pixelTexture, optionBounds, optionBgColor);

                spriteBatch.Draw(pixelTexture, new Rectangle(optionBounds.X, optionBounds.Y, optionBounds.Width, thickness), borderColor);
                spriteBatch.Draw(pixelTexture, new Rectangle(optionBounds.X, optionBounds.Y, thickness, optionBounds.Height), borderColor);
                spriteBatch.Draw(pixelTexture, new Rectangle(optionBounds.Right - thickness, optionBounds.Y, thickness, optionBounds.Height), borderColor);
                spriteBatch.Draw(pixelTexture, new Rectangle(optionBounds.X, optionBounds.Bottom - thickness, optionBounds.Width, thickness), borderColor);

                var optionText = _options[i];
                var optionTextSize = font.MeasureString(optionText);
                var optionTextPosition = new Vector2(optionBounds.X + 10, optionBounds.Y + (optionBounds.Height - optionTextSize.Y) / 2);
                spriteBatch.DrawString(font, optionText, optionTextPosition, Color.White, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
            }
        }
    }
}

public class RadioButton
{
    private Rectangle _bounds;
    private string _label;
    private string _groupName;
    private bool _isSelected;
    private bool _isHovered;
    private bool _wasPressed;

    public bool IsSelected
    {
        get => _isSelected;
        set => _isSelected = value;
    }

    public bool IsClicked { get; private set; }
    public string GroupName => _groupName;

    public RadioButton(Rectangle bounds, string label, string groupName)
    {
        _bounds = bounds;
        _label = label;
        _groupName = groupName;
    }

    public void Update(MouseState mouseState)
    {
        IsClicked = false;
        
        var radioBounds = new Rectangle(_bounds.X, _bounds.Y, _bounds.Height, _bounds.Height);
        _isHovered = radioBounds.Contains(mouseState.Position);

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
        var labelSize = font.MeasureString(_label);
        spriteBatch.DrawString(font, _label,
            new Vector2(_bounds.X, _bounds.Y - labelSize.Y - 5),
            Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);

        var radioBounds = new Rectangle(_bounds.X, _bounds.Y, _bounds.Height, _bounds.Height);
        Color backgroundColor = _isHovered ? new Color(40, 40, 60) : new Color(30, 30, 40);
        Color borderColor = _isHovered ? Color.Cyan : Color.Gray;

        spriteBatch.Draw(pixelTexture, radioBounds, backgroundColor);

        int thickness = 2;
        int centerX = radioBounds.X + radioBounds.Width / 2;
        int centerY = radioBounds.Y + radioBounds.Height / 2;
        int radius = radioBounds.Width / 2 - thickness;

        for (int angle = 0; angle < 360; angle += 5)
        {
            float radian = angle * (float)Math.PI / 180f;
            int x1 = centerX + (int)(radius * Math.Cos(radian));
            int y1 = centerY + (int)(radius * Math.Sin(radian));
            int x2 = centerX + (int)((radius + thickness) * Math.Cos(radian));
            int y2 = centerY + (int)((radius + thickness) * Math.Sin(radian));
            
            var lineRect = new Rectangle(x1, y1, Math.Max(1, Math.Abs(x2 - x1)), Math.Max(1, Math.Abs(y2 - y1)));
            spriteBatch.Draw(pixelTexture, lineRect, borderColor);
        }

        if (_isSelected)
        {
            int innerRadius = radius - 4;
            for (int dy = -innerRadius; dy <= innerRadius; dy++)
            {
                for (int dx = -innerRadius; dx <= innerRadius; dx++)
                {
                    if (dx * dx + dy * dy <= innerRadius * innerRadius)
                    {
                        var dotRect = new Rectangle(centerX + dx, centerY + dy, 1, 1);
                        spriteBatch.Draw(pixelTexture, dotRect, Color.Cyan);
                    }
                }
            }
        }

        var labelPosition = new Vector2(_bounds.X + _bounds.Height + 15, _bounds.Y + (_bounds.Height - labelSize.Y) / 2);
        spriteBatch.DrawString(font, _label, labelPosition, Color.White, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);
    }
}

public class RadioButtonGroup
{
    private List<RadioButton> _radioButtons = new();
    private int _selectedIndex = -1;

    public int SelectedIndex => _selectedIndex;

    public void AddRadioButton(RadioButton radioButton)
    {
        _radioButtons.Add(radioButton);
    }

    public void Update(MouseState mouseState)
    {
        for (int i = 0; i < _radioButtons.Count; i++)
        {
            _radioButtons[i].Update(mouseState);
            
            if (_radioButtons[i].IsClicked)
            {
                _selectedIndex = i;
                
                for (int j = 0; j < _radioButtons.Count; j++)
                {
                    _radioButtons[j].IsSelected = (j == i);
                }
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture, SpriteFont font)
    {
        foreach (var radioButton in _radioButtons)
        {
            radioButton.Draw(spriteBatch, pixelTexture, font);
        }
    }

    public void SetSelected(int index)
    {
        if (index >= 0 && index < _radioButtons.Count)
        {
            _selectedIndex = index;
            
            for (int i = 0; i < _radioButtons.Count; i++)
            {
                _radioButtons[i].IsSelected = (i == index);
            }
        }
    }
}
