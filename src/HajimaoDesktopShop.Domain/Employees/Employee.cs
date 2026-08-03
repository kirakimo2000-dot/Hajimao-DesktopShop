using HajimaoDesktopShop.Domain.Economy;

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
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Employee name is required.", nameof(name));
        }

        if (efficiencyPermille <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(efficiencyPermille));
        }

        if (!hourlyWage.IsPositive)
        {
            throw new ArgumentOutOfRangeException(nameof(hourlyWage));
        }

        Id = id;
        Name = name.Trim();
        Role = role;
        EfficiencyPermille = efficiencyPermille;
        HourlyWage = hourlyWage;
    }

    public EmployeeId Id { get; }

    public string Name { get; }

    public EmployeeRole Role { get; }

    public int EfficiencyPermille { get; }

    public Money HourlyWage { get; }

    public int WorkedMinutes { get; private set; }

    public Money TotalWagesAccrued { get; private set; }

    public Money NextMinuteWage =>
        new(checked(_wageRemainder + HourlyWage.Cents) / 60L);

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
}
