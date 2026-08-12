namespace HajimaoDesktopShop.Application.Catalog;

public sealed record ProductDefinition
{
    public ProductDefinition(
        string id,
        string name,
        long wholesalePriceCents,
        long initialSalePriceCents,
        int capacity,
        string shelfKind,
        int requiredPlayerLevel = 1,
        string categoryId = "general",
        string? iconKey = null,
        IReadOnlyList<string>? regionTags = null)
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

        if (requiredPlayerLevel < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(requiredPlayerLevel));
        }

        if (string.IsNullOrWhiteSpace(categoryId))
        {
            throw new ArgumentException("Product category is required.", nameof(categoryId));
        }

        var normalizedRegions = (regionTags ?? ["global"])
            .Select(tag => tag.Trim())
            .Where(tag => tag.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (normalizedRegions.Length == 0)
        {
            throw new ArgumentException("At least one product region is required.", nameof(regionTags));
        }

        Id = id.Trim();
        Name = name.Trim();
        WholesalePriceCents = wholesalePriceCents;
        InitialSalePriceCents = initialSalePriceCents;
        Capacity = capacity;
        ShelfKind = shelfKind.Trim();
        RequiredPlayerLevel = requiredPlayerLevel;
        CategoryId = categoryId.Trim();
        IconKey = string.IsNullOrWhiteSpace(iconKey) ? $"product-{Id}" : iconKey.Trim();
        RegionTags = Array.AsReadOnly(normalizedRegions);
    }

    public string Id { get; }

    public string Name { get; }

    public long WholesalePriceCents { get; }

    public long InitialSalePriceCents { get; }

    public int Capacity { get; }

    public string ShelfKind { get; }

    public int RequiredPlayerLevel { get; }

    public string CategoryId { get; }

    public string IconKey { get; }

    public IReadOnlyList<string> RegionTags { get; }
}
