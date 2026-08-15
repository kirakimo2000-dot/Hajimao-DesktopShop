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

    public string FormatId => Proposal.FormatId;

    public string ReturnProfileText => Proposal.FormatId switch
    {
        "convenience" => "稳定现金流",
        "discount" => "高周转扩张",
        "premium" => "高毛利回报",
        _ => Proposal.FormatName
    };

    public string DecisionPromptText => Proposal.FormatId switch
    {
        "convenience" => "稳健起步",
        "discount" => "用规模换增长",
        "premium" => "用服务换利润",
        _ => "选择投资方向"
    };

    public string EarningText { get; }

    public string RiskText { get; }

    public string FitText { get; }

    public IRelayCommand SelectCommand { get; }
}
