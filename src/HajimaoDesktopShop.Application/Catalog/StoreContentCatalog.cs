namespace HajimaoDesktopShop.Application.Catalog;

public sealed record StoreContentCatalog(
    IReadOnlyList<StoreFormatDefinition> Formats,
    IReadOnlyList<StoreBrandDefinition> Brands);
