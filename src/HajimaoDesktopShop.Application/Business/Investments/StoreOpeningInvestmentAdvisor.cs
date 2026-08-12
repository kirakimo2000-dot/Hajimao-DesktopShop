using System.Collections.ObjectModel;
using HajimaoDesktopShop.Application.Business.Analysis;
using HajimaoDesktopShop.Application.Business.Simulation;
using HajimaoDesktopShop.Application.Business.StorePortfolio;

namespace HajimaoDesktopShop.Application.Business.Investments;

public static class StoreOpeningInvestmentAdvisor
{
    public static IReadOnlyList<InvestmentCandidate> Create(
        IReadOnlyList<StoreOpeningProposal> proposals)
    {
        ArgumentNullException.ThrowIfNull(proposals);
        return new ReadOnlyCollection<InvestmentCandidate>(proposals
            .Select(proposal =>
            {
                ArgumentNullException.ThrowIfNull(proposal);
                var estimate = InvestmentReturnCalculator.Calculate(
                    proposal.OpeningCostCents,
                    expectedDailyNetBenefitCents: 0,
                    checked(proposal.CashAfterOpeningCents + proposal.OpeningCostCents),
                    proposal.RecommendedReserveCents);
                return new InvestmentCandidate(
                    $"store:open:{proposal.ProspectiveStoreId}:{proposal.BrandId}",
                    proposal.ProspectiveStoreId,
                    InvestmentKind.OpenStore,
                    proposal.ProspectiveStoreId,
                    proposal.BrandName,
                    estimate,
                    new InvestmentObservableEffect(StoreCountChange: 1),
                    StoreBottleneck.Demand,
                    InvestmentEstimateCondition.NewStoreNeedsCompletedDay,
                    estimate.IsAffordable
                        ? InvestmentAvailability.Available
                        : InvestmentAvailability.InsufficientFunds,
                    StoreBrandId: proposal.BrandId,
                    StoreFormatId: proposal.FormatId,
                    StreetOrdinal: proposal.StreetOrdinal,
                    RecommendedReserveCents: proposal.RecommendedReserveCents);
            })
            .ToArray());
    }

    public static IReadOnlyList<InvestmentCandidate> Create(
        BusinessSimulationSnapshot snapshot,
        IReadOnlyList<StoreCatalogItemSnapshot> storeCatalog,
        StoreEconomyAnalysis economy)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(storeCatalog);
        ArgumentNullException.ThrowIfNull(economy);

        var candidates = new List<InvestmentCandidate>();
        foreach (var store in storeCatalog)
        {
            ArgumentNullException.ThrowIfNull(store);
            if (store.IsOpen)
            {
                continue;
            }

            var estimate = InvestmentReturnCalculator.Calculate(
                store.OpeningCostCents,
                expectedDailyNetBenefitCents: 0,
                snapshot.Business.CashCents,
                economy.NecessaryOutflowCents);
            var availability = estimate.IsAffordable
                ? InvestmentAvailability.Available
                : InvestmentAvailability.InsufficientFunds;
            candidates.Add(new InvestmentCandidate(
                $"store:open:{store.Id}",
                store.Id,
                InvestmentKind.OpenStore,
                store.Id,
                store.Name,
                estimate,
                new InvestmentObservableEffect(StoreCountChange: 1),
                StoreBottleneck.Demand,
                InvestmentEstimateCondition.NewStoreNeedsCompletedDay,
                availability));
        }

        return new ReadOnlyCollection<InvestmentCandidate>(candidates);
    }
}
