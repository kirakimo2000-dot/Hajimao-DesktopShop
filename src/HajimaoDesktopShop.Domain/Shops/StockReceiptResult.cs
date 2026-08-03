namespace HajimaoDesktopShop.Domain.Shops;

public enum StockReceiptStatus
{
    Success,
    UnknownProduct,
    InvalidQuantity,
    CapacityExceeded
}

public readonly record struct StockReceiptResult(StockReceiptStatus Status);
