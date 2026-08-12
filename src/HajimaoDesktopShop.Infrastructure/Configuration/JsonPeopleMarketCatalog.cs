using System.Text.Json;
using System.Text.Json.Serialization;
using HajimaoDesktopShop.Application.Business.Events;
using HajimaoDesktopShop.Application.Catalog;

namespace HajimaoDesktopShop.Infrastructure.Configuration;

public sealed class JsonPeopleMarketCatalog
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _employeesPath;
    private readonly string _eventsPath;

    public JsonPeopleMarketCatalog(string employeesPath, string eventsPath)
    {
        _employeesPath = NormalizePath(employeesPath, nameof(employeesPath));
        _eventsPath = NormalizePath(eventsPath, nameof(eventsPath));
    }

    public async Task<PeopleMarketContent> LoadAsync(CancellationToken cancellationToken = default)
    {
        var employees = await LoadAsync<EmployeeCatalogDocument>(_employeesPath, cancellationToken);
        var events = await LoadAsync<MarketEventCatalogDocument>(_eventsPath, cancellationToken);
        ValidateSchema(employees.SchemaVersion, "employee");
        ValidateSchema(events.SchemaVersion, "market event");
        ValidateCount(employees.Profiles, 96, "employee profile");
        ValidateCount(events.Events, 96, "market event");
        ValidateUniqueIds(employees.Profiles!, profile => profile.Id, "employee profile");
        ValidateUniqueIds(events.Events!, marketEvent => marketEvent.Id, "market event");
        ValidateEventReferences(events.Events!);

        return new PeopleMarketContent(
            Array.AsReadOnly(employees.Profiles!.ToArray()),
            Array.AsReadOnly(events.Events!.ToArray()));
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

    private static void ValidateSchema(int schemaVersion, string kind)
    {
        if (schemaVersion != 1)
        {
            throw new InvalidDataException($"Unsupported {kind} catalog schema version: {schemaVersion}.");
        }
    }

    private static void ValidateCount<T>(IReadOnlyCollection<T>? items, int minimum, string kind)
    {
        if (items is null || items.Count < minimum)
        {
            throw new InvalidDataException($"The {kind} catalog requires at least {minimum} records.");
        }
    }

    private static void ValidateUniqueIds<T>(IEnumerable<T> items, Func<T, string> idSelector, string kind)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in items)
        {
            var id = idSelector(item);
            if (!ids.Add(id))
            {
                throw new InvalidDataException($"Duplicate {kind} ID: {id}.");
            }
        }
    }

    private static void ValidateEventReferences(IEnumerable<MarketEventDefinition> events)
    {
        foreach (var marketEvent in events)
        {
            foreach (var effect in marketEvent.Effects.Concat(marketEvent.Choices.SelectMany(choice => choice.Effects)))
            {
                if (effect.ModifierPermille is 0 or < -500 or > 500)
                {
                    throw new InvalidDataException($"Market event '{marketEvent.Id}' has an invalid modifier.");
                }

                if (effect.Kind == MarketEventEffectKind.CategoryWeight && string.IsNullOrWhiteSpace(effect.TargetTag))
                {
                    throw new InvalidDataException($"Market event '{marketEvent.Id}' requires a category target.");
                }
            }

            if (marketEvent.Choices.Select(choice => choice.Id).Distinct(StringComparer.Ordinal).Count() != marketEvent.Choices.Count)
            {
                throw new InvalidDataException($"Market event '{marketEvent.Id}' has duplicate choice IDs.");
            }
        }
    }

    private static string NormalizePath(string path, string parameterName) =>
        string.IsNullOrWhiteSpace(path)
            ? throw new ArgumentException("Catalog path is required.", parameterName)
            : Path.GetFullPath(path);

    private sealed record EmployeeCatalogDocument(int SchemaVersion, List<EmployeeProfileDefinition>? Profiles);
    private sealed record MarketEventCatalogDocument(int SchemaVersion, List<MarketEventDefinition>? Events);
}
