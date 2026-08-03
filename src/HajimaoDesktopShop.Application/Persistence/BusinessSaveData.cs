using HajimaoDesktopShop.Application.Business.Simulation;
using HajimaoDesktopShop.Domain.Employees;

namespace HajimaoDesktopShop.Application.Persistence;

public sealed record BusinessSaveData(
    long TotalExperience,
    long CashCents,
    IReadOnlyList<BusinessStoreSaveData> Stores);

public sealed record BusinessStoreSaveData(
    string StoreId,
    long RevenueCents,
    long StockPurchaseCostCents,
    long GrossProfitCents,
    long WageCostCents,
    IReadOnlyList<BusinessProductSaveData> Products);

public sealed record BusinessProductSaveData(
    string ProductId,
    long SalePriceCents,
    int Quantity);

public sealed record BusinessSimulationSaveData(
    long GameMinute,
    ulong RandomState,
    IReadOnlyList<EmployeeAssignmentSaveData> Employees,
    IReadOnlyList<StoreRuntimeSaveData> Stores,
    BusinessDayReport? LastCompletedDay);

public sealed record EmployeeAssignmentSaveData(
    string StoreId,
    string EmployeeId,
    string Name,
    EmployeeRole Role,
    int EfficiencyPermille,
    long HourlyWageCents,
    int WorkedMinutes,
    long TotalWagesAccruedCents,
    long WageRemainderCents);

public sealed record StoreRuntimeSaveData(
    string StoreId,
    int Visitors,
    int AcceptedPurchases,
    int CompletedSales,
    int LostSales,
    IReadOnlyList<string> CheckoutQueue,
    ActiveCheckoutSaveData? ActiveCheckout,
    int CleanlinessPermille,
    int ServicePermille,
    int WagePaymentFailures,
    int DayStartVisitors,
    int DayStartAcceptedPurchases,
    int DayStartCompletedSales,
    int DayStartLostSales,
    long DayStartRevenueCents,
    long DayStartGrossProfitCents,
    long DayStartWageCostCents,
    long DayQueueLengthTotal,
    int DayTickCount);

public sealed record ActiveCheckoutSaveData(
    string ProductId,
    int RemainingMinutes);
