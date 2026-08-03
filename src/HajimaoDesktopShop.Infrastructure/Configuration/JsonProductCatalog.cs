using System.Text.Json;
using HajimaoDesktopShop.Application.Catalog;

namespace HajimaoDesktopShop.Infrastructure.Configuration;

public sealed class JsonProductCatalog : IProductCatalog
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly string _path;

    public JsonProductCatalog(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Catalog path is required.", nameof(path));
        }

        _path = Path.GetFullPath(path);
    }

    public async Task<IReadOnlyList<ProductDefinition>> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(_path);
        var document = await JsonSerializer.DeserializeAsync<ProductCatalogDocument>(
            stream,
            SerializerOptions,
            cancellationToken);

        if (document is null)
        {
            throw new InvalidDataException("Product catalog is empty.");
        }

        if (document.SchemaVersion != 1)
        {
            throw new InvalidDataException(
                $"Unsupported product catalog schema version: {document.SchemaVersion}.");
        }

        if (document.Products is not { Count: > 0 })
        {
            throw new InvalidDataException("Product catalog must contain at least one product.");
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var product in document.Products)
        {
            if (!ids.Add(product.Id))
            {
                throw new InvalidDataException($"Duplicate product ID: {product.Id}.");
            }
        }

        return Array.AsReadOnly(document.Products.ToArray());
    }

    private sealed record ProductCatalogDocument(
        int SchemaVersion,
        List<ProductDefinition>? Products);
}
