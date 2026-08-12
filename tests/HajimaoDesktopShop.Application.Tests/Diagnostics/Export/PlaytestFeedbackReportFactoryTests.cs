using System.Collections;
using System.Collections.ObjectModel;
using System.Text.Json;
using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Business.Employees;
using HajimaoDesktopShop.Application.Business.Simulation;
using HajimaoDesktopShop.Application.Business.StoreGrowth;
using HajimaoDesktopShop.Application.Business.Street;
using HajimaoDesktopShop.Application.Diagnostics.Export;
using HajimaoDesktopShop.Application.Game;
using HajimaoDesktopShop.Application.Persistence;
using HajimaoDesktopShop.Domain.Demand;
using HajimaoDesktopShop.Domain.Economy;
using HajimaoDesktopShop.Domain.Employees;
using HajimaoDesktopShop.Domain.Streets;

namespace HajimaoDesktopShop.Application.Tests.Diagnostics.Export;

public sealed class PlaytestFeedbackReportFactoryTests
{
    [Fact]
    public void Create_ProducesExpectedTopLevelAndStoreSummaryFields()
    {
        var report = PlaytestFeedbackReportFactory.Create(
            Snapshot(lastCompletedDay: new BusinessDayReport(
                7,
                new[]
                {
                    DayStore("north", completedSales: 8, lostSales: 2, netProfitCents: 700),
                    DayStore("south", completedSales: 3, lostSales: 1, netProfitCents: 200),
                })),
            new[] { Diagnostic("application.started") },
            "0.1.21",
            DateTimeOffset.Parse("2026-08-11T09:30:00Z"));

        Assert.Equal(
            new[]
            {
                "Product",
                "Version",
                "SaveSchemaVersion",
                "CreatedAtUtc",
                "GameMinute",
                "PlayerLevel",
                "CashCents",
                "OpenStoreCount",
                "EmployeeCount",
                "LastCompletedDayNumber",
                "Stores",
                "DiagnosticEvents",
            },
            typeof(PlaytestFeedbackReport).GetProperties().Select(property => property.Name));
        Assert.Equal(
            new[]
            {
                "StoreId",
                "ExpansionLevel",
                "ShelfLevel",
                "DecorationLevel",
                "RevenueCents",
                "GrossProfitCents",
                "WageCostCents",
                "OperatingCostCents",
                "NetProfitCents",
                "LastCompletedSales",
                "LastLostSales",
                "LastNetProfitCents",
            },
            report.Stores[0].GetType().GetProperties().Select(property => property.Name));

        Assert.Equal("Hajimao DesktopShop", report.Product);
        Assert.Equal("0.1.21", report.Version);
        Assert.Equal(GameSaveSchema.CurrentVersion, report.SaveSchemaVersion);
        Assert.Equal(1234, report.GameMinute);
        Assert.Equal(5, report.PlayerLevel);
        Assert.Equal(98765, report.CashCents);
        Assert.Equal(2, report.OpenStoreCount);
        Assert.Equal(1, report.EmployeeCount);
        Assert.Equal(7, report.LastCompletedDayNumber);

        Assert.Collection(
            report.Stores,
            north =>
            {
                Assert.Equal("north", north.StoreId);
                Assert.Equal(2, north.ExpansionLevel);
                Assert.Equal(3, north.ShelfLevel);
                Assert.Equal(4, north.DecorationLevel);
                Assert.Equal(10_000, north.RevenueCents);
                Assert.Equal(3_000, north.GrossProfitCents);
                Assert.Equal(1_200, north.WageCostCents);
                Assert.Equal(400, north.OperatingCostCents);
                Assert.Equal(1_400, north.NetProfitCents);
                Assert.Equal(8, north.LastCompletedSales);
                Assert.Equal(2, north.LastLostSales);
                Assert.Equal(700, north.LastNetProfitCents);
            },
            south =>
            {
                Assert.Equal("south", south.StoreId);
                Assert.Equal(0, south.ExpansionLevel);
                Assert.Equal(0, south.ShelfLevel);
                Assert.Equal(0, south.DecorationLevel);
                Assert.Equal(6_000, south.RevenueCents);
                Assert.Equal(2_000, south.GrossProfitCents);
                Assert.Equal(500, south.WageCostCents);
                Assert.Equal(100, south.OperatingCostCents);
                Assert.Equal(1_000, south.NetProfitCents);
                Assert.Equal(3, south.LastCompletedSales);
                Assert.Equal(1, south.LastLostSales);
                Assert.Equal(200, south.LastNetProfitCents);
            });
    }

