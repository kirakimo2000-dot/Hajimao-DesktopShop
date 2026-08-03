namespace HajimaoDesktopShop.Domain.Inventory;

public enum StockChangeStatus
{
    Success,
    InvalidQuantity,
    CapacityExceeded,
    InsufficientStock
}
