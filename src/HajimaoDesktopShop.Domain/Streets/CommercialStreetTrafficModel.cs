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
            5 => CommercialStreetTier.Block,
            _ => throw new ArgumentOutOfRangeException(nameof(openStoreCount))
        };

    public static StreetWeather GetWeather(long gameMinute)
    {
        if (gameMinute < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(gameMinute));
        }

        return ((gameMinute / WeatherPeriodMinutes) % 4L) switch
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

    public static int GetVisibleVehicleCount(int minuteOfDay, StreetWeather weather)
    {
        if (minuteOfDay is < 0 or >= 1_440)
        {
            throw new ArgumentOutOfRangeException(nameof(minuteOfDay));
        }

        var count = minuteOfDay switch
        {
            >= 420 and < 600 => 2,
            >= 960 and < 1_140 => 2,
            >= 360 and < 1_320 => 1,
            _ => 0
        };
        return weather == StreetWeather.Wind ? Math.Max(0, count - 1) : count;
    }
}
