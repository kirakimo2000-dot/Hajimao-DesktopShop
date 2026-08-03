namespace HajimaoDesktopShop.Domain.Shops;

public readonly record struct ShopId
{
    public ShopId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Shop ID is required.", nameof(value));
        }

        Value = value.Trim();
    }

    public string Value { get; }

    public override string ToString() => Value;
}
