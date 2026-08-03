namespace HajimaoDesktopShop.Application.Catalog;

public sealed record ProductDefinition
{
    public ProductDefinition(
        string id,
        string name,
        long wholesalePriceCents,
        long initialSalePriceCents,
        int capacity,
        string shelfKind)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Product ID is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Product name is required.", nameof(name));
        }

        if (wholesalePriceCents <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(wholesalePriceCents));
        }

        if (initialSalePriceCents <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialSalePriceCents));
        }

        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        if (string.IsNullOrWhiteSpace(shelfKind))
        {
            throw new ArgumentException("Shelf kind is required.", nameof(shelfKind));
        }

        Id = id.Trim();
        Name = name.Trim();
        WholesalePriceCents = wholesalePriceCents;
        InitialSalePriceCents = initialSalePriceCents;
        Capacity = capacity;
        ShelfKind = shelfKind.Trim();
    }

    public string Id { get; }

    public string Name { get; }

    public long WholesalePriceCents { get; }

    public long InitialSalePriceCents { get; }

    public int Capacity { get; }

    public string ShelfKind { get; }
}
