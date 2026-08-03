namespace HajimaoDesktopShop.Application.Business.Simulation;

public sealed record BusinessSimulationOptions
{
    public BusinessSimulationOptions(
        int baseArrivalBasisPoints = 3_000,
        int basePurchaseBasisPoints = 9_000,
        int baseCheckoutMinutes = 2,
        int visitorDirtPermille = 12,
        int cleanerBaseRecoveryPermille = 10,
        int initialCleanlinessPermille = 1_000)
    {
        if (baseArrivalBasisPoints is < 0 or > 10_000)
        {
            throw new ArgumentOutOfRangeException(nameof(baseArrivalBasisPoints));
        }

        if (basePurchaseBasisPoints is < 0 or > 10_000)
        {
            throw new ArgumentOutOfRangeException(nameof(basePurchaseBasisPoints));
        }

        if (baseCheckoutMinutes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(baseCheckoutMinutes));
        }

        if (visitorDirtPermille <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(visitorDirtPermille));
        }

        if (cleanerBaseRecoveryPermille <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cleanerBaseRecoveryPermille));
        }

        if (initialCleanlinessPermille is < 0 or > 1_000)
        {
            throw new ArgumentOutOfRangeException(nameof(initialCleanlinessPermille));
        }

        BaseArrivalBasisPoints = baseArrivalBasisPoints;
        BasePurchaseBasisPoints = basePurchaseBasisPoints;
        BaseCheckoutMinutes = baseCheckoutMinutes;
        VisitorDirtPermille = visitorDirtPermille;
        CleanerBaseRecoveryPermille = cleanerBaseRecoveryPermille;
        InitialCleanlinessPermille = initialCleanlinessPermille;
    }

    public int BaseArrivalBasisPoints { get; }

    public int BasePurchaseBasisPoints { get; }

    public int BaseCheckoutMinutes { get; }

    public int VisitorDirtPermille { get; }

    public int CleanerBaseRecoveryPermille { get; }

    public int InitialCleanlinessPermille { get; }
}
