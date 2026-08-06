using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Business.Procurement;
using HajimaoDesktopShop.Application.Catalog;
using HajimaoDesktopShop.Domain.Economy;
using HajimaoDesktopShop.Domain.Employees;
using HajimaoDesktopShop.Domain.Players;
using HajimaoDesktopShop.Domain.Shops;

namespace HajimaoDesktopShop.Application.Tests.Business;

public sealed class BusinessGameServiceTests
{
    [Fact]
    public void NewBusiness_StartsWithBalancedAutomaticStockingWithoutImmediatePurchase()
    {
        var game = BusinessTestSessionFactory.Create().Game;

        var procurement = game.GetProcurementSnapshot();
        var policies = procurement.AutoRestockPolicies
            .Where(policy => policy.StoreId == "store-1")
            .ToArray();

        Assert.Equal(2, policies.Length);
        Assert.All(policies, policy =>
        {
            Assert.True(policy.IsEnabled);
            Assert.Equal("regional-distributor", policy.PreferredChannelId);
            Assert.True(policy.UseEmergencySupplierWhenOutOfStock);
        });
        Assert.Empty(procurement.PendingOrders);
    }

    [Fact]
    public void OpenStore_AddsBalancedAutomaticStockingWithoutImmediatePurchase()
    {
        var game = BusinessTestSessionFactory.Create().Game;

        var result = game.OpenStore("store-2");
        var procurement = game.GetProcurementSnapshot();

        Assert.Equal(OpenShopStatus.Success, result.Status);
        Assert.Equal(
            ["bread", "water"],
            procurement.AutoRestockPolicies
                .Where(policy => policy.StoreId == "store-2")
                .Select(policy => policy.ProductId));
        Assert.All(
            procurement.AutoRestockPolicies.Where(policy => policy.StoreId == "store-2"),
            policy => Assert.True(policy.IsEnabled));
        Assert.Empty(procurement.PendingOrders);
    }

    [Fact]
    public void RestoredBusiness_PreservesExactAutomaticStockingPolicies()
    {
        var session = BusinessTestSessionFactory.Create();
        session.Game.ConfigureAutoRestock(new AutoRestockPolicy(
            "store-1",
            "water",
            IsEnabled: false,
            ReorderPoint: 5,
            TargetQuantity: 20,
            PreferredChannelId: "local-wholesale",
            UseEmergencySupplierWhenOutOfStock: false));
        var expected = session.Game.GetProcurementSnapshot().AutoRestockPolicies;

        var restored = BusinessTestSessionFactory.Restore(session.CaptureSaveData());

        Assert.Equal(expected, restored.Game.GetProcurementSnapshot().AutoRestockPolicies);
    }

    [Fact]
    public void StoreCatalog_ExposesOpenAndLockedStoresWithoutLeakingDefinitions()
    {
        var service = CreateService();

        var stores = service.GetStoreCatalogSnapshot();

        Assert.Collection(
            stores,
            store =>
            {
                Assert.True(store.IsOpen);
                Assert.Equal("corner-store", store.Id);
            },
            store =>
            {
                Assert.False(store.IsOpen);
                Assert.Equal("station-store", store.Id);
                Assert.Equal(2, store.RequiredPlayerLevel);
                Assert.Equal(30_000, store.OpeningCostCents);
            });
    }

    [Fact]
    public void NewBusiness_StartsWithOneStoreAndOnlyLevelOneProducts()
    {
        var service = CreateService();

        var snapshot = service.GetSnapshot();

        Assert.Equal(1, snapshot.PlayerLevel);
        var store = Assert.Single(snapshot.Stores);
        Assert.Equal("corner-store", store.Id);
        Assert.Equal(["water"], store.Products.Select(product => product.Id));
    }

    [Fact]
    public void SuccessfulSale_AwardsExperienceAndUnlocksProductsIdempotently()
    {
        var service = CreateService();
        service.PurchaseStock("corner-store", "water", 2);

        var first = service.Sell("corner-store", "water", 1);
        var second = service.Sell("corner-store", "water", 1);

        Assert.Equal(1, first.PreviousPlayerLevel);
        Assert.Equal(2, first.CurrentPlayerLevel);
        Assert.Equal(["milk"], first.NewlyUnlockedProductIds);
        Assert.Empty(second.NewlyUnlockedProductIds);
        Assert.Equal(2, service.GetSnapshot().PlayerLevel);
        Assert.Equal(
            ["water", "milk"],
            Assert.Single(service.GetSnapshot().Stores).Products.Select(product => product.Id));
    }

