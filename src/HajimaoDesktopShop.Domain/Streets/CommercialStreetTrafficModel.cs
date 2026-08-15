namespace HajimaoDesktopShop.Domain.Streets;

public static class CommercialStreetTrafficModel
{
    private const int MaximumBasisPoints = 10_000;
    private const int StoreSynergyBasisPoints = 1_500;
    private const int WeatherPeriodMinutes = 360;

    public static CommercialStreetTier GetTier(int playerLevel)
    {
        if (playerLevel <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(playerLevel));
        }

        return playerLevel switch
        {
            >= 10 => CommercialStreetTier.Block,
            >= 5 => CommercialStreetTier.Street,
            >= 3 => CommercialStreetTier.Neighbors,
            _ => CommercialStreetTier.Corner
        };
    }

    public static int GetUnlockedStorefrontCount(int playerLevel) =>
        GetUnlockedStorefrontCount(GetTier(playerLevel));

    public static int GetUnlockedStorefrontCount(CommercialStreetTier tier) => tier switch
    {
        CommercialStreetTier.Corner => 1,
        CommercialStreetTier.Neighbors => 2,
        CommercialStreetTier.Street => 4,
        CommercialStreetTier.Block => 5,
        _ => throw new ArgumentOutOfRangeException(nameof(tier))
    };

    public static CommercialStreetTier GetTierForStorefrontCount(int openStoreCount) =>
        openStoreCount switch
        {
            1 => CommercialStreetTier.Corner,
            2 => CommercialStreetTier.Neighbors,
            3 or 4 => CommercialStreetTier.Street,
            >= 5 => CommercialStreetTier.Block,
            _ => throw new ArgumentOutOfRangeException(nameof(openStoreCount))
        };

    public static int GetVisitorOpportunityCount(int openStoreCount)
    {
        if (openStoreCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(openStoreCount));
        }

        return checked(Math.Max(1, (openStoreCount * 3 + 4) / 5));
    }

    public static StreetWeather GetWeather(long activeRuntimeTick)
    {
        if (activeRuntimeTick < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(activeRuntimeTick));
        }

        return ((activeRuntimeTick / WeatherPeriodMinutes) % 4L) switch
        {
            0 => StreetWeather.Clear,
            1 => StreetWeather.Cloudy,
            2 => StreetWeather.Rain,
            _ => StreetWeather.Wind
        };
    }

    public static int CalculateSharedTrafficBasisPoints(
        int strongestStoreDemandBasisPoints,
        int openStoreCount,
        StreetWeather weather)
    {
        if (strongestStoreDemandBasisPoints is < 0 or > MaximumBasisPoints)
        {
            throw new ArgumentOutOfRangeException(nameof(strongestStoreDemandBasisPoints));
        }

        if (openStoreCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(openStoreCount));
        }

        var storeFactor = checked(
            MaximumBasisPoints
            + Math.Min(openStoreCount - 1, 2) * StoreSynergyBasisPoints);
        var weatherFactor = weather switch
        {
            StreetWeather.Clear => 10_000,
            StreetWeather.Cloudy => 9_500,
            StreetWeather.Rain => 7_000,
            StreetWeather.Wind => 8_500,
            _ => throw new ArgumentOutOfRangeException(nameof(weather))
        };
        var traffic = checked(
            (long)strongestStoreDemandBasisPoints
            * storeFactor
            * weatherFactor
            / MaximumBasisPoints
            / MaximumBasisPoints);
        return checked((int)Math.Clamp(traffic, 0L, MaximumBasisPoints));
    }

    public static int GetVisiblePedestrianCount(int trafficBasisPoints)
    {
        if (trafficBasisPoints is < 0 or > MaximumBasisPoints)
        {
            throw new ArgumentOutOfRangeException(nameof(trafficBasisPoints));
        }

        return Math.Min(6, checked((trafficBasisPoints + 1_666) / 1_667));
    }

    public static int GetVisibleVehicleCount(int trafficBasisPoints, StreetWeather weather)
    {
        if (trafficBasisPoints is < 0 or > MaximumBasisPoints)
        {
            throw new ArgumentOutOfRangeException(nameof(trafficBasisPoints));
        }

        var count = trafficBasisPoints switch
        {
            0 => 0,
            <= 3_333 => 1,
            <= 6_666 => 2,
            _ => 3
        };
        return weather == StreetWeather.Wind ? Math.Max(0, count - 1) : count;
    }
}
