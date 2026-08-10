namespace HajimaoDesktopShop.Application.Business.Strategy;

public enum StoreStrategyCommandStatus
{
    Success,
    UnknownStore,
    NoProducts
}

public sealed record StoreStrategyCommandResult(
    StoreStrategyCommandStatus Status,
    StoreStrategyPlan? AppliedPlan);
