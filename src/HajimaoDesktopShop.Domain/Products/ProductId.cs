namespace HajimaoDesktopShop.Domain.Products;

public readonly record struct ProductId
{
    public ProductId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Product ID is required.", nameof(value));
        }

        Value = value.Trim();
    }

    public string Value { get; }

    public override string ToString() => Value;
}
