using HajimaoDesktopShop.Domain.Economy;

namespace HajimaoDesktopShop.Domain.Shops;

public enum StockPurchaseStatus
{
    Success,
    UnknownProduct,
    InvalidQuantity,
    CapacityExceeded,
    InsufficientFunds
}

public readonly record struct StockPurchaseResult(StockPurchaseStatus Status, Money TotalCost);
