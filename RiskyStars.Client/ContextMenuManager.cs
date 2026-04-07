using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Myra.Graphics2D.UI;
using RiskyStars.Shared;
using static RiskyStars.Client.ThemeManager;

namespace RiskyStars.Client;

public class ContextMenuManager
{
    private readonly GrpcGameClient _gameClient;
    private readonly GameStateCache _gameStateCache;
    private readonly MapData _mapData;
    private readonly Camera2D _camera;
    private readonly Desktop _desktop;
    
    private VerticalStackPanel? _activeMenu;
    private string? _currentPlayerId;
    
    public bool IsMenuOpen => _activeMenu != null && _desktop.Widgets.Contains(_activeMenu);
    
    public ContextMenuManager(GrpcGameClient gameClient, GameStateCache gameStateCache, MapData mapData, Camera2D camera, Desktop desktop)
    {
        _gameClient = gameClient;
        _gameStateCache = gameStateCache;
        _mapData = mapData;
        _camera = camera;
        _desktop = desktop;
    }
    
    public void SetCurrentPlayer(string? playerId)
    {
        _currentPlayerId = playerId;
    }
    
    public void OpenContextMenu(Vector2 screenPosition, Vector2 worldPosition, SelectionState selection)
    {
        CloseContextMenu();
        
        if (selection.Type == SelectionType.Army && selection.SelectedArmy != null)
        {
            OpenArmyContextMenu(selection.SelectedArmy, screenPosition);
        }
        else if (selection.Type == SelectionType.Region && selection.SelectedRegion != null)
        {
            OpenRegionContextMenu(selection.SelectedRegion, screenPosition);
        }
        else if (selection.Type == SelectionType.StellarBody && selection.SelectedStellarBody != null)
        {
            OpenStellarBodyContextMenu(selection.SelectedStellarBody, screenPosition);
        }
        else if (selection.Type == SelectionType.HyperspaceLaneMouth && selection.SelectedHyperspaceLaneMouthId != null)
        {
            OpenHyperspaceLaneMouthContextMenu(selection.SelectedHyperspaceLaneMouthId, screenPosition);
        }
        else
        {
            var clickedArmy = FindArmyAtPosition(worldPosition);
            if (clickedArmy != null)
            {
                OpenArmyContextMenu(clickedArmy, screenPosition);
                return;
            }
            
            var clickedRegion = FindRegionAtPosition(worldPosition);
            if (clickedRegion != null)
            {
                OpenRegionContextMenu(clickedRegion, screenPosition);
                return;
            }
            
            var clickedBody = FindStellarBodyAtPosition(worldPosition);
            if (clickedBody != null)
            {
                OpenStellarBodyContextMenu(clickedBody, screenPosition);
                return;
            }
            
            var clickedMouth = FindHyperspaceLaneMouthAtPosition(worldPosition);
            if (clickedMouth != null)
            {
                OpenHyperspaceLaneMouthContextMenu(clickedMouth.Value.Item1, screenPosition);
                return;
            }
        }
    }
    
    public void CloseContextMenu()
    {
        if (_activeMenu != null)
        {
            _desktop.Widgets.Remove(_activeMenu);
            _activeMenu = null;
        }
    }
    
    private void OpenArmyContextMenu(ArmyState army, Vector2 screenPosition)
    {
        var items = new List<Widget>();
        
        var armyShortId = army.ArmyId.Length > 8 ? army.ArmyId.Substring(0, 8) + "..." : army.ArmyId;
        items.Add(CreateMenuHeader($"Army {armyShortId}"));
        items.Add(CreateMenuSeparator());
        
        if (army.OwnerId == _currentPlayerId)
        {
            items.Add(CreateMenuItem("View Info", () =>
            {
                CloseContextMenu();
                ShowArmyInfoDialog(army);
            }));
            
            items.Add(CreateMenuItem("Move Army", () =>
            {
                CloseContextMenu();
            }));
            
            if (army.UnitCount > 1)
            {
                items.Add(CreateMenuItem("Split Army", () =>
                {
                    CloseContextMenu();
                    ShowSplitArmyDialog(army);
                }));
            }
            
            var armiesAtLocation = _gameStateCache.GetArmiesAtLocation(army.LocationId, army.LocationType);
            var otherPlayerArmies = armiesAtLocation.Where(a => a.OwnerId == _currentPlayerId && a.ArmyId != army.ArmyId).ToList();
            
            if (otherPlayerArmies.Count > 0)
            {
                items.Add(CreateMenuItem("Merge with Army...", () =>
                {
                    CloseContextMenu();
                    ShowMergeArmiesDialog(army, otherPlayerArmies);
                }));
            }
            
            items.Add(CreateMenuSeparator());
            items.Add(CreateMenuItem("Assign Hero...", () =>
            {
                CloseContextMenu();
                ShowAssignHeroDialog(army);
            }));
        }
        else
        {
            items.Add(CreateMenuItem("View Info", () =>
            {
                CloseContextMenu();
                ShowArmyInfoDialog(army);
            }));
            
            items.Add(CreateMenuSeparator());
            items.Add(CreateMenuItem("Diplomacy...", () =>
            {
                CloseContextMenu();
                ShowDiplomacyDialog(army.OwnerId);
            }));
        }
        
        ShowMenu(items, screenPosition);
    }
    
