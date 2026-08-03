using HajimaoDesktopShop.Domain.Economy;
using HajimaoDesktopShop.Domain.Shops;

namespace HajimaoDesktopShop.Application.Business.Procurement;

internal interface IProcurementStockGateway
{
    bool ContainsOpenStore(string storeId);

    ProcurementProductState? FindProduct(string storeId, string productId);

    StockPurchaseResult TryPayForStockOrder(
        string storeId,
        string productId,
        int quantity,
        Money unitCost);

    StockReceiptResult TryReceivePaidStock(string storeId, string productId, int quantity);
}

internal sealed record ProcurementProductState(
    string StoreId,
    string ProductId,
    int Quantity,
    int Capacity,
    Money WholesalePrice);
