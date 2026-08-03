using HajimaoDesktopShop.Desktop.Services;

namespace HajimaoDesktopShop.Desktop.Tests.Services;

public sealed class RefreshCadencePolicyTests
{
    [Theory]
    [InlineData(false, 2000)]
    [InlineData(true, 250)]
    public void GetInterval_UsesLowFrequencyForDesktopOnly(bool managementOpen, int expectedMilliseconds)
    {
        Assert.Equal(
            TimeSpan.FromMilliseconds(expectedMilliseconds),
            RefreshCadencePolicy.GetInterval(managementOpen));
    }
}
