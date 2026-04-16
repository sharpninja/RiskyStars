using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace RiskyStars.Client;

/// <summary>
/// A TextInputField with built-in validation and visual error feedback
/// </summary>
public class ValidatedTextInputField
{
    private readonly TextInputField _textField;
    private Func<string, ValidationResult>? _validator;
    private ValidationResult? _lastValidation;
    private string? _errorMessage;
    private Rectangle _bounds;

    public string Text
    {
        get => _textField.Text;
        set => _textField.Text = value;
    }

    public bool IsValid => _lastValidation?.IsValid ?? true;
    public string? ErrorMessage => _errorMessage;
    public bool IsFocused => _textField.IsFocused;

    public ValidatedTextInputField(Rectangle bounds, string label, int maxLength = 50)
    {
        _bounds = bounds;
        _textField = new TextInputField(bounds, label, maxLength);
    }

    public void SetValidator(Func<string, ValidationResult> validator)
    {
        _validator = validator;
    }

    public ValidationResult ValidateInput()
    {
        if (_validator == null)
        {
            _lastValidation = new ValidationResult(true, "");
            _errorMessage = null;
            return _lastValidation;
        }

        _lastValidation = _validator(_textField.Text);
        _errorMessage = _lastValidation.IsValid ? null : _lastValidation.Message;
        return _lastValidation;
    }

    public void Update(MouseState mouseState, KeyboardState keyState, KeyboardState previousKeyState)
    {
        _textField.Update(mouseState, keyState, previousKeyState);
        
        // Validate on text change
        if (_validator != null)
        {
            ValidateInput();
        }
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture, SpriteFont font)
    {
        // Draw the text field with error state
        _textField.Draw(spriteBatch, pixelTexture, font);

        // Draw error border if invalid
        if (_lastValidation != null && !_lastValidation.IsValid)
        {
            int thickness = ThemeManager.BorderThickness.Thick;
            var errorBorder = ThemeManager.Colors.TextError;
            
            spriteBatch.Draw(pixelTexture, new Rectangle(_bounds.X, _bounds.Y, _bounds.Width, thickness), errorBorder);
            spriteBatch.Draw(pixelTexture, new Rectangle(_bounds.X, _bounds.Y, thickness, _bounds.Height), errorBorder);
            spriteBatch.Draw(pixelTexture, new Rectangle(_bounds.Right - thickness, _bounds.Y, thickness, _bounds.Height), errorBorder);
            spriteBatch.Draw(pixelTexture, new Rectangle(_bounds.X, _bounds.Bottom - thickness, _bounds.Width, thickness), errorBorder);
        }

        // Draw error message below the field
        if (!string.IsNullOrEmpty(_errorMessage))
        {
            var errorMessageSize = font.MeasureString(_errorMessage);
            var errorMessagePosition = new Vector2(_bounds.X, _bounds.Bottom + 5);
            spriteBatch.DrawString(font, _errorMessage, errorMessagePosition, 
                ThemeManager.Colors.TextError, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
        }
    }

    public void ClearValidation()
    {
        _lastValidation = null;
        _errorMessage = null;
    }
}
