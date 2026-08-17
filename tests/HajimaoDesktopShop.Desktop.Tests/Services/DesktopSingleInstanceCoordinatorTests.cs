using HajimaoDesktopShop.Desktop.Services;

namespace HajimaoDesktopShop.Desktop.Tests.Services;

public sealed class DesktopSingleInstanceCoordinatorTests
{
    [Fact]
    public void SecondInstance_SignalsPrimaryWithoutBecomingPrimary()
    {
        var instanceName = $"tests-{Guid.NewGuid():N}";
        using var activationRequested = new ManualResetEventSlim();
        using var primary = new DesktopSingleInstanceCoordinator(
            instanceName,
            activationRequested.Set);
        using var secondary = new DesktopSingleInstanceCoordinator(
            instanceName,
            static () => { });

        Assert.True(primary.IsPrimary);
        Assert.False(secondary.IsPrimary);

        secondary.SignalPrimary();

        Assert.True(
            activationRequested.Wait(TimeSpan.FromSeconds(2)),
            "The primary instance did not receive the activation signal.");
    }
}
