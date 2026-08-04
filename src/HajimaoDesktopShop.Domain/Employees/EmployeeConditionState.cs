namespace HajimaoDesktopShop.Domain.Employees;

public sealed record EmployeeConditionState(
    int TrainingLevel,
    int EnergyPermille,
    int SatisfactionPermille,
    int WorkMinutesTowardSatisfactionLoss,
    int RestMinutesTowardSatisfactionGain);
