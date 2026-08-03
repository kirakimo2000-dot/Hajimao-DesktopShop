using HajimaoDesktopShop.Domain.Economy;

namespace HajimaoDesktopShop.Domain.Shops;

public enum PriceChangeStatus
{
    Success,
    UnknownProduct,
    InvalidPrice
}

public readonly record struct PriceChangeResult(PriceChangeStatus Status, Money SalePrice);
