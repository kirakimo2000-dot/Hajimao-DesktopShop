using HajimaoDesktopShop.Application.Business.Combat;
using HajimaoDesktopShop.Application.Catalog;
using HajimaoDesktopShop.Application.Simulation;
using HajimaoDesktopShop.Domain.Collections;

namespace HajimaoDesktopShop.Application.Tests.Business.Combat;

public sealed class ProductDropServiceTests
{
    private static readonly ProductCombatDefinition[] Products =
    [
        Product("water"),
        Product("chips")
    ];

    [Fact]
    public void Roll_NormalCustomerAwardsAtMostOneWeightedProduct()
    {
        var service = new ProductDropService(Products, new ScriptedIntegerRandomSource(0));

        var result = service.Roll(Customer("regular", ["regular"]));

        Assert.Equal(["water"], result.ProductIds);
        var roll = Assert.Single(result.Rolls);
        Assert.Equal("normal", roll.Source);
        Assert.True(roll.Awarded);
        Assert.Equal("water", roll.ProductId);
    }

    [Fact]
    public void Roll_NormalNoDropIsRecordedForDiagnostics()
    {
        var service = new ProductDropService(Products, new ScriptedIntegerRandomSource(199));

        var result = service.Roll(Customer("regular", ["regular"]));

        Assert.Empty(result.ProductIds);
        var roll = Assert.Single(result.Rolls);
        Assert.False(roll.Awarded);
        Assert.Null(roll.ProductId);
    }

    [Fact]
    public void Roll_EliteBonusIsIndependentFromNormalRoll()
    {
        var service = new ProductDropService(
            Products,
            new ScriptedIntegerRandomSource(199, 0, 99));

        var result = service.Roll(Customer("collector", ["elite"]));

        Assert.Equal(["chips"], result.ProductIds);
        Assert.Equal(3, result.Rolls.Count);
        Assert.Contains(result.Rolls, roll => roll.Source == "elite-chance" && roll.Awarded);
        Assert.Contains(result.Rolls, roll => roll.Source == "elite-product" && roll.ProductId == "chips");
    }

    [Fact]
    public void Roll_EquipmentBonusCanAwardAProductAfterTheNormalRollMisses()
    {
        var service = new ProductDropService(
            Products,
            new ScriptedIntegerRandomSource(199, 89, 99));

        var result = service.Roll(
            Customer("regular", ["regular"]),
            bonusDropPermille: 90);

        Assert.Equal(["chips"], result.ProductIds);
        Assert.Contains(result.Rolls, roll =>
            roll.Source == "equipment-bonus-chance" && roll.Awarded);
        Assert.Contains(result.Rolls, roll =>
            roll.Source == "equipment-bonus-product" && roll.ProductId == "chips");
    }

    [Fact]
    public void Equip_RequiresAnUnlockedProductAndUsesOneReplacementAction()
    {
        var collection = new ProductCollection();
        collection.RegisterCopy("water");
        var loadout = new StoreProductLoadout("store-a", 3, []);
        var service = new ProductLoadoutService();

        var equipped = service.Equip(loadout, collection, 0, "water");

        Assert.Equal(["water"], equipped.ProductIds);
        Assert.Throws<InvalidOperationException>(() =>
            service.Equip(equipped, collection, 1, "chips"));
    }

    private static ProductCombatDefinition Product(string id) =>
        new(id, 10, 20, 1_000, ProductEffectKind.None, 0, ["basic"], 1);

    private static CustomerArchetypeDefinition Customer(string id, string[] tags) =>
        new(
            id,
            100,
            50,
            100,
            tags,
            new Dictionary<string, int>(),
            new Dictionary<string, int> { ["water"] = 60, ["chips"] = 40 });

    private sealed class ScriptedIntegerRandomSource(params int[] values) : IRandomSource
    {
        private readonly Queue<int> _values = new(values);

        public double NextDouble() => throw new NotSupportedException();

        public int Next(int exclusiveMax)
        {
            var value = _values.Dequeue();
            Assert.InRange(value, 0, exclusiveMax - 1);
            return value;
        }
    }
}
