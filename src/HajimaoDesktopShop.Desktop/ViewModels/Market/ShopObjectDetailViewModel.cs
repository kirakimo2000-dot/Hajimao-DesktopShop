using HajimaoDesktopShop.Rendering.Interactions;

namespace HajimaoDesktopShop.Desktop.ViewModels.Market;

public sealed record ShopObjectDetailViewModel(
    BusinessShopInteractionKind Kind,
    string Key,
    string Title,
    string CategoryText,
    string SummaryText,
    string StatusText);
