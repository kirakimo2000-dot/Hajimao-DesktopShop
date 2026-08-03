using HajimaoDesktopShop.Domain.Shops;

namespace HajimaoDesktopShop.Application.Business;

public sealed record BusinessSaleResult(
    SaleResult Sale,
    int PreviousPlayerLevel,
    int CurrentPlayerLevel,
    IReadOnlyList<string> NewlyUnlockedProductIds);
