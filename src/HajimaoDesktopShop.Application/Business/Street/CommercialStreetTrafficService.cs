using HajimaoDesktopShop.Application.Simulation;
using HajimaoDesktopShop.Domain.Streets;

namespace HajimaoDesktopShop.Application.Business.Street;

public sealed class CommercialStreetTrafficService
{
    private readonly IRandomSource _random;

    public CommercialStreetTrafficService(IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(random);
        _random = random;
    }

    public CommercialStreetSnapshot CreateSnapshot(
        long activeRuntimeTick,
        int playerLevel,
        IEnumerable<StreetStoreDemand> storeDemands)
    {
        ArgumentNullException.ThrowIfNull(storeDemands);
        var stores = ValidateAndOrder(storeDemands);
        ArgumentOutOfRangeException.ThrowIfLessThan(playerLevel, 1);
        var tier = CommercialStreetTrafficModel.GetTierForStorefrontCount(stores.Length);

        var weather = CommercialStreetTrafficModel.GetWeather(activeRuntimeTick);
        var traffic = CommercialStreetTrafficModel.CalculateSharedTrafficBasisPoints(
            stores.Max(store => store.AttractionBasisPoints),
            stores.Length,
            weather);
        var attractionTotal = stores.Sum(store => (long)store.AttractionBasisPoints);
        var remainingShare = attractionTotal == 0 ? 0 : 10_000;
        var snapshots = new CommercialStreetStoreSnapshot[stores.Length];
        for (var index = 0; index < stores.Length; index++)
        {
            var isLast = index == stores.Length - 1;
            var share = attractionTotal == 0
                ? 0
                : isLast
                    ? remainingShare
                    : checked((int)(stores[index].AttractionBasisPoints * 10_000L / attractionTotal));
            remainingShare -= share;
            snapshots[index] = new CommercialStreetStoreSnapshot(
                stores[index].StoreId,
                stores[index].StoreName,
                stores[index].AttractionBasisPoints,
                share,
                stores[index].FacadeStyleKey);
        }

        return new CommercialStreetSnapshot(
            tier,
            weather,
            traffic,
            CommercialStreetTrafficModel.GetVisiblePedestrianCount(traffic),
            CommercialStreetTrafficModel.GetVisibleVehicleCount(traffic, weather),
            Array.AsReadOnly(snapshots),
            CommercialStreetTrafficModel.GetVisitorOpportunityCount(stores.Length));
    }

    public string? TryRouteVisitor(CommercialStreetSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.Stores.Count == 0)
        {
            return null;
        }

        if (_random.NextDouble() >= snapshot.SharedTrafficBasisPoints / 10_000d)
        {
            return null;
        }

        if (snapshot.Stores.Count == 1)
        {
            return snapshot.Stores[0].StoreId;
        }

        var totalAttraction = checked(snapshot.Stores.Sum(store => store.AttractionBasisPoints));
        if (totalAttraction <= 0)
        {
            return null;
        }

        var roll = _random.Next(totalAttraction);
        foreach (var store in snapshot.Stores)
        {
            if (roll < store.AttractionBasisPoints)
            {
                return store.StoreId;
            }

            roll -= store.AttractionBasisPoints;
        }

        throw new InvalidOperationException("Commercial street routing exhausted its attraction weights.");
    }

    private static StreetStoreDemand[] ValidateAndOrder(IEnumerable<StreetStoreDemand> storeDemands)
    {
        var stores = storeDemands.ToArray();
        if (stores.Length == 0 || stores.Any(store => store is null))
        {
            throw new ArgumentException("At least one street store demand is required.", nameof(storeDemands));
        }

        foreach (var store in stores)
        {
            if (string.IsNullOrWhiteSpace(store.StoreId)
                || string.IsNullOrWhiteSpace(store.StoreName)
                || string.IsNullOrWhiteSpace(store.FacadeStyleKey)
                || store.AttractionBasisPoints is < 0 or > 10_000)
            {
                throw new ArgumentException("Street store demand is invalid.", nameof(storeDemands));
            }
        }

        if (stores.Select(store => store.StoreId).Distinct(StringComparer.Ordinal).Count() != stores.Length)
        {
            throw new ArgumentException("Street store IDs must be unique.", nameof(storeDemands));
        }

        return stores.OrderBy(store => store.StoreId, StringComparer.Ordinal).ToArray();
    }
}
