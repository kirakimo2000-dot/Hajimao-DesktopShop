using HajimaoDesktopShop.Desktop.Services;

namespace HajimaoDesktopShop.Desktop.Tests.Services;

public sealed class DesktopGameContentTests
{
    [Fact]
    public void LevelCurve_ReachesTheLevelTenCommercialBlockUnlock()
    {
        Assert.Equal(10, DesktopGameContent.LevelCurve.MaximumLevel);
        Assert.Equal(10, DesktopGameContent.LevelCurve.GetLevel(7_500));
    }

    [Fact]
    public void EconomyConstants_AreNamedAtTheProductionCompositionRoot()
    {
        Assert.True(DesktopGameContent.OpeningCashCents > 0);
        Assert.True(DesktopGameContent.ExperiencePerItemSold > 0);
        Assert.InRange(DesktopGameContent.BaseArrivalBasisPoints, 1, 10_000);
        Assert.Equal([0, 80_000, 120_000], DesktopGameContent.ShopOpeningCostsCents);
        Assert.Equal(10, DesktopGameContent.LevelThresholds.Count);
    }
}
