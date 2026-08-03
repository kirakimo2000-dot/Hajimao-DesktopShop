using HajimaoDesktopShop.Domain.Economy;

namespace HajimaoDesktopShop.Domain.Shops;

public enum WagePaymentStatus
{
    Success,
    UnknownStore,
    InsufficientFunds
}

public sealed record WagePaymentResult(WagePaymentStatus Status, Money Amount);
