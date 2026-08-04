using HajimaoDesktopShop.Application.Business.Procurement;
using HajimaoDesktopShop.Application.Business.Simulation;

namespace HajimaoDesktopShop.Application.Business.Onboarding;

public static class OnboardingProgressService
{
    public static OnboardingSnapshot CreateSnapshot(
        BusinessSimulationSnapshot simulation,
        ProcurementSnapshot procurement)
    {
        ArgumentNullException.ThrowIfNull(simulation);
        ArgumentNullException.ThrowIfNull(procurement);

        var tasks = new[]
        {
            new OnboardingTaskState(
                OnboardingTaskId.RestockProduct,
                simulation.Business.Stores.Any(store => store.StockPurchaseCostCents > 0)),
            new OnboardingTaskState(
                OnboardingTaskId.AdjustPrice,
                simulation.Business.Stores
                    .SelectMany(store => store.Products)
                    .Any(product => product.SalePriceCents != product.ReferenceSalePriceCents)),
            new OnboardingTaskState(
                OnboardingTaskId.EnableAutoRestock,
                procurement.AutoRestockPolicies.Any(policy => policy.IsEnabled)),
            new OnboardingTaskState(
                OnboardingTaskId.CompleteFirstSale,
                simulation.Business.Stores.Any(store => store.RevenueCents > 0)),
            new OnboardingTaskState(
                OnboardingTaskId.TrainEmployee,
                simulation.Employees.Employees.Any(employee => employee.TrainingLevel > 0)),
            new OnboardingTaskState(
                OnboardingTaskId.UpgradeStore,
                simulation.Business.Stores.Any(store =>
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
}
