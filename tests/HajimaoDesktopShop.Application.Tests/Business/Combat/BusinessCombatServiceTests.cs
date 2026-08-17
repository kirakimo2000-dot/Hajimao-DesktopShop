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
    public async Task GetSnapshot_WaitsForAnInProgressTickSoStateIsAtomic()
    {
        using var random = new BlockingRandomSource(41);
        var service = new BusinessCombatService(
            Game(),
            Content(),
            random,
            options: new BusinessCombatOptions(10_000, 1));

        var tickTask = Task.Run(() => service.Tick(12, []));
        Assert.True(random.WaitUntilBlocked(TimeSpan.FromSeconds(2)));

        var snapshotTask = Task.Run(service.GetSnapshot);
        await Task.Delay(100);

        Assert.False(snapshotTask.IsCompleted, "Snapshot escaped while Tick was mutating combat state.");

        random.Release();
        await Task.WhenAll(tickTask, snapshotTask);
    }

    [Fact]
    public void Restore_RemovesProjectilesWhoseProductsAreNoLongerCombatContent()
    {
        var state = new StoreCombatState(
            3,
            0,
            0,
            [new ActiveCustomerState(
                1, "regular", 100, 5_000, 10, ["regular"],
                new Dictionary<string, int>(), 0, 0, 100)],
            [new ProductProjectileState(
                2, "dish_soap", 1, 1, 100, ["household"],
                ProductCombatEffectKind.None, 0, 1)]);
        var restored = new CombatSaveData(
            new ProductCollectionSaveData([new ProductCollectionEntry("water", 1, 0)]),
            [new StoreProductLoadoutSaveData("corner-store", 3, ["water"])],
            [new StoreCombatStateSaveData("corner-store", state)],
            41,
            new LegacyCombatCompatibilitySaveData([]));

        var service = new BusinessCombatService(
            Game(),
            Content(),
            new ZeroRandomSource(41),
            restored,
            new BusinessCombatOptions(0, 1));

        Assert.Empty(service.GetSnapshot().Stores.Single().State.Projectiles);
        service.Tick(12, []);
    }

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
    public void Tick_AppliesTheFinalProductsBonusDropEffect()
    {
        var service = new BusinessCombatService(
            Game(),
            Content(ProductEffectKind.BonusDrop, effectStrengthPermille: 90),
            new ScriptedStatefulRandomSource(41, 0, 199, 0, 0),
            options: new BusinessCombatOptions(10_000, 1));

        BusinessCombatSnapshot snapshot = service.Tick(12, []);
        for (var tick = 0; tick < 6; tick++)
        {
            snapshot = service.Tick(12, []);
        }

        var store = Assert.Single(snapshot.Stores);
        Assert.Equal(1, store.DroppedProducts);
        Assert.Contains(store.DropRolls, roll =>
            roll.Source == "equipment-bonus-product" && roll.Awarded);
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

    private static CombatContentCatalog Content(
        ProductEffectKind effect = ProductEffectKind.None,
        int effectStrengthPermille = 0)
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
                "water", 100, 1, 1_100, effect, effectStrengthPermille, ["liquid"], 1)],
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

    private sealed class ScriptedStatefulRandomSource(
        ulong state,
        params int[] values) : IStatefulRandomSource
    {
        private readonly Queue<int> _values = new(values);

        public ulong State { get; private set; } = state;

        public void RestoreState(ulong restoredState) => State = restoredState;

        public double NextDouble() => throw new NotSupportedException();

        public int Next(int exclusiveMax)
        {
            var value = _values.Dequeue();
            Assert.InRange(value, 0, exclusiveMax - 1);
            State++;
            return value;
        }
    }

    private sealed class BlockingRandomSource(ulong state) : IStatefulRandomSource, IDisposable
    {
        private readonly ManualResetEventSlim _entered = new();
        private readonly ManualResetEventSlim _release = new();

        public ulong State { get; private set; } = state;

        public void RestoreState(ulong restoredState) => State = restoredState;

        public double NextDouble() => 0;

        public int Next(int exclusiveMax)
        {
            _entered.Set();
            _release.Wait(TimeSpan.FromSeconds(5));
            State++;
            return 0;
        }

        public bool WaitUntilBlocked(TimeSpan timeout) => _entered.Wait(timeout);

        public void Release() => _release.Set();

        public void Dispose()
        {
            _release.Set();
            _entered.Dispose();
            _release.Dispose();
        }
    }
}
