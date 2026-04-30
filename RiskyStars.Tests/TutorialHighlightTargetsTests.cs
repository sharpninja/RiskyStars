using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI;
using RiskyStars.Client;

namespace RiskyStars.Tests;

public class TutorialHighlightTargetsTests
{
    public static TheoryData<TutorialStepCompletion, TutorialHighlightTarget[]> CompletionTargets =>
        new()
        {
            { TutorialStepCompletion.WorldSynced, [TutorialHighlightTarget.TopBar] },
            { TutorialStepCompletion.OwnTurn, [TutorialHighlightTarget.TopBar] },
            { TutorialStepCompletion.AnySelection, [TutorialHighlightTarget.MapViewport, TutorialHighlightTarget.SelectionPanel] },
            { TutorialStepCompletion.HelpOpen, [TutorialHighlightTarget.TopBar, TutorialHighlightTarget.HelpPanel] },
            { TutorialStepCompletion.PurchasePhase, [TutorialHighlightTarget.TopBar] },
            { TutorialStepCompletion.DashboardOpen, [TutorialHighlightTarget.TopBar, TutorialHighlightTarget.PlayerDashboard] },
            { TutorialStepCompletion.ArmyPurchased, [TutorialHighlightTarget.PlayerDashboard] },
            { TutorialStepCompletion.ReinforcementPhase, [TutorialHighlightTarget.TopBar] },
            { TutorialStepCompletion.OwnedReinforcementTargetSelected, [TutorialHighlightTarget.MapViewport, TutorialHighlightTarget.SelectionPanel] },
            { TutorialStepCompletion.MovementPhase, [TutorialHighlightTarget.TopBar] },
            { TutorialStepCompletion.OwnArmySelected, [TutorialHighlightTarget.MapViewport, TutorialHighlightTarget.SelectionPanel] },
            { TutorialStepCompletion.ArmyMoved, [TutorialHighlightTarget.MapViewport] },
            { TutorialStepCompletion.ReferenceOpen, [TutorialHighlightTarget.TopBar, TutorialHighlightTarget.EncyclopediaWindow] },
            { TutorialStepCompletion.Manual, [] }
        };

    [Theory]
    [MemberData(nameof(CompletionTargets))]
    public void ForCompletion_MapsEveryTutorialObjectiveToExpectedTargets(
        TutorialStepCompletion completion,
        TutorialHighlightTarget[] expectedTargets)
    {
        var targets = TutorialHighlightTargets.ForCompletion(completion);

        Assert.Equal(expectedTargets, targets);
    }

    [Fact]
    public void ForCompletion_OwnTurnUsesSingleTopBarHighlight()
    {
        var targets = TutorialHighlightTargets.ForCompletion(TutorialStepCompletion.OwnTurn);

        Assert.Contains(TutorialHighlightTarget.TopBar, targets);
        Assert.DoesNotContain(TutorialHighlightTarget.ResourceChips, targets);
    }

    [Fact]
    public void ForCompletion_AnySelectionHighlightsMapAndSelectionDetails()
    {
        var targets = TutorialHighlightTargets.ForCompletion(TutorialStepCompletion.AnySelection);

        Assert.Contains(TutorialHighlightTarget.MapViewport, targets);
        Assert.Contains(TutorialHighlightTarget.SelectionPanel, targets);
    }

    [Fact]
    public void ForCompletion_DashboardOpenHighlightsShortcutHintAndDashboard()
    {
        var targets = TutorialHighlightTargets.ForCompletion(TutorialStepCompletion.DashboardOpen);

        Assert.Contains(TutorialHighlightTarget.TopBar, targets);
        Assert.Contains(TutorialHighlightTarget.PlayerDashboard, targets);
    }

    [Fact]
    public void ForCompletion_ManualDoesNotLeaveStaleHighlightsVisible()
    {
        var targets = TutorialHighlightTargets.ForCompletion(TutorialStepCompletion.Manual);

        Assert.Empty(targets);
    }

    [Fact]
    public void Resolve_ReturnsEmptyWhenNoTargetsAreRequested()
    {
        var visibleBounds = new Dictionary<TutorialHighlightTarget, Rectangle>
        {
            [TutorialHighlightTarget.TopBar] = new(0, 0, 1920, 90)
        };

        var resolved = TutorialHighlightBoundsResolver.Resolve([], visibleBounds);

        Assert.Empty(resolved);
    }

    [Fact]
    public void Resolve_ReturnsEmptyWhenNoTargetsAreVisible()
    {
        var requested = new[]
        {
            TutorialHighlightTarget.TopBar
        };

        var resolved = TutorialHighlightBoundsResolver.Resolve(requested, new Dictionary<TutorialHighlightTarget, Rectangle>());

        Assert.Empty(resolved);
    }

    [Fact]
    public void Resolve_ReturnsVisibleRequestedBoundsWithLabels()
    {
        var requested = new[]
        {
            TutorialHighlightTarget.TopBar,
            TutorialHighlightTarget.HelpPanel
        };
        var visibleBounds = new Dictionary<TutorialHighlightTarget, Rectangle>
        {
            [TutorialHighlightTarget.TopBar] = new(0, 0, 1920, 90),
            [TutorialHighlightTarget.HelpPanel] = new(700, 220, 520, 430)
        };

        var resolved = TutorialHighlightBoundsResolver.Resolve(requested, visibleBounds);

        Assert.Collection(
            resolved,
            highlight =>
            {
                Assert.Equal(TutorialHighlightTarget.TopBar, highlight.Target);
                Assert.Equal("Turn and phase", highlight.Label);
                Assert.Equal(new Rectangle(0, 0, 1920, 90), highlight.Bounds);
            },
            highlight =>
            {
                Assert.Equal(TutorialHighlightTarget.HelpPanel, highlight.Target);
                Assert.Equal("Shortcut sheet", highlight.Label);
                Assert.Equal(new Rectangle(700, 220, 520, 430), highlight.Bounds);
            });
    }

