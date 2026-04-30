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

    [Fact]
    public void BuildLegendContent_UsesScrollerForSidebarLegend()
    {
        var overlay = CreateOverlay();

        var legendContent = overlay.BuildLegendContent();
        var scrollViewer = Assert.Single(CollectWidgets(legendContent).OfType<ScrollViewer>());

        Assert.Equal(HorizontalAlignment.Stretch, legendContent.HorizontalAlignment);
        Assert.IsType<VerticalStackPanel>(scrollViewer.Content);
    }

    [Fact]
    public void SidePanelContainer_BoundsContentScrollerToRemainingPanelSpace()
    {
        CreateOverlay();
        var sidePanel = new SidePanelContainer("left", 280, 1280, 720, topOffset: 96);

        var rootGrid = Assert.Single(GetChildWidgets(sidePanel.Container).OfType<Grid>());
        var scrollViewer = Assert.Single(CollectWidgets(sidePanel.Container).OfType<ScrollViewer>());

        Assert.Same(sidePanel.Content, scrollViewer.Content);
        Assert.Equal(2, rootGrid.RowsProportions.Count);
        Assert.Equal("Auto", ReadProportionType(rootGrid.RowsProportions[0]));
        Assert.Equal("Fill", ReadProportionType(rootGrid.RowsProportions[1]));
        Assert.Equal(VerticalAlignment.Stretch, scrollViewer.VerticalAlignment);
        Assert.Equal(720 - 96, ((Panel)sidePanel.Container).Height);
    }

    [Fact]
    public void SidePanelContainer_DoesNotUseUnboundedVerticalStackAsPanelRoot()
    {
        CreateOverlay();
        var sidePanel = new SidePanelContainer("right", 280, 1280, 720, topOffset: 96);

        Assert.DoesNotContain(GetChildWidgets(sidePanel.Container), child => child is VerticalStackPanel);
        Assert.Contains(GetChildWidgets(sidePanel.Container), child => child is Grid);
    }

    [Fact]
    public void SidePanelContainer_CollapseKeepsHeaderAndRestoresScrollableContent()
    {
        CreateOverlay();
        var sidePanel = new SidePanelContainer("left", 280, 1280, 720, topOffset: 96);
        int collapseEvents = 0;
        sidePanel.CollapseChanged += (_, _) => collapseEvents++;

        sidePanel.SetCollapsed(true);

        Assert.True(sidePanel.IsCollapsed);
        Assert.Equal(60, ((Panel)sidePanel.Container).Width);
        Assert.False(sidePanel.Content.Visible);
        Assert.Contains(GetChildWidgets(sidePanel.Container), child => child.Visible);

        sidePanel.SetCollapsed(false);

        Assert.False(sidePanel.IsCollapsed);
        Assert.Equal(280, ((Panel)sidePanel.Container).Width);
        Assert.True(sidePanel.Content.Visible);
        Assert.Equal(2, collapseEvents);

        sidePanel.SetCollapsed(false);

        Assert.Equal(2, collapseEvents);
    }

    [Fact]
    public void SidePanelContainer_SetWidthClampsToSafePanelBounds()
    {
        CreateOverlay();
        var sidePanel = new SidePanelContainer("left", 280, 1280, 720, topOffset: 96);
        var widths = new List<int>();
        sidePanel.WidthChanged += (_, width) => widths.Add(width);

        sidePanel.SetWidth(50);
        sidePanel.SetWidth(999);

        Assert.Equal(sidePanel.MinWidth, widths[0]);
        Assert.Equal(sidePanel.MaxWidth, widths[1]);
        Assert.Equal(sidePanel.MaxWidth, sidePanel.CurrentWidth);
    }

    [Fact]
    public void SidePanelContainer_ResizeAndToggleControlsUpdateBoundedPanel()
    {
        CreateOverlay();
        var sidePanel = new SidePanelContainer("left", 280, 1280, 720, topOffset: 96);
        int collapseEvents = 0;
        sidePanel.CollapseChanged += (_, _) => collapseEvents++;

        sidePanel.ResizeViewport(1024, 640, topOffset: 120);

        Assert.Equal("left", sidePanel.Side);
        Assert.Equal(120, sidePanel.CurrentTopOffset);
        Assert.Equal(640 - 120, ((Panel)sidePanel.Container).Height);

        sidePanel.ToggleCollapse();
        sidePanel.ToggleCollapse();

        Assert.False(sidePanel.IsCollapsed);
        Assert.Equal(2, collapseEvents);
    }

    [Fact]
    public void SidePanelContainer_DoesNotRenderDebugTopOffsetLabel()
    {
        CreateOverlay();
        var sidePanel = new SidePanelContainer("left", 280, 1280, 720, topOffset: 96);

        sidePanel.ResizeViewport(1280, 720, topOffset: 120);

        Assert.DoesNotContain(
            CollectWidgets(sidePanel.Container).OfType<Label>(),
            label => label.Text.StartsWith("top=", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void SidePanelContainer_HeaderButtonsResizeAndCollapseWithoutUnboundingContent()
    {
        CreateOverlay();
        var sidePanel = new SidePanelContainer("left", 280, 1280, 720, topOffset: 96);
#pragma warning disable CS0618
        var button = new TextButton();
#pragma warning restore CS0618

        InvokePrivateSidePanelButton(sidePanel, "OnResizeInClick", button);
        InvokePrivateSidePanelButton(sidePanel, "OnResizeOutClick", button);
        InvokePrivateSidePanelButton(sidePanel, "OnCollapseClick", button);

        Assert.True(sidePanel.IsCollapsed);
        Assert.Equal(">", button.Text);
        Assert.Single(CollectWidgets(sidePanel.Container).OfType<ScrollViewer>());
    }

    [Fact]
    public void SidePanelContainer_RightCollapseKeepsDockedRightEdge()
    {
        CreateOverlay();
        var sidePanel = new SidePanelContainer("right", 280, 1280, 720, topOffset: 96);
        int originalRight = ((Panel)sidePanel.Container).Left + (((Panel)sidePanel.Container).Width ?? 0);

        sidePanel.SetCollapsed(true);

        int collapsedRight = ((Panel)sidePanel.Container).Left + (((Panel)sidePanel.Container).Width ?? 0);
        Assert.Equal(originalRight, collapsedRight);
    }

    [Fact]
    public void SidePanelContainer_AddRemoveAndClearMutateScrollableContentOnly()
    {
        CreateOverlay();
        var sidePanel = new SidePanelContainer("left", 280, 1280, 720, topOffset: 96);
        var first = new Panel();
        var second = new Panel();

        sidePanel.AddWidget(first);
        sidePanel.AddWidget(second);
        sidePanel.RemoveWidget(first);

        Assert.DoesNotContain(first, sidePanel.Content.Widgets);
        Assert.Contains(second, sidePanel.Content.Widgets);

        sidePanel.Clear();

        Assert.Empty(sidePanel.Content.Widgets);
    }

    [Fact]
    public void HelpPanel_UsesScrollerForWrappedShortcutText()
    {
        var overlay = CreateOverlay();

        var scrollViewer = Assert.Single(CollectWidgets(overlay.HelpPanel).OfType<ScrollViewer>());

        Assert.True(overlay.HelpPanel.Height > 0);
        Assert.IsType<VerticalStackPanel>(scrollViewer.Content);
    }

    private static GameplayHudOverlay CreateOverlay()
    {
        Stylesheet.Current = new Stylesheet
        {
            LabelStyle = new LabelStyle(),
            ButtonStyle = new ButtonStyle(),
            ScrollViewerStyle = new ScrollViewerStyle()
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

    private static IReadOnlyList<Widget> CollectWidgets(Widget root)
    {
        var widgets = new List<Widget> { root };
        foreach (var child in GetChildWidgets(root))
        {
            widgets.AddRange(CollectWidgets(child));
        }

        return widgets;
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

    private static string ReadProportionType(Proportion proportion)
    {
        var property = typeof(Proportion).GetProperty("Type") ??
                       typeof(Proportion).GetProperty("ProportionType");

        return property?.GetValue(proportion)?.ToString() ?? string.Empty;
    }

    private static void InvokePrivateSidePanelButton(SidePanelContainer panel, string methodName, Widget widget)
    {
        var method = typeof(SidePanelContainer).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method.Invoke(panel, [widget]);
    }
}
