namespace HajimaoDesktopShop.Domain.Demand;

public static class DemandModel
{
    public static DemandBreakdown CalculateArrival(DemandContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var price = Math.Clamp((10_000 - context.PriceIndexBasisPoints) / 2, -2_500, 1_500);
        var service = Math.Clamp((context.ServicePermille - 1_000) * 2, -2_000, 2_000);
        var queue = -checked((int)Math.Min((long)context.QueueLength * 350L, 2_800L));
        var cleanliness = Math.Clamp((context.CleanlinessPermille - 1_000) * 2, -2_000, 2_000);
        var time = CalculateTimeAdjustment(context.MinuteOfDay);

        return CreateBreakdown(context.BaseBasisPoints, price, service, queue, cleanliness, time);
    }

    public static DemandBreakdown CalculatePurchase(DemandContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var price = Math.Clamp((10_000 - context.PriceIndexBasisPoints) * 2, -6_000, 1_000);
        var service = Math.Clamp(context.ServicePermille - 1_000, -1_000, 1_000);
        var queue = -checked((int)Math.Min((long)context.QueueLength * 250L, 3_000L));
        var cleanliness = Math.Clamp(context.CleanlinessPermille - 1_000, -1_000, 1_000);
        var time = CalculateTimeAdjustment(context.MinuteOfDay) / 2;

        return CreateBreakdown(context.BaseBasisPoints, price, service, queue, cleanliness, time);
    }

    private static DemandBreakdown CreateBreakdown(
        int baseBasisPoints,
        int price,
        int service,
        int queue,
        int cleanliness,
        int time)
    {
        var total = (long)baseBasisPoints + price + service + queue + cleanliness + time;
        return new DemandBreakdown(
            baseBasisPoints,
            price,
            service,
            queue,
            cleanliness,
            time,
            checked((int)Math.Clamp(total, 0L, 10_000L)));
    }

    private static int CalculateTimeAdjustment(int minuteOfDay) => minuteOfDay switch
    {
        < 360 => -1_500,
        >= 420 and < 600 => 800,
        >= 660 and < 840 => 700,
        >= 1_020 and < 1_200 => 1_000,
        _ => 0
    };
}
