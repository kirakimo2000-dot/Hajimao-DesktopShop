using HajimaoDesktopShop.Application.Simulation;

namespace HajimaoDesktopShop.Application.Tests.Simulation;

public sealed class SimulationClockTests
{
    [Fact]
    public void AdvanceRealSecond_ExecutesExactlyOneTick()
    {
        var clock = new SimulationClock();
        var processedTicks = 0;

        clock.AdvanceRealSecond(() => processedTicks++);

        Assert.Equal(1, processedTicks);
        Assert.Equal(1, clock.GameMinute);
    }

    [Fact]
    public void Constructor_WithNegativeGameMinute_IsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SimulationClock(-1));
    }
}
