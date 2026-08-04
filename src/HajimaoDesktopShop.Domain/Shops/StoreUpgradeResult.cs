using HajimaoDesktopShop.Domain.Economy;

namespace HajimaoDesktopShop.Domain.Shops;

public enum StoreUpgradeStatus
{
    Success,
    UnknownStore,
    MaximumLevel,
    PrerequisiteNotMet,
    InsufficientFunds
}

public sealed record StoreUpgradeResult(StoreUpgradeStatus Status, Money Cost);
