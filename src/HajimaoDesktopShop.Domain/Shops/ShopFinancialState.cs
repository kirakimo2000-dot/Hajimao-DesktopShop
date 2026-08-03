using HajimaoDesktopShop.Domain.Economy;

namespace HajimaoDesktopShop.Domain.Shops;

public sealed record ShopFinancialState(
    Money Cash,
    Money TotalRevenue,
    Money TotalStockPurchaseCost,
    Money TotalGrossProfit);
