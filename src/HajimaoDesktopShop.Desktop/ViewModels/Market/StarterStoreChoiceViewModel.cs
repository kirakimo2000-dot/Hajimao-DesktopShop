using HajimaoDesktopShop.Application.Business.StorePortfolio;

namespace HajimaoDesktopShop.Desktop.ViewModels.Market;

public sealed class StarterStoreChoiceViewModel
{
    public StarterStoreChoiceViewModel(IReadOnlyList<StoreOpeningProposal> proposals)
    {
        ArgumentNullException.ThrowIfNull(proposals);
        if (proposals.Count != 3)
        {
            throw new ArgumentException("Exactly three starter store proposals are required.", nameof(proposals));
        }

        Choices = Array.AsReadOnly(proposals.Select(CreateCard).ToArray());
    }

    public event EventHandler? SelectionCompleted;

    public IReadOnlyList<StarterStoreChoiceCardViewModel> Choices { get; }

    public StoreOpeningProposal? SelectedProposal { get; private set; }

    private StarterStoreChoiceCardViewModel CreateCard(StoreOpeningProposal proposal)
    {
        var copy = StarterStoreCopy.ForFormat(proposal.FormatId);
        return new StarterStoreChoiceCardViewModel(
            proposal,
            copy.Earning,
            copy.Risk,
            copy.Fit,
            Select);
    }

    private void Select(StarterStoreChoiceCardViewModel choice)
    {
        if (SelectedProposal is not null)
        {
            return;
        }

        SelectedProposal = choice.Proposal;
        SelectionCompleted?.Invoke(this, EventArgs.Empty);
    }

    private sealed record StarterStoreCopy(string Earning, string Risk, string Fit)
    {
        public static StarterStoreCopy ForFormat(string formatId) => formatId switch
        {
            "convenience" => new(
                "客流、顾客需求与单次收益均衡",
                "没有明显短板，成长速度稳定",
                "适合第一次体验挂机战斗"),
            "discount" => new(
                "顾客来得更多，靠招待数量积累收益",
                "单次收益较低且顾客移动更快，容易漏客",
                "适合偏重攻击速度与群体效果的商品组合"),
            "premium" => new(
                "顾客较少，但每次成功招待的收益更高",
                "顾客需求更高，威力不足时更容易失败",
                "适合高威力与高收益倍率的商品组合"),
            _ => throw new ArgumentOutOfRangeException(
                nameof(formatId),
                formatId,
                "Unsupported starter store format.")
        };
    }
}
