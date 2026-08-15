using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Business.Combat;
using HajimaoDesktopShop.Application.Business.Strategy;
using HajimaoDesktopShop.Application.Catalog;
using HajimaoDesktopShop.Domain.Economy;
using HajimaoDesktopShop.Domain.Players;
using HajimaoDesktopShop.Domain.Shops;

namespace HajimaoDesktopShop.Application.Tests.Business.Combat;

public sealed class CombatStoreExpansionServiceTests
{
    [Fact]
    public void Open_UsesSharedCashAndOpensChosenCombatStoreWithoutSimulationReports()
    {
        var game = CreateGame(300_000);
        var service = new CombatStoreExpansionService(game, CreateCatalog());
        var proposal = service.GetProposals().Single(item => item.FormatId == "premium");

        var result = service.Open(proposal.BrandId);

        Assert.Equal(CombatStoreExpansionStatus.Success, result.Status);
        var snapshot = game.GetSnapshot();
        var opened = Assert.Single(snapshot.Stores, store => store.Id == "store-0002");
        Assert.Equal("premium", opened.StoreFormatId);
        Assert.Equal(proposal.BrandName, opened.Name);
        Assert.Equal(300_000 - proposal.OpeningCostCents, snapshot.CashCents);
    }

    [Fact]
    public void GetProposals_KeepsUnaffordableChoicesVisible()
    {
        var service = new CombatStoreExpansionService(CreateGame(1_000), CreateCatalog());

        var proposals = service.GetProposals();

        Assert.Equal(3, proposals.Count);
        Assert.All(proposals, proposal => Assert.True(proposal.CashAfterOpeningCents < 0));
    }

    private static BusinessGameService CreateGame(long cashCents)
    {
        var starter = new ShopDefinition(
            new ShopId("corner-store"),
            new StoreBrandId("brand-a"),
            new StoreFormatId("convenience"),
            "7-Eleven",
            1,
            Money.Zero);
        return new BusinessGameService(
            [new ProductDefinition("water", "矿泉水", 100, 200, 1, "ambient", 1)],
            [starter],
            new LevelCurve([0, 10]),
            starter.Id.Value,
            cashCents,
            storeContent: CreateCatalog());
    }

    private static StoreContentCatalog CreateCatalog() => new(
        [
            Format("convenience", 40_000),
            Format("discount", 70_000),
            Format("premium", 90_000)
        ],
        [
            Brand("brand-a", "7-Eleven", "convenience"),
            Brand("brand-b", "FamilyMart", "convenience"),
            Brand("brand-c", "ALDI", "discount"),
            Brand("brand-d", "Lidl", "discount"),
            Brand("brand-e", "银座三越", "premium"),
            Brand("brand-f", "Harrods", "premium")
        ]);

    private static StoreFormatDefinition Format(string id, long openingCost) => new(
        id, id, openingCost, 50_000, 1_000, 1_000, 1_000, 1_000, 1_000, 1_000,
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
