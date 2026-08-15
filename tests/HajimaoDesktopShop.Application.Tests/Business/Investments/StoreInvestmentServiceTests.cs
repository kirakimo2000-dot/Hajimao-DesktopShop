using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Business.Investments;
using HajimaoDesktopShop.Application.Business.Simulation;
using HajimaoDesktopShop.Application.Business.Strategy;
using HajimaoDesktopShop.Application.Catalog;
using HajimaoDesktopShop.Application.Tests.Business;
using HajimaoDesktopShop.Application.Tests.Simulation;
using HajimaoDesktopShop.Domain.Economy;
using HajimaoDesktopShop.Domain.Employees;
using HajimaoDesktopShop.Domain.Players;
using HajimaoDesktopShop.Domain.Shops;

namespace HajimaoDesktopShop.Application.Tests.Business.Investments;

public sealed class StoreInvestmentServiceTests
{
    [Fact]
    public void GetCapitalAllocation_ComposesOpenStorePortfoliosWithoutChangingState()
    {
        var session = CreateOpeningSession(openingCashCents: 200_000);
        var cashBefore = session.Game.GetSnapshot().CashCents;

        var allocation = session.Investments.GetCapitalAllocation();

        Assert.Equal(2, allocation.Options.Count);
        Assert.Contains(allocation.Options, option =>
            option.Thesis == CapitalAllocationThesis.StabilizeWeakestStore
            && option.ExecutionStoreId == "store-1"
            && option.Candidate.Kind == InvestmentKind.Shelf);
        Assert.Contains(allocation.Options, option =>
            option.Thesis == CapitalAllocationThesis.ExpandStreet
            && option.ExecutionStoreId == "store-1"
            && option.Candidate.TargetId == "store-2");
        Assert.Equal(cashBefore, session.Game.GetSnapshot().CashCents);
        Assert.Single(session.Game.GetSnapshot().Stores);
    }

    [Fact]
    public void Execute_GrowthCandidateUsesExistingUpgradeCommandForSelectedStoreOnly()
    {
        var session = BusinessTestSessionFactory.Create(
            openSecondStore: true,
            openingCashCents: 500_000);
        var otherBefore = session.Game.GetStoreGrowthSnapshot("store-2");

        var result = session.Investments.Execute("store-1", "growth:shelf");

        Assert.Equal(InvestmentCommandStatus.Success, result.Status);
        Assert.Equal(InvestmentKind.Shelf, result.AppliedCandidate?.Kind);
        Assert.Equal(1, session.Game.GetStoreGrowthSnapshot("store-1").ShelfLevel);
        Assert.Equal(otherBefore, session.Game.GetStoreGrowthSnapshot("store-2"));
        var tracking = session.Investments.GetLatestComparison("store-1");
        Assert.NotNull(tracking);
        Assert.Equal("growth:shelf", tracking.CandidateId);
        Assert.Equal(InvestmentComparisonStatus.BaselineUnavailable, tracking.Status);
    }

    [Fact]
    public void Execute_EmployeeCandidateUsesExistingHireCommand()
    {
        var session = BusinessTestSessionFactory.Create(openingCashCents: 500_000);
        var candidate = session.Investments.GetPortfolio("store-1")!.Candidates
            .First(item => item.Kind == InvestmentKind.Employee);

        var result = session.Investments.Execute("store-1", candidate.Id);

        Assert.Equal(InvestmentCommandStatus.Success, result.Status);
        Assert.Equal(candidate.Return.CostCents, result.CostCents);
        Assert.Contains(
            session.Simulation.GetSnapshot().Employees.Employees,
            employee => employee.EmployeeId == result.CreatedEmployeeId
                && employee.StoreId == "store-1");
        Assert.DoesNotContain(
            session.Simulation.GetSnapshot().Employees.Candidates,
            item => item.CandidateId == candidate.TargetId);
        Assert.Equal(candidate.Id, session.Investments.GetLatestComparison("store-1")?.CandidateId);
    }

    [Fact]
    public void Execute_UnknownOrStaleCandidateDoesNotChangeCash()
    {
        var session = BusinessTestSessionFactory.Create(openingCashCents: 500_000);
        var candidate = session.Investments.GetPortfolio("store-1")!.Candidates
            .First(item => item.Kind == InvestmentKind.Employee);

        var first = session.Investments.Execute("store-1", candidate.Id);
        var cashAfterFirst = session.Game.GetSnapshot().CashCents;
        var stale = session.Investments.Execute("store-1", candidate.Id);
        var missing = session.Investments.Execute("store-1", "missing");

        Assert.Equal(InvestmentCommandStatus.Success, first.Status);
        Assert.Equal(InvestmentCommandStatus.UnknownCandidate, stale.Status);
        Assert.Equal(InvestmentCommandStatus.UnknownCandidate, missing.Status);
        Assert.Equal(cashAfterFirst, session.Game.GetSnapshot().CashCents);
    }

