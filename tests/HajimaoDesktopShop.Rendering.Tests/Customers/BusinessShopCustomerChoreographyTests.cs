using HajimaoDesktopShop.Rendering.Customers;

namespace HajimaoDesktopShop.Rendering.Tests.Customers;

public sealed class BusinessShopCustomerChoreographyTests
{
    [Theory]
    [InlineData(0, CustomerJourneyStage.Entering)]
    [InlineData(16, CustomerJourneyStage.SeekingShelf)]
    [InlineData(40, CustomerJourneyStage.PickingProduct)]
    [InlineData(48, CustomerJourneyStage.JoiningQueue)]
    [InlineData(64, CustomerJourneyStage.CheckingOut)]
    [InlineData(80, CustomerJourneyStage.Leaving)]
    public void CreatePose_TraversesEveryJourneyStage(
        int frame,
        CustomerJourneyStage expected)
    {
        var pose = BusinessShopCustomerChoreography.CreatePose(
            "ambient",
            frame,
            actorSeed: 0,
            reduceMotion: false);

        Assert.Equal(expected, pose.Stage);
    }

    [Theory]
    [InlineData("ambient", 110)]
    [InlineData("chilled", 214)]
    [InlineData("frozen", 318)]
    public void CreatePose_PicksTheRequestedShelfZone(string shelfKind, int expectedX)
    {
        var pose = BusinessShopCustomerChoreography.CreatePose(
            shelfKind,
            presentationFrame: 40,
            actorSeed: 0,
            reduceMotion: false);

        Assert.Equal(expectedX, pose.X);
        Assert.Equal(148, pose.Y);
        Assert.True(pose.CarryingProduct);
    }

    [Fact]
    public void CreatePose_ReducedMotionFreezesPositionAndStage()
    {
        var first = BusinessShopCustomerChoreography.CreatePose(
            "ambient",
            presentationFrame: 3,
            actorSeed: 7,
            reduceMotion: true);
        var later = BusinessShopCustomerChoreography.CreatePose(
            "ambient",
            presentationFrame: 83,
            actorSeed: 7,
            reduceMotion: true);

        Assert.Equal(first, later);
    }

    [Fact]
    public void CreatePose_RejectsNullShelfKind()
    {
        Assert.Throws<ArgumentNullException>(() =>
            BusinessShopCustomerChoreography.CreatePose(
                null!,
                presentationFrame: 0,
                actorSeed: 0,
                reduceMotion: false));
    }
}
