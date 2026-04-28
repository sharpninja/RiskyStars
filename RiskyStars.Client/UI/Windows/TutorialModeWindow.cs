using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using RiskyStars.Shared;
using MyraButton = Myra.Graphics2D.UI.Button;

namespace RiskyStars.Client;

public enum TutorialStepCompletion
{
    Manual,
    WorldSynced,
    OwnTurn,
    AnySelection,
    HelpOpen,
    DashboardOpen,
    PurchasePhase,
    ArmyPurchased,
    ReinforcementPhase,
    OwnedReinforcementTargetSelected,
    MovementPhase,
    OwnArmySelected,
    ArmyMoved,
    ReferenceOpen
}

public sealed record TutorialModeStep(
    string Id,
    string Title,
    string Briefing,
    string Objective,
    IReadOnlyList<string> Actions,
    TutorialStepCompletion Completion);

public sealed record TutorialModeSnapshot(
    GameStateCache? GameStateCache,
    string? CurrentPlayerId,
    SelectionState? Selection,
    bool HelpVisible,
    bool DashboardVisible,
    bool EncyclopediaVisible,
    bool TutorialReferenceVisible,
    bool ContextMenuOpen,
    bool CombatActive);

public sealed class TutorialModeWindow : DockableWindow
{
    private static readonly IReadOnlyList<TutorialModeStep> Steps =
    [
        new TutorialModeStep(
            "sync",
            "Boot the command deck",
            "Tutorial mode has started a small single-player scenario against one easy AI commander.",
            "Wait for the first world snapshot to arrive.",
            [
                "Watch the status banner while the embedded server starts.",
                "Do not issue orders until the top bar shows the live turn."
            ],
            TutorialStepCompletion.WorldSynced),
        new TutorialModeStep(
            "turn",
            "Read the turn banner",
            "Every order starts with the active player and phase in the top bar.",
            "Confirm that it is your turn.",
            [
                "Find the turn number and current phase.",
                "Check the resource chips for population, metal, and fuel."
            ],
            TutorialStepCompletion.OwnTurn),
        new TutorialModeStep(
            "select",
            "Select something on the map",
            "Selection drives the right-side detail panel and most context actions.",
            "Left-click a region, lane mouth, body, system, or your army.",
            [
                "Use the mouse wheel if you need to zoom.",
                "Right-drag the map if the target is off screen."
            ],
            TutorialStepCompletion.AnySelection),
        new TutorialModeStep(
            "help",
            "Open the shortcut sheet",
            "The compact help overlay is the fastest way to recover controls.",
            "Press H to open the command shortcut sheet.",
            [
                "Press H again when you want to hide it.",
                "The top bar always reports whether help is open."
            ],
            TutorialStepCompletion.HelpOpen),
        new TutorialModeStep(
            "production",
            "Resolve production",
            "Production adds resources before you buy and reinforce.",
            "Press P to produce resources, or press Space to advance out of production.",
            [
                "The top bar resource chips update after the server accepts the order.",
                "If production is already complete, this step will complete when the phase changes."
            ],
            TutorialStepCompletion.PurchasePhase),
        new TutorialModeStep(
            "dashboard",
            "Open the command dashboard",
            "The dashboard is where resource totals and army purchase buttons live.",
            "Press F2 to open the player dashboard.",
            [
                "Buy controls only enable during the purchase phase.",
                "Use F2 again if you want to close the panel later."
            ],
            TutorialStepCompletion.DashboardOpen),
        new TutorialModeStep(
            "purchase",
            "Buy a starter army",
            "Purchased armies become the reserve you place during reinforcement.",
            "Buy one army from the dashboard or press B.",
            [
                "One army costs 1 population, 3 metal, and 1 fuel.",
                "If you cannot afford a buy, press Next to continue the tutorial path."
            ],
            TutorialStepCompletion.ArmyPurchased),
        new TutorialModeStep(
            "reinforcement-phase",
            "Advance to reinforcement",
            "Reinforcement places purchased strength onto owned map positions.",
            "Press Space until the top bar shows Reinforcement Phase.",
            [
                "The tutorial keeps running if an AI turn briefly takes over.",
                "Wait for your turn again before placing reinforcements."
            ],
            TutorialStepCompletion.ReinforcementPhase),
        new TutorialModeStep(
            "reinforcement-target",
            "Pick a reinforcement target",
            "Owned regions and lane mouths are valid reinforcement anchors.",
            "Select one of your owned regions or lane mouths.",
            [
                "The selection panel reports ownership.",
                "Lane mouths are strong targets when they control travel routes."
            ],
            TutorialStepCompletion.OwnedReinforcementTargetSelected),
        new TutorialModeStep(
            "movement-phase",
            "Advance to movement",
            "Movement is where you reposition armies and pressure borders.",
            "Press Space until the top bar shows Movement Phase.",
            [
                "Finish any reinforcement you want first.",
                "The AI may act between your phases depending on turn order."
            ],
            TutorialStepCompletion.MovementPhase),
        new TutorialModeStep(
            "army",
            "Select a mobile army",
            "Armies are the units that move, split, merge, and fight.",
            "Select one of your own armies.",
            [
                "Tab cycles armies.",
                "Press C after selecting an army to center the camera on it."
            ],
            TutorialStepCompletion.OwnArmySelected),
        new TutorialModeStep(
            "movement",
            "Issue or inspect movement",
            "Movement orders are submitted by right-clicking a destination or using the context menu.",
            "Move an army or open a right-click menu to inspect movement actions.",
            [
                "Right-click without dragging to open context actions.",
                "A moved army is marked spent until the next turn."
            ],
            TutorialStepCompletion.ArmyMoved),
        new TutorialModeStep(
            "reference",
            "Open the reference layer",
            "Rules and context help stay available after tutorial mode ends.",
            "Open the encyclopedia with F5 or the full tutorial with F6.",
            [
                "The Full Guide button below opens the existing lesson browser.",
                "Use these panels any time during normal play."
            ],
            TutorialStepCompletion.ReferenceOpen),
        new TutorialModeStep(
            "complete",
            "Continue into free play",
            "You have used the main loop: read phase, select, produce, buy, reinforce, move, and recover help.",
            "End tutorial mode when you are ready to keep playing normally.",
            [
                "The current game continues after the tutorial panel closes.",
                "F5 and F6 remain available for reference."
            ],
            TutorialStepCompletion.Manual)
    ];

