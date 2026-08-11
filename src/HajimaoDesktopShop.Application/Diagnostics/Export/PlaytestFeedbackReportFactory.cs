using System.Collections.ObjectModel;
using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Business.Simulation;
using HajimaoDesktopShop.Application.Persistence;

namespace HajimaoDesktopShop.Application.Diagnostics.Export;

public static class PlaytestFeedbackReportFactory
{
    public static PlaytestFeedbackReport Create(
        BusinessSimulationSnapshot snapshot,
        IReadOnlyList<SanitizedDiagnosticEvent> diagnosticEvents,
        string version,
        DateTimeOffset createdAtUtc)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(diagnosticEvents);

        if (string.IsNullOrWhiteSpace(version))
        {
            throw new ArgumentException("Version is required.", nameof(version));
        }

        var lastDayByStore = snapshot.LastCompletedDay?.Stores.ToDictionary(
            store => store.StoreId,
            StringComparer.Ordinal);
        var stores = snapshot.Business.Stores
            .Select(store => CreateStoreSummary(store, lastDayByStore))
            .ToArray();

        return new PlaytestFeedbackReport(
            "Hajimao DesktopShop",
            version.Trim(),
            GameSaveSchema.CurrentVersion,
            createdAtUtc.ToUniversalTime(),
            snapshot.GameMinute,
            snapshot.Business.PlayerLevel,
            snapshot.Business.CashCents,
            snapshot.Business.Stores.Count,
            snapshot.Employees.Employees.Count,
            snapshot.LastCompletedDay?.DayNumber,
            new ReadOnlyCollection<PlaytestFeedbackStoreSummary>(stores),
            new ReadOnlyCollection<SanitizedDiagnosticEvent>(diagnosticEvents.ToArray()));
    }

    private static PlaytestFeedbackStoreSummary CreateStoreSummary(
        BusinessStoreSnapshot store,
        IReadOnlyDictionary<string, StoreDayReport>? lastDayByStore)
    {
        StoreDayReport? lastDay = null;
        lastDayByStore?.TryGetValue(store.Id, out lastDay);
        var growth = store.Growth;

        return new PlaytestFeedbackStoreSummary(
            store.Id,
            growth?.ExpansionLevel ?? 0,
            growth?.ShelfLevel ?? 0,
            growth?.DecorationLevel ?? 0,
            store.RevenueCents,
            store.GrossProfitCents,
            store.WageCostCents,
            store.OperatingCostCents,
            store.NetProfitCents,
            lastDay?.CompletedSales,
            lastDay?.LostSales,
            lastDay?.NetProfitCents);
    }
}
