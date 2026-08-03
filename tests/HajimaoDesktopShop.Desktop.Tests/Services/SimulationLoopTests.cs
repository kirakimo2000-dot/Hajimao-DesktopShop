using System.Diagnostics;
using HajimaoDesktopShop.Application.Catalog;
using HajimaoDesktopShop.Application.Game;
using HajimaoDesktopShop.Application.Simulation;
using HajimaoDesktopShop.Desktop.Services;

namespace HajimaoDesktopShop.Desktop.Tests.Services;

public sealed class SimulationLoopTests
{
    [Fact]
    public async Task StartAndStopAsync_AdvancesThenStopsSimulation()
    {
        var game = new ShopGameService(
            [new ProductDefinition("water", "矿泉水", 100, 200, 20, "ambient")],
            openingCashCents: 10_000);
        var simulation = new ShopSimulation(game, new NeverSpawnRandomSource(), customerSpawnChance: 0d);
        await using var loop = new SimulationLoop(simulation, TimeSpan.FromMilliseconds(10));

        loop.Start();
        var timeout = Stopwatch.StartNew();
        while (simulation.GetSnapshot().GameMinute == 0 && timeout.Elapsed < TimeSpan.FromSeconds(2))
        {
            await Task.Delay(10);
        }

        await loop.StopAsync();
        var stoppedAt = simulation.GetSnapshot().GameMinute;
        await Task.Delay(40);

        Assert.True(stoppedAt > 0);
        Assert.Equal(stoppedAt, simulation.GetSnapshot().GameMinute);
    }

    private sealed class NeverSpawnRandomSource : IRandomSource
    {
        public double NextDouble() => 1d;

        public int Next(int exclusiveMax) => 0;
    }
}
