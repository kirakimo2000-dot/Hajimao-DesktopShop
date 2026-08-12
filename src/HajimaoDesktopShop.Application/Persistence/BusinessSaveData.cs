using HajimaoDesktopShop.Application.Business.Simulation;
using HajimaoDesktopShop.Application.Business.Procurement;
using HajimaoDesktopShop.Domain.Employees;

namespace HajimaoDesktopShop.Application.Persistence;

public sealed record BusinessSaveData(
    long TotalExperience,
    long CashCents,
    IReadOnlyList<BusinessStoreSaveData> Stores,
    BusinessProcurementSaveData? Procurement = null,
    IReadOnlyList<StorePromotionSaveData>? Promotions = null);

public sealed record BusinessProcurementSaveData(
    long NextOrderId,
    IReadOnlyList<ProcurementOrderSaveData> PendingOrders,
    IReadOnlyList<AutoRestockPolicySaveData> AutoRestockPolicies);

public sealed record ProcurementOrderSaveData(
    long OrderId,
    string StoreId,
    string ProductId,
    string ChannelId,
    int Quantity,
    long UnitCostCents,
    int RemainingMinutes,
    ProcurementOrderStatus Status,
    bool IsAutomatic);

public sealed record AutoRestockPolicySaveData(
    string StoreId,
    string ProductId,
    bool IsEnabled,
    int ReorderPoint,
    int TargetQuantity,
    string PreferredChannelId,
    bool UseEmergencySupplierWhenOutOfStock);

public sealed record BusinessStoreSaveData(
    string StoreId,
    long RevenueCents,
    long StockPurchaseCostCents,
    long GrossProfitCents,
    long WageCostCents,
    IReadOnlyList<BusinessProductSaveData> Products,
    long OperatingCostCents = 0,
    StoreDevelopmentSaveData? Development = null,
    string StoreName = "",
    string StoreBrandId = "",
    string StoreFormatId = "",
    int StreetOrdinal = 0);

public sealed record StoreDevelopmentSaveData(
    int ExpansionLevel,
    int ShelfLevel,
    int DecorationLevel);

public sealed record StorePromotionSaveData(
    string StoreId,
    string CampaignId,
    int RemainingMinutes);

public sealed record BusinessProductSaveData(
    string ProductId,
    long SalePriceCents,
    int Quantity);

public sealed record BusinessSimulationSaveData(
    long GameMinute,
    ulong RandomState,
    IReadOnlyList<EmployeeAssignmentSaveData> Employees,
    IReadOnlyList<StoreRuntimeSaveData> Stores,
    BusinessDayReport? LastCompletedDay,
    EmployeeOperationsSaveData? EmployeeOperations = null);

public sealed record EmployeeOperationsSaveData(
    ulong CandidateRandomState,
    long NextCandidateId,
    IReadOnlyList<EmployeeCandidateSaveData> Candidates);

public sealed record EmployeeCandidateSaveData(
    string CandidateId,
    string Name,
    EmployeeRole Role,
    int EfficiencyPermille,
    long HourlyWageCents);

public sealed record EmployeeAssignmentSaveData(
    string StoreId,
    string EmployeeId,
    string Name,
    EmployeeRole Role,
    int EfficiencyPermille,
    long HourlyWageCents,
    int WorkedMinutes,
    long TotalWagesAccruedCents,
    long WageRemainderCents,
    int TrainingLevel = 0,
    int EnergyPermille = 1_000,
    int SatisfactionPermille = 700,
    int WorkMinutesTowardSatisfactionLoss = 0,
    int RestMinutesTowardSatisfactionGain = 0,
    int ShiftStartMinute = 0,
    int ShiftEndMinute = 0,
    bool IsAlwaysOn = true);

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
    int DayTickCount,
    long DayStartOperatingCostCents = 0);

public sealed record ActiveCheckoutSaveData(
    string ProductId,
    int RemainingMinutes);
