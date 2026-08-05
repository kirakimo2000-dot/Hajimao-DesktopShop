namespace HajimaoDesktopShop.Application.Business.Auditing;

public sealed record BusinessSimulationAuditReport(
    int RequestedSeconds,
    int AppliedSeconds,
    int BatchCount,
    long StartingGameMinute,
    long EndingGameMinute,
    int StartingPlayerLevel,
    int EndingPlayerLevel,
    long TotalExperienceDelta,
    long CashDeltaCents,
    IReadOnlyList<StoreSimulationAuditReport> Stores);