    [Fact]
    public void Resolve_ReturnsLabelsForEveryVisibleTarget()
    {
        var requested = new[]
        {
            TutorialHighlightTarget.TopBar,
            TutorialHighlightTarget.ResourceChips,
            TutorialHighlightTarget.MapViewport,
            TutorialHighlightTarget.HelpPanel,
            TutorialHighlightTarget.PlayerDashboard,
            TutorialHighlightTarget.SelectionPanel,
            TutorialHighlightTarget.EncyclopediaWindow
        };
        var visibleBounds = requested.ToDictionary(
            target => target,
            target => new Rectangle((int)target + 1, (int)target + 2, 40, 30));

        var resolved = TutorialHighlightBoundsResolver.Resolve(requested, visibleBounds);

        Assert.Collection(
            resolved,
            highlight => Assert.Equal("Turn and phase", highlight.Label),
            highlight => Assert.Equal("Resources", highlight.Label),
            highlight => Assert.Equal("Map target", highlight.Label),
            highlight => Assert.Equal("Shortcut sheet", highlight.Label),
            highlight => Assert.Equal("Command dashboard", highlight.Label),
            highlight => Assert.Equal("Selection details", highlight.Label),
            highlight => Assert.Equal("Reference layer", highlight.Label));
    }

    [Fact]
    public void Resolve_UsesFallbackLabelForUnknownTarget()
    {
        var unknown = (TutorialHighlightTarget)999;
        var requested = new[]
        {
            unknown
        };
        var visibleBounds = new Dictionary<TutorialHighlightTarget, Rectangle>
        {
            [unknown] = new(10, 20, 40, 30)
        };

        var resolved = TutorialHighlightBoundsResolver.Resolve(requested, visibleBounds);

        var highlight = Assert.Single(resolved);
        Assert.Equal("Tutorial target", highlight.Label);
    }

    [Fact]
    public void Resolve_DoesNotDrawInvisibleOrMissingTargets()
    {
        var requested = new[]
        {
            TutorialHighlightTarget.TopBar,
            TutorialHighlightTarget.SelectionPanel
        };
        var visibleBounds = new Dictionary<TutorialHighlightTarget, Rectangle>
        {
            [TutorialHighlightTarget.TopBar] = new(0, 0, 1920, 90)
        };

        var resolved = TutorialHighlightBoundsResolver.Resolve(requested, visibleBounds);

        var highlight = Assert.Single(resolved);
        Assert.Equal(TutorialHighlightTarget.TopBar, highlight.Target);
        Assert.DoesNotContain(resolved, item => item.Target == TutorialHighlightTarget.SelectionPanel);
    }

    [Fact]
    public void Resolve_DeduplicatesTargetsSoHighlightDoesNotPulseTwice()
    {
        var requested = new[]
        {
            TutorialHighlightTarget.TopBar,
            TutorialHighlightTarget.TopBar
        };
        var visibleBounds = new Dictionary<TutorialHighlightTarget, Rectangle>
        {
            [TutorialHighlightTarget.TopBar] = new(0, 0, 1920, 90)
        };

        var resolved = TutorialHighlightBoundsResolver.Resolve(requested, visibleBounds);

        Assert.Single(resolved);
    }

    [Fact]
    public void ExpandAndClamp_ExpandsInsideViewport()
    {
        var expanded = TutorialHighlightBoundsResolver.ExpandAndClamp(
            new Rectangle(100, 100, 40, 30),
            padding: 8,
            screenWidth: 300,
            screenHeight: 200);

        Assert.Equal(new Rectangle(92, 92, 56, 46), expanded);
    }

    [Fact]
    public void ExpandAndClamp_ClampsAtScreenEdges()
    {
        var expanded = TutorialHighlightBoundsResolver.ExpandAndClamp(
            new Rectangle(0, 0, 40, 30),
            padding: 20,
            screenWidth: 50,
            screenHeight: 40);

        Assert.Equal(new Rectangle(0, 0, 50, 40), expanded);
    }

    [Fact]
    public void ExpandAndClamp_ReturnsEmptyForInvalidBounds()
    {
        var expanded = TutorialHighlightBoundsResolver.ExpandAndClamp(
            Rectangle.Empty,
            padding: 8,
            screenWidth: 300,
            screenHeight: 200);

        Assert.Equal(Rectangle.Empty, expanded);
    }

    [Fact]
    public void PreferExplicitBounds_UsesWindowSizeInsteadOfStaleActualBounds()
    {
        var staleActualBounds = new Rectangle(284, 154, 397, 858);

        var bounds = TutorialHighlightBoundsResolver.PreferExplicitBounds(
            staleActualBounds,
            left: 284,
            top: 154,
            width: 696,
            height: 858);

        Assert.Equal(new Rectangle(284, 154, 696, 858), bounds);
        Assert.NotEqual(staleActualBounds, bounds);
    }

    [Fact]
    public void PreferExplicitBounds_FallsBackToActualBoundsWhenExplicitSizeMissing()
    {
        var actualBounds = new Rectangle(284, 154, 696, 858);

        var bounds = TutorialHighlightBoundsResolver.PreferExplicitBounds(
            actualBounds,
            left: 284,
            top: 154,
            width: null,
            height: null);

        Assert.Equal(actualBounds, bounds);
    }

    [Fact]
    public void PreferExplicitBounds_ReturnsEmptyWhenBothActualAndExplicitBoundsAreInvalid()
    {
        var bounds = TutorialHighlightBoundsResolver.PreferExplicitBounds(
            Rectangle.Empty,
            left: 284,
            top: 154,
            width: 0,
            height: 0);

        Assert.Equal(Rectangle.Empty, bounds);
    }

