namespace HajimaoDesktopShop.Application.Persistence;

public sealed record BusinessSaveData(
    long TotalExperience,
    long CashCents,
    IReadOnlyList<BusinessStoreSaveData> Stores);

public sealed record BusinessStoreSaveData(
    string StoreId,
    long RevenueCents,
    long StockPurchaseCostCents,
    long GrossProfitCents,
    long WageCostCents,
    IReadOnlyList<BusinessProductSaveData> Products);

public sealed record BusinessProductSaveData(
    string ProductId,
    long SalePriceCents,
    int Quantity);

public sealed record BusinessSimulationSaveData;
