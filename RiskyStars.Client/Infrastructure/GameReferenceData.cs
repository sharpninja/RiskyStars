namespace RiskyStars.Client;

public sealed record EncyclopediaArticle(
    string Id,
    string Category,
    string Title,
    string Summary,
    IReadOnlyList<string> KeyPoints,
    IReadOnlyList<string> Commands);

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
            ["F1 debug", "F2 dashboard", "F3 AI", "F4 scale", "F5 encyclopedia", "F6 guided tutorial", "H help"]),
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
}
