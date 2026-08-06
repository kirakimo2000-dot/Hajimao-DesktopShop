using HajimaoDesktopShop.Domain.Employees;
using HajimaoDesktopShop.Application.Business.Employees;

namespace HajimaoDesktopShop.Rendering.Interactions;

public sealed record BusinessShopEmployeePose(
    string EmployeeId,
    EmployeeRole Role,
    int X,
    int Y,
    bool IsSupporting,
    EmployeeTaskKind TaskKind = EmployeeTaskKind.Idle,
    string? TargetKey = null)
{
    public string EmployeeId { get; } =
        string.IsNullOrWhiteSpace(EmployeeId)
            ? throw new ArgumentException("Employee id is required.", nameof(EmployeeId))
            : EmployeeId;
}
