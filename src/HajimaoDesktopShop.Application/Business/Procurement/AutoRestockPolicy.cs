namespace HajimaoDesktopShop.Application.Business.Procurement;

public sealed record AutoRestockPolicy(
    string StoreId,
    string ProductId,
    bool IsEnabled,
    int ReorderPoint,
    int TargetQuantity,
    string PreferredChannelId,
    bool UseEmergencySupplierWhenOutOfStock);
