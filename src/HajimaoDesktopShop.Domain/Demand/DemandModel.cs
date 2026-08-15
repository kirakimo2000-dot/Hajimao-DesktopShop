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
        return CreateBreakdown(
            baseDemand,
            price,
            service,
            queue,
            cleanliness,
            time: 0,
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
        return CreateBreakdown(
            baseDemand,
            price,
            service,
            queue,
            cleanliness,
            time: 0,
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

    private static int Scale(int value, int permille) =>
        checked((int)((long)value * permille / 1_000L));
}
