using HajimaoDesktopShop.Domain.Economy;
using HajimaoDesktopShop.Domain.Shops;
using HajimaoDesktopShop.Application.Persistence;

namespace HajimaoDesktopShop.Application.Business.Procurement;

internal sealed class BusinessProcurementService
{
    private readonly Dictionary<string, ProcurementChannel> _channels;
    private readonly IProcurementStockGateway _stock;
    private readonly List<ProcurementOrder> _pendingOrders = [];
    private readonly Dictionary<(string StoreId, string ProductId), AutoRestockPolicy> _policies = [];
    private long _nextOrderId = 1;

    public BusinessProcurementService(IProcurementStockGateway stock)
    {
        _stock = stock ?? throw new ArgumentNullException(nameof(stock));
        _channels = ProcurementChannel.DefaultChannels.ToDictionary(
            channel => channel.Id,
            StringComparer.Ordinal);
    }

    public BusinessProcurementService(
        IProcurementStockGateway stock,
        BusinessProcurementSaveData? restoredState)
        : this(stock)
    {
        if (restoredState is null)
        {
            return;
        }

        if (restoredState.NextOrderId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(restoredState));
        }

        var orders = restoredState.PendingOrders?.ToArray()
            ?? throw new ArgumentException("Restored procurement orders are required.", nameof(restoredState));
        var policies = restoredState.AutoRestockPolicies?.ToArray()
            ?? throw new ArgumentException("Restored auto-restock policies are required.", nameof(restoredState));
        if (orders.Any(order => order is null)
            || orders.Select(order => order.OrderId).Distinct().Count() != orders.Length
            || orders.Any(order => order.OrderId <= 0 || order.OrderId >= restoredState.NextOrderId))
        {
            throw new ArgumentException("Restored procurement order IDs are invalid.", nameof(restoredState));
        }

        foreach (var saved in orders.OrderBy(order => order.OrderId))
        {
            RestoreOrder(saved, nameof(restoredState));
        }

        foreach (var saved in policies)
        {
            ArgumentNullException.ThrowIfNull(saved);
            ConfigureAutoRestock(new AutoRestockPolicy(
                saved.StoreId,
                saved.ProductId,
                saved.IsEnabled,
                saved.ReorderPoint,
                saved.TargetQuantity,
                saved.PreferredChannelId,
                saved.UseEmergencySupplierWhenOutOfStock));
        }

