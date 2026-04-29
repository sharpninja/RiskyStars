using Microsoft.Xna.Framework;
using RiskyStars.Client;

namespace RiskyStars.Tests;

public class TutorialHighlightTargetsTests
{
    public static TheoryData<TutorialStepCompletion, TutorialHighlightTarget[]> CompletionTargets =>
        new()
        {
            { TutorialStepCompletion.WorldSynced, [TutorialHighlightTarget.TopBar] },
            { TutorialStepCompletion.OwnTurn, [TutorialHighlightTarget.TopBar, TutorialHighlightTarget.ResourceChips] },
            { TutorialStepCompletion.AnySelection, [TutorialHighlightTarget.MapViewport, TutorialHighlightTarget.SelectionPanel] },
            { TutorialStepCompletion.HelpOpen, [TutorialHighlightTarget.TopBar, TutorialHighlightTarget.HelpPanel] },
            { TutorialStepCompletion.PurchasePhase, [TutorialHighlightTarget.TopBar, TutorialHighlightTarget.ResourceChips] },
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
    public void ForCompletion_OwnTurnHighlightsTopBarAndResources()
    {
        var targets = TutorialHighlightTargets.ForCompletion(TutorialStepCompletion.OwnTurn);

        Assert.Contains(TutorialHighlightTarget.TopBar, targets);
        Assert.Contains(TutorialHighlightTarget.ResourceChips, targets);
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

}
