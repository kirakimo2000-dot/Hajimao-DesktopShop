using HajimaoDesktopShop.Application.Business.Simulation;

namespace HajimaoDesktopShop.Application.Business.Offline;

public static class OfflineSettlementService
{
    public static OfflineSettlementResult Settle(
        BusinessSimulation simulation,
        DateTimeOffset savedAtUtc,
        DateTimeOffset nowUtc,
        OfflineSettlementPolicy? policy = null)
    {
        ArgumentNullException.ThrowIfNull(simulation);
        var effectivePolicy = policy ?? new OfflineSettlementPolicy();
        var before = simulation.GetSnapshot();
        var elapsedTicks = nowUtc.UtcDateTime.Ticks - savedAtUtc.UtcDateTime.Ticks;
        if (elapsedTicks < 0)
        {
            var totals = CreateTotals(before);
            return new OfflineSettlementResult(
                0,
                0,
                false,
                OfflineTimeAnomaly.ClockMovedBackward,
                totals,
                totals,
                before.LastCompletedDay);
        }

        var requestedSeconds = elapsedTicks / TimeSpan.TicksPerSecond;
        var appliedSeconds = checked((int)Math.Min(
            requestedSeconds,
            effectivePolicy.MaxOfflineSeconds));
        var remaining = appliedSeconds;
        while (remaining > 0)
        {
            var batch = Math.Min(remaining, effectivePolicy.BatchSize);
            simulation.AdvanceRealSeconds(batch);
            remaining -= batch;
        }

        var after = simulation.GetSnapshot();
        return new OfflineSettlementResult(
            requestedSeconds,
            appliedSeconds,
            requestedSeconds > effectivePolicy.MaxOfflineSeconds,
            OfflineTimeAnomaly.None,
            CreateTotals(before),
            CreateTotals(after),
            after.LastCompletedDay);
    }

    private static OfflineBusinessTotals CreateTotals(BusinessSimulationSnapshot snapshot) =>
        new(
            snapshot.Business.CashCents,
            snapshot.Business.Stores.Sum(store => store.RevenueCents),
            snapshot.Business.Stores.Sum(store => store.GrossProfitCents),
            snapshot.Business.Stores.Sum(store => store.WageCostCents),
            snapshot.Business.Stores.Sum(store => store.NetProfitCents),
            snapshot.Stores.Sum(store => store.CompletedSales));
}