    private void OpenRegionContextMenu(RegionData region, Vector2 screenPosition)
    {
        var items = new List<Widget>();
        var ownership = _gameStateCache.GetRegionOwnership(region.Id);
        var armies = _gameStateCache.GetArmiesAtLocation(region.Id, LocationType.Region);
        
        items.Add(CreateMenuHeader($"{region.Name}"));
        items.Add(CreateMenuSeparator());
        
        items.Add(CreateMenuItem("View Info", () =>
        {
            CloseContextMenu();
            ShowRegionInfoDialog(region);
        }));
        
        if (ownership != null && ownership.OwnerId == _currentPlayerId)
        {
            items.Add(CreateMenuItem("Reinforce Location", () =>
            {
                CloseContextMenu();
                ShowReinforceDialog(region.Id, LocationType.Region, region.Name);
            }));
        }
        else if (ownership != null && !string.IsNullOrEmpty(ownership.OwnerId) && ownership.OwnerId != _currentPlayerId)
        {
            items.Add(CreateMenuSeparator());
            items.Add(CreateMenuItem("Diplomacy...", () =>
            {
                CloseContextMenu();
                ShowDiplomacyDialog(ownership.OwnerId);
            }));
        }
        
        if (armies.Count > 0 && armies.Any(a => a.OwnerId == _currentPlayerId))
        {
            var playerArmies = armies.Where(a => a.OwnerId == _currentPlayerId).ToList();
            if (playerArmies.Count > 1)
            {
                items.Add(CreateMenuItem("Merge All Armies", () =>
                {
                    CloseContextMenu();
                    ShowMergeAllArmiesDialog(playerArmies);
                }));
            }
        }
        
        ShowMenu(items, screenPosition);
    }
    
    private void OpenStellarBodyContextMenu(StellarBodyData body, Vector2 screenPosition)
    {
        var items = new List<Widget>();
        
        items.Add(CreateMenuHeader($"{body.Name}"));
        items.Add(CreateMenuSeparator());
        
        items.Add(CreateMenuItem("View Info", () =>
        {
            CloseContextMenu();
            ShowStellarBodyInfoDialog(body);
        }));
        
        var ownedRegions = body.Regions.Where(r => 
        {
            var ownership = _gameStateCache.GetRegionOwnership(r.Id);
            return ownership != null && ownership.OwnerId == _currentPlayerId;
        }).ToList();
        
        if (ownedRegions.Count > 0)
        {
            items.Add(CreateMenuItem("Upgrade Stellar Body...", () =>
            {
                CloseContextMenu();
                ShowUpgradeStellarBodyDialog(body);
            }));
        }
        
        ShowMenu(items, screenPosition);
    }
    
    private void OpenHyperspaceLaneMouthContextMenu(string mouthId, Vector2 screenPosition)
    {
        var items = new List<Widget>();
        var ownership = _gameStateCache.GetHyperspaceLaneMouthOwnership(mouthId);
        var lane = _mapData.HyperspaceLanes.FirstOrDefault(l => l.MouthAId == mouthId || l.MouthBId == mouthId);
        
        var laneName = lane != null ? lane.Name : "Hyperspace Lane";
        items.Add(CreateMenuHeader(laneName));
        items.Add(CreateMenuSeparator());
        
        items.Add(CreateMenuItem("View Info", () =>
        {
            CloseContextMenu();
            if (lane != null)
            {
                ShowHyperspaceLaneInfoDialog(lane, mouthId);
            }
        }));
        
        if (ownership != null && ownership.OwnerId == _currentPlayerId)
        {
            items.Add(CreateMenuItem("Reinforce Portal", () =>
            {
                CloseContextMenu();
                var name = lane != null ? $"{lane.Name} Portal" : "Portal";
                ShowReinforceDialog(mouthId, LocationType.HyperspaceLaneMouth, name);
            }));
        }
        
        var armies = _gameStateCache.GetArmiesAtLocation(mouthId, LocationType.HyperspaceLaneMouth);
        if (armies.Count > 0 && armies.Any(a => a.OwnerId == _currentPlayerId))
        {
            var playerArmies = armies.Where(a => a.OwnerId == _currentPlayerId).ToList();
            if (playerArmies.Count > 1)
            {
                items.Add(CreateMenuItem("Merge All Armies", () =>
                {
                    CloseContextMenu();
                    ShowMergeAllArmiesDialog(playerArmies);
                }));
            }
        }
        
        ShowMenu(items, screenPosition);
    }
    
    private Widget CreateMenuHeader(string text)
    {
        var label = new Label
        {
            Text = text,
            TextColor = Colors.AccentCyan,
            Scale = FontScale.Medium,
            Padding = Padding.Small
        };
        
        return label;
    }
    
    private Widget CreateMenuSeparator()
    {
        var separator = new HorizontalSeparator
        {
            Background = CreateSolidBrush(Colors.BorderNormal),
            Thickness = BorderThickness.Thin
        };
        
        return separator;
    }
    
    private Widget CreateMenuItem(string text, Action onClick)
    {
        var label = new Label
        {
            Text = text,
            TextColor = Colors.TextPrimary,
            Padding = Padding.Small
        };
        
        label.TouchDown += (s, e) =>
        {
            onClick();
        };
        
        return label;
    }
    
    private void ShowMenu(List<Widget> items, Vector2 screenPosition)
    {
        if (items.Count == 0)
            return;
        
        _activeMenu = new VerticalStackPanel
        {
            Left = (int)screenPosition.X,
            Top = (int)screenPosition.Y
        };
        
        _activeMenu.Background = CreateSolidBrush(Colors.BackgroundDark);
        _activeMenu.Border = CreateSolidBrush(Colors.BorderNormal);
        _activeMenu.BorderThickness = new Myra.Graphics2D.Thickness(BorderThickness.Normal);
        _activeMenu.Padding = Padding.Small;
        
        foreach (var item in items)
        {
            _activeMenu.Widgets.Add(item);
        }
        
        _desktop.Widgets.Add(_activeMenu);
    }
    
