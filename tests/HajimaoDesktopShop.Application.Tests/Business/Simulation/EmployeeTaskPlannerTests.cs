using HajimaoDesktopShop.Application.Business.Employees;
using HajimaoDesktopShop.Application.Business.Simulation;
using HajimaoDesktopShop.Domain.Employees;

namespace HajimaoDesktopShop.Application.Tests.Business.Simulation;

public sealed class EmployeeTaskPlannerTests
{
    [Theory]
    [InlineData(EmployeeRole.Cashier, EmployeeTaskKind.Checkout)]
    [InlineData(EmployeeRole.Restocker, EmployeeTaskKind.Restock)]
    [InlineData(EmployeeRole.Cleaner, EmployeeTaskKind.Clean)]
    [InlineData(EmployeeRole.SalesAssistant, EmployeeTaskKind.CustomerService)]
    public void Plan_AssignsPrimaryAvailableDuty(EmployeeRole role, EmployeeTaskKind expected)
    {
        var result = EmployeeTaskPlanner.Plan(
            [Worker("employee", role)],
            FullDemand());

        Assert.Equal(expected, result["employee"].Kind);
    }

    [Fact]
    public void Plan_ReportsRestWithoutClaimingWork()
    {
        var result = EmployeeTaskPlanner.Plan(
            [Worker("cashier", EmployeeRole.Cashier, EmployeeTaskAvailability.Resting)],
            FullDemand());

        var task = result["cashier"];
        Assert.Equal(EmployeeTaskKind.Rest, task.Kind);
        Assert.True(task.IsResting);
        Assert.Equal("员工休息区", task.TargetName);
    }

    [Fact]
    public void Plan_ReportsIdleWhenPayrollFailed()
    {
        var result = EmployeeTaskPlanner.Plan(
            [Worker("cashier", EmployeeRole.Cashier, EmployeeTaskAvailability.Unpaid)],
            FullDemand());

        Assert.Equal(EmployeeTaskKind.Idle, result["cashier"].Kind);
        Assert.Equal("工资支付失败", result["cashier"].TargetName);
    }

    [Fact]
    public void Plan_AssignsExclusiveDutiesOnlyOnceAndUsesManagerAsFallback()
    {
        EmployeeTaskWorker[] workers =
        [
            Worker("manager", EmployeeRole.Manager),
            Worker("cashier-b", EmployeeRole.Cashier),
            Worker("cashier-a", EmployeeRole.Cashier),
            Worker("restocker-b", EmployeeRole.Restocker),
            Worker("restocker-a", EmployeeRole.Restocker)
        ];

        var result = EmployeeTaskPlanner.Plan(workers, FullDemand());

        Assert.Equal("cashier-a", Assert.Single(result, pair =>
            pair.Value.Kind == EmployeeTaskKind.Checkout).Key);
        Assert.Equal("restocker-a", Assert.Single(result, pair =>
            pair.Value.Kind == EmployeeTaskKind.Restock).Key);
        Assert.Equal(EmployeeTaskKind.CustomerService, result["manager"].Kind);
    }

    [Fact]
    public void Plan_ManagerCoversCheckoutWhenSpecialistCannotWork()
    {
        var result = EmployeeTaskPlanner.Plan(
            [
                Worker("cashier", EmployeeRole.Cashier, EmployeeTaskAvailability.Resting),
                Worker("manager", EmployeeRole.Manager)
            ],
            FullDemand());

        Assert.Equal(EmployeeTaskKind.Checkout, result["manager"].Kind);
    }

    [Fact]
    public void Plan_IsIndependentOfInputOrder()
    {
        EmployeeTaskWorker[] workers =
        [
            Worker("manager", EmployeeRole.Manager),
            Worker("cashier", EmployeeRole.Cashier),
            Worker("cleaner", EmployeeRole.Cleaner)
        ];

        var forward = EmployeeTaskPlanner.Plan(workers, FullDemand());
        var reverse = EmployeeTaskPlanner.Plan(workers.Reverse().ToArray(), FullDemand());

        Assert.Equal(
            forward.OrderBy(pair => pair.Key),
            reverse.OrderBy(pair => pair.Key));
    }

    [Fact]
    public void Plan_PreservesTargetAndRemainingTime()
    {
        var result = EmployeeTaskPlanner.Plan(
            [Worker("restocker", EmployeeRole.Restocker)],
            FullDemand());

        Assert.Equal("chilled", result["restocker"].TargetKey);
        Assert.Equal("牛奶", result["restocker"].TargetName);
        Assert.Equal(7, result["restocker"].RemainingMinutes);
    }

    private static EmployeeTaskWorker Worker(
        string id,
        EmployeeRole role,
        EmployeeTaskAvailability availability = EmployeeTaskAvailability.Working) =>
        new(id, role, availability);

    private static StoreTaskDemand FullDemand() =>
        new(
            new EmployeeTaskTarget("water", "矿泉水", 2),
            new EmployeeTaskTarget("chilled", "牛奶", 7),
            new EmployeeTaskTarget("store-1", "便利店地面", 3),
            new EmployeeTaskTarget("store-1", "店内顾客", 1));
}
