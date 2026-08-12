namespace HajimaoDesktopShop.Domain.Demand;

public static class DemandModel
{
    public static DemandBreakdown CalculateArrival(DemandContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var sensitivity = context.Sensitivity;
        var baseDemand = Scale(context.BaseBasisPoints, sensitivity.BaseDemandPermille);
        var price = Scale(
            Math.Clamp((10_000 - context.PriceIndexBasisPoints) / 2, -2_500, 1_500),
            sensitivity.PricePermille);
        var service = Scale(
            Math.Clamp((context.ServicePermille - 1_000) * 2, -2_000, 2_000),
            sensitivity.ServicePermille);
        var queue = Scale(
            -checked((int)Math.Min((long)context.QueueLength * 350L, 2_800L)),
            sensitivity.QueuePermille);
        var cleanliness = Scale(
            Math.Clamp((context.CleanlinessPermille - 1_000) * 2, -2_000, 2_000),
            sensitivity.CleanlinessPermille);
        var time = CalculateTimeAdjustment(context.MinuteOfDay, context.TimeCurve);

        return CreateBreakdown(
            baseDemand,
            price,
            service,
            queue,
            cleanliness,
            time,
            context.AttractionBasisPoints,
            context.PromotionBasisPoints);
    }

    public static DemandBreakdown CalculatePurchase(DemandContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var sensitivity = context.Sensitivity;
        var baseDemand = Scale(context.BaseBasisPoints, sensitivity.BaseDemandPermille);
        var price = Scale(
            Math.Clamp((10_000 - context.PriceIndexBasisPoints) * 2, -6_000, 1_000),
            sensitivity.PricePermille);
        var service = Scale(
            Math.Clamp(context.ServicePermille - 1_000, -1_000, 1_000),
            sensitivity.ServicePermille);
        var queue = Scale(
            -checked((int)Math.Min((long)context.QueueLength * 250L, 3_000L)),
            sensitivity.QueuePermille);
        var cleanliness = Scale(
            Math.Clamp(context.CleanlinessPermille - 1_000, -1_000, 1_000),
            sensitivity.CleanlinessPermille);
        var time = CalculateTimeAdjustment(context.MinuteOfDay, context.TimeCurve) / 2;

        return CreateBreakdown(
            baseDemand,
            price,
            service,
            queue,
            cleanliness,
            time,
            context.AttractionBasisPoints,
            context.PromotionBasisPoints);
    }

    private static DemandBreakdown CreateBreakdown(
        int baseBasisPoints,
        int price,
        int service,
        int queue,
        int cleanliness,
        int time,
        int attraction,
        int promotion)
    {
        var total = (long)baseBasisPoints
            + price
            + service
            + queue
            + cleanliness
            + time
            + attraction
            + promotion;
        return new DemandBreakdown(
            baseBasisPoints,
            price,
            service,
            queue,
            cleanliness,
            time,
            checked((int)Math.Clamp(total, 0L, 10_000L)),
            attraction,
            promotion);
    }

    private static int CalculateTimeAdjustment(int minuteOfDay, DemandTimeCurve curve) => curve switch
    {
        DemandTimeCurve.Steady => SteadyTimeAdjustment(minuteOfDay),
        DemandTimeCurve.AllDayVolume => minuteOfDay < 360 ? -800 : 300,
        DemandTimeCurve.AfternoonSelect => minuteOfDay switch
        {
            < 600 => -900,
            >= 720 and < 1_200 => 900,
            _ => 100
        },
        DemandTimeCurve.CommuterPeaks => minuteOfDay switch
        {
            >= 420 and < 600 => 1_800,
            >= 1_020 and < 1_200 => 2_100,
            < 360 => -1_800,
            _ => -700
        },
        _ => throw new ArgumentOutOfRangeException(nameof(curve))
    };

    private static int SteadyTimeAdjustment(int minuteOfDay) => minuteOfDay switch
    {
        < 360 => -1_500,
        >= 420 and < 600 => 800,
        >= 660 and < 840 => 700,
        >= 1_020 and < 1_200 => 1_000,
        _ => 0
    };

    private static int Scale(int value, int permille) =>
        checked((int)((long)value * permille / 1_000L));
}
