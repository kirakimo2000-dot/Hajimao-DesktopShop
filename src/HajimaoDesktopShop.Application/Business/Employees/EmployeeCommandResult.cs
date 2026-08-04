using HajimaoDesktopShop.Domain.Economy;

namespace HajimaoDesktopShop.Application.Business.Employees;

public enum EmployeeCommandStatus
{
    Success,
    UnknownCandidate,
    UnknownEmployee,
    UnknownStore,
    InsufficientFunds,
    MaximumTraining,
    InvalidShift,
    DuplicateEmployee
}

public readonly record struct EmployeeCommandResult(
    EmployeeCommandStatus Status,
    string? EmployeeId,
    Money Cost);
