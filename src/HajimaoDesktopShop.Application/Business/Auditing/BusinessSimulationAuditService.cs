using HajimaoDesktopShop.Application.Business.Simulation;

namespace HajimaoDesktopShop.Application.Business.Auditing;

public static class BusinessSimulationAuditService
{
    public static BusinessSimulationAuditReport Run(
        BusinessSimulation simulation,
        int seconds,
        BusinessSimulationAuditOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(simulation);
        if (seconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(seconds));
        }

        var effectiveOptions = options ?? new BusinessSimulationAuditOptions();
        var before = simulation.GetSnapshot();
        var remaining = seconds;
        var batchCount = 0;
        while (remaining > 0)
        {
            var batch = Math.Min(remaining, effectiveOptions.BatchSize);
            simulation.AdvanceRealSeconds(batch);
            remaining -= batch;
            batchCount++;
        }

        var after = simulation.GetSnapshot();
        return CreateReport(seconds, batchCount, before, after);
    }

    private static BusinessSimulationAuditReport CreateReport(
        int requestedSeconds,
        int batchCount,
        BusinessSimulationSnapshot before,
        BusinessSimulationSnapshot after)
    {
        var beforeRuntime = before.Stores.ToDictionary(store => store.StoreId, StringComparer.Ordinal);
        var afterRuntime = after.Stores.ToDictionary(store => store.StoreId, StringComparer.Ordinal);
        var beforeBusiness = before.Business.Stores.ToDictionary(store => store.Id, StringComparer.Ordinal);
        var afterBusiness = after.Business.Stores.ToDictionary(store => store.Id, StringComparer.Ordinal);
        var storeIds = beforeRuntime.Keys.OrderBy(id => id, StringComparer.Ordinal).ToArray();
        if (!storeIds.SequenceEqual(afterRuntime.Keys.OrderBy(id => id, StringComparer.Ordinal), StringComparer.Ordinal)
            || !storeIds.SequenceEqual(beforeBusiness.Keys.OrderBy(id => id, StringComparer.Ordinal), StringComparer.Ordinal)
            || !storeIds.SequenceEqual(afterBusiness.Keys.OrderBy(id => id, StringComparer.Ordinal), StringComparer.Ordinal))
        {
            throw new InvalidOperationException("Open stores changed while the simulation audit was running.");
        }

        var stores = storeIds.Select(storeId => CreateStoreReport(
            beforeRuntime[storeId],
            afterRuntime[storeId],
            beforeBusiness[storeId],
            afterBusiness[storeId])).ToArray();
        return new BusinessSimulationAuditReport(
            requestedSeconds,
            requestedSeconds,
            batchCount,
            before.GameMinute,
            after.GameMinute,
            before.Business.PlayerLevel,
            after.Business.PlayerLevel,
            checked(after.Business.TotalExperience - before.Business.TotalExperience),
            checked(after.Business.CashCents - before.Business.CashCents),
            Array.AsReadOnly(stores));
    }

    private static StoreSimulationAuditReport CreateStoreReport(
        StoreOperationsSnapshot beforeRuntime,
        StoreOperationsSnapshot afterRuntime,
        BusinessStoreSnapshot beforeBusiness,
        BusinessStoreSnapshot afterBusiness) =>
        new(
            afterRuntime.StoreId,
            checked(afterRuntime.Visitors - beforeRuntime.Visitors),
            checked(afterRuntime.AcceptedPurchases - beforeRuntime.AcceptedPurchases),
            checked(afterRuntime.CompletedSales - beforeRuntime.CompletedSales),
            checked(afterRuntime.LostSales - beforeRuntime.LostSales),
            checked(afterRuntime.WagePaymentFailures - beforeRuntime.WagePaymentFailures),
            checked(afterBusiness.RevenueCents - beforeBusiness.RevenueCents),
            checked(afterBusiness.StockPurchaseCostCents - beforeBusiness.StockPurchaseCostCents),
            checked(afterBusiness.GrossProfitCents - beforeBusiness.GrossProfitCents),
            checked(afterBusiness.WageCostCents - beforeBusiness.WageCostCents),
            checked(afterBusiness.OperatingCostCents - beforeBusiness.OperatingCostCents),
            checked(afterBusiness.NetProfitCents - beforeBusiness.NetProfitCents),
            afterRuntime.CheckoutQueueLength,
            afterRuntime.CleanlinessPermille,
            checked(afterBusiness.Products.Sum(product => product.Quantity)));
}
