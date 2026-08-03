using HajimaoDesktopShop.Application.Persistence;
using HajimaoDesktopShop.Application.Simulation;

namespace HajimaoDesktopShop.Application.Tests.Simulation;

public sealed class FixedTimeContractTests
{
    [Fact]
    public void PublicContracts_DoNotExposePlayerControlledSpeed()
    {
        Assert.Null(typeof(ShopSimulation).GetMethod("SetSpeed"));
        Assert.Null(typeof(SimulationSnapshot).GetProperty("Speed"));
        Assert.DoesNotContain(
            typeof(SimulationSaveData).GetProperties(),
            property => property.Name == "Speed");
    }

    [Fact]
    public void OneRealSecond_AlwaysAdvancesExactlyOneGameMinute()
    {
        var processedTicks = 0;
        var clock = new SimulationClock();

        clock.AdvanceRealSecond(() => processedTicks++);

        Assert.Equal(1, processedTicks);
        Assert.Equal(1, clock.GameMinute);
    }
}
