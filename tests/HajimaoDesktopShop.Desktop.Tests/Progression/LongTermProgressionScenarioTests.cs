using HajimaoDesktopShop.Desktop.Services;
using HajimaoDesktopShop.Application.Business.Strategy;

namespace HajimaoDesktopShop.Desktop.Tests.Progression;

public sealed class LongTermProgressionScenarioTests
{
    [Fact]
    public void ProductionScenario_LoadsRichContentWithoutExpandingVisibleChoices()
    {
        var session = LongTermProgressionScenarioRunner.CreateSession(seed: 8_101);

        Assert.Equal(3, session.Investments.GetOpeningProposals().Count);
        Assert.Equal(3, session.Simulation.Employees.GetSnapshot().Candidates.Count);
        Assert.DoesNotContain(
            session.Simulation.Employees.GetSnapshot().Candidates,
            candidate => candidate.ProfileId.StartsWith("legacy-", StringComparison.Ordinal));
        Assert.NotNull(session.Simulation.GetSnapshot().MarketEvents);
    }

    [Theory]
    [InlineData("convenience", StorePricingPreset.Balanced, StoreStockingPreset.Balanced)]
    [InlineData("discount", StorePricingPreset.HighTurnover, StoreStockingPreset.FullShelves)]
    [InlineData("premium", StorePricingPreset.HighMargin, StoreStockingPreset.Lean)]
    public void ProductionScenario_CanStartEachStoreRouteWithItsRecommendedStrategy(
        string formatId,
        StorePricingPreset expectedPricing,
        StoreStockingPreset expectedStocking)
    {
        var session = LongTermProgressionScenarioRunner.CreateSession(seed: 8_101, formatId);
        var store = Assert.Single(session.Game.GetSnapshot().Stores);
        var strategy = Assert.IsType<StoreStrategyPlan>(session.Strategy.GetAppliedPlan(store.Id));

        Assert.Equal(formatId, store.StoreFormatId);
        Assert.Equal(expectedPricing, strategy.Pricing);
        Assert.Equal(expectedStocking, strategy.Stocking);
    }

    [Theory]
    [InlineData("convenience", LongTermProgressionPolicy.CashPreservation)]
    [InlineData("discount", LongTermProgressionPolicy.HighTurnover)]
    [InlineData("premium", LongTermProgressionPolicy.HighMargin)]
    public void OneYearStarterRoutes_RemainSolventAndReachExpansionGoals(
        string formatId,
        LongTermProgressionPolicy policy)
    {
        var scenario = LongTermProgressionScenarioRunner.Run(
            policy,
            days: 365,
            starterFormatId: formatId);

        Assert.All(scenario.Checkpoints, checkpoint =>
        {
            Assert.True(checkpoint.CashCents >= 0, $"{formatId}: {checkpoint}");
            Assert.Equal(0, checkpoint.WagePaymentFailures);
        });
        Assert.True(scenario.Day(7).CashCents >= 0, $"{formatId}: {scenario.Day(7)}");
        Assert.True(scenario.Day(30).Investments > 0, $"{formatId}: {scenario.Day(30)}");
        Assert.True(scenario.Day(90).AvailableInvestmentRoutes > 0, $"{formatId}: {scenario.Day(90)}");
        Assert.True(scenario.Day(365).OpenStores >= 6, $"{formatId}: {scenario.Day(365)}");
        Assert.True(scenario.Day(365).NetProfitCents > 0, $"{formatId}: {scenario.Day(365)}");
    }

