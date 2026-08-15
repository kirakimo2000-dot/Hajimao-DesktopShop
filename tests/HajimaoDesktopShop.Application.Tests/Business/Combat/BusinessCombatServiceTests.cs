using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Business.Combat;
using HajimaoDesktopShop.Application.Catalog;
using HajimaoDesktopShop.Application.Persistence;
using HajimaoDesktopShop.Application.Simulation;
using HajimaoDesktopShop.Domain.Collections;
using HajimaoDesktopShop.Domain.Combat;
using HajimaoDesktopShop.Domain.Economy;
using HajimaoDesktopShop.Domain.Players;
using HajimaoDesktopShop.Domain.Shops;

namespace HajimaoDesktopShop.Application.Tests.Business.Combat;

public sealed class BusinessCombatServiceTests
{
    [Fact]
    public void Tick_RewardsAndDropsOnlyWhenCustomerIsServed()
    {
        var game = Game();
        var service = new BusinessCombatService(
            game,
            Content(),
            new ZeroRandomSource(41),
            options: new BusinessCombatOptions(10_000, 1));

        var arrival = service.Tick(12, []);
        var served = arrival;
        for (var tick = 0; tick < 6; tick++)
        {
            served = service.Tick(12, []);
        }

        Assert.Equal(1_000, arrival.CashCents);
        Assert.Equal(1_110, served.CashCents);
        var store = Assert.Single(served.Stores);
        Assert.Equal(1, store.ServedCustomers);
        Assert.Equal(1, store.EncounteredCustomers);
        Assert.Equal(100, store.TotalDamage);
        Assert.Equal(110, store.RevenueCents);
        Assert.Equal(1, store.DroppedProducts);
        var water = served.Collection.Single(entry => entry.ProductId == "water");
        Assert.Equal(1, water.StoredCopies);
    }

    [Fact]
    public void Tick_EscapedCustomerNeverAwardsRevenueOrDrop()
    {
        var game = Game();
        var state = new StoreCombatState(
            2,
            0,
            0,
            [new ActiveCustomerState(
                1, "regular", 100, 1, 10, ["regular"],
                new Dictionary<string, int>(), 0, 0)],
            []);
        var restored = new CombatSaveData(
            new ProductCollectionSaveData([new ProductCollectionEntry("water", 1, 0)]),
            [new StoreProductLoadoutSaveData("corner-store", 3, [])],
            [new StoreCombatStateSaveData("corner-store", state)],
            41,
            new LegacyCombatCompatibilitySaveData([]));
        var service = new BusinessCombatService(
            game,
            Content(),
            new ZeroRandomSource(41),
            restored,
            new BusinessCombatOptions(0, 1));

        var snapshot = service.Tick(12, []);

        Assert.Equal(1_000, snapshot.CashCents);
        var store = Assert.Single(snapshot.Stores);
        Assert.Equal(1, store.EscapedCustomers);
        Assert.Equal(0, store.ServedCustomers);
        Assert.Equal(0, store.DroppedProducts);
    }

    [Fact]
    public void CaptureAndRestore_UsesStableStoreOrderAndDoesNotProgressWhileClosed()
    {
        var game = Game(includeSecondStore: true);
        var service = new BusinessCombatService(
            game,
            Content(),
            new ZeroRandomSource(77),
            options: new BusinessCombatOptions(0, 1));

        var captured = service.CaptureSaveData();
        var restored = new BusinessCombatService(
            game,
            Content(),
            new ZeroRandomSource(1),
            captured,
            new BusinessCombatOptions(0, 1));

        Assert.Equal(["corner-store", "station-store"], captured.Stores.Select(store => store.StoreId));
        Assert.Equivalent(captured, restored.CaptureSaveData(), strict: true);
    }

    private static BusinessGameService Game(bool includeSecondStore = false)
    {
        var stores = new[]
        {
            new ShopDefinition(new ShopId("corner-store"), "街角店", 1, Money.Zero),
            new ShopDefinition(new ShopId("station-store"), "车站店", 1, Money.Zero)
        };
        var game = new BusinessGameService(
            [new ProductDefinition("water", "矿泉水", 100, 180, 24, "ambient")],
            stores,
            new LevelCurve([0, 10, 100]),
            "corner-store",
            1_000);
        if (includeSecondStore)
        {
            Assert.Equal(OpenShopStatus.Success, game.OpenStore("station-store").Status);
        }

        return game;
    }

    private static CombatContentCatalog Content()
    {
        var customer = new CustomerArchetypeDefinition(
            "regular",
            100,
            10,
            100,
            ["regular"],
            new Dictionary<string, int>(),
            new Dictionary<string, int> { ["water"] = 100 });
        return new CombatContentCatalog(
            [new ProductCombatDefinition(
                "water", 100, 1, 1_100, ProductEffectKind.None, 0, ["liquid"], 1)],
            [customer],
            [new CustomerSpawnPoolDefinition(
                "all-day", 0, 0, [new CustomerSpawnPoolEntry(customer.Id, 1)])],
            [],
            [new CharacterDefinition("maomao-default", "humanoid-v1", "maomao-default", 30)],
            []);
    }

    private sealed class ZeroRandomSource(ulong state) : IStatefulRandomSource
    {
        public ulong State { get; private set; } = state;

        public void RestoreState(ulong restoredState) => State = restoredState;

        public double NextDouble() => 0;

        public int Next(int exclusiveMax)
        {
            State++;
            return 0;
        }
    }
}
