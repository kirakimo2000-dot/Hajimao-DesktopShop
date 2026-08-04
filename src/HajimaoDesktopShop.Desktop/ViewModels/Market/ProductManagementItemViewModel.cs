using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HajimaoDesktopShop.Application.Business.Procurement;
using HajimaoDesktopShop.Application.Game;

namespace HajimaoDesktopShop.Desktop.ViewModels.Market;

public sealed class ProductManagementItemViewModel : ObservableObject
{
    private long _salePriceCents;
    private int _quantity;
    private int _capacity;
    private long _unitGrossProfitCents;
    private int _grossMarginBasisPoints;
    private bool _isAutoRestockEnabled;
    private int _reorderPoint;
    private int _targetQuantity;

    internal ProductManagementItemViewModel(
        ProductSnapshot snapshot,
        Action<ProductManagementItemViewModel, int> changePrice,
        Action<ProductManagementItemViewModel, string, int> order,
        Action<ProductManagementItemViewModel> toggleAutoRestock)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        Id = snapshot.Id;
        Name = snapshot.Name;
        WholesalePriceCents = snapshot.WholesalePriceCents;
        ShelfKind = snapshot.ShelfKind;
        IncreasePriceCommand = new RelayCommand(() => changePrice(this, 10));
        DecreasePriceCommand = new RelayCommand(() => changePrice(this, -10));
        OrderLocalCommand = new RelayCommand(() => order(this, "local-wholesale", 5));
        OrderRegionalCommand = new RelayCommand(() => order(this, "regional-distributor", 6));
        OrderDirectCommand = new RelayCommand(() => order(this, "direct-manufacturer", 24));
        ToggleAutoRestockCommand = new RelayCommand(() => toggleAutoRestock(this));
        Update(snapshot, null);
    }

    public string Id { get; }

    public string Name { get; }

    public long WholesalePriceCents { get; }

    public string ShelfKind { get; }

    public IRelayCommand IncreasePriceCommand { get; }

    public IRelayCommand DecreasePriceCommand { get; }

    public IRelayCommand OrderLocalCommand { get; }

    public IRelayCommand OrderRegionalCommand { get; }

    public IRelayCommand OrderDirectCommand { get; }

    public IRelayCommand ToggleAutoRestockCommand { get; }

    public long SalePriceCents
    {
        get => _salePriceCents;
        private set => SetProperty(ref _salePriceCents, value);
    }

    public int Quantity
    {
        get => _quantity;
        private set => SetProperty(ref _quantity, value);
    }

    public int Capacity
    {
        get => _capacity;
        private set => SetProperty(ref _capacity, value);
    }

    public long UnitGrossProfitCents
    {
        get => _unitGrossProfitCents;
        private set => SetProperty(ref _unitGrossProfitCents, value);
    }

    public string GrossMarginText =>
        string.Format(CultureInfo.InvariantCulture, "{0:0.0}%", _grossMarginBasisPoints / 100m);

    public bool IsAutoRestockEnabled
    {
        get => _isAutoRestockEnabled;
        private set => SetProperty(ref _isAutoRestockEnabled, value);
    }

    public int ReorderPoint
    {
        get => _reorderPoint;
        private set => SetProperty(ref _reorderPoint, value);
    }

    public int TargetQuantity
    {
        get => _targetQuantity;
        private set => SetProperty(ref _targetQuantity, value);
    }

    internal void Update(ProductSnapshot snapshot, AutoRestockPolicy? policy)
    {
        SalePriceCents = snapshot.SalePriceCents;
        Quantity = snapshot.Quantity;
        Capacity = snapshot.Capacity;
        UnitGrossProfitCents = snapshot.UnitGrossProfitCents;
        if (_grossMarginBasisPoints != snapshot.GrossMarginBasisPoints)
        {
            _grossMarginBasisPoints = snapshot.GrossMarginBasisPoints;
            OnPropertyChanged(nameof(GrossMarginText));
        }

        IsAutoRestockEnabled = policy?.IsEnabled ?? false;
        ReorderPoint = policy?.ReorderPoint ?? Math.Max(1, snapshot.Capacity / 4);
        TargetQuantity = policy?.TargetQuantity ?? Math.Max(1, snapshot.Capacity * 4 / 5);
    }
}