    private readonly HashSet<string> _completedStepIds = new();
    private readonly Label _progressLabel;
    private readonly Label _titleLabel;
    private readonly Label _briefingLabel;
    private readonly Label _objectiveLabel;
    private readonly Label _statusLabel;
    private readonly VerticalStackPanel _actionsStack;
    private readonly VerticalStackPanel _progressStack;
    private readonly MyraButton _backButton;
    private readonly MyraButton _nextButton;

    private TutorialModeSnapshot? _lastSnapshot;
    private int _currentStepIndex;
    private int _baselineOwnArmyCount = -1;
    private int _baselineMovedArmyCount = -1;

    public event Action? EndRequested;
    public event Action? ReferenceRequested;

    public TutorialModeWindow(WindowPreferences preferences, int screenWidth, int screenHeight)
        : base("tutorial_mode", "Tutorial Mode", preferences, screenWidth, screenHeight, 520, 640)
    {
        _progressLabel = ThemedUIFactory.CreateSmallLabel(string.Empty);
        _titleLabel = ThemedUIFactory.CreateHeadingLabel(string.Empty);
        _briefingLabel = ThemedUIFactory.CreateSecondaryLabel(string.Empty);
        _objectiveLabel = ThemedUIFactory.CreateSmallLabel(string.Empty);
        _statusLabel = ThemedUIFactory.CreateSmallLabel(string.Empty);
        _actionsStack = ThemedUIFactory.CreateCompactVerticalStack();
        _progressStack = ThemedUIFactory.CreateCompactVerticalStack();
        _backButton = ThemedUIFactory.CreateButton("Back", ThemeManager.ScalePixels(110), ThemeManager.Sizes.ButtonSmallHeight, ThemeManager.ButtonTheme.Default);
        _nextButton = ThemedUIFactory.CreateButton("Next", ThemeManager.ScalePixels(130), ThemeManager.Sizes.ButtonSmallHeight, ThemeManager.ButtonTheme.Primary);

        BuildContent();
        DockTo(DockPosition.TopLeft);
        Show();
        RefreshView();
    }