    [Fact]
    public async Task OneYearPolicies_RemainHealthyLeaveGrowthHeadroomAndProduceDistinctTradeoffs()
    {
        var policies = Enum.GetValues<LongTermProgressionPolicy>();
        var completed = await Task.WhenAll(policies.Select(policy => Task.Run(() =>
            new KeyValuePair<LongTermProgressionPolicy, LongTermProgressionScenario>(
                policy,
                LongTermProgressionScenarioRunner.Run(policy, days: 365)))));
        var scenarios = completed.ToDictionary(pair => pair.Key, pair => pair.Value);

        foreach (var (policy, scenario) in scenarios)
        {
            var dayNinety = scenario.Day(90);
            var dayOneEighty = scenario.Day(180);
            var dayThreeSixtyFive = scenario.Day(365);

            Assert.All(scenario.Checkpoints, checkpoint =>
            {
                Assert.True(checkpoint.CashCents >= 0, $"{policy}: {checkpoint}");
                Assert.Equal(0, checkpoint.WagePaymentFailures);
            });
            Assert.True(dayOneEighty.OpenStores >= 4, $"{policy}: day180={dayOneEighty}");
            Assert.True(dayThreeSixtyFive.OpenStores >= 6, $"{policy}: day365={dayThreeSixtyFive}");
            Assert.True(
                dayOneEighty.Investments > dayNinety.Investments,
                $"{policy}: day90={dayNinety}; day180={dayOneEighty}");
            Assert.True(
                dayThreeSixtyFive.Investments > dayOneEighty.Investments,
                $"{policy}: day180={dayOneEighty}; day365={dayThreeSixtyFive}");
            Assert.True(
                dayThreeSixtyFive.MaximumGrowthStores < dayThreeSixtyFive.OpenStores,
                $"{policy}: day365={dayThreeSixtyFive}");
            Assert.True(
                scenario.Checkpoints
                    .Where(checkpoint => checkpoint.Day > 335)
                    .Count(checkpoint => checkpoint.NetProfitCents > 0) >= 24,
                $"{policy}: final30={string.Join("; ", scenario.Checkpoints.Where(checkpoint => checkpoint.Day > 335))}");
            Assert.True(
                dayThreeSixtyFive.NetProfitCents > 0,
                $"{policy}: day365={dayThreeSixtyFive}");
        }

        var turnover = scenarios[LongTermProgressionPolicy.HighTurnover];
        var margin = scenarios[LongTermProgressionPolicy.HighMargin];

        Assert.NotEqual(
            turnover.Day(365).CompletedSales,
            margin.Day(365).CompletedSales);
        Assert.True(
            GrossMarginBasisPoints(margin.Day(365)) > GrossMarginBasisPoints(turnover.Day(365)),
            $"turnover={turnover.Day(365)}; margin={margin.Day(365)}");
    }

    [Fact]
    public void CashPreservation_IsStrictlyDeterministicForOneHundredEightyDays()
    {
        var first = LongTermProgressionScenarioRunner.Run(
            LongTermProgressionPolicy.CashPreservation,
            days: 180);
        var second = LongTermProgressionScenarioRunner.Run(
            LongTermProgressionPolicy.CashPreservation,
            days: 180);

        Assert.Equivalent(first.Checkpoints, second.Checkpoints, strict: true);
    }

    [Theory]
    [InlineData(LongTermProgressionPolicy.HighTurnover)]
    [InlineData(LongTermProgressionPolicy.HighMargin)]
    [InlineData(LongTermProgressionPolicy.CashPreservation)]
    public void NinetyDayPolicies_RemainSolventAndRetainCapitalChoices(
        LongTermProgressionPolicy policy)
    {
        var scenario = LongTermProgressionScenarioRunner.Run(policy, days: 90);
        var dayThirty = scenario.Day(30);
        var dayNinety = scenario.Day(90);
        var firstWageFailure = scenario.Checkpoints.FirstOrDefault(
            checkpoint => checkpoint.WagePaymentFailures > 0);
        var beforeWageFailure = firstWageFailure is null || firstWageFailure.Day == 1
            ? null
            : scenario.Day(firstWageFailure.Day - 1);
        var firstThreeStores = scenario.Checkpoints.FirstOrDefault(
            checkpoint => checkpoint.OpenStores == 3);
        var firstFourthInvestment = scenario.Checkpoints.FirstOrDefault(
            checkpoint => checkpoint.Investments >= 4);

        Assert.True(dayNinety.CashCents >= 0);
        Assert.True(
            dayNinety.WagePaymentFailures == 0,
            $"{policy}: firstThreeStores={firstThreeStores}; "
                + $"firstFourthInvestment={firstFourthInvestment}; "
                + $"beforeFailure={beforeWageFailure}; firstFailure={firstWageFailure}; "
                + $"day30={dayThirty}; day90={dayNinety}");
        Assert.True(
            dayNinety.OpenStores >= 3,
            $"{policy}: day30={dayThirty}; day90={dayNinety}");
        Assert.True(
            dayNinety.Investments > dayThirty.Investments,
            $"{policy}: day30={dayThirty}; day90={dayNinety}");
        Assert.True(
            dayNinety.AvailableInvestmentRoutes > 0,
            $"{policy}: day30={dayThirty}; day90={dayNinety}");
        Assert.True(
            dayNinety.MaximumGrowthStores < dayNinety.OpenStores,
            $"{policy}: day30={dayThirty}; day90={dayNinety}");
    }

