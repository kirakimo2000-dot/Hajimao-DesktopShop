using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Business.Investments;
using HajimaoDesktopShop.Application.Business.Offline;
using HajimaoDesktopShop.Application.Business.Simulation;
using HajimaoDesktopShop.Application.Business.Strategy;
using HajimaoDesktopShop.Application.Catalog;
using HajimaoDesktopShop.Desktop.Services;
using HajimaoDesktopShop.Infrastructure.Configuration;
using HajimaoDesktopShop.Domain.Employees;
using System.IO;

namespace HajimaoDesktopShop.Desktop.Tests.Progression;

public enum LongTermProgressionPolicy
{
    HighTurnover,
    HighMargin,
    CashPreservation
}

internal sealed record ProgressionCheckpoint(
    int Day,
    long CashCents,
    int PlayerLevel,
    int OpenStores,
    int Investments,
    int AvailableInvestmentRoutes,
    int MaximumGrowthStores,
    long NetProfitCents,
    int WagePaymentFailures,
    int CashRunwayTenthsOfDay,
    int CompletedSales,
    long RevenueCents,
    long GrossProfitCents);

internal sealed record LongTermProgressionScenario(
    IReadOnlyList<ProgressionCheckpoint> Checkpoints,
    BusinessSession Session)
{
    public ProgressionCheckpoint Day(int number) =>
        Checkpoints.Single(checkpoint => checkpoint.Day == number);
}

internal static class LongTermProgressionScenarioRunner
{
    private const int RealSecondsPerBusinessDay = 1_440;
    private static readonly Lazy<IReadOnlyList<ProductDefinition>> Products = new(LoadProducts);

    public static LongTermProgressionScenario Run(
        LongTermProgressionPolicy policy,
        int days,
        int seed = 8_101)
    {
        if (days <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(days));
        }

        var session = CreateSession(seed);
        var checkpoints = new List<ProgressionCheckpoint>(days);
        var investments = 0;
        for (var day = 1; day <= days; day++)
        {
            ApplyStrategy(session, policy);
            session.Simulation.AdvanceRealSeconds(RealSecondsPerBusinessDay);
            checkpoints.Add(Capture(session, day, investments));
            if (day < days && TryInvest(session, policy))
            {
                investments++;
            }
        }

