using HajimaoDesktopShop.Domain.Economy;
using HajimaoDesktopShop.Domain.Products;
using HajimaoDesktopShop.Domain.Shops;

namespace HajimaoDesktopShop.Domain.Tests.Shops;

public sealed class ShopTests
{
    [Fact]
    public void PurchaseThenSell_UpdatesCashStockAndLedgerAtomically()
    {
        var water = CreateWater();
        var shop = CreateShop(water, Money.FromYuan(100m));

        var purchase = shop.TryPurchaseStock(water.Id, 10);

        Assert.Equal(StockPurchaseStatus.Success, purchase.Status);
        Assert.Equal(Money.FromYuan(90m), shop.Cash);
        Assert.Equal(10, shop.GetInventory(water.Id).Quantity);

        var sale = shop.TrySell(water.Id, 3);

        Assert.Equal(SaleStatus.Success, sale.Status);
        Assert.Equal(Money.FromYuan(6m), sale.Revenue);
        Assert.Equal(Money.FromYuan(3m), sale.GrossProfit);
        Assert.Equal(Money.FromYuan(96m), shop.Cash);
        Assert.Equal(7, shop.GetInventory(water.Id).Quantity);
        Assert.Collection(
            shop.Ledger,
            entry => Assert.Equal(LedgerEntryType.OpeningBalance, entry.Type),
            entry => Assert.Equal(LedgerEntryType.StockPurchase, entry.Type),
            entry => Assert.Equal(LedgerEntryType.Sale, entry.Type));
    }

    [Fact]
    public void PurchaseThenSell_TracksFinancialTotals()
    {
        var water = CreateWater();
        var shop = CreateShop(water, Money.FromYuan(100m));

        shop.TryPurchaseStock(water.Id, 10);
        shop.TrySell(water.Id, 3);

        Assert.Equal(Money.FromYuan(6m), shop.TotalRevenue);
        Assert.Equal(Money.FromYuan(10m), shop.TotalStockPurchaseCost);
        Assert.Equal(Money.FromYuan(3m), shop.TotalGrossProfit);
    }

    [Fact]
    public void PayForStockOrder_DebitsQuotedCostWithoutChangingInventory()
    {
        var water = CreateWater();
        var shop = CreateShop(water, Money.FromYuan(100m));

        var result = shop.TryPayForStockOrder(water.Id, 3, new Money(85));

        Assert.Equal(StockPurchaseStatus.Success, result.Status);
        Assert.Equal(new Money(255), result.TotalCost);
        Assert.Equal(new Money(9_745), shop.Cash);
        Assert.Equal(new Money(255), shop.TotalStockPurchaseCost);
        Assert.Equal(0, shop.GetInventory(water.Id).Quantity);
    }

    [Fact]
    public void ReceivePaidStock_AddsInventoryWithoutChargingAgain()
    {
        var water = CreateWater();
        var shop = CreateShop(water, Money.FromYuan(100m));
        shop.TryPayForStockOrder(water.Id, 3, new Money(85));

        var result = shop.TryReceivePaidStock(water.Id, 3);

        Assert.Equal(StockReceiptStatus.Success, result.Status);
        Assert.Equal(3, shop.GetInventory(water.Id).Quantity);
        Assert.Equal(new Money(9_745), shop.Cash);
        Assert.Equal(new Money(255), shop.TotalStockPurchaseCost);
    }

    [Fact]
    public void Purchase_WithInsufficientFunds_DoesNotMutateState()
    {
        var water = CreateWater();
        var shop = CreateShop(water, Money.FromYuan(5m));

        var result = shop.TryPurchaseStock(water.Id, 10);

        Assert.Equal(StockPurchaseStatus.InsufficientFunds, result.Status);
        Assert.Equal(Money.FromYuan(5m), shop.Cash);
        Assert.Equal(0, shop.GetInventory(water.Id).Quantity);
        Assert.Single(shop.Ledger);
    }

    [Fact]
    public void Sale_WithInsufficientStock_DoesNotMutateState()
    {
        var water = CreateWater();
        var shop = CreateShop(water, Money.FromYuan(100m));
        shop.TryPurchaseStock(water.Id, 1);

        var result = shop.TrySell(water.Id, 2);

        Assert.Equal(SaleStatus.InsufficientStock, result.Status);
        Assert.Equal(Money.FromYuan(99m), shop.Cash);
        Assert.Equal(1, shop.GetInventory(water.Id).Quantity);
        Assert.Equal(2, shop.Ledger.Count);
    }

    [Fact]
    public void ChangePrice_ForRegisteredProduct_UpdatesSalePrice()
    {
        var water = CreateWater();
        var shop = CreateShop(water, Money.FromYuan(100m));

        var result = shop.TryChangePrice(water.Id, Money.FromYuan(2.5m));

        Assert.Equal(PriceChangeStatus.Success, result.Status);
        Assert.Equal(Money.FromYuan(2.5m), shop.GetInventory(water.Id).Product.SalePrice);
    }

    [Fact]
    public void ChangePrice_WithUnknownProductOrInvalidPrice_DoesNotMutateState()
    {
        var water = CreateWater();
        var shop = CreateShop(water, Money.FromYuan(100m));

        var unknown = shop.TryChangePrice(new ProductId("missing"), Money.FromYuan(3m));
        var invalid = shop.TryChangePrice(water.Id, Money.Zero);

        Assert.Equal(PriceChangeStatus.UnknownProduct, unknown.Status);
        Assert.Equal(PriceChangeStatus.InvalidPrice, invalid.Status);
        Assert.Equal(Money.FromYuan(2m), shop.GetInventory(water.Id).Product.SalePrice);
    }

    [Fact]
    public void Restore_RehydratesFinancialsAndInventoryWithoutReplayingTransactions()
    {
        var water = CreateWater();
        var shop = Shop.Restore(new ShopFinancialState(
            Money.FromYuan(123m),
            Money.FromYuan(80m),
            Money.FromYuan(40m),
            Money.FromYuan(35m)));

        shop.RegisterProduct(water, capacity: 20, initialQuantity: 7);

        Assert.Equal(Money.FromYuan(123m), shop.Cash);
        Assert.Equal(Money.FromYuan(80m), shop.TotalRevenue);
        Assert.Equal(Money.FromYuan(40m), shop.TotalStockPurchaseCost);
        Assert.Equal(Money.FromYuan(35m), shop.TotalGrossProfit);
        Assert.Equal(7, shop.GetInventory(water.Id).Quantity);
        Assert.Empty(shop.Ledger);
    }

    private static Product CreateWater() =>
        new(new ProductId("water"), "矿泉水", Money.FromYuan(1m), Money.FromYuan(2m));

    private static Shop CreateShop(Product product, Money cash)
    {
        var shop = new Shop(cash);
        shop.RegisterProduct(product, capacity: 20);
        return shop;
    }
}
