namespace HajimaoDesktopShop.Application.Diagnostics.Export;

public sealed record SanitizedDiagnosticEvent(
    DateTimeOffset TimestampUtc,
    string Level,
    string Name);