    [Fact]
    public void Execute_RejectsUnknownStoreAndUnaffordableCandidateAtomically()
    {
        var session = BusinessTestSessionFactory.Create(openingCashCents: 10_000);
        var cashBefore = session.Game.GetSnapshot().CashCents;

        var unknownStore = session.Investments.Execute("missing-store", "growth:shelf");
        var unaffordable = session.Investments.Execute("store-1", "growth:shelf");

        Assert.Equal(InvestmentCommandStatus.UnknownStore, unknownStore.Status);
        Assert.Equal(InvestmentCommandStatus.InsufficientFunds, unaffordable.Status);
        Assert.Equal(cashBefore, session.Game.GetSnapshot().CashCents);
        Assert.Equal(0, session.Game.GetStoreGrowthSnapshot("store-1").ShelfLevel);
    }

    [Fact]
    public void Execute_RevalidatesPrerequisiteImmediatelyBeforeDispatch()
    {
        var session = BusinessTestSessionFactory.Create(openingCashCents: 500_000);
        Assert.Equal(
            InvestmentCommandStatus.Success,
            session.Investments.Execute("store-1", "growth:shelf").Status);

        var blocked = session.Investments.Execute("store-1", "growth:shelf");

        Assert.Equal(InvestmentCommandStatus.NotAvailable, blocked.Status);
        Assert.Equal(1, session.Game.GetStoreGrowthSnapshot("store-1").ShelfLevel);
    }

    [Fact]
    public void Execute_OpenStoreCandidateUsesExistingOpenCommandAndCreatesRuntime()
    {
        var session = CreateOpeningSession(openingCashCents: 200_000);
        var candidate = session.Investments.GetPortfolio("store-1")!.Candidates
            .Single(item => item.Kind == InvestmentKind.OpenStore);

        var result = session.Investments.Execute("store-1", candidate.Id);

        Assert.Equal(InvestmentCommandStatus.Success, result.Status);
        Assert.Equal(80_000, result.CostCents);
        Assert.Equal(120_000, session.Game.GetSnapshot().CashCents);
        Assert.Equal(2, session.Game.GetSnapshot().Stores.Count);
        Assert.Contains(session.Simulation.GetSnapshot().Stores, store => store.StoreId == "store-2");
        Assert.Equal(candidate.Id, session.Investments.GetLatestComparison("store-2")?.CandidateId);
    }

    [Fact]
    public void Execute_RevalidatesOpenedAndUnaffordableStoreCandidatesAtomically()
    {
        var available = CreateOpeningSession(openingCashCents: 200_000);
        var candidateId = available.Investments.GetPortfolio("store-1")!.Candidates
            .Single(item => item.Kind == InvestmentKind.OpenStore).Id;
        Assert.Equal(
            InvestmentCommandStatus.Success,
            available.Investments.Execute("store-1", candidateId).Status);
        var cashAfterOpen = available.Game.GetSnapshot().CashCents;

        var stale = available.Investments.Execute("store-1", candidateId);
        var poor = CreateOpeningSession(openingCashCents: 79_999);
        var poorCandidate = poor.Investments.GetPortfolio("store-1")!.Candidates
            .Single(item => item.Kind == InvestmentKind.OpenStore);
        var insufficient = poor.Investments.Execute("store-1", poorCandidate.Id);

        Assert.Equal(InvestmentCommandStatus.UnknownCandidate, stale.Status);
        Assert.Equal(cashAfterOpen, available.Game.GetSnapshot().CashCents);
        Assert.Equal(InvestmentCommandStatus.InsufficientFunds, insufficient.Status);
        Assert.Single(poor.Game.GetSnapshot().Stores);
        Assert.Equal(79_999, poor.Game.GetSnapshot().CashCents);
    }

