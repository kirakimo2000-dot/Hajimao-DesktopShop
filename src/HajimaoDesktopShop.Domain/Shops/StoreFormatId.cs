namespace HajimaoDesktopShop.Domain.Shops;

public readonly record struct StoreFormatId
{
    public StoreFormatId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Store format ID is required.", nameof(value));
        }

        Value = value.Trim();
    }

    public string Value { get; }

    public override string ToString() => Value;
}
