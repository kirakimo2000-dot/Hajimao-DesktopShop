using HajimaoDesktopShop.Domain.Economy;
using HajimaoDesktopShop.Domain.Players;
using HajimaoDesktopShop.Domain.Products;
using HajimaoDesktopShop.Domain.Shops;

namespace HajimaoDesktopShop.Domain.Tests.Shops;

public sealed class StoreDevelopmentTests
{
    [Fact]
    public void NewStore_StartsWithDocumentedGrowthLimitsAndDerivedValues()
    {
        var development = StoreDevelopment.CreateNew();

        Assert.Equal(0, development.ExpansionLevel);
        Assert.Equal(0, development.ShelfLevel);
        Assert.Equal(0, development.DecorationLevel);
        Assert.Equal(1, development.FloorAreaUnits);
        Assert.Equal(3, development.ShelfSlotCount);
        Assert.Equal(0, development.QueueComfortCapacity);
        Assert.Equal(1_000, development.InventoryCapacityPermille);
        Assert.Equal(0, development.AttractionBonusBasisPoints);
        Assert.Equal(new Money(60_000), development.PreviewUpgrade(StoreUpgradeKind.Expansion).Cost);
        Assert.Equal(new Money(25_000), development.PreviewUpgrade(StoreUpgradeKind.Shelf).Cost);
        Assert.Equal(new Money(30_000), development.PreviewUpgrade(StoreUpgradeKind.Decoration).Cost);
    }

    [Fact]
    public void ShelfUpgrade_IncreasesExistingAndFutureProductCapacity()
    {
        var business = CreateBusiness(openingCash: new Money(1_000_000));
        var storeId = new ShopId("corner-store");
        var shop = business.GetShop(storeId);
        var water = CreateProduct("water");
        var bread = CreateProduct("bread");
        shop.RegisterProduct(water, capacity: 20);

        var result = business.TryUpgradeStore(storeId, StoreUpgradeKind.Shelf);
        shop.RegisterProduct(bread, capacity: 16);

        Assert.Equal(StoreUpgradeStatus.Success, result.Status);
        Assert.Equal(new Money(25_000), result.Cost);
        Assert.Equal(1_250, shop.Development.InventoryCapacityPermille);
        Assert.Equal(25, shop.GetInventory(water.Id).Capacity);
        Assert.Equal(20, shop.GetInventory(bread.Id).Capacity);
    }

    [Fact]
    public void Upgrade_WithMissingPrerequisiteOrFunds_IsAtomic()
    {
        var business = CreateBusiness(openingCash: new Money(100_000));
        var storeId = new ShopId("corner-store");
        Assert.Equal(
            StoreUpgradeStatus.Success,
            business.TryUpgradeStore(storeId, StoreUpgradeKind.Shelf).Status);
        var cashBeforeBlockedUpgrade = business.Cash;

        var blocked = business.TryUpgradeStore(storeId, StoreUpgradeKind.Shelf);
        var poor = CreateBusiness(openingCash: new Money(24_999));
        var insufficient = poor.TryUpgradeStore(storeId, StoreUpgradeKind.Shelf);

        Assert.Equal(StoreUpgradeStatus.PrerequisiteNotMet, blocked.Status);
        Assert.Equal(cashBeforeBlockedUpgrade, business.Cash);
        Assert.Equal(1, business.GetShop(storeId).Development.ShelfLevel);
        Assert.Equal(StoreUpgradeStatus.InsufficientFunds, insufficient.Status);
        Assert.Equal(new Money(24_999), poor.Cash);
        Assert.Equal(0, poor.GetShop(storeId).Development.ShelfLevel);
    }

    [Fact]
    public void DevelopmentExpense_ReducesNetProfitAndCreatesLedgerEntry()
    {
        var business = CreateBusiness(openingCash: new Money(100_000));
        var storeId = new ShopId("corner-store");
        var shop = business.GetShop(storeId);

        business.TryUpgradeStore(storeId, StoreUpgradeKind.Shelf);

        Assert.Equal(new Money(75_000), business.Cash);
        Assert.Equal(new Money(25_000), shop.TotalOperatingCost);
        Assert.Equal(new Money(-25_000), shop.TotalNetProfit);
        var entry = Assert.Single(shop.Ledger);
        Assert.Equal(LedgerEntryType.StoreDevelopment, entry.Type);
        Assert.Equal(new Money(-25_000), entry.Amount);
    }

    [Fact]
    public void Restore_ValidatesLevelsAndRehydratesDerivedCapacity()
    {
        var shop = Shop.Restore(
            new ShopFinancialState(
                new Money(100_000),
                Money.Zero,
                Money.Zero,
                Money.Zero,
                Money.Zero,
                new Money(12_000)),
            new StoreDevelopmentState(2, 3, 2));
        var water = CreateProduct("water");
        shop.RegisterProduct(water, capacity: 20, initialQuantity: 30);

        Assert.Equal(1_750, shop.Development.InventoryCapacityPermille);
        Assert.Equal(35, shop.GetInventory(water.Id).Capacity);
        Assert.Equal(30, shop.GetInventory(water.Id).Quantity);
        Assert.Equal(new Money(12_000), shop.TotalOperatingCost);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            StoreDevelopment.Restore(new StoreDevelopmentState(5, 0, 0)));
        Assert.Throws<ArgumentException>(() =>
            StoreDevelopment.Restore(new StoreDevelopmentState(0, 2, 0)));
    }

    [Fact]
    public void Upgrade_RejectsUnknownStoreWithoutCharging()
    {
        var business = CreateBusiness(openingCash: new Money(100_000));

        var result = business.TryUpgradeStore(new ShopId("missing"), StoreUpgradeKind.Expansion);

        Assert.Equal(StoreUpgradeStatus.UnknownStore, result.Status);
        Assert.Equal(new Money(100_000), business.Cash);
    }

    private static RetailBusiness CreateBusiness(Money openingCash)
    {
        var player = new PlayerProfile(new LevelCurve([0, 100, 300]));
        return RetailBusiness.Start(
            player,
            openingCash,
            new ShopDefinition(
                new ShopId("corner-store"),
                "街角便利店",
                requiredPlayerLevel: 1,
                Money.Zero));
    }

    private static Product CreateProduct(string id) =>
        new(new ProductId(id), id, new Money(100), new Money(200));
}
