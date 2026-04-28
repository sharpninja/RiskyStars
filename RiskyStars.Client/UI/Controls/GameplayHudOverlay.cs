using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using RiskyStars.Shared;

namespace RiskyStars.Client;

public sealed class GameplayHudOverlay
{
    private int _screenWidth;
    private int _screenHeight;

    private readonly Panel _topBar;
    private readonly Label _turnLabel;
    private readonly Label _statusLabel;
    private readonly Label _hintLabel;
    private readonly Label _populationLabel;
    private readonly Label _metalLabel;
    private readonly Label _fuelLabel;

    private readonly Panel _legendPanel;
    private readonly Panel _aiActivityPanel;
    private readonly Label _aiActivityTitleLabel;
    private readonly Label[] _aiActivityDetailLabels;
    private readonly Panel _selectionPanel;
    private readonly Label _selectionTitleLabel;
    private readonly Label[] _selectionDetailLabels;
    private readonly Panel _helpPanel;

    public Widget TopBar => _topBar;
    public Widget LegendPanel => _legendPanel;
    public Widget AiActivityPanel => _aiActivityPanel;
    public Widget SelectionPanel => _selectionPanel;
    public Widget HelpPanel => _helpPanel;

    public Panel BuildAiActivityContent()
    {
        PreparePanelForSideBar(_aiActivityPanel);
        return _aiActivityPanel;
    }

    public Panel BuildSelectionContent()
    {
        PreparePanelForSideBar(_selectionPanel);
        return _selectionPanel;
    }

    public Panel BuildLegendContent()
    {
        var panel = ThemedUIFactory.CreateGameplayPanel();
        panel.HorizontalAlignment = HorizontalAlignment.Stretch;

        var stack = ThemedUIFactory.CreateCompactVerticalStack();
        stack.Spacing = ThemeManager.Spacing.Small;

        var heading = ThemedUIFactory.CreateHeadingLabel("Map Key");
        heading.TextColor = ThemeManager.Colors.TextWarning;
        stack.Widgets.Add(heading);
        stack.Widgets.Add(CreateLegendRow(CreateOrbitIcon(), "System orbit"));
        stack.Widgets.Add(CreateLegendRow(CreateBodyIcon(), "Stellar body"));
        stack.Widgets.Add(CreateLegendRow(CreateRegionIcon(), "Region marker"));
        stack.Widgets.Add(CreateLegendRow(CreateLaneMouthIcon(), "Lane mouth"));

        panel.Widgets.Add(stack);
        return panel;
    }

    private static void PreparePanelForSideBar(Panel panel)
    {
        panel.HorizontalAlignment = HorizontalAlignment.Stretch;
        panel.VerticalAlignment = VerticalAlignment.Top;
        panel.Margin = new Thickness(0);
    }

    public GameplayHudOverlay(int screenWidth, int screenHeight)
    {
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;

        _turnLabel = ThemedUIFactory.CreateSecondaryLabel(string.Empty);
        _turnLabel.TextColor = ThemeManager.Colors.TextPrimary;

        _statusLabel = ThemedUIFactory.CreateSmallLabel(string.Empty);
        _statusLabel.TextColor = ThemeManager.Colors.TextSecondary;
        _statusLabel.HorizontalAlignment = HorizontalAlignment.Right;

        _hintLabel = ThemedUIFactory.CreateSmallLabel(string.Empty);
        _hintLabel.TextColor = ThemeManager.Colors.TextSecondary;
        _hintLabel.HorizontalAlignment = HorizontalAlignment.Right;

        _populationLabel = ThemedUIFactory.CreateSmallLabel("POP --");
        _metalLabel = ThemedUIFactory.CreateSmallLabel("MET --");
        _fuelLabel = ThemedUIFactory.CreateSmallLabel("FUEL --");

        _topBar = BuildTopBar();
        _legendPanel = BuildLegendPanel();
        (_aiActivityPanel, _aiActivityTitleLabel, _aiActivityDetailLabels) = BuildAiActivityPanel();
        (_selectionPanel, _selectionTitleLabel, _selectionDetailLabels) = BuildSelectionPanel();
        _helpPanel = BuildHelpPanel();

        ResizeViewport(screenWidth, screenHeight);
    }

