using HajimaoDesktopShop.Domain.Economy;
using HajimaoDesktopShop.Domain.Shops;

namespace HajimaoDesktopShop.Application.Business.StoreGrowth;

internal interface IStoreGrowthGateway
{
    StoreDevelopment? FindDevelopment(string storeId);

    StoreUpgradeResult TryUpgradeStore(string storeId, StoreUpgradeKind kind);

    bool TryChargePromotion(string storeId, Money cost);
}
