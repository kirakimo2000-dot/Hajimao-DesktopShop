using HajimaoDesktopShop.Application.Business.Offline;
using HajimaoDesktopShop.Desktop.Services;

namespace HajimaoDesktopShop.Desktop.Tests.Progression;

public sealed class LongTermProgressionScenarioTests
{
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
