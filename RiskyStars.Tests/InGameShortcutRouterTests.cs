using Microsoft.Xna.Framework.Input;
using RiskyStars.Client;

namespace RiskyStars.Tests;

public class InGameShortcutRouterTests
{
    [Fact]
    public void F6_TogglesGuidedTutorialPanel()
    {
        var panel = InGameShortcutRouter.GetPanelToggle(Keys.F6);

        Assert.Equal(InGamePanelToggle.GuidedTutorial, panel);
        Assert.DoesNotContain(
            InGameShortcutRouter.PanelToggles,
            toggle => toggle.Key == Keys.F6 && toggle.Panel != InGamePanelToggle.GuidedTutorial);
    }

    [Fact]
    public void LegacyTutorialWindow_IsRemovedFromClientAssembly()
    {
        var legacyTutorialType = typeof(RiskyStarsGame).Assembly.GetType("RiskyStars.Client.TutorialWindow");

        Assert.Null(legacyTutorialType);
    }
}
