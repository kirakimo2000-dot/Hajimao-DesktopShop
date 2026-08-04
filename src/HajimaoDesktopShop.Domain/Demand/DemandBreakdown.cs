namespace HajimaoDesktopShop.Domain.Demand;

public sealed record DemandBreakdown(
    int BaseBasisPoints,
    int PriceAdjustmentBasisPoints,
    int ServiceAdjustmentBasisPoints,
    int QueueAdjustmentBasisPoints,
    int CleanlinessAdjustmentBasisPoints,
    int TimeAdjustmentBasisPoints,
    int FinalBasisPoints,
    int AttractionAdjustmentBasisPoints = 0,
    int PromotionAdjustmentBasisPoints = 0);
