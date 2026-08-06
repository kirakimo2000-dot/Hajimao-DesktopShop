namespace HajimaoDesktopShop.Application.Business.Employees;

public sealed record EmployeeTaskSnapshot(
    EmployeeTaskKind Kind,
    string? TargetKey,
    string? TargetName,
    int? RemainingMinutes)
{
    public bool IsResting => Kind == EmployeeTaskKind.Rest;
}
