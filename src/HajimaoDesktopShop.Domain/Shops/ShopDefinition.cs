using HajimaoDesktopShop.Domain.Economy;

namespace HajimaoDesktopShop.Domain.Shops;

public sealed record ShopDefinition
{
    public ShopDefinition(ShopId id, string name, int requiredPlayerLevel, Money openingCost)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Shop name is required.", nameof(name));
        }

        if (requiredPlayerLevel < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(requiredPlayerLevel));
        }

        if (openingCost.Cents < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(openingCost));
        }

        Id = id;
        Name = name.Trim();
        RequiredPlayerLevel = requiredPlayerLevel;
        OpeningCost = openingCost;
    }

    public ShopId Id { get; }

    public string Name { get; }

    public int RequiredPlayerLevel { get; }

    public Money OpeningCost { get; }
}
