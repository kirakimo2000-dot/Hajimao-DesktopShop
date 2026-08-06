namespace HajimaoDesktopShop.Application.Business.Analysis;

public sealed record StoreEconomyAnalysisInput(
    string StoreId,
    long SharedCashCents,
    long RevenueCents,
    long GrossProfitCents,
    long WageCostCents,
    long OperatingCostCents,
    long NetProfitCents,
    int Visitors,
    int CompletedSales,
    int LostSales,
    int OutOfStockProductCount,
    int CheckoutQueueLength,
    int ServicePermille,
    bool IsCompletedDay);
