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
}
