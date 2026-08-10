namespace HajimaoDesktopShop.Application.Business.Investments;

public sealed record CapitalAllocationOption(
    CapitalAllocationThesis Thesis,
    string ExecutionStoreId,
    string StoreName,
    InvestmentCandidate Candidate);
