using System.Collections.ObjectModel;

namespace HajimaoDesktopShop.Desktop.ViewModels.Market;

public sealed class MarketOverviewViewModel
{
    public ObservableCollection<StoreNavigationItemViewModel> Stores { get; } = [];

    internal void Synchronize(IEnumerable<StoreNavigationItemViewModel> stores)
    {
        Stores.Clear();
        foreach (var store in stores)
        {
            Stores.Add(store);
        }
    }
}