    [Fact]
    public void ShouldFillHighlight_ReturnsFalseForMapViewport()
    {
        bool shouldFill = TutorialHighlightBoundsResolver.ShouldFillHighlight(TutorialHighlightTarget.MapViewport);

        Assert.False(shouldFill);
    }

    [Fact]
    public void ShouldFillHighlight_ReturnsTrueForPanelTargets()
    {
        bool shouldFill = TutorialHighlightBoundsResolver.ShouldFillHighlight(TutorialHighlightTarget.SelectionPanel);

        Assert.True(shouldFill);
    }

    [Fact]
    public void GetHighlightPadding_UsesLargerPaddingForMapTargets()
    {
        int padding = TutorialHighlightBoundsResolver.GetHighlightPadding(
            TutorialHighlightTarget.MapViewport,
            defaultPadding: 8,
            mapPadding: 14);

        Assert.Equal(14, padding);
        Assert.NotEqual(8, padding);
    }

    [Fact]
    public void GetHighlightPadding_KeepsDefaultPaddingForPanelTargets()
    {
        int padding = TutorialHighlightBoundsResolver.GetHighlightPadding(
            TutorialHighlightTarget.SelectionPanel,
            defaultPadding: 8,
            mapPadding: 14);

        Assert.Equal(8, padding);
    }

    [Fact]
    public void SelectBestTarget_ReturnsClickableObjectInsteadOfWholeViewport()
    {
        var viewport = new Rectangle(260, 92, 1540, 988);
        var clickableTarget = new Rectangle(1480, 760, 90, 90);

        var selected = TutorialMapHighlightResolver.SelectBestTarget(
            [clickableTarget],
            viewport,
            Rectangle.Empty);

        Assert.Equal(clickableTarget, selected);
        Assert.NotEqual(viewport, selected);
    }

    [Fact]
    public void SelectBestTarget_FiltersTargetsHiddenByTutorialWindow()
    {
        var viewport = new Rectangle(260, 92, 1540, 988);
        var tutorialWindow = new Rectangle(284, 154, 696, 858);
        var hiddenTarget = new Rectangle(910, 500, 80, 80);
        var visibleTarget = new Rectangle(1500, 780, 80, 80);

        var selected = TutorialMapHighlightResolver.SelectBestTarget(
            [hiddenTarget, visibleTarget],
            viewport,
            tutorialWindow);

        Assert.Equal(visibleTarget, selected);
        Assert.False(selected.Intersects(tutorialWindow));
    }

    [Fact]
    public void SelectBestTarget_ReturnsEmptyInsteadOfViewportWhenAllTargetsAreHidden()
    {
        var viewport = new Rectangle(260, 92, 1540, 988);
        var tutorialWindow = new Rectangle(284, 154, 696, 858);
        var hiddenTarget = new Rectangle(910, 500, 80, 80);

        var selected = TutorialMapHighlightResolver.SelectBestTarget(
            [hiddenTarget],
            viewport,
            tutorialWindow);

        Assert.Equal(Rectangle.Empty, selected);
    }

    [Fact]
    public void SelectBestTarget_ChoosesTargetNearestVisibleViewportCenter()
    {
        var viewport = new Rectangle(1000, 100, 800, 900);
        var farTarget = new Rectangle(1700, 900, 80, 80);
        var nearTarget = new Rectangle(1360, 520, 80, 80);

        var selected = TutorialMapHighlightResolver.SelectBestTarget(
            [farTarget, nearTarget],
            viewport,
            Rectangle.Empty);

        Assert.Equal(nearTarget, selected);
    }

    [Fact]
    public void SelectBestTarget_ClipsPartiallyVisibleTargetsToViewport()
    {
        var viewport = new Rectangle(1000, 100, 800, 900);
        var partiallyVisibleTarget = new Rectangle(980, 520, 80, 80);

        var selected = TutorialMapHighlightResolver.SelectBestTarget(
            [partiallyVisibleTarget],
            viewport,
            Rectangle.Empty);

        Assert.Equal(new Rectangle(1000, 520, 60, 80), selected);
    }

    [Fact]
    public void ToScreenBounds_TransformsWorldRadiusToScreenBounds()
    {
        Matrix worldToScreen = Matrix.CreateTranslation(new Vector3(100f, 50f, 0f));

        var bounds = TutorialMapHighlightResolver.ToScreenBounds(
            new Vector2(20f, 30f),
            worldRadius: 10f,
            worldToScreen,
            minimumScreenRadius: 1,
            maximumScreenRadius: 100);

        Assert.Equal(new Rectangle(110, 70, 20, 20), bounds);
    }

    [Fact]
    public void ToScreenBounds_ClampsSmallWorldRadiusToMinimumScreenRadius()
    {
        var bounds = TutorialMapHighlightResolver.ToScreenBounds(
            Vector2.Zero,
            worldRadius: 1f,
            Matrix.Identity,
            minimumScreenRadius: 18,
            maximumScreenRadius: 100);

        Assert.Equal(new Rectangle(-18, -18, 36, 36), bounds);
    }

    [Fact]
    public void ToScreenBounds_UsesReadableMinimumMapTargetRadius()
    {
        var bounds = TutorialMapHighlightResolver.ToScreenBounds(
            Vector2.Zero,
            worldRadius: 1f,
            Matrix.Identity,
            TutorialMapHighlightResolver.MinimumReadableScreenRadius,
            TutorialMapHighlightResolver.MaximumReadableScreenRadius);

        Assert.Equal(new Rectangle(-48, -48, 96, 96), bounds);
        Assert.True(bounds.Width > 36);
    }

