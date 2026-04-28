using System.Collections;
using System.Reflection;
using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;
using RiskyStars.Client;

namespace RiskyStars.Tests;

[Collection("Myra UI tests")]
public class GameplayHudOverlayTests
{
    [Fact]
    public void BuildSelectionContent_ReturnsLiveSelectionPanelForSideBar()
    {
        var overlay = CreateOverlay();

        var sideBarContent = overlay.BuildSelectionContent();

        Assert.Same(overlay.SelectionPanel, sideBarContent);
        Assert.Equal(HorizontalAlignment.Stretch, sideBarContent.HorizontalAlignment);
        Assert.Equal(VerticalAlignment.Top, sideBarContent.VerticalAlignment);
    }

    [Fact]
    public void SideBarSelectionPanel_UpdatesWhenRegionSelected()
    {
        var overlay = CreateOverlay();
        var sideBarContent = overlay.BuildSelectionContent();
        var selection = new SelectionState();
        selection.SelectRegion(new RegionData
        {
            Id = "alpha-region",
            Name = "Alpha Ridge",
            StellarBodyId = "body-1",
            Position = Vector2.Zero
        });

        overlay.Update(
            gameStateCache: null,
            mapData: CreateMapDataWithRegion("alpha-region", "body-1", "Proxima b", "Proxima Centauri"),
            currentPlayerId: null,
            statusTitle: null,
            statusDetail: null,
            statusAccent: Color.White,
            selection: selection,
            isAiThinking: false,
            activeAiPlayerName: null,
            recentAiLogEntries: null,
            showHelp: false,
            dashboardVisible: false,
            aiVisible: false,
            debugVisible: false,
            uiScaleVisible: false,
            encyclopediaVisible: false,
            tutorialVisible: false);

        Assert.True(sideBarContent.Visible);

        var visibleLabels = CollectVisibleLabelTexts(sideBarContent);
        Assert.Contains("Selected Region", visibleLabels);
        Assert.Contains("Name: Alpha Ridge", visibleLabels);
        Assert.Contains("Body: Proxima b", visibleLabels);
        Assert.Contains("Star: Proxima Centauri", visibleLabels);
        Assert.Contains("Owner: Unowned", visibleLabels);
    }

    [Fact]
    public void SideBarSelectionPanel_DoesNotShowBodyOrStarWhenRegionIsMissingFromMap()
    {
        var overlay = CreateOverlay();
        var sideBarContent = overlay.BuildSelectionContent();
        var selection = new SelectionState();
        selection.SelectRegion(new RegionData
        {
            Id = "lost-region",
            Name = "Lost Basin",
            StellarBodyId = "missing-body",
            Position = Vector2.Zero
        });

        overlay.Update(
            gameStateCache: null,
            mapData: CreateMapDataWithRegion("alpha-region", "body-1", "Proxima b", "Proxima Centauri"),
            currentPlayerId: null,
            statusTitle: null,
            statusDetail: null,
            statusAccent: Color.White,
            selection: selection,
            isAiThinking: false,
            activeAiPlayerName: null,
            recentAiLogEntries: null,
            showHelp: false,
            dashboardVisible: false,
            aiVisible: false,
            debugVisible: false,
            uiScaleVisible: false,
            encyclopediaVisible: false,
            tutorialVisible: false);

        var visibleLabels = CollectVisibleLabelTexts(sideBarContent);
        Assert.Contains("Selected Region", visibleLabels);
        Assert.Contains("Name: Lost Basin", visibleLabels);
        Assert.Contains("Owner: Unowned", visibleLabels);
        Assert.DoesNotContain(visibleLabels, label => label.StartsWith("Body:", StringComparison.Ordinal));
        Assert.DoesNotContain(visibleLabels, label => label.StartsWith("Star:", StringComparison.Ordinal));
    }

