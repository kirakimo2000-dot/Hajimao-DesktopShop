using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Business.Combat;
using HajimaoDesktopShop.Application.Catalog;
using HajimaoDesktopShop.Domain.Economy;
using HajimaoDesktopShop.Domain.Players;
using HajimaoDesktopShop.Domain.Shops;
using HajimaoDesktopShop.Infrastructure.Configuration;
using HajimaoDesktopShop.Infrastructure.Simulation;

namespace HajimaoDesktopShop.Infrastructure.Tests.Configuration;

public sealed class CombatLongTermScenarioTests
{
    [Fact]
    public async Task ActiveIdleProgressesAcrossOneToThreeHundredSixtyFiveSessionsWithoutOfflineGain()
    {
        var content = await LoadContent();
        var game = new BusinessGameService(
            content.Products.Select(product =>
                new ProductDefinition(product.ProductId, product.ProductId, 50, 100, 20, "ambient")),
            [new ShopDefinition(
                new ShopId("corner-store"),
                new StoreBrandId("seven-eleven"),
                new StoreFormatId("convenience"),
                "7-Eleven",
                1,
                Money.Zero)],
            new LevelCurve([0, 40, 120, 300, 650, 1_200, 2_000, 3_200, 5_000, 7_500]),
            "corner-store",
            120_000);
        var service = new BusinessCombatService(
            game,
            content,
            new DeterministicRandomSource(81_501));
        var checkpoints = new Dictionary<int, BusinessCombatSnapshot>();
        var checkpointDays = new HashSet<int> { 1, 7, 30, 90, 365 };

        for (var day = 1; day <= 365; day++)
        {
            for (var activeSecond = 0; activeSecond < 600; activeSecond++)
            {
                service.Tick((day + (activeSecond / 25)) % 24);
            }

            if (checkpointDays.Contains(day))
            {
                checkpoints.Add(day, service.GetSnapshot());
            }
        }

        var served = checkpointDays.Order().Select(day => checkpoints[day].Stores.Single().ServedCustomers).ToArray();
        var revenue = checkpointDays.Order().Select(day => checkpoints[day].Stores.Single().RevenueCents).ToArray();
        Assert.All(served, value => Assert.True(value > 0));
        Assert.True(served.SequenceEqual(served.Order()));
        Assert.True(revenue.SequenceEqual(revenue.Order()));
        Assert.True(checkpoints[365].Collection.Count > checkpoints[1].Collection.Count);
        Assert.Contains(checkpoints[365].Collection, product => product.MasteryLevel > 1);
        Assert.True(checkpoints[365].Loadouts.Single().UnlockedSlots > 3);

        var captured = service.CaptureSaveData();
        var restored = new BusinessCombatService(
            game,
            content,
            new DeterministicRandomSource(1),
            captured);
        Assert.Equivalent(captured, restored.CaptureSaveData(), strict: true);
        Assert.Equal(service.GetSnapshot().CashCents, restored.GetSnapshot().CashCents);
    }

    private static async Task<CombatContentCatalog> LoadContent()
    {
        var testData = Path.Combine(AppContext.BaseDirectory, "TestData");
        return await new JsonCombatContentCatalog(
            Path.Combine(testData, "products.json"),
            Path.Combine(testData, "store-brands.json"),
            Path.Combine(testData, "product-combat.json"),
            Path.Combine(testData, "customer-archetypes.json"),
            Path.Combine(testData, "customer-spawn-pools.json"),
            Path.Combine(testData, "characters.json"),
            Path.Combine(testData, "interiors.json")).LoadAsync();
    }
}