        return new LongTermProgressionScenario(
            Array.AsReadOnly(checkpoints.ToArray()),
            session);
    }

    public static BusinessSession CreateSession(int seed)
    {
        var start = DesktopBusinessSessionFactory.Create(
            Products.Value,
            save: null,
            seed,
            new DateTimeOffset(2026, 8, 10, 8, 0, 0, TimeSpan.Zero));
        return start.Session;
    }

    public static IReadOnlyList<ProductDefinition> ProductionProducts => Products.Value;

    private static void ApplyStrategy(BusinessSession session, LongTermProgressionPolicy policy)
    {
        var storeId = session.Game.GetSnapshot().Stores
            .OrderBy(store => GrowthTotal(session, store.Id))
            .ThenBy(store => store.Id, StringComparer.Ordinal)
            .First().Id;
        var (pricing, stocking) = policy switch
        {
            LongTermProgressionPolicy.HighTurnover =>
                (StorePricingPreset.HighTurnover, StoreStockingPreset.FullShelves),
            LongTermProgressionPolicy.HighMargin =>
                (StorePricingPreset.HighMargin, StoreStockingPreset.Balanced),
            LongTermProgressionPolicy.CashPreservation =>
                (StorePricingPreset.Balanced, StoreStockingPreset.Lean),
            _ => throw new ArgumentOutOfRangeException(nameof(policy))
        };
        var result = session.Strategy.Apply(storeId, pricing, stocking);
        Assert.Equal(StoreStrategyCommandStatus.Success, result.Status);
    }

    private static bool TryInvest(BusinessSession session, LongTermProgressionPolicy policy)
    {
        var business = session.Game.GetSnapshot();
        var employees = session.Simulation.GetSnapshot().Employees.Employees;
        var candidates = business.Stores
            .SelectMany(store => session.Investments.GetPortfolio(store.Id)!.Candidates
                .Select(candidate => new CandidateRoute(store.Id, candidate)))
            .Where(route => route.Candidate.IsExecutable
                && route.Candidate.Return.CashPressure != InvestmentCashPressure.Critical
                && PreservesOperatingReserve(session, route.Candidate))
            .DistinctBy(route => (route.StoreId, route.Candidate.Id))
            .ToArray();

        var unstaffedStoreIds = business.Stores
            .Where(store => employees.All(employee => employee.StoreId != store.Id))
            .Select(store => store.Id)
            .ToHashSet(StringComparer.Ordinal);
        var staffing = candidates
            .Where(route => route.Candidate.Kind == InvestmentKind.Employee
                && unstaffedStoreIds.Contains(route.StoreId))
            .OrderBy(route => route.Candidate.Effect.AddedRole == EmployeeRole.Cashier ? 0 : 1)
            .ThenBy(route => route.Candidate.Return.CostCents)
            .ThenBy(route => route.StoreId, StringComparer.Ordinal)
            .ThenBy(route => route.Candidate.Id, StringComparer.Ordinal)
            .ToArray();
        if (unstaffedStoreIds.Count > 0)
        {
            return TryExecuteFirst(session, staffing);
        }

        var nextStore = session.Game.GetStoreCatalogSnapshot().FirstOrDefault(store => !store.IsOpen);
        if (nextStore is not null)
        {
            if (business.PlayerLevel + 1 >= nextStore.RequiredPlayerLevel)
            {
                var opening = candidates
                    .Where(route => route.Candidate.Kind == InvestmentKind.OpenStore
                        && route.Candidate.TargetId == nextStore.Id)
                    .OrderBy(route => route.StoreId, StringComparer.Ordinal)
                    .ToArray();
                return TryExecuteFirst(session, opening);
            }
        }

        var ordered = candidates
            .Where(route => route.Candidate.Kind != InvestmentKind.OpenStore)
            .OrderBy(route => Priority(policy, route.Candidate.Kind))
            .ThenBy(route => route.Candidate.Return.CostCents)
            .ThenBy(route => route.StoreId, StringComparer.Ordinal)
            .ThenBy(route => route.Candidate.Id, StringComparer.Ordinal)
            .ToArray();
        return TryExecuteFirst(session, ordered);
    }

    private static bool TryExecuteFirst(
        BusinessSession session,
        IEnumerable<CandidateRoute> candidates)
    {
        foreach (var route in candidates)
        {
            var result = session.Investments.Execute(route.StoreId, route.Candidate.Id);
            if (result.Status == InvestmentCommandStatus.Success)
            {
                return true;
            }
        }

        return false;
    }

    private static bool PreservesOperatingReserve(
        BusinessSession session,
        InvestmentCandidate candidate)
    {
        var report = session.Simulation.GetSnapshot().LastCompletedDay;
        if (report is null)
        {
            return false;
        }

        var necessaryOutflow = report.Stores.Sum(store => checked(
            store.RevenueCents - store.GrossProfitCents
            + store.WageCostCents
            + store.OperatingCostCents));
        return candidate.Return.CashAfterInvestmentCents >= necessaryOutflow;
    }

    private static ProgressionCheckpoint Capture(
        BusinessSession session,
        int day,
        int investments)
    {
        var snapshot = session.Simulation.GetSnapshot();
        var report = Assert.IsType<BusinessDayReport>(snapshot.LastCompletedDay);
        var routes = snapshot.Business.Stores
            .SelectMany(store => session.Investments.GetPortfolio(store.Id)!.Candidates
                .Select(candidate => (store.Id, candidate)))
            .Count(route => route.candidate.IsExecutable);
        var maximumGrowthStores = snapshot.Business.Stores.Count(store =>
        {
            var growth = session.Game.GetStoreGrowthSnapshot(store.Id);
            return growth.NextExpansionUpgradeCostCents is null
                && growth.NextShelfUpgradeCostCents is null
                && growth.NextDecorationUpgradeCostCents is null;
        });
        var necessaryOutflow = report.Stores.Sum(store => checked(
            store.RevenueCents - store.GrossProfitCents
            + store.WageCostCents
            + store.OperatingCostCents));
        var cashRunway = necessaryOutflow <= 0
            ? 0
            : checked((int)Math.Min(
                int.MaxValue,
                snapshot.Business.CashCents * 10 / necessaryOutflow));
        return new ProgressionCheckpoint(
            day,
            snapshot.Business.CashCents,
            snapshot.Business.PlayerLevel,
            snapshot.Business.Stores.Count,
            investments,
            routes,
            maximumGrowthStores,
            report.Stores.Sum(store => store.NetProfitCents),
            snapshot.Stores.Sum(store => store.WagePaymentFailures),
            cashRunway,
            report.Stores.Sum(store => store.CompletedSales),
            report.Stores.Sum(store => store.RevenueCents),
            report.Stores.Sum(store => store.GrossProfitCents));
    }

    private static int Priority(
        LongTermProgressionPolicy policy,
        InvestmentKind kind) => policy switch
        {
            LongTermProgressionPolicy.HighTurnover => kind switch
            {
                InvestmentKind.Shelf => 0,
                InvestmentKind.Expansion => 1,
                InvestmentKind.OpenStore => 2,
                InvestmentKind.Employee => 3,
                _ => 4
            },
            LongTermProgressionPolicy.HighMargin => kind switch
            {
                InvestmentKind.Decoration => 0,
                InvestmentKind.Expansion => 1,
                InvestmentKind.OpenStore => 2,
                InvestmentKind.Shelf => 3,
                _ => 4
            },
            LongTermProgressionPolicy.CashPreservation => kind switch
            {
                InvestmentKind.OpenStore => 0,
                InvestmentKind.Shelf => 1,
                InvestmentKind.Decoration => 2,
                InvestmentKind.Expansion => 3,
                _ => 4
            },
            _ => throw new ArgumentOutOfRangeException(nameof(policy))
        };

    private static int GrowthTotal(BusinessSession session, string storeId)
    {
        var growth = session.Game.GetStoreGrowthSnapshot(storeId);
        return growth.ExpansionLevel + growth.ShelfLevel + growth.DecorationLevel;
    }

    private static IReadOnlyList<ProductDefinition> LoadProducts()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "Config", "products.json");
        return new JsonProductCatalog(path).LoadAsync().GetAwaiter().GetResult();
    }

    private sealed record CandidateRoute(string StoreId, InvestmentCandidate Candidate);
}
