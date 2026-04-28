using RiskyStars.Shared;

namespace RiskyStars.Tests;

public class MapNameCatalogTests
{
    [Fact]
    public void GetStarName_UsesRealStarCatalogInsteadOfPlaceholders()
    {
        Assert.Equal("Sirius", MapNameCatalog.GetStarName(0));
        Assert.Equal("Canopus", MapNameCatalog.GetStarName(1));
        Assert.DoesNotContain("System", MapNameCatalog.GetStarName(0), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetStarName_RejectsNegativeIndex()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MapNameCatalog.GetStarName(-1));
    }

    [Theory]
    [InlineData(0, "Sirius b")]
    [InlineData(1, "Sirius c")]
    [InlineData(24, "Sirius z")]
    [InlineData(25, "Sirius aa")]
    public void GetStellarBodyName_UsesIauStyleLowercaseDesignationsBeginningAtB(int bodyIndex, string expectedName)
    {
        Assert.Equal(expectedName, MapNameCatalog.GetStellarBodyName("Sirius", bodyIndex));
    }

    [Fact]
    public void GetStellarBodyName_TrimsHostStarName()
    {
        Assert.Equal("Vega b", MapNameCatalog.GetStellarBodyName(" Vega ", 0));
    }

    [Fact]
    public void GetStellarBodyName_RejectsBlankHostStarName()
    {
        Assert.Throws<ArgumentException>(() => MapNameCatalog.GetStellarBodyName(" ", 0));
    }

    [Fact]
    public void GetStellarBodyName_RejectsNegativeBodyIndex()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MapNameCatalog.GetStellarBodyName("Sirius", -1));
    }

    [Fact]
    public void GetContinentName_UsesFictionalPlaceCatalogInsteadOfContinentPlaceholder()
    {
        var continentName = MapNameCatalog.GetContinentName("Sirius b", 0);

        Assert.Contains(continentName, MapNameCatalog.FictionalPlaceNames);
        Assert.DoesNotMatch(@"^Continent \d+$", continentName);
    }

    [Fact]
    public void GetContinentName_IsStableForSameBodyAndIndex()
    {
        var firstName = MapNameCatalog.GetContinentName("Canopus c", 2);
        var secondName = MapNameCatalog.GetContinentName("Canopus c", 2);

        Assert.Equal(firstName, secondName);
    }

    [Fact]
    public void GetContinentName_RejectsBlankBodyName()
    {
        Assert.Throws<ArgumentException>(() => MapNameCatalog.GetContinentName("", 0));
    }

    [Fact]
    public void GetContinentName_RejectsNegativeContinentIndex()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MapNameCatalog.GetContinentName("Sirius b", -1));
    }
}
