using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using HajimaoDesktopShop.Application.Business.Combat;
using HajimaoDesktopShop.Application.Business.StorePortfolio;

namespace HajimaoDesktopShop.Desktop.ViewModels.Market;

public sealed class InvestmentCandidateCardViewModel
{
    internal InvestmentCandidateCardViewModel(
        StoreOpeningProposal proposal,
        Action<InvestmentCandidateCardViewModel> invest)
    {
        Proposal = proposal ?? throw new ArgumentNullException(nameof(proposal));
        ArgumentNullException.ThrowIfNull(invest);
        InvestCommand = new RelayCommand(
            () => invest(this),
            () => Proposal.CashAfterOpeningCents >= 0);
    }

    internal StoreOpeningProposal Proposal { get; }

    public string Id => $"store:open:{Proposal.ProspectiveStoreId}:{Proposal.BrandId}";
    public string BrandId => Proposal.BrandId;
    public string ThesisText => "扩张街区";
    public string StoreContextText => FormatName(Proposal.FormatId);
    public string TitleText => $"开设 {Proposal.BrandName}";
    public string CostText => $"投入 {FormatMoney(Proposal.OpeningCostCents)}";
    public string ExpectedBenefitText
    {
        get
        {
            var profile = StoreCombatProfilePolicy.Resolve(Proposal.FormatId);
            return $"{profile.ProfitStyleText} · {profile.RiskText}";
        }
    }
    public string PaybackText => "实际回报由挂机招待效率和商品组合决定";
    public string CashAfterText => $"开店后现金 {FormatMoney(Proposal.CashAfterOpeningCents)}";
    public string CashPressureText => Proposal.CashAfterOpeningCents < 0
        ? "现金不足，暂时无法开店"
        : Proposal.HasRecommendedReserve
            ? "现金储备健康"
            : "现金偏紧：建议先继续挂机";
    public string EffectText => "新增独立战斗店铺 · 共享商品图鉴";
    public string EstimateConditionText => "新店拥有独立装备组合、客流与累计收益";
    public string AvailabilityText => Proposal.CashAfterOpeningCents >= 0 ? "可开店" : "资金不足";
    public IRelayCommand InvestCommand { get; }

    private static string FormatName(string formatId) => formatId switch
    {
        "discount" => "折扣店 · 高客流",
        "premium" => "精品店 · 高收益",
        "convenience" => "便利店 · 均衡",
        _ => "新店选择"
    };

    private static string FormatMoney(long cents) =>
        string.Format(CultureInfo.InvariantCulture, "¥{0:N2}", cents / 100m);
}
