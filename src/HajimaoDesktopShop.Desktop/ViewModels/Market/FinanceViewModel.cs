using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using HajimaoDesktopShop.Application.Business;

namespace HajimaoDesktopShop.Desktop.ViewModels.Market;

public sealed class FinanceViewModel : ObservableObject
{
    private readonly BusinessSession _session;
    private readonly Func<string> _selectedStoreId;
    private string _revenueText = "¥0.00";
    private string _stockCostText = "¥0.00";
    private string _grossProfitText = "¥0.00";
    private string _wageCostText = "¥0.00";
    private string _operatingCostText = "¥0.00";
    private string _netProfitText = "¥0.00";
    private string _latestDayText = "尚无完整日报";

    public FinanceViewModel(BusinessSession session, Func<string> selectedStoreId)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(selectedStoreId);
        _session = session;
        _selectedStoreId = selectedStoreId;
        Refresh();
    }

    public string RevenueText { get => _revenueText; private set => SetProperty(ref _revenueText, value); }
    public string StockCostText { get => _stockCostText; private set => SetProperty(ref _stockCostText, value); }
    public string GrossProfitText { get => _grossProfitText; private set => SetProperty(ref _grossProfitText, value); }
    public string WageCostText { get => _wageCostText; private set => SetProperty(ref _wageCostText, value); }
    public string OperatingCostText { get => _operatingCostText; private set => SetProperty(ref _operatingCostText, value); }
    public string NetProfitText { get => _netProfitText; private set => SetProperty(ref _netProfitText, value); }
    public string LatestDayText { get => _latestDayText; private set => SetProperty(ref _latestDayText, value); }

    public void Refresh()
    {
        var snapshot = _session.Simulation.GetSnapshot();
        var storeId = _selectedStoreId();
        var store = snapshot.Business.Stores.SingleOrDefault(item => item.Id == storeId);
        if (store is null)
        {
            return;
        }

        RevenueText = FormatMoney(store.RevenueCents);
        StockCostText = FormatMoney(store.StockPurchaseCostCents);
        GrossProfitText = FormatMoney(store.GrossProfitCents);
        WageCostText = FormatMoney(store.WageCostCents);
        OperatingCostText = FormatMoney(store.OperatingCostCents);
        NetProfitText = FormatMoney(store.NetProfitCents);

        var report = snapshot.LastCompletedDay?.Stores.SingleOrDefault(item => item.StoreId == storeId);
        LatestDayText = report is null
            ? "尚无完整日报"
            : $"昨日：顾客 {report.Visitors} · 成交 {report.CompletedSales} · 流失 {report.LostSales}";
    }

    private static string FormatMoney(long cents)
    {
        var absolute = Math.Abs(cents) / 100m;
        return string.Format(
            CultureInfo.InvariantCulture,
            cents < 0 ? "-¥{0:N2}" : "¥{0:N2}",
            absolute);
    }
}
