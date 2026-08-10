using HajimaoDesktopShop.Application.Business.Offline;
using HajimaoDesktopShop.Desktop.Services;

namespace HajimaoDesktopShop.Desktop.Tests.Progression;

public sealed class LongTermProgressionScenarioTests
{
    [Theory]
    [InlineData(LongTermProgressionPolicy.HighTurnover)]
    [InlineData(LongTermProgressionPolicy.HighMargin)]
    [InlineData(LongTermProgressionPolicy.CashPreservation)]
    public void OneYearPolicies_RemainHealthyAndLeaveGrowthHeadroom(
        LongTermProgressionPolicy policy)
    {
        var scenario = LongTermProgressionScenarioRunner.Run(policy, days: 365);
        var dayNinety = scenario.Day(90);
        var dayOneEighty = scenario.Day(180);
        var dayThreeSixtyFive = scenario.Day(365);

        Assert.All(scenario.Checkpoints, checkpoint =>
        {
            Assert.True(checkpoint.CashCents >= 0, $"{policy}: {checkpoint}");
            Assert.Equal(0, checkpoint.WagePaymentFailures);
        });
        Assert.Equal(3, dayOneEighty.OpenStores);
        Assert.Equal(3, dayThreeSixtyFive.OpenStores);
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
    }

    [Fact]
    public void OneYearPolicies_ProduceDistinctTradeoffsAndPositiveDay365Profit()
    {
        var turnover = LongTermProgressionScenarioRunner.Run(
            LongTermProgressionPolicy.HighTurnover,
            days: 365);
        var margin = LongTermProgressionScenarioRunner.Run(
            LongTermProgressionPolicy.HighMargin,
            days: 365);
        var preservation = LongTermProgressionScenarioRunner.Run(
            LongTermProgressionPolicy.CashPreservation,
            days: 365);

        Assert.NotEqual(
            turnover.Day(365).CompletedSales,
            margin.Day(365).CompletedSales);
        Assert.True(
            GrossMarginBasisPoints(margin.Day(365)) > GrossMarginBasisPoints(turnover.Day(365)),
            $"turnover={turnover.Day(365)}; margin={margin.Day(365)}");
        Assert.All(
            new[] { turnover, margin, preservation },
            scenario => Assert.True(scenario.Day(365).NetProfitCents > 0));
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
            dayNinety.OpenStores == 3,
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
            dayThirty.OpenStores is >= 2 && dayThirty.OpenStores <= 3,
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

    [Fact]
    public void SavedCheckpoint_AdvancesEquallyOnlineAndThroughRepeatedOfflineReturns()
    {
        var savedAt = new DateTimeOffset(2026, 8, 10, 8, 0, 0, TimeSpan.Zero);
        var source = LongTermProgressionScenarioRunner.Run(
            LongTermProgressionPolicy.CashPreservation,
            days: 7).Session;
        var save = source.CaptureSaveData(savedAt);
        var online = DesktopBusinessSessionFactory.Create(
            LongTermProgressionScenarioRunner.ProductionProducts,
            save,
            seed: 99,
            nowUtc: savedAt).Session;
        online.Simulation.AdvanceRealSeconds(4_320);

        var offlineSave = save;
        var offlineTime = savedAt;
        for (var day = 0; day < 3; day++)
        {
            var nextTime = offlineTime.AddSeconds(1_440);
            var restored = DesktopBusinessSessionFactory.Create(
                LongTermProgressionScenarioRunner.ProductionProducts,
                offlineSave,
                seed: 99,
                nowUtc: nextTime,
                new OfflineSettlementPolicy(maxOfflineSeconds: 1_440, batchSize: 137));
            Assert.Equal(1_440, restored.OfflineSettlement?.AppliedSeconds);
            offlineTime = nextTime;
            offlineSave = restored.Session.CaptureSaveData(offlineTime);
        }

        var offline = DesktopBusinessSessionFactory.Create(
            LongTermProgressionScenarioRunner.ProductionProducts,
            offlineSave,
            seed: 99,
            nowUtc: offlineTime).Session;
        Assert.Equivalent(
            online.Simulation.GetSnapshot(),
            offline.Simulation.GetSnapshot(),
            strict: true);
    }

    private static int GrossMarginBasisPoints(ProgressionCheckpoint checkpoint) =>
        checkpoint.RevenueCents == 0
            ? 0
            : checked((int)(checkpoint.GrossProfitCents * 10_000 / checkpoint.RevenueCents));
}
