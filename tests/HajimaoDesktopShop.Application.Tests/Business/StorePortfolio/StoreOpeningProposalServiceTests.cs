using HajimaoDesktopShop.Application.Business.StorePortfolio;
using HajimaoDesktopShop.Application.Business.Strategy;
using HajimaoDesktopShop.Application.Catalog;

namespace HajimaoDesktopShop.Application.Tests.Business.StorePortfolio;

public sealed class StoreOpeningProposalServiceTests
{
    [Fact]
    public void CreateStarterProposals_ReturnsOneBrandPerFormatWithNoOpeningCharge()
    {
        var service = new StoreOpeningProposalService(CreateCatalog());

        var proposals = service.CreateStarterProposals(seed: 711);

        Assert.Equal(4, proposals.Count);
        Assert.Equal(4, proposals.Select(item => item.FormatId).Distinct(StringComparer.Ordinal).Count());
        Assert.All(proposals, item =>
        {
            Assert.Equal("store-0001", item.ProspectiveStoreId);
            Assert.Equal(1, item.StreetOrdinal);
            Assert.Equal(0, item.OpeningCostCents);
        });
    }

    [Fact]
    public void CreateExpansionProposals_ReturnsThreeBrandsAcrossFormatsWithoutLevelInput()
    {
        var service = new StoreOpeningProposalService(CreateCatalog());

        var proposals = service.CreateExpansionProposals(
            openStoreCount: 1,
            sharedCashCents: 200_000,
            seed: 711);

        Assert.Equal(3, proposals.Count);
        Assert.True(proposals.Select(item => item.FormatId).Distinct(StringComparer.Ordinal).Count() >= 2);
        Assert.All(proposals, item =>
        {
            Assert.Equal("store-0002", item.ProspectiveStoreId);
            Assert.Equal(2, item.StreetOrdinal);
            Assert.True(item.OpeningCostCents >= 80_000);
            Assert.Equal(200_000 - item.OpeningCostCents, item.CashAfterOpeningCents);
        });
    }

    [Fact]
    public void CreateExpansionProposals_IsDeterministicAndAllowsExistingBrandAgain()
    {
        var service = new StoreOpeningProposalService(CreateCatalog());

        var first = service.CreateExpansionProposals(3, 500_000, 42, ["brand-a"]);
        var repeated = service.CreateExpansionProposals(3, 500_000, 42, ["brand-a"]);

        Assert.Equal(first, repeated);
        Assert.Contains(first, item => item.BrandId == "brand-a");
    }

    private static StoreContentCatalog CreateCatalog()
    {
        var formats = new[]
        {
            Format("convenience", 40_000),
            Format("discount", 70_000),
            Format("premium", 90_000),
            Format("commuter", 60_000)
        };
        var brands = new[]
        {
            Brand("brand-a", "7-Eleven", "convenience"),
            Brand("brand-b", "FamilyMart", "convenience"),
            Brand("brand-c", "ALDI", "discount"),
            Brand("brand-d", "Lidl", "discount"),
            Brand("brand-e", "银座三越", "premium"),
            Brand("brand-f", "Harrods", "premium"),
            Brand("brand-g", "Circle K", "commuter"),
            Brand("brand-h", "Watsons", "commuter")
        };
        return new StoreContentCatalog(formats, brands);
    }

    private static StoreFormatDefinition Format(string id, long openingCost) =>
        new(
            id,
            id,
            openingCost,
            recommendedReserveCents: 50_000,
            baseDemandPermille: 1_000,
            priceSensitivityPermille: 1_000,
            serviceSensitivityPermille: 1_000,
            queueSensitivityPermille: 1_000,
            cleanlinessSensitivityPermille: 1_000,
            inventoryCapacityPermille: 1_000,
            timeProfile: "steady",
            new Dictionary<string, int>
            {
                ["ambient"] = 1_000,
                ["chilled"] = 1_000,
                ["frozen"] = 1_000
            },
            StorePricingPreset.Balanced,
            StoreStockingPreset.Balanced);

    private static StoreBrandDefinition Brand(string id, string name, string formatId) =>
        new(id, name, "global", formatId, "facade", "real-world-name", "review-required");
}
