using HajimaoDesktopShop.Application.Game;
using HajimaoDesktopShop.Application.Business.StoreGrowth;

namespace HajimaoDesktopShop.Application.Business;

public sealed record BusinessSnapshot(
    int PlayerLevel,
    long TotalExperience,
    long CashCents,
    IReadOnlyList<BusinessStoreSnapshot> Stores);

public sealed record BusinessStoreSnapshot(
    string Id,
    string Name,
    long RevenueCents,
    long StockPurchaseCostCents,
    long GrossProfitCents,
    IReadOnlyList<ProductSnapshot> Products,
    long WageCostCents = 0,
    long NetProfitCents = 0,
    long OperatingCostCents = 0,
    StoreGrowthSnapshot? Growth = null);
