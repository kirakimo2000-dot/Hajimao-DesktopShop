namespace HajimaoDesktopShop.Application.Business.Progression;

public sealed record ProgressionGoalSnapshot(
    ProgressionGoalId Id,
    string TargetStoreId,
    long CurrentValue,
    long TargetValue,
    int RequiredPlayerLevel = 0,
    long RequiredCashCents = 0);