    public void ResizeViewport(int screenWidth, int screenHeight)
    {
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;

        _topBar.Width = screenWidth;
        _topBar.Margin = new Thickness(0);

        _legendPanel.Width = Math.Min(ThemeManager.ScalePixels(240), Math.Max(180, screenWidth / 4));
        _legendPanel.Margin = new Thickness(0, 0, ThemeManager.ScalePixels(12), 0);

        _aiActivityPanel.Width = Math.Min(ThemeManager.ScalePixels(360), Math.Max(260, screenWidth / 4));
        _aiActivityPanel.Margin = new Thickness(ThemeManager.ScalePixels(12), 0, 0, 0);

        _selectionPanel.Width = Math.Min(ThemeManager.ScalePixels(380), Math.Max(260, screenWidth / 3));
        _selectionPanel.Margin = new Thickness(0, 0, 0, ThemeManager.ScalePixels(16));

        _helpPanel.Width = Math.Min(ThemeManager.ScalePixels(520), Math.Max(320, screenWidth - ThemeManager.ScalePixels(80)));
        _helpPanel.Height = Math.Min(ThemeManager.ScalePixels(430), Math.Max(260, screenHeight - ThemeManager.ScalePixels(120)));
    }

    public int GetTopBarHeight()
    {
        return _topBar.Height ?? ThemeManager.ScalePixels(80);
    }

    public void Update(
        GameStateCache? gameStateCache,
        MapData? mapData,
        string? currentPlayerId,
        string? statusTitle,
        string? statusDetail,
        Color statusAccent,
        SelectionState? selection,
        bool isAiThinking,
        string? activeAiPlayerName,
        IReadOnlyList<GameLogEntry>? recentAiLogEntries,
        bool showHelp,
        bool dashboardVisible,
        bool aiVisible,
        bool debugVisible,
        bool uiScaleVisible,
        bool encyclopediaVisible,
        bool tutorialVisible)
    {
        UpdateTopBar(gameStateCache, currentPlayerId, statusTitle, statusDetail, statusAccent);
        UpdateAiActivity(isAiThinking, activeAiPlayerName, recentAiLogEntries);
        UpdateSelection(selection, gameStateCache, mapData);
        UpdatePanelHints(statusAccent, dashboardVisible, aiVisible, debugVisible, uiScaleVisible, encyclopediaVisible, tutorialVisible);
        _helpPanel.Visible = showHelp;
    }

    private Panel BuildTopBar()
    {
        var panel = ThemedUIFactory.CreateGameplayPanel();
        panel.HorizontalAlignment = HorizontalAlignment.Center;
        panel.VerticalAlignment = VerticalAlignment.Top;

        var layout = ThemedUIFactory.CreateGrid(ThemeManager.Spacing.Small, ThemeManager.Spacing.Medium);
        layout.ColumnsProportions.Add(new Proportion(ProportionType.Fill));
        layout.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
        layout.RowsProportions.Add(new Proportion(ProportionType.Auto));
        layout.RowsProportions.Add(new Proportion(ProportionType.Auto));

        _turnLabel.GridRow = 0;
        _turnLabel.GridColumn = 0;
        layout.Widgets.Add(_turnLabel);

        _statusLabel.GridRow = 0;
        _statusLabel.GridColumn = 1;
        layout.Widgets.Add(_statusLabel);

        var resourceRow = ThemedUIFactory.CreateHorizontalStack(ThemeManager.Spacing.Small);
        resourceRow.GridRow = 1;
        resourceRow.GridColumn = 0;
        resourceRow.Widgets.Add(CreateHudChip("POP", ThemeManager.Colors.PopulationColor, _populationLabel));
        resourceRow.Widgets.Add(CreateHudChip("MET", ThemeManager.Colors.MetalColor, _metalLabel));
        resourceRow.Widgets.Add(CreateHudChip("FUEL", ThemeManager.Colors.FuelColor, _fuelLabel));
        layout.Widgets.Add(resourceRow);

        _hintLabel.GridRow = 1;
        _hintLabel.GridColumn = 1;
        layout.Widgets.Add(_hintLabel);

        panel.Widgets.Add(layout);
        return panel;
    }

    private Panel BuildLegendPanel()
    {
        var panel = ThemedUIFactory.CreateGameplayPanel();
        panel.HorizontalAlignment = HorizontalAlignment.Right;
        panel.VerticalAlignment = VerticalAlignment.Top;

        var stack = ThemedUIFactory.CreateCompactVerticalStack();
        stack.Spacing = ThemeManager.Spacing.Small;

        var heading = ThemedUIFactory.CreateHeadingLabel("Map Key");
        heading.TextColor = ThemeManager.Colors.TextWarning;
        stack.Widgets.Add(heading);
        stack.Widgets.Add(CreateLegendRow(CreateOrbitIcon(), "System orbit"));
        stack.Widgets.Add(CreateLegendRow(CreateBodyIcon(), "Stellar body"));
        stack.Widgets.Add(CreateLegendRow(CreateRegionIcon(), "Region marker"));
        stack.Widgets.Add(CreateLegendRow(CreateLaneMouthIcon(), "Lane mouth"));

        panel.Widgets.Add(stack);
        return panel;
    }

