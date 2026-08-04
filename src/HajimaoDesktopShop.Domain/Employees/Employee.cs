using HajimaoDesktopShop.Domain.Economy;
using System.Numerics;

namespace HajimaoDesktopShop.Domain.Employees;

public sealed class Employee
{
    private const int MaximumTrainingLevel = 5;
    private const int MaximumConditionPermille = 1_000;
    private const int WorkMinutesPerSatisfactionLoss = 60;
    private const int RestMinutesPerSatisfactionGain = 120;

    private long _wageRemainder;
    private int _workMinutesTowardSatisfactionLoss;
    private int _restMinutesTowardSatisfactionGain;

    public Employee(
        EmployeeId id,
        string name,
        EmployeeRole role,
        int efficiencyPermille,
        Money hourlyWage)
        : this(
            id,
            name,
            role,
            efficiencyPermille,
            hourlyWage,
            new EmployeeWorkState(0, Money.Zero, 0),
            new EmployeeConditionState(0, 1_000, 700, 0, 0))
    {
    }

    private Employee(
        EmployeeId id,
        string name,
        EmployeeRole role,
        int efficiencyPermille,
        Money hourlyWage,
        EmployeeWorkState workState,
        EmployeeConditionState conditionState)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Employee name is required.", nameof(name));
        }

        if (efficiencyPermille <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(efficiencyPermille));
        }

        if (!hourlyWage.IsPositive || hourlyWage.Cents > long.MaxValue - 59L)
        {
            throw new ArgumentOutOfRangeException(nameof(hourlyWage));
        }

        ArgumentNullException.ThrowIfNull(workState);
        ArgumentNullException.ThrowIfNull(conditionState);
        ValidateWorkState(hourlyWage, workState);
        ValidateConditionState(conditionState);

        Id = id;
        Name = name.Trim();
        Role = role;
        EfficiencyPermille = efficiencyPermille;
        HourlyWage = hourlyWage;
        WorkedMinutes = workState.WorkedMinutes;
        TotalWagesAccrued = workState.TotalWagesAccrued;
        _wageRemainder = workState.WageRemainderCents;
        TrainingLevel = conditionState.TrainingLevel;
        EnergyPermille = conditionState.EnergyPermille;
        SatisfactionPermille = conditionState.SatisfactionPermille;
        _workMinutesTowardSatisfactionLoss = conditionState.WorkMinutesTowardSatisfactionLoss;
        _restMinutesTowardSatisfactionGain = conditionState.RestMinutesTowardSatisfactionGain;
    }

    public static Employee Restore(
        EmployeeId id,
        string name,
        EmployeeRole role,
        int efficiencyPermille,
        Money hourlyWage,
        EmployeeWorkState workState,
        EmployeeConditionState? conditionState = null) =>
        new(
            id,
            name,
            role,
            efficiencyPermille,
            hourlyWage,
            workState,
            conditionState ?? new EmployeeConditionState(0, 1_000, 700, 0, 0));

    public EmployeeId Id { get; }

    public string Name { get; }

    public EmployeeRole Role { get; }

    public int EfficiencyPermille { get; }

    public Money HourlyWage { get; }

    public int TrainingLevel { get; private set; }

    public int EnergyPermille { get; private set; }

    public int SatisfactionPermille { get; private set; }

    public int EffectiveEfficiencyPermille
    {
        get
        {
            var trainingMultiplier = 1_000L + (TrainingLevel * 50L);
            var energyMultiplier = 500L + (EnergyPermille / 2L);
            var satisfactionMultiplier = 750L + (SatisfactionPermille * 300L / 1_000L);
            var composed = checked(
                (long)EfficiencyPermille
                * trainingMultiplier
                * energyMultiplier
                * satisfactionMultiplier);
            return checked((int)Math.Max(1L, composed / 1_000_000_000L));
        }
    }

    public bool CanWork => EnergyPermille > 0;

    public int WorkedMinutes { get; private set; }

    public Money TotalWagesAccrued { get; private set; }

    public Money NextMinuteWage =>
        new(checked(_wageRemainder + HourlyWage.Cents) / 60L);

    public EmployeeWorkState CaptureWorkState() =>
        new(WorkedMinutes, TotalWagesAccrued, _wageRemainder);

    public EmployeeConditionState CaptureConditionState() =>
        new(
            TrainingLevel,
            EnergyPermille,
            SatisfactionPermille,
            _workMinutesTowardSatisfactionLoss,
            _restMinutesTowardSatisfactionGain);

    public int CalculateTaskMinutes(int baseTaskMinutes)
    {
        if (baseTaskMinutes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(baseTaskMinutes));
        }

        var scaled = checked((long)baseTaskMinutes * 1_000L);
        var effectiveEfficiency = EffectiveEfficiencyPermille;
        return checked((int)((scaled + effectiveEfficiency - 1L) / effectiveEfficiency));
    }

    public void CompleteTraining()
    {
        if (TrainingLevel >= MaximumTrainingLevel)
        {
            throw new InvalidOperationException("Employee has reached the maximum training level.");
        }

        TrainingLevel++;
    }

    public void RecordWorkedConditionMinute()
    {
        if (!CanWork)
        {
            throw new InvalidOperationException("An exhausted employee cannot work.");
        }

        EnergyPermille = Math.Max(0, EnergyPermille - 2);
        _restMinutesTowardSatisfactionGain = 0;
        _workMinutesTowardSatisfactionLoss++;
        if (_workMinutesTowardSatisfactionLoss < WorkMinutesPerSatisfactionLoss)
        {
            return;
        }

        _workMinutesTowardSatisfactionLoss = 0;
        SatisfactionPermille = Math.Max(0, SatisfactionPermille - 1);
    }

    public void RecordRestMinute()
    {
        EnergyPermille = Math.Min(MaximumConditionPermille, EnergyPermille + 4);
        _workMinutesTowardSatisfactionLoss = 0;
        _restMinutesTowardSatisfactionGain++;
        if (_restMinutesTowardSatisfactionGain < RestMinutesPerSatisfactionGain)
        {
            return;
        }

        _restMinutesTowardSatisfactionGain = 0;
        SatisfactionPermille = Math.Min(MaximumConditionPermille, SatisfactionPermille + 1);
    }

    public Money RecordWorkedMinute()
    {
        WorkedMinutes = checked(WorkedMinutes + 1);
        _wageRemainder = checked(_wageRemainder + HourlyWage.Cents);
        var chargedCents = _wageRemainder / 60L;
        _wageRemainder %= 60L;
        var charged = new Money(chargedCents);
        TotalWagesAccrued += charged;
        return charged;
    }

    private static void ValidateWorkState(Money hourlyWage, EmployeeWorkState state)
    {
        if (state.WorkedMinutes < 0
            || state.TotalWagesAccrued.Cents < 0
            || state.WageRemainderCents is < 0 or >= 60)
        {
            throw new ArgumentOutOfRangeException(nameof(state));
        }

        var accumulated = (BigInteger)state.WorkedMinutes * hourlyWage.Cents;
        if (accumulated / 60 != state.TotalWagesAccrued.Cents
            || accumulated % 60 != state.WageRemainderCents)
        {
            throw new ArgumentException("Employee work state does not match the hourly wage.", nameof(state));
        }
    }

    private static void ValidateConditionState(EmployeeConditionState state)
    {
        if (state.TrainingLevel is < 0 or > MaximumTrainingLevel
            || state.EnergyPermille is < 0 or > MaximumConditionPermille
            || state.SatisfactionPermille is < 0 or > MaximumConditionPermille
            || state.WorkMinutesTowardSatisfactionLoss is < 0 or >= WorkMinutesPerSatisfactionLoss
            || state.RestMinutesTowardSatisfactionGain is < 0 or >= RestMinutesPerSatisfactionGain)
        {
            throw new ArgumentOutOfRangeException(nameof(state));
        }

        if (state.WorkMinutesTowardSatisfactionLoss > 0
            && state.RestMinutesTowardSatisfactionGain > 0)
        {
            throw new ArgumentException("Work and rest satisfaction progress cannot both be active.", nameof(state));
        }
    }
}
