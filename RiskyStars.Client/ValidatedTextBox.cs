using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.Brushes;
using System;

namespace RiskyStars.Client;

/// <summary>
/// A TextBox with built-in validation and visual error feedback using tooltips
/// </summary>
public class ValidatedTextBox
{
    private readonly TextBox _textBox;
    private readonly Label? _errorLabel;
    private readonly Panel _container;
    private Func<string, ValidationResult>? _validator;
    private ValidationResult? _lastValidation;
    private bool _showErrorLabel;

    public TextBox TextBox => _textBox;
    public Panel Container => _container;
    public bool IsValid => _lastValidation?.IsValid ?? true;
    public string? ErrorMessage => _lastValidation?.IsValid == false ? _lastValidation.Message : null;

    public string Text
    {
        get => _textBox.Text;
        set => _textBox.Text = value;
    }

    public ValidatedTextBox(int width, string placeholder = "", bool showErrorLabel = false)
    {
        _showErrorLabel = showErrorLabel;

        // Create the main text box using themed factory
        _textBox = ThemedUIFactory.CreateTextBox("", width);
        _textBox.HintText = placeholder;

        // Set up real-time validation on text change
        _textBox.TextChanged += (s, a) => ValidateInput();

        if (_showErrorLabel)
        {
            // Create container with error label
            var grid = new Grid
            {
                RowSpacing = 4
            };
            grid.RowsProportions.Add(new Proportion(ProportionType.Auto));
            grid.RowsProportions.Add(new Proportion(ProportionType.Auto));

            _textBox.GridRow = 0;
            grid.Widgets.Add(_textBox);

            _errorLabel = new Label
            {
                Text = "",
                TextColor = ThemeManager.Colors.TextError,
                Scale = ThemeManager.FontScale.Small,
                GridRow = 1,
                Visible = false
            };
            grid.Widgets.Add(_errorLabel);

            _container = new Panel();
            _container.Widgets.Add(grid);
        }
        else
        {
            // Simple container with just the text box
            _container = new Panel();
            _container.Widgets.Add(_textBox);
        }
    }

    /// <summary>
    /// Sets the validation function for this text box
    /// </summary>
    public void SetValidator(Func<string, ValidationResult> validator)
    {
        _validator = validator;
        ValidateInput();
    }

    /// <summary>
    /// Manually trigger validation
    /// </summary>
    public ValidationResult ValidateInput()
    {
        if (_validator == null)
        {
            _lastValidation = new ValidationResult(true, "");
            UpdateVisualState();
            return _lastValidation;
        }

        _lastValidation = _validator(_textBox.Text);
        UpdateVisualState();
        return _lastValidation;
    }

    /// <summary>
    /// Update visual state based on validation result
    /// </summary>
    private void UpdateVisualState()
    {
        if (_lastValidation == null || _lastValidation.IsValid)
        {
            // Valid state - use default border colors
            _textBox.Border = ThemeManager.CreateSolidBrush(ThemeManager.Colors.BorderNormal);
            _textBox.OverBorder = ThemeManager.CreateSolidBrush(ThemeManager.Colors.BorderHover);
            _textBox.FocusedBorder = ThemeManager.CreateSolidBrush(ThemeManager.Colors.BorderFocus);

            if (_errorLabel != null)
            {
                _errorLabel.Visible = false;
                _errorLabel.Text = "";
            }
        }
        else
        {
            // Invalid state - use error border colors
            var errorBorder = ThemeManager.CreateSolidBrush(ThemeManager.Colors.TextError);
            _textBox.Border = errorBorder;
            _textBox.OverBorder = errorBorder;
            _textBox.FocusedBorder = errorBorder;

            if (_errorLabel != null)
            {
                _errorLabel.Visible = true;
                _errorLabel.Text = _lastValidation.Message;
            }
        }
    }

    /// <summary>
    /// Clear any validation errors
    /// </summary>
    public void ClearValidation()
    {
        _lastValidation = null;
        UpdateVisualState();
    }
}
