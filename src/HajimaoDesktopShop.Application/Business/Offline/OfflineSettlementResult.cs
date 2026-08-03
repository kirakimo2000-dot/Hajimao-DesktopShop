using HajimaoDesktopShop.Application.Business.Simulation;

namespace HajimaoDesktopShop.Application.Business.Offline;

public sealed record OfflineSettlementResult(
    long RequestedSeconds,
    int AppliedSeconds,
    bool WasCapped,
    OfflineTimeAnomaly Anomaly,
    OfflineBusinessTotals Before,
    OfflineBusinessTotals After,
    BusinessDayReport? LastCompletedDay);

public sealed record OfflineBusinessTotals(
    long CashCents,
    long RevenueCents,
    long GrossProfitCents,
    long WageCostCents,
    long NetProfitCents,
    int CompletedSales);
