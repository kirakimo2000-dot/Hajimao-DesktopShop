using HajimaoDesktopShop.Desktop.ViewModels.Market;

namespace HajimaoDesktopShop.Desktop.Tests.ViewModels.Market;

public sealed class StoreEmployeeAssignmentViewModelTests
{
    [Fact]
    public void Refresh_ProjectsGlobalMaomaoAsAssignedToTheInitialStore()
    {
        var session = MarketTestSession.Create();
        var viewModel = new StoreEmployeeAssignmentViewModel(
            session,
            () => "corner-store",
            () => { });

        viewModel.Refresh();

        var employee = Assert.Single(viewModel.Employees);
        Assert.Equal("毛毛", employee.Name);
        Assert.Equal("已在本店", employee.LocationText);
        Assert.True(employee.IsAssignedHere);
        Assert.Contains("正在挂机", viewModel.StatusText, StringComparison.Ordinal);
    }
}