    [Fact]
    public void ToScreenBounds_ClampsLargeWorldRadiusToMaximumScreenRadius()
    {
        var bounds = TutorialMapHighlightResolver.ToScreenBounds(
            Vector2.Zero,
            worldRadius: 400f,
            Matrix.Identity,
            minimumScreenRadius: 18,
            maximumScreenRadius: 110);

        Assert.Equal(new Rectangle(-110, -110, 220, 220), bounds);
    }

    [Fact]
    public void TutorialFooterHeight_PreservesButtonHeight()
    {
        int footerHeight = TutorialModeLayoutMetrics.GetFooterRowHeight(buttonHeight: 40, verticalPadding: 4);

        Assert.True(TutorialModeLayoutMetrics.HasFullButtonHeight(footerHeight, buttonHeight: 40));
        Assert.Equal(48, footerHeight);
    }

    [Fact]
    public void TutorialFooterHeight_RejectsCollapsedButtonRow()
    {
        Assert.False(TutorialModeLayoutMetrics.HasFullButtonHeight(footerRowHeight: 8, buttonHeight: 40));
    }

    [Fact]
    public void TryGetScreenBounds_UsesGlobalCoordinatesForNestedMyraWidgets()
    {
        var sidePanel = new Panel
        {
            Left = 1780,
            Top = 156,
            Width = 268,
            Height = 948
        };
        var selectionPanel = new Panel
        {
            Left = 12,
            Top = 84,
            Width = 244,
            Height = 250
        };
        sidePanel.Widgets.Add(selectionPanel);

        bool resolved = TutorialWidgetBoundsResolver.TryGetScreenBounds(selectionPanel, out Rectangle bounds);

        Assert.True(resolved);
        Assert.Equal(new Rectangle(1792, 240, 244, 250), bounds);
        Assert.NotEqual(new Rectangle(12, 84, 244, 250), bounds);
    }

    [Fact]
    public void TryGetScreenBounds_RejectsHiddenParentWidget()
    {
        var sidePanel = new Panel
        {
            Left = 1780,
            Top = 156,
            Width = 268,
            Height = 948,
            Visible = false
        };
        var selectionPanel = new Panel
        {
            Left = 12,
            Top = 84,
            Width = 244,
            Height = 250
        };
        sidePanel.Widgets.Add(selectionPanel);

        bool resolved = TutorialWidgetBoundsResolver.TryGetScreenBounds(selectionPanel, out _);

        Assert.False(resolved);
    }

    [Fact]
    public void GameUiVisualTree_ResolvesMyraAndXnaElementsInSharedScreenSpace()
    {
        var sidePanel = new Panel
        {
            Left = 1780,
            Top = 156,
            Width = 268,
            Height = 948
        };
        var selectionPanel = new Panel
        {
            Left = 12,
            Top = 84,
            Width = 244,
            Height = 250
        };
        sidePanel.Widgets.Add(selectionPanel);
        var tree = new GameUiVisualTree();

        tree.AddMyraElement(GameUiVisualElementIds.SelectionPanel, selectionPanel);
        tree.AddXnaElement(GameUiVisualElementIds.MapSelectionTarget, new Rectangle(1000, 520, 160, 160));
        IReadOnlyDictionary<string, Rectangle> resolved = tree.ResolveBounds();

        Assert.Equal(new Rectangle(1792, 240, 244, 250), resolved[GameUiVisualElementIds.SelectionPanel]);
        Assert.Equal(new Rectangle(1000, 520, 160, 160), resolved[GameUiVisualElementIds.MapSelectionTarget]);
        Assert.NotEqual(new Rectangle(12, 84, 244, 250), resolved[GameUiVisualElementIds.SelectionPanel]);
    }

    [Fact]
    public void ResolveVisualBounds_MapsTutorialTargetsThroughGameUiVisualTree()
    {
        var sidePanel = new Panel
        {
            Left = 1780,
            Top = 156,
            Width = 268,
            Height = 948
        };
        var selectionPanel = new Panel
        {
            Left = 12,
            Top = 84,
            Width = 244,
            Height = 250
        };
        sidePanel.Widgets.Add(selectionPanel);
        var tree = new GameUiVisualTree();
        tree.AddMyraElement(GameUiVisualElementIds.SelectionPanel, selectionPanel);
        tree.AddXnaElement(GameUiVisualElementIds.MapSelectionTarget, new Rectangle(1000, 520, 160, 160));

        var targets = new[]
        {
            TutorialHighlightTarget.MapViewport,
            TutorialHighlightTarget.SelectionPanel
        };
        IReadOnlyDictionary<TutorialHighlightTarget, Rectangle> resolved = TutorialHighlightTargets.ResolveVisualBounds(targets, tree);

        Assert.Equal(new Rectangle(1000, 520, 160, 160), resolved[TutorialHighlightTarget.MapViewport]);
        Assert.Equal(new Rectangle(1792, 240, 244, 250), resolved[TutorialHighlightTarget.SelectionPanel]);
        Assert.NotEqual(new Rectangle(12, 84, 244, 250), resolved[TutorialHighlightTarget.SelectionPanel]);
    }

