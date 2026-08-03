using HajimaoDesktopShop.Domain.Economy;
using HajimaoDesktopShop.Domain.Employees;
using HajimaoDesktopShop.Domain.Players;
using HajimaoDesktopShop.Domain.Products;
using HajimaoDesktopShop.Domain.Shops;

namespace HajimaoDesktopShop.Domain.Tests.Shops;

public sealed class RetailBusinessTests
{
    [Fact]
    public void Start_CreatesExactlyOneStarterStore()
    {
        var business = CreateBusiness();

        Assert.Equal(Money.FromYuan(1_000m), business.Cash);
        Assert.Equal([new ShopId("corner-store")], business.StoreIds);
        Assert.NotNull(business.GetShop(new ShopId("corner-store")));
    }

    [Fact]
    public void OpenStore_RequiresPlayerLevelWithoutMutatingCash()
    {
        var business = CreateBusiness();
        var definition = new ShopDefinition(
            new ShopId("station-store"),
            "车站便利店",
            requiredPlayerLevel: 2,
            Money.FromYuan(300m));

        var result = business.TryOpenStore(definition);

        Assert.Equal(OpenShopStatus.LevelLocked, result.Status);
        Assert.Equal(Money.FromYuan(1_000m), business.Cash);
        Assert.Single(business.StoreIds);
    }

    [Fact]
    public void OpenStore_ChargesSharedWalletAndRejectsDuplicateAtomically()
    {
        var business = CreateBusiness(totalExperience: 100);
        var definition = new ShopDefinition(
            new ShopId("station-store"),
            "车站便利店",
            requiredPlayerLevel: 2,
            Money.FromYuan(300m));

        var opened = business.TryOpenStore(definition);
        var duplicate = business.TryOpenStore(definition);

        Assert.Equal(OpenShopStatus.Success, opened.Status);
        Assert.Equal(OpenShopStatus.AlreadyOpen, duplicate.Status);
        Assert.Equal(Money.FromYuan(700m), business.Cash);
        Assert.Equal(2, business.StoreIds.Count);
    }

    [Fact]
    public void OpenStore_WithInsufficientFunds_DoesNotAddStore()
    {
        var business = CreateBusiness(totalExperience: 100);
        var definition = new ShopDefinition(
            new ShopId("mall-store"),
            "商场便利店",
            requiredPlayerLevel: 2,
            Money.FromYuan(1_500m));

        var result = business.TryOpenStore(definition);

        Assert.Equal(OpenShopStatus.InsufficientFunds, result.Status);
        Assert.Equal(Money.FromYuan(1_000m), business.Cash);
        Assert.Single(business.StoreIds);
    }

    [Fact]
    public void StoresShareOneWalletButTrackTheirOwnRevenue()
    {
        var business = CreateBusiness(totalExperience: 100);
        var secondId = new ShopId("station-store");
        business.TryOpenStore(new ShopDefinition(
            secondId,
            "车站便利店",
            requiredPlayerLevel: 2,
            Money.FromYuan(300m)));
        var water = new Product(
            new ProductId("water"),
            "矿泉水",
            Money.FromYuan(1m),
            Money.FromYuan(2m));
        var starter = business.GetShop(new ShopId("corner-store"));
        var second = business.GetShop(secondId);
        starter.RegisterProduct(water, capacity: 20);
        second.RegisterProduct(new Product(
            water.Id,
            water.Name,
            water.WholesalePrice,
            water.SalePrice), capacity: 20);

        starter.TryPurchaseStock(water.Id, 2);
        starter.TrySell(water.Id, 1);

        Assert.Equal(Money.FromYuan(700m), starter.Cash);
        Assert.Equal(starter.Cash, second.Cash);
        Assert.Equal(Money.FromYuan(2m), starter.TotalRevenue);
        Assert.Equal(Money.Zero, second.TotalRevenue);
    }

    [Fact]
    public void PayEmployeeMinute_DebitsSharedCashAndRecordsStoreNetProfit()
    {
        var business = CreateBusiness();
        var storeId = new ShopId("corner-store");
        var employee = CreateCashier(hourlyWageCents: 6_000);

        var result = business.TryPayEmployeeMinute(storeId, employee);
        var store = business.GetShop(storeId);

        Assert.Equal(WagePaymentStatus.Success, result.Status);
        Assert.Equal(new Money(100), result.Amount);
        Assert.Equal(Money.FromYuan(999m), business.Cash);
        Assert.Equal(1, employee.WorkedMinutes);
        Assert.Equal(new Money(100), store.TotalWageCost);
        Assert.Equal(new Money(-100), store.TotalNetProfit);
        Assert.Equal(LedgerEntryType.WagePayment, Assert.Single(store.Ledger).Type);
    }

    [Fact]
    public void PayEmployeeMinute_WithInsufficientCash_MutatesNothing()
    {
        var business = CreateBusiness(openingCash: new Money(50));
        var storeId = new ShopId("corner-store");
        var employee = CreateCashier(hourlyWageCents: 6_000);

        var result = business.TryPayEmployeeMinute(storeId, employee);
        var store = business.GetShop(storeId);

        Assert.Equal(WagePaymentStatus.InsufficientFunds, result.Status);
        Assert.Equal(new Money(100), result.Amount);
        Assert.Equal(new Money(50), business.Cash);
        Assert.Equal(0, employee.WorkedMinutes);
        Assert.Equal(Money.Zero, store.TotalWageCost);
        Assert.Empty(store.Ledger);
    }

    [Fact]
    public void PayEmployeeMinute_ForUnknownStore_MutatesNothing()
    {
        var business = CreateBusiness();
        var employee = CreateCashier(hourlyWageCents: 6_000);

        var result = business.TryPayEmployeeMinute(new ShopId("missing-store"), employee);

        Assert.Equal(WagePaymentStatus.UnknownStore, result.Status);
        Assert.Equal(new Money(100), result.Amount);
        Assert.Equal(Money.FromYuan(1_000m), business.Cash);
        Assert.Equal(0, employee.WorkedMinutes);
    }

    private static RetailBusiness CreateBusiness(long totalExperience = 0, Money? openingCash = null)
    {
        var player = new PlayerProfile(new LevelCurve([0, 100, 300]), totalExperience);
        return RetailBusiness.Start(
            player,
            openingCash ?? Money.FromYuan(1_000m),
            new ShopDefinition(
                new ShopId("corner-store"),
                "街角便利店",
                requiredPlayerLevel: 1,
                Money.Zero));
    }

    private static Employee CreateCashier(long hourlyWageCents) =>
        new(
            new EmployeeId("cashier"),
            "小葵",
            EmployeeRole.Cashier,
            efficiencyPermille: 1_000,
            new Money(hourlyWageCents));
}
