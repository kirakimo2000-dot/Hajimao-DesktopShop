using HajimaoDesktopShop.Application.Business.Procurement;
using HajimaoDesktopShop.Application.Business.Simulation;

namespace HajimaoDesktopShop.Application.Business.Onboarding;

public static class OnboardingProgressService
{
    public static OnboardingSnapshot CreateSnapshot(
        BusinessSimulationSnapshot simulation,
        ProcurementSnapshot procurement,
        bool hasRecordedInvestment = false,
        bool hasComparableInvestmentReturn = false)
    {
        ArgumentNullException.ThrowIfNull(simulation);
        ArgumentNullException.ThrowIfNull(procurement);

        var tasks = new[]
        {
            new OnboardingTaskState(
                OnboardingTaskId.ObserveFirstDay,
                simulation.LastCompletedDay is not null),
            new OnboardingTaskState(
                OnboardingTaskId.MakeFirstInvestment,
                hasRecordedInvestment || simulation.Business.Stores.Any(store =>
                    store.Growth is not null
                    && (store.Growth.ExpansionLevel > 0
                        || store.Growth.ShelfLevel > 0
                        || store.Growth.DecorationLevel > 0))),
            new OnboardingTaskState(
                OnboardingTaskId.ReviewInvestmentReturn,
                hasRecordedInvestment && hasComparableInvestmentReturn)
        };
        var completedTasks = tasks.Count(task => task.IsCompleted);
        var currentTaskId = tasks.FirstOrDefault(task => !task.IsCompleted)?.Id;

        return new OnboardingSnapshot(tasks, completedTasks, currentTaskId);
    }

}