    [Fact]
    public void ResolveVisualBounds_MapsEveryKnownTutorialTarget()
    {
        var tree = new GameUiVisualTree();
        tree.AddXnaElement(GameUiVisualElementIds.TopBar, new Rectangle(10, 20, 100, 30));
        tree.AddXnaElement(GameUiVisualElementIds.MapSelectionTarget, new Rectangle(300, 400, 160, 160));
        tree.AddXnaElement(GameUiVisualElementIds.HelpPanel, new Rectangle(700, 120, 300, 240));
        tree.AddXnaElement(GameUiVisualElementIds.PlayerDashboard, new Rectangle(500, 220, 260, 300));
        tree.AddXnaElement(GameUiVisualElementIds.SelectionPanel, new Rectangle(1700, 240, 280, 250));
        tree.AddXnaElement(GameUiVisualElementIds.EncyclopediaWindow, new Rectangle(80, 100, 520, 420));
        TutorialHighlightTarget[] targets = Enum.GetValues<TutorialHighlightTarget>();

        IReadOnlyDictionary<TutorialHighlightTarget, Rectangle> resolved = TutorialHighlightTargets.ResolveVisualBounds(targets, tree);

        Assert.Equal(targets.Length, resolved.Count);
        Assert.Equal(new Rectangle(10, 20, 100, 30), resolved[TutorialHighlightTarget.TopBar]);
        Assert.Equal(new Rectangle(10, 20, 100, 30), resolved[TutorialHighlightTarget.ResourceChips]);
        Assert.Equal(new Rectangle(300, 400, 160, 160), resolved[TutorialHighlightTarget.MapViewport]);
        Assert.Equal(new Rectangle(700, 120, 300, 240), resolved[TutorialHighlightTarget.HelpPanel]);
        Assert.Equal(new Rectangle(500, 220, 260, 300), resolved[TutorialHighlightTarget.PlayerDashboard]);
        Assert.Equal(new Rectangle(1700, 240, 280, 250), resolved[TutorialHighlightTarget.SelectionPanel]);
        Assert.Equal(new Rectangle(80, 100, 520, 420), resolved[TutorialHighlightTarget.EncyclopediaWindow]);
    }

    [Fact]
    public void ResolveVisualBounds_IgnoresUnknownTutorialTargets()
    {
        var tree = new GameUiVisualTree();
        tree.AddXnaElement(GameUiVisualElementIds.TopBar, new Rectangle(10, 20, 100, 30));
        var targets = new[] { (TutorialHighlightTarget)999 };

        IReadOnlyDictionary<TutorialHighlightTarget, Rectangle> resolved = TutorialHighlightTargets.ResolveVisualBounds(targets, tree);

        Assert.Empty(resolved);
    }

    [Fact]
    public void GameUiVisualTree_AuditsScaleAndDpiForMyraAndXnaElements()
    {
        var panel = new Panel
        {
            Left = 40,
            Top = 50,
            Width = 240,
            Height = 120
        };
        var tree = new GameUiVisualTree();
        tree.AddMyraElement("myra.panel", panel);
        tree.AddXnaElement("xna.panel", new Rectangle(400, 300, 160, 90), "XnaPanel");
        var scale = GameUiScaleContext.Create(
            backBufferWidth: 3840,
            backBufferHeight: 2160,
            clientWidth: 1920,
            clientHeight: 1080,
            uiScalePercent: 150,
            uiScaleFactor: 1.5f);

        GameUiAuditReport report = tree.CreateAuditReport(scale);

        Assert.Equal(2, report.Entries.Count);
        Assert.Equal(1, report.MyraCount);
        Assert.Equal(1, report.XnaCount);
        Assert.Equal(0, report.WarningCount);
        Assert.Equal(2f, report.Scale.DpiScaleX);
        Assert.Equal(2f, report.Scale.DpiScaleY);
        Assert.Contains("UI scale 150%", report.Summary);
        GameUiAuditEntry myraEntry = Assert.Single(report.Entries, entry => entry.Id == "myra.panel");
        Assert.Equal(GameUiVisualElementSource.Myra, myraEntry.Source);
        Assert.Equal(nameof(Panel), myraEntry.TypeName);
        Assert.True(myraEntry.Visible);
        Assert.True(myraEntry.TreeVisible);
        Assert.Equal(new Rectangle(40, 50, 240, 120), myraEntry.DeclaredBounds);
        Assert.Equal(new Rectangle(40, 50, 240, 120), myraEntry.ScreenBounds);
        Assert.Empty(myraEntry.Warnings);
        GameUiAuditEntry xnaEntry = Assert.Single(report.Entries, entry => entry.Id == "xna.panel");
        Assert.Equal(GameUiVisualElementSource.Xna, xnaEntry.Source);
        Assert.Equal("XnaPanel", xnaEntry.TypeName);
        Assert.True(xnaEntry.Visible);
        Assert.True(xnaEntry.TreeVisible);
        Assert.Equal(new Rectangle(400, 300, 160, 90), xnaEntry.DeclaredBounds);
        Assert.Equal(new Rectangle(400, 300, 160, 90), xnaEntry.LocalBounds);
        Assert.Equal(new Rectangle(400, 300, 160, 90), xnaEntry.ScreenBounds);
        Assert.All(report.Entries, entry =>
        {
            Assert.Equal(150, entry.UiScalePercent);
            Assert.Equal(1.5f, entry.UiScaleFactor);
            Assert.Equal(2f, entry.DpiScaleX);
            Assert.Equal(2f, entry.DpiScaleY);
            Assert.True(entry.HasValidScreenBounds);
        });
    }

    [Fact]
    public void GameUiVisualTree_AuditsHiddenAndInvalidElementsAsWarnings()
    {
        var hiddenParent = new Panel
        {
            Left = 40,
            Top = 50,
            Width = 240,
            Height = 120,
            Visible = false
        };
        var child = new Panel
        {
            Left = 12,
            Top = 16,
            Width = 80,
            Height = 40
        };
        hiddenParent.Widgets.Add(child);
        var tree = new GameUiVisualTree();
        tree.AddMyraElement("myra.hidden.child", child);
        tree.AddXnaElement("xna.invalid", Rectangle.Empty);
        var scale = GameUiScaleContext.Create(1920, 1080, 1920, 1080, 100, 1f);

        GameUiAuditReport report = tree.CreateAuditReport(scale);

        Assert.False(tree.TryResolveBounds("xna.invalid", out _));
        Assert.Equal(2, report.Entries.Count);
        Assert.Equal(2, report.WarningCount);
        Assert.Equal(1, report.HiddenCount);
        Assert.Equal(1, report.InvalidCount);
        GameUiAuditEntry hiddenEntry = Assert.Single(report.Entries, entry => entry.Id == "myra.hidden.child");
        GameUiAuditEntry invalidEntry = Assert.Single(report.Entries, entry => entry.Id == "xna.invalid");
        Assert.Contains("hidden", hiddenEntry.Warnings);
        Assert.True(hiddenEntry.HasValidScreenBounds);
        Assert.Contains("no resolved screen size", invalidEntry.Warnings);
        Assert.False(invalidEntry.HasValidScreenBounds);
    }

