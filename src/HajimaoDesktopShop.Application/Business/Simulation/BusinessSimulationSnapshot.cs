using HajimaoDesktopShop.Domain.Demand;

namespace HajimaoDesktopShop.Application.Business.Simulation;

public sealed record BusinessSimulationSnapshot(
    long GameMinute,
    BusinessSnapshot Business,
    IReadOnlyList<StoreOperationsSnapshot> Stores);

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
