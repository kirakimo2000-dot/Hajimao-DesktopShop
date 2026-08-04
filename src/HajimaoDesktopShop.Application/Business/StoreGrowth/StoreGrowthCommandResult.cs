namespace HajimaoDesktopShop.Application.Business.StoreGrowth;

public enum StoreGrowthCommandStatus
{
    Success,
    UnknownStore,
    UnknownPromotion,
    MaximumLevel,
    PrerequisiteNotMet,
    PromotionAlreadyActive,
    InsufficientFunds
}

public sealed record StoreGrowthCommandResult(
    StoreGrowthCommandStatus Status,
    long CostCents);
