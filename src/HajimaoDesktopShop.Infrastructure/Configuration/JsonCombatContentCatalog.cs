using System.Text.Json;
using System.Text.Json.Serialization;
using HajimaoDesktopShop.Application.Catalog;

namespace HajimaoDesktopShop.Infrastructure.Configuration;

public sealed class JsonCombatContentCatalog
{
    private const int SchemaVersion = 1;
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly string[] _paths;

    public JsonCombatContentCatalog(
        string productsPath,
        string storeBrandsPath,
        string productCombatPath,
        string customerArchetypesPath,
        string customerSpawnPoolsPath,
        string charactersPath,
        string interiorsPath)
    {
        _paths =
        [
            NormalizePath(productsPath, nameof(productsPath)),
            NormalizePath(storeBrandsPath, nameof(storeBrandsPath)),
            NormalizePath(productCombatPath, nameof(productCombatPath)),
            NormalizePath(customerArchetypesPath, nameof(customerArchetypesPath)),
            NormalizePath(customerSpawnPoolsPath, nameof(customerSpawnPoolsPath)),
            NormalizePath(charactersPath, nameof(charactersPath)),
            NormalizePath(interiorsPath, nameof(interiorsPath))
        ];
    }

    public async Task<CombatContentCatalog> LoadAsync(CancellationToken cancellationToken = default)
    {
        var productIndex = await LoadAsync<ProductIndexDocument>(_paths[0], cancellationToken);
        var storeIndex = await LoadAsync<StoreIndexDocument>(_paths[1], cancellationToken);
        var productsDocument = await LoadAsync<ProductCombatDocument>(_paths[2], cancellationToken);
        var customersDocument = await LoadAsync<CustomerDocument>(_paths[3], cancellationToken);
        var poolsDocument = await LoadAsync<SpawnPoolDocument>(_paths[4], cancellationToken);
        var charactersDocument = await LoadAsync<CharacterDocument>(_paths[5], cancellationToken);
        var interiorsDocument = await LoadAsync<InteriorDocument>(_paths[6], cancellationToken);

        ValidateSchemas(productIndex, storeIndex, productsDocument, customersDocument, poolsDocument, charactersDocument, interiorsDocument);
        var products = Required(productsDocument.Products, "product combat");
        var customers = Required(customersDocument.Customers, "customer archetype");
        var pools = Required(poolsDocument.Pools, "customer spawn pool");
        var modifiers = poolsDocument.EventModifiers?.ToArray() ?? [];
        var characters = Required(charactersDocument.Characters, "character");
        var interiors = Required(interiorsDocument.Interiors, "store interior");
        var knownProductIds = Required(productIndex.Products, "base product").Select(item => item.Id).ToHashSet(StringComparer.Ordinal);
        var knownStoreIds = Required(storeIndex.Brands, "store brand").Select(item => item.Id).ToHashSet(StringComparer.Ordinal);

        ValidateProducts(products, knownProductIds);
        ValidateCustomers(customers, products);
        ValidatePools(pools, modifiers, customers);
        ValidateCharacters(characters);
        ValidateInteriors(interiors, knownStoreIds);

        return new CombatContentCatalog(
            Array.AsReadOnly(products),
            Array.AsReadOnly(customers),
            Array.AsReadOnly(pools),
            Array.AsReadOnly(modifiers),
            Array.AsReadOnly(characters),
            Array.AsReadOnly(interiors));
    }

