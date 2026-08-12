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
                "靠全天稳定客流与均衡商品持续赚钱",
                "高峰排队和缺货会慢慢吃掉薄利",
                "适合稳健起步，先把一家店经营顺畅"),
            "discount" => new(
                "靠低价走量与更大的库存周转赚钱",
                "备货占用现金，卖不动时压力来得更快",
                "适合愿意承受现金波动、追求规模的人"),
            "premium" => new(
                "靠高毛利商品与优质服务赚取单笔回报",
                "客流较少，服务或整洁不足会明显掉单",
                "适合耐心经营、重视利润质量的人"),
            _ => throw new ArgumentOutOfRangeException(
                nameof(formatId),
                formatId,
                "Unsupported starter store format.")
        };
    }
}
