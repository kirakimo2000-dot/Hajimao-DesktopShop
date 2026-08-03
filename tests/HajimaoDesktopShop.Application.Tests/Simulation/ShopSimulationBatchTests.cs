using HajimaoDesktopShop.Application.Catalog;
using HajimaoDesktopShop.Application.Game;
using HajimaoDesktopShop.Application.Simulation;

namespace HajimaoDesktopShop.Application.Tests.Simulation;

public sealed class ShopSimulationBatchTests
{
    [Fact]
    public void AdvanceRealSeconds_ProcessesOneTickPerSecond()
    {
        var simulation = CreateSimulation();

        simulation.AdvanceRealSeconds(30);

        Assert.Equal(30, simulation.GetSnapshot().GameMinute);
    }

    [Fact]
    public void AdvanceRealSeconds_WithInvalidDuration_PreservesGameTime()
    {
        var simulation = CreateSimulation();

        Assert.Throws<ArgumentOutOfRangeException>(() => simulation.AdvanceRealSeconds(0));

        Assert.Equal(0, simulation.GetSnapshot().GameMinute);
    }

    private static ShopSimulation CreateSimulation()
    {
        var game = new ShopGameService(
            [new ProductDefinition("water", "矿泉水", 100, 200, 20, "ambient")],
            openingCashCents: 10_000);

        return new ShopSimulation(
            game,
            new ScriptedRandomSource(),
            customerSpawnChance: 0d);
    }
}
