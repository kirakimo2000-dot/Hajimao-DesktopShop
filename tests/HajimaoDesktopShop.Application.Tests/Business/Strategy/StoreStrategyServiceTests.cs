using HajimaoDesktopShop.Application.Business.Strategy;

namespace HajimaoDesktopShop.Application.Tests.Business.Strategy;

public sealed class StoreStrategyServiceTests
{
    [Fact]
    public void Apply_ChangesOnlySelectedStoreAndConfiguresEveryUnlockedProduct()
    {
        var session = BusinessTestSessionFactory.Create(openSecondStore: true);
        var beforeOther = session.Game.GetSnapshot().Stores.Single(store => store.Id == "store-2");

        var result = session.Strategy.Apply(
            "store-1",
            StorePricingPreset.HighMargin,
            StoreStockingPreset.Lean);

        Assert.Equal(StoreStrategyCommandStatus.Success, result.Status);
        Assert.All(
            session.Game.GetSnapshot().Stores.Single(store => store.Id == "store-1").Products,
            product => Assert.True(product.SalePriceCents > product.ReferenceSalePriceCents));
        Assert.All(
            session.Game.GetProcurementSnapshot().AutoRestockPolicies
                .Where(policy => policy.StoreId == "store-1"),
            policy => Assert.True(policy.IsEnabled));
        Assert.Equal(
            beforeOther.Products.Select(product => product.SalePriceCents),
            session.Game.GetSnapshot().Stores.Single(store => store.Id == "store-2")
                .Products.Select(product => product.SalePriceCents));
        Assert.Equal(
            StoreStockingPreset.Balanced,
            session.Strategy.GetAppliedPlan("store-2")?.Stocking);
    }

    [Fact]
    public void Apply_RejectsUnknownStoreWithoutChangingAnyStore()
    {
        var session = BusinessTestSessionFactory.Create(openSecondStore: true);
        var before = session.Game.GetSnapshot();
        var beforeProcurement = session.Game.GetProcurementSnapshot();

        var result = session.Strategy.Apply(
            "missing-store",
            StorePricingPreset.HighTurnover,
            StoreStockingPreset.FullShelves);

        Assert.Equal(StoreStrategyCommandStatus.UnknownStore, result.Status);
        Assert.Null(result.AppliedPlan);
        Assert.Equivalent(before, session.Game.GetSnapshot(), strict: true);
        Assert.Equivalent(
            beforeProcurement,
            session.Game.GetProcurementSnapshot(),
            strict: true);
    }

    [Fact]
    public void GetAppliedPlan_InfersPresetFromPersistedPricesAndPolicies()
    {
        var session = BusinessTestSessionFactory.Create();
        session.Strategy.Apply(
            "store-1",
            StorePricingPreset.HighTurnover,
            StoreStockingPreset.FullShelves);
        var save = session.CaptureSaveData();
        var restored = BusinessTestSessionFactory.Restore(save);

        var applied = restored.Strategy.GetAppliedPlan("store-1");

        Assert.NotNull(applied);
        Assert.Equal(StorePricingPreset.HighTurnover, applied.Pricing);
        Assert.Equal(StoreStockingPreset.FullShelves, applied.Stocking);
        Assert.Equal(2, applied.Products.Count);
    }

    [Fact]
    public void GetAppliedPlan_ReturnsNullWhenStoreDoesNotMatchACompletePreset()
    {
        var session = BusinessTestSessionFactory.Create();
        var water = session.Game.GetSnapshot().Stores.Single().Products
            .Single(product => product.Id == "water");
        session.Game.ChangePrice("store-1", "water", water.SalePriceCents + 1);

        var applied = session.Strategy.GetAppliedPlan("store-1");

        Assert.Null(applied);
    }
}
