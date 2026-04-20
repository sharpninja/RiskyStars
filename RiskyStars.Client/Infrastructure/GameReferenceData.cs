using Microsoft.Xna.Framework;
using RiskyStars.Shared;

namespace RiskyStars.Client;

public sealed record EncyclopediaArticle(
    string Id,
    string Category,
    string Title,
    string Summary,
    IReadOnlyList<string> KeyPoints,
    IReadOnlyList<string> Commands);

public sealed record TutorialLesson(
    string Id,
    string Category,
    string Title,
    string Summary,
    IReadOnlyList<string> Steps,
    IReadOnlyList<string> Tips);

public sealed record TutorialContext(
    string Title,
    string Summary,
    string Focus,
    IReadOnlyList<string> NextSteps,
    string RecommendedLessonId);

public static class GameReferenceData
{
    public static IReadOnlyList<EncyclopediaArticle> EncyclopediaArticles { get; } =
    [
        new EncyclopediaArticle(
            "turn-flow",
            "Core Rules",
            "Turn Flow",
            "Every turn runs through production, purchase, reinforcement, and movement in that order.",
            [
                "Production adds population, metal, and fuel to each player stockpile.",
                "Purchase turns resources into new armies before they hit the map.",
                "Reinforcement places purchased strength into owned regions or lane mouths.",
                "Movement repositions armies and often triggers combat."
            ],
            ["Space advances phase", "P resolves production", "B / 1 / 5 / 0 buy armies", "R reinforces the selected location"]),
        new EncyclopediaArticle(
            "resources",
            "Core Rules",
            "Resources",
            "Population, metal, and fuel drive the whole campaign economy.",
            [
                "Population is the manpower pool used to field armies.",
                "Metal supports construction and force expansion.",
                "Fuel enables projection and sustained operations.",
                "The top bar shows stockpiles and per-turn deltas at all times."
            ],
            ["F2 opens the command dashboard", "F4 opens UI scale"]),
        new EncyclopediaArticle(
            "armies",
            "Core Rules",
            "Armies",
            "Armies are your mobile force packages. They can reinforce, move, split, merge, and fight.",
            [
                "Select an army with left click to inspect it.",
                "Right click opens contextual commands for the selected force.",
                "Armies that moved this turn are marked as spent until the next turn.",
                "Army ownership, unit count, and location appear in the selection panel."
            ],
            ["Left click selects", "Right click opens context menu", "Tab cycles armies", "C centers on selection"]),
        new EncyclopediaArticle(
            "regions",
            "Map Elements",
            "Regions",
            "Regions are the controllable locations on stellar bodies.",
            [
                "Owning regions matters for territory control and reinforcement targets.",
                "Region markers sit on planets and can be selected directly.",
                "The selection panel shows region owner and basic identity.",
                "Context actions on owned regions drive reinforcement and local commands."
            ],
            ["Left click region", "Right click region for commands"]),
        new EncyclopediaArticle(
            "lane-mouths",
            "Map Elements",
            "Hyperspace Lane Mouths",
            "Lane mouths are strategic chokepoints between star systems.",
            [
                "They connect systems and shape movement lanes across the map.",
                "They can be owned and reinforced like other strategic positions.",
                "Lane mouths are rendered away from bodies to keep routes readable.",
                "Capturing mouths improves control over map flow."
            ],
            ["Map key explains lane mouths", "Right click lane mouth for commands"]),
        new EncyclopediaArticle(
            "combat",
            "Combat",
            "Combat",
            "Combat resolves through attacker and defender rolls, pairings, and casualties.",
            [
                "When combat is active, a dedicated Myra combat overlay takes over the workspace UI.",
                "The combat overlay shows forces, rolls, pairings, casualties, and survivors.",
                "Rounds continue until the event stream declares the battle complete.",
                "Strategic turn flow resumes once combat closes."
            ],
            ["Enter / Space advances combat presentation", "Esc skips current combat presentation"]),
        new EncyclopediaArticle(
            "controls",
            "Controls",
            "Controls",
            "The map and command deck are driven by mouse-first controls with keyboard shortcuts for speed.",
            [
                "Right-mouse drag pans the map.",
                "Mouse wheel zooms around the cursor anchor.",
                "Right-click release without drag opens the context menu.",
                "F1-F6 control the main in-game panels."
            ],
            ["F1 debug", "F2 dashboard", "F3 AI", "F4 scale", "F5 encyclopedia", "F6 tutorial", "H help"]),
        new EncyclopediaArticle(
            "panels",
            "Controls",
            "Panels",
            "All in-game workspace UI panels are Myra windows styled to match gameplay chrome.",
            [
                "Panels are dockable, resizable, and saved in window preferences.",
                "The top bar always shows which core panels are open.",
                "If a panel closes, use the hotkey to bring it back immediately.",
                "The map/world rendering remains separate from the Myra workspace UI."
            ],
            ["Window state persists automatically", "Use hotkeys when panels are lost"])
    ];