    private void ShowSplitArmyDialog(ArmyState army)
    {
        var dialog = new Dialog();
        var mainGrid = ThemedUIFactory.CreateGrid(Spacing.Medium, Spacing.Medium);
        
        mainGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        mainGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        mainGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        mainGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        mainGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        
        var titleLabel = ThemedUIFactory.CreateHeadingLabel("Split Army");
        titleLabel.GridRow = 0;
        titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        mainGrid.Widgets.Add(titleLabel);
        
        var infoLabel = ThemedUIFactory.CreateLabel($"Total Units: {army.UnitCount}");
        infoLabel.GridRow = 1;
        infoLabel.HorizontalAlignment = HorizontalAlignment.Center;
        mainGrid.Widgets.Add(infoLabel);
        
        var splitPanel = new HorizontalStackPanel
        {
            Spacing = Spacing.Medium,
            HorizontalAlignment = HorizontalAlignment.Center,
            GridRow = 2
        };
        
        var splitLabel = ThemedUIFactory.CreateLabel("Units to split off:");
        splitPanel.Widgets.Add(splitLabel);
        
        var splitSpinner = ThemedUIFactory.CreateSpinButton(1, 1, army.UnitCount - 1);
        splitSpinner.Width = 100;
        splitPanel.Widgets.Add(splitSpinner);
        
        mainGrid.Widgets.Add(splitPanel);
        
        var remainingLabel = ThemedUIFactory.CreateSecondaryLabel($"Remaining units: {army.UnitCount - 1}");
        remainingLabel.GridRow = 3;
        remainingLabel.HorizontalAlignment = HorizontalAlignment.Center;
        mainGrid.Widgets.Add(remainingLabel);
        
        splitSpinner.ValueChanged += (s, e) =>
        {
            var splitCount = splitSpinner.Value ?? 1;
            var remaining = army.UnitCount - splitCount;
            remainingLabel.Text = $"Remaining units: {remaining}";
        };
        
        var buttonsPanel = new HorizontalStackPanel
        {
            Spacing = Spacing.Medium,
            HorizontalAlignment = HorizontalAlignment.Center,
            GridRow = 4
        };
        
        var confirmButton = ThemedUIFactory.CreateButton("Split", ButtonTheme.Primary);
        confirmButton.Click += (s, a) =>
        {
            var splitCount = (int)(splitSpinner.Value ?? 1);
            SendSplitArmyCommand(army.ArmyId, splitCount);
            dialog.Close();
        };
        buttonsPanel.Widgets.Add(confirmButton);
        
        var cancelButton = ThemedUIFactory.CreateButton("Cancel", ButtonTheme.Danger);
        cancelButton.Click += (s, a) => dialog.Close();
        buttonsPanel.Widgets.Add(cancelButton);
        
        mainGrid.Widgets.Add(buttonsPanel);
        
        var containerPanel = ThemedUIFactory.CreateAccentFramePanel();
        containerPanel.Width = 400;
        containerPanel.Padding = Padding.XLarge;
        containerPanel.Widgets.Add(mainGrid);
        
        dialog.Content = containerPanel;
        dialog.ShowModal(_desktop);
    }
    
    private void ShowMergeArmiesDialog(ArmyState selectedArmy, List<ArmyState> otherArmies)
    {
        var dialog = new Dialog();
        var mainGrid = ThemedUIFactory.CreateGrid(Spacing.Medium, Spacing.Medium);
        
        mainGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        mainGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        mainGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        mainGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        
        var titleLabel = ThemedUIFactory.CreateHeadingLabel("Merge Armies");
        titleLabel.GridRow = 0;
        titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        mainGrid.Widgets.Add(titleLabel);
        
        var selectedArmyShortId = selectedArmy.ArmyId.Length > 8 ? selectedArmy.ArmyId.Substring(0, 8) + "..." : selectedArmy.ArmyId;
        var infoLabel = ThemedUIFactory.CreateLabel($"Selected: Army {selectedArmyShortId} ({selectedArmy.UnitCount} units)");
        infoLabel.GridRow = 1;
        infoLabel.HorizontalAlignment = HorizontalAlignment.Center;
        mainGrid.Widgets.Add(infoLabel);
        
        var armiesPanel = new VerticalStackPanel
        {
            Spacing = Spacing.Small,
            GridRow = 2
        };
        
        var selectLabel = ThemedUIFactory.CreateSecondaryLabel("Select army to merge with:");
        armiesPanel.Widgets.Add(selectLabel);
        
        ArmyState? targetArmy = null;
        
        foreach (var army in otherArmies)
        {
            var armyShortId = army.ArmyId.Length > 8 ? army.ArmyId.Substring(0, 8) + "..." : army.ArmyId;
            var armyButton = ThemedUIFactory.CreateButton($"Army {armyShortId} ({army.UnitCount} units)", 300, Sizes.ButtonMediumHeight);
            armyButton.Click += (s, a) =>
            {
                targetArmy = army;
                SendMergeArmiesCommand(selectedArmy.ArmyId, army.ArmyId);
                dialog.Close();
            };
            armiesPanel.Widgets.Add(armyButton);
        }
        
        mainGrid.Widgets.Add(armiesPanel);
        
        var buttonsPanel = new HorizontalStackPanel
        {
            Spacing = Spacing.Medium,
            HorizontalAlignment = HorizontalAlignment.Center,
            GridRow = 3
        };
        
        var cancelButton = ThemedUIFactory.CreateButton("Cancel", ButtonTheme.Danger);
        cancelButton.Click += (s, a) => dialog.Close();
        buttonsPanel.Widgets.Add(cancelButton);
        
        mainGrid.Widgets.Add(buttonsPanel);
        
        var containerPanel = ThemedUIFactory.CreateAccentFramePanel();
        containerPanel.Width = 400;
        containerPanel.Padding = Padding.XLarge;
        containerPanel.Widgets.Add(mainGrid);
        
        dialog.Content = containerPanel;
        dialog.ShowModal(_desktop);
    }
    
