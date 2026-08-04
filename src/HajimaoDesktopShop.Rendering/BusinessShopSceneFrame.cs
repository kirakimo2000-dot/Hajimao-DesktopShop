using HajimaoDesktopShop.Application.Business.Simulation;

namespace HajimaoDesktopShop.Rendering;

public sealed record BusinessShopSceneFrame(
    BusinessSimulationSnapshot Snapshot,
    string StoreId);
