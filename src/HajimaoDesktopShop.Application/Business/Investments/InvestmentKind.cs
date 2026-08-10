namespace HajimaoDesktopShop.Application.Business.Investments;

public enum InvestmentKind
{
    Expansion,
    Shelf,
    Decoration,
    Employee,
    OpenStore
}

public enum InvestmentEstimateCondition
{
    InsufficientEvidence,
    QueueLossesRepeat,
    StockLossesRepeat,
    TrafficConversionStaysStable,
    RoleBottleneckPersists,
    NewStoreNeedsCompletedDay
}

public enum InvestmentAvailability
{
    Available,
    InsufficientFunds,
    PrerequisiteNotMet,
    LevelLocked
}
