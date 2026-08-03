namespace HajimaoDesktopShop.Application.Business.Procurement;

internal sealed class ProcurementOrder(
    long orderId,
    string storeId,
    string productId,
    string channelId,
    int quantity,
    long unitCostCents,
    int remainingMinutes,
    bool isAutomatic)
{
    public long OrderId { get; } = orderId;

    public string StoreId { get; } = storeId;

    public string ProductId { get; } = productId;

    public string ChannelId { get; } = channelId;

    public int Quantity { get; } = quantity;

    public long UnitCostCents { get; } = unitCostCents;

    public int RemainingMinutes { get; set; } = remainingMinutes;

    public ProcurementOrderStatus Status { get; set; } = remainingMinutes == 0
        ? ProcurementOrderStatus.AwaitingSpace
        : ProcurementOrderStatus.InTransit;

    public bool IsAutomatic { get; } = isAutomatic;

    public ProcurementOrderSnapshot CreateSnapshot() =>
        new(
            OrderId,
            StoreId,
            ProductId,
            ChannelId,
            Quantity,
            UnitCostCents,
            checked(UnitCostCents * Quantity),
            RemainingMinutes,
            Status,
            IsAutomatic);
}
