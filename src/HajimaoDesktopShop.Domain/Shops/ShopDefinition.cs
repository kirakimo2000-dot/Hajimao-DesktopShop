using HajimaoDesktopShop.Domain.Economy;

namespace HajimaoDesktopShop.Domain.Shops;

public sealed record ShopDefinition
{
    public ShopDefinition(
        ShopId id,
        StoreBrandId brandId,
        StoreFormatId formatId,
        string name,
        int streetOrdinal,
        Money openingCost)
        : this(
            id,
            brandId,
            formatId,
            name,
            streetOrdinal,
            openingCost,
            legacyRequiredPlayerLevel: 1)
    {
    }

    public ShopDefinition(ShopId id, string name, int requiredPlayerLevel, Money openingCost)
        : this(
            id,
            new StoreBrandId(id.Value),
            new StoreFormatId("legacy"),
            name,
            streetOrdinal: requiredPlayerLevel,
            openingCost,
            legacyRequiredPlayerLevel: requiredPlayerLevel)
    {
    }

    private ShopDefinition(
        ShopId id,
        StoreBrandId brandId,
        StoreFormatId formatId,
        string name,
        int streetOrdinal,
        Money openingCost,
        int legacyRequiredPlayerLevel)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Shop name is required.", nameof(name));
        }

        if (streetOrdinal < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(streetOrdinal));
        }

        if (openingCost.Cents < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(openingCost));
        }

        Id = id;
        BrandId = brandId;
        FormatId = formatId;
        Name = name.Trim();
        StreetOrdinal = streetOrdinal;
        RequiredPlayerLevel = legacyRequiredPlayerLevel;
        OpeningCost = openingCost;
    }

    public ShopId Id { get; }

    public string Name { get; }

    public StoreBrandId BrandId { get; }

    public StoreFormatId FormatId { get; }

    public int StreetOrdinal { get; }

    // Transitional compatibility for Application contracts removed in the next portfolio task.
    public int RequiredPlayerLevel { get; }

    public Money OpeningCost { get; }
}