    [Fact]
    public void OpenStore_UsesLevelGateSharedCashAndCurrentProductUnlocks()
    {
        var service = CreateService();
        var locked = service.OpenStore("station-store");
        service.PurchaseStock("corner-store", "water", 1);
        service.Sell("corner-store", "water", 1);

        var opened = service.OpenStore("station-store");
        var snapshot = service.GetSnapshot();

        Assert.Equal(OpenShopStatus.LevelLocked, locked.Status);
        Assert.Equal(OpenShopStatus.Success, opened.Status);
        Assert.Equal(2, snapshot.Stores.Count);
        Assert.All(snapshot.Stores, store => Assert.Equal(["water", "milk"], store.Products.Select(p => p.Id)));
        Assert.Equal(20_100, snapshot.CashCents);
    }

    [Fact]
    public void ChangePrice_IsStoreSpecificAndRetainsCatalogReferencePrice()
    {
        var service = CreateService();
        service.PurchaseStock("corner-store", "water", 1);
        service.Sell("corner-store", "water", 1);
        service.OpenStore("station-store");

        var result = service.ChangePrice("corner-store", "water", 260);
        var snapshot = service.GetSnapshot();
        var cornerWater = snapshot.Stores.Single(store => store.Id == "corner-store")
            .Products.Single(product => product.Id == "water");
        var stationWater = snapshot.Stores.Single(store => store.Id == "station-store")
            .Products.Single(product => product.Id == "water");

        Assert.Equal(PriceChangeStatus.Success, result.Status);
        Assert.Equal(260, cornerWater.SalePriceCents);
        Assert.Equal(200, cornerWater.ReferenceSalePriceCents);
        Assert.Equal(200, stationWater.SalePriceCents);
        Assert.Equal(200, stationWater.ReferenceSalePriceCents);
    }

    [Fact]
    public void PayEmployeeMinute_UpdatesSharedCashAndSelectedStoreFinance()
    {
        var service = CreateService();
        var employee = new Employee(
            new EmployeeId("cashier"),
            "小葵",
            EmployeeRole.Cashier,
            efficiencyPermille: 1_000,
            hourlyWage: new Money(6_000));

        var result = service.PayEmployeeMinute("corner-store", employee);
        var snapshot = service.GetSnapshot();
        var store = Assert.Single(snapshot.Stores);

        Assert.Equal(WagePaymentStatus.Success, result.Status);
        Assert.Equal(49_900, snapshot.CashCents);
        Assert.Equal(100, store.WageCostCents);
        Assert.Equal(-100, store.NetProfitCents);
        Assert.Equal(1, employee.WorkedMinutes);
    }

    [Fact]
    public void CaptureAndRestore_RoundTripsEveryStoreAndFinancialCounter()
    {
        var service = CreateService();
        service.PurchaseStock("corner-store", "water", 3);
        service.ChangePrice("corner-store", "water", 260);
        service.Sell("corner-store", "water", 1);
        service.OpenStore("station-store");
        service.PurchaseStock("station-store", "milk", 2);
        service.ChangePrice("station-store", "milk", 525);
        var employee = new Employee(
            new EmployeeId("station-cashier"),
            "小满",
            EmployeeRole.Cashier,
            1_100,
            new Money(6_000));
        service.PayEmployeeMinute("station-store", employee);

        var save = service.CaptureSaveData();
        var restored = CreateRestoredService(save);

        Assert.Equivalent(service.GetSnapshot(), restored.GetSnapshot(), strict: true);
    }

    private static BusinessGameService CreateService() =>
        new(
            [
                new ProductDefinition("water", "矿泉水", 100, 200, 20, "ambient", 1),
                new ProductDefinition("milk", "鲜牛奶", 300, 480, 12, "chilled", 2),
                new ProductDefinition("ice", "冰淇淋", 360, 620, 12, "frozen", 3)
            ],
            [
                new ShopDefinition(new ShopId("corner-store"), "街角便利店", 1, Money.Zero),
                new ShopDefinition(new ShopId("station-store"), "车站便利店", 2, new Money(30_000))
            ],
            new LevelCurve([0, 10, 30]),
            starterShopId: "corner-store",
            openingCashCents: 50_000,
            experiencePerItemSold: 10);

    private static BusinessGameService CreateRestoredService(
        HajimaoDesktopShop.Application.Persistence.BusinessSaveData save) =>
        new(
            [
                new ProductDefinition("water", "矿泉水", 100, 200, 20, "ambient", 1),
                new ProductDefinition("milk", "鲜牛奶", 300, 480, 12, "chilled", 2),
                new ProductDefinition("ice", "冰淇淋", 360, 620, 12, "frozen", 3)
            ],
            [
                new ShopDefinition(new ShopId("corner-store"), "街角便利店", 1, Money.Zero),
                new ShopDefinition(new ShopId("station-store"), "车站便利店", 2, new Money(30_000))
            ],
            new LevelCurve([0, 10, 30]),
            save,
            experiencePerItemSold: 10);
}
