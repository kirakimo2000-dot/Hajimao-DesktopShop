using HajimaoDesktopShop.Desktop.ViewModels.Market;

namespace HajimaoDesktopShop.Desktop.Tests.ViewModels.Market;

public sealed class EmployeeManagementViewModelTests
{
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
