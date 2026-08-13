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
    private string _performanceHeadlineText = "等待第一份经营结果";
    private string _performanceDetailText = "店铺正在自动经营";
    private string _reasonHeadlineText = "主要原因：等待首批经营数据";
    private string _reasonDetailText = "完成日结后会说明收入、成本与现金压力";

    public string PeriodText { get => _periodText; private set => SetProperty(ref _periodText, value); }
    public string RevenueText { get => _revenueText; private set => SetProperty(ref _revenueText, value); }
    public string GrossProfitText { get => _grossProfitText; private set => SetProperty(ref _grossProfitText, value); }
    public string WageCostText { get => _wageCostText; private set => SetProperty(ref _wageCostText, value); }
    public string OperatingCostText { get => _operatingCostText; private set => SetProperty(ref _operatingCostText, value); }
    public string NetProfitText { get => _netProfitText; private set => SetProperty(ref _netProfitText, value); }
    public string CashRunwayText { get => _cashRunwayText; private set => SetProperty(ref _cashRunwayText, value); }
    public string CustomerFlowText { get => _customerFlowText; private set => SetProperty(ref _customerFlowText, value); }
    public string BottleneckText { get => _bottleneckText; private set => SetProperty(ref _bottleneckText, value); }
    public string PerformanceHeadlineText { get => _performanceHeadlineText; private set => SetProperty(ref _performanceHeadlineText, value); }
    public string PerformanceDetailText { get => _performanceDetailText; private set => SetProperty(ref _performanceDetailText, value); }
    public string ReasonHeadlineText { get => _reasonHeadlineText; private set => SetProperty(ref _reasonHeadlineText, value); }
    public string ReasonDetailText { get => _reasonDetailText; private set => SetProperty(ref _reasonDetailText, value); }

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
        PerformanceHeadlineText = FormatPerformanceHeadline(analysis);
        PerformanceDetailText =
            $"收入 {RevenueText} · 毛利 {GrossProfitText} · {CustomerFlowText}";
        ReasonHeadlineText = $"主要原因：{BottleneckText}";
        ReasonDetailText =
            $"工资 {WageCostText} · 运营成本 {OperatingCostText} · 现金续航 {CashRunwayText}";
    }

    private static string FormatPerformanceHeadline(StoreEconomyAnalysis analysis)
    {
        var period = analysis.Period == "CompletedDay" ? "昨日" : "开店以来";
        return analysis.NetProfitCents switch
        {
            > 0 => $"{period}净赚 {FormatMoney(analysis.NetProfitCents)}",
            < 0 => $"{period}亏损 {FormatMoney(-analysis.NetProfitCents)}",
            _ => $"{period}盈亏平衡"
        };
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
