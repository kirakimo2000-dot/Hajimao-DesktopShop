namespace HajimaoDesktopShop.Rendering.Customers;

public sealed record CustomerJourneyPose(
    CustomerJourneyStage Stage,
    int X,
    int Y,
    bool CarryingProduct);
