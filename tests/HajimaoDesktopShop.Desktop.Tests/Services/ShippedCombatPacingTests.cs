using System.IO;
using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Business.Combat;
using HajimaoDesktopShop.Application.Business.StorePortfolio;
using HajimaoDesktopShop.Application.Catalog;
using HajimaoDesktopShop.Desktop.Services;
using HajimaoDesktopShop.Infrastructure.Configuration;
using Xunit.Abstractions;

namespace HajimaoDesktopShop.Desktop.Tests.Services;

public sealed class ShippedCombatPacingTests(ITestOutputHelper output)
{
    [Fact]
    public async Task StarterFormats_ReachFirstServiceAndDropDuringTheOpeningMinutes()
    {
        var content = await LoadContentAsync();
        var proposals = new StoreOpeningProposalService(content.Stores)
            .CreateStarterProposals(42, DesktopGameContent.OpeningCashCents);

        foreach (var proposal in proposals)
        {
            var session = DesktopBusinessSessionFactory.Create(
                content.Products,
                save: null,
                seed: 42,
                nowUtc: new DateTimeOffset(2026, 8, 18, 12, 0, 0, TimeSpan.Zero),
                content.Stores,
                starterStoreProposal: proposal,
                combatContent: content.Combat).Session;
            int? firstServiceSecond = null;
            int? firstDropSecond = null;

            for (var second = 1; second <= 300; second++)
            {
                var snapshot = session.AdvanceCombatRealSecond(12, []);
                var store = Assert.Single(snapshot.Stores);
                firstServiceSecond ??= store.ServedCustomers > 0 ? second : null;
                firstDropSecond ??= store.DroppedProducts > 0 ? second : null;
            }

            output.WriteLine(
                $"{proposal.FormatId}: first service={firstServiceSecond}s, first drop={firstDropSecond}s");
            Assert.InRange(Assert.IsType<int>(firstServiceSecond), 1, 120);
            Assert.InRange(Assert.IsType<int>(firstDropSecond), 1, 300);
        }
    }

    [Fact]
    public async Task FirstSuccessfulService_AwardsANewProductInEveryStarterFormat()
    {
        var content = await LoadContentAsync();
        var proposals = new StoreOpeningProposalService(content.Stores)
            .CreateStarterProposals(42, DesktopGameContent.OpeningCashCents);

        foreach (var proposal in proposals)
        {
            var session = DesktopBusinessSessionFactory.Create(
                content.Products,
                save: null,
                seed: 42,
                nowUtc: new DateTimeOffset(2026, 8, 18, 12, 0, 0, TimeSpan.Zero),
                content.Stores,
                starterStoreProposal: proposal,
                combatContent: content.Combat).Session;
            var startingProductIds = session.Combat!.GetSnapshot().Collection
                .Select(entry => entry.ProductId)
                .ToHashSet(StringComparer.Ordinal);
            BusinessCombatSnapshot? firstService = null;

            for (var second = 1; second <= 120 && firstService is null; second++)
            {
                var snapshot = session.AdvanceCombatRealSecond(12, []);
                firstService = snapshot.Stores.Single().ServedCustomers > 0 ? snapshot : null;
            }

            var served = Assert.IsType<BusinessCombatSnapshot>(firstService);
            Assert.Contains(
                served.Collection,
                entry => !startingProductIds.Contains(entry.ProductId));
            Assert.True(served.Stores.Single().DroppedProducts > 0);
            Assert.Contains(served.Stores.Single().DropRolls, roll => roll.IsNewUnlock);
        }
    }

    [Fact]
    public async Task SecondStoreBecomesAffordableAfterTwoToTwelveHoursOfActiveHanging()
    {
        var content = await LoadContentAsync();
        var starter = new StoreOpeningProposalService(content.Stores)
            .CreateStarterProposals(42, DesktopGameContent.OpeningCashCents)
            .First();
        var session = DesktopBusinessSessionFactory.Create(
            content.Products,
            save: null,
            seed: 42,
            nowUtc: new DateTimeOffset(2026, 8, 18, 12, 0, 0, TimeSpan.Zero),
            content.Stores,
            starterStoreProposal: starter,
            combatContent: content.Combat).Session;

        var minimumOpeningCost = session.CombatExpansion!.GetProposals()
            .Min(proposal => proposal.OpeningCostCents);
        int? firstAffordableSecond = session.Game.GetSnapshot().CashCents >= minimumOpeningCost
            ? 0
            : null;
        for (var second = 1; second <= 43_200 && firstAffordableSecond is null; second++)
        {
            var snapshot = session.AdvanceCombatRealSecond(12, []);
            firstAffordableSecond = snapshot.CashCents >= minimumOpeningCost ? second : null;
        }

        output.WriteLine($"second store first affordable at {firstAffordableSecond}s");
        Assert.InRange(Assert.IsType<int>(firstAffordableSecond), 7_200, 43_200);
    }

    private static async Task<ShippedContent> LoadContentAsync()
    {
        var root = LocateRepositoryRoot();
        var assets = Path.Combine(root, "src", "HajimaoDesktopShop.Desktop", "Assets");
        var productsPath = Path.Combine(assets, "Config", "products.json");
        var brandsPath = Path.Combine(assets, "Config", "store-brands.json");
        var stores = await new JsonStoreContentCatalog(
            Path.Combine(assets, "Config", "store-formats.json"),
            brandsPath).LoadAsync();
        var products = await new JsonProductCatalog(productsPath).LoadAsync();
        var combat = await new JsonCombatContentCatalog(
            productsPath,
            brandsPath,
            Path.Combine(assets, "Config", "product-combat.json"),
            Path.Combine(assets, "Content", "customers", "customer-archetypes.json"),
            Path.Combine(assets, "Content", "customers", "customer-spawn-pools.json"),
            Path.Combine(assets, "Content", "characters", "characters.json"),
            Path.Combine(assets, "Content", "interiors", "interiors.json")).LoadAsync();
        return new ShippedContent(products, stores, combat);
    }

    private static string LocateRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null
            && !File.Exists(Path.Combine(directory.FullName, "HajimaoDesktopShop.slnx")))
        {
            directory = directory.Parent;
        }

        return Assert.IsType<DirectoryInfo>(directory).FullName;
    }

    private sealed record ShippedContent(
        IReadOnlyList<ProductDefinition> Products,
        StoreContentCatalog Stores,
        CombatContentCatalog Combat);
}
