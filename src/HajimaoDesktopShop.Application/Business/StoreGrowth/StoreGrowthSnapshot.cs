namespace HajimaoDesktopShop.Application.Business.StoreGrowth;

public sealed record StoreGrowthSnapshot(
    string StoreId,
    int ExpansionLevel,
    int ShelfLevel,
    int DecorationLevel,
    int FloorAreaUnits,
    int ShelfSlotCount,
    int QueueComfortCapacity,
    int InventoryCapacityPermille,
    int AttractionBonusBasisPoints,
    long? NextExpansionUpgradeCostCents,
    long? NextShelfUpgradeCostCents,
    long? NextDecorationUpgradeCostCents,
    int PromotionArrivalBonusBasisPoints,
    int PromotionPurchaseBonusBasisPoints,
    ActivePromotionSnapshot? ActivePromotion);
