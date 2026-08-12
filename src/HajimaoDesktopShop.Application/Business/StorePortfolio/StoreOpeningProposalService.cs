using System.Collections.ObjectModel;
using HajimaoDesktopShop.Application.Catalog;

namespace HajimaoDesktopShop.Application.Business.StorePortfolio;

public sealed class StoreOpeningProposalService
{
    private const long StreetExpansionCostCentsPerOpenStore = 40_000;
    private static readonly IReadOnlyList<string> StarterFormatIds =
        Array.AsReadOnly(new[] { "convenience", "discount", "premium" });
    private readonly StoreContentCatalog _catalog;
    private readonly IReadOnlyDictionary<string, StoreFormatDefinition> _formatsById;

    public StoreOpeningProposalService(StoreContentCatalog catalog)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        if (catalog.Formats.Count == 0 || catalog.Brands.Count == 0)
        {
            throw new ArgumentException("Store content cannot be empty.", nameof(catalog));
        }

        _formatsById = new ReadOnlyDictionary<string, StoreFormatDefinition>(
            catalog.Formats.ToDictionary(item => item.Id, StringComparer.Ordinal));
    }

    public IReadOnlyList<StoreOpeningProposal> CreateStarterProposals(
        int seed,
        long openingCashCents)
    {
        if (openingCashCents < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(openingCashCents));
        }

        var missingFormat = StarterFormatIds.FirstOrDefault(id => !_formatsById.ContainsKey(id));
        if (missingFormat is not null)
        {
            throw new InvalidOperationException($"Starter store format '{missingFormat}' is missing.");
        }

        var proposals = StarterFormatIds
            .Select(formatId => SelectBrand(formatId, seed))
            .Select(brand => CreateProposal(brand, openStoreCount: 0, openingCashCents))
            .ToArray();
        return Array.AsReadOnly(proposals);
    }

    public IReadOnlyList<StoreOpeningProposal> CreateExpansionProposals(
        int openStoreCount,
        long sharedCashCents,
        int seed,
        IReadOnlyCollection<string>? openedBrandIds = null)
    {
        if (openStoreCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(openStoreCount));
        }

        if (sharedCashCents < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sharedCashCents));
        }

        var opened = openedBrandIds?.ToHashSet(StringComparer.Ordinal)
            ?? new HashSet<string>(StringComparer.Ordinal);
        var ranked = _catalog.Brands
            .OrderBy(brand => opened.Contains(brand.Id) ? 0 : 1)
            .ThenBy(brand => StableRank(seed, brand.Id))
            .ThenBy(brand => brand.Id, StringComparer.Ordinal)
            .ToArray();
        var chosen = new List<StoreBrandDefinition>(capacity: 3);
        foreach (var brand in ranked)
        {
            if (chosen.Count == 3)
            {
                break;
            }

            if (chosen.Count == 1
                && chosen[0].FormatId == brand.FormatId
                && ranked.Any(item => item.FormatId != chosen[0].FormatId && !chosen.Contains(item)))
            {
                continue;
            }

            chosen.Add(brand);
        }

        if (chosen.Select(item => item.FormatId).Distinct(StringComparer.Ordinal).Count() < 2)
        {
            var alternate = ranked.First(item => item.FormatId != chosen[0].FormatId);
            chosen[^1] = alternate;
        }

        return Array.AsReadOnly(chosen
            .Select(brand => CreateProposal(brand, openStoreCount, sharedCashCents))
            .ToArray());
    }

    private StoreBrandDefinition SelectBrand(string formatId, int seed) =>
        _catalog.Brands
            .Where(item => item.FormatId == formatId)
            .OrderBy(item => StableRank(seed, item.Id))
            .ThenBy(item => item.Id, StringComparer.Ordinal)
            .First();

    private StoreOpeningProposal CreateProposal(
        StoreBrandDefinition brand,
        int openStoreCount,
        long sharedCashCents)
    {
        var format = _formatsById[brand.FormatId];
        var openingCost = openStoreCount == 0
            ? 0
            : checked(format.BaseOpeningCostCents
                + StreetExpansionCostCentsPerOpenStore * openStoreCount);
        var cashAfterOpening = checked(sharedCashCents - openingCost);
        return new StoreOpeningProposal(
            $"store-{openStoreCount + 1:D4}",
            openStoreCount + 1,
            brand.Id,
            brand.DisplayName,
            format.Id,
            format.DisplayName,
            openingCost,
            format.RecommendedReserveCents,
            cashAfterOpening,
            cashAfterOpening >= format.RecommendedReserveCents);
    }

    private static ulong StableRank(int seed, string id)
    {
        var hash = 14_695_981_039_346_656_037UL ^ unchecked((uint)seed);
        foreach (var character in id)
        {
            hash ^= character;
            hash *= 1_099_511_628_211UL;
        }

        return hash;
    }
}
