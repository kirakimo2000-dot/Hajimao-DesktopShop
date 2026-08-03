using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using HajimaoDesktopShop.Application.Game;

namespace HajimaoDesktopShop.Desktop.ViewModels;

public sealed class ProductItemViewModel : ObservableObject
{
    private int _quantity;
    private int _capacity;
    private long _wholesalePriceCents;
    private long _salePriceCents;
    private string _stockStatusText = string.Empty;

    public ProductItemViewModel(ProductSnapshot snapshot)
    {
        Id = snapshot.Id;
        Name = snapshot.Name;
        ShelfKind = snapshot.ShelfKind;
        Update(snapshot);
    }

    public string Id { get; }

    public string Name { get; }

    public string ShelfKind { get; }

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

    public long WholesalePriceCents
    {
        get => _wholesalePriceCents;
        private set
        {
            if (SetProperty(ref _wholesalePriceCents, value))
            {
                OnPropertyChanged(nameof(WholesalePriceText));
                OnPropertyChanged(nameof(UnitProfitText));
            }
        }
    }

    public long SalePriceCents
    {
        get => _salePriceCents;
        private set
        {
            if (SetProperty(ref _salePriceCents, value))
            {
                OnPropertyChanged(nameof(SalePriceText));
                OnPropertyChanged(nameof(UnitProfitText));
            }
        }
    }

    public string StockStatusText
    {
        get => _stockStatusText;
        private set => SetProperty(ref _stockStatusText, value);
    }

    public string InventoryText => $"{Quantity}/{Capacity}";

    public string WholesalePriceText => FormatMoney(WholesalePriceCents);

    public string SalePriceText => FormatMoney(SalePriceCents);

    public string UnitProfitText => FormatMoney(SalePriceCents - WholesalePriceCents);

    public void Update(ProductSnapshot snapshot)
    {
        Quantity = snapshot.Quantity;
        Capacity = snapshot.Capacity;
        WholesalePriceCents = snapshot.WholesalePriceCents;
        SalePriceCents = snapshot.SalePriceCents;
        StockStatusText = snapshot.Quantity switch
        {
            0 => "已缺货",
            _ when snapshot.Quantity * 4 < snapshot.Capacity => "库存偏低",
            _ => "库存充足"
        };
        OnPropertyChanged(nameof(InventoryText));
    }

    private static string FormatMoney(long cents) =>
        string.Format(CultureInfo.InvariantCulture, "¥{0:N2}", cents / 100m);
}
