using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Business.Procurement;
using HajimaoDesktopShop.Desktop.ViewModels;
using HajimaoDesktopShop.Domain.Shops;

namespace HajimaoDesktopShop.Desktop.ViewModels.Market;

public sealed class ProductManagementViewModel : ObservableObject
{
    private readonly BusinessSession _session;
    private readonly Func<string> _selectedStoreId;
    private readonly Dictionary<string, ProductManagementItemViewModel> _productsById =
        new(StringComparer.Ordinal);
    private string _statusMessage = "商品与采购已就绪";

    public ProductManagementViewModel(BusinessSession session, Func<string> selectedStoreId)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(selectedStoreId);
        _session = session;
        _selectedStoreId = selectedStoreId;
    }

    public ObservableCollection<ProductManagementItemViewModel> Products { get; } = [];

    public ObservableCollection<ProcurementOrderItemViewModel> PendingOrders { get; } = [];

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public void Refresh()
    {
        var storeId = _selectedStoreId();
        var store = _session.Game.GetSnapshot().Stores.SingleOrDefault(item => item.Id == storeId);
        var procurement = _session.Game.GetProcurementSnapshot();
        var policies = procurement.AutoRestockPolicies
            .Where(policy => policy.StoreId == storeId)
            .ToDictionary(policy => policy.ProductId, StringComparer.Ordinal);

        Products.Clear();
        if (store is not null)
        {
            foreach (var snapshot in store.Products)
            {
                if (!_productsById.TryGetValue(snapshot.Id, out var item))
                {
                    item = new ProductManagementItemViewModel(
                        snapshot,
                        ChangePrice,
                        PlaceOrder,
                        ToggleAutoRestock);
                    _productsById.Add(snapshot.Id, item);
                }

                policies.TryGetValue(snapshot.Id, out var policy);
                item.Update(snapshot, policy);
                Products.Add(item);
            }
        }

        var channels = procurement.Channels.ToDictionary(channel => channel.Id, StringComparer.Ordinal);
        PendingOrders.Clear();
        foreach (var order in procurement.PendingOrders.Where(order => order.StoreId == storeId))
        {
            PendingOrders.Add(new ProcurementOrderItemViewModel(
                order.OrderId,
                order.ProductId,
                channels[order.ChannelId].Name,
                order.Quantity,
                order.RemainingMinutes,
                order.Status,
                order.IsAutomatic));
        }
    }

    private void ChangePrice(ProductManagementItemViewModel product, int deltaCents)
    {
        var result = _session.Game.ChangePrice(
            _selectedStoreId(),
            product.Id,
            checked(product.SalePriceCents + deltaCents));
        StatusMessage = result.Status == PriceChangeStatus.Success
            ? $"{product.Name} 售价已调整"
            : $"调价失败：{result.Status}";
        Refresh();
    }

    private void PlaceOrder(ProductManagementItemViewModel product, string channelId, int quantity)
    {
        var result = _session.Game.PlaceProcurementOrder(
            _selectedStoreId(),
            product.Id,
            channelId,
            quantity);
        StatusMessage = result.Status == ProcurementOrderPlacementStatus.Success
            ? $"{product.Name} 采购单已创建"
            : $"采购失败：{result.Status}";
        Refresh();
    }

    private void ToggleAutoRestock(ProductManagementItemViewModel product)
    {
        _session.Game.ConfigureAutoRestock(new AutoRestockPolicy(
            _selectedStoreId(),
            product.Id,
            !product.IsAutoRestockEnabled,
            Math.Max(1, product.Capacity / 4),
            Math.Max(1, product.Capacity * 4 / 5),
            "regional-distributor",
            UseEmergencySupplierWhenOutOfStock: true));
        StatusMessage = product.IsAutoRestockEnabled ? "自动补货已关闭" : "自动补货已开启";
        Refresh();
    }
}
