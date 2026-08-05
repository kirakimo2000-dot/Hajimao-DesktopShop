namespace HajimaoDesktopShop.Rendering.Customers;

public static class BusinessShopCustomerChoreography
{
    public const int CycleFrames = 96;

    private const int ActorY = 148;

    public static CustomerJourneyPose CreatePose(
        string shelfKind,
        long presentationFrame,
        int actorSeed,
        bool reduceMotion)
    {
        ArgumentNullException.ThrowIfNull(shelfKind);
        var shelfX = shelfKind.ToLowerInvariant() switch
        {
            "chilled" => 214,
            "frozen" => 318,
            _ => 110
        };
        var localFrame = reduceMotion
            ? PositiveModulo(actorSeed * 7L, CycleFrames)
            : PositiveModulo(presentationFrame + actorSeed * 7L, CycleFrames);

        return localFrame switch
        {
            < 16 => Pose(CustomerJourneyStage.Entering, localFrame, 0, 15, 408, 380, false),
            < 40 => Pose(CustomerJourneyStage.SeekingShelf, localFrame, 16, 39, 380, shelfX, false),
            < 48 => new CustomerJourneyPose(
                CustomerJourneyStage.PickingProduct,
                shelfX,
                ActorY,
                CarryingProduct: true),
            < 64 => Pose(CustomerJourneyStage.JoiningQueue, localFrame, 48, 63, shelfX, 302, true),
            < 80 => Pose(CustomerJourneyStage.CheckingOut, localFrame, 64, 79, 302, 330, true),
            _ => Pose(CustomerJourneyStage.Leaving, localFrame, 80, 95, 330, 408, false)
        };
    }

    private static CustomerJourneyPose Pose(
        CustomerJourneyStage stage,
        int frame,
        int firstFrame,
        int lastFrame,
        int firstX,
        int lastX,
        bool carryingProduct)
    {
        var elapsed = frame - firstFrame;
        var duration = lastFrame - firstFrame;
        var x = checked(firstX + ((lastX - firstX) * elapsed / duration));
        return new CustomerJourneyPose(stage, x, ActorY, carryingProduct);
    }

    private static int PositiveModulo(long value, int modulus) =>
        (int)((value % modulus + modulus) % modulus);
}
