namespace HajimaoDesktopShop.Application.Diagnostics.Export;

public sealed record PlaytestFeedbackReport(
    string Product,
    string Version,
    int SaveSchemaVersion,
    DateTimeOffset CreatedAtUtc,
    long GameMinute,
    int PlayerLevel,
    long CashCents,
    int OpenStoreCount,
    int EmployeeCount,
    int? LastCompletedDayNumber,
    IReadOnlyList<PlaytestFeedbackStoreSummary> Stores,
    IReadOnlyList<SanitizedDiagnosticEvent> DiagnosticEvents);

public sealed record PlaytestFeedbackStoreSummary(
    string StoreId,
    int ExpansionLevel,
    int ShelfLevel,
    int DecorationLevel,
    long RevenueCents,
    long GrossProfitCents,
    long WageCostCents,
    long OperatingCostCents,
    long NetProfitCents,
    int? LastCompletedSales,
    int? LastLostSales,
    long? LastNetProfitCents);
