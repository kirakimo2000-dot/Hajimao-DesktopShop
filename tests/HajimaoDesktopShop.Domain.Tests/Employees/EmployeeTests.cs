using HajimaoDesktopShop.Domain.Economy;
using HajimaoDesktopShop.Domain.Employees;

namespace HajimaoDesktopShop.Domain.Tests.Employees;

public sealed class EmployeeTests
{
    [Fact]
    public void Efficiency_ChangesRequiredTaskMinutesWithIntegerCeiling()
    {
        var slower = RestoreEmployee("slow", efficiencyPermille: 800, hourlyWageCents: 1_800, energy: 1_000, satisfaction: 1_000);
        var faster = RestoreEmployee("fast", efficiencyPermille: 1_500, hourlyWageCents: 2_600, energy: 1_000, satisfaction: 1_000);

        Assert.Equal(12, slower.CalculateTaskMinutes(baseTaskMinutes: 10));
        Assert.Equal(7, faster.CalculateTaskMinutes(baseTaskMinutes: 10));
    }

    [Fact]
    public void NewEmployee_StartsWithDefaultCondition()
    {
        var employee = CreateEmployee("cashier", efficiencyPermille: 1_000, hourlyWageCents: 1_800);

        Assert.Equal(0, employee.TrainingLevel);
        Assert.Equal(1_000, employee.EnergyPermille);
        Assert.Equal(700, employee.SatisfactionPermille);
        Assert.Equal(960, employee.EffectiveEfficiencyPermille);
        Assert.True(employee.CanWork);
    }

    [Fact]
    public void TrainingEnergyAndSatisfaction_ComposeEffectiveEfficiency()
    {
        var employee = RestoreEmployee(
            "cashier",
            efficiencyPermille: 1_000,
            hourlyWageCents: 1_800,
            energy: 800,
            satisfaction: 900);

        employee.CompleteTraining();

        Assert.Equal(1, employee.TrainingLevel);
        Assert.Equal(963, employee.EffectiveEfficiencyPermille);
        Assert.Equal(11, employee.CalculateTaskMinutes(baseTaskMinutes: 10));
    }

    [Fact]
    public void WorkedConditionMinutes_DrainEnergyAndEventuallyReduceSatisfaction()
    {
        var employee = CreateEmployee("cashier", efficiencyPermille: 1_000, hourlyWageCents: 1_800);

        for (var minute = 0; minute < 60; minute++)
        {
            employee.RecordWorkedConditionMinute();
        }

        Assert.Equal(880, employee.EnergyPermille);
        Assert.Equal(699, employee.SatisfactionPermille);
        Assert.Equal(0, employee.CaptureConditionState().WorkMinutesTowardSatisfactionLoss);
    }

    [Fact]
    public void ExhaustedEmployee_CannotWorkAndRecoversOffShift()
    {
        var employee = RestoreEmployee(
            "cashier",
            efficiencyPermille: 1_000,
            hourlyWageCents: 1_800,
            energy: 0,
            satisfaction: 700);

        Assert.False(employee.CanWork);
        Assert.Throws<InvalidOperationException>(() => employee.RecordWorkedConditionMinute());

        employee.RecordRestMinute();

        Assert.Equal(4, employee.EnergyPermille);
        Assert.True(employee.CanWork);
    }

    [Fact]
    public void RestMinutes_RecoverEnergyAndEventuallyIncreaseSatisfaction()
    {
        var employee = RestoreEmployee(
            "cashier",
            efficiencyPermille: 1_000,
            hourlyWageCents: 1_800,
            energy: 600,
            satisfaction: 700);

        for (var minute = 0; minute < 120; minute++)
        {
            employee.RecordRestMinute();
        }

        Assert.Equal(1_000, employee.EnergyPermille);
        Assert.Equal(701, employee.SatisfactionPermille);
        Assert.Equal(0, employee.CaptureConditionState().RestMinutesTowardSatisfactionGain);
    }

    [Fact]
    public void ConditionCaptureAndRestore_PreservesProgress()
    {
        var employee = CreateEmployee("cashier", efficiencyPermille: 1_000, hourlyWageCents: 1_800);
        employee.CompleteTraining();
        employee.RecordWorkedConditionMinute();

        var restored = Employee.Restore(
            employee.Id,
            employee.Name,
            employee.Role,
            employee.EfficiencyPermille,
            employee.HourlyWage,
            employee.CaptureWorkState(),
            employee.CaptureConditionState());

        Assert.Equal(employee.CaptureConditionState(), restored.CaptureConditionState());
        Assert.Equal(employee.EffectiveEfficiencyPermille, restored.EffectiveEfficiencyPermille);
    }

