using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Catalog;
using HajimaoDesktopShop.Domain.Economy;
using HajimaoDesktopShop.Domain.Players;
using HajimaoDesktopShop.Domain.Shops;

namespace HajimaoDesktopShop.Application.Tests.Business;

public sealed class BusinessGameServiceTests
{
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
}
