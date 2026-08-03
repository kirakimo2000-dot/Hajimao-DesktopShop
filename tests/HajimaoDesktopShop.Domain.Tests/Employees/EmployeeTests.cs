using HajimaoDesktopShop.Domain.Economy;
using HajimaoDesktopShop.Domain.Employees;

namespace HajimaoDesktopShop.Domain.Tests.Employees;

public sealed class EmployeeTests
{
    [Fact]
    public void Efficiency_ChangesRequiredTaskMinutesWithIntegerCeiling()
    {
        var slower = CreateEmployee("slow", efficiencyPermille: 800, hourlyWageCents: 1_800);
        var faster = CreateEmployee("fast", efficiencyPermille: 1_500, hourlyWageCents: 2_600);

        Assert.Equal(13, slower.CalculateTaskMinutes(baseTaskMinutes: 10));
        Assert.Equal(7, faster.CalculateTaskMinutes(baseTaskMinutes: 10));
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
    public void ConstructorAndTaskCalculation_RejectInvalidValues()
    {
        Assert.Throws<ArgumentException>(() => new EmployeeId(" "));
        Assert.Throws<ArgumentException>(() =>
            new Employee(new EmployeeId("cashier"), " ", EmployeeRole.Cashier, 1_000, new Money(1_800)));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Employee(new EmployeeId("cashier"), "小葵", EmployeeRole.Cashier, 0, new Money(1_800)));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Employee(new EmployeeId("cashier"), "小葵", EmployeeRole.Cashier, 1_000, Money.Zero));

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
}
