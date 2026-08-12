using HajimaoDesktopShop.Application.Business.Street;
using HajimaoDesktopShop.Application.Simulation;
using HajimaoDesktopShop.Domain.Streets;

namespace HajimaoDesktopShop.Application.Tests.Business.Street;

public sealed class CommercialStreetTrafficServiceTests
{
    [Fact]
    public void CreateSnapshot_ProjectsTierWeatherSharedTrafficAndStoreShares()
    {
        var service = new CommercialStreetTrafficService(new FixedRandomSource());

        var snapshot = service.CreateSnapshot(
            gameMinute: 720,
            playerLevel: 5,
            [
                new StreetStoreDemand("zeta", "街角店", 8_000),
                new StreetStoreDemand("alpha", "车站店", 8_000)
            ]);

        Assert.Equal(CommercialStreetTier.Neighbors, snapshot.Tier);
        Assert.Equal(StreetWeather.Rain, snapshot.Weather);
        Assert.Equal(6_440, snapshot.SharedTrafficBasisPoints);
        Assert.Equal(4, snapshot.VisiblePedestrians);
        Assert.Equal(1, snapshot.VisibleVehicles);
        Assert.Equal(2, snapshot.VisitorOpportunities);
        Assert.Collection(
            snapshot.Stores,
            store =>
            {
                Assert.Equal("alpha", store.StoreId);
                Assert.Equal(5_000, store.TrafficShareBasisPoints);
            },
            store =>
            {
                Assert.Equal("zeta", store.StoreId);
                Assert.Equal(5_000, store.TrafficShareBasisPoints);
            });
    }

    [Fact]
    public void CreateSnapshot_UsesStoreCountRatherThanPlayerLevelForTierAndOpportunities()
    {
        var service = new CommercialStreetTrafficService(new FixedRandomSource());
        var stores = Enumerable.Range(1, 6)
            .Select(index => new StreetStoreDemand($"store-{index:D4}", $"店铺 {index}", 5_000))
            .ToArray();

        var snapshot = service.CreateSnapshot(gameMinute: 0, playerLevel: 1, stores);

        Assert.Equal(CommercialStreetTier.Block, snapshot.Tier);
        Assert.Equal(6, snapshot.Stores.Count);
        Assert.Equal(4, snapshot.VisitorOpportunities);
    }

    [Fact]
    public void TryRouteVisitor_SingleStoreUsesOneArrivalRoll()
    {
        var random = new FixedRandomSource(doubles: [0.49]);
        var service = new CommercialStreetTrafficService(random);
        var snapshot = service.CreateSnapshot(
            0,
            1,
            [new StreetStoreDemand("corner", "街角店", 5_000)]);

        Assert.Equal("corner", service.TryRouteVisitor(snapshot));
        Assert.Equal(1, random.DoubleCalls);
        Assert.Equal(0, random.IntegerCalls);
    }

    [Fact]
    public void TryRouteVisitor_MultipleStoresUsesOneSharedArrivalAndWeightedRoll()
    {
        var random = new FixedRandomSource(doubles: [0], integers: [7_500]);
        var service = new CommercialStreetTrafficService(random);
        var snapshot = service.CreateSnapshot(
            0,
            3,
            [
                new StreetStoreDemand("alpha", "街角店", 2_000),
                new StreetStoreDemand("zeta", "车站店", 8_000)
            ]);

        Assert.Equal("zeta", service.TryRouteVisitor(snapshot));
        Assert.Equal(1, random.DoubleCalls);
        Assert.Equal(1, random.IntegerCalls);
    }

    [Fact]
    public void CreateSnapshot_PromotesPresentationTierToContainDomainOpenedStores()
    {
        var service = new CommercialStreetTrafficService(new FixedRandomSource());

        var snapshot = service.CreateSnapshot(
            gameMinute: 0,
            playerLevel: 1,
            [
                new StreetStoreDemand("corner", "街角店", 8_000),
                new StreetStoreDemand("station", "车站店", 5_000)
            ]);

        Assert.Equal(CommercialStreetTier.Neighbors, snapshot.Tier);
        Assert.Equal(2, snapshot.Stores.Count);
    }

    private sealed class FixedRandomSource(
        IEnumerable<double>? doubles = null,
        IEnumerable<int>? integers = null) : IRandomSource
    {
        private readonly Queue<double> _doubles = new(doubles ?? []);
        private readonly Queue<int> _integers = new(integers ?? []);

        public int DoubleCalls { get; private set; }

        public int IntegerCalls { get; private set; }

        public double NextDouble()
        {
            DoubleCalls++;
            return _doubles.TryDequeue(out var value) ? value : 1d;
        }

        public int Next(int exclusiveMax)
        {
            IntegerCalls++;
            var value = _integers.TryDequeue(out var next) ? next : 0;
            return Math.Clamp(value, 0, exclusiveMax - 1);
        }
    }
}