    [Fact]
    public void SideBarSelectionPanel_DoesNotRequireMapDataForRegionSelection()
    {
        var overlay = CreateOverlay();
        var sideBarContent = overlay.BuildSelectionContent();
        var selection = new SelectionState();
        selection.SelectRegion(new RegionData
        {
            Id = "orphan-region",
            Name = "Orphan Reach",
            StellarBodyId = "body-1",
            Position = Vector2.Zero
        });

        overlay.Update(
            gameStateCache: null,
            mapData: null,
            currentPlayerId: null,
            statusTitle: null,
            statusDetail: null,
            statusAccent: Color.White,
            selection: selection,
            isAiThinking: false,
            activeAiPlayerName: null,
            recentAiLogEntries: null,
            showHelp: false,
            dashboardVisible: false,
            aiVisible: false,
            debugVisible: false,
            uiScaleVisible: false,
            encyclopediaVisible: false,
            tutorialVisible: false);

        var visibleLabels = CollectVisibleLabelTexts(sideBarContent);
        Assert.Contains("Selected Region", visibleLabels);
        Assert.Contains("Name: Orphan Reach", visibleLabels);
        Assert.Contains("Owner: Unowned", visibleLabels);
        Assert.DoesNotContain(visibleLabels, label => label.StartsWith("Body:", StringComparison.Ordinal));
        Assert.DoesNotContain(visibleLabels, label => label.StartsWith("Star:", StringComparison.Ordinal));
    }

    [Fact]
    public void BuildAiActivityContent_ReturnsLiveAiActivityPanelForSideBar()
    {
        var overlay = CreateOverlay();

        var sideBarContent = overlay.BuildAiActivityContent();

        Assert.Same(overlay.AiActivityPanel, sideBarContent);
        Assert.Equal(HorizontalAlignment.Stretch, sideBarContent.HorizontalAlignment);
        Assert.Equal(VerticalAlignment.Top, sideBarContent.VerticalAlignment);
    }

    private static GameplayHudOverlay CreateOverlay()
    {
        Stylesheet.Current = new Stylesheet
        {
            LabelStyle = new LabelStyle()
        };
        ThemeManager.Initialize();
        return new GameplayHudOverlay(1280, 720);
    }

    private static MapData CreateMapDataWithRegion(string regionId, string bodyId, string bodyName, string starName)
    {
        var region = new RegionData
        {
            Id = regionId,
            Name = "Mapped Region",
            StellarBodyId = bodyId,
            Position = Vector2.Zero
        };

        return new MapData
        {
            StarSystems =
            {
                new StarSystemData
                {
                    Id = "star-1",
                    Name = starName,
                    Type = StarSystemType.Home,
                    StellarBodies =
                    {
                        new StellarBodyData
                        {
                            Id = bodyId,
                            Name = bodyName,
                            StarSystemId = "star-1",
                            Type = StellarBodyType.RockyPlanet,
                            Regions = { region }
                        }
                    }
                }
            }
        };
    }

    private static IReadOnlyCollection<string> CollectVisibleLabelTexts(Widget root)
    {
        var labels = new List<string>();
        CollectVisibleLabelTexts(root, labels);
        return labels;
    }

    private static void CollectVisibleLabelTexts(Widget widget, List<string> labels)
    {
        if (widget is Label { Visible: true } label && !string.IsNullOrWhiteSpace(label.Text))
        {
            labels.Add(label.Text);
        }

        foreach (var child in GetChildWidgets(widget))
        {
            CollectVisibleLabelTexts(child, labels);
        }
    }

    private static IEnumerable<Widget> GetChildWidgets(Widget widget)
    {
        var widgetsProperty = widget.GetType().GetProperty("Widgets", BindingFlags.Instance | BindingFlags.Public);
        if (widgetsProperty?.GetValue(widget) is IEnumerable widgets)
        {
            foreach (var child in widgets)
            {
                if (child is Widget childWidget)
                {
                    yield return childWidget;
                }
            }
        }

        var contentProperty = widget.GetType().GetProperty("Content", BindingFlags.Instance | BindingFlags.Public);
        if (contentProperty?.GetValue(widget) is Widget content)
        {
            yield return content;
        }
    }
}
