using HajimaoDesktopShop.Application.Business.Combat;
using HajimaoDesktopShop.Desktop.ViewModels.Market;
using HajimaoDesktopShop.Domain.Combat;

namespace HajimaoDesktopShop.Desktop.Tests.ViewModels.Market;

public sealed class NextActionViewModelTests
{
    [Theory]
    [InlineData(0, 0, false, "等待毛毛完成首位顾客", ManagementSection.Overview)]
    [InlineData(2, 0, false, "等待第一件商品掉落", ManagementSection.Overview)]
    [InlineData(2, 1, false, "调整商品组合", ManagementSection.Strategy)]
    [InlineData(2, 1, true, "比较下一家店", ManagementSection.Investment)]
    public void Update_GivesOnePlainCombatNextStep(
        int served,
        int drops,
        bool canOpenStore,
        string expectedTitle,
        ManagementSection expectedSection)
    {
        var viewModel = new NextActionViewModel();

        viewModel.Update(Snapshot(served, drops), "corner-store", canOpenStore);

        Assert.Equal(expectedTitle, viewModel.Title);
        Assert.Equal(expectedSection, viewModel.SuggestedSection);
        Assert.DoesNotContain("报告", viewModel.DetailText, StringComparison.Ordinal);
        Assert.DoesNotContain("补货", viewModel.DetailText, StringComparison.Ordinal);
    }

    private static BusinessCombatSnapshot Snapshot(int served, int drops) => new(
        1_000,
        [new StoreCombatSnapshot(
            "corner-store", StoreCombatState.Empty, [], [], served * 100, served, 0, drops)],
        [],
        []);
}
