using HajimaoDesktopShop.Application.Business.Street;

namespace HajimaoDesktopShop.Rendering;

public static class CommercialStreetLayout
{
    public const int LogicalHeight = 180;
    public const int StreetMargin = 12;
    public const int StorefrontWidth = 224;
    public const int StorefrontGap = 8;
    public const int StorefrontTop = 28;
    public const int StorefrontHeight = 102;

    public static int GetContentWidth(int openedStoreCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(openedStoreCount, 1);
        return checked(
            (StreetMargin * 2)
            + (openedStoreCount * StorefrontWidth)
            + ((openedStoreCount - 1) * StorefrontGap));
    }

    public static IReadOnlyList<CommercialStreetStorefrontLayout> CreateStorefronts(
        IReadOnlyList<CommercialStreetStoreSnapshot> stores)
    {
        ArgumentNullException.ThrowIfNull(stores);
        if (stores.Count == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stores), "Street requires one opened store.");
        }

        return Array.AsReadOnly(stores
            .Select((store, index) => new CommercialStreetStorefrontLayout(
                store.StoreId,
                new LogicalPixelRect(
                    StreetMargin + index * (StorefrontWidth + StorefrontGap),
                    StorefrontTop,
                    StorefrontWidth,
                    StorefrontHeight)))
            .ToArray());
    }

    public static string? HitTest(
        IReadOnlyList<CommercialStreetStorefrontLayout> storefronts,
        int contentX,
        int contentY)
    {
        ArgumentNullException.ThrowIfNull(storefronts);
        return storefronts.FirstOrDefault(item => item.Bounds.Contains(contentX, contentY))?.StoreId;
    }

    public static int ClampCameraOffset(int contentWidth, int viewportWidth, int requestedOffset)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(contentWidth, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(viewportWidth, 1);
        return Math.Clamp(requestedOffset, 0, Math.Max(0, contentWidth - viewportWidth));
    }
}

public sealed record CommercialStreetStorefrontLayout(string StoreId, LogicalPixelRect Bounds)
{
    public string StoreId { get; } =
        string.IsNullOrWhiteSpace(StoreId)
            ? throw new ArgumentException("Store id is required.", nameof(StoreId))
            : StoreId;
}
