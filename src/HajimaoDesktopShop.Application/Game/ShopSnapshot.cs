namespace HajimaoDesktopShop.Application.Game;

public sealed record ShopSnapshot(
    long CashCents,
    long RevenueCents,
    long StockPurchaseCostCents,
    long GrossProfitCents,
    IReadOnlyList<ProductSnapshot> Products);