    private void ShowMergeAllArmiesDialog(List<ArmyState> armies)
    {
        var totalUnits = armies.Sum(a => a.UnitCount);
        var dialog = new Dialog();
        var mainGrid = ThemedUIFactory.CreateGrid(Spacing.Medium, Spacing.Medium);
        
        mainGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        mainGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        mainGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        
        var titleLabel = ThemedUIFactory.CreateHeadingLabel("Merge All Armies");
        titleLabel.GridRow = 0;
        titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        mainGrid.Widgets.Add(titleLabel);
        
        var infoLabel = ThemedUIFactory.CreateLabel($"Merge {armies.Count} armies into one?\nTotal units: {totalUnits}");
        infoLabel.GridRow = 1;
        infoLabel.HorizontalAlignment = HorizontalAlignment.Center;
        infoLabel.Wrap = true;
        mainGrid.Widgets.Add(infoLabel);
        
        var buttonsPanel = new HorizontalStackPanel
        {
            Spacing = Spacing.Medium,
            HorizontalAlignment = HorizontalAlignment.Center,
            GridRow = 2
        };
        
        var confirmButton = ThemedUIFactory.CreateButton("Merge", ButtonTheme.Primary);
        confirmButton.Click += (s, a) =>
        {
            SendMergeAllArmiesCommand(armies);
            dialog.Close();
        };
        buttonsPanel.Widgets.Add(confirmButton);
        
        var cancelButton = ThemedUIFactory.CreateButton("Cancel", ButtonTheme.Danger);
        cancelButton.Click += (s, a) => dialog.Close();
        buttonsPanel.Widgets.Add(cancelButton);
        
        mainGrid.Widgets.Add(buttonsPanel);
        
        var containerPanel = ThemedUIFactory.CreateAccentFramePanel();
        containerPanel.Width = 400;
        containerPanel.Padding = Padding.XLarge;
        containerPanel.Widgets.Add(mainGrid);
        
        dialog.Content = containerPanel;
        dialog.ShowModal(_desktop);
    }
    
    private void ShowAssignHeroDialog(ArmyState army)
    {
        var dialog = new Dialog();
        var mainGrid = ThemedUIFactory.CreateGrid(Spacing.Medium, Spacing.Medium);
        
        mainGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        mainGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        mainGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        mainGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        
        var titleLabel = ThemedUIFactory.CreateHeadingLabel("Assign Hero");
        titleLabel.GridRow = 0;
        titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        titleLabel.TextColor = Colors.HeroColor;
        mainGrid.Widgets.Add(titleLabel);
        
        var armyShortId = army.ArmyId.Length > 8 ? army.ArmyId.Substring(0, 8) + "..." : army.ArmyId;
        var infoLabel = ThemedUIFactory.CreateLabel($"Army: {armyShortId} ({army.UnitCount} units)");
        infoLabel.GridRow = 1;
        infoLabel.HorizontalAlignment = HorizontalAlignment.Center;
        mainGrid.Widgets.Add(infoLabel);
        
        var heroesPanel = new VerticalStackPanel
        {
            Spacing = Spacing.Small,
            GridRow = 2
        };
        
        var selectLabel = ThemedUIFactory.CreateSecondaryLabel("Available Heroes:");
        heroesPanel.Widgets.Add(selectLabel);
        
        var heroNames = new[] { "General Vex", "Admiral Kora", "Commander Riven", "Captain Dax" };
        var heroClasses = new[] { "Warrior", "Tactician", "Engineer", "Scout" };
        
        for (int i = 0; i < heroNames.Length; i++)
        {
            var heroName = heroNames[i];
            var heroClass = heroClasses[i];
            var heroButton = ThemedUIFactory.CreateButton($"{heroName} ({heroClass})", 300, Sizes.ButtonMediumHeight, ButtonTheme.Hero);
            heroButton.Click += (s, a) =>
            {
                SendAssignHeroCommand(army.ArmyId, heroName);
                dialog.Close();
            };
            heroesPanel.Widgets.Add(heroButton);
        }
        
        var noHeroLabel = ThemedUIFactory.CreateSmallLabel("(Hero system not yet fully implemented)");
        noHeroLabel.TextColor = Colors.TextSecondary;
        heroesPanel.Widgets.Add(noHeroLabel);
        
        mainGrid.Widgets.Add(heroesPanel);
        
        var buttonsPanel = new HorizontalStackPanel
        {
            Spacing = Spacing.Medium,
            HorizontalAlignment = HorizontalAlignment.Center,
            GridRow = 3
        };
        
        var cancelButton = ThemedUIFactory.CreateButton("Cancel", ButtonTheme.Danger);
        cancelButton.Click += (s, a) => dialog.Close();
        buttonsPanel.Widgets.Add(cancelButton);
        
        mainGrid.Widgets.Add(buttonsPanel);
        
        var containerPanel = ThemedUIFactory.CreateHeroPanel();
        containerPanel.Width = 400;
        containerPanel.Padding = Padding.XLarge;
        containerPanel.Widgets.Add(mainGrid);
        
        dialog.Content = containerPanel;
        dialog.ShowModal(_desktop);
    }
    
