using CommunityToolkit.Mvvm.Input;
using HajimaoDesktopShop.Application.Business.StorePortfolio;

namespace HajimaoDesktopShop.Desktop.ViewModels.Market;

public sealed class StarterStoreChoiceCardViewModel
{
    internal StarterStoreChoiceCardViewModel(
        StoreOpeningProposal proposal,
        string earningText,
        string riskText,
        string fitText,
        Action<StarterStoreChoiceCardViewModel> select)
    {
        Proposal = proposal ?? throw new ArgumentNullException(nameof(proposal));
        EarningText = earningText;
        RiskText = riskText;
        FitText = fitText;
        SelectCommand = new RelayCommand(() => select(this));
    }

    internal StoreOpeningProposal Proposal { get; }

    public string BrandName => Proposal.BrandName;

    public string FormatName => Proposal.FormatName;

    public string EarningText { get; }

    public string RiskText { get; }

    public string FitText { get; }

    public IRelayCommand SelectCommand { get; }
}
