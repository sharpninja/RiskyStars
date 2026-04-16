using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI;
using Myra.Graphics2D;
using System;
using static RiskyStars.Client.ThemeManager;

namespace RiskyStars.Client;

public enum DialogType
{
    Info,
    Warning,
    Error,
    Success,
    Question,
    CombatEvent
}

public enum DialogResult
{
    None,
    OK,
    Cancel,
    Yes,
    No,
    Retry,
    Close
}

public class DialogManager
{
    private Desktop? _desktop;
    private Dialog? _currentDialog;
    private DialogResult _result = DialogResult.None;
    private Action<DialogResult>? _callback;

    public bool IsDialogOpen => _currentDialog != null;
    public DialogResult LastResult => _result;

    public DialogManager(Desktop desktop)
    {
        _desktop = desktop;
    }

    public void ShowError(string title, string message, Action<DialogResult>? callback = null)
    {
        ShowDialog(DialogType.Error, title, message, new[] { "OK" }, callback);
    }

    public void ShowWarning(string title, string message, Action<DialogResult>? callback = null)
    {
        ShowDialog(DialogType.Warning, title, message, new[] { "OK" }, callback);
    }

    public void ShowInfo(string title, string message, Action<DialogResult>? callback = null)
    {
        ShowDialog(DialogType.Info, title, message, new[] { "OK" }, callback);
    }

    public void ShowSuccess(string title, string message, Action<DialogResult>? callback = null)
    {
        ShowDialog(DialogType.Success, title, message, new[] { "OK" }, callback);
    }

    public void ShowQuestion(string title, string message, Action<DialogResult>? callback = null)
    {
        ShowDialog(DialogType.Question, title, message, new[] { "Yes", "No" }, callback);
    }

    public void ShowConfirmation(string title, string message, Action<DialogResult>? callback = null)
    {
        ShowDialog(DialogType.Question, title, message, new[] { "OK", "Cancel" }, callback);
    }

    public void ShowRetryDialog(string title, string message, Action<DialogResult>? callback = null)
    {
        ShowDialog(DialogType.Error, title, message, new[] { "Retry", "Cancel" }, callback);
    }

    public void ShowCombatEvent(string title, string message, Action<DialogResult>? callback = null)
    {
        ShowDialog(DialogType.CombatEvent, title, message, new[] { "Continue" }, callback);
    }

    public void ShowDialog(DialogType type, string title, string message, string[] buttons, Action<DialogResult>? callback = null)
    {
        CloseDialog();

        _callback = callback;
        _result = DialogResult.None;

        var dialog = CreateDialog(type, title, message, buttons);
        _currentDialog = dialog;

        if (_desktop != null)
        {
            dialog.ShowModal(_desktop);
        }
    }

    private Dialog CreateDialog(DialogType type, string title, string message, string[] buttons)
    {
        var dialog = new Dialog();

        var mainGrid = ThemedUIFactory.CreateGrid(Spacing.Medium, Spacing.Medium);
        mainGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        mainGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        mainGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));

        var titleLabel = CreateTitleLabel(type, title);
        titleLabel.GridRow = 0;
        mainGrid.Widgets.Add(titleLabel);

        var messageLabel = CreateMessageLabel(message);
        messageLabel.GridRow = 1;
        messageLabel.Margin = new Thickness(0, Spacing.Large, 0, Spacing.Large);
        mainGrid.Widgets.Add(messageLabel);

        var buttonsPanel = CreateButtonsPanel(buttons);
        buttonsPanel.GridRow = 2;
        mainGrid.Widgets.Add(buttonsPanel);

        var containerPanel = ThemedUIFactory.CreateAccentFramePanel();
        containerPanel.Width = Math.Max(400, Math.Min(600, message.Length * 6));
        containerPanel.Padding = Padding.XLarge;
        containerPanel.Widgets.Add(mainGrid);

        ApplyDialogTheme(containerPanel, type);

        dialog.Content = containerPanel;
        
        return dialog;
    }

    private Label CreateTitleLabel(DialogType type, string title)
    {
        var label = ThemedUIFactory.CreateHeadingLabel(title);
        label.HorizontalAlignment = HorizontalAlignment.Center;

        label.TextColor = type switch
        {
            DialogType.Error => Colors.TextError,
            DialogType.Warning => Colors.TextWarning,
            DialogType.Success => Colors.TextSuccess,
            DialogType.CombatEvent => Color.Orange,
            DialogType.Question => Colors.TextAccent,
            _ => Colors.TextPrimary
        };

        return label;
    }

    private Label CreateMessageLabel(string message)
    {
        var label = new Label
        {
            Text = message,
            TextColor = Colors.TextPrimary,
            HorizontalAlignment = HorizontalAlignment.Center,
            Wrap = true,
            Width = 500
        };

        return label;
    }

    private Widget CreateButtonsPanel(string[] buttons)
    {
        var panel = new HorizontalStackPanel
        {
            Spacing = Spacing.Medium,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        foreach (var buttonText in buttons)
        {
            var button = CreateDialogButton(buttonText);
            panel.Widgets.Add(button);
        }

        return panel;
    }

    private TextButton CreateDialogButton(string text)
    {
        var theme = text.ToLower() switch
        {
            "ok" or "yes" or "continue" or "retry" => ButtonTheme.Primary,
            "cancel" or "no" => ButtonTheme.Danger,
            "close" => ButtonTheme.Default,
            _ => ButtonTheme.Default
        };

        var button = ThemedUIFactory.CreateButton(text, Sizes.ButtonSmallWidth + 20, Sizes.ButtonMediumHeight, theme);
        
        button.Click += (s, a) =>
        {
            var result = text.ToLower() switch
            {
                "ok" => DialogResult.OK,
                "cancel" => DialogResult.Cancel,
                "yes" => DialogResult.Yes,
                "no" => DialogResult.No,
                "retry" => DialogResult.Retry,
                "close" => DialogResult.Close,
                "continue" => DialogResult.OK,
                _ => DialogResult.None
            };

            OnDialogClosed(result);
        };

        return button;
    }

    private void ApplyDialogTheme(Panel panel, DialogType type)
    {
        var borderColor = type switch
        {
            DialogType.Error => Colors.TextError,
            DialogType.Warning => Colors.TextWarning,
            DialogType.Success => Colors.TextSuccess,
            DialogType.CombatEvent => Color.Orange,
            DialogType.Question => Colors.AccentCyan,
            _ => Colors.BorderNormal
        };

        panel.Border = ThemeManager.CreateSolidBrush(borderColor);
        panel.BorderThickness = new Thickness(BorderThickness.Thick);
    }

    private void OnDialogClosed(DialogResult result)
    {
        _result = result;
        _callback?.Invoke(result);
        CloseDialog();
    }

    public void CloseDialog()
    {
        if (_currentDialog != null)
        {
            _currentDialog.Close();
            _currentDialog = null;
        }
    }

    public void Update()
    {
    }
}
