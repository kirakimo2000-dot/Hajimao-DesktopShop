namespace HajimaoDesktopShop.Domain.Shops;

public readonly record struct StoreBrandId
{
    public StoreBrandId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Store brand ID is required.", nameof(value));
        }

        Value = value.Trim();
    }

    public string Value { get; }

    public override string ToString() => Value;
}
