using System.Collections.ObjectModel;
using HajimaoDesktopShop.Application.Business.Employees;
using HajimaoDesktopShop.Domain.Employees;

namespace HajimaoDesktopShop.Application.Business.Simulation;

public enum EmployeeTaskAvailability
{
    Working,
    Resting,
    Unpaid
}

public sealed record EmployeeTaskWorker(
    string EmployeeId,
    EmployeeRole Role,
    EmployeeTaskAvailability Availability);

public sealed record EmployeeTaskTarget(
    string TargetKey,
    string TargetName,
    int RemainingMinutes);

public sealed record StoreTaskDemand(
    EmployeeTaskTarget? Checkout,
    EmployeeTaskTarget? Restock,
    EmployeeTaskTarget? Clean,
    EmployeeTaskTarget? CustomerService);

public static class EmployeeTaskPlanner
{
    public static IReadOnlyDictionary<string, EmployeeTaskSnapshot> Plan(
        IReadOnlyList<EmployeeTaskWorker> workers,
        StoreTaskDemand demand)
    {
        ArgumentNullException.ThrowIfNull(workers);
        ArgumentNullException.ThrowIfNull(demand);

        var assignments = new Dictionary<string, EmployeeTaskSnapshot>(StringComparer.Ordinal);
        var checkoutClaimed = false;
        var restockClaimed = false;
        foreach (var worker in workers
                     .OrderBy(worker => SpecialistOrder(worker.Role))
                     .ThenBy(worker => worker.EmployeeId, StringComparer.Ordinal))
        {
            var task = worker.Availability switch
            {
                EmployeeTaskAvailability.Resting => new EmployeeTaskSnapshot(
                    EmployeeTaskKind.Rest,
                    null,
                    "员工休息区",
                    null),
                EmployeeTaskAvailability.Unpaid => new EmployeeTaskSnapshot(
                    EmployeeTaskKind.Idle,
                    null,
                    "工资支付失败",
                    null),
                _ => ChooseWorkingTask(worker.Role, demand, ref checkoutClaimed, ref restockClaimed)
            };
            assignments.Add(worker.EmployeeId, task);
        }

        return new ReadOnlyDictionary<string, EmployeeTaskSnapshot>(assignments);
    }

    private static EmployeeTaskSnapshot ChooseWorkingTask(
        EmployeeRole role,
        StoreTaskDemand demand,
        ref bool checkoutClaimed,
        ref bool restockClaimed)
    {
        foreach (var kind in EmployeeTaskPriorityCatalog.GetPriorities(role))
        {
            var target = kind switch
            {
                EmployeeTaskKind.Checkout when !checkoutClaimed => demand.Checkout,
                EmployeeTaskKind.Restock when !restockClaimed => demand.Restock,
                EmployeeTaskKind.Clean => demand.Clean,
                EmployeeTaskKind.CustomerService => demand.CustomerService,
                _ => null
            };
            if (target is not null)
            {
                checkoutClaimed |= kind == EmployeeTaskKind.Checkout;
                restockClaimed |= kind == EmployeeTaskKind.Restock;
                return new EmployeeTaskSnapshot(
                    kind,
                    target.TargetKey,
                    target.TargetName,
                    target.RemainingMinutes);
            }

            if (kind == EmployeeTaskKind.Idle)
            {
                return new EmployeeTaskSnapshot(kind, null, null, null);
            }
        }

        throw new InvalidOperationException("Every employee role must provide an idle fallback.");
    }

    private static int SpecialistOrder(EmployeeRole role) => role switch
    {
        EmployeeRole.Cashier => 0,
        EmployeeRole.Restocker => 1,
        EmployeeRole.Cleaner => 2,
        EmployeeRole.SalesAssistant => 3,
        EmployeeRole.Buyer => 4,
        EmployeeRole.Manager => 5,
        _ => 6
    };
}
