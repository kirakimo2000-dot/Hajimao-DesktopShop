namespace HajimaoDesktopShop.Domain.Demand;

public sealed record DemandSensitivity
{
    public static DemandSensitivity Neutral { get; } = new(1_000, 1_000, 1_000, 1_000, 1_000);

    public DemandSensitivity(
        int BaseDemandPermille,
        int PricePermille,
        int ServicePermille,
        int QueuePermille,
        int CleanlinessPermille)
    {
        var values = new[]
        {
            BaseDemandPermille,
            PricePermille,
            ServicePermille,
            QueuePermille,
            CleanlinessPermille
        };
        if (values.Any(value => value is <= 0 or > 3_000))
        {
            throw new ArgumentOutOfRangeException(nameof(BaseDemandPermille));
        }

        this.BaseDemandPermille = BaseDemandPermille;
        this.PricePermille = PricePermille;
        this.ServicePermille = ServicePermille;
        this.QueuePermille = QueuePermille;
        this.CleanlinessPermille = CleanlinessPermille;
    }

    public int BaseDemandPermille { get; }
    public int PricePermille { get; }
    public int ServicePermille { get; }
    public int QueuePermille { get; }
    public int CleanlinessPermille { get; }
}