    private static (Panel Panel, Label Title, Label[] Details) BuildSelectionPanel()
    {
        var panel = ThemedUIFactory.CreateGameplayPanel(ThemeManager.Colors.BorderFocus);
        panel.HorizontalAlignment = HorizontalAlignment.Center;
        panel.VerticalAlignment = VerticalAlignment.Bottom;
        panel.Visible = false;

        var stack = ThemedUIFactory.CreateCompactVerticalStack();
        stack.Spacing = ThemeManager.Spacing.Small;

        var title = ThemedUIFactory.CreateHeadingLabel(string.Empty);
        title.TextColor = ThemeManager.Colors.TextWarning;
        stack.Widgets.Add(title);

        var details = new Label[5];
        for (int i = 0; i < details.Length; i++)
        {
            var label = ThemedUIFactory.CreateSmallLabel(string.Empty);
            label.TextColor = ThemeManager.Colors.TextPrimary;
            label.Visible = false;
            stack.Widgets.Add(label);
            details[i] = label;
        }

        panel.Widgets.Add(stack);
        return (panel, title, details);
    }

    private static (Panel Panel, Label Title, Label[] Details) BuildAiActivityPanel()
    {
        var panel = ThemedUIFactory.CreateGameplayPanel(ThemeManager.Colors.BorderFocus);
        panel.HorizontalAlignment = HorizontalAlignment.Left;
        panel.VerticalAlignment = VerticalAlignment.Top;
        panel.Visible = false;

        var stack = ThemedUIFactory.CreateCompactVerticalStack();
        stack.Spacing = ThemeManager.Spacing.Small;

        var title = ThemedUIFactory.CreateHeadingLabel("AI Activity");
        title.TextColor = ThemeManager.Colors.TextAccent;
        stack.Widgets.Add(title);

        var details = new Label[4];
        for (int i = 0; i < details.Length; i++)
        {
            var label = ThemedUIFactory.CreateSmallLabel(string.Empty);
            label.TextColor = ThemeManager.Colors.TextPrimary;
            label.Wrap = true;
            label.Visible = false;
            stack.Widgets.Add(label);
            details[i] = label;
        }

        panel.Widgets.Add(stack);
        return (panel, title, details);
    }

    private Panel BuildHelpPanel()
    {
        var panel = ThemedUIFactory.CreateGameplayPanel(ThemeManager.Colors.BorderFocus);
        panel.HorizontalAlignment = HorizontalAlignment.Center;
        panel.VerticalAlignment = VerticalAlignment.Center;
        panel.Visible = false;

        var stack = ThemedUIFactory.CreateVerticalStack(ThemeManager.Spacing.Small);
        var heading = ThemedUIFactory.CreateHeadingLabel("Command Shortcuts");
        heading.TextColor = ThemeManager.Colors.TextAccent;
        stack.Widgets.Add(heading);

        foreach (var (key, description) in GetShortcutEntries())
        {
            var row = ThemedUIFactory.CreateGrid(0, ThemeManager.Spacing.Medium);
            row.ColumnsProportions.Add(new Proportion(ProportionType.Auto));
            row.ColumnsProportions.Add(new Proportion(ProportionType.Fill));

            var keyLabel = ThemedUIFactory.CreateSmallLabel(key);
            keyLabel.TextColor = ThemeManager.Colors.TextWarning;
            keyLabel.Width = ThemeManager.ScalePixels(120);
            keyLabel.GridColumn = 0;
            row.Widgets.Add(keyLabel);

            var descriptionLabel = ThemedUIFactory.CreateSmallLabel(description);
            descriptionLabel.TextColor = ThemeManager.Colors.TextPrimary;
            descriptionLabel.Wrap = true;
            descriptionLabel.GridColumn = 1;
            row.Widgets.Add(descriptionLabel);

            stack.Widgets.Add(row);
        }

        panel.Widgets.Add(stack);
        return panel;
    }

