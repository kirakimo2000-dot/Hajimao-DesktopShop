using HajimaoDesktopShop.Rendering.Interactions;

namespace HajimaoDesktopShop.Desktop.Controls;

public sealed class BusinessShopObjectClickedEventArgs(
    BusinessShopInteractionTarget target) : EventArgs
{
    public BusinessShopInteractionTarget Target { get; } =
        target ?? throw new ArgumentNullException(nameof(target));
}
