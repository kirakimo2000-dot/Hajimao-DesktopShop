using System.Diagnostics;
using HajimaoDesktopShop.Desktop.Services;

namespace HajimaoDesktopShop.Desktop.Tests.Services;

public sealed class SimulationLoopTests
{
    [Fact]
    public async Task DelegateLoop_AdvancesAtFixedBoundaryThenStops()
    {
        var advances = 0;
        await using var loop = new SimulationLoop(
            () => Interlocked.Increment(ref advances),
            TimeSpan.FromMilliseconds(10));

        loop.Start();
        var timeout = Stopwatch.StartNew();
        while (Volatile.Read(ref advances) == 0 && timeout.Elapsed < TimeSpan.FromSeconds(2))
        {
            await Task.Delay(10);
        }

        await loop.StopAsync();
        var stoppedAt = Volatile.Read(ref advances);
        await Task.Delay(40);

        Assert.True(stoppedAt > 0);
        Assert.Equal(stoppedAt, Volatile.Read(ref advances));
    }

    [Fact]
    public async Task UnexpectedSimulationFailure_IsReportedExactlyOnceAndStopsLoop()
    {
        var expected = new InvalidOperationException("simulation failed");
        var advances = 0;
        var reported = new List<Exception>();
        await using var loop = new SimulationLoop(
            () =>
            {
                Interlocked.Increment(ref advances);
                throw expected;
            },
            TimeSpan.FromMilliseconds(10),
            exception => reported.Add(exception));

        loop.Start();
        var timeout = Stopwatch.StartNew();
        while (reported.Count == 0 && timeout.Elapsed < TimeSpan.FromSeconds(2))
        {
            await Task.Delay(10);
        }

        await loop.StopAsync();
        await Task.Delay(40);

        Assert.Equal(1, Volatile.Read(ref advances));
        Assert.Same(expected, Assert.Single(reported));
    }

    [Fact]
    public async Task NormalCancellation_DoesNotReportFailure()
    {
        var reported = new List<Exception>();
        await using var loop = new SimulationLoop(
            () => { },
            TimeSpan.FromMilliseconds(10),
            exception => reported.Add(exception));

        loop.Start();
        await Task.Delay(30);
        await loop.StopAsync();

        Assert.Empty(reported);
    }
}
