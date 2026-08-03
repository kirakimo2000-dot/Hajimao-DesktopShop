using HajimaoDesktopShop.Domain.Economy;

namespace HajimaoDesktopShop.Domain.Products;

public sealed class Product
{
    public Product(ProductId id, string name, Money wholesalePrice, Money salePrice)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Product name is required.", nameof(name));
        }

        if (!wholesalePrice.IsPositive)
        {
            throw new ArgumentException("Wholesale price must be positive.", nameof(wholesalePrice));
        }

        if (!salePrice.IsPositive)
        {
            throw new ArgumentException("Sale price must be positive.", nameof(salePrice));
        }

        Id = id;
        Name = name.Trim();
        WholesalePrice = wholesalePrice;
        SalePrice = salePrice;
    }

    public ProductId Id { get; }

    public string Name { get; }

    public Money WholesalePrice { get; }

    public Money SalePrice { get; private set; }

    public void ChangeSalePrice(Money price)
    {
        if (!price.IsPositive)
        {
            throw new ArgumentException("Sale price must be positive.", nameof(price));
        }

        SalePrice = price;
    }
}
