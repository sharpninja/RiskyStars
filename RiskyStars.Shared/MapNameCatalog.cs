namespace RiskyStars.Shared;

public static class MapNameCatalog
{
    public static IReadOnlyList<string> StarNames { get; } =
    [
        "Sirius",
        "Canopus",
        "Arcturus",
        "Vega",
        "Capella",
        "Rigel",
        "Procyon",
        "Achernar",
        "Betelgeuse",
        "Hadar",
        "Altair",
        "Acrux",
        "Aldebaran",
        "Spica",
        "Antares",
        "Pollux",
        "Fomalhaut",
        "Deneb",
        "Regulus",
        "Adhara",
        "Castor",
        "Gacrux",
        "Bellatrix",
        "Elnath",
        "Miaplacidus",
        "Alnilam",
        "Alnair",
        "Alioth",
        "Dubhe",
        "Mirfak",
        "Wezen",
        "Sargas",
        "Kaus Australis",
        "Avior",
        "Alkaid",
        "Menkalinan",
        "Atria",
        "Alhena",
        "Peacock",
        "Alsephina",
        "Mirzam",
        "Hamal",
        "Polaris",
        "Algol",
        "Almach",
        "Caph",
        "Schedar",
        "Saiph",
        "Nunki",
        "Alpheratz",
        "Alderamin",
        "Markab",
        "Enif",
        "Scheat",
        "Sabik",
        "Rasalhague",
        "Cebalrai",
        "Unukalhai",
        "Kochab",
        "Alphecca",
        "Mintaka",
        "Merak",
        "Phecda",
        "Megrez",
        "Mizar",
        "Merga",
        "Thuban",
        "Izar",
        "Vindemiatrix",
        "Zubenelgenubi",
        "Zubeneschamali",
        "Shaula",
        "Lesath",
        "Kaus Media",
        "Kaus Borealis",
        "Algedi",
        "Dabih",
        "Nashira",
        "Sadalsuud",
        "Sadalbari",
        "Skat",
        "Matar",
        "Alrescha"
    ];

    public static IReadOnlyList<string> FictionalPlaceNames { get; } =
    [
        "Arrakeen",
        "Sietch Tabr",
        "Carthag",
        "Caladan",
        "Trantor",
        "Terminus",
        "Helicon",
        "Anacreon",
        "Smyrno",
        "Mos Espa",
        "Anchorhead",
        "Jundland",
        "Theed",
        "Otoh Gunga",
        "Coronet",
        "Minas Tirith",
        "Gondor",
        "Rohan",
        "Lothlorien",
        "Rivendell",
        "Mordor",
        "The Shire",
        "Cair Paravel",
        "Lantern Waste",
        "Archenland",
        "Benden",
        "Ruatha",
        "Fort Hold",
        "Gont",
        "Roke",
        "Atuan",
        "Winterfell",
        "Riverrun",
        "Highgarden",
        "Storms End",
        "Sunspear",
        "Kings Landing",
        "Dragonstone",
        "Shadizar",
        "Aquilonia",
        "Barsoom",
        "Helium",
        "Thark",
        "Pellucidar",
        "Amber",
        "Rebma",
        "Lankhmar",
        "Rillanon",
        "Krondor",
        "Midkemia",
        "Pern",
        "Earthsea",
        "Numenor",
        "Valinor",
        "Gondolin",
        "Doriath",
        "Nargothrond",
        "Hyrule",
        "Kakariko",
        "Gerudo",
        "Rapture",
        "Columbia",
        "City 17",
        "Black Mesa",
        "Night City",
        "New Crobuzon",
        "Bas Lag",
        "Ankh-Morpork",
        "Lancre",
        "Omnia",
        "Cybertron",
        "Iacon",
        "Kaon",
        "Vulcan",
        "ShiKahr",
        "Risa",
        "Bajor",
        "Cardassia",
        "Romulus",
        "Remus",
        "Nimbus III",
        "Gallifrey",
        "Skaro",
        "Mondas",
        "Serenity Valley",
        "Persephone",
        "Londinium",
        "Ariel"
    ];

    public static string GetStarName(int zeroBasedSystemIndex)
    {
        if (zeroBasedSystemIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(zeroBasedSystemIndex), "System index must not be negative.");
        }

        return GetRepeatedName(StarNames, zeroBasedSystemIndex);
    }

    public static string GetStellarBodyName(string hostStarName, int zeroBasedBodyIndex)
    {
        if (string.IsNullOrWhiteSpace(hostStarName))
        {
            throw new ArgumentException("Host star name is required.", nameof(hostStarName));
        }

        if (zeroBasedBodyIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(zeroBasedBodyIndex), "Body index must not be negative.");
        }

        return $"{hostStarName.Trim()} {GetBodyDesignation(zeroBasedBodyIndex)}";
    }

    public static string GetContinentName(string stellarBodyName, int zeroBasedContinentIndex)
    {
        if (string.IsNullOrWhiteSpace(stellarBodyName))
        {
            throw new ArgumentException("Stellar body name is required.", nameof(stellarBodyName));
        }

        if (zeroBasedContinentIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(zeroBasedContinentIndex), "Continent index must not be negative.");
        }

        var offset = GetStableOffset(stellarBodyName);
        return GetRepeatedName(FictionalPlaceNames, offset + zeroBasedContinentIndex);
    }

    private static string GetBodyDesignation(int zeroBasedBodyIndex)
    {
        // IAU-style exoplanet designations reserve the host star as "a", so bodies start at "b".
        return ToLowercaseSequence(zeroBasedBodyIndex + 1);
    }

    private static string ToLowercaseSequence(int sequenceIndex)
    {
        var letters = new Stack<char>();

        while (sequenceIndex >= 0)
        {
            letters.Push((char)('a' + sequenceIndex % 26));
            sequenceIndex = (sequenceIndex / 26) - 1;
        }

        return new string(letters.ToArray());
    }

    private static int GetStableOffset(string value)
    {
        unchecked
        {
            var hash = 17;
            foreach (var character in value.Trim().ToUpperInvariant())
            {
                hash = (hash * 31) + character;
            }

            return Math.Abs(hash) % FictionalPlaceNames.Count;
        }
    }

    private static string GetRepeatedName(IReadOnlyList<string> names, int zeroBasedIndex)
    {
        var cycle = zeroBasedIndex / names.Count;
        var name = names[zeroBasedIndex % names.Count];

        return cycle == 0 ? name : $"{name} {cycle + 1}";
    }
}
