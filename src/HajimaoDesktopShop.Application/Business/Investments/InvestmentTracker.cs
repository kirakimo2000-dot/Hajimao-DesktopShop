using System.Collections.ObjectModel;
using HajimaoDesktopShop.Application.Business.Simulation;
using HajimaoDesktopShop.Application.Persistence;

namespace HajimaoDesktopShop.Application.Business.Investments;

public sealed class InvestmentTracker
{
    private readonly Dictionary<string, LatestInvestmentSaveData> _latestByStore =
        new(StringComparer.Ordinal);

    public InvestmentTracker(InvestmentTrackingSaveData? restored = null)
    {
        foreach (var investment in restored?.LatestInvestments ?? [])
        {
            Validate(investment);
            if (!_latestByStore.TryAdd(investment.StoreId, investment))
            {
                throw new ArgumentException(
                    $"Store '{investment.StoreId}' has duplicate latest investments.",
                    nameof(restored));
            }
        }
    }

    public bool HasAnyInvestment => _latestByStore.Count > 0;

    public void Record(
        InvestmentCandidate candidate,
        long gameMinute,
        BusinessDayReport? baselineDay)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        if (gameMinute < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(gameMinute));
        }

        var baseline = FindStoreReport(baselineDay, candidate.StoreId);
        _latestByStore[candidate.StoreId] = new LatestInvestmentSaveData(
            candidate.StoreId,
            candidate.Id,
            candidate.Kind,
            candidate.Return.CostCents,
            candidate.Return.ExpectedDailyNetBenefitCents,
            gameMinute,
            baseline is null ? null : baselineDay!.DayNumber,
            baseline?.NetProfitCents,
            baseline?.CompletedSales,
            baseline?.LostSales);
    }

    public InvestmentTrackingSnapshot? GetSnapshot(
        string storeId,
        BusinessDayReport? currentDay)
    {
        if (string.IsNullOrWhiteSpace(storeId))
        {
            return null;
        }

        var normalizedStoreId = storeId.Trim();
        if (!_latestByStore.TryGetValue(normalizedStoreId, out var investment))
        {
            return null;
        }

        var current = FindStoreReport(currentDay, normalizedStoreId);
        var canCompare = investment.BaselineDayNumber is { } baselineDayNumber
            && currentDay is not null
            && current is not null
            && currentDay.DayNumber > baselineDayNumber;
        var status = investment.BaselineDayNumber is null
            ? InvestmentComparisonStatus.BaselineUnavailable
            : canCompare
                ? InvestmentComparisonStatus.Compared
                : InvestmentComparisonStatus.WaitingForCompletedDay;

        return new InvestmentTrackingSnapshot(
            investment.StoreId,
            investment.CandidateId,
            investment.Kind,
            investment.CostCents,
            investment.ExpectedDailyNetBenefitCents,
            investment.GameMinute,
            status,
            investment.BaselineDayNumber,
            investment.BaselineNetProfitCents,
            investment.BaselineCompletedSales,
            investment.BaselineLostSales,
            current is null ? null : currentDay!.DayNumber,
            current?.NetProfitCents,
            current?.CompletedSales,
            current?.LostSales,
            canCompare ? current!.NetProfitCents - investment.BaselineNetProfitCents!.Value : null,
            canCompare ? current!.CompletedSales - investment.BaselineCompletedSales!.Value : null,
            canCompare ? current!.LostSales - investment.BaselineLostSales!.Value : null);
    }

    public InvestmentTrackingSaveData CaptureSaveData() =>
        new(new ReadOnlyCollection<LatestInvestmentSaveData>(
            _latestByStore.Values
                .OrderBy(item => item.StoreId, StringComparer.Ordinal)
                .ToArray()));

    private static StoreDayReport? FindStoreReport(BusinessDayReport? report, string storeId) =>
        report?.Stores.SingleOrDefault(item =>
            string.Equals(item.StoreId, storeId, StringComparison.Ordinal));

    private static void Validate(LatestInvestmentSaveData investment)
    {
        ArgumentNullException.ThrowIfNull(investment);
        if (string.IsNullOrWhiteSpace(investment.StoreId)
            || string.IsNullOrWhiteSpace(investment.CandidateId)
            || investment.CostCents < 0
            || investment.GameMinute < 0
            || investment.BaselineDayNumber < 0
            || investment.BaselineCompletedSales < 0
            || investment.BaselineLostSales < 0)
        {
            throw new ArgumentException("Restored investment tracking data is invalid.");
        }

        var hasBaseline = investment.BaselineDayNumber is not null;
        if (hasBaseline != (investment.BaselineNetProfitCents is not null)
            || hasBaseline != (investment.BaselineCompletedSales is not null)
            || hasBaseline != (investment.BaselineLostSales is not null))
        {
            throw new ArgumentException("Restored investment baseline is incomplete.");
        }
    }
}