    [Fact]
    public void GameUiVisualTree_AuditsNestedMyraTreeWithoutDuplicatingNamedRoots()
    {
        var root = new Panel
        {
            Left = 100,
            Top = 200,
            Width = 300,
            Height = 220
        };
        var child = new Panel
        {
            Left = 20,
            Top = 30,
            Width = 80,
            Height = 40
        };
        root.Widgets.Add(child);
        var tree = new GameUiVisualTree();
        tree.AddMyraElement("named.root", root);

        tree.AddMyraTree("myra.desktop", new[] { root });
        GameUiAuditReport report = tree.CreateAuditReport(GameUiScaleContext.Create(1920, 1080, 1920, 1080, 100, 1f));

        Assert.Equal(2, report.Entries.Count);
        GameUiAuditEntry rootEntry = Assert.Single(report.Entries, entry => entry.Id == "named.root");
        GameUiAuditEntry childEntry = Assert.Single(
            report.Entries,
            entry => entry.Id.EndsWith(".Panel.0.Panel", StringComparison.Ordinal));
        Assert.Null(rootEntry.ParentId);
        Assert.Equal(0, rootEntry.Depth);
        Assert.Equal("named.root", childEntry.ParentId);
        Assert.Equal(1, childEntry.Depth);
        Assert.DoesNotContain(report.Entries, entry => entry.Id == "myra.desktop.0.Panel");
    }

    [Fact]
    public void GameUiVisualTree_AddsMyraDesktopRootSoTopLevelContainerCanBeInspected()
    {
        var root = new Panel
        {
            Left = 12,
            Top = 34,
            Width = 300,
            Height = 220
        };
        var tree = new GameUiVisualTree();

        tree.AddMyraRoot(GameUiVisualElementIds.MyraDesktop, new Rectangle(0, 0, 1920, 1080));
        tree.AddMyraTree(GameUiVisualElementIds.MyraDesktop, new[] { root });
        GameUiAuditReport report = tree.CreateAuditReport(GameUiScaleContext.Create(1920, 1080, 1920, 1080, 100, 1f));

        GameUiAuditEntry desktopEntry = Assert.Single(report.Entries, entry => entry.Id == GameUiVisualElementIds.MyraDesktop);
        Assert.Equal(GameUiVisualElementSource.Myra, desktopEntry.Source);
        Assert.Equal("Desktop", desktopEntry.TypeName);
        Assert.Equal(new Rectangle(0, 0, 1920, 1080), desktopEntry.ScreenBounds);
        GameUiAuditEntry rootEntry = Assert.Single(report.Entries, entry => entry.Id == "myra.desktop.0.Panel");
        Assert.Equal(GameUiVisualElementIds.MyraDesktop, rootEntry.ParentId);
        Assert.Equal(1, rootEntry.Depth);
    }

    [Fact]
    public void GameUiVisualTreeHierarchyValidator_AcceptsWellNestedMyraAndXnaElements()
    {
        var root = new Panel
        {
            Width = 500,
            Height = 400
        };
        var child = new Panel
        {
            Left = 20,
            Top = 30,
            Width = 100,
            Height = 80
        };
        root.Widgets.Add(child);
        var tree = new GameUiVisualTree();
        tree.AddXnaElement(GameUiVisualElementIds.BackBuffer, new Rectangle(0, 0, 1920, 1080), "BackBuffer");
        tree.AddXnaElement(GameUiVisualElementIds.MapViewport, new Rectangle(260, 92, 1200, 800), "MapViewport", GameUiVisualElementIds.BackBuffer);
        tree.AddMyraRoot(GameUiVisualElementIds.MyraDesktop, new Rectangle(0, 0, 1920, 1080));
        tree.AddMyraElement("hud.root", root, GameUiVisualElementIds.MyraDesktop);
        tree.AddMyraTree(GameUiVisualElementIds.MyraDesktop, new[] { root });
        GameUiAuditReport report = tree.CreateAuditReport(GameUiScaleContext.Create(1920, 1080, 1920, 1080, 100, 1f));

        GameUiHierarchyValidationReport validation = GameUiVisualTreeHierarchyValidator.Validate(report);

        Assert.True(validation.IsValid);
        Assert.Empty(validation.Errors);
    }

