namespace HajimaoDesktopShop.Domain.Shops;

public sealed record RetailBusinessStoreState(
    ShopDefinition Definition,
    ShopFinancialState FinancialState);
