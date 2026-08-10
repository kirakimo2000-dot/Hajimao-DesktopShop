using System.Numerics;
using HajimaoDesktopShop.Application.Business.Analysis;
using HajimaoDesktopShop.Application.Business.Employees;
using HajimaoDesktopShop.Application.Business.Simulation;
using HajimaoDesktopShop.Application.Business.StoreGrowth;
using HajimaoDesktopShop.Domain.Employees;

namespace HajimaoDesktopShop.Application.Business.Investments;

public static class StoreInvestmentAdvisor
{
    public static StoreInvestmentPortfolio Create(
        BusinessSimulationSnapshot snapshot,
        StoreGrowthSnapshot growth,
        StoreEconomyAnalysis economy)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(growth);
        ArgumentNullException.ThrowIfNull(economy);
        if (!string.Equals(growth.StoreId, economy.StoreId, StringComparison.Ordinal))
        {
            throw new ArgumentException("Growth and economy snapshots must identify the same store.");
        }

        var store = snapshot.Business.Stores.SingleOrDefault(item =>
            string.Equals(item.Id, economy.StoreId, StringComparison.Ordinal))
            ?? throw new ArgumentException("Investment store was not found in the simulation snapshot.");
        var candidates = new List<InvestmentCandidate>();
        AddGrowthCandidates(candidates, snapshot, growth, economy);
        foreach (var candidate in snapshot.Employees.Candidates.OrderBy(
                     item => item.CandidateId,
                     StringComparer.Ordinal))
        {
            candidates.Add(CreateEmployeeCandidate(snapshot, economy, candidate));
        }

