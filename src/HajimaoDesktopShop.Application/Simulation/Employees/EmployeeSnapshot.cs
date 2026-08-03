namespace HajimaoDesktopShop.Application.Simulation.Employees;

public sealed record EmployeeSnapshot(
    string Id,
    string Name,
    EmployeeRole Role,
    EmployeeState State,
    string? CurrentTask);
