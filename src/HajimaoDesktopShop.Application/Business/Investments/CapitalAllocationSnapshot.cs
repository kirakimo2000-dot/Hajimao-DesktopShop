namespace HajimaoDesktopShop.Application.Business.Investments;

public sealed record CapitalAllocationSnapshot(
    IReadOnlyList<CapitalAllocationOption> Options);
