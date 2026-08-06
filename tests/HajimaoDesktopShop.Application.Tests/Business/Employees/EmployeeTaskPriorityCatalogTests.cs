using HajimaoDesktopShop.Application.Business.Employees;
using HajimaoDesktopShop.Domain.Employees;

namespace HajimaoDesktopShop.Application.Tests.Business.Employees;

public sealed class EmployeeTaskPriorityCatalogTests
{
    [Theory]
    [InlineData(EmployeeRole.Cashier, EmployeeTaskKind.Checkout)]
    [InlineData(EmployeeRole.Restocker, EmployeeTaskKind.Restock)]
    [InlineData(EmployeeRole.Cleaner, EmployeeTaskKind.Clean)]
    [InlineData(EmployeeRole.SalesAssistant, EmployeeTaskKind.CustomerService)]
    [InlineData(EmployeeRole.Manager, EmployeeTaskKind.Checkout)]
    [InlineData(EmployeeRole.Buyer, EmployeeTaskKind.Restock)]
    public void GetPriorities_StartsWithRolesPrimaryDuty(
        EmployeeRole role,
        EmployeeTaskKind expected)
    {
        var priorities = EmployeeTaskPriorityCatalog.GetPriorities(role);

        Assert.Equal(expected, priorities[0]);
        Assert.Equal(EmployeeTaskKind.Idle, priorities[^1]);
        Assert.Equal(priorities.Count, priorities.Distinct().Count());
    }

    [Fact]
    public void GetPriorities_ReturnsCachedReadOnlyRules()
    {
        var first = EmployeeTaskPriorityCatalog.GetPriorities(EmployeeRole.Cashier);
        var second = EmployeeTaskPriorityCatalog.GetPriorities(EmployeeRole.Cashier);

        Assert.Same(first, second);
        Assert.IsAssignableFrom<IReadOnlyList<EmployeeTaskKind>>(first);
        Assert.False(first is EmployeeTaskKind[]);
    }

    [Fact]
    public void TaskSnapshot_ReportsRestOnlyForRestDuty()
    {
        var rest = new EmployeeTaskSnapshot(EmployeeTaskKind.Rest, null, "员工休息区", null);
        var idle = new EmployeeTaskSnapshot(EmployeeTaskKind.Idle, null, null, null);

        Assert.True(rest.IsResting);
        Assert.False(idle.IsResting);
    }
}
