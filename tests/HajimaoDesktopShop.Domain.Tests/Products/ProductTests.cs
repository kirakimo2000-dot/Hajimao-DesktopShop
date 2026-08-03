using HajimaoDesktopShop.Domain.Economy;
using HajimaoDesktopShop.Domain.Products;

namespace HajimaoDesktopShop.Domain.Tests.Products;

public sealed class ProductTests
{
    [Fact]
    public void ChangeSalePrice_UpdatesPositivePrice()
    {
        var product = CreateProduct();

        product.ChangeSalePrice(Money.FromYuan(2.5m));

        Assert.Equal(Money.FromYuan(2.5m), product.SalePrice);
    }

    [Fact]
    public void Constructor_RejectsInvalidIdentityNameAndPrices()
    {
        Assert.Throws<ArgumentException>(() => new ProductId(" "));
        Assert.Throws<ArgumentException>(() =>
            new Product(new ProductId("water"), " ", Money.FromYuan(1m), Money.FromYuan(2m)));
        Assert.Throws<ArgumentException>(() =>
            new Product(new ProductId("water"), "矿泉水", Money.Zero, Money.FromYuan(2m)));
        Assert.Throws<ArgumentException>(() =>
            new Product(new ProductId("water"), "矿泉水", Money.FromYuan(1m), Money.Zero));
    }

    private static Product CreateProduct() =>
        new(new ProductId("water"), "矿泉水", Money.FromYuan(1m), Money.FromYuan(2m));
}
