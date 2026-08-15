using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Business.Simulation;
using HajimaoDesktopShop.Application.Business.Strategy;
using HajimaoDesktopShop.Application.Catalog;
using HajimaoDesktopShop.Application.Simulation;
using HajimaoDesktopShop.Desktop.Services;
using HajimaoDesktopShop.Domain.Players;
using HajimaoDesktopShop.Infrastructure.Simulation;

namespace HajimaoDesktopShop.Desktop.Tests.ViewModels.Market;

internal static class MarketTestSession
{
    public static BusinessSession Create(
        long openingCashCents = 100_000,
        long purchasePriceCents = 100) =>
        BusinessSession.Create(
            [new ProductDefinition("water", "矿泉水", purchasePriceCents, 200, 20, "ambient", 1)],
            DesktopGameContent.Shops,
            new LevelCurve([0, 40, 120, 300, 650, 1_200]),
            DesktopGameContent.StarterStoreId,
            openingCashCents,
            [],
            new DeterministicRandomSource(42),
            new BusinessSimulationOptions(),
            storeContent: StoreContent(),
            combatContent: CombatContent());

    private static StoreContentCatalog StoreContent() => new(
        [
            Format("convenience", 40_000),
            Format("discount", 70_000),
            Format("premium", 90_000)
        ],
        [
            new StoreBrandDefinition("familymart", "FamilyMart", "global", "convenience", "facade", "reference", "review"),
            new StoreBrandDefinition("aldi", "ALDI", "global", "discount", "facade", "reference", "review"),
            new StoreBrandDefinition("harrods", "Harrods", "global", "premium", "facade", "reference", "review")
        ]);

    private static StoreFormatDefinition Format(string id, long openingCost) => new(
        id,
        id,
        openingCost,
        20_000,
        1_000,
        1_000,
        1_000,
        1_000,
        1_000,
        1_000,
        new Dictionary<string, int>
        {
            ["ambient"] = 1_000,
            ["chilled"] = 1_000,
            ["frozen"] = 1_000
        },
        StorePricingPreset.Balanced,
        StoreStockingPreset.Balanced);

    private static CombatContentCatalog CombatContent() => new(
        [new ProductCombatDefinition("water", 10, 3, 1_000, ProductEffectKind.None, 0, ["basic"], 1)],
        [new CustomerArchetypeDefinition(
            "regular", 100, 10, 100, ["regular"],
            new Dictionary<string, int>(),
            new Dictionary<string, int> { ["water"] = 1 })],
        [new CustomerSpawnPoolDefinition(
            "all-day", 0, 0, [new CustomerSpawnPoolEntry("regular", 1)])],
        [],
        [new CharacterDefinition("maomao-default", "humanoid-v1", "maomao-default", 3)],
        [new StoreInteriorDefinition(
            "seven-eleven",
            "Assets/Content/interiors/placeholders/default-shop.png")]);
}