        return new StoreInvestmentPortfolio(
            store.Id,
            economy,
            Array.AsReadOnly(candidates.ToArray()));
    }

    private static void AddGrowthCandidates(
        ICollection<InvestmentCandidate> candidates,
        BusinessSimulationSnapshot snapshot,
        StoreGrowthSnapshot growth,
        StoreEconomyAnalysis economy)
    {
        if (growth.NextExpansionUpgradeCostCents is { } expansionCost)
        {
            candidates.Add(CreateGrowthCandidate(
                snapshot,
                economy,
                InvestmentKind.Expansion,
                "growth:expansion",
                expansionCost,
                new InvestmentObservableEffect(
                    ShelfSlotChange: 2,
                    QueueComfortChange: 2,
                    AttractionChangeBasisPoints: 150),
                economy.Bottleneck == StoreBottleneck.Checkout
                    ? StoreBottleneck.Checkout
                    : StoreBottleneck.Demand,
                economy.Bottleneck == StoreBottleneck.Checkout
                    ? InvestmentEstimateCondition.QueueLossesRepeat
                    : InvestmentEstimateCondition.TrafficConversionStaysStable,
                EstimateExpansionBenefit(economy)));
        }

        if (growth.NextShelfUpgradeCostCents is { } shelfCost)
        {
            candidates.Add(CreateGrowthCandidate(
                snapshot,
                economy,
                InvestmentKind.Shelf,
                "growth:shelf",
                shelfCost,
                new InvestmentObservableEffect(InventoryCapacityChangePermille: 250),
                StoreBottleneck.Stock,
                economy.Bottleneck == StoreBottleneck.Stock
                    ? InvestmentEstimateCondition.StockLossesRepeat
                    : InvestmentEstimateCondition.InsufficientEvidence,
                EstimateRecoveredSaleBenefit(economy, StoreBottleneck.Stock, divisor: 10),
                prerequisiteMet: growth.ShelfLevel < growth.ExpansionLevel + 1));
        }

        if (growth.NextDecorationUpgradeCostCents is { } decorationCost)
        {
            candidates.Add(CreateGrowthCandidate(
                snapshot,
                economy,
                InvestmentKind.Decoration,
                "growth:decoration",
                decorationCost,
                new InvestmentObservableEffect(AttractionChangeBasisPoints: 250),
                StoreBottleneck.Demand,
                economy.Bottleneck == StoreBottleneck.Demand
                    ? InvestmentEstimateCondition.TrafficConversionStaysStable
                    : InvestmentEstimateCondition.InsufficientEvidence,
                EstimateTrafficBenefit(economy, attractionChangeBasisPoints: 250),
                prerequisiteMet: growth.DecorationLevel < growth.ExpansionLevel + 1));
        }
    }

    private static InvestmentCandidate CreateGrowthCandidate(
        BusinessSimulationSnapshot snapshot,
        StoreEconomyAnalysis economy,
        InvestmentKind kind,
        string id,
        long costCents,
        InvestmentObservableEffect effect,
        StoreBottleneck addressedBottleneck,
        InvestmentEstimateCondition estimateCondition,
        long expectedBenefitCents,
        bool prerequisiteMet = true)
    {
        var estimate = InvestmentReturnCalculator.Calculate(
            costCents,
            expectedBenefitCents,
            snapshot.Business.CashCents,
            economy.NecessaryOutflowCents);
        return new InvestmentCandidate(
            id,
            economy.StoreId,
            kind,
            id["growth:".Length..],
            GrowthTargetName(kind),
            estimate,
            effect,
            addressedBottleneck,
            estimateCondition,
            Availability(estimate, prerequisiteMet));
    }

    private static InvestmentCandidate CreateEmployeeCandidate(
        BusinessSimulationSnapshot snapshot,
        StoreEconomyAnalysis economy,
        EmployeeCandidate candidate)
    {
        var addressed = RoleBottleneck(candidate.Role);
        var recoveredBenefit = EstimateRecoveredSaleBenefit(
            economy,
            addressed,
            divisor: Math.Max(1, 10_000 / Math.Min(candidate.EfficiencyPermille, 10_000)));
        var dailyWage = SaturatingMultiply(candidate.HourlyWage.Cents, 8);
        var expectedBenefit = SaturatingSubtract(recoveredBenefit, dailyWage);
        var estimate = InvestmentReturnCalculator.Calculate(
            candidate.HireCost.Cents,
            expectedBenefit,
            snapshot.Business.CashCents,
            economy.NecessaryOutflowCents);
        return new InvestmentCandidate(
            $"employee:{candidate.CandidateId}",
            economy.StoreId,
            InvestmentKind.Employee,
            candidate.CandidateId,
            candidate.Name,
            estimate,
            new InvestmentObservableEffect(
                AddedRole: candidate.Role,
                AddedEfficiencyPermille: candidate.EfficiencyPermille),
            addressed,
            addressed == economy.Bottleneck
                ? InvestmentEstimateCondition.RoleBottleneckPersists
                : InvestmentEstimateCondition.InsufficientEvidence,
            Availability(estimate, prerequisiteMet: true));
    }

    private static InvestmentAvailability Availability(
        InvestmentReturnEstimate estimate,
        bool prerequisiteMet)
    {
        if (!prerequisiteMet)
        {
            return InvestmentAvailability.PrerequisiteNotMet;
        }

        return estimate.IsAffordable
            ? InvestmentAvailability.Available
            : InvestmentAvailability.InsufficientFunds;
    }

    private static long EstimateExpansionBenefit(StoreEconomyAnalysis economy)
    {
        if (economy.Bottleneck == StoreBottleneck.Checkout)
        {
            return EstimateRecoveredSaleBenefit(economy, StoreBottleneck.Checkout, divisor: 20);
        }

        return economy.Bottleneck == StoreBottleneck.Demand
            ? EstimateTrafficBenefit(economy, attractionChangeBasisPoints: 150)
            : 0;
    }

    private static long EstimateRecoveredSaleBenefit(
        StoreEconomyAnalysis economy,
        StoreBottleneck requiredBottleneck,
        int divisor)
    {
        if (economy.Bottleneck != requiredBottleneck
            || economy.LostSales <= 0
            || economy.CompletedSales <= 0
            || economy.GrossProfitCents <= 0)
        {
            return 0;
        }

        var recoverableSales = Math.Min(
            economy.LostSales,
            Math.Max(1, economy.CompletedSales / divisor));
        return SaturatingMultiply(
            economy.GrossProfitCents / economy.CompletedSales,
            recoverableSales);
    }

    private static long EstimateTrafficBenefit(
        StoreEconomyAnalysis economy,
        int attractionChangeBasisPoints)
    {
        if (economy.Bottleneck != StoreBottleneck.Demand
            || economy.Visitors <= 0
            || economy.CompletedSales <= 0
            || economy.GrossProfitCents <= 0)
        {
            return 0;
        }

        var additionalVisitors = (long)economy.Visitors * attractionChangeBasisPoints / 10_000;
        var additionalSales = additionalVisitors * economy.CompletedSales / economy.Visitors;
        return SaturatingMultiply(
            economy.GrossProfitCents / economy.CompletedSales,
            additionalSales);
    }

    private static StoreBottleneck RoleBottleneck(EmployeeRole role) => role switch
    {
        EmployeeRole.Cashier => StoreBottleneck.Checkout,
        EmployeeRole.Restocker or EmployeeRole.Buyer => StoreBottleneck.Stock,
        EmployeeRole.SalesAssistant or EmployeeRole.Cleaner or EmployeeRole.Manager =>
            StoreBottleneck.Service,
        _ => StoreBottleneck.Service
    };

    private static string GrowthTargetName(InvestmentKind kind) => kind switch
    {
        InvestmentKind.Expansion => "扩建店面",
        InvestmentKind.Shelf => "升级货架",
        InvestmentKind.Decoration => "店铺装修",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    private static long SaturatingMultiply(long left, long right)
    {
        var value = new BigInteger(left) * right;
        return Saturate(value);
    }

    private static long SaturatingSubtract(long left, long right) =>
        Saturate(new BigInteger(left) - right);

    private static long Saturate(BigInteger value)
    {
        if (value > long.MaxValue)
        {
            return long.MaxValue;
        }

        return value < long.MinValue ? long.MinValue : (long)value;
    }
}
