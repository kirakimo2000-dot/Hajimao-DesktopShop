using HajimaoDesktopShop.Application.Business.Analysis;
using HajimaoDesktopShop.Application.Business.Simulation;

namespace HajimaoDesktopShop.Application.Business.Offline;

public static class ReturnBriefingService
{
    public static ReturnBriefingSnapshot Create(
        OfflineSettlementResult settlement,
        BusinessSimulationSnapshot simulation)
    {
        ArgumentNullException.ThrowIfNull(settlement);
        ArgumentNullException.ThrowIfNull(simulation);

        var cashDelta = checked(settlement.After.CashCents - settlement.Before.CashCents);
        var salesDelta = checked(
            settlement.After.CompletedSales - settlement.Before.CompletedSales);
        var netProfitDelta = checked(
            settlement.After.NetProfitCents - settlement.Before.NetProfitCents);
        var weakestStore = settlement.LastCompletedDay?.Stores
            .OrderBy(store => store.NetProfitCents)
            .ThenBy(store => store.StoreId, StringComparer.Ordinal)
            .FirstOrDefault();
        var analysis = weakestStore is null
            ? null
            : StoreEconomyAnalysisService.Calculate(simulation, weakestStore.StoreId);
        var portfolioNetProfit = settlement.LastCompletedDay?.Stores.Sum(
            store => store.NetProfitCents);
        var priority = weakestStore?.NetProfitCents < 0
            ? ReturnBriefingPriority.Recovery
            : portfolioNetProfit > 0 && cashDelta > 0
                ? ReturnBriefingPriority.Reinvest
                : ReturnBriefingPriority.Observe;

        return new ReturnBriefingSnapshot(
            settlement.AppliedSeconds > 0,
            settlement.AppliedSeconds,
            cashDelta,
            salesDelta,
            netProfitDelta,
            weakestStore?.StoreId,
            analysis?.Bottleneck ?? StoreBottleneck.InsufficientData,
            priority);
    }
}
