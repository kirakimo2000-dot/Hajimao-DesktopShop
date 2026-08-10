using HajimaoDesktopShop.Application.Business.Simulation;
using HajimaoDesktopShop.Application.Business.StoreGrowth;

namespace HajimaoDesktopShop.Application.Business.Progression;

public static class LongTermProgressionService
{
    private const int CommercialBlockLevel = 10;
    private const int StrengthenedStoreGrowthTotal = 2;

    public static LongTermProgressionSnapshot Create(
        BusinessSimulationSnapshot simulation,
        IReadOnlyList<StoreCatalogItemSnapshot> storeCatalog,
        IReadOnlyList<StoreGrowthSnapshot> growth,
        bool hasAnyInvestment)
    {
        ArgumentNullException.ThrowIfNull(simulation);
        ArgumentNullException.ThrowIfNull(storeCatalog);
        ArgumentNullException.ThrowIfNull(growth);

        var catalog = ValidateCatalog(storeCatalog);
        var growthByStore = ValidateGrowth(simulation, catalog, growth);
        var openStores = simulation.Business.Stores
            .OrderBy(store => store.Id, StringComparer.Ordinal)
            .ToArray();
        var goal = SelectGoal(simulation, catalog, growthByStore, openStores, hasAnyInvestment);
        return new LongTermProgressionSnapshot(
            goal,
            openStores.Length,
            catalog.Length,
            simulation.Business.PlayerLevel,
            simulation.Business.CashCents);
    }

    private static ProgressionGoalSnapshot SelectGoal(
        BusinessSimulationSnapshot simulation,
        StoreCatalogItemSnapshot[] catalog,
        IReadOnlyDictionary<string, StoreGrowthSnapshot> growthByStore,
        IReadOnlyList<BusinessStoreSnapshot> openStores,
        bool hasAnyInvestment)
    {
        var starterStoreId = openStores[0].Id;
        var completedProfit = simulation.LastCompletedDay?.Stores
            .Where(report => growthByStore.ContainsKey(report.StoreId))
            .Sum(report => report.NetProfitCents) ?? 0;
        if (completedProfit <= 0)
        {
            return new ProgressionGoalSnapshot(
                ProgressionGoalId.ReachProfitableDay,
                starterStoreId,
                completedProfit,
                TargetValue: 1);
        }

        if (!hasAnyInvestment)
        {
            return new ProgressionGoalSnapshot(
                ProgressionGoalId.MakeFirstInvestment,
                starterStoreId,
                CurrentValue: 0,
                TargetValue: 1);
        }

        var weakest = FindWeakest(openStores, growthByStore);
        if (openStores.Count > 1 && GrowthTotal(weakest) < StrengthenedStoreGrowthTotal)
        {
            return new ProgressionGoalSnapshot(
                ProgressionGoalId.StrengthenPortfolio,
                weakest.StoreId,
                GrowthTotal(weakest),
                StrengthenedStoreGrowthTotal);
        }

        var nextStore = catalog.FirstOrDefault(store => !store.IsOpen);
        if (nextStore is not null)
        {
            var isReady = simulation.Business.PlayerLevel >= nextStore.RequiredPlayerLevel
                && simulation.Business.CashCents >= nextStore.OpeningCostCents;
            var isSecondStore = openStores.Count == 1;
            return new ProgressionGoalSnapshot(
                isSecondStore
                    ? isReady
                        ? ProgressionGoalId.OpenSecondStore
                        : ProgressionGoalId.PrepareSecondStore
                    : isReady
                        ? ProgressionGoalId.OpenThirdStore
                        : ProgressionGoalId.PrepareThirdStore,
                nextStore.Id,
                simulation.Business.CashCents,
                nextStore.OpeningCostCents,
                nextStore.RequiredPlayerLevel,
                nextStore.OpeningCostCents);
        }

        if (simulation.Business.PlayerLevel < CommercialBlockLevel)
        {
            return new ProgressionGoalSnapshot(
                ProgressionGoalId.UnlockCommercialBlock,
                string.Empty,
                simulation.Business.PlayerLevel,
                CommercialBlockLevel,
                RequiredPlayerLevel: CommercialBlockLevel);
        }

        return new ProgressionGoalSnapshot(
            ProgressionGoalId.ImproveWeakestStore,
            weakest.StoreId,
            GrowthTotal(weakest),
            checked(GrowthTotal(weakest) + 1));
    }

    private static StoreCatalogItemSnapshot[] ValidateCatalog(
        IReadOnlyList<StoreCatalogItemSnapshot> storeCatalog)
    {
        if (storeCatalog.Count == 0)
        {
            throw new ArgumentException("At least one store must be configured.", nameof(storeCatalog));
        }

        var catalog = storeCatalog.ToArray();
        if (catalog.Any(store => store is null || string.IsNullOrWhiteSpace(store.Id))
            || catalog.Select(store => store.Id).Distinct(StringComparer.Ordinal).Count() != catalog.Length)
        {
            throw new ArgumentException("Store catalog IDs must be non-empty and unique.", nameof(storeCatalog));
        }

        return catalog;
    }

    private static IReadOnlyDictionary<string, StoreGrowthSnapshot> ValidateGrowth(
        BusinessSimulationSnapshot simulation,
        IReadOnlyList<StoreCatalogItemSnapshot> catalog,
        IReadOnlyList<StoreGrowthSnapshot> growth)
    {
        if (simulation.Business.Stores.Count == 0)
        {
            throw new ArgumentException("Progression requires at least one open store.", nameof(simulation));
        }

        var businessStoreIds = simulation.Business.Stores.Select(store => store.Id).ToHashSet(StringComparer.Ordinal);
        var catalogOpenIds = catalog.Where(store => store.IsOpen).Select(store => store.Id).ToHashSet(StringComparer.Ordinal);
        if (!businessStoreIds.SetEquals(catalogOpenIds))
        {
            throw new ArgumentException("Catalog open stores must match the business snapshot.", nameof(catalog));
        }

        var growthByStore = new Dictionary<string, StoreGrowthSnapshot>(StringComparer.Ordinal);
        foreach (var item in growth)
        {
            ArgumentNullException.ThrowIfNull(item);
            if (!growthByStore.TryAdd(item.StoreId, item))
            {
                throw new ArgumentException("Growth store IDs must be unique.", nameof(growth));
            }
        }

        if (!businessStoreIds.SetEquals(growthByStore.Keys))
        {
            throw new ArgumentException("Every open store must have exactly one growth snapshot.", nameof(growth));
        }

        return growthByStore;
    }

    private static StoreGrowthSnapshot FindWeakest(
        IReadOnlyList<BusinessStoreSnapshot> openStores,
        IReadOnlyDictionary<string, StoreGrowthSnapshot> growthByStore) =>
        openStores
            .Select(store => growthByStore[store.Id])
            .OrderBy(GrowthTotal)
            .ThenBy(item => item.StoreId, StringComparer.Ordinal)
            .First();

    private static int GrowthTotal(StoreGrowthSnapshot growth) => checked(
        growth.ExpansionLevel + growth.ShelfLevel + growth.DecorationLevel);
}
