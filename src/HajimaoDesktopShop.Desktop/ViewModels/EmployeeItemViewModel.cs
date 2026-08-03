using HajimaoDesktopShop.Application.Simulation.Employees;
using HajimaoDesktopShop.Domain.Employees;

namespace HajimaoDesktopShop.Desktop.ViewModels;

public sealed record EmployeeItemViewModel(
    string Id,
    string Name,
    string RoleText,
    string StateText,
    string CurrentTask)
{
    public static EmployeeItemViewModel FromSnapshot(EmployeeSnapshot snapshot) =>
        new(
            snapshot.Id,
            snapshot.Name,
            snapshot.Role == EmployeeRole.Cashier ? "收银员" : "补货员",
            snapshot.State == EmployeeState.Working ? "工作中" : "待命",
            snapshot.CurrentTask ?? "等待任务");
}