    private void BuildContent()
    {
        var root = ThemedUIFactory.CreateVerticalStack(ThemeManager.Spacing.Medium);

        var headerPanel = ThemedUIFactory.CreateGameplayPanel(ThemeManager.Colors.TextAccent);
        var headerStack = ThemedUIFactory.CreateCompactVerticalStack();
        headerStack.Spacing = ThemeManager.Spacing.Small;

        _progressLabel.TextColor = ThemeManager.Colors.TextWarning;
        headerStack.Widgets.Add(_progressLabel);

        _titleLabel.TextColor = ThemeManager.Colors.TextAccent;
        _titleLabel.Wrap = true;
        headerStack.Widgets.Add(_titleLabel);

        _briefingLabel.Wrap = true;
        _briefingLabel.TextColor = ThemeManager.Colors.TextPrimary;
        headerStack.Widgets.Add(_briefingLabel);

        _objectiveLabel.Wrap = true;
        _objectiveLabel.TextColor = ThemeManager.Colors.TextWarning;
        headerStack.Widgets.Add(_objectiveLabel);

        _statusLabel.Wrap = true;
        headerStack.Widgets.Add(_statusLabel);

        headerPanel.Widgets.Add(headerStack);
        root.Widgets.Add(headerPanel);

        var actionsPanel = ThemedUIFactory.CreateGameplayPanel();
        var actionHeading = ThemedUIFactory.CreateSmallLabel("Step actions");
        actionHeading.TextColor = ThemeManager.Colors.TextWarning;
        _actionsStack.Widgets.Add(actionHeading);
        actionsPanel.Widgets.Add(_actionsStack);
        root.Widgets.Add(actionsPanel);

        var progressPanel = ThemedUIFactory.CreateGameplayPanel();
        var progressHeading = ThemedUIFactory.CreateSmallLabel("Tutorial path");
        progressHeading.TextColor = ThemeManager.Colors.TextWarning;
        _progressStack.Widgets.Add(progressHeading);
        progressPanel.Widgets.Add(ThemedUIFactory.CreateAutoScrollViewer(_progressStack, ThemeManager.ScalePixels(170)));
        root.Widgets.Add(progressPanel);

        var buttonRow = ThemedUIFactory.CreateHorizontalStack(ThemeManager.Spacing.Small);
        buttonRow.HorizontalAlignment = HorizontalAlignment.Center;

        _backButton.Click += (_, _) => MovePrevious();
        buttonRow.Widgets.Add(_backButton);

        _nextButton.Click += (_, _) => MoveNext();
        buttonRow.Widgets.Add(_nextButton);

        var referenceButton = ThemedUIFactory.CreateButton("Full Guide", ThemeManager.ScalePixels(130), ThemeManager.Sizes.ButtonSmallHeight, ThemeManager.ButtonTheme.Default);
        referenceButton.Click += (_, _) => ReferenceRequested?.Invoke();
        buttonRow.Widgets.Add(referenceButton);

        var endButton = ThemedUIFactory.CreateButton("End", ThemeManager.ScalePixels(100), ThemeManager.Sizes.ButtonSmallHeight, ThemeManager.ButtonTheme.Danger);
        endButton.Click += (_, _) => EndRequested?.Invoke();
        buttonRow.Widgets.Add(endButton);

        root.Widgets.Add(buttonRow);
        _window.Content = root;
    }

    public void UpdateContent(TutorialModeSnapshot snapshot)
    {
        _lastSnapshot = snapshot;
        EnsureBaseline(snapshot);

        var current = Steps[_currentStepIndex];
        if (IsStepComplete(current, snapshot))
        {
            _completedStepIds.Add(current.Id);
        }

        RefreshView();
    }

    private void MovePrevious()
    {
        if (_currentStepIndex <= 0)
        {
            return;
        }

        _currentStepIndex--;
        CaptureBaseline(_lastSnapshot);
        RefreshView();
    }

    private void MoveNext()
    {
        if (_currentStepIndex >= Steps.Count - 1)
        {
            EndRequested?.Invoke();
            return;
        }

        _completedStepIds.Add(Steps[_currentStepIndex].Id);
        _currentStepIndex++;
        CaptureBaseline(_lastSnapshot);
        RefreshView();
    }