    [Theory]
    [InlineData(LongTermProgressionPolicy.HighTurnover)]
    [InlineData(LongTermProgressionPolicy.HighMargin)]
    [InlineData(LongTermProgressionPolicy.CashPreservation)]
    public void ThirtyDayPolicies_AreDeterministicSolventAndLeaveLongTermGoals(
        LongTermProgressionPolicy policy)
    {
        var first = LongTermProgressionScenarioRunner.Run(policy, days: 30);
        var second = LongTermProgressionScenarioRunner.Run(policy, days: 30);

        Assert.Equivalent(first.Checkpoints, second.Checkpoints, strict: true);
        Assert.All(first.Checkpoints, checkpoint =>
        {
            Assert.True(checkpoint.CashCents >= 0);
            Assert.Equal(0, checkpoint.WagePaymentFailures);
        });

        var dayOne = first.Day(1);
        var daySeven = first.Day(7);
        var dayThirty = first.Day(30);
        Assert.Equal(1, dayOne.OpenStores);
        Assert.Equal(0, dayOne.MaximumGrowthStores);
        Assert.True(daySeven.Investments > 0);
        Assert.True(
            dayThirty.OpenStores >= 2,
            $"{policy}: day1={dayOne}; day7={daySeven}; day30={dayThirty}");
        Assert.True(dayThirty.AvailableInvestmentRoutes > 0);
        Assert.True(dayThirty.MaximumGrowthStores < dayThirty.OpenStores);
    }

    [Fact]
    public void Policies_ProduceDifferentTradeoffsWithoutOneExclusiveSurvivor()
    {
        var turnover = LongTermProgressionScenarioRunner.Run(
            LongTermProgressionPolicy.HighTurnover,
            days: 30);
        var margin = LongTermProgressionScenarioRunner.Run(
            LongTermProgressionPolicy.HighMargin,
            days: 30);
        var preservation = LongTermProgressionScenarioRunner.Run(
            LongTermProgressionPolicy.CashPreservation,
            days: 30);

        Assert.NotEqual(turnover.Day(30).CompletedSales, margin.Day(30).CompletedSales);
        Assert.NotEqual(
            GrossMarginBasisPoints(turnover.Day(30)),
            GrossMarginBasisPoints(margin.Day(30)));
        Assert.True(
            preservation.Day(1).CashRunwayTenthsOfDay > turnover.Day(1).CashRunwayTenthsOfDay,
            $"turnover={turnover.Day(1)}; margin={margin.Day(1)}; preservation={preservation.Day(1)}");
        Assert.All(
            new[] { turnover, margin, preservation },
            scenario => Assert.True(scenario.Day(30).OpenStores >= 2));
    }

    private static int GrossMarginBasisPoints(ProgressionCheckpoint checkpoint) =>
        checkpoint.RevenueCents == 0
            ? 0
            : checked((int)(checkpoint.GrossProfitCents * 10_000 / checkpoint.RevenueCents));
}
