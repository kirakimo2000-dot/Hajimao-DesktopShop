namespace HajimaoDesktopShop.Application.Business.Investments;

public enum InvestmentComparisonStatus
{
    BaselineUnavailable,
    WaitingForCompletedDay,
    Compared
}

public sealed record InvestmentTrackingSnapshot(
    string StoreId,
    string CandidateId,
    InvestmentKind Kind,
    long CostCents,
    long ExpectedDailyNetBenefitCents,
    long GameMinute,
    InvestmentComparisonStatus Status,
    int? BaselineDayNumber,
    long? BaselineNetProfitCents,
    int? BaselineCompletedSales,
    int? BaselineLostSales,
    int? CurrentDayNumber,
    long? CurrentNetProfitCents,
    int? CurrentCompletedSales,
    int? CurrentLostSales,
    long? NetProfitChangeCents,
    int? CompletedSalesChange,
    int? LostSalesChange);
