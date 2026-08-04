using HajimaoDesktopShop.Application.Business.Procurement;

namespace HajimaoDesktopShop.Desktop.ViewModels.Market;

public sealed record ProcurementOrderItemViewModel(
    long OrderId,
    string ProductId,
    string ChannelName,
    int Quantity,
    int RemainingMinutes,
    ProcurementOrderStatus Status,
    bool IsAutomatic);
