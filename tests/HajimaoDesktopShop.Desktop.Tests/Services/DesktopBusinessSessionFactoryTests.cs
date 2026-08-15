using System.IO;
using HajimaoDesktopShop.Application.Catalog;
using HajimaoDesktopShop.Application.Business.StorePortfolio;
using HajimaoDesktopShop.Desktop.Services;
using HajimaoDesktopShop.Infrastructure.Configuration;
using HajimaoDesktopShop.Infrastructure.Persistence;

namespace HajimaoDesktopShop.Desktop.Tests.Services;

public sealed class DesktopBusinessSessionFactoryTests
{
    [Fact]
    public async Task ShippedCombatSession_CanWriteInitialSqliteSaveBeforeTimerStarts()
    {
        var root = LocateRepositoryRoot();
        var assets = Path.Combine(root, "src", "HajimaoDesktopShop.Desktop", "Assets");
        var productsPath = Path.Combine(assets, "Config", "products.json");
        var brandsPath = Path.Combine(assets, "Config", "store-brands.json");
        var storeContent = await new JsonStoreContentCatalog(
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
        var proposal = new StoreOpeningProposalService(storeContent)
            .CreateStarterProposals(42, DesktopGameContent.OpeningCashCents)
            .First();
        var session = DesktopBusinessSessionFactory.Create(
            products,
            null,
            42,
            DateTimeOffset.UtcNow,
            storeContent,
            starterStoreProposal: proposal,
            combatContent: combat).Session;
        var tempRoot = Path.Combine(Path.GetTempPath(), $"hajimao-initial-save-{Guid.NewGuid():N}");
        try
        {
            var savePath = Path.Combine(tempRoot, "hajimao.db");
            var store = new SqliteGameSaveStore(savePath);
            var coordinator = new AutosaveCoordinator(
                store,
                () => session.CaptureSaveData(),
                () => new HajimaoDesktopShop.Application.Persistence.DesktopWindowPlacement(10, 20, false));

            await coordinator.FlushAsync();

            Assert.True(File.Exists(savePath));
            Assert.NotNull(await store.LoadGameAsync());
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void CreateNew_StartsOneStoreWithoutLegacyEmployeeRoles()
    {
        var products = CreateProducts(10);

        var start = DesktopBusinessSessionFactory.Create(
            products,
            save: null,
            seed: 42,
            nowUtc: new DateTimeOffset(2026, 8, 4, 8, 0, 0, TimeSpan.Zero));
        var snapshot = start.Session.Simulation.GetSnapshot();

        Assert.True(start.IsNewGame);
        Assert.Equal(1, snapshot.Business.PlayerLevel);
        var store = Assert.Single(snapshot.Business.Stores);
        Assert.Equal("corner-store", store.Id);
        Assert.Equal(2, store.Products.Count);
        Assert.Empty(snapshot.Employees.Employees);
        Assert.Equal(DesktopGameContent.OpeningCashCents, snapshot.Business.CashCents);
    }

    [Fact]
    public void CreateNew_WithStoreContent_AppliesFamousBrandAndFormatEconomics()
    {
        var content = CreateStoreContent();

        var start = DesktopBusinessSessionFactory.Create(
            CreateProducts(10),
            save: null,
            seed: 42,
            nowUtc: new DateTimeOffset(2026, 8, 4, 8, 0, 0, TimeSpan.Zero),
            storeContent: content);
        var store = Assert.Single(start.Session.Game.GetSnapshot().Stores);

        Assert.Equal("seven-eleven", store.StoreBrandId);
        Assert.Equal("convenience", store.StoreFormatId);
        Assert.Equal(1_100, store.FormatEconomics!.DemandSensitivity.BaseDemandPermille);
        Assert.Equal(22, store.Products.First().Capacity);
    }

    [Fact]
    public void CreateNew_WithStarterSelection_UsesSelectedBrandFormatAndEconomics()
    {
        var content = CreateStoreContent();
        var selected = CreateDiscountProposal();

        var start = DesktopBusinessSessionFactory.Create(
            CreateProducts(10),
            save: null,
            seed: 42,
            nowUtc: new DateTimeOffset(2026, 8, 12, 8, 0, 0, TimeSpan.Zero),
            storeContent: content,
            starterStoreProposal: selected);
        var store = Assert.Single(start.Session.Game.GetSnapshot().Stores);

        Assert.Equal("corner-store", store.Id);
        Assert.Equal("ALDI", store.Name);
        Assert.Equal("aldi", store.StoreBrandId);
        Assert.Equal("discount", store.StoreFormatId);
        Assert.Equal(1, store.StreetOrdinal);
        Assert.Equal(1_220, store.FormatEconomics!.DemandSensitivity.BaseDemandPermille);
        Assert.Equal(DesktopGameContent.OpeningCashCents, start.Session.Game.GetSnapshot().CashCents);
    }

    [Fact]
    public void Restore_SelectedStarterIdentityBypassesNewSelectionAndSurvivesReload()
    {
        var content = CreateStoreContent();
        var savedAtUtc = new DateTimeOffset(2026, 8, 12, 8, 0, 0, TimeSpan.Zero);
        var original = DesktopBusinessSessionFactory.Create(
            CreateProducts(10),
            save: null,
            seed: 42,
            nowUtc: savedAtUtc,
            storeContent: content,
            starterStoreProposal: CreateDiscountProposal()).Session;
        var save = original.CaptureSaveData(savedAtUtc);
        var originalStore = Assert.Single(original.Game.GetSnapshot().Stores);

        var restored = DesktopBusinessSessionFactory.Create(
            CreateProducts(10),
            save,
            seed: 999,
            nowUtc: savedAtUtc.AddDays(30),
            storeContent: content).Session;
        var store = Assert.Single(restored.Game.GetSnapshot().Stores);

        Assert.Equal("ALDI", store.Name);
        Assert.Equal("aldi", store.StoreBrandId);
        Assert.Equal("discount", store.StoreFormatId);
        Assert.Equal(1_220, store.FormatEconomics!.DemandSensitivity.BaseDemandPermille);
        Assert.Equal(
            originalStore.Products.Select(product => product.Capacity),
            store.Products.Select(product => product.Capacity));
    }

    [Fact]
    public void CreateNew_RejectsStarterSelectionThatDoesNotMatchLoadedCatalog()
    {
        var invalid = CreateDiscountProposal() with
        {
            BrandId = "unknown-brand",
            BrandName = "Unknown"
        };

        Assert.Throws<ArgumentException>(() => DesktopBusinessSessionFactory.Create(
            CreateProducts(10),
            save: null,
            seed: 42,
            nowUtc: new DateTimeOffset(2026, 8, 12, 8, 0, 0, TimeSpan.Zero),
            storeContent: CreateStoreContent(),
            starterStoreProposal: invalid));
    }

    [Fact]
    public void Restore_UsesCompleteBusinessSaveAndDeterministicRandomState()
    {
        var products = CreateProducts(10);
        var savedAtUtc = new DateTimeOffset(2026, 8, 4, 8, 0, 0, TimeSpan.Zero);
        var original = DesktopBusinessSessionFactory.Create(
            products,
            save: null,
            seed: 42,
            nowUtc: savedAtUtc).Session;
        original.Simulation.AdvanceRealSeconds(17);
        var save = original.CaptureSaveData(savedAtUtc);

        var start = DesktopBusinessSessionFactory.Create(
            products,
            save,
            seed: 999,
            nowUtc: savedAtUtc);

        Assert.False(start.IsNewGame);
        Assert.Equivalent(
            original.Simulation.GetSnapshot(),
            start.Session.Simulation.GetSnapshot(),
            strict: true);
        Assert.Equivalent(save, start.Session.CaptureSaveData(save.SavedAtUtc), strict: true);
    }

    [Fact]
    public void Restore_DoesNotAdvanceWhileTheApplicationWasClosed()
    {
        var products = CreateProducts(10);
        var savedAtUtc = new DateTimeOffset(2026, 8, 4, 8, 0, 0, TimeSpan.Zero);
        var original = DesktopBusinessSessionFactory.Create(
            products,
            save: null,
            seed: 42,
            nowUtc: savedAtUtc).Session;
        var save = original.CaptureSaveData(savedAtUtc);

        var start = DesktopBusinessSessionFactory.Create(
            products,
            save,
            seed: 999,
            nowUtc: savedAtUtc.AddSeconds(10));

        Assert.Equivalent(
            original.Simulation.GetSnapshot(),
            start.Session.Simulation.GetSnapshot(),
            strict: true);
        Assert.Equivalent(save, start.Session.CaptureSaveData(save.SavedAtUtc), strict: true);
    }

    private static ProductDefinition[] CreateProducts(int count) =>
        Enumerable.Range(1, count)
            .Select(index => new ProductDefinition(
                $"product-{index}",
                $"商品 {index}",
                100 + index,
                200 + index,
                20,
                "ambient",
                requiredPlayerLevel: ((index - 1) / 2) + 1))
            .ToArray();

    private static StoreContentCatalog CreateStoreContent() =>
        new(
            [
                new StoreFormatDefinition(
                    "convenience",
                    "社区便利",
                    40_000,
                    40_000,
                    1_100,
                    1_000,
                    1_000,
                    1_000,
                    1_000,
                    1_100,
                    new Dictionary<string, int>
                    {
                        ["ambient"] = 1_000,
                        ["chilled"] = 1_000,
                        ["frozen"] = 1_000
                    },
                    Application.Business.Strategy.StorePricingPreset.Balanced,
                    Application.Business.Strategy.StoreStockingPreset.Balanced),
                new StoreFormatDefinition(
                    "discount",
                    "平价量贩",
                    70_000,
                    70_000,
                    1_220,
                    1_450,
                    800,
                    1_250,
                    800,
                    1_300,
                    new Dictionary<string, int>
                    {
                        ["ambient"] = 1_250,
                        ["chilled"] = 1_000,
                        ["frozen"] = 850
                    },
                    Application.Business.Strategy.StorePricingPreset.HighTurnover,
                    Application.Business.Strategy.StoreStockingPreset.FullShelves)
            ],
            [
                new StoreBrandDefinition(
                    "seven-eleven",
                    "7-Eleven",
                    "global",
                    "convenience",
                    "facade-convenience-a",
                    "real-world-name",
                    "review-required"),
                new StoreBrandDefinition(
                    "aldi",
                    "ALDI",
                    "europe",
                    "discount",
                    "facade-discount-a",
                    "real-world-name",
                    "review-required")
            ]);

    private static StoreOpeningProposal CreateDiscountProposal() =>
        new(
            "store-0001",
            1,
            "aldi",
            "ALDI",
            "discount",
            "平价量贩",
            0,
            70_000,
            DesktopGameContent.OpeningCashCents,
            true);

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
}
