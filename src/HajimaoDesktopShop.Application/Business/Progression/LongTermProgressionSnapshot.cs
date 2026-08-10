namespace HajimaoDesktopShop.Application.Business.Progression;

public sealed record LongTermProgressionSnapshot(
    ProgressionGoalSnapshot CurrentGoal,
    int OpenStoreCount,
    int ConfiguredStoreCount,
    int PlayerLevel,
    long SharedCashCents);