    [Fact]
    public void GetOpeningProposals_WithStoreContentReturnsThreeBrandChoicesAcrossFormats()
    {
        var session = CreatePortfolioSession(openingCashCents: 300_000);
        var cashBefore = session.Game.GetSnapshot().CashCents;

        var proposals = session.Investments.GetOpeningProposals();

        Assert.Equal(3, proposals.Count);
        Assert.Equal(3, proposals.Select(item => item.Id).Distinct(StringComparer.Ordinal).Count());
        Assert.True(proposals.Select(item => item.StoreFormatId).Distinct(StringComparer.Ordinal).Count() >= 2);
        Assert.All(proposals, item =>
        {
            Assert.Equal(InvestmentKind.OpenStore, item.Kind);
            Assert.Equal("store-0002", item.TargetId);
            Assert.False(string.IsNullOrWhiteSpace(item.StoreBrandId));
            Assert.True(item.Return.IsAffordable);
        });
        var expansion = session.Investments.GetCapitalAllocation().Options.Single(option =>
            option.Thesis == CapitalAllocationThesis.ExpandStreet);
        Assert.Contains(expansion.Candidate, proposals);
        Assert.Equal(cashBefore, session.Game.GetSnapshot().CashCents);
        Assert.Single(session.Game.GetSnapshot().Stores);
    }

    [Fact]
    public void Execute_BrandProposalCreatesDynamicStoreAndTracksTheNewInstance()
    {
        var session = CreatePortfolioSession(openingCashCents: 300_000);
        var proposal = session.Investments.GetOpeningProposals()[0];

        var result = session.Investments.Execute("store-0001", proposal.Id);

        Assert.Equal(InvestmentCommandStatus.Success, result.Status);
        var opened = session.Game.GetSnapshot().Stores.Single(store => store.Id == "store-0002");
        Assert.Equal(proposal.StoreBrandId, opened.StoreBrandId);
        Assert.Equal(proposal.StoreFormatId, opened.StoreFormatId);
        Assert.Equal(2, opened.StreetOrdinal);
        Assert.Contains(session.Simulation.GetSnapshot().Stores, store => store.StoreId == "store-0002");
        Assert.Equal(proposal.Id, session.Investments.GetLatestComparison("store-0002")?.CandidateId);
    }

    private static BusinessSession CreateOpeningSession(long openingCashCents) =>
        BusinessSession.Create(
            [new ProductDefinition("water", "矿泉水", 100, 200, 10, "ambient")],
            [
                new ShopDefinition(new ShopId("store-1"), "街角店", 1, Money.Zero),
                new ShopDefinition(new ShopId("store-2"), "车站店", 1, new Money(80_000))
            ],
            new LevelCurve([0]),
            "store-1",
            openingCashCents,
            [
                new StoreEmployeeAssignment(
                    "store-1",
                    new Employee(
                        new EmployeeId("cashier"),
                        "收银员",
                        EmployeeRole.Cashier,
                        1_000,
                        new Money(600)))
            ],
            new StatefulTestRandomSource(123),
            new BusinessSimulationOptions());

    private static BusinessSession CreatePortfolioSession(long openingCashCents)
    {
        var formats = new[]
        {
            Format("convenience", 40_000),
            Format("discount", 70_000),
            Format("premium", 90_000),
            Format("commuter", 60_000)
        };
        var brands = new[]
        {
            Brand("seven-eleven", "7-Eleven", "convenience"),
            Brand("familymart", "FamilyMart", "convenience"),
            Brand("aldi", "ALDI", "discount"),
            Brand("lidl", "Lidl", "discount"),
            Brand("ginza", "银座三越", "premium"),
            Brand("harrods", "Harrods", "premium"),
            Brand("circle-k", "Circle K", "commuter"),
            Brand("watsons", "Watsons", "commuter")
        };
        var content = new StoreContentCatalog(formats, brands);
        return BusinessSession.Create(
            [new ProductDefinition("water", "矿泉水", 100, 200, 10, "ambient")],
            [new ShopDefinition(
                new ShopId("store-0001"),
                new StoreBrandId("seven-eleven"),
                new StoreFormatId("convenience"),
                "7-Eleven",
                1,
                Money.Zero)],
            new LevelCurve([0]),
            "store-0001",
            openingCashCents,
            [],
            new StatefulTestRandomSource(123),
            new BusinessSimulationOptions(),
            storeContent: content);
    }

    private static StoreFormatDefinition Format(string id, long cost) => new(
        id,
        id,
        cost,
        recommendedReserveCents: 80_000,
        baseDemandPermille: 1_000,
        priceSensitivityPermille: 1_000,
        serviceSensitivityPermille: 1_000,
        queueSensitivityPermille: 1_000,
        cleanlinessSensitivityPermille: 1_000,
        inventoryCapacityPermille: 1_000,
        new Dictionary<string, int>
        {
            ["ambient"] = 1_000,
            ["chilled"] = 1_000,
            ["frozen"] = 1_000
        },
        StorePricingPreset.Balanced,
        StoreStockingPreset.Balanced);

    private static StoreBrandDefinition Brand(string id, string name, string formatId) =>
        new(id, name, "global", formatId, "facade", "real-world-name", "review-required");
}
