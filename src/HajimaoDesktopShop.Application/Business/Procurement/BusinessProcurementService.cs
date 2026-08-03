using HajimaoDesktopShop.Domain.Economy;
using HajimaoDesktopShop.Domain.Shops;

namespace HajimaoDesktopShop.Application.Business.Procurement;

internal sealed class BusinessProcurementService
{
    private readonly Dictionary<string, ProcurementChannel> _channels;
    private readonly IProcurementStockGateway _stock;
    private readonly List<ProcurementOrder> _pendingOrders = [];
    private long _nextOrderId = 1;

    public BusinessProcurementService(IProcurementStockGateway stock)
    {
        _stock = stock ?? throw new ArgumentNullException(nameof(stock));
        _channels = ProcurementChannel.DefaultChannels.ToDictionary(
            channel => channel.Id,
            StringComparer.Ordinal);
    }

    public ProcurementSnapshot GetSnapshot()
    {
        var orders = _pendingOrders
            .OrderBy(order => order.OrderId)
            .Select(order => order.CreateSnapshot())
            .ToArray();
        return new ProcurementSnapshot(
            ProcurementChannel.DefaultChannels,
            Array.AsReadOnly(orders));
    }

    public Money QuoteUnitCost(Money wholesalePrice, string channelId) =>
        FindChannel(channelId).QuoteUnitCost(wholesalePrice);

    public ProcurementOrderResult PlaceOrder(
        string storeId,
        string productId,
        string channelId,
        int quantity,
        bool isAutomatic)
    {
        storeId = NormalizeId(storeId, nameof(storeId));
        productId = NormalizeId(productId, nameof(productId));
        channelId = NormalizeId(channelId, nameof(channelId));
        if (!_stock.ContainsOpenStore(storeId))
        {
            return Failure(ProcurementOrderPlacementStatus.UnknownStore);
        }

        var product = _stock.FindProduct(storeId, productId);
        if (product is null)
        {
            return Failure(ProcurementOrderPlacementStatus.UnknownProduct);
        }

        if (!_channels.TryGetValue(channelId, out var channel))
        {
            return Failure(ProcurementOrderPlacementStatus.UnknownChannel);
        }

        if (quantity <= 0)
        {
            return Failure(ProcurementOrderPlacementStatus.InvalidQuantity);
        }

        if (quantity < channel.MinimumOrderQuantity)
        {
            return Failure(ProcurementOrderPlacementStatus.BelowMinimum);
        }

        var inbound = _pendingOrders
            .Where(order => string.Equals(order.StoreId, storeId, StringComparison.Ordinal)
                && string.Equals(order.ProductId, productId, StringComparison.Ordinal))
            .Sum(order => order.Quantity);
        if (checked(product.Quantity + inbound + quantity) > product.Capacity)
        {
            return Failure(ProcurementOrderPlacementStatus.CapacityExceeded);
        }

        var unitCost = channel.QuoteUnitCost(product.WholesalePrice);
        var payment = _stock.TryPayForStockOrder(storeId, productId, quantity, unitCost);
        if (payment.Status != StockPurchaseStatus.Success)
        {
            return Failure(MapPaymentFailure(payment.Status), payment.TotalCost);
        }

        var order = new ProcurementOrder(
            _nextOrderId++,
            storeId,
            productId,
            channelId,
            quantity,
            unitCost.Cents,
            channel.DeliveryMinutes,
            isAutomatic);
        if (channel.DeliveryMinutes == 0 && TryDeliver(order))
        {
            return new ProcurementOrderResult(
                ProcurementOrderPlacementStatus.Success,
                order.CreateSnapshot(),
                payment.TotalCost);
        }

        _pendingOrders.Add(order);
        return new ProcurementOrderResult(
            ProcurementOrderPlacementStatus.Success,
            order.CreateSnapshot(),
            payment.TotalCost);
    }

    public void AdvanceMinute()
    {
        foreach (var order in _pendingOrders.OrderBy(order => order.OrderId).ToArray())
        {
            if (order.RemainingMinutes > 0)
            {
                order.RemainingMinutes--;
            }

            if (order.RemainingMinutes == 0 && TryDeliver(order))
            {
                _pendingOrders.Remove(order);
            }
        }
    }

    private bool TryDeliver(ProcurementOrder order)
    {
        var receipt = _stock.TryReceivePaidStock(order.StoreId, order.ProductId, order.Quantity);
        if (receipt.Status == StockReceiptStatus.Success)
        {
            order.Status = ProcurementOrderStatus.Delivered;
            return true;
        }

        if (receipt.Status == StockReceiptStatus.CapacityExceeded)
        {
            order.Status = ProcurementOrderStatus.AwaitingSpace;
            return false;
        }

        throw new InvalidOperationException(
            $"Paid procurement order {order.OrderId} could not be delivered: {receipt.Status}.");
    }

    private ProcurementChannel FindChannel(string channelId)
    {
        channelId = NormalizeId(channelId, nameof(channelId));
        return _channels.TryGetValue(channelId, out var channel)
            ? channel
            : throw new KeyNotFoundException($"Procurement channel '{channelId}' was not found.");
    }

    private static string NormalizeId(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A stable ID is required.", parameterName);
        }

        return value.Trim();
    }

    private static ProcurementOrderPlacementStatus MapPaymentFailure(StockPurchaseStatus status) =>
        status switch
        {
            StockPurchaseStatus.UnknownProduct => ProcurementOrderPlacementStatus.UnknownProduct,
            StockPurchaseStatus.InvalidQuantity => ProcurementOrderPlacementStatus.InvalidQuantity,
            StockPurchaseStatus.CapacityExceeded => ProcurementOrderPlacementStatus.CapacityExceeded,
            StockPurchaseStatus.InsufficientFunds => ProcurementOrderPlacementStatus.InsufficientFunds,
            _ => throw new InvalidOperationException($"Unexpected stock payment status: {status}.")
        };

    private static ProcurementOrderResult Failure(
        ProcurementOrderPlacementStatus status,
        Money? totalCost = null) =>
        new(status, null, totalCost ?? Money.Zero);
}