    public static IReadOnlyList<TutorialLesson> TutorialLessons { get; } =
    [
        new TutorialLesson(
            "first-turn",
            "Getting Started",
            "First Turn",
            "Use this when you first drop into a live map and need the command order fast.",
            [
                "Read the top bar to confirm whose turn it is.",
                "Check stockpiles and production deltas.",
                "Produce or purchase if the phase allows it.",
                "Select a force or position and issue commands from the context menu."
            ],
            ["If nothing is selected, left click a unit or location.", "Use H for the shortcut sheet."]),
        new TutorialLesson(
            "economy",
            "Getting Started",
            "Economy Loop",
            "Resources convert into fielded strength through purchase and reinforcement.",
            [
                "Resolve production to collect the turn income.",
                "Open the dashboard with F2 to buy armies.",
                "Watch population, metal, and fuel together before overbuying.",
                "Reinforce owned positions before moving into danger."
            ],
            ["The top bar already shows per-turn deltas.", "Dashboard is the fastest place to buy."]),
        new TutorialLesson(
            "reinforcement",
            "Turn Play",
            "Reinforcement",
            "Reinforcement turns reserves into map presence.",
            [
                "Select an owned region or lane mouth.",
                "Use the reinforce action from the context menu or shortcut.",
                "Concentrate strength where lanes converge.",
                "Finish reinforcement before shifting to movement."
            ],
            ["Lane mouths are strong reinforcement anchors.", "Selection panel confirms what is currently targeted."]),
        new TutorialLesson(
            "movement",
            "Turn Play",
            "Movement",
            "Movement is about pressure, positioning, and forcing profitable combat.",
            [
                "Select an army you own.",
                "Pan and zoom to inspect route options.",
                "Open the context menu on a valid destination.",
                "Keep enough force behind to hold critical regions and lane mouths."
            ],
            ["Right-mouse drag pans.", "C recenters on the current selection."]),
        new TutorialLesson(
            "combat",
            "Turn Play",
            "Combat Readout",
            "Combat now uses the same Myra workspace style as the rest of the game UI.",
            [
                "Watch attacker and defender forces at the top of the combat overlay.",
                "Read roll columns and pairings before reacting to casualties.",
                "Use Enter or Space to advance each stage.",
                "Check survivors before returning to the strategic map."
            ],
            ["Esc skips the presentation if you only need the result.", "The top bar still reports overall status."]),
        new TutorialLesson(
            "workspace",
            "Workspace",
            "Panels and Reference",
            "The workspace is designed to be recoverable at all times.",
            [
                "Use F1-F6 to reopen key panels instantly.",
                "Open the encyclopedia when you need static rules and system explanations.",
                "Open the tutorial when you need context-sensitive guidance.",
                "Use the help overlay for the compact shortcut map."
            ],
            ["If a panel is missing, the hotkey is faster than hunting for it.", "Window placement persists automatically."])
    ];

