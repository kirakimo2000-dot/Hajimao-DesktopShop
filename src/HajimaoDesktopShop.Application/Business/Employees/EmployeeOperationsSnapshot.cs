using HajimaoDesktopShop.Domain.Employees;

namespace HajimaoDesktopShop.Application.Business.Employees;

public sealed record EmployeeOperationsSnapshot(
    ulong CandidateRandomState,
    long NextCandidateId,
    IReadOnlyList<EmployeeCandidate> Candidates,
    IReadOnlyList<EmployeeOperationsEmployeeSnapshot> Employees);

public sealed record EmployeeOperationsEmployeeSnapshot(
    string EmployeeId,
    string Name,
    EmployeeRole Role,
    int BaseEfficiencyPermille,
    int EffectiveEfficiencyPermille,
    long HourlyWageCents,
    int TrainingLevel,
    int EnergyPermille,
    int SatisfactionPermille,
    string StoreId,
    int ShiftStartMinute,
    int ShiftEndMinute,
    bool IsAlwaysOn,
    EmployeeTaskSnapshot? CurrentTask = null,
    IReadOnlyList<EmployeeTaskKind>? TaskPriorities = null);

internal sealed record EmployeeRuntimeAssignment(
    string StoreId,
    Employee Employee,
    EmployeeShift Shift);
