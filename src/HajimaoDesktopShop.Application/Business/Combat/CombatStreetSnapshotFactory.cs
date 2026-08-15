using HajimaoDesktopShop.Application.Business.Street;
using HajimaoDesktopShop.Domain.Streets;

namespace HajimaoDesktopShop.Application.Business.Combat;

public static class CombatStreetSnapshotFactory
{
    public static CommercialStreetSnapshot Create(
        BusinessSnapshot business,
        BusinessCombatSnapshot combat)
    {
        ArgumentNullException.ThrowIfNull(business);
        ArgumentNullException.ThrowIfNull(combat);
        var ordered = business.Stores
            .OrderBy(store => store.StreetOrdinal)
            .ThenBy(store => store.Id, StringComparer.Ordinal)
            .ToArray();
        var weights = ordered
            .Select(store => Math.Max(
                1,
                combat.Stores.SingleOrDefault(item => item.StoreId == store.Id)?.Profile?.ArrivalModifierPermille
                ?? 1_000))
            .ToArray();
        var totalWeight = weights.Sum();
        var remainingShare = 10_000;
        var streetStores = ordered.Select((store, index) =>
        {
            var share = index == ordered.Length - 1
                ? remainingShare
                : weights[index] * 10_000 / totalWeight;
            remainingShare -= share;
            return new CommercialStreetStoreSnapshot(
                store.Id,
                store.Name,
                weights[index] * 10,
                share,
                store.FacadeStyleKey);
        }).ToArray();
        var tier = ordered.Length switch
        {
            <= 1 => CommercialStreetTier.Corner,
            2 => CommercialStreetTier.Neighbors,
            <= 4 => CommercialStreetTier.Street,
            _ => CommercialStreetTier.Block
        };
        var activeCustomers = combat.Stores.Sum(store => store.State.Customers.Count);
        return new CommercialStreetSnapshot(
            tier,
            StreetWeather.Clear,
            10_000,
            Math.Min(18, Math.Max(1, ordered.Length * 2 + activeCustomers)),
            ordered.Length >= 3 ? Math.Min(4, ordered.Length / 2) : 0,
            streetStores);
    }
}
