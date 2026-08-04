namespace HajimaoDesktopShop.Domain.Shops;

public sealed record StoreDevelopmentState(
    int ExpansionLevel,
    int ShelfLevel,
    int DecorationLevel);