    private void UpdateTopBar(
        GameStateCache? gameStateCache,
        string? currentPlayerId,
        string? statusTitle,
        string? statusDetail,
        Color statusAccent)
    {
        if (gameStateCache == null)
        {
            _turnLabel.Text = "Turn 0 | Production Phase";
            _statusLabel.Text = BuildStatusText(statusTitle, statusDetail, null);
            _statusLabel.TextColor = statusAccent == default ? ThemeManager.Colors.TextPrimary : statusAccent;
            _populationLabel.Text = "POP --";
            _metalLabel.Text = "MET --";
            _fuelLabel.Text = "FUEL --";
            _topBar.Border = ThemeManager.CreateSolidBrush(statusAccent == default ? ThemeManager.Colors.BorderNormal : statusAccent);
            return;
        }

        GameStateCache cache = gameStateCache;
        _topBar.Border = ThemeManager.CreateSolidBrush(statusAccent == default ? ThemeManager.Colors.BorderNormal : statusAccent);

        int turnNumber = cache.GetTurnNumber();
        TurnPhase currentPhase = cache.GetCurrentPhase();
        string? activePlayerId = cache.GetCurrentPlayerId();
        string? eventMessage = cache.GetEventMessage();

        string phaseText = currentPhase switch
        {
            TurnPhase.Production => "Production Phase",
            TurnPhase.Purchase => "Purchase Phase",
            TurnPhase.Reinforcement => "Reinforcement Phase",
            TurnPhase.Movement => "Movement Phase",
            _ => "Unknown Phase"
        };

        string topText = $"Turn {turnNumber} | {phaseText}";
        if (!string.IsNullOrEmpty(activePlayerId))
        {
            var playerState = gameStateCache?.GetPlayerState(activePlayerId);
            if (playerState != null)
            {
                topText += $" | Active: {playerState.PlayerName}";
            }
        }

        _turnLabel.Text = topText;
        _statusLabel.Text = BuildStatusText(statusTitle, statusDetail, eventMessage);
        _statusLabel.TextColor = statusAccent == default ? ThemeManager.Colors.TextPrimary : statusAccent;

        if (string.IsNullOrWhiteSpace(currentPlayerId))
        {
            _populationLabel.Text = "POP --";
            _metalLabel.Text = "MET --";
            _fuelLabel.Text = "FUEL --";
            return;
        }

        string playerId = currentPlayerId;
        PlayerState? playerStateForHud = cache.GetPlayerState(playerId);
        if (playerStateForHud == null)
        {
            _populationLabel.Text = "POP --";
            _metalLabel.Text = "MET --";
            _fuelLabel.Text = "FUEL --";
            return;
        }

        _populationLabel.Text = BuildResourceText("POP", playerStateForHud.PopulationStockpile, cache.GetProductionRate(playerId, "population"));
        _metalLabel.Text = BuildResourceText("MET", playerStateForHud.MetalStockpile, cache.GetProductionRate(playerId, "metal"));
        _fuelLabel.Text = BuildResourceText("FUEL", playerStateForHud.FuelStockpile, cache.GetProductionRate(playerId, "fuel"));
    }

    private void UpdatePanelHints(
        Color statusAccent,
        bool dashboardVisible,
        bool aiVisible,
        bool debugVisible,
        bool uiScaleVisible,
        bool encyclopediaVisible,
        bool tutorialVisible)
    {
        _hintLabel.Text = $"Panels F1 Dbg:{OnOff(debugVisible)} F2 Cmd:{OnOff(dashboardVisible)} F3 AI:{OnOff(aiVisible)} F4 UI:{OnOff(uiScaleVisible)} F5 Ref:{OnOff(encyclopediaVisible)} F6 Tut:{OnOff(tutorialVisible)} H Help";
        _hintLabel.TextColor = statusAccent == default ? ThemeManager.Colors.TextSecondary : statusAccent;
    }