    private static void ValidateProducts(ProductCombatDefinition[] products, HashSet<string> knownProductIds)
    {
        if (products.Length < 24)
        {
            throw new InvalidDataException("Combat content requires at least 24 products.");
        }

        ValidateUnique(products, product => product.ProductId, "combat product");
        var mechanicalRows = new HashSet<string>(StringComparer.Ordinal);
        foreach (var product in products)
        {
            if (!knownProductIds.Contains(product.ProductId)
                || product.BasePower <= 0
                || product.AttackIntervalTicks <= 0
                || product.RevenueModifierPermille <= 0
                || product.EffectStrengthPermille is < 0 or > 5_000
                || product.DropWeight <= 0
                || product.Tags is not { Length: > 0 }
                || product.Tags.Any(string.IsNullOrWhiteSpace))
            {
                throw new InvalidDataException($"Combat product '{product.ProductId}' is invalid or unknown.");
            }

            var signature = string.Join(':',
                product.BasePower,
                product.AttackIntervalTicks,
                product.RevenueModifierPermille,
                product.Effect,
                product.EffectStrengthPermille,
                string.Join(',', product.Tags.Order(StringComparer.Ordinal)),
                product.DropWeight);
            if (!mechanicalRows.Add(signature))
            {
                throw new InvalidDataException($"Combat product '{product.ProductId}' duplicates another mechanical row.");
            }
        }
    }

    private static void ValidateCustomers(
        CustomerArchetypeDefinition[] customers,
        IReadOnlyList<ProductCombatDefinition> products)
    {
        if (customers.Length < 12)
        {
            throw new InvalidDataException("Combat content requires at least 12 customer archetypes.");
        }

        ValidateUnique(customers, customer => customer.Id, "customer archetype");
        var productIds = products.Select(product => product.ProductId).ToHashSet(StringComparer.Ordinal);
        foreach (var customer in customers)
        {
            if (customer.DemandHp <= 0
                || customer.MovementPermillePerTick is <= 0 or > 10_000
                || customer.BaseRewardCents <= 0
                || customer.Tags is not { Length: > 0 }
                || customer.Tags.Any(string.IsNullOrWhiteSpace)
                || customer.ResistancePermille is null
                || customer.ProductDropWeights is not { Count: > 0 }
                || customer.ResistancePermille.Any(pair => string.IsNullOrWhiteSpace(pair.Key) || pair.Value is < 0 or > 900)
                || customer.ProductDropWeights.Any(pair => !productIds.Contains(pair.Key) || pair.Value <= 0))
            {
                throw new InvalidDataException($"Customer archetype '{customer.Id}' is invalid.");
            }
        }
    }

    private static void ValidatePools(
        CustomerSpawnPoolDefinition[] pools,
        CustomerSpawnEventModifierDefinition[] modifiers,
        IReadOnlyList<CustomerArchetypeDefinition> customers)
    {
        if (pools.Length != 4)
        {
            throw new InvalidDataException("Exactly four real-time customer pools are required.");
        }

        ValidateUnique(pools, pool => pool.Id, "customer spawn pool");
        var customerIds = customers.Select(customer => customer.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var pool in pools)
        {
            if (pool.StartHourInclusive is < 0 or > 23
                || pool.EndHourExclusive is < 0 or > 23
                || pool.StartHourInclusive == pool.EndHourExclusive
                || pool.Entries is not { Count: > 0 }
                || pool.Entries.Any(entry => !customerIds.Contains(entry.CustomerId) || entry.Weight <= 0)
                || pool.Entries.Select(entry => entry.CustomerId).Distinct(StringComparer.Ordinal).Count() != pool.Entries.Count)
            {
                throw new InvalidDataException($"Customer spawn pool '{pool.Id}' is invalid.");
            }
        }

        for (var hour = 0; hour < 24; hour++)
        {
            if (pools.Count(pool => ContainsHour(pool, hour)) != 1)
            {
                throw new InvalidDataException("Customer spawn pools must cover each real local hour exactly once.");
            }
        }

        var customerTags = customers.SelectMany(customer => customer.Tags).ToHashSet(StringComparer.Ordinal);
        foreach (var modifier in modifiers)
        {
            if (string.IsNullOrWhiteSpace(modifier.EventTag)
                || !customerTags.Contains(modifier.CustomerTag)
                || modifier.WeightModifierPermille is < 0 or > 5_000
                || modifier.AddedWeight < 0
                || modifier is { WeightModifierPermille: 1_000, AddedWeight: 0 })
            {
                throw new InvalidDataException($"Customer event modifier '{modifier.EventTag}' is invalid.");
            }
        }
    }

