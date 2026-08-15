using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Business.Combat;
using HajimaoDesktopShop.Application.Business.StorePortfolio;

namespace HajimaoDesktopShop.Desktop.ViewModels.Market;

public sealed class InvestmentPortfolioViewModel : ObservableObject
{
    private readonly BusinessSession _session;
    private readonly Func<string> _selectedStoreId;
    private readonly Action _refreshMarket;
    private string _statusMessage = "选择不同店型，承担不同收益与客流风险。";
    private string _nextInvestmentTitle = "选择下一家店";
    private string _nextInvestmentDetailText = "新店独立挂机，商品图鉴跨店共享。";

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

    public ObservableCollection<InvestmentCandidateCardViewModel> Candidates { get; } = [];
    public bool HasLatestInvestment => false;
    public string LatestInvestmentTitle => "";
    public string LatestComparisonText => "";

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string NextInvestmentTitle
    {
        get => _nextInvestmentTitle;
        private set => SetProperty(ref _nextInvestmentTitle, value);
    }

    public string NextInvestmentDetailText
    {
        get => _nextInvestmentDetailText;
        private set => SetProperty(ref _nextInvestmentDetailText, value);
    }

    public void Refresh()
    {
        Candidates.Clear();
        foreach (var candidate in _session.CombatExpansion?.GetProposals().Take(3)
                     ?? Enumerable.Empty<StoreOpeningProposal>())
        {
            Candidates.Add(new InvestmentCandidateCardViewModel(candidate, Invest));
        }

        if (Candidates.Count == 0)
        {
            NextInvestmentTitle = "街区店铺已全部开放";
            NextInvestmentDetailText = "继续强化各店的商品组合与挂机收益。";
            return;
        }

        NextInvestmentTitle = "选择下一家店";
        NextInvestmentDetailText = "比较店型收益、客流风险与开店后的现金储备。";
    }

    private void Invest(InvestmentCandidateCardViewModel card)
    {
        var result = _session.CombatExpansion?.Open(card.BrandId)
            ?? new CombatStoreExpansionResult(CombatStoreExpansionStatus.NotAvailable);
        StatusMessage = result.Status switch
        {
            CombatStoreExpansionStatus.Success => $"开店成功：{card.TitleText}",
            CombatStoreExpansionStatus.InsufficientFunds => "开店失败：资金不足",
            CombatStoreExpansionStatus.NotAvailable => "开店失败：当前条件未满足",
            _ => "开店方案已经变化，请重新选择"
        };
        if (result.Status == CombatStoreExpansionStatus.Success)
        {
            _refreshMarket();
        }

        Refresh();
    }
}