    private void UpdateSelection(SelectionState? selection, GameStateCache? gameStateCache, MapData? mapData)
    {
        if (selection == null || selection.Type == SelectionType.None)
        {
            _selectionPanel.Visible = false;
            return;
        }

        _selectionPanel.Visible = true;
        var lines = new List<string>(5);

        switch (selection.Type)
        {
            case SelectionType.Army when selection.SelectedArmy != null:
                var army = selection.SelectedArmy;
                _selectionTitleLabel.Text = "Selected Army";
                lines.Add($"Owner: {army.OwnerId}");
                lines.Add($"Units: {army.UnitCount}");
                lines.Add($"Location: {army.LocationId}");
                lines.Add($"Status: {(army.IsInCombat ? "In Combat" : army.HasMovedThisTurn ? "Moved" : "Ready")}");
                break;

            case SelectionType.Region when selection.SelectedRegion != null:
                var region = selection.SelectedRegion;
                _selectionTitleLabel.Text = "Selected Region";
                lines.Add($"Name: {region.Name}");
                var regionLocation = ResolveRegionLocation(mapData, region);
                if (regionLocation != null)
                {
                    lines.Add($"Body: {regionLocation.Value.BodyName}");
                    lines.Add($"Star: {regionLocation.Value.StarName}");
                }

                lines.Add(GetOwnershipText(gameStateCache?.GetRegionOwnership(region.Id)?.OwnerId));
                break;

            case SelectionType.HyperspaceLaneMouth when selection.SelectedHyperspaceLaneMouthId != null:
                _selectionTitleLabel.Text = "Selected Lane Mouth";
                lines.Add($"Id: {selection.SelectedHyperspaceLaneMouthId}");
                lines.Add(GetOwnershipText(gameStateCache?.GetHyperspaceLaneMouthOwnership(selection.SelectedHyperspaceLaneMouthId)?.OwnerId));
                break;

            case SelectionType.StellarBody when selection.SelectedStellarBody != null:
                var body = selection.SelectedStellarBody;
                _selectionTitleLabel.Text = "Selected Stellar Body";
                lines.Add($"Name: {body.Name}");
                lines.Add($"Type: {body.Type}");
                lines.Add($"Regions: {body.Regions.Count}");
                break;

            case SelectionType.StarSystem when selection.SelectedStarSystem != null:
                var system = selection.SelectedStarSystem;
                _selectionTitleLabel.Text = "Selected Star System";
                lines.Add($"Name: {system.Name}");
                lines.Add($"Type: {system.Type}");
                lines.Add($"Bodies: {system.StellarBodies.Count}");
                break;
        }

        for (int i = 0; i < _selectionDetailLabels.Length; i++)
        {
            bool visible = i < lines.Count;
            _selectionDetailLabels[i].Visible = visible;
            _selectionDetailLabels[i].Text = visible ? lines[i] : string.Empty;
        }
    }

    private void UpdateAiActivity(bool isAiThinking, string? activeAiPlayerName, IReadOnlyList<GameLogEntry>? recentAiLogEntries)
    {
        var lines = new List<(string Text, Color Color)>(_aiActivityDetailLabels.Length);

        if (isAiThinking)
        {
            string aiName = string.IsNullOrWhiteSpace(activeAiPlayerName) ? "AI commander" : activeAiPlayerName;
            lines.Add(($"{aiName} is resolving a turn.", ThemeManager.Colors.TextWarning));
        }

        if (recentAiLogEntries != null)
        {
            foreach (var entry in recentAiLogEntries)
            {
                if (lines.Count >= _aiActivityDetailLabels.Length)
                {
                    break;
                }

                lines.Add((entry.Message, entry.Color));
            }
        }

        _aiActivityPanel.Visible = lines.Count > 0;
        if (!_aiActivityPanel.Visible)
        {
            return;
        }

        _aiActivityTitleLabel.Text = isAiThinking ? "AI Activity" : "Recent AI Orders";
        _aiActivityTitleLabel.TextColor = isAiThinking ? ThemeManager.Colors.TextWarning : ThemeManager.Colors.TextAccent;

        for (int i = 0; i < _aiActivityDetailLabels.Length; i++)
        {
            bool visible = i < lines.Count;
            _aiActivityDetailLabels[i].Visible = visible;
            _aiActivityDetailLabels[i].Text = visible ? lines[i].Text : string.Empty;
            _aiActivityDetailLabels[i].TextColor = visible ? lines[i].Color : ThemeManager.Colors.TextPrimary;
        }
    }

    private static Panel CreateHudChip(string label, Color accent, Label valueLabel)
    {
        var panel = ThemedUIFactory.CreateGameplayPanel(accent);
        panel.Padding = new Thickness(ThemeManager.ScalePixels(10), ThemeManager.ScalePixels(6));

        valueLabel.TextColor = ThemeManager.Colors.TextPrimary;
        valueLabel.Text = label;
        panel.Widgets.Add(valueLabel);
        return panel;
    }

