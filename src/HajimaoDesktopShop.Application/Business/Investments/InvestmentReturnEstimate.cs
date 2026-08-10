namespace HajimaoDesktopShop.Application.Business.Investments;

public sealed record InvestmentReturnEstimate(
    long CostCents,
    long ExpectedDailyNetBenefitCents,
    long? PaybackDaysTenths,
    long CashAfterInvestmentCents,
    InvestmentCashPressure CashPressure,
    bool IsAffordable);