    [Fact]
    public void Create_UsesNullLastDayValuesWhenNoCompletedDayExists()
    {
        var report = PlaytestFeedbackReportFactory.Create(
            Snapshot(lastCompletedDay: null),
            Array.Empty<SanitizedDiagnosticEvent>(),
            "0.1.21",
            DateTimeOffset.Parse("2026-08-11T09:30:00Z"));

        Assert.Null(report.LastCompletedDayNumber);
        Assert.All(
            report.Stores,
            store =>
            {
                Assert.Null(store.LastCompletedSales);
                Assert.Null(store.LastLostSales);
                Assert.Null(store.LastNetProfitCents);
            });
    }

    [Fact]
    public void Create_CopiesCollectionsIntoReadOnlySnapshots()
    {
        var diagnosticEvents = new List<SanitizedDiagnosticEvent>
        {
            Diagnostic("application.started"),
        };
        var snapshot = Snapshot(lastCompletedDay: null);

        var report = PlaytestFeedbackReportFactory.Create(
            snapshot,
            diagnosticEvents,
            "0.1.21",
            DateTimeOffset.Parse("2026-08-11T09:30:00Z"));

        diagnosticEvents.Add(Diagnostic("application.after-create"));

        Assert.Single(report.DiagnosticEvents);
        Assert.True(Assert.IsAssignableFrom<IList>(report.Stores).IsReadOnly);
        Assert.True(Assert.IsAssignableFrom<IList>(report.DiagnosticEvents).IsReadOnly);
    }

