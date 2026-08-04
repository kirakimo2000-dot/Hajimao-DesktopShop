namespace HajimaoDesktopShop.Desktop.ViewModels;

public enum GameFeedbackKind
{
    RestockQueued,
    PriceChanged,
    SaleCompleted,
    ProcurementOrdered,
    AutoRestockChanged,
    EmployeeChanged,
    StoreGrowthChanged,
    PromotionStarted
}

public sealed class GameFeedbackEventArgs(GameFeedbackKind kind) : EventArgs
{
    public GameFeedbackKind Kind { get; } = kind;
}
