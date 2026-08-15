using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Catalog;
using HajimaoDesktopShop.Desktop.ViewModels.Market;
using HajimaoDesktopShop.Domain.Economy;
using HajimaoDesktopShop.Domain.Players;
using HajimaoDesktopShop.Domain.Shops;
using HajimaoDesktopShop.Infrastructure.Simulation;

namespace HajimaoDesktopShop.Desktop.Tests.ViewModels.Market;

public sealed class StoreLoadoutViewModelTests
{
    [Fact]
    public void Refresh_ProjectsThreeEquippedSlotsAndCollectionMastery()
    {
        var session = Session();
        var loadout = new StoreLoadoutViewModel(session, () => "corner-store");
        var collection = new ProductCollectionViewModel(session, () => "corner-store", loadout.Equip);

        loadout.Refresh();
        collection.Refresh();

        Assert.Equal(3, loadout.Slots.Count);
        Assert.All(loadout.Slots, slot => Assert.False(slot.IsEmpty));
        Assert.Equal(3, collection.Products.Count);
        Assert.All(collection.Products, product =>
        {
            Assert.Equal(1, product.MasteryLevel);
            Assert.True(product.IsEquipped);
            Assert.Contains("威力", product.PowerText, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void UseRecommendedLoadout_AtomicallyOrdersUnlockedProductsByCombatValue()
    {
        var session = Session();
        var loadout = new StoreLoadoutViewModel(session, () => "corner-store");

        loadout.UseRecommendedLoadoutCommand.Execute(null);

        Assert.Equal(["soda", "chips", "water"], loadout.Slots.Select(slot => slot.ProductId));
        Assert.Contains("推荐组合", loadout.StatusMessage, StringComparison.Ordinal);
    }

    private static BusinessSession Session()
    {
        var products = new[]
        {
            new ProductDefinition("water", "矿泉水", 100, 180, 24, "ambient"),
            new ProductDefinition("chips", "海盐薯片", 100, 180, 24, "ambient"),
            new ProductDefinition("soda", "橘子汽水", 100, 180, 24, "ambient")
        };
        var store = new ShopDefinition(
            new ShopId("corner-store"),
            new StoreBrandId("seven-eleven"),
            new StoreFormatId("convenience"),
            "7-Eleven",
            1,
            Money.Zero);
        var combat = new CombatContentCatalog(
            [
                CombatProduct("water", 10, 30, 1_000),
                CombatProduct("chips", 20, 25, 1_050),
                CombatProduct("soda", 30, 20, 1_100)
            ],
            [new CustomerArchetypeDefinition(
                "regular", 100, 10, 100, ["regular"],
                new Dictionary<string, int>(),
                new Dictionary<string, int> { ["water"] = 1 })],
            [new CustomerSpawnPoolDefinition(
                "all-day", 0, 0, [new CustomerSpawnPoolEntry("regular", 1)])],
            [],
            [new CharacterDefinition("maomao-default", "humanoid-v1", "maomao-default", 30)],
            [new StoreInteriorDefinition("seven-eleven", "Assets/Content/interiors/placeholders/default-shop.png")]);
        return BusinessSession.Create(
            products,
            [store],
            new LevelCurve([0, 10]),
            "corner-store",
            1_000,
            [],
            new DeterministicRandomSource(1),
            combatContent: combat);
    }

    private static ProductCombatDefinition CombatProduct(
        string id,
        int power,
        int interval,
        int revenue) =>
        new(id, power, interval, revenue, ProductEffectKind.None, 0, ["basic"], 1);
}