    private void ShowArmyInfoDialog(ArmyState army)
    {
        var playerState = _gameStateCache.GetPlayerState(army.OwnerId);
        var playerName = playerState?.PlayerName ?? "Unknown";
        var armyShortId = army.ArmyId.Length > 8 ? army.ArmyId.Substring(0, 8) + "..." : army.ArmyId;
        
        var armyInfo = $"Army: {armyShortId}\n" +
                      $"Owner: {playerName}\n" +
                      $"Units: {army.UnitCount}\n" +
                      $"Location: {GetLocationName(army.LocationId, army.LocationType)}\n" +
                      $"Has Moved: {(army.HasMovedThisTurn ? "Yes" : "No")}\n" +
                      $"In Combat: {(army.IsInCombat ? "Yes" : "No")}";
        
        var dialog = Dialog.CreateMessageBox("Army Info", armyInfo);
        dialog.ShowModal(_desktop);
    }
    
    private void ShowRegionInfoDialog(RegionData region)
    {
        var ownership = _gameStateCache.GetRegionOwnership(region.Id);
        var ownerName = "Unowned";
        if (ownership != null && !string.IsNullOrEmpty(ownership.OwnerId))
        {
            var playerState = _gameStateCache.GetPlayerState(ownership.OwnerId);
            ownerName = playerState?.PlayerName ?? "Unknown";
        }
        
        var armies = _gameStateCache.GetArmiesAtLocation(region.Id, LocationType.Region);
        var armyCount = armies.Count;
        var totalUnits = armies.Sum(a => a.UnitCount);
        
        var body = FindStellarBodyForRegion(region.Id);
        var bodyName = body?.Name ?? "Unknown";
        
        var regionInfo = $"Region: {region.Name}\n" +
                        $"Stellar Body: {bodyName}\n" +
                        $"Owner: {ownerName}\n" +
                        $"Armies: {armyCount}\n" +
                        $"Total Units: {totalUnits}";
        
        var dialog = Dialog.CreateMessageBox("Region Info", regionInfo);
        dialog.ShowModal(_desktop);
    }
    
    private void ShowStellarBodyInfoDialog(StellarBodyData body)
    {
        var regionCount = body.Regions.Count;
        var ownedRegions = body.Regions.Count(r =>
        {
            var ownership = _gameStateCache.GetRegionOwnership(r.Id);
            return ownership != null && ownership.OwnerId == _currentPlayerId;
        });
        
        var bodyInfo = $"{body.Name}\n" +
                      $"Type: {body.Type}\n" +
                      $"Regions: {regionCount}\n" +
                      $"Your Regions: {ownedRegions}";
        
        var dialog = Dialog.CreateMessageBox("Stellar Body Info", bodyInfo);
        dialog.ShowModal(_desktop);
    }
    
    private void ShowUpgradeStellarBodyDialog(StellarBodyData body)
    {
        var dialog = new Dialog();
        var mainGrid = ThemedUIFactory.CreateGrid(Spacing.Medium, Spacing.Medium);
        
        mainGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        mainGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        mainGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        mainGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        
        var titleLabel = ThemedUIFactory.CreateHeadingLabel("Upgrade Stellar Body");
        titleLabel.GridRow = 0;
        titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        mainGrid.Widgets.Add(titleLabel);
        
        var infoLabel = ThemedUIFactory.CreateLabel($"{body.Name} ({body.Type})");
        infoLabel.GridRow = 1;
        infoLabel.HorizontalAlignment = HorizontalAlignment.Center;
        mainGrid.Widgets.Add(infoLabel);
        
        var upgradesPanel = new VerticalStackPanel
        {
            Spacing = Spacing.Small,
            GridRow = 2
        };
        
        var selectLabel = ThemedUIFactory.CreateSecondaryLabel("Available Upgrades:");
        upgradesPanel.Widgets.Add(selectLabel);
        
        var upgrades = new[]
        {
            ("Production Facility", "Cost: 50 Metal, +2 Production"),
            ("Defense Station", "Cost: 40 Metal, +3 Defense"),
            ("Research Lab", "Cost: 60 Metal, Research Boost"),
            ("Shipyard", "Cost: 80 Metal, Build Ships")
        };
        
        foreach (var (upgradeName, upgradeInfo) in upgrades)
        {
            var upgradeButton = ThemedUIFactory.CreateButton($"{upgradeName}\n{upgradeInfo}", 350, 50);
            upgradeButton.Click += (s, a) =>
            {
                SendUpgradeStellarBodyCommand(body.Id, upgradeName);
                dialog.Close();
            };
            upgradesPanel.Widgets.Add(upgradeButton);
        }
        
        var noteLabel = ThemedUIFactory.CreateSmallLabel("(Upgrade system not yet fully implemented)");
        noteLabel.TextColor = Colors.TextSecondary;
        upgradesPanel.Widgets.Add(noteLabel);
        
        mainGrid.Widgets.Add(upgradesPanel);
        
        var buttonsPanel = new HorizontalStackPanel
        {
            Spacing = Spacing.Medium,
            HorizontalAlignment = HorizontalAlignment.Center,
            GridRow = 3
        };
        
        var cancelButton = ThemedUIFactory.CreateButton("Cancel", ButtonTheme.Danger);
        cancelButton.Click += (s, a) => dialog.Close();
        buttonsPanel.Widgets.Add(cancelButton);
        
        mainGrid.Widgets.Add(buttonsPanel);
        
        var containerPanel = ThemedUIFactory.CreateAccentFramePanel();
        containerPanel.Width = 400;
        containerPanel.Padding = Padding.XLarge;
        containerPanel.Widgets.Add(mainGrid);
        
        dialog.Content = containerPanel;
        dialog.ShowModal(_desktop);
    }
    
