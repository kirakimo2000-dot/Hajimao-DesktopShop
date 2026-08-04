using HajimaoDesktopShop.Application.Business.Employees;

namespace HajimaoDesktopShop.Application.Tests.Business.Employees;

public sealed class EmployeeRosterTests
{
    [Theory]
    [InlineData(480, true)]
    [InlineData(959, true)]
    [InlineData(960, false)]
    [InlineData(479, false)]
    public void DayShift_UsesStartInclusiveEndExclusive(int minute, bool expected)
    {
        var shift = new EmployeeShift("employee-1", "store-1", 480, 960);

        Assert.Equal(expected, shift.ContainsMinute(minute));
        Assert.Equal(480, shift.DurationMinutes);
    }

    [Theory]
    [InlineData(1_320, true)]
    [InlineData(1_439, true)]
    [InlineData(0, true)]
    [InlineData(359, true)]
    [InlineData(360, false)]
    [InlineData(1_319, false)]
    public void OvernightShift_WrapsAcrossMidnight(int minute, bool expected)
    {
        var shift = new EmployeeShift("employee-1", "store-1", 1_320, 360);

        Assert.Equal(expected, shift.ContainsMinute(minute));
        Assert.Equal(480, shift.DurationMinutes);
    }

    [Fact]
    public void Schedule_RejectsMoreThanEightHoursOrEmptyShift()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new EmployeeShift("employee-1", "store-1", 480, 961));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new EmployeeShift("employee-1", "store-1", 480, 480));
    }

    [Fact]
    public void Schedule_RejectsInvalidIdentifiersAndMinutes()
    {
        Assert.Throws<ArgumentException>(() =>
            new EmployeeShift(" ", "store-1", 480, 960));
        Assert.Throws<ArgumentException>(() =>
            new EmployeeShift("employee-1", " ", 480, 960));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new EmployeeShift("employee-1", "store-1", -1, 480));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new EmployeeShift("employee-1", "store-1", 480, 1_440));
    }

    [Fact]
    public void Roster_ReplacesEmployeeShiftAndScopesAvailabilityToStore()
    {
        var roster = new EmployeeRoster();
        roster.SetShift(new EmployeeShift("employee-1", "store-1", 480, 960));
        roster.SetShift(new EmployeeShift("employee-1", "store-2", 600, 900));

        Assert.False(roster.IsScheduled("employee-1", "store-1", 700));
        Assert.True(roster.IsScheduled("employee-1", "store-2", 700));
        Assert.False(roster.IsScheduled("employee-1", "store-2", 599));
        Assert.False(roster.IsScheduled("unknown", "store-2", 700));
        Assert.Equal(new EmployeeShift("employee-1", "store-2", 600, 900), roster.GetShift("employee-1"));
        Assert.Single(roster.Shifts);
    }

    [Fact]
    public void Roster_RemoveShiftReturnsWhetherAssignmentExisted()
    {
        var roster = new EmployeeRoster();
        roster.SetShift(new EmployeeShift("employee-1", "store-1", 480, 960));

        Assert.True(roster.RemoveShift("employee-1"));
        Assert.False(roster.RemoveShift("employee-1"));
        Assert.Null(roster.GetShift("employee-1"));
    }
}
