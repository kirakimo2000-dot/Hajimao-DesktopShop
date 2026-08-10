namespace HajimaoDesktopShop.Application.Business.Strategy;

public sealed record StoreProductStrategyPlan(
    string ProductId,
    long SalePriceCents,
    int PriceMultiplierBasisPoints,
    int ReorderPoint,
    int TargetQuantity,
    string PreferredChannelId,
    bool UseEmergencySupplierWhenOutOfStock);

public sealed record StoreStrategyPlan(
    string StoreId,
    StorePricingPreset Pricing,
    StoreStockingPreset Stocking,
    IReadOnlyList<StoreProductStrategyPlan> Products);
