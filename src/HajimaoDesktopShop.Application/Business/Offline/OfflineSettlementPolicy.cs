namespace HajimaoDesktopShop.Application.Business.Offline;

public sealed record OfflineSettlementPolicy
{
    public OfflineSettlementPolicy(
        int maxOfflineSeconds = 28_800,
        int batchSize = 10_000)
    {
        if (maxOfflineSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxOfflineSeconds));
        }

        if (batchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize));
        }

        MaxOfflineSeconds = maxOfflineSeconds;
        BatchSize = batchSize;
    }

    public int MaxOfflineSeconds { get; }

    public int BatchSize { get; }
}

public enum OfflineTimeAnomaly
{
    None,
    ClockMovedBackward
}
