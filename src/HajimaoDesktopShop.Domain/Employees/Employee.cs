using HajimaoDesktopShop.Domain.Economy;
using System.Numerics;

namespace HajimaoDesktopShop.Domain.Employees;

public sealed class Employee
{
    private long _wageRemainder;

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
            new EmployeeWorkState(0, Money.Zero, 0))
    {
    }

    private Employee(
        EmployeeId id,
        string name,
        EmployeeRole role,
        int efficiencyPermille,
        Money hourlyWage,
        EmployeeWorkState workState)
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
        ValidateWorkState(hourlyWage, workState);

        Id = id;
        Name = name.Trim();
        Role = role;
        EfficiencyPermille = efficiencyPermille;
        HourlyWage = hourlyWage;
        WorkedMinutes = workState.WorkedMinutes;
        TotalWagesAccrued = workState.TotalWagesAccrued;
        _wageRemainder = workState.WageRemainderCents;
    }

    public static Employee Restore(
        EmployeeId id,
        string name,
        EmployeeRole role,
        int efficiencyPermille,
        Money hourlyWage,
        EmployeeWorkState workState) =>
        new(id, name, role, efficiencyPermille, hourlyWage, workState);

    public EmployeeId Id { get; }

    public string Name { get; }

    public EmployeeRole Role { get; }

    public int EfficiencyPermille { get; }

    public Money HourlyWage { get; }

    public int WorkedMinutes { get; private set; }

    public Money TotalWagesAccrued { get; private set; }

    public Money NextMinuteWage =>
        new(checked(_wageRemainder + HourlyWage.Cents) / 60L);

    public EmployeeWorkState CaptureWorkState() =>
        new(WorkedMinutes, TotalWagesAccrued, _wageRemainder);

    public int CalculateTaskMinutes(int baseTaskMinutes)
    {
        if (baseTaskMinutes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(baseTaskMinutes));
        }

        var scaled = checked((long)baseTaskMinutes * 1_000L);
        return checked((int)((scaled + EfficiencyPermille - 1L) / EfficiencyPermille));
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
}
