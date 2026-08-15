namespace HajimaoDesktopShop.Application.Business.Combat;

public static class ProductMasteryScaling
{
    public static int PowerPermille(int masteryLevel)
    {
        Validate(masteryLevel);
        return 1_000 + ((masteryLevel - 1) * 35);
    }

    public static int RevenuePermille(int masteryLevel)
    {
        Validate(masteryLevel);
        return 1_000 + ((masteryLevel - 1) * 15);
    }

    private static void Validate(int masteryLevel)
    {
        if (masteryLevel is < 1 or > 20)
        {
            throw new ArgumentOutOfRangeException(nameof(masteryLevel));
        }
    }
}

public static class ProductSlotProgressionPolicy
{
    public static int SlotsForServedCustomers(int servedCustomers)
    {
        if (servedCustomers < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(servedCustomers));
        }

        return servedCustomers switch
        {
            >= 1_000 => 6,
            >= 250 => 5,
            >= 50 => 4,
            _ => 3
        };
    }
}
