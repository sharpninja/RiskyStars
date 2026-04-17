using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI;
using Myra.Graphics2D;
using RiskyStars.Shared;
using System;
using System.Linq;
using static RiskyStars.Client.ThemeManager;

namespace RiskyStars.Client;

public class CombatEventDialog
{
    private Desktop? _desktop;
    private Dialog? _currentDialog;
    private Action? _onClosed;

    public bool IsOpen => _currentDialog != null;

    public CombatEventDialog(Desktop desktop)
    {
        _desktop = desktop;
    }

    public void ShowCombatInitiated(CombatEvent combatEvent, Action? onClosed = null)
    {
        _onClosed = onClosed;
        var message = BuildCombatInitiatedMessage(combatEvent);
        ShowDialog("Combat Initiated", message, "View Combat");
    }

    public void ShowReinforcementsArrived(CombatEvent combatEvent, Action? onClosed = null)
    {
        _onClosed = onClosed;
        var message = BuildReinforcementsMessage(combatEvent);
        ShowDialog("Reinforcements Arrived", message, "Continue");
    }

    public void ShowCombatEnded(CombatEvent combatEvent, Action? onClosed = null)
    {
        _onClosed = onClosed;
        var message = BuildCombatEndedMessage(combatEvent);
        ShowDialog("Combat Complete", message, "OK");
    }

    private string BuildCombatInitiatedMessage(CombatEvent combatEvent)
    {
        var attackers = combatEvent.ArmyStates.Where(a => a.CombatRole == "Attacker").ToList();
        var defenders = combatEvent.ArmyStates.Where(a => a.CombatRole == "Defender").ToList();

        var message = $"Combat has begun at {combatEvent.LocationId}!\n\n";
        
        message += "Attackers:\n";
        foreach (var army in attackers)
        {
            message += $"  {army.PlayerId}: {army.UnitCount} units\n";
        }
        
        message += "\nDefenders:\n";
        foreach (var army in defenders)
        {
            message += $"  {army.PlayerId}: {army.UnitCount} units\n";
        }

        return message;
    }

    private string BuildReinforcementsMessage(CombatEvent combatEvent)
    {
        var message = $"Reinforcements have arrived at {combatEvent.LocationId}!\n\n";
        
        message += "Current Forces:\n";
        foreach (var army in combatEvent.ArmyStates)
        {
            message += $"  {army.PlayerId} ({army.CombatRole}): {army.UnitCount} units\n";
        }

        return message;
    }

    private string BuildCombatEndedMessage(CombatEvent combatEvent)
    {
        var survivors = combatEvent.ArmyStates.Where(a => a.UnitCount > 0).ToList();
        var eliminated = combatEvent.ArmyStates.Where(a => a.UnitCount == 0).ToList();

        var message = $"The battle at {combatEvent.LocationId} has concluded!\n\n";

        if (survivors.Count > 0)
        {
            message += "Victors:\n";
            foreach (var army in survivors)
            {
                message += $"  {army.PlayerId}: {army.UnitCount} units remaining\n";
            }
        }

        if (eliminated.Count > 0)
        {
            message += "\nEliminated:\n";
            foreach (var army in eliminated)
            {
                message += $"  {army.PlayerId}\n";
            }
        }

        return message;
    }

    private void ShowDialog(string title, string message, string buttonText)
    {
        CloseDialog();

        var dialog = new Dialog();

        var mainGrid = ThemedUIFactory.CreateGrid(Spacing.Medium, Spacing.Medium);
        mainGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        mainGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        mainGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));

        var titleLabel = ThemedUIFactory.CreateHeadingLabel(title);
        titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        titleLabel.TextColor = Color.Orange;
#pragma warning disable CS0618 // Type or member is obsolete
        titleLabel.GridRow = 0;
#pragma warning restore CS0618 // Type or member is obsolete
        mainGrid.Widgets.Add(titleLabel);

#pragma warning disable CS0618 // Type or member is obsolete
        var messageLabel = new Label
        {
            Text = message,
            TextColor = Colors.TextPrimary,
            HorizontalAlignment = HorizontalAlignment.Left,
            Wrap = true,
            Width = 450,
            GridRow = 1,
            Margin = new Thickness(0, Spacing.Large, 0, Spacing.Large)
        };
#pragma warning restore CS0618 // Type or member is obsolete
        mainGrid.Widgets.Add(messageLabel);

        var button = ThemedUIFactory.CreateButton(buttonText, Sizes.ButtonMediumWidth, Sizes.ButtonMediumHeight, ButtonTheme.Primary);
        button.HorizontalAlignment = HorizontalAlignment.Center;
#pragma warning disable CS0618 // Type or member is obsolete
        button.GridRow = 2;
#pragma warning restore CS0618 // Type or member is obsolete
        button.Click += (s, a) =>
        {
            _onClosed?.Invoke();
            CloseDialog();
        };
        mainGrid.Widgets.Add(button);

        var containerPanel = ThemedUIFactory.CreateAccentFramePanel();
        containerPanel.Width = 500;
        containerPanel.Padding = Padding.XLarge;
        containerPanel.Border = ThemeManager.CreateSolidBrush(Color.Orange);
        containerPanel.BorderThickness = new Thickness(BorderThickness.Thick);
        containerPanel.Widgets.Add(mainGrid);

        dialog.Content = containerPanel;
        _currentDialog = dialog;

        if (_desktop != null)
        {
            dialog.ShowModal(_desktop);
        }
    }

    public void CloseDialog()
    {
        if (_currentDialog != null)
        {
            _currentDialog.Close();
            _currentDialog = null;
        }
    }
}
