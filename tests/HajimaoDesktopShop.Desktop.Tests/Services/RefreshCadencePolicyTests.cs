using HajimaoDesktopShop.Desktop.Services;

namespace HajimaoDesktopShop.Desktop.Tests.Services;

public sealed class RefreshCadencePolicyTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetInterval_UsesDedicatedPresentationCadenceWithoutChangingSimulation(bool managementOpen)
    {
        Assert.Equal(
            TimeSpan.FromSeconds(1d / 24d),
            RefreshCadencePolicy.GetInterval(managementOpen));
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(23, false)]
    [InlineData(24, true)]
    [InlineData(48, true)]
    public void IsManagementRefresh_RebuildsManagementDataOncePerSecond(
        int presentationTick,
        bool expected)
    {
        Assert.Equal(expected, RefreshCadencePolicy.IsManagementRefresh(presentationTick));
    }
}
