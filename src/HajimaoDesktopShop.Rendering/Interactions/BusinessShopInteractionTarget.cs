namespace HajimaoDesktopShop.Rendering.Interactions;

public sealed record BusinessShopInteractionTarget
{
    public BusinessShopInteractionTarget(
        BusinessShopInteractionKind kind,
        string key,
        LogicalPixelRect bounds)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Interaction key is required.", nameof(key));
        }

        Kind = kind;
        Key = key;
        Bounds = bounds;
    }

    public BusinessShopInteractionKind Kind { get; }

    public string Key { get; }

    public LogicalPixelRect Bounds { get; }
}
