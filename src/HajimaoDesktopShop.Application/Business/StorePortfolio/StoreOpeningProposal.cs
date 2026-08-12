namespace HajimaoDesktopShop.Application.Business.StorePortfolio;

public sealed record StoreOpeningProposal(
    string ProspectiveStoreId,
    int StreetOrdinal,
    string BrandId,
    string BrandName,
    string FormatId,
    string FormatName,
    long OpeningCostCents,
    long RecommendedReserveCents,
    long CashAfterOpeningCents,
    bool HasRecommendedReserve);
