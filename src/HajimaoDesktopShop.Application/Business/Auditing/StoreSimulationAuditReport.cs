namespace HajimaoDesktopShop.Application.Business.Auditing;

public sealed record StoreSimulationAuditReport(
    string StoreId,
    int VisitorsDelta,
    int AcceptedPurchasesDelta,
    int CompletedSalesDelta,
    int LostSalesDelta,
    int WagePaymentFailuresDelta,
    long RevenueDeltaCents,
    long StockPurchaseCostDeltaCents,
    long GrossProfitDeltaCents,
    long WageCostDeltaCents,
    long OperatingCostDeltaCents,
    long NetProfitDeltaCents,
    int EndingCheckoutQueueLength,
    int EndingCleanlinessPermille,
    int EndingInventoryUnits);
