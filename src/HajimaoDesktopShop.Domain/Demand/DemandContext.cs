namespace HajimaoDesktopShop.Domain.Demand;

public sealed record DemandContext
{
    public DemandContext(
        int baseBasisPoints,
        int priceIndexBasisPoints,
        int servicePermille,
        int queueLength,
        int cleanlinessPermille,
        int minuteOfDay,
        int attractionBasisPoints = 0,
        int promotionBasisPoints = 0,
        DemandSensitivity? sensitivity = null,
        DemandTimeCurve timeCurve = DemandTimeCurve.Steady)
    {
        if (baseBasisPoints is < 0 or > 10_000)
        {
            throw new ArgumentOutOfRangeException(nameof(baseBasisPoints));
        }

        if (priceIndexBasisPoints is < 1 or > 30_000)
        {
            throw new ArgumentOutOfRangeException(nameof(priceIndexBasisPoints));
        }

        if (servicePermille is < 0 or > 2_000)
        {
            throw new ArgumentOutOfRangeException(nameof(servicePermille));
        }

        if (queueLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(queueLength));
        }

        if (cleanlinessPermille is < 0 or > 2_000)
        {
            throw new ArgumentOutOfRangeException(nameof(cleanlinessPermille));
        }

        if (minuteOfDay is < 0 or >= 1_440)
        {
            throw new ArgumentOutOfRangeException(nameof(minuteOfDay));
        }

        if (attractionBasisPoints is < 0 or > 10_000)
        {
            throw new ArgumentOutOfRangeException(nameof(attractionBasisPoints));
        }

        if (promotionBasisPoints is < 0 or > 10_000)
        {
            throw new ArgumentOutOfRangeException(nameof(promotionBasisPoints));
        }

        BaseBasisPoints = baseBasisPoints;
        PriceIndexBasisPoints = priceIndexBasisPoints;
        ServicePermille = servicePermille;
        QueueLength = queueLength;
        CleanlinessPermille = cleanlinessPermille;
        MinuteOfDay = minuteOfDay;
        AttractionBasisPoints = attractionBasisPoints;
        PromotionBasisPoints = promotionBasisPoints;
        Sensitivity = sensitivity ?? DemandSensitivity.Neutral;
        TimeCurve = timeCurve;
    }

    public int BaseBasisPoints { get; }

    public int PriceIndexBasisPoints { get; }

    public int ServicePermille { get; }

    public int QueueLength { get; }

    public int CleanlinessPermille { get; }

    public int MinuteOfDay { get; }

    public int AttractionBasisPoints { get; }

    public int PromotionBasisPoints { get; }

    public DemandSensitivity Sensitivity { get; }

    public DemandTimeCurve TimeCurve { get; }
}
