using CommunityToolkit.Mvvm.ComponentModel;
using HajimaoDesktopShop.Application.Business;

namespace HajimaoDesktopShop.Desktop.ViewModels.Market;

public sealed class StoreNavigationItemViewModel : ObservableObject
{
    private bool _isOpen;

    public StoreNavigationItemViewModel(StoreCatalogItemSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        Id = snapshot.Id;
        Name = snapshot.Name;
        RequiredPlayerLevel = snapshot.RequiredPlayerLevel;
        OpeningCostCents = snapshot.OpeningCostCents;
        _isOpen = snapshot.IsOpen;
    }

    public string Id { get; }

    public string Name { get; }

    public int RequiredPlayerLevel { get; }

    public long OpeningCostCents { get; }

    public bool IsOpen
    {
        get => _isOpen;
        private set => SetProperty(ref _isOpen, value);
    }

    internal void Update(StoreCatalogItemSnapshot snapshot) => IsOpen = snapshot.IsOpen;
}
