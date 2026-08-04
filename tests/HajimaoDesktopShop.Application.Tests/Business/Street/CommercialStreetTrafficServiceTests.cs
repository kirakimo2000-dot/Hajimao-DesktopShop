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

        Assert.Equal(CommercialStreetTier.Street, snapshot.Tier);
        Assert.Equal(StreetWeather.Rain, snapshot.Weather);
        Assert.Equal(6_440, snapshot.SharedTrafficBasisPoints);
        Assert.Equal(4, snapshot.VisiblePedestrians);
        Assert.Equal(1, snapshot.VisibleVehicles);
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
