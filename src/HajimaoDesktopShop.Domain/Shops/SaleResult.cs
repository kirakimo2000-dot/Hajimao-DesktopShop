using HajimaoDesktopShop.Domain.Economy;

namespace HajimaoDesktopShop.Domain.Shops;

public enum SaleStatus
{
    Success,
    UnknownProduct,
    InvalidQuantity,
    InsufficientStock
}

public readonly record struct SaleResult(SaleStatus Status, Money Revenue, Money GrossProfit);
