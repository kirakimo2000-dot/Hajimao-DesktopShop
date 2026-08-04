namespace HajimaoDesktopShop.Domain.Shops;

public sealed record RetailBusinessStoreState(
    ShopDefinition Definition,
    ShopFinancialState FinancialState,
    StoreDevelopmentState? DevelopmentState = null);