    private void ShowHyperspaceLaneInfoDialog(HyperspaceLaneData lane, string mouthId)
    {
        var systemA = _mapData.StarSystems.FirstOrDefault(s => s.Id == lane.StarSystemAId);
        var systemB = _mapData.StarSystems.FirstOrDefault(s => s.Id == lane.StarSystemBId);
        
        var ownershipA = _gameStateCache.GetHyperspaceLaneMouthOwnership(lane.MouthAId);
        var ownershipB = _gameStateCache.GetHyperspaceLaneMouthOwnership(lane.MouthBId);
        
        var ownerA = "Unowned";
        var ownerB = "Unowned";
        
        if (ownershipA != null && !string.IsNullOrEmpty(ownershipA.OwnerId))
        {
            var playerState = _gameStateCache.GetPlayerState(ownershipA.OwnerId);
            ownerA = playerState?.PlayerName ?? "Unknown";
        }
        
        if (ownershipB != null && !string.IsNullOrEmpty(ownershipB.OwnerId))
        {
            var playerState = _gameStateCache.GetPlayerState(ownershipB.OwnerId);
            ownerB = playerState?.PlayerName ?? "Unknown";
        }
        
        var laneInfo = $"{lane.Name}\n\n" +
                      $"Connects:\n" +
                      $"  {systemA?.Name ?? "Unknown"} - {ownerA}\n" +
                      $"  {systemB?.Name ?? "Unknown"} - {ownerB}";
        
        var dialog = Dialog.CreateMessageBox("Hyperspace Lane", laneInfo);
        dialog.ShowModal(_desktop);
    }
    
    private void ShowReinforceDialog(string locationId, LocationType locationType, string locationName)
    {
        var dialog = new Dialog();
        var mainGrid = ThemedUIFactory.CreateGrid(Spacing.Medium, Spacing.Medium);
        
        mainGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        mainGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        mainGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        mainGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        
        var titleLabel = ThemedUIFactory.CreateHeadingLabel("Reinforce Location");
        titleLabel.GridRow = 0;
        titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        mainGrid.Widgets.Add(titleLabel);
        
        var infoLabel = ThemedUIFactory.CreateLabel($"Location: {locationName}");
        infoLabel.GridRow = 1;
        infoLabel.HorizontalAlignment = HorizontalAlignment.Center;
        mainGrid.Widgets.Add(infoLabel);
        
        var reinforcePanel = new HorizontalStackPanel
        {
            Spacing = Spacing.Medium,
            HorizontalAlignment = HorizontalAlignment.Center,
            GridRow = 2
        };
        
        var unitsLabel = ThemedUIFactory.CreateLabel("Units to deploy:");
        reinforcePanel.Widgets.Add(unitsLabel);
        
        var unitsSpinner = ThemedUIFactory.CreateSpinButton(1, 1, 10);
        unitsSpinner.Width = 100;
        reinforcePanel.Widgets.Add(unitsSpinner);
        
        mainGrid.Widgets.Add(reinforcePanel);
        
        var buttonsPanel = new HorizontalStackPanel
        {
            Spacing = Spacing.Medium,
            HorizontalAlignment = HorizontalAlignment.Center,
            GridRow = 3
        };
        
        var confirmButton = ThemedUIFactory.CreateButton("Reinforce", ButtonTheme.Primary);
        confirmButton.Click += (s, a) =>
        {
            var unitCount = (int)(unitsSpinner.Value ?? 1);
            SendReinforceLocationCommand(locationId, locationType, unitCount);
            dialog.Close();
        };
        buttonsPanel.Widgets.Add(confirmButton);
        
        var cancelButton = ThemedUIFactory.CreateButton("Cancel", ButtonTheme.Danger);
        cancelButton.Click += (s, a) => dialog.Close();
        buttonsPanel.Widgets.Add(cancelButton);
        
        mainGrid.Widgets.Add(buttonsPanel);
        
        var containerPanel = ThemedUIFactory.CreateAccentFramePanel();
        containerPanel.Width = 400;
        containerPanel.Padding = Padding.XLarge;
        containerPanel.Widgets.Add(mainGrid);
        
        dialog.Content = containerPanel;
        dialog.ShowModal(_desktop);
    }
    
    private void ShowDiplomacyDialog(string targetPlayerId)
    {
        var playerState = _gameStateCache.GetPlayerState(targetPlayerId);
        var playerName = playerState?.PlayerName ?? "Unknown Player";
        
        var dialog = new Dialog();
        var mainGrid = ThemedUIFactory.CreateGrid(Spacing.Medium, Spacing.Medium);
        
        mainGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        mainGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        mainGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        mainGrid.RowsProportions.Add(new Proportion(ProportionType.Auto));
        
        var titleLabel = ThemedUIFactory.CreateHeadingLabel("Diplomacy");
        titleLabel.GridRow = 0;
        titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        mainGrid.Widgets.Add(titleLabel);
        
        var playerLabel = ThemedUIFactory.CreateLabel($"Player: {playerName}");
        playerLabel.GridRow = 1;
        playerLabel.HorizontalAlignment = HorizontalAlignment.Center;
        mainGrid.Widgets.Add(playerLabel);
        
        var actionsPanel = new VerticalStackPanel
        {
            Spacing = Spacing.Small,
            GridRow = 2
        };
        
        var allianceButton = ThemedUIFactory.CreateButton("Propose Alliance", 300, Sizes.ButtonMediumHeight, ButtonTheme.Success);
        allianceButton.Click += (s, a) =>
        {
            SendFormAllianceCommand(targetPlayerId);
            dialog.Close();
        };
        actionsPanel.Widgets.Add(allianceButton);
        
        var breakAllianceButton = ThemedUIFactory.CreateButton("Break Alliance", 300, Sizes.ButtonMediumHeight, ButtonTheme.Danger);
        breakAllianceButton.Click += (s, a) =>
        {
            SendBreakAllianceCommand(targetPlayerId);
            dialog.Close();
        };
        actionsPanel.Widgets.Add(breakAllianceButton);
        
        var viewInfoButton = ThemedUIFactory.CreateButton("View Player Info", 300, Sizes.ButtonMediumHeight);
        viewInfoButton.Click += (s, a) =>
        {
            dialog.Close();
            ShowPlayerInfoDialog(targetPlayerId);
        };
        actionsPanel.Widgets.Add(viewInfoButton);
        
        var noteLabel = ThemedUIFactory.CreateSmallLabel("(Alliance system not yet fully implemented)");
        noteLabel.TextColor = Colors.TextSecondary;
        noteLabel.HorizontalAlignment = HorizontalAlignment.Center;
        actionsPanel.Widgets.Add(noteLabel);
        
        mainGrid.Widgets.Add(actionsPanel);
        
        var buttonsPanel = new HorizontalStackPanel
        {
            Spacing = Spacing.Medium,
            HorizontalAlignment = HorizontalAlignment.Center,
            GridRow = 3
        };
        
        var cancelButton = ThemedUIFactory.CreateButton("Cancel", ButtonTheme.Danger);
        cancelButton.Click += (s, a) => dialog.Close();
        buttonsPanel.Widgets.Add(cancelButton);
        
        mainGrid.Widgets.Add(buttonsPanel);
        
        var containerPanel = ThemedUIFactory.CreateAccentFramePanel();
        containerPanel.Width = 400;
        containerPanel.Padding = Padding.XLarge;
        containerPanel.Widgets.Add(mainGrid);
        
        dialog.Content = containerPanel;
        dialog.ShowModal(_desktop);
    }
    