    private void CaptureBaseline(TutorialModeSnapshot? snapshot)
    {
        _baselineOwnArmyCount = GetOwnArmies(snapshot).Count;
        _baselineMovedArmyCount = GetOwnArmies(snapshot).Count(army => army.HasMovedThisTurn);
    }

    private void EnsureBaseline(TutorialModeSnapshot snapshot)
    {
        if (_baselineOwnArmyCount < 0 || _baselineMovedArmyCount < 0)
        {
            CaptureBaseline(snapshot);
        }
    }

    private void RefreshView()
    {
        var current = Steps[_currentStepIndex];
        bool currentComplete = _lastSnapshot != null && IsStepComplete(current, _lastSnapshot);

        _progressLabel.Text = $"Step {_currentStepIndex + 1} of {Steps.Count}";
        _titleLabel.Text = current.Title;
        _briefingLabel.Text = current.Briefing;
        _objectiveLabel.Text = $"Objective: {current.Objective}";
        _statusLabel.Text = current.Completion == TutorialStepCompletion.Manual
            ? "Status: Manual step. Press Next when ready."
            : currentComplete
                ? "Status: Objective complete. Press Next."
                : "Status: Waiting for the objective.";
        _statusLabel.TextColor = currentComplete || current.Completion == TutorialStepCompletion.Manual
            ? ThemeManager.Colors.TextSuccess
            : ThemeManager.Colors.TextSecondary;

        _actionsStack.Widgets.Clear();
        var actionHeading = ThemedUIFactory.CreateSmallLabel("Step actions");
        actionHeading.TextColor = ThemeManager.Colors.TextWarning;
        _actionsStack.Widgets.Add(actionHeading);
        foreach (var action in current.Actions)
        {
            _actionsStack.Widgets.Add(CreateBulletLabel(action));
        }

        _progressStack.Widgets.Clear();
        var progressHeading = ThemedUIFactory.CreateSmallLabel("Tutorial path");
        progressHeading.TextColor = ThemeManager.Colors.TextWarning;
        _progressStack.Widgets.Add(progressHeading);
        for (int i = 0; i < Steps.Count; i++)
        {
            _progressStack.Widgets.Add(CreateProgressRow(i));
        }

        _backButton.Enabled = _currentStepIndex > 0;
        SetButtonText(_nextButton, _currentStepIndex >= Steps.Count - 1 ? "Finish" : currentComplete || current.Completion == TutorialStepCompletion.Manual ? "Next" : "Skip");
    }

    private Widget CreateProgressRow(int index)
    {
        var step = Steps[index];
        var row = ThemedUIFactory.CreateGrid(0, ThemeManager.Spacing.Small);
        row.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        row.ColumnsProportions.Add(new Proportion(ProportionType.Fill));

        string marker = index == _currentStepIndex
            ? "NOW"
            : index < _currentStepIndex || _completedStepIds.Contains(step.Id)
                ? "DONE"
                : "NEXT";

        var markerLabel = ThemedUIFactory.CreateSmallLabel(marker);
        markerLabel.Width = ThemeManager.ScalePixels(54);
        markerLabel.TextColor = index == _currentStepIndex
            ? ThemeManager.Colors.TextAccent
            : index < _currentStepIndex || _completedStepIds.Contains(step.Id)
                ? ThemeManager.Colors.TextSuccess
                : ThemeManager.Colors.TextSecondary;
        markerLabel.GridColumn = 0;
        row.Widgets.Add(markerLabel);

        var titleLabel = ThemedUIFactory.CreateSmallLabel(step.Title);
        titleLabel.Wrap = true;
        titleLabel.TextColor = index == _currentStepIndex ? ThemeManager.Colors.TextPrimary : ThemeManager.Colors.TextSecondary;
        titleLabel.GridColumn = 1;
        row.Widgets.Add(titleLabel);
        return row;
    }

