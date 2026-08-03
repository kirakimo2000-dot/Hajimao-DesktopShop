namespace HajimaoDesktopShop.Application.Catalog;

public interface IProductCatalog
{
    Task<IReadOnlyList<ProductDefinition>> LoadAsync(CancellationToken cancellationToken = default);
}
