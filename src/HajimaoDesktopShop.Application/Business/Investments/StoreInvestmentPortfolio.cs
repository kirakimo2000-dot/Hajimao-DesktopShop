using HajimaoDesktopShop.Application.Business.Analysis;

namespace HajimaoDesktopShop.Application.Business.Investments;

public sealed record StoreInvestmentPortfolio(
    string StoreId,
    StoreEconomyAnalysis Economy,
    IReadOnlyList<InvestmentCandidate> Candidates);
