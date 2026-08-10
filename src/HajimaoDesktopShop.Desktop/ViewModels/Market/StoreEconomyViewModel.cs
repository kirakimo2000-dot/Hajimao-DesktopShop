using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using HajimaoDesktopShop.Application.Business.Analysis;

namespace HajimaoDesktopShop.Desktop.ViewModels.Market;

public sealed class StoreEconomyViewModel : ObservableObject
{
    private string _periodText = "开店以来";
    private string _revenueText = "¥0.00";
    private string _grossProfitText = "¥0.00 · 0.0%";
    private string _wageCostText = "¥0.00";
    private string _operatingCostText = "¥0.00";
    private string _netProfitText = "¥0.00 · 0.0%";
    private string _cashRunwayText = "尚无可计算支出";
    private string _customerFlowText = "顾客 0 · 成交 0 · 流失 0";
    private string _bottleneckText = "等待首批经营数据";

    public string PeriodText { get => _periodText; private set => SetProperty(ref _periodText, value); }
    public string RevenueText { get => _revenueText; private set => SetProperty(ref _revenueText, value); }
    public string GrossProfitText { get => _grossProfitText; private set => SetProperty(ref _grossProfitText, value); }
    public string WageCostText { get => _wageCostText; private set => SetProperty(ref _wageCostText, value); }
    public string OperatingCostText { get => _operatingCostText; private set => SetProperty(ref _operatingCostText, value); }
    public string NetProfitText { get => _netProfitText; private set => SetProperty(ref _netProfitText, value); }
    public string CashRunwayText { get => _cashRunwayText; private set => SetProperty(ref _cashRunwayText, value); }
    public string CustomerFlowText { get => _customerFlowText; private set => SetProperty(ref _customerFlowText, value); }
    public string BottleneckText { get => _bottleneckText; private set => SetProperty(ref _bottleneckText, value); }

    public void Update(StoreEconomyAnalysis analysis)
    {
        ArgumentNullException.ThrowIfNull(analysis);
        PeriodText = analysis.Period == "CompletedDay" ? "昨日经营" : "开店以来";
        RevenueText = FormatMoney(analysis.RevenueCents);
        GrossProfitText = $"{FormatMoney(analysis.GrossProfitCents)} · {FormatPercent(analysis.GrossMarginBasisPoints)}";
        WageCostText = FormatMoney(analysis.WageCostCents);
        OperatingCostText = FormatMoney(analysis.OperatingCostCents);
        NetProfitText = $"{FormatMoney(analysis.NetProfitCents)} · {FormatPercent(analysis.NetMarginBasisPoints)}";
        CashRunwayText = analysis.CashRunwayTenthsOfDay == 0
            ? "尚无可计算支出"
            : string.Format(
                CultureInfo.InvariantCulture,
                "{0:0.0} 天",
                analysis.CashRunwayTenthsOfDay / 10m);
        CustomerFlowText = $"顾客 {analysis.Visitors} · 成交 {analysis.CompletedSales} · 流失 {analysis.LostSales}";
        BottleneckText = FormatBottleneck(analysis.Bottleneck);
    }

    private static string FormatMoney(long cents)
    {
        var absolute = Math.Abs(cents) / 100m;
        return string.Format(
            CultureInfo.InvariantCulture,
            cents < 0 ? "-¥{0:N2}" : "¥{0:N2}",
            absolute);
    }

    private static string FormatPercent(int basisPoints) =>
        string.Format(CultureInfo.InvariantCulture, "{0:0.0}%", basisPoints / 100m);

    private static string FormatBottleneck(StoreBottleneck bottleneck) => bottleneck switch
    {
        StoreBottleneck.InsufficientData => "等待首批经营数据",
        StoreBottleneck.Stock => "库存不足正在损失订单",
        StoreBottleneck.Checkout => "收银能力不足正在造成排队流失",
        StoreBottleneck.Service => "服务效率偏低正在压制成交",
        StoreBottleneck.Cost => "固定成本正在侵蚀利润",
        StoreBottleneck.Demand => "客流或成交需求不足",
        StoreBottleneck.None => "当前没有明显经营瓶颈",
        _ => throw new ArgumentOutOfRangeException(nameof(bottleneck))
    };
}
