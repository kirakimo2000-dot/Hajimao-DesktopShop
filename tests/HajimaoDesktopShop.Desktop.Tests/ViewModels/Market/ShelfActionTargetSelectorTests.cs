using HajimaoDesktopShop.Application.Game;
using HajimaoDesktopShop.Desktop.ViewModels.Market;

namespace HajimaoDesktopShop.Desktop.Tests.ViewModels.Market;

public sealed class ShelfActionTargetSelectorTests
{
    [Fact]
    public void Select_PicksLowestFillRatioFromMatchingShelf()
    {
        var selected = ShelfActionTargetSelector.Select(
            [
                Product("tea", quantity: 2, capacity: 20, shelfKind: "ambient"),
                Product("water", quantity: 1, capacity: 20, shelfKind: "ambient"),
                Product("juice", quantity: 0, capacity: 20, shelfKind: "chilled")
            ],
            "ambient");

        Assert.Equal("water", selected?.Id);
    }

    [Fact]
    public void Select_UsesOrdinalProductIdAsStableTieBreaker()
    {
        var selected = ShelfActionTargetSelector.Select(
            [
                Product("water", quantity: 0, capacity: 20, shelfKind: "ambient"),
                Product("tea", quantity: 0, capacity: 20, shelfKind: "ambient")
            ],
            "ambient");

        Assert.Equal("tea", selected?.Id);
    }

    [Fact]
    public void Select_ReturnsNullWhenShelfHasNoProducts()
    {
        var selected = ShelfActionTargetSelector.Select(
            [Product("juice", quantity: 0, capacity: 20, shelfKind: "chilled")],
            "frozen");

        Assert.Null(selected);
    }

    [Fact]
    public void Select_RejectsMissingInputs()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ShelfActionTargetSelector.Select(null!, "ambient"));
        Assert.Throws<ArgumentException>(() =>
            ShelfActionTargetSelector.Select([], " "));
    }

    private static ProductSnapshot Product(
        string id,
        int quantity,
        int capacity,
        string shelfKind) =>
        new(
            id,
            id,
            WholesalePriceCents: 100,
            SalePriceCents: 200,
            quantity,
            capacity,
            shelfKind);
}