    [Fact]
    public void GameUiVisualTreeHierarchyValidator_RejectsMissingAndSelfParents()
    {
        var tree = new GameUiVisualTree();
        tree.AddXnaElement("xna.orphan", new Rectangle(0, 0, 10, 10), "Orphan", "xna.missing");
        tree.AddXnaElement("xna.self", new Rectangle(0, 0, 10, 10), "Self", "xna.self");
        GameUiAuditReport report = tree.CreateAuditReport(GameUiScaleContext.Create(1920, 1080, 1920, 1080, 100, 1f));

        GameUiHierarchyValidationReport validation = GameUiVisualTreeHierarchyValidator.Validate(report);

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Errors, issue => issue.ElementId == "xna.orphan" && issue.Message.Contains("missing", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(validation.Errors, issue => issue.ElementId == "xna.self" && issue.Message.Contains("own parent", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GameUiVisualTreeHierarchyValidator_RejectsDuplicateIdsAndBadDepth()
    {
        var report = new GameUiAuditReport(
            GameUiScaleContext.Create(1920, 1080, 1920, 1080, 100, 1f),
            new[]
            {
                CreateAuditEntry("root", null, 0, GameUiVisualElementSource.Xna, true, true, new Rectangle(0, 0, 500, 400)),
                CreateAuditEntry("dup", "root", 1, GameUiVisualElementSource.Xna, true, true, new Rectangle(10, 10, 80, 60)),
                CreateAuditEntry("dup", "root", 1, GameUiVisualElementSource.Xna, true, true, new Rectangle(20, 20, 80, 60)),
                CreateAuditEntry("shallow", "root", 0, GameUiVisualElementSource.Xna, true, true, new Rectangle(30, 30, 80, 60))
            });

        GameUiHierarchyValidationReport validation = GameUiVisualTreeHierarchyValidator.Validate(report);

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Errors, issue => issue.ElementId == "dup" && issue.Message.Contains("Duplicate", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(validation.Errors, issue => issue.ElementId == "shallow" && issue.Message.Contains("depth", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GameUiVisualTreeHierarchyValidator_WarnsWhenChildVisibilityOrBoundsContradictParent()
    {
        var report = new GameUiAuditReport(
            GameUiScaleContext.Create(1920, 1080, 1920, 1080, 100, 1f),
            new[]
            {
                CreateAuditEntry("myra.parent", null, 0, GameUiVisualElementSource.Myra, false, false, new Rectangle(0, 0, 100, 100)),
                CreateAuditEntry("myra.visibleChild", "myra.parent", 1, GameUiVisualElementSource.Myra, true, true, new Rectangle(10, 10, 20, 20)),
                CreateAuditEntry("myra.boundsParent", null, 0, GameUiVisualElementSource.Myra, true, true, new Rectangle(0, 0, 100, 100)),
                CreateAuditEntry("myra.outsideChild", "myra.boundsParent", 1, GameUiVisualElementSource.Myra, true, true, new Rectangle(90, 90, 40, 40))
            });

        GameUiHierarchyValidationReport validation = GameUiVisualTreeHierarchyValidator.Validate(report);

        Assert.True(validation.IsValid);
        Assert.Contains(validation.Warnings, issue => issue.ElementId == "myra.parent" && issue.Message.Contains("not nested", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(validation.Warnings, issue => issue.ElementId == "myra.visibleChild" && issue.Message.Contains("parent is hidden", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(validation.Warnings, issue => issue.ElementId == "myra.outsideChild" && issue.Message.Contains("outside parent", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GameUiVisualTree_RejectsDuplicateMyraAliasesForSameWidget()
    {
        var topBar = new Panel
        {
            Left = 0,
            Top = 0,
            Width = 1506,
            Height = 65
        };
        var tree = new GameUiVisualTree();

        tree.AddMyraElement(GameUiVisualElementIds.TopBar, topBar);
        tree.AddMyraElement(GameUiVisualElementIds.ResourceChips, topBar);
        GameUiAuditReport report = tree.CreateAuditReport(GameUiScaleContext.Create(2048, 1152, 2048, 1152, 100, 1f));

        GameUiAuditEntry entry = Assert.Single(report.Entries);
        Assert.Equal(GameUiVisualElementIds.TopBar, entry.Id);
        Assert.Equal(new Rectangle(0, 0, 1506, 65), entry.ScreenBounds);
        Assert.DoesNotContain(report.Entries, item => item.Id == GameUiVisualElementIds.ResourceChips);
    }

    [Fact]
    public void GameUiLayoutMetrics_UsesMeasuredTopBarBottomInsteadOfFallbackHeight()
    {
        var measuredTopBar = new Rectangle(0, 0, 2048, 128);

        int contentTop = GameUiLayoutMetrics.ResolveContentTop(
            measuredTopBar,
            fallbackHeight: 80,
            gap: 12);

        Assert.Equal(140, contentTop);
        Assert.NotEqual(92, contentTop);
    }

    [Fact]
    public void GameUiLayoutMetrics_FallsBackWhenTopBarHasNoResolvedSize()
    {
        int contentTop = GameUiLayoutMetrics.ResolveContentTop(
            Rectangle.Empty,
            fallbackHeight: 80,
            gap: 12);

        Assert.Equal(92, contentTop);
    }

    [Fact]
    public void GameUiVisualTreeInspector_BuildsSelectableRowsWithBoundsAndWarnings()
    {
        var tree = new GameUiVisualTree();
        tree.AddXnaElement(GameUiVisualElementIds.BackBuffer, new Rectangle(0, 0, 2048, 1152), "BackBuffer");
        tree.AddXnaElement(GameUiVisualElementIds.TopBar, new Rectangle(0, 0, 2048, 128), "TopBar", GameUiVisualElementIds.BackBuffer);
        tree.AddXnaElement(GameUiVisualElementIds.MapViewport, new Rectangle(268, 128, 1512, 976), "MapViewport", GameUiVisualElementIds.BackBuffer);
        tree.AddXnaElement("xna.invalid", Rectangle.Empty, "Invalid");
        GameUiAuditReport report = tree.CreateAuditReport(GameUiScaleContext.Create(2048, 1152, 2048, 1152, 100, 1f));

        IReadOnlyList<GameUiVisualTreeRow> rows = GameUiVisualTreeInspector.BuildRows(report, GameUiVisualElementIds.TopBar);

        GameUiVisualTreeRow selected = Assert.Single(rows, row => row.Id == GameUiVisualElementIds.TopBar);
        Assert.True(selected.IsSelected);
        Assert.Contains(GameUiVisualElementIds.TopBar, selected.DisplayText);
        Assert.Equal("2048x128 @ 0,0", selected.BoundsText);
        Assert.Equal(1, selected.Depth);
        GameUiVisualTreeRow invalid = Assert.Single(rows, row => row.Id == "xna.invalid");
        Assert.True(invalid.HasWarnings);
        Assert.False(invalid.HasValidScreenBounds);
    }

    [Fact]
    public void GameUiVisualTreeInspector_ResolvesSelectedBoundsAndRejectsBadSelections()
    {
        var tree = new GameUiVisualTree();
        tree.AddXnaElement(GameUiVisualElementIds.TopBar, new Rectangle(0, 0, 2048, 128), "TopBar");
        tree.AddXnaElement("xna.invalid", Rectangle.Empty, "Invalid");
        GameUiAuditReport report = tree.CreateAuditReport(GameUiScaleContext.Create(2048, 1152, 2048, 1152, 100, 1f));

        bool resolved = GameUiVisualTreeInspector.TryResolveSelectedBounds(report, GameUiVisualElementIds.TopBar, out Rectangle bounds);
        bool invalidResolved = GameUiVisualTreeInspector.TryResolveSelectedBounds(report, "xna.invalid", out _);
        bool missingResolved = GameUiVisualTreeInspector.TryResolveSelectedBounds(report, "xna.missing", out _);

        Assert.True(resolved);
        Assert.Equal(new Rectangle(0, 0, 2048, 128), bounds);
        Assert.False(invalidResolved);
        Assert.False(missingResolved);
    }

    [Fact]
    public void GameUiVisualTreeInspector_FormatsSelectedElementInspectionDetails()
    {
        var tree = new GameUiVisualTree();
        tree.AddXnaElement(GameUiVisualElementIds.TopBar, new Rectangle(0, 0, 2048, 128), "TopBar");
        GameUiAuditReport report = tree.CreateAuditReport(GameUiScaleContext.Create(2048, 1152, 2048, 1152, 100, 1f));

        string selectedDetails = GameUiVisualTreeInspector.FormatSelectionDetails(report, GameUiVisualElementIds.TopBar);
        string missingDetails = GameUiVisualTreeInspector.FormatSelectionDetails(report, "xna.missing");
        string noneDetails = GameUiVisualTreeInspector.FormatSelectionDetails(report, null);

        Assert.Contains($"Selected: {GameUiVisualElementIds.TopBar}", selectedDetails);
        Assert.Contains("Source: Xna TopBar", selectedDetails);
        Assert.Contains("Screen: 2048x128 @ 0,0", selectedDetails);
        Assert.Contains("not in current visual tree", missingDetails);
        Assert.Equal("Selected: none", noneDetails);
    }

    [Fact]
    public void DebugWindowContentLayout_DetectsClippedInspectorContent()
    {
        bool clipped = DebugWindowContentLayout.WouldClipWithoutScroll(
            contentHeight: 820,
            viewportHeight: 520);

        Assert.True(clipped);
    }

    [Fact]
    public void DebugWindowContentLayout_DoesNotFlagContentThatFits()
    {
        bool clipped = DebugWindowContentLayout.WouldClipWithoutScroll(
            contentHeight: 420,
            viewportHeight: 520);

        Assert.False(clipped);
    }

    [Fact]
    public void DebugUiAuditText_FormatsActualSizesAndDpiScaleForVisibleAudit()
    {
        var tree = new GameUiVisualTree();
        tree.AddXnaElement(GameUiVisualElementIds.BackBuffer, new Rectangle(0, 0, 3840, 2160), "BackBuffer");
        tree.AddXnaElement(GameUiVisualElementIds.MapViewport, new Rectangle(260, 92, 1540, 988), "MapViewport");
        GameUiAuditReport report = tree.CreateAuditReport(GameUiScaleContext.Create(3840, 2160, 1920, 1080, 150, 1.5f));

        string summary = DebugUiAuditText.FormatSummary(report);
        string scale = DebugUiAuditText.FormatScale(report.Scale);
        string warnings = DebugUiAuditText.FormatWarnings(report);
        string details = DebugUiAuditText.FormatDetails(report);

        Assert.Equal("Elements: 2 (0 Myra, 2 XNA)", summary);
        Assert.Contains("DPI 2.00x2.00", scale);
        Assert.Contains("BB 3840x2160", scale);
        Assert.Equal("Warnings: 0, hidden: 0, invalid bounds: 0", warnings);
        Assert.Contains("xna.mapViewport: Xna 1540x988 @ 260,92", details);
    }

    [Fact]
    public void DebugUiAuditText_PrioritizesBadBoundsOverHealthyElements()
    {
        var tree = new GameUiVisualTree();
        tree.AddXnaElement("z.healthy", new Rectangle(10, 20, 30, 40), "Healthy");
        tree.AddXnaElement("a.bad", Rectangle.Empty, "Bad");
        GameUiAuditReport report = tree.CreateAuditReport(GameUiScaleContext.Create(1920, 1080, 1920, 1080, 100, 1f));

        string details = DebugUiAuditText.FormatDetails(report, maxEntries: 1);

        Assert.Contains("a.bad", details);
        Assert.Contains("0x0", details);
        Assert.Contains("no resolved screen size", details);
        Assert.DoesNotContain("z.healthy", details);
    }

    private static GameUiAuditEntry CreateAuditEntry(
        string id,
        string? parentId,
        int depth,
        GameUiVisualElementSource source,
        bool visible,
        bool treeVisible,
        Rectangle bounds)
    {
        return new GameUiAuditEntry(
            id,
            parentId,
            depth,
            source,
            source.ToString(),
            visible,
            treeVisible,
            bounds,
            bounds,
            bounds,
            100,
            1f,
            1f,
            1f,
            []);
    }

}
