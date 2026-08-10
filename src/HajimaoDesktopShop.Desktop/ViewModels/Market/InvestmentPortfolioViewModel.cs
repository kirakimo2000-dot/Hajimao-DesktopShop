using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Business.Investments;
using HajimaoDesktopShop.Desktop.ViewModels;

namespace HajimaoDesktopShop.Desktop.ViewModels.Market;

public sealed class InvestmentPortfolioViewModel : ObservableObject
{
    private readonly BusinessSession _session;
    private readonly Func<string> _selectedStoreId;
    private readonly Action _refreshMarket;
    private bool _hasLatestInvestment;
    private string _latestInvestmentTitle = "暂无投资记录";
    private string _latestComparisonText = "完成投资后将在这里比较后续完整日结。";
    private string _statusMessage = "投资方案已按当前经营数据计算";

    public InvestmentPortfolioViewModel(
        BusinessSession session,
        Func<string> selectedStoreId,
        Action refreshMarket)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _selectedStoreId = selectedStoreId ?? throw new ArgumentNullException(nameof(selectedStoreId));
        _refreshMarket = refreshMarket ?? throw new ArgumentNullException(nameof(refreshMarket));
        Refresh();
    }

    public event EventHandler<GameFeedbackEventArgs>? FeedbackRaised;

    public ObservableCollection<InvestmentCandidateCardViewModel> Candidates { get; } = [];

    public bool HasLatestInvestment
    {
        get => _hasLatestInvestment;
        private set => SetProperty(ref _hasLatestInvestment, value);
    }

    public string LatestInvestmentTitle
    {
        get => _latestInvestmentTitle;
        private set => SetProperty(ref _latestInvestmentTitle, value);
    }

    public string LatestComparisonText
    {
        get => _latestComparisonText;
        private set => SetProperty(ref _latestComparisonText, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public void Refresh()
    {
        Candidates.Clear();
        var storeId = _selectedStoreId();
        var portfolio = _session.Investments.GetPortfolio(storeId);
        if (portfolio is not null)
        {
            foreach (var candidate in portfolio.Candidates)
            {
                Candidates.Add(new InvestmentCandidateCardViewModel(candidate, Invest));
            }
        }

        RefreshComparison(storeId);
    }

    private void Invest(InvestmentCandidateCardViewModel card)
    {
        var result = _session.Investments.Execute(_selectedStoreId(), card.Id);
        StatusMessage = result.Status switch
        {
            InvestmentCommandStatus.Success => $"投资成功：{card.TitleText}",
            InvestmentCommandStatus.InsufficientFunds => "投资失败：资金不足",
            InvestmentCommandStatus.NotAvailable => "投资失败：前置能力不足",
            InvestmentCommandStatus.UnknownCandidate => "投资方案已经变化，请重新比较",
            _ => $"投资失败：{result.Status}"
        };
        if (result.Status == InvestmentCommandStatus.Success)
        {
            FeedbackRaised?.Invoke(
                this,
                new GameFeedbackEventArgs(
                    result.AppliedCandidate?.Kind == InvestmentKind.Employee
                        ? GameFeedbackKind.EmployeeChanged
                        : GameFeedbackKind.StoreGrowthChanged));
            _refreshMarket();
        }

        Refresh();
    }

    private void RefreshComparison(string storeId)
    {
        var comparison = _session.Investments.GetLatestComparison(storeId);
        HasLatestInvestment = comparison is not null;
        if (comparison is null)
        {
            LatestInvestmentTitle = "暂无投资记录";
            LatestComparisonText = "完成投资后将在这里比较后续完整日结。";
            return;
        }

        LatestInvestmentTitle = $"最近投资：{KindText(comparison.Kind)}";
        LatestComparisonText = comparison.Status switch
        {
            InvestmentComparisonStatus.BaselineUnavailable =>
                "投资前没有完整日结；本次不编造前后差值。",
            InvestmentComparisonStatus.WaitingForCompletedDay =>
                "等待下一份完整日结后比较净利润、成交与流失。",
            _ => string.Format(
                CultureInfo.InvariantCulture,
                "净利润 {0} · 成交 {1:+#;-#;0} · 流失 {2:+#;-#;0}",
                SignedMoney(comparison.NetProfitChangeCents!.Value),
                comparison.CompletedSalesChange,
                comparison.LostSalesChange)
        };
    }

    private static string KindText(InvestmentKind kind) => kind switch
    {
        InvestmentKind.Expansion => "扩建店面",
        InvestmentKind.Shelf => "升级货架",
        InvestmentKind.Decoration => "店铺装修",
        InvestmentKind.Employee => "招聘员工",
        InvestmentKind.OpenStore => "开设新店",
        _ => kind.ToString()
    };

    private static string SignedMoney(long cents)
    {
        var sign = cents > 0 ? "+" : cents < 0 ? "-" : string.Empty;
        var absoluteCents = cents == long.MinValue ? (decimal)long.MaxValue + 1 : Math.Abs(cents);
        return string.Format(
            CultureInfo.InvariantCulture,
            "{0}¥{1:N2}",
            sign,
            absoluteCents / 100m);
    }
}
