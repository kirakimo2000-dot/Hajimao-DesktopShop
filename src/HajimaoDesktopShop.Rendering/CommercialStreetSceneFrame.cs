using HajimaoDesktopShop.Application.Business.Street;

namespace HajimaoDesktopShop.Rendering;

public sealed record CommercialStreetSceneFrame(
    CommercialStreetSnapshot Snapshot,
    int AnimationFrame = 0,
    bool ReduceMotion = false);
