using HajimaoDesktopShop.Application.Business.Employees;
using HajimaoDesktopShop.Domain.Employees;
using HajimaoDesktopShop.Rendering.Interactions;

namespace HajimaoDesktopShop.Rendering.Tests.Interactions;

public sealed class BusinessShopEmployeeChoreographyTests
{
    [Theory]
    [InlineData(EmployeeTaskKind.Checkout, "water", 350, 362)]
    [InlineData(EmployeeTaskKind.Restock, "ambient", 30, 94)]
    [InlineData(EmployeeTaskKind.Restock, "chilled", 30, 198)]
    [InlineData(EmployeeTaskKind.Restock, "frozen", 30, 302)]
    [InlineData(EmployeeTaskKind.Clean, "corner-store", 30, 318)]
    [InlineData(EmployeeTaskKind.CustomerService, "corner-store", 112, 280)]
    public void CreatePoses_UsesDutySpecificRoute(
        EmployeeTaskKind task,
        string target,
        int minimumX,
        int maximumX)
    {
        var employee = Employee("employee", EmployeeRole.Manager, task, target);

        var positions = Enumerable.Range(0, 24)
            .Select(frame => Assert.Single(
                BusinessShopEmployeeChoreography.CreatePoses([employee], frame, false)).X)
            .ToArray();

        Assert.All(positions, x => Assert.InRange(x, minimumX, maximumX));
        Assert.True(positions.Distinct().Count() > 1);
    }

    [Fact]
    public void CreatePoses_RestStaysInStaffArea()
    {
        var employee = Employee(
            "employee",
            EmployeeRole.Cashier,
            EmployeeTaskKind.Rest,
            null);

        var positions = Enumerable.Range(0, 24)
            .Select(frame => Assert.Single(
                BusinessShopEmployeeChoreography.CreatePoses([employee], frame, false)).X)
            .ToArray();

        Assert.Single(positions.Distinct());
        Assert.InRange(positions[0], 18, 82);
    }

    [Fact]
    public void CreatePoses_ReducedMotionFreezesTaskRoute()
    {
        var employee = Employee(
            "employee",
            EmployeeRole.Restocker,
            EmployeeTaskKind.Restock,
            "frozen");

        var positions = Enumerable.Range(0, 24)
            .Select(frame => Assert.Single(
                BusinessShopEmployeeChoreography.CreatePoses([employee], frame, true)).X)
            .ToArray();

        Assert.Single(positions.Distinct());
    }

    private static EmployeeOperationsEmployeeSnapshot Employee(
        string id,
        EmployeeRole role,
        EmployeeTaskKind task,
        string? target) =>
        new(
            id,
            id,
            role,
            1_000,
            1_000,
            6_000,
            0,
            1_000,
            1_000,
            "corner-store",
            0,
            0,
            true,
            new EmployeeTaskSnapshot(task, target, target, 1),
            EmployeeTaskPriorityCatalog.GetPriorities(role));
}
