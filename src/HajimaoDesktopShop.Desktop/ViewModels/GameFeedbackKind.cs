namespace HajimaoDesktopShop.Desktop.ViewModels;

public enum GameFeedbackKind
{
    RestockQueued,
    PriceChanged,
    SaleCompleted
}

public sealed class GameFeedbackEventArgs(GameFeedbackKind kind) : EventArgs
{
    public GameFeedbackKind Kind { get; } = kind;
}
