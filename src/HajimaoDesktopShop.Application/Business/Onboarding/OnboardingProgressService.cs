using HajimaoDesktopShop.Application.Business.Procurement;
using HajimaoDesktopShop.Application.Business.Simulation;

namespace HajimaoDesktopShop.Application.Business.Onboarding;

public static class OnboardingProgressService
{
    public static OnboardingSnapshot CreateSnapshot(
        BusinessSimulationSnapshot simulation,
        ProcurementSnapshot procurement,
        bool hasRecordedInvestment = false)
    {
        ArgumentNullException.ThrowIfNull(simulation);
        ArgumentNullException.ThrowIfNull(procurement);

        var tasks = new[]
        {
            new OnboardingTaskState(
                OnboardingTaskId.ReviewEconomy,
                simulation.GameMinute > 0),
            new OnboardingTaskState(
                OnboardingTaskId.ChooseStoreStrategy,
                HasChosenNonDefaultStrategy(simulation, procurement)),
            new OnboardingTaskState(
                OnboardingTaskId.CompleteFirstSale,
                simulation.Business.Stores.Any(store => store.RevenueCents > 0)),
            new OnboardingTaskState(
                OnboardingTaskId.ReachPositiveDay,
                simulation.LastCompletedDay?.Stores.Any(store => store.NetProfitCents > 0) == true),
            new OnboardingTaskState(
                OnboardingTaskId.MakeFirstInvestment,
                hasRecordedInvestment || simulation.Business.Stores.Any(store =>
                    store.Growth is not null
                    && (store.Growth.ExpansionLevel > 0
                        || store.Growth.ShelfLevel > 0
                        || store.Growth.DecorationLevel > 0))),
            new OnboardingTaskState(
                OnboardingTaskId.OpenSecondStore,
                simulation.Business.Stores.Count > 1)
        };
        var completedTasks = tasks.Count(task => task.IsCompleted);
        var currentTaskId = tasks.FirstOrDefault(task => !task.IsCompleted)?.Id;

        return new OnboardingSnapshot(tasks, completedTasks, currentTaskId);
    }

    private static bool HasChosenNonDefaultStrategy(
        BusinessSimulationSnapshot simulation,
        ProcurementSnapshot procurement)
    {
        var policies = procurement.AutoRestockPolicies.ToDictionary(
            policy => (policy.StoreId, policy.ProductId));
        foreach (var store in simulation.Business.Stores)
        {
            foreach (var product in store.Products)
            {
                if (product.SalePriceCents != product.ReferenceSalePriceCents)
                {
                    return true;
                }

                var balancedReorderPoint = Math.Max(1, product.Capacity * 300 / 1_000);
                var balancedTargetQuantity = Math.Max(1, product.Capacity * 750 / 1_000);
                if (policies.TryGetValue((store.Id, product.Id), out var policy)
                    && (!policy.IsEnabled
                        || policy.ReorderPoint != balancedReorderPoint
                        || policy.TargetQuantity != balancedTargetQuantity
                        || !string.Equals(
                            policy.PreferredChannelId,
                            "regional-distributor",
                            StringComparison.Ordinal)
                        || !policy.UseEmergencySupplierWhenOutOfStock))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
