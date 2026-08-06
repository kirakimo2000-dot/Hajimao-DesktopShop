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
            TimeSpan.FromMilliseconds(125),
            RefreshCadencePolicy.GetInterval(managementOpen));
    }
}