    private static Widget CreateLegendRow(Widget icon, string text)
    {
        var row = ThemedUIFactory.CreateHorizontalStack(ThemeManager.Spacing.Small);
        row.Widgets.Add(icon);

        var label = ThemedUIFactory.CreateSmallLabel(text);
        label.TextColor = ThemeManager.Colors.TextPrimary;
        row.Widgets.Add(label);
        return row;
    }

    private static Panel CreateOrbitIcon()
    {
        return new Panel
        {
            Width = ThemeManager.ScalePixels(18),
            Height = ThemeManager.ScalePixels(2),
            Background = ThemeManager.CreateSolidBrush(new Color(110, 140, 110)),
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    private static Panel CreateBodyIcon()
    {
        return new Panel
        {
            Width = ThemeManager.ScalePixels(10),
            Height = ThemeManager.ScalePixels(10),
            Background = ThemeManager.CreateSolidBrush(new Color(110, 150, 200)),
            Border = ThemeManager.CreateSolidBrush(ThemeManager.Colors.BorderNormal),
            BorderThickness = new Thickness(ThemeManager.BorderThickness.Thin),
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    private static Panel CreateRegionIcon()
    {
        return new Panel
        {
            Width = ThemeManager.ScalePixels(8),
            Height = ThemeManager.ScalePixels(8),
            Background = ThemeManager.CreateSolidBrush(Color.White),
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    private static Panel CreateLaneMouthIcon()
    {
        return new Panel
        {
            Width = ThemeManager.ScalePixels(12),
            Height = ThemeManager.ScalePixels(12),
            Background = ThemeManager.CreateSolidBrush(Color.Transparent),
            Border = ThemeManager.CreateSolidBrush(new Color(180, 220, 180)),
            BorderThickness = new Thickness(ThemeManager.BorderThickness.Thin),
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    private static string BuildStatusText(string? statusTitle, string? statusDetail, string? eventMessage)
    {
        if (!string.IsNullOrWhiteSpace(statusTitle) && !string.IsNullOrWhiteSpace(statusDetail))
        {
            return $"{statusTitle}: {statusDetail}";
        }

        if (!string.IsNullOrWhiteSpace(statusTitle))
        {
            return statusTitle;
        }

        return eventMessage ?? string.Empty;
    }

    private static string BuildResourceText(string code, int amount, int delta)
    {
        return delta switch
        {
            > 0 => $"{code} {amount} (+{delta})",
            < 0 => $"{code} {amount} ({delta})",
            _ => $"{code} {amount}"
        };
    }

    private static string GetOwnershipText(string? ownerId)
    {
        return string.IsNullOrWhiteSpace(ownerId) ? "Owner: Unowned" : $"Owner: {ownerId}";
    }

    private static RegionLocationNames? ResolveRegionLocation(MapData? mapData, RegionData region)
    {
        if (mapData == null)
        {
            return null;
        }

        foreach (var system in mapData.StarSystems)
        {
            foreach (var body in system.StellarBodies)
            {
                bool regionBelongsToBody = string.Equals(body.Id, region.StellarBodyId, StringComparison.OrdinalIgnoreCase) ||
                                           body.Regions.Any(candidate => string.Equals(candidate.Id, region.Id, StringComparison.OrdinalIgnoreCase));
                if (!regionBelongsToBody ||
                    string.IsNullOrWhiteSpace(body.Name) ||
                    string.IsNullOrWhiteSpace(system.Name))
                {
                    continue;
                }

                return new RegionLocationNames(body.Name, system.Name);
            }
        }

        return null;
    }

    private readonly record struct RegionLocationNames(string BodyName, string StarName);

    private static string OnOff(bool value) => value ? "On" : "Off";

    private static (string Key, string Description)[] GetShortcutEntries()
    {
        return
        [
            ("Left Click", "Select unit or location"),
            ("Right Click", "Open context menu"),
            ("RMB Drag", "Pan map"),
            ("Tab", "Cycle armies"),
            ("C", "Center on selection"),
            ("Space", "Advance phase"),
            ("P", "Produce resources"),
            ("B / 1", "Purchase 1 army"),
            ("5", "Purchase 5 armies"),
            ("0", "Purchase 10 armies"),
            ("R", "Reinforce location"),
            ("Esc", "Clear selection"),
            ("F1", "Toggle debug info"),
            ("F2", "Toggle dashboard"),
            ("F3", "Toggle AI panel"),
            ("F4", "Toggle UI scale panel"),
            ("F5", "Toggle encyclopedia"),
            ("F6", "Toggle guided tutorial"),
            ("H", "Toggle help")
        ];
    }
}
