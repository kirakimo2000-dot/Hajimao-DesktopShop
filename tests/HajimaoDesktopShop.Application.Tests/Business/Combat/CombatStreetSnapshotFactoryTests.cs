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
}
