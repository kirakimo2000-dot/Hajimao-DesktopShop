using HajimaoDesktopShop.Domain.Collections;

namespace HajimaoDesktopShop.Domain.Tests.Collections;

public sealed class ProductCollectionTests
{
    [Fact]
    public void RegisterCopy_FirstCopyUnlocksAtMasteryOne()
    {
        var collection = new ProductCollection();

        var update = collection.RegisterCopy("water");

        Assert.True(update.FirstUnlock);
        Assert.False(update.MasteryIncreased);
        Assert.Equal(new ProductCollectionEntry("water", 1, 0), update.Entry);
    }

    [Fact]
    public void RegisterCopy_ThreeDuplicatesRaiseMasteryOneToTwo()
    {
        var collection = new ProductCollection();
        collection.RegisterCopy("water");
        collection.RegisterCopy("water");
        collection.RegisterCopy("water");

        var update = collection.RegisterCopy("water");

        Assert.True(update.MasteryIncreased);
        Assert.Equal(2, update.Entry.MasteryLevel);
        Assert.Equal(0, update.Entry.StoredCopies);
    }

    [Theory]
    [InlineData(1, 3)]
    [InlineData(2, 5)]
    [InlineData(10, 21)]
    [InlineData(19, 39)]
    [InlineData(20, int.MaxValue)]
    public void CopiesRequired_FollowsLongTermGrowthCurve(int level, int expected)
    {
        Assert.Equal(expected, ProductCollection.CopiesRequired(level));
    }

    [Fact]
    public void RegisterCopy_MasteryNeverExceedsTwenty()
    {
        var collection = new ProductCollection(
            [new ProductCollectionEntry("water", 19, 38)]);

        collection.RegisterCopy("water");
        var capped = collection.RegisterCopy("water");

        Assert.Equal(20, capped.Entry.MasteryLevel);
        Assert.Equal(1, capped.Entry.StoredCopies);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(7)]
    public void Loadout_RequiresThreeToSixUnlockedSlots(int slots)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new StoreProductLoadout("store-a", slots, []));
    }

    [Fact]
    public void Loadout_RejectsDuplicateProductsInsideOneStore()
    {
        Assert.Throws<ArgumentException>(() =>
            new StoreProductLoadout("store-a", 3, ["water", "water"]));
    }

    [Fact]
    public void Loadout_AllowsSameProductAcrossDifferentStores()
    {
        var first = new StoreProductLoadout("store-a", 3, ["water"]);
        var second = new StoreProductLoadout("store-b", 3, ["water"]);

        Assert.Equal("water", Assert.Single(first.ProductIds));
        Assert.Equal("water", Assert.Single(second.ProductIds));
    }

    [Fact]
    public void WithEquipped_ReplacesExistingSlotWithoutOperationalSteps()
    {
        var loadout = new StoreProductLoadout("store-a", 3, ["water", "chips", "soda"]);

        var updated = loadout.WithEquipped(1, "green_tea");

        Assert.Equal(["water", "green_tea", "soda"], updated.ProductIds);
    }
}
