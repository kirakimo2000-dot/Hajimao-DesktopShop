using HajimaoDesktopShop.Domain.Demand;
using HajimaoDesktopShop.Application.Business.Employees;
using HajimaoDesktopShop.Application.Business.Street;

namespace HajimaoDesktopShop.Application.Business.Simulation;

public sealed record BusinessSimulationSnapshot(
    long GameMinute,
    BusinessSnapshot Business,
    IReadOnlyList<StoreOperationsSnapshot> Stores,
    EmployeeOperationsSnapshot Employees,
    CommercialStreetSnapshot Street,
    BusinessDayReport? LastCompletedDay = null);

public sealed record StoreOperationsSnapshot(
    string StoreId,
    int Visitors,
    int AcceptedPurchases,
    int CompletedSales,
    int LostSales,
    int CheckoutQueueLength,
    int CleanlinessPermille,
    int ServicePermille,
    int WagePaymentFailures,
    DemandBreakdown ArrivalDemand);
