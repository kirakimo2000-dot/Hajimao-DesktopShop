using HajimaoDesktopShop.Application.Business.Combat;
using HajimaoDesktopShop.Application.Catalog;
using HajimaoDesktopShop.Application.Simulation;

namespace HajimaoDesktopShop.Application.Tests.Business.Combat;

public sealed class CustomerSpawnPoolServiceTests
{
    [Theory]
    [InlineData(4, "night-customer")]
    [InlineData(5, "morning-customer")]
    [InlineData(8, "morning-customer")]
    [InlineData(9, "day-customer")]
    [InlineData(16, "day-customer")]
    [InlineData(17, "evening-customer")]
    [InlineData(21, "evening-customer")]
    [InlineData(22, "night-customer")]
    [InlineData(23, "night-customer")]
    public void Select_UsesInjectedRealLocalHourWithoutOwningAClock(int localHour, string expectedId)
    {
        var service = CreateSegmentService();

        var selected = service.Select(localHour, [], new LastRollRandomSource());

        Assert.Equal(expectedId, selected.Id);
    }

    [Fact]
    public void Select_ActiveEventCanAddCustomerThatIsAbsentFromBasePool()
    {
        var regular = Customer("regular", "regular");
        var elite = Customer("elite", "elite");
        var service = new CustomerSpawnPoolService(
            [regular, elite],
            [Pool("all-day", 0, 0, (regular.Id, 100))],
            [new CustomerSpawnEventModifierDefinition("festival", "elite", 1_000, 200)]);

        var withoutEvent = service.Select(12, [], new LastRollRandomSource());
        var withEvent = service.Select(12, ["festival"], new LastRollRandomSource());

        Assert.Equal("regular", withoutEvent.Id);
        Assert.Equal("elite", withEvent.Id);
    }

    [Fact]
    public void Select_ActiveEventCanRemoveTaggedCustomerFromFutureSpawns()
    {
        var regular = Customer("regular", "regular");
        var elite = Customer("elite", "elite");
        var service = new CustomerSpawnPoolService(
            [regular, elite],
            [Pool("all-day", 0, 0, (regular.Id, 100), (elite.Id, 10))],
            [new CustomerSpawnEventModifierDefinition("quiet", "regular", 0, 0)]);

        var selected = service.Select(12, ["quiet"], new FirstRollRandomSource());

        Assert.Equal("elite", selected.Id);
    }

    [Fact]
    public void Select_InvalidLocalHour_IsRejected()
    {
        var service = CreateSegmentService();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            service.Select(24, [], new FirstRollRandomSource()));
    }

    private static CustomerSpawnPoolService CreateSegmentService()
    {
        var customers = new[]
        {
            Customer("morning-customer", "morning"),
            Customer("day-customer", "day"),
            Customer("evening-customer", "evening"),
            Customer("night-customer", "night")
        };
        return new CustomerSpawnPoolService(
            customers,
            [
                Pool("morning", 5, 9, (customers[0].Id, 1)),
                Pool("day", 9, 17, (customers[1].Id, 1)),
                Pool("evening", 17, 22, (customers[2].Id, 1)),
                Pool("night", 22, 5, (customers[3].Id, 1))
            ],
            []);
    }

    private static CustomerArchetypeDefinition Customer(string id, string tag) =>
        new(id, 100, 50, 100, [tag], new Dictionary<string, int>(), new Dictionary<string, int> { ["water"] = 1 });

    private static CustomerSpawnPoolDefinition Pool(
        string id,
        int startHour,
        int endHour,
        params (string Id, int Weight)[] entries) =>
        new(id, startHour, endHour, entries.Select(entry => new CustomerSpawnPoolEntry(entry.Id, entry.Weight)).ToArray());

    private sealed class FirstRollRandomSource : IRandomSource
    {
        public double NextDouble() => 0;
        public int Next(int exclusiveMax) => 0;
    }

    private sealed class LastRollRandomSource : IRandomSource
    {
        public double NextDouble() => 0;
        public int Next(int exclusiveMax) => exclusiveMax - 1;
    }
}
