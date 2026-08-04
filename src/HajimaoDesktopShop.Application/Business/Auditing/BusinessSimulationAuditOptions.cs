namespace HajimaoDesktopShop.Application.Business.Auditing;

public sealed record BusinessSimulationAuditOptions
{
    public BusinessSimulationAuditOptions(int batchSize = 10_000)
    {
        if (batchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize));
        }

        BatchSize = batchSize;
    }

    public int BatchSize { get; }
}
