using HajimaoDesktopShop.Application.Business.StorePortfolio;
using HajimaoDesktopShop.Desktop.ViewModels.Market;

namespace HajimaoDesktopShop.Desktop.Tests.ViewModels.Market;

public sealed class StarterStoreChoiceViewModelTests
{
    [Fact]
    public void Constructor_ProjectsExactlyThreeReadableChoicesWithoutFormulaText()
    {
        var viewModel = new StarterStoreChoiceViewModel(CreateProposals());

        Assert.Equal(3, viewModel.Choices.Count);
        Assert.Equal(
            ["社区便利", "平价量贩", "精品食品"],
            viewModel.Choices.Select(choice => choice.FormatName));
        Assert.Equal(3, viewModel.Choices.Select(choice => choice.EarningText).Distinct().Count());
        Assert.Equal(3, viewModel.Choices.Select(choice => choice.RiskText).Distinct().Count());
        Assert.All(viewModel.Choices, choice =>
        {
            Assert.NotEmpty(choice.FitText);
            Assert.DoesNotContain("%", choice.EarningText, StringComparison.Ordinal);
            Assert.DoesNotContain("倍率", choice.EarningText, StringComparison.Ordinal);
            Assert.DoesNotContain("permille", choice.EarningText, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void SelectCommand_RecordsProposalAndCompletesOnlyOnce()
    {
        var viewModel = new StarterStoreChoiceViewModel(CreateProposals());
        var completionCount = 0;
        viewModel.SelectionCompleted += (_, _) => completionCount++;

        viewModel.Choices[1].SelectCommand.Execute(null);
        viewModel.Choices[2].SelectCommand.Execute(null);

        Assert.Equal("aldi", viewModel.SelectedProposal?.BrandId);
        Assert.Equal(1, completionCount);
    }

    private static IReadOnlyList<StoreOpeningProposal> CreateProposals() =>
    [
        Proposal("seven-eleven", "7-Eleven", "convenience", "社区便利", 40_000),
        Proposal("aldi", "ALDI", "discount", "平价量贩", 70_000),
        Proposal("ginza-mitsukoshi", "银座三越", "premium", "精品食品", 55_000)
    ];

    private static StoreOpeningProposal Proposal(
        string brandId,
        string brandName,
        string formatId,
        string formatName,
        long reserveCents) =>
        new(
            "store-0001",
            1,
            brandId,
            brandName,
            formatId,
            formatName,
            0,
            reserveCents,
            120_000,
            true);
}
