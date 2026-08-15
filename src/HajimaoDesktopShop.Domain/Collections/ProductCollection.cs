namespace HajimaoDesktopShop.Domain.Collections;

public sealed record ProductCollectionEntry(
    string ProductId,
    int MasteryLevel,
    int StoredCopies);

public sealed record ProductCollectionUpdate(
    ProductCollectionEntry Entry,
    bool FirstUnlock,
    bool MasteryIncreased);

public sealed class ProductCollection
{
    private const int MaximumMasteryLevel = 20;
    private readonly Dictionary<string, ProductCollectionEntry> _entries;

    public ProductCollection(IEnumerable<ProductCollectionEntry>? entries = null)
    {
        _entries = new Dictionary<string, ProductCollectionEntry>(StringComparer.Ordinal);
        foreach (var entry in entries ?? [])
        {
            ValidateEntry(entry);
            if (!_entries.TryAdd(entry.ProductId, entry))
            {
                throw new ArgumentException($"Duplicate collection product '{entry.ProductId}'.", nameof(entries));
            }
        }
    }

    public IReadOnlyList<ProductCollectionEntry> Entries =>
        _entries.Values.OrderBy(entry => entry.ProductId, StringComparer.Ordinal).ToArray();

    public bool IsUnlocked(string productId) =>
        !string.IsNullOrWhiteSpace(productId) && _entries.ContainsKey(productId);

    public ProductCollectionUpdate RegisterCopy(string productId)
    {
        if (string.IsNullOrWhiteSpace(productId))
        {
            throw new ArgumentException("Product ID is required.", nameof(productId));
        }

        if (!_entries.TryGetValue(productId, out var current))
        {
            var unlocked = new ProductCollectionEntry(productId, 1, 0);
            _entries.Add(productId, unlocked);
            return new ProductCollectionUpdate(unlocked, FirstUnlock: true, MasteryIncreased: false);
        }

        var storedCopies = checked(current.StoredCopies + 1);
        var mastery = current.MasteryLevel;
        var increased = false;
        var required = CopiesRequired(mastery);
        if (mastery < MaximumMasteryLevel && storedCopies >= required)
        {
            storedCopies -= required;
            mastery++;
            increased = true;
        }

        var updated = current with { MasteryLevel = mastery, StoredCopies = storedCopies };
        _entries[productId] = updated;
        return new ProductCollectionUpdate(updated, FirstUnlock: false, MasteryIncreased: increased);
    }

    public static int CopiesRequired(int level)
    {
        if (level is < 1 or > MaximumMasteryLevel)
        {
            throw new ArgumentOutOfRangeException(nameof(level));
        }

        return level >= MaximumMasteryLevel ? int.MaxValue : 3 + (2 * (level - 1));
    }

    private static void ValidateEntry(ProductCollectionEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.ProductId)
            || entry.MasteryLevel is < 1 or > MaximumMasteryLevel
            || entry.StoredCopies < 0
            || (entry.MasteryLevel < MaximumMasteryLevel
                && entry.StoredCopies >= CopiesRequired(entry.MasteryLevel)))
        {
            throw new ArgumentException("Product collection entry is invalid.", nameof(entry));
        }
    }
}
