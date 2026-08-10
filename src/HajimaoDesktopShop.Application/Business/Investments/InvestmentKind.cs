namespace HajimaoDesktopShop.Application.Business.Investments;

public enum InvestmentKind
{
    Expansion,
    Shelf,
    Decoration,
    Employee
}

public enum InvestmentEstimateCondition
{
    InsufficientEvidence,
    QueueLossesRepeat,
    StockLossesRepeat,
    TrafficConversionStaysStable,
    RoleBottleneckPersists
}

public enum InvestmentAvailability
{
    Available,
    InsufficientFunds,
    PrerequisiteNotMet
}
