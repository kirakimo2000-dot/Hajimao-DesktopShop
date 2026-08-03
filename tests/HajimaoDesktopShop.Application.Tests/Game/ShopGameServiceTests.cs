using HajimaoDesktopShop.Application.Catalog;
using HajimaoDesktopShop.Application.Game;
using HajimaoDesktopShop.Domain.Shops;

namespace HajimaoDesktopShop.Application.Tests.Game;

public sealed class ShopGameServiceTests
{
    [Fact]
    public void PurchaseAndPriceChange_AreVisibleInFreshSnapshot()
    {
        var game = CreateGame();

        var purchase = game.PurchaseStock("water", 5);
        var priceChange = game.ChangePrice("water", 250);
        var snapshot = game.GetSnapshot();
        var water = Assert.Single(snapshot.Products, product => product.Id == "water");

        Assert.Equal(StockPurchaseStatus.Success, purchase.Status);
        Assert.Equal(PriceChangeStatus.Success, priceChange.Status);
        Assert.Equal(9_500, snapshot.CashCents);
        Assert.Equal(500, snapshot.StockPurchaseCostCents);
        Assert.Equal(5, water.Quantity);
        Assert.Equal(250, water.SalePriceCents);
        Assert.Equal("ambient", water.ShelfKind);
    }

    [Fact]
    public void Sell_UpdatesSnapshotFinancialTotals()
    {
        var game = CreateGame();
        game.PurchaseStock("water", 5);

        var sale = game.Sell("water", 2);
        var snapshot = game.GetSnapshot();

        Assert.Equal(SaleStatus.Success, sale.Status);
        Assert.Equal(9_900, snapshot.CashCents);
        Assert.Equal(400, snapshot.RevenueCents);
        Assert.Equal(500, snapshot.StockPurchaseCostCents);
        Assert.Equal(200, snapshot.GrossProfitCents);
    }

    [Fact]
    public void Snapshot_IsDetachedFromLaterOperations()
    {
        var game = CreateGame();
        var before = game.GetSnapshot();

        game.PurchaseStock("water", 5);
        var after = game.GetSnapshot();

        Assert.Equal(0, Assert.Single(before.Products, product => product.Id == "water").Quantity);
        Assert.Equal(5, Assert.Single(after.Products, product => product.Id == "water").Quantity);
    }

    private static ShopGameService CreateGame() =>
        new(
            [
                new ProductDefinition("water", "矿泉水", 100, 200, 20, "ambient"),
                new ProductDefinition("milk", "鲜牛奶", 280, 450, 12, "chilled")
            ],
            openingCashCents: 10_000);
}