    private static void ValidateCharacters(CharacterDefinition[] characters)
    {
        if (characters.Length != 1
            || characters[0].Id != "maomao-default"
            || characters[0].RigId != "humanoid-v1"
            || characters[0].SkinId != "maomao-default"
            || characters[0].BaseAttackIntervalTicks <= 0
            || characters[0].ProjectileTravelTicks is < 2 or > 12)
        {
            throw new InvalidDataException("Version 0.2 requires only the neutral maomao-default character.");
        }
    }

    private static void ValidateInteriors(StoreInteriorDefinition[] interiors, HashSet<string> knownStoreIds)
    {
        ValidateUnique(interiors, interior => interior.StoreId, "store interior");
        if (interiors.Length != knownStoreIds.Count
            || !interiors.Select(interior => interior.StoreId).ToHashSet(StringComparer.Ordinal).SetEquals(knownStoreIds)
            || interiors.Any(interior => string.IsNullOrWhiteSpace(interior.BackgroundAssetPath)
                || !interior.BackgroundAssetPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidDataException("Every configured store requires one PNG interior background assignment.");
        }
    }

    private static bool ContainsHour(CustomerSpawnPoolDefinition pool, int hour) =>
        pool.StartHourInclusive < pool.EndHourExclusive
            ? hour >= pool.StartHourInclusive && hour < pool.EndHourExclusive
            : hour >= pool.StartHourInclusive || hour < pool.EndHourExclusive;

    private static void ValidateSchemas(params ISchemaDocument[] documents)
    {
        if (documents.Any(document => document.SchemaVersion != SchemaVersion))
        {
            throw new InvalidDataException("Unsupported combat content schema version.");
        }
    }

    private static T[] Required<T>(IReadOnlyList<T>? items, string kind) =>
        items is { Count: > 0 }
            ? items.ToArray()
            : throw new InvalidDataException($"The {kind} catalog is empty.");

    private static void ValidateUnique<T>(IEnumerable<T> items, Func<T, string> idSelector, string kind)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in items)
        {
            var id = idSelector(item);
            if (string.IsNullOrWhiteSpace(id) || !ids.Add(id))
            {
                throw new InvalidDataException($"Duplicate or empty {kind} ID: '{id}'.");
            }
        }
    }

    private static async Task<T> LoadAsync<T>(string path, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<T>(stream, SerializerOptions, cancellationToken)
                ?? throw new InvalidDataException($"Content file '{Path.GetFileName(path)}' is empty.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"Content file '{Path.GetFileName(path)}' is invalid: {exception.Message}", exception);
        }
    }

    private static string NormalizePath(string path, string parameterName) =>
        string.IsNullOrWhiteSpace(path)
            ? throw new ArgumentException("Catalog path is required.", parameterName)
            : Path.GetFullPath(path);

    private interface ISchemaDocument { int SchemaVersion { get; } }
    private sealed record ProductIndexDocument(int SchemaVersion, IReadOnlyList<IdItem>? Products) : ISchemaDocument;
    private sealed record StoreIndexDocument(int SchemaVersion, IReadOnlyList<IdItem>? Brands) : ISchemaDocument;
    private sealed record ProductCombatDocument(int SchemaVersion, IReadOnlyList<ProductCombatDefinition>? Products) : ISchemaDocument;
    private sealed record CustomerDocument(int SchemaVersion, IReadOnlyList<CustomerArchetypeDefinition>? Customers) : ISchemaDocument;
    private sealed record SpawnPoolDocument(int SchemaVersion, IReadOnlyList<CustomerSpawnPoolDefinition>? Pools, IReadOnlyList<CustomerSpawnEventModifierDefinition>? EventModifiers) : ISchemaDocument;
    private sealed record CharacterDocument(int SchemaVersion, IReadOnlyList<CharacterDefinition>? Characters) : ISchemaDocument;
    private sealed record InteriorDocument(int SchemaVersion, IReadOnlyList<StoreInteriorDefinition>? Interiors) : ISchemaDocument;
    private sealed record IdItem(string Id);
}
