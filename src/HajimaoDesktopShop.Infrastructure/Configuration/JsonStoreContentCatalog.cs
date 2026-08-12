using System.Text.Json;
using HajimaoDesktopShop.Application.Catalog;

namespace HajimaoDesktopShop.Infrastructure.Configuration;

public sealed class JsonStoreContentCatalog
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    private readonly string _formatsPath;
    private readonly string _brandsPath;

    public JsonStoreContentCatalog(string formatsPath, string brandsPath)
    {
        if (string.IsNullOrWhiteSpace(formatsPath) || string.IsNullOrWhiteSpace(brandsPath))
        {
            throw new ArgumentException("Store content paths are required.");
        }

        _formatsPath = Path.GetFullPath(formatsPath);
        _brandsPath = Path.GetFullPath(brandsPath);
    }

    public async Task<StoreContentCatalog> LoadAsync(CancellationToken cancellationToken = default)
    {
        var formatDocument = await ReadAsync<StoreFormatDocument>(_formatsPath, cancellationToken);
        var brandDocument = await ReadAsync<StoreBrandDocument>(_brandsPath, cancellationToken);
        if (formatDocument.SchemaVersion != 1 || brandDocument.SchemaVersion != 1)
        {
            throw new InvalidDataException("Unsupported store content schema version.");
        }

        var formats = RequireItems(formatDocument.Formats, "Store formats");
        var brands = RequireItems(brandDocument.Brands, "Store brands");
        EnsureUnique(formats.Select(item => item.Id), "store format");
        EnsureUnique(brands.Select(item => item.Id), "store brand");
        var formatIds = formats.Select(item => item.Id).ToHashSet(StringComparer.Ordinal);
        var unknownBrand = brands.FirstOrDefault(item => !formatIds.Contains(item.FormatId));
        if (unknownBrand is not null)
        {
            throw new InvalidDataException(
                $"Store brand '{unknownBrand.Id}' references unknown format '{unknownBrand.FormatId}'.");
        }

        return new StoreContentCatalog(
            Array.AsReadOnly(formats.ToArray()),
            Array.AsReadOnly(brands.ToArray()));
    }

    private static async Task<T> ReadAsync<T>(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<T>(stream, SerializerOptions, cancellationToken)
            ?? throw new InvalidDataException($"Store content '{path}' is empty.");
    }

    private static List<T> RequireItems<T>(List<T>? items, string label)
    {
        if (items is not { Count: > 0 } || items.Any(item => item is null))
        {
            throw new InvalidDataException($"{label} must contain at least one item.");
        }

        return items;
    }

    private static void EnsureUnique(IEnumerable<string> ids, string label)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var duplicate = ids.FirstOrDefault(id => !seen.Add(id));
        if (duplicate is not null)
        {
            throw new InvalidDataException($"Duplicate {label} ID: {duplicate}.");
        }
    }

    private sealed record StoreFormatDocument(int SchemaVersion, List<StoreFormatDefinition>? Formats);
    private sealed record StoreBrandDocument(int SchemaVersion, List<StoreBrandDefinition>? Brands);
}
