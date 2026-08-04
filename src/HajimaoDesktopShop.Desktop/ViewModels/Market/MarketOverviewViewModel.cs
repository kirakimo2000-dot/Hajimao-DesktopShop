using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Domain.Shops;

namespace HajimaoDesktopShop.Desktop.ViewModels.Market;

public sealed class MarketOverviewViewModel : ObservableObject
{
    private readonly BusinessGameService _game;
    private readonly Action _refreshOwner;
    private string _statusMessage = "店铺经营中";

    public MarketOverviewViewModel(BusinessGameService game, Action refreshOwner)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(refreshOwner);
        _game = game;
        _refreshOwner = refreshOwner;
        OpenStoreCommand = new RelayCommand<StoreNavigationItemViewModel>(OpenStore);
    }

    public ObservableCollection<StoreNavigationItemViewModel> Stores { get; } = [];

    public IRelayCommand<StoreNavigationItemViewModel> OpenStoreCommand { get; }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    internal void Synchronize(IEnumerable<StoreNavigationItemViewModel> stores)
    {
        Stores.Clear();
        foreach (var store in stores)
        {
            Stores.Add(store);
        }
    }

    private void OpenStore(StoreNavigationItemViewModel? store)
    {
        if (store is null || store.IsOpen)
        {
            return;
        }

        var result = _game.OpenStore(store.Id);
        StatusMessage = result.Status switch
        {
            OpenShopStatus.Success => $"{store.Name} 已开业",
            OpenShopStatus.LevelLocked => $"需要达到 Lv.{store.RequiredPlayerLevel}",
            OpenShopStatus.InsufficientFunds => "资金不足，暂时无法开店",
            _ => $"开店失败：{result.Status}"
        };
        _refreshOwner();
    }
}
