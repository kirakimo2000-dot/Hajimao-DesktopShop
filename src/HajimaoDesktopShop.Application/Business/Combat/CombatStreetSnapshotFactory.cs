using HajimaoDesktopShop.Application.Business.Street;
using HajimaoDesktopShop.Domain.Streets;

namespace HajimaoDesktopShop.Application.Business.Combat;

public static class CombatStreetSnapshotFactory
{
    private const int CrowdPhaseFrames = 24 * 8;
    private static readonly int[] CrowdWave = [-1, 0, 2, 4, 1, -1];
    private static readonly string[] BusyEvents =
    [
        "morning-commute",
        "school-holiday",
        "office-payday",
        "local-festival",
        "night-owls",
        "lost-tour-group",
        "senior-club-visit"
    ];

    public static CommercialStreetSnapshot Create(
        BusinessSnapshot business,
        BusinessCombatSnapshot combat,
        long presentationFrame = 0)
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
            var combatStore = combat.Stores.SingleOrDefault(item => item.StoreId == store.Id);
            var share = index == ordered.Length - 1
                ? remainingShare
                : weights[index] * 10_000 / totalWeight;
            remainingShare -= share;
            return new CommercialStreetStoreSnapshot(
                store.Id,
                store.Name,
                weights[index] * 10,
                share,
                store.FacadeStyleKey,
                combatStore?.IsStaffed ?? false);
        }).ToArray();
        var tier = ordered.Length switch
        {
            <= 1 => CommercialStreetTier.Corner,
            2 => CommercialStreetTier.Neighbors,
            <= 4 => CommercialStreetTier.Street,
            _ => CommercialStreetTier.Block
        };
        var activeCustomers = combat.Stores.Sum(store => store.State.Customers.Count);
        var phase = (int)(Math.Max(0, presentationFrame) / CrowdPhaseFrames % CrowdWave.Length);
        var activeEvents = combat.ActiveEventTags ?? [];
        var eventDelta = activeEvents.Contains("quiet-weekday", StringComparer.Ordinal)
            || activeEvents.Contains("rainy-evening", StringComparer.Ordinal)
                ? -1
                : activeEvents.Any(tag => BusyEvents.Contains(tag, StringComparer.Ordinal))
                    ? 1
                    : 0;
        var visiblePedestrians = Math.Clamp(
            Math.Min(2, ordered.Length) + Math.Min(2, activeCustomers) + CrowdWave[phase] + eventDelta,
            0,
            6);
        return new CommercialStreetSnapshot(
            tier,
            StreetWeather.Clear,
            Math.Min(10_000, visiblePedestrians * 1_667),
            visiblePedestrians,
            ordered.Length >= 3 ? Math.Min(4, visiblePedestrians / 2) : 0,
            streetStores);
    }
}
