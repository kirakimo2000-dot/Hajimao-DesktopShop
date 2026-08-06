using HajimaoDesktopShop.Desktop.ViewModels.Market;

namespace HajimaoDesktopShop.Desktop.Tests.ViewModels.Market;

public sealed class EmployeeManagementViewModelTests
{
    [Fact]
    public void EmployeePage_ProjectsLiveDutyAndOrderedRolePriorities()
    {
        var session = MarketTestSession.Create();
        var page = new EmployeeManagementViewModel(session, () => "corner-store");

        page.Refresh();

        var cashier = page.Employees.Single(employee => employee.EmployeeId == "starter-cashier");
        Assert.Contains("导购", cashier.TaskText);
        Assert.Contains("店内顾客", cashier.TaskText);
        Assert.Equal("优先级 收银 → 导购 → 待命", cashier.PriorityText);
    }

    [Fact]
    public void EmployeePage_HiresTrainsAndAppliesExplicitDayShift()
    {
        var session = MarketTestSession.Create(openingCashCents: 2_000_000);
        var page = new EmployeeManagementViewModel(session, () => "corner-store");
        page.Refresh();
        var candidate = page.Candidates[0];

        candidate.HireCommand.Execute(null);
        page.Refresh();
        var employee = Assert.Single(page.Employees, item => item.Name == candidate.Name);
        employee.TrainCommand.Execute(null);
        employee.SetDayShiftCommand.Execute(null);
        page.Refresh();

        Assert.Equal(1, employee.TrainingLevel);
        Assert.Equal("08:00–16:00", employee.ShiftText);
    }
}
