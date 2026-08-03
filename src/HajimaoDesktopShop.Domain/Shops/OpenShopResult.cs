using HajimaoDesktopShop.Domain.Economy;

namespace HajimaoDesktopShop.Domain.Shops;

public enum OpenShopStatus
{
    Success,
    UnknownDefinition,
    LevelLocked,
    AlreadyOpen,
    InsufficientFunds
}

public readonly record struct OpenShopResult(
    OpenShopStatus Status,
    ShopId ShopId,
    Money OpeningCost);