    private void ShowPlayerInfoDialog(string playerId)
    {
        var playerState = _gameStateCache.GetPlayerState(playerId);
        if (playerState == null)
        {
            var errorDialog = Dialog.CreateMessageBox("Error", "Player information not available");
            errorDialog.ShowModal(_desktop);
            return;
        }
        
        var ownedRegions = _gameStateCache.GetRegionsOwnedByPlayer(playerId).Count;
        var ownedLaneMouths = _gameStateCache.GetHyperspaceLaneMouthsOwnedByPlayer(playerId).Count;
        var armies = _gameStateCache.GetArmiesOwnedByPlayer(playerId).Count;
        var totalUnits = _gameStateCache.GetArmiesOwnedByPlayer(playerId).Sum(a => a.UnitCount);
        
        var playerInfo = $"Player: {playerState.PlayerName}\n" +
                        $"Turn Order: {playerState.TurnOrder}\n\n" +
                        $"Resources:\n" +
                        $"  Population: {playerState.PopulationStockpile}\n" +
                        $"  Metal: {playerState.MetalStockpile}\n" +
                        $"  Fuel: {playerState.FuelStockpile}\n\n" +
                        $"Territories:\n" +
                        $"  Regions: {ownedRegions}\n" +
                        $"  Lane Mouths: {ownedLaneMouths}\n\n" +
                        $"Military:\n" +
                        $"  Armies: {armies}\n" +
                        $"  Total Units: {totalUnits}";
        
        var dialog = Dialog.CreateMessageBox("Player Info", playerInfo);
        dialog.ShowModal(_desktop);
    }
    
    private string GetLocationName(string locationId, LocationType locationType)
    {
        if (locationType == LocationType.Region)
        {
            foreach (var system in _mapData.StarSystems)
            {
                foreach (var body in system.StellarBodies)
                {
                    var region = body.Regions.FirstOrDefault(r => r.Id == locationId);
                    if (region != null)
                        return region.Name;
                }
            }
        }
        else if (locationType == LocationType.HyperspaceLaneMouth)
        {
            var lane = _mapData.HyperspaceLanes.FirstOrDefault(l => l.MouthAId == locationId || l.MouthBId == locationId);
            if (lane != null)
                return $"{lane.Name} Portal";
        }
        
        return "Unknown";
    }
    
    private void SendSplitArmyCommand(string armyId, int splitCount)
    {
        if (_currentPlayerId == null)
            return;
        
        var gameId = _gameStateCache.GetGameId();
        if (gameId == null)
            return;
        
        Task.Run(async () =>
        {
            try
            {
                await _gameClient.SendSplitArmyAsync(_currentPlayerId, gameId, armyId, splitCount);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Failed to send split army command: {ex.Message}");
            }
        });
    }
    
    private void SendMergeArmiesCommand(string sourceArmyId, string targetArmyId)
    {
        if (_currentPlayerId == null)
            return;
        
        var gameId = _gameStateCache.GetGameId();
        if (gameId == null)
            return;
        
        Task.Run(async () =>
        {
            try
            {
                await _gameClient.SendMergeArmiesAsync(_currentPlayerId, gameId, sourceArmyId, targetArmyId);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Failed to send merge armies command: {ex.Message}");
            }
        });
    }
    
    private void SendMergeAllArmiesCommand(List<ArmyState> armies)
    {
        if (_currentPlayerId == null || armies.Count == 0)
            return;
        
        var gameId = _gameStateCache.GetGameId();
        if (gameId == null)
            return;
        
        var firstArmy = armies.First();
        var armyIds = armies.Select(a => a.ArmyId).ToList();
        
        Task.Run(async () =>
        {
            try
            {
                await _gameClient.SendMergeAllArmiesAsync(_currentPlayerId, gameId, armyIds, firstArmy.LocationId, firstArmy.LocationType);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Failed to send merge all armies command: {ex.Message}");
            }
        });
    }
    
