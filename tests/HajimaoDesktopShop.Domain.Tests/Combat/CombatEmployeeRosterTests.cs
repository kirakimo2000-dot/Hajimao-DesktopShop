using HajimaoDesktopShop.Domain.Combat;

namespace HajimaoDesktopShop.Domain.Tests.Combat;

public sealed class CombatEmployeeRosterTests
{
    [Fact]
    public void Assign_MovesAnOwnedEmployeeBetweenStoresWithoutDuplicatingIt()
    {
        var roster = new CombatEmployeeRoster(
        [
            new CombatEmployeeInstance(
                "maomao-0001", "maomao-default", "毛毛", "corner-store")
        ]);

        var moved = roster.Assign("maomao-0001", "station-store");

        var employee = Assert.Single(moved.Employees);
        Assert.Equal("station-store", employee.AssignedStoreId);
        Assert.Empty(moved.AssignedTo("corner-store"));
        Assert.Single(moved.AssignedTo("station-store"));
    }

    [Fact]
    public void Assign_ReleasesTheEmployeePreviouslyOccupyingTheTargetStore()
    {
        var roster = new CombatEmployeeRoster(
        [
            new CombatEmployeeInstance("maomao-0001", "maomao-default", "毛毛", "corner-store"),
            new CombatEmployeeInstance("guest-0001", "guest", "小雨", "station-store")
        ]);

        var moved = roster.Assign("maomao-0001", "station-store");

        Assert.Equal("station-store", moved.Employees.Single(item => item.InstanceId == "maomao-0001").AssignedStoreId);
        Assert.Null(moved.Employees.Single(item => item.InstanceId == "guest-0001").AssignedStoreId);
    }
}