    public static TutorialContext BuildTutorialContext(
        GameStateCache? gameStateCache,
        string? currentPlayerId,
        SelectionState? selection,
        bool combatActive)
    {
        if (combatActive)
        {
            return new TutorialContext(
                "Combat underway",
                "The battle overlay is live. Resolve it before strategic orders continue.",
                "Current focus: Combat presentation",
                [
                    "Read attacker and defender forces first.",
                    "Advance stages with Enter or Space.",
                    "Use Esc only if you want to skip the presentation."
                ],
                "combat");
        }

        if (gameStateCache == null || gameStateCache.GetLastUpdateTimestamp() <= 0)
        {
            return new TutorialContext(
                "Waiting for world sync",
                "The game stream has not delivered the first full world snapshot yet.",
                "Current focus: Connection startup",
                [
                    "Watch the top status bar for stream state.",
                    "Wait for the first turn/phase update before issuing orders.",
                    "If sync stalls, inspect the status or debug panels."
                ],
                "first-turn");
        }

        string? activePlayerId = gameStateCache.GetCurrentPlayerId();
        TurnPhase phase = gameStateCache.GetCurrentPhase();

        if (string.IsNullOrWhiteSpace(currentPlayerId) || activePlayerId != currentPlayerId)
        {
            string activeName = string.IsNullOrWhiteSpace(activePlayerId)
                ? "another commander"
                : gameStateCache.GetPlayerState(activePlayerId)?.PlayerName ?? "another commander";

            return new TutorialContext(
                "Observe while waiting",
                $"{activeName} is resolving the current phase. Use the downtime to review map state and plan the next turn.",
                BuildSelectionFocus(selection),
                [
                    "Pan the map and inspect fronts.",
                    "Open the encyclopedia for rules refreshers.",
                    "Use the AI panel if you want to watch opposing actions."
                ],
                "workspace");
        }

        return phase switch
        {
            TurnPhase.Production => new TutorialContext(
                "Production phase",
                "Confirm what the empire generated before spending anything.",
                BuildSelectionFocus(selection),
                [
                    "Read the resource deltas on the top bar.",
                    "Resolve production when ready.",
                    "Plan purchases before ending the phase."
                ],
                "economy"),
            TurnPhase.Purchase => new TutorialContext(
                "Purchase phase",
                "Turn stockpiles into armies you can reinforce onto the map.",
                BuildSelectionFocus(selection),
                [
                    "Open F2 dashboard if you need fast buy controls.",
                    "Balance population, metal, and fuel before buying deep.",
                    "Think about reinforcement targets before committing."
                ],
                "economy"),
            TurnPhase.Reinforcement => new TutorialContext(
                "Reinforcement phase",
                "Place new strength where it changes the next movement and combat cycle.",
                BuildSelectionFocus(selection),
                [
                    "Select an owned region or lane mouth.",
                    "Reinforce the selected location.",
                    "Favor chokepoints and threatened systems."
                ],
                "reinforcement"),
            TurnPhase.Movement => new TutorialContext(
                "Movement phase",
                "Reposition armies, pressure borders, and set up favorable fights.",
                BuildSelectionFocus(selection),
                [
                    "Select an army you own.",
                    "Pan/zoom to the destination.",
                    "Issue movement from the context menu."
                ],
                "movement"),
            _ => new TutorialContext(
                "Command guidance",
                "Use the current phase and selection to decide the next meaningful order.",
                BuildSelectionFocus(selection),
                [
                    "Check the top bar for the active phase.",
                    "Select a relevant force or location.",
                    "Issue the next command from the context menu."
                ],
                "first-turn")
        };
    }

    private static string BuildSelectionFocus(SelectionState? selection)
    {
        if (selection == null || selection.Type == SelectionType.None)
        {
            return "Current focus: Nothing selected";
        }

        return selection.Type switch
        {
            SelectionType.Army when selection.SelectedArmy != null => $"Current focus: Army at {selection.SelectedArmy.LocationId}",
            SelectionType.Region when selection.SelectedRegion != null => $"Current focus: Region {selection.SelectedRegion.Name}",
            SelectionType.HyperspaceLaneMouth when !string.IsNullOrWhiteSpace(selection.SelectedHyperspaceLaneMouthId) => $"Current focus: Lane mouth {selection.SelectedHyperspaceLaneMouthId}",
            SelectionType.StellarBody when selection.SelectedStellarBody != null => $"Current focus: Stellar body {selection.SelectedStellarBody.Name}",
            SelectionType.StarSystem when selection.SelectedStarSystem != null => $"Current focus: Star system {selection.SelectedStarSystem.Name}",
            _ => "Current focus: Active selection"
        };
    }
}