    [Fact]
    public void TrainingAndConditionRestore_RejectInvalidValues()
    {
        var employee = CreateEmployee("cashier", efficiencyPermille: 1_000, hourlyWageCents: 1_800);
        for (var level = 0; level < 5; level++)
        {
            employee.CompleteTraining();
        }

        Assert.Equal(5, employee.TrainingLevel);
        Assert.Throws<InvalidOperationException>(() => employee.CompleteTraining());
        Assert.Throws<ArgumentOutOfRangeException>(() => RestoreEmployee(
            "invalid",
            efficiencyPermille: 1_000,
            hourlyWageCents: 1_800,
            energy: 1_001,
            satisfaction: 700));
    }

    [Fact]
    public void WorkedMinutes_AccrueExactHourlyWageWithoutLosingFractionalCents()
    {
        var employee = CreateEmployee("cashier", efficiencyPermille: 1_000, hourlyWageCents: 1_001);
        var charged = Money.Zero;

        for (var minute = 0; minute < 60; minute++)
        {
            charged += employee.RecordWorkedMinute();
        }

        Assert.Equal(60, employee.WorkedMinutes);
        Assert.Equal(new Money(1_001), charged);
        Assert.Equal(new Money(1_001), employee.TotalWagesAccrued);
    }

    [Fact]
    public void NextMinuteWage_PreviewsExactChargeWithoutMutation()
    {
        var employee = CreateEmployee("cashier", efficiencyPermille: 1_000, hourlyWageCents: 1_001);

        var firstPreview = employee.NextMinuteWage;
        var firstCharge = employee.RecordWorkedMinute();
        var secondPreview = employee.NextMinuteWage;

        Assert.Equal(new Money(16), firstPreview);
        Assert.Equal(firstPreview, firstCharge);
        Assert.Equal(new Money(17), secondPreview);
        Assert.Equal(1, employee.WorkedMinutes);
    }

    [Fact]
    public void CaptureAndRestore_PreservesFractionalWageProgress()
    {
        var employee = CreateEmployee("cashier", efficiencyPermille: 1_250, hourlyWageCents: 1_001);
        for (var minute = 0; minute < 61; minute++)
        {
            employee.RecordWorkedMinute();
        }

        var restored = Employee.Restore(
            employee.Id,
            employee.Name,
            employee.Role,
            employee.EfficiencyPermille,
            employee.HourlyWage,
            employee.CaptureWorkState());

        Assert.Equal(employee.WorkedMinutes, restored.WorkedMinutes);
        Assert.Equal(employee.TotalWagesAccrued, restored.TotalWagesAccrued);
        Assert.Equal(employee.NextMinuteWage, restored.NextMinuteWage);
        Assert.Equal(employee.EfficiencyPermille, restored.EfficiencyPermille);
        Assert.Equal(employee.Role, restored.Role);
    }

    [Fact]
    public void ConstructorAndTaskCalculation_RejectInvalidValues()
    {
        Assert.Throws<ArgumentException>(() => new EmployeeId(" "));
        Assert.Throws<ArgumentException>(() =>
            new Employee(new EmployeeId("cashier"), " ", EmployeeRole.Cashier, 1_000, new Money(1_800)));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Employee(new EmployeeId("cashier"), "小葵", EmployeeRole.Cashier, 0, new Money(1_800)));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Employee(new EmployeeId("cashier"), "小葵", EmployeeRole.Cashier, 1_000, Money.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Employee(
                new EmployeeId("cashier"),
                "小葵",
                EmployeeRole.Cashier,
                1_000,
                new Money(long.MaxValue)));

        var employee = CreateEmployee("cashier", 1_000, 1_800);
        Assert.Throws<ArgumentOutOfRangeException>(() => employee.CalculateTaskMinutes(0));
    }

    private static Employee CreateEmployee(string id, int efficiencyPermille, long hourlyWageCents) =>
        new(
            new EmployeeId(id),
            id,
            EmployeeRole.Cashier,
            efficiencyPermille,
            new Money(hourlyWageCents));

    private static Employee RestoreEmployee(
        string id,
        int efficiencyPermille,
        long hourlyWageCents,
        int energy,
        int satisfaction) =>
        Employee.Restore(
            new EmployeeId(id),
            id,
            EmployeeRole.Cashier,
            efficiencyPermille,
            new Money(hourlyWageCents),
            new EmployeeWorkState(0, Money.Zero, 0),
            new EmployeeConditionState(0, energy, satisfaction, 0, 0));
}