    private bool IsStepComplete(TutorialModeStep step, TutorialModeSnapshot snapshot)
    {
        var cache = snapshot.GameStateCache;
        string? playerId = snapshot.CurrentPlayerId;

        return step.Completion switch
        {
            TutorialStepCompletion.Manual => true,
            TutorialStepCompletion.WorldSynced => cache?.GetLastUpdateTimestamp() > 0,
            TutorialStepCompletion.OwnTurn => !string.IsNullOrWhiteSpace(playerId) && cache?.GetCurrentPlayerId() == playerId,
            TutorialStepCompletion.AnySelection => snapshot.Selection != null && snapshot.Selection.Type != SelectionType.None,
            TutorialStepCompletion.HelpOpen => snapshot.HelpVisible,
            TutorialStepCompletion.DashboardOpen => snapshot.DashboardVisible,
            TutorialStepCompletion.PurchasePhase => cache?.GetCurrentPhase() == TurnPhase.Purchase,
            TutorialStepCompletion.ArmyPurchased => HasPurchasedArmy(snapshot),
            TutorialStepCompletion.ReinforcementPhase => cache?.GetCurrentPhase() == TurnPhase.Reinforcement,
            TutorialStepCompletion.OwnedReinforcementTargetSelected => HasOwnedReinforcementTarget(snapshot),
            TutorialStepCompletion.MovementPhase => cache?.GetCurrentPhase() == TurnPhase.Movement,
            TutorialStepCompletion.OwnArmySelected => !string.IsNullOrWhiteSpace(playerId) && snapshot.Selection?.SelectedArmy?.OwnerId == playerId,
            TutorialStepCompletion.ArmyMoved => HasMovedArmy(snapshot) || snapshot.ContextMenuOpen,
            TutorialStepCompletion.ReferenceOpen => snapshot.EncyclopediaVisible || snapshot.TutorialReferenceVisible,
            _ => false
        };
    }

    private bool HasPurchasedArmy(TutorialModeSnapshot snapshot)
    {
        var armies = GetOwnArmies(snapshot);
        if (_baselineOwnArmyCount >= 0 && armies.Count > _baselineOwnArmyCount)
        {
            return true;
        }

        var phase = snapshot.GameStateCache?.GetCurrentPhase();
        return phase == TurnPhase.Reinforcement || phase == TurnPhase.Movement;
    }

    private bool HasMovedArmy(TutorialModeSnapshot snapshot)
    {
        var movedCount = GetOwnArmies(snapshot).Count(army => army.HasMovedThisTurn);
        return _baselineMovedArmyCount >= 0 && movedCount > _baselineMovedArmyCount;
    }

    private static bool HasOwnedReinforcementTarget(TutorialModeSnapshot snapshot)
    {
        if (snapshot.GameStateCache == null || string.IsNullOrWhiteSpace(snapshot.CurrentPlayerId) || snapshot.Selection == null)
        {
            return false;
        }

        string playerId = snapshot.CurrentPlayerId;
        return snapshot.Selection.Type switch
        {
            SelectionType.Region when snapshot.Selection.SelectedRegion != null =>
                snapshot.GameStateCache.GetRegionOwnership(snapshot.Selection.SelectedRegion.Id)?.OwnerId == playerId,
            SelectionType.HyperspaceLaneMouth when !string.IsNullOrWhiteSpace(snapshot.Selection.SelectedHyperspaceLaneMouthId) =>
                snapshot.GameStateCache.GetHyperspaceLaneMouthOwnership(snapshot.Selection.SelectedHyperspaceLaneMouthId)?.OwnerId == playerId,
            _ => false
        };
    }

    private static IReadOnlyList<ArmyState> GetOwnArmies(TutorialModeSnapshot? snapshot)
    {
        if (snapshot?.GameStateCache == null || string.IsNullOrWhiteSpace(snapshot.CurrentPlayerId))
        {
            return [];
        }

        return snapshot.GameStateCache.GetArmiesOwnedByPlayer(snapshot.CurrentPlayerId);
    }

    private static Label CreateBulletLabel(string text)
    {
        var label = ThemedUIFactory.CreateSmallLabel($"- {text}");
        label.Wrap = true;
        label.TextColor = ThemeManager.Colors.TextPrimary;
        return label;
    }

    private static void SetButtonText(MyraButton button, string text)
    {
        if (button.Content is Label label)
        {
            label.Text = text;
        }
    }
}
