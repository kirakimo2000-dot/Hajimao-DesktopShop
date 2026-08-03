using HajimaoDesktopShop.Domain.Products;

namespace HajimaoDesktopShop.Domain.Economy;

public sealed record LedgerEntry(
    long Sequence,
    LedgerEntryType Type,
    ProductId? ProductId,
    int Quantity,
    Money Amount,
    Money BalanceAfter);
