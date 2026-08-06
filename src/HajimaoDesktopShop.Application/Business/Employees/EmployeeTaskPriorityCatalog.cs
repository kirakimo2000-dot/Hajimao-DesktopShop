using HajimaoDesktopShop.Domain.Employees;

namespace HajimaoDesktopShop.Application.Business.Employees;

public static class EmployeeTaskPriorityCatalog
{
    private static readonly IReadOnlyDictionary<EmployeeRole, IReadOnlyList<EmployeeTaskKind>> Rules =
        new Dictionary<EmployeeRole, IReadOnlyList<EmployeeTaskKind>>
        {
            [EmployeeRole.Cashier] = Priorities(
                EmployeeTaskKind.Checkout,
                EmployeeTaskKind.CustomerService),
            [EmployeeRole.Restocker] = Priorities(
                EmployeeTaskKind.Restock,
                EmployeeTaskKind.CustomerService),
            [EmployeeRole.SalesAssistant] = Priorities(
                EmployeeTaskKind.CustomerService,
                EmployeeTaskKind.Restock),
            [EmployeeRole.Cleaner] = Priorities(
                EmployeeTaskKind.Clean,
                EmployeeTaskKind.CustomerService),
            [EmployeeRole.Manager] = Priorities(
                EmployeeTaskKind.Checkout,
                EmployeeTaskKind.CustomerService,
                EmployeeTaskKind.Clean,
                EmployeeTaskKind.Restock),
            [EmployeeRole.Buyer] = Priorities(
                EmployeeTaskKind.Restock,
                EmployeeTaskKind.CustomerService)
        };

    public static IReadOnlyList<EmployeeTaskKind> GetPriorities(EmployeeRole role) =>
        Rules.TryGetValue(role, out var priorities)
            ? priorities
            : throw new ArgumentOutOfRangeException(nameof(role));

    private static IReadOnlyList<EmployeeTaskKind> Priorities(params EmployeeTaskKind[] priorities) =>
        Array.AsReadOnly([.. priorities, EmployeeTaskKind.Idle]);
}
