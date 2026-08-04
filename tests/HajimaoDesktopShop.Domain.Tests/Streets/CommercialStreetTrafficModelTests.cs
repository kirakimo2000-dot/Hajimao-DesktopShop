using HajimaoDesktopShop.Domain.Streets;

namespace HajimaoDesktopShop.Domain.Tests.Streets;

public sealed class CommercialStreetTrafficModelTests
{
    [Theory]
    [InlineData(1, CommercialStreetTier.Corner)]
    [InlineData(2, CommercialStreetTier.Corner)]
    [InlineData(3, CommercialStreetTier.Neighbors)]
    [InlineData(5, CommercialStreetTier.Street)]
    [InlineData(10, CommercialStreetTier.Block)]
    public void GetTier_UnlocksStreetExtentAtConfiguredLevels(
        int playerLevel,
        CommercialStreetTier expected)
    {
        Assert.Equal(expected, CommercialStreetTrafficModel.GetTier(playerLevel));
    }

    [Theory]
    [InlineData(0, StreetWeather.Clear)]
    [InlineData(360, StreetWeather.Cloudy)]
    [InlineData(720, StreetWeather.Rain)]
    [InlineData(1_080, StreetWeather.Wind)]
    [InlineData(1_440, StreetWeather.Clear)]
    public void GetWeather_UsesDeterministicSixHourPeriods(long gameMinute, StreetWeather expected)
    {
        Assert.Equal(expected, CommercialStreetTrafficModel.GetWeather(gameMinute));
    }

    [Fact]
    public void CalculateSharedTraffic_AppliesStoreSynergyAndWeatherPenalty()
    {
        var clear = CommercialStreetTrafficModel.CalculateSharedTrafficBasisPoints(
            8_000,
            2,
            StreetWeather.Clear);
        var rain = CommercialStreetTrafficModel.CalculateSharedTrafficBasisPoints(
            8_000,
            2,
            StreetWeather.Rain);
        var wind = CommercialStreetTrafficModel.CalculateSharedTrafficBasisPoints(
            8_000,
            2,
            StreetWeather.Wind);

        Assert.Equal(9_200, clear);
        Assert.Equal(6_440, rain);
        Assert.Equal(7_820, wind);
    }

    [Fact]
    public void CalculateSharedTraffic_ClampsBusyStreetToOneHundredPercent()
    {
        Assert.Equal(
            10_000,
            CommercialStreetTrafficModel.CalculateSharedTrafficBasisPoints(
                10_000,
                3,
                StreetWeather.Clear));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(5_000, 3)]
    [InlineData(10_000, 6)]
    public void GetVisiblePedestrianCount_StaysWithinSceneBudget(int basisPoints, int expected)
    {
        Assert.Equal(expected, CommercialStreetTrafficModel.GetVisiblePedestrianCount(basisPoints));
    }

    [Theory]
    [InlineData(480, StreetWeather.Clear, 2)]
    [InlineData(720, StreetWeather.Clear, 1)]
    [InlineData(120, StreetWeather.Clear, 0)]
    [InlineData(1_020, StreetWeather.Wind, 1)]
    public void GetVisibleVehicleCount_ReflectsRoadPeriodAndWind(
        int minuteOfDay,
        StreetWeather weather,
        int expected)
    {
        Assert.Equal(
            expected,
            CommercialStreetTrafficModel.GetVisibleVehicleCount(minuteOfDay, weather));
    }

    [Fact]
    public void Rules_RejectInvalidInputs()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CommercialStreetTrafficModel.GetTier(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => CommercialStreetTrafficModel.GetWeather(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CommercialStreetTrafficModel.CalculateSharedTrafficBasisPoints(-1, 1, StreetWeather.Clear));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CommercialStreetTrafficModel.CalculateSharedTrafficBasisPoints(1, 0, StreetWeather.Clear));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CommercialStreetTrafficModel.GetVisiblePedestrianCount(10_001));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CommercialStreetTrafficModel.GetVisibleVehicleCount(1_440, StreetWeather.Clear));
    }
}
