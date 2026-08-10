namespace HajimaoDesktopShop.Application.Business.Investments;

public enum InvestmentCommandStatus
{
    Success,
    UnknownStore,
    UnknownCandidate,
    NotAvailable,
    InsufficientFunds,
    CommandRejected
}

public sealed record InvestmentCommandResult(
    InvestmentCommandStatus Status,
    InvestmentCandidate? AppliedCandidate,
    long CostCents,
    string? CreatedEmployeeId = null);