        _nextOrderId = restoredState.NextOrderId;
    }

    public ProcurementSnapshot GetSnapshot()
    {
        var orders = _pendingOrders
            .OrderBy(order => order.OrderId)
            .Select(order => order.CreateSnapshot())
            .ToArray();
        var policies = _policies.Values
            .OrderBy(policy => policy.StoreId, StringComparer.Ordinal)
            .ThenBy(policy => policy.ProductId, StringComparer.Ordinal)
            .ToArray();
        return new ProcurementSnapshot(
            ProcurementChannel.DefaultChannels,
            Array.AsReadOnly(orders),
            Array.AsReadOnly(policies));
    }

    public void ConfigureAutoRestock(AutoRestockPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        var storeId = NormalizeId(policy.StoreId, nameof(policy));
        var productId = NormalizeId(policy.ProductId, nameof(policy));
        var channelId = NormalizeId(policy.PreferredChannelId, nameof(policy));
        if (!_stock.ContainsOpenStore(storeId))
        {
            throw new ArgumentException($"Store '{storeId}' is not open.", nameof(policy));
        }

        var product = _stock.FindProduct(storeId, productId)
            ?? throw new ArgumentException(
                $"Product '{productId}' is not available in store '{storeId}'.",
                nameof(policy));
        var channel = FindChannel(channelId);
        if (policy.ReorderPoint < 0
            || policy.TargetQuantity <= policy.ReorderPoint
            || policy.TargetQuantity > product.Capacity
            || channel.MinimumOrderQuantity > product.Capacity)
        {
            throw new ArgumentOutOfRangeException(nameof(policy));
        }

        _policies[(storeId, productId)] = policy with
        {
            StoreId = storeId,
            ProductId = productId,
            PreferredChannelId = channelId
        };
    }

    public Money QuoteUnitCost(Money wholesalePrice, string channelId) =>
        FindChannel(channelId).QuoteUnitCost(wholesalePrice);

    public BusinessProcurementSaveData CaptureSaveData()
    {
        var orders = _pendingOrders
            .OrderBy(order => order.OrderId)
            .Select(order => new ProcurementOrderSaveData(
                order.OrderId,
                order.StoreId,
                order.ProductId,
                order.ChannelId,
                order.Quantity,
                order.UnitCostCents,
                order.RemainingMinutes,
                order.Status,
                order.IsAutomatic))
            .ToArray();
        var policies = _policies.Values
            .OrderBy(policy => policy.StoreId, StringComparer.Ordinal)
            .ThenBy(policy => policy.ProductId, StringComparer.Ordinal)
            .Select(policy => new AutoRestockPolicySaveData(
                policy.StoreId,
                policy.ProductId,
                policy.IsEnabled,
                policy.ReorderPoint,
                policy.TargetQuantity,
                policy.PreferredChannelId,
                policy.UseEmergencySupplierWhenOutOfStock))
            .ToArray();
        return new BusinessProcurementSaveData(
            _nextOrderId,
            Array.AsReadOnly(orders),
            Array.AsReadOnly(policies));
    }

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

        ProcessAutoRestock();
    }

    private void ProcessAutoRestock()
    {
        foreach (var policy in _policies.Values
                     .Where(policy => policy.IsEnabled)
                     .OrderBy(policy => policy.StoreId, StringComparer.Ordinal)
                     .ThenBy(policy => policy.ProductId, StringComparer.Ordinal))
        {
            var product = _stock.FindProduct(policy.StoreId, policy.ProductId);
            if (product is null)
            {
                continue;
            }

            var matchingOrders = _pendingOrders.Where(order =>
                    string.Equals(order.StoreId, policy.StoreId, StringComparison.Ordinal)
                    && string.Equals(order.ProductId, policy.ProductId, StringComparison.Ordinal))
                .ToArray();
            var inbound = matchingOrders.Sum(order => order.Quantity);
            var effectiveQuantity = checked(product.Quantity + inbound);
            if (effectiveQuantity > policy.ReorderPoint
                || matchingOrders.Any(order => order.IsAutomatic))
            {
                continue;
            }

            var channel = FindChannel(policy.PreferredChannelId);
            var desired = checked(policy.TargetQuantity - effectiveQuantity);
            var orderQuantity = Math.Max(desired, channel.MinimumOrderQuantity);
            var result = PlaceOrder(
                policy.StoreId,
                policy.ProductId,
                channel.Id,
                orderQuantity,
                isAutomatic: true);
            if (result.Status == ProcurementOrderPlacementStatus.Success
                || product.Quantity != 0
                || !policy.UseEmergencySupplierWhenOutOfStock
                || string.Equals(channel.Id, "local-wholesale", StringComparison.Ordinal))
            {
                continue;
            }

            PlaceOrder(
                policy.StoreId,
                policy.ProductId,
                "local-wholesale",
                1,
                isAutomatic: true);
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

    private void RestoreOrder(ProcurementOrderSaveData saved, string parameterName)
    {
        var storeId = NormalizeId(saved.StoreId, parameterName);
        var productId = NormalizeId(saved.ProductId, parameterName);
        var channelId = NormalizeId(saved.ChannelId, parameterName);
        if (!_stock.ContainsOpenStore(storeId)
            || _stock.FindProduct(storeId, productId) is null
            || !_channels.ContainsKey(channelId)
            || saved.Quantity <= 0
            || saved.UnitCostCents <= 0
            || saved.RemainingMinutes < 0
            || saved.Status == ProcurementOrderStatus.Delivered
            || (saved.RemainingMinutes > 0 && saved.Status != ProcurementOrderStatus.InTransit)
            || (saved.RemainingMinutes == 0 && saved.Status != ProcurementOrderStatus.AwaitingSpace))
        {
            throw new ArgumentException(
                $"Restored procurement order '{saved.OrderId}' is invalid.",
                parameterName);
        }

        var product = _stock.FindProduct(storeId, productId)!;
        var existingInbound = _pendingOrders
            .Where(order => string.Equals(order.StoreId, storeId, StringComparison.Ordinal)
                && string.Equals(order.ProductId, productId, StringComparison.Ordinal))
            .Sum(order => order.Quantity);
        if (checked(existingInbound + saved.Quantity) > product.Capacity)
        {
            throw new ArgumentException(
                $"Restored procurement order '{saved.OrderId}' exceeds product capacity.",
                parameterName);
        }

        _pendingOrders.Add(new ProcurementOrder(
            saved.OrderId,
            storeId,
            productId,
            channelId,
            saved.Quantity,
            saved.UnitCostCents,
            saved.RemainingMinutes,
            saved.IsAutomatic,
            saved.Status));
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
