using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace HajimaoDesktopShop.Desktop.ViewModels.Market;

public sealed class DesktopNavigationViewModel : ObservableObject
{
    private readonly Action<string> _selectStore;
    private IReadOnlyDictionary<string, StoreNavigationItemViewModel> _stores =
        new Dictionary<string, StoreNavigationItemViewModel>(StringComparer.Ordinal);
    private DesktopSurfaceMode _mode = DesktopSurfaceMode.Street;
    private string _pageTitle = "街区";

    public DesktopNavigationViewModel(Action<string> selectStore)
    {
        _selectStore = selectStore ?? throw new ArgumentNullException(nameof(selectStore));
        OpenStoreCommand = new RelayCommand<string>(OpenStore);
        BackToStreetCommand = new RelayCommand(BackToStreet);
    }

    public DesktopSurfaceMode Mode
    {
        get => _mode;
        private set
        {
            if (SetProperty(ref _mode, value))
            {
                OnPropertyChanged(nameof(IsStreet));
                OnPropertyChanged(nameof(IsStore));
            }
        }
    }

    public bool IsStreet => Mode == DesktopSurfaceMode.Street;

    public bool IsStore => Mode == DesktopSurfaceMode.Store;

    public string PageTitle
    {
        get => _pageTitle;
        private set => SetProperty(ref _pageTitle, value);
    }

    public IRelayCommand<string> OpenStoreCommand { get; }

    public IRelayCommand BackToStreetCommand { get; }

    public void Synchronize(
        IEnumerable<StoreNavigationItemViewModel> stores,
        string selectedStoreId)
    {
        ArgumentNullException.ThrowIfNull(stores);
        _stores = stores.ToDictionary(store => store.Id, StringComparer.Ordinal);
        var hasOpenSelection = _stores.TryGetValue(selectedStoreId, out var selectedStore)
            && selectedStore.IsOpen;
        if (Mode == DesktopSurfaceMode.Store && !hasOpenSelection)
        {
            BackToStreet();
        }
        else if (Mode == DesktopSurfaceMode.Store && selectedStore is not null)
        {
            PageTitle = selectedStore.Name;
        }
    }

    public void ShowStreet() => BackToStreet();

    private void OpenStore(string? storeId)
    {
        if (storeId is null || !_stores.TryGetValue(storeId, out var store) || !store.IsOpen)
        {
            return;
        }

        _selectStore(store.Id);
        PageTitle = store.Name;
        Mode = DesktopSurfaceMode.Store;
    }

    private void BackToStreet()
    {
        Mode = DesktopSurfaceMode.Street;
        PageTitle = "街区";
    }
}