    private void SendAssignHeroCommand(string armyId, string heroName)
    {
        if (_currentPlayerId == null)
            return;
        
        var gameId = _gameStateCache.GetGameId();
        if (gameId == null)
            return;
        
        Task.Run(async () =>
        {
            try
            {
                await _gameClient.SendAssignHeroAsync(_currentPlayerId, gameId, armyId, heroName);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Failed to send assign hero command: {ex.Message}");
            }
        });
    }
    
    private void SendUpgradeStellarBodyCommand(string bodyId, string upgradeName)
    {
        if (_currentPlayerId == null)
            return;
        
        var gameId = _gameStateCache.GetGameId();
        if (gameId == null)
            return;
        
        Task.Run(async () =>
        {
            try
            {
                await _gameClient.SendUpgradeStellarBodyAsync(_currentPlayerId, gameId, bodyId, upgradeName);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Failed to send upgrade stellar body command: {ex.Message}");
            }
        });
    }
    
    private void SendReinforceLocationCommand(string locationId, LocationType locationType, int unitCount)
    {
        if (_currentPlayerId == null)
            return;
        
        var gameId = _gameStateCache.GetGameId();
        if (gameId == null)
            return;
        
        Task.Run(async () =>
        {
            try
            {
                await _gameClient.SendReinforceLocationAsync(_currentPlayerId, gameId, locationId, locationType, unitCount);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Failed to send reinforce location command: {ex.Message}");
            }
        });
    }
    
    private void SendFormAllianceCommand(string targetPlayerId)
    {
        if (_currentPlayerId == null)
            return;
        
        var gameId = _gameStateCache.GetGameId();
        if (gameId == null)
            return;
        
        Task.Run(async () =>
        {
            try
            {
                await _gameClient.SendFormAllianceAsync(_currentPlayerId, gameId, targetPlayerId);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Failed to send form alliance command: {ex.Message}");
            }
        });
    }
    
    private void SendBreakAllianceCommand(string targetPlayerId)
    {
        if (_currentPlayerId == null)
            return;
        
        var gameId = _gameStateCache.GetGameId();
        if (gameId == null)
            return;
        
        Task.Run(async () =>
        {
            try
            {
                await _gameClient.SendBreakAllianceAsync(_currentPlayerId, gameId, targetPlayerId);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Failed to send break alliance command: {ex.Message}");
            }
        });
    }
    
    private ArmyState? FindArmyAtPosition(Vector2 worldPosition)
    {
        foreach (var system in _mapData.StarSystems)
        {
            foreach (var body in system.StellarBodies)
            {
                foreach (var region in body.Regions)
                {
                    if (Vector2.Distance(worldPosition, region.Position) < 15f)
                    {
                        var armies = _gameStateCache.GetArmiesAtLocation(region.Id, LocationType.Region);
                        if (armies.Count > 0)
                        {
                            return armies.FirstOrDefault(a => a.OwnerId == _currentPlayerId) ?? armies.First();
                        }
                    }
                }
            }
        }
        
        foreach (var lane in _mapData.HyperspaceLanes)
        {
            if (Vector2.Distance(worldPosition, lane.MouthAPosition) < 15f)
            {
                var armies = _gameStateCache.GetArmiesAtLocation(lane.MouthAId, LocationType.HyperspaceLaneMouth);
                if (armies.Count > 0)
                {
                    return armies.FirstOrDefault(a => a.OwnerId == _currentPlayerId) ?? armies.First();
                }
            }
            
            if (Vector2.Distance(worldPosition, lane.MouthBPosition) < 15f)
            {
                var armies = _gameStateCache.GetArmiesAtLocation(lane.MouthBId, LocationType.HyperspaceLaneMouth);
                if (armies.Count > 0)
                {
                    return armies.FirstOrDefault(a => a.OwnerId == _currentPlayerId) ?? armies.First();
                }
            }
        }
        
        return null;
    }
    
    private RegionData? FindRegionAtPosition(Vector2 worldPosition)
    {
        foreach (var system in _mapData.StarSystems)
        {
            foreach (var body in system.StellarBodies)
            {
                foreach (var region in body.Regions)
                {
                    if (Vector2.Distance(worldPosition, region.Position) < 10f)
                    {
                        return region;
                    }
                }
            }
        }
        
        return null;
    }
    
    private StellarBodyData? FindStellarBodyAtPosition(Vector2 worldPosition)
    {
        foreach (var system in _mapData.StarSystems)
        {
            foreach (var body in system.StellarBodies)
            {
                float bodyRadius = body.Type switch
                {
                    StellarBodyType.GasGiant => 20f,
                    StellarBodyType.RockyPlanet => 15f,
                    StellarBodyType.Planetoid => 8f,
                    StellarBodyType.Comet => 6f,
                    _ => 10f
                };
                
                if (Vector2.Distance(worldPosition, body.Position) < bodyRadius)
                {
                    return body;
                }
            }
        }
        
        return null;
    }
    
    private (string, Vector2)? FindHyperspaceLaneMouthAtPosition(Vector2 worldPosition)
    {
        foreach (var lane in _mapData.HyperspaceLanes)
        {
            if (Vector2.Distance(worldPosition, lane.MouthAPosition) < 12f)
            {
                return (lane.MouthAId, lane.MouthAPosition);
            }
            
            if (Vector2.Distance(worldPosition, lane.MouthBPosition) < 12f)
            {
                return (lane.MouthBId, lane.MouthBPosition);
            }
        }
        
        return null;
    }
    
    private StellarBodyData? FindStellarBodyForRegion(string regionId)
    {
        foreach (var system in _mapData.StarSystems)
        {
            foreach (var body in system.StellarBodies)
            {
                if (body.Regions.Any(r => r.Id == regionId))
                {
                    return body;
                }
            }
        }
        
        return null;
    }
}
