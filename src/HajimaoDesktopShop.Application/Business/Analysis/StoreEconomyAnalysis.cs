namespace HajimaoDesktopShop.Application.Business.Analysis;

public sealed record StoreEconomyAnalysis(
    string StoreId,
    string Period,
    long RevenueCents,
    long GrossProfitCents,
    long WageCostCents,
    long OperatingCostCents,
    long NetProfitCents,
    long NecessaryOutflowCents,
    int GrossMarginBasisPoints,
    int NetMarginBasisPoints,
    int CashRunwayTenthsOfDay,
    int Visitors,
    int CompletedSales,
    int LostSales,
    StoreBottleneck Bottleneck);
