using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Business.Combat;
using HajimaoDesktopShop.Domain.Combat;
using HajimaoDesktopShop.Domain.Streets;

namespace HajimaoDesktopShop.Application.Tests.Business.Combat;

public sealed class CombatStreetSnapshotFactoryTests
{
    [Fact]
    public void Create_ProjectsOnlyOpenCombatStoresIntoAnUnboundedStreet()
    {
        var stores = Enumerable.Range(1, 6)
            .Select(index => new BusinessStoreSnapshot(
                $"store-{index}",
                $"店铺 {index}",
                0, 0, 0, [],
                StoreFormatId: index % 2 == 0 ? "discount" : "premium",
                StreetOrdinal: index))
            .ToArray();
        var combatStores = stores
            .Select(store => new StoreCombatSnapshot(
                store.Id, StoreCombatState.Empty, [], [], 0, 0, 0, 0,
                store.StoreFormatId,
                StoreCombatProfilePolicy.Resolve(store.StoreFormatId)))
            .ToArray();

        var street = CombatStreetSnapshotFactory.Create(
            new BusinessSnapshot(1, 0, 0, stores),
            new BusinessCombatSnapshot(0, combatStores, [], []));

        Assert.Equal(CommercialStreetTier.Block, street.Tier);
        Assert.Equal(6, street.Stores.Count);
        Assert.Equal(stores.Select(store => store.Name), street.Stores.Select(store => store.StoreName));
        Assert.Equal(10_000, street.Stores.Sum(store => store.TrafficShareBasisPoints));
    }

    [Fact]
    public void Create_CyclesOneStoreStreetThroughQuietAndCrowdedWaves()
    {
        var (business, combat) = OneStoreStreet();

        var pedestrians = Enumerable.Range(0, 6)
            .Select(phase => CombatStreetSnapshotFactory.Create(
                business,
                combat,
                presentationFrame: phase * 24 * 8).VisiblePedestrians)
            .ToArray();

        Assert.Equal([0, 1, 3, 5, 2, 0], pedestrians);
    }

    [Fact]
    public void Create_RuntimeEventsShiftTheVisibleCrowdWithoutGameTime()
    {
        var (business, combat) = OneStoreStreet();
        const int peakFrame = 2 * 24 * 8;

        var normal = CombatStreetSnapshotFactory.Create(business, combat, peakFrame);
        var festival = CombatStreetSnapshotFactory.Create(
            business,
            combat with { ActiveEventTags = ["local-festival"] },
            peakFrame);
        var quiet = CombatStreetSnapshotFactory.Create(
            business,
            combat with { ActiveEventTags = ["quiet-weekday"] },
            peakFrame);

        Assert.Equal(normal.VisiblePedestrians + 1, festival.VisiblePedestrians);
        Assert.Equal(normal.VisiblePedestrians - 1, quiet.VisiblePedestrians);
    }

    [Fact]
    public void Create_ProjectsStaffingStateForEachSpecificStorefront()
    {
        var store = new BusinessStoreSnapshot(
            "corner-store", "街角店", 0, 0, 0, [],
            StoreFormatId: "convenience", StreetOrdinal: 1);
        var combatStore = new StoreCombatSnapshot(
            store.Id, StoreCombatState.Empty, [], [], 0, 0, 0, 0,
            store.StoreFormatId, StoreCombatProfilePolicy.Resolve(store.StoreFormatId),
            IsStaffed: false);

        var street = CombatStreetSnapshotFactory.Create(
            new BusinessSnapshot(1, 0, 0, [store]),
            new BusinessCombatSnapshot(0, [combatStore], [], []));

        Assert.False(Assert.Single(street.Stores).IsStaffed);
    }

    private static (BusinessSnapshot Business, BusinessCombatSnapshot Combat) OneStoreStreet()
    {
        var store = new BusinessStoreSnapshot(
            "corner-store",
            "街角店",
            0, 0, 0, [],
            StoreFormatId: "convenience",
            StreetOrdinal: 1);
        var combatStore = new StoreCombatSnapshot(
            store.Id,
            StoreCombatState.Empty,
            [], [], 0, 0, 0, 0,
            store.StoreFormatId,
            StoreCombatProfilePolicy.Resolve(store.StoreFormatId));
        return (
            new BusinessSnapshot(1, 0, 0, [store]),
            new BusinessCombatSnapshot(0, [combatStore], [], []));
    }
}
