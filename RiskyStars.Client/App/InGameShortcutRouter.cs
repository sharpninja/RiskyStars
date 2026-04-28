using Microsoft.Xna.Framework.Input;

namespace RiskyStars.Client;

public enum InGamePanelToggle
{
    DebugInfo,
    CommandDashboard,
    AiVisualization,
    UiScale,
    Encyclopedia,
    GuidedTutorial
}

public static class InGameShortcutRouter
{
    public static IReadOnlyList<(Keys Key, InGamePanelToggle Panel)> PanelToggles { get; } =
    [
        (Keys.F1, InGamePanelToggle.DebugInfo),
        (Keys.F2, InGamePanelToggle.CommandDashboard),
        (Keys.F3, InGamePanelToggle.AiVisualization),
        (Keys.F4, InGamePanelToggle.UiScale),
        (Keys.F5, InGamePanelToggle.Encyclopedia),
        (Keys.F6, InGamePanelToggle.GuidedTutorial)
    ];

    public static InGamePanelToggle? GetPanelToggle(Keys key)
    {
        foreach (var toggle in PanelToggles)
        {
            if (toggle.Key == key)
            {
                return toggle.Panel;
            }
        }

        return null;
    }
}
