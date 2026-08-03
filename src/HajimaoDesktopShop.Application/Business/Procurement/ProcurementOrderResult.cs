using HajimaoDesktopShop.Domain.Economy;

namespace HajimaoDesktopShop.Application.Business.Procurement;

public enum ProcurementOrderPlacementStatus
{
    Success,
    UnknownStore,
    UnknownProduct,
    UnknownChannel,
    InvalidQuantity,
    BelowMinimum,
    CapacityExceeded,
    InsufficientFunds
}

public readonly record struct ProcurementOrderResult(
    ProcurementOrderPlacementStatus Status,
    ProcurementOrderSnapshot? Order,
    Money TotalCost);
