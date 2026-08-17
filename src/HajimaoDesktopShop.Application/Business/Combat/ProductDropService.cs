using HajimaoDesktopShop.Application.Catalog;
using HajimaoDesktopShop.Application.Simulation;

namespace HajimaoDesktopShop.Application.Business.Combat;

public sealed record ProductDropRoll(
    string Source,
    int Roll,
    int ExclusiveMaximum,
    string? ProductId,
    bool Awarded);

public sealed record ProductDropResult(
    IReadOnlyList<string> ProductIds,
    IReadOnlyList<ProductDropRoll> Rolls);

public sealed class ProductDropService
{
    private const int NormalNoDropWeight = 100;
    private const int EliteBonusChanceBasisPoints = 2_500;
    private readonly HashSet<string> _knownProductIds;
    private readonly IRandomSource _random;

    public ProductDropService(
        IReadOnlyList<ProductCombatDefinition> products,
        IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(products);
        ArgumentNullException.ThrowIfNull(random);
        if (products.Count == 0
            || products.Any(product => string.IsNullOrWhiteSpace(product.ProductId))
            || products.Select(product => product.ProductId).Distinct(StringComparer.Ordinal).Count() != products.Count)
        {
            throw new ArgumentException("Unique product combat definitions are required.", nameof(products));
        }

        _knownProductIds = products.Select(product => product.ProductId).ToHashSet(StringComparer.Ordinal);
        _random = random;
    }

    public ProductDropResult Roll(
        CustomerArchetypeDefinition customer,
        int bonusDropPermille = 0)
    {
        ArgumentNullException.ThrowIfNull(customer);
        if (bonusDropPermille is < 0 or > 900)
        {
            throw new ArgumentOutOfRangeException(nameof(bonusDropPermille));
        }

        var table = customer.ProductDropWeights?.ToArray()
            ?? throw new ArgumentException("Customer drop table is required.", nameof(customer));
        if (table.Length == 0
            || table.Any(entry => !_knownProductIds.Contains(entry.Key) || entry.Value <= 0))
        {
            throw new ArgumentException("Customer drop table is invalid.", nameof(customer));
        }

        var products = new List<string>(2);
        var rolls = new List<ProductDropRoll>(3);
        var productWeight = checked(table.Sum(entry => entry.Value));
        var normalMaximum = checked(productWeight + NormalNoDropWeight);
        var normalRoll = _random.Next(normalMaximum);
        ValidateRoll(normalRoll, normalMaximum);
        var normalProduct = normalRoll < productWeight
            ? SelectProduct(table, normalRoll)
            : null;
        if (normalProduct is not null)
        {
            products.Add(normalProduct);
        }

        rolls.Add(new ProductDropRoll(
            "normal",
            normalRoll,
            normalMaximum,
            normalProduct,
            normalProduct is not null));

        if (bonusDropPermille > 0)
        {
            var chanceRoll = _random.Next(1_000);
            ValidateRoll(chanceRoll, 1_000);
            var bonusAwarded = chanceRoll < bonusDropPermille;
            rolls.Add(new ProductDropRoll(
                "equipment-bonus-chance",
                chanceRoll,
                1_000,
                null,
                bonusAwarded));
            if (bonusAwarded)
            {
                var productRoll = _random.Next(productWeight);
                ValidateRoll(productRoll, productWeight);
                var bonusProduct = SelectProduct(table, productRoll);
                products.Add(bonusProduct);
                rolls.Add(new ProductDropRoll(
                    "equipment-bonus-product",
                    productRoll,
                    productWeight,
                    bonusProduct,
                    Awarded: true));
            }
        }

        if (customer.Tags.Contains("elite", StringComparer.Ordinal))
        {
            var chanceRoll = _random.Next(10_000);
            ValidateRoll(chanceRoll, 10_000);
            var bonusAwarded = chanceRoll < EliteBonusChanceBasisPoints;
            rolls.Add(new ProductDropRoll(
                "elite-chance",
                chanceRoll,
                10_000,
                null,
                bonusAwarded));
            if (bonusAwarded)
            {
                var productRoll = _random.Next(productWeight);
                ValidateRoll(productRoll, productWeight);
                var bonusProduct = SelectProduct(table, productRoll);
                products.Add(bonusProduct);
                rolls.Add(new ProductDropRoll(
                    "elite-product",
                    productRoll,
                    productWeight,
                    bonusProduct,
                    Awarded: true));
            }
        }

        return new ProductDropResult(products.ToArray(), rolls.ToArray());
    }

    private static string SelectProduct(
        IReadOnlyList<KeyValuePair<string, int>> table,
        int roll)
    {
        foreach (var entry in table)
        {
            if (roll < entry.Value)
            {
                return entry.Key;
            }

            roll -= entry.Value;
        }

        throw new InvalidOperationException("Product drop selection failed.");
    }

    private static void ValidateRoll(int roll, int exclusiveMaximum)
    {
        if (roll < 0 || roll >= exclusiveMaximum)
        {
            throw new InvalidOperationException("Random source returned an out-of-range product roll.");
        }
    }
}
