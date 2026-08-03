namespace HajimaoDesktopShop.Application.Business.Procurement;

public enum ProcurementOrderStatus
{
    InTransit,
    AwaitingSpace,
    Delivered
}

public sealed record ProcurementOrderSnapshot(
    long OrderId,
    string StoreId,
    string ProductId,
    string ChannelId,
    int Quantity,
    long UnitCostCents,
    long TotalCostCents,
    int RemainingMinutes,
    ProcurementOrderStatus Status,
    bool IsAutomatic);

public sealed record ProcurementSnapshot(
    IReadOnlyList<ProcurementChannel> Channels,
    IReadOnlyList<ProcurementOrderSnapshot> PendingOrders,
    IReadOnlyList<AutoRestockPolicy> AutoRestockPolicies);