    [Fact]
    public void Create_SerializesOnlyAggregateAndSanitizedData()
    {
        var report = PlaytestFeedbackReportFactory.Create(
            Snapshot(lastCompletedDay: new BusinessDayReport(1, new[] { DayStore("north", 1, 2, 3) })),
            new[] { Diagnostic("application.failed") },
            "0.1.21",
            DateTimeOffset.Parse("2026-08-11T09:30:00Z"));

        var json = JsonSerializer.Serialize(report);

        Assert.Contains("north", json, StringComparison.Ordinal);
        Assert.DoesNotContain("North Shop Secret", json, StringComparison.Ordinal);
        Assert.DoesNotContain("emp-001", json, StringComparison.Ordinal);
        Assert.DoesNotContain("Alice Secret", json, StringComparison.Ordinal);
        Assert.DoesNotContain("candidate-001", json, StringComparison.Ordinal);
        Assert.DoesNotContain("Bob Candidate", json, StringComparison.Ordinal);
        Assert.DoesNotContain("customer-001", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("p-secret", json, StringComparison.Ordinal);
        Assert.DoesNotContain("Secret Product", json, StringComparison.Ordinal);
        Assert.DoesNotContain(Environment.UserName, json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(Path.GetTempPath(), json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hajimao.db", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Raw diagnostic message", json, StringComparison.Ordinal);
        Assert.DoesNotContain("Properties", json, StringComparison.Ordinal);
        Assert.DoesNotContain(Path.GetPathRoot(Environment.CurrentDirectory)!, json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("EmployeeId", json, StringComparison.Ordinal);
        Assert.DoesNotContain("CustomerId", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_ValidatesInputs()
    {
        var snapshot = Snapshot(lastCompletedDay: null);
        var events = Array.Empty<SanitizedDiagnosticEvent>();
        var createdAtUtc = DateTimeOffset.Parse("2026-08-11T09:30:00Z");

        Assert.Throws<ArgumentNullException>(() => PlaytestFeedbackReportFactory.Create(null!, events, "0.1.21", createdAtUtc));
        Assert.Throws<ArgumentNullException>(() => PlaytestFeedbackReportFactory.Create(snapshot, null!, "0.1.21", createdAtUtc));
        Assert.Throws<ArgumentException>(() => PlaytestFeedbackReportFactory.Create(snapshot, events, "", createdAtUtc));
        Assert.Throws<ArgumentException>(() => PlaytestFeedbackReportFactory.Create(snapshot, events, "   ", createdAtUtc));
    }

    private static BusinessSimulationSnapshot Snapshot(BusinessDayReport? lastCompletedDay)
    {
        var stores = new[]
        {
            Store(
                "north",
                "North Shop Secret",
                revenueCents: 10_000,
                grossProfitCents: 3_000,
                wageCostCents: 1_200,
                operatingCostCents: 400,
                netProfitCents: 1_400,
                growth: Growth("north", 2, 3, 4)),
            Store(
                "south",
                "South Shop Secret",
                revenueCents: 6_000,
                grossProfitCents: 2_000,
                wageCostCents: 500,
                operatingCostCents: 100,
                netProfitCents: 1_000,
                growth: null),
        };

        return new BusinessSimulationSnapshot(
            1234,
            new BusinessSnapshot(5, 456, 98_765, stores),
            new[]
            {
                Operations("north"),
                Operations("south"),
            },
            new EmployeeOperationsSnapshot(
                1,
                2,
                new[]
                {
                    new EmployeeCandidate("candidate-001", "Bob Candidate", EmployeeRole.Cashier, 900, new Money(100)),
                },
                new[]
                {
                    new EmployeeOperationsEmployeeSnapshot(
                        "emp-001",
                        "Alice Secret",
                        EmployeeRole.Cashier,
                        1000,
                        1000,
                        100,
                        1,
                        1000,
                        1000,
                        "north",
                        0,
                        480,
                        true),
                }),
            new CommercialStreetSnapshot(
                CommercialStreetTier.Neighbors,
                StreetWeather.Clear,
                1000,
                2,
                1,
                new[] { new CommercialStreetStoreSnapshot("north", "North Shop Secret", 100, 1000) }),
            lastCompletedDay);
    }

    private static BusinessStoreSnapshot Store(
        string id,
        string name,
        long revenueCents,
        long grossProfitCents,
        long wageCostCents,
        long operatingCostCents,
        long netProfitCents,
        StoreGrowthSnapshot? growth) =>
        new(
            id,
            name,
            revenueCents,
            StockPurchaseCostCents: revenueCents - grossProfitCents,
            grossProfitCents,
            new[]
            {
                new ProductSnapshot("p-secret", "Secret Product", 100, 150, 10, 20, "shelf", UnitGrossProfitCents: 50),
            },
            wageCostCents,
            netProfitCents,
            operatingCostCents,
            growth);

    private static StoreOperationsSnapshot Operations(string storeId) =>
        new(
            storeId,
            Visitors: 10,
            AcceptedPurchases: 9,
            CompletedSales: 8,
            LostSales: 1,
            CheckoutQueueLength: 0,
            CleanlinessPermille: 1000,
            ServicePermille: 1000,
            WagePaymentFailures: 0,
            ArrivalDemand: new DemandBreakdown(1000, 0, 0, 0, 0, 0, 1000));

    private static StoreDayReport DayStore(
        string storeId,
        int completedSales,
        int lostSales,
        long netProfitCents) =>
        new(
            storeId,
            Visitors: completedSales + lostSales,
            AcceptedPurchases: completedSales,
            CompletedSales: completedSales,
            LostSales: lostSales,
            RevenueCents: 100,
            GrossProfitCents: 80,
            WageCostCents: 20,
            NetProfitCents: netProfitCents,
            ClosingCleanlinessPermille: 1000,
            AverageQueueLengthBasisPoints: 0,
            OperatingCostCents: 10);

    private static StoreGrowthSnapshot Growth(
        string storeId,
        int expansionLevel,
        int shelfLevel,
        int decorationLevel) =>
        new(
            storeId,
            expansionLevel,
            shelfLevel,
            decorationLevel,
            FloorAreaUnits: 1,
            ShelfSlotCount: 3,
            QueueComfortCapacity: 0,
            InventoryCapacityPermille: 1000,
            AttractionBonusBasisPoints: 0,
            NextExpansionUpgradeCostCents: null,
            NextShelfUpgradeCostCents: null,
            NextDecorationUpgradeCostCents: null,
            PromotionArrivalBonusBasisPoints: 0,
            PromotionPurchaseBonusBasisPoints: 0,
            ActivePromotion: null);

    private static SanitizedDiagnosticEvent Diagnostic(string name) =>
        new(
            DateTimeOffset.Parse("2026-08-11T09:00:00Z"),
            "INF",
            name);
}
