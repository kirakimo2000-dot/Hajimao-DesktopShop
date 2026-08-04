namespace HajimaoDesktopShop.Application.Business.Simulation;

public sealed record BusinessDayReport(
    int DayNumber,
    IReadOnlyList<StoreDayReport> Stores);

public sealed record StoreDayReport(
    string StoreId,
    int Visitors,
    int AcceptedPurchases,
    int CompletedSales,
    int LostSales,
    long RevenueCents,
    long GrossProfitCents,
    long WageCostCents,
    long NetProfitCents,
    int ClosingCleanlinessPermille,
    int AverageQueueLengthBasisPoints,
    long OperatingCostCents = 0);
