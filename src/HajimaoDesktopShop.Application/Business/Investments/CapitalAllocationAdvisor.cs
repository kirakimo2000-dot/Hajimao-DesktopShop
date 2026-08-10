using HajimaoDesktopShop.Application.Business.Analysis;

namespace HajimaoDesktopShop.Application.Business.Investments;

public static class CapitalAllocationAdvisor
{
    public static CapitalAllocationSnapshot Create(
        IReadOnlyList<StoreCatalogItemSnapshot> catalog,
        IReadOnlyList<StoreInvestmentPortfolio> portfolios)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(portfolios);

        var catalogById = catalog.ToDictionary(store => store.Id, StringComparer.Ordinal);
        var orderedPortfolios = portfolios
            .OrderBy(portfolio => portfolio.StoreId, StringComparer.Ordinal)
            .ToArray();
        var routes = orderedPortfolios
            .SelectMany(portfolio => portfolio.Candidates.Select(candidate =>
                new CandidateRoute(portfolio, candidate)))
            .DistinctBy(route => (route.Candidate.StoreId, route.Candidate.Id))
            .ToArray();
        var options = new List<CapitalAllocationOption>(capacity: 3);

        var weakest = orderedPortfolios
            .OrderBy(portfolio => portfolio.Economy.NetProfitCents)
            .ThenBy(portfolio => portfolio.Economy.CashRunwayTenthsOfDay)
            .ThenBy(portfolio => portfolio.StoreId, StringComparer.Ordinal)
            .FirstOrDefault();
        CandidateRoute? stabilization = null;
        if (weakest is not null)
        {
            var hasSpecificBottleneck = weakest.Economy.Bottleneck is not (
                StoreBottleneck.InsufficientData or StoreBottleneck.None);
            stabilization = routes
                .Where(route => route.Portfolio == weakest
                    && route.Candidate.Kind != InvestmentKind.OpenStore
                    && IsSafeAndExecutable(route.Candidate))
                .OrderBy(route => hasSpecificBottleneck
                    && route.Candidate.AddressedBottleneck == weakest.Economy.Bottleneck
                        ? 0
                        : 1)
                .ThenByDescending(route => route.Candidate.Return.ExpectedDailyNetBenefitCents)
                .ThenBy(route => route.Candidate.Return.CostCents)
                .ThenBy(route => route.Candidate.Id, StringComparer.Ordinal)
                .FirstOrDefault();
            AddOption(
                options,
                CapitalAllocationThesis.StabilizeWeakestStore,
                stabilization,
                catalogById);
        }

        var improveReturn = routes
            .Where(route => route.Candidate.Kind != InvestmentKind.OpenStore
                && IsSafeAndExecutable(route.Candidate)
                && route.Candidate.Return.ExpectedDailyNetBenefitCents > 0
                && route.Candidate.Return.PaybackDaysTenths is not null
                && !SameRoute(route, stabilization))
            .OrderBy(route => route.Candidate.Return.PaybackDaysTenths)
            .ThenBy(route => route.Candidate.Return.CostCents)
            .ThenBy(route => route.Portfolio.StoreId, StringComparer.Ordinal)
            .ThenBy(route => route.Candidate.Id, StringComparer.Ordinal)
            .FirstOrDefault();
        AddOption(
            options,
            CapitalAllocationThesis.ImproveReturn,
            improveReturn,
            catalogById);

        var expansion = routes
            .Where(route => route.Candidate.Kind == InvestmentKind.OpenStore
                && catalogById.TryGetValue(route.Candidate.TargetId, out var store)
                && !store.IsOpen)
            .OrderBy(route => catalogById[route.Candidate.TargetId].RequiredPlayerLevel)
            .ThenBy(route => route.Candidate.TargetId, StringComparer.Ordinal)
            .ThenBy(route => route.Portfolio.StoreId, StringComparer.Ordinal)
            .FirstOrDefault();
        AddOption(
            options,
            CapitalAllocationThesis.ExpandStreet,
            expansion,
            catalogById,
            useTargetStoreName: true);

        return new CapitalAllocationSnapshot(Array.AsReadOnly(options.ToArray()));
    }

    private static bool IsSafeAndExecutable(InvestmentCandidate candidate) =>
        candidate.IsExecutable
        && candidate.Return.CashPressure != InvestmentCashPressure.Critical;

    private static bool SameRoute(CandidateRoute route, CandidateRoute? other) =>
        other is not null
        && string.Equals(
            route.Portfolio.StoreId,
            other.Portfolio.StoreId,
            StringComparison.Ordinal)
        && string.Equals(route.Candidate.Id, other.Candidate.Id, StringComparison.Ordinal);

    private static void AddOption(
        ICollection<CapitalAllocationOption> options,
        CapitalAllocationThesis thesis,
        CandidateRoute? route,
        IReadOnlyDictionary<string, StoreCatalogItemSnapshot> catalogById,
        bool useTargetStoreName = false)
    {
        if (route is null)
        {
            return;
        }

        var nameId = useTargetStoreName
            ? route.Candidate.TargetId
            : route.Portfolio.StoreId;
        if (!catalogById.TryGetValue(nameId, out var store))
        {
            return;
        }

        options.Add(new CapitalAllocationOption(
            thesis,
            route.Portfolio.StoreId,
            store.Name,
            route.Candidate));
    }

    private sealed record CandidateRoute(
        StoreInvestmentPortfolio Portfolio,
        InvestmentCandidate Candidate);
}
