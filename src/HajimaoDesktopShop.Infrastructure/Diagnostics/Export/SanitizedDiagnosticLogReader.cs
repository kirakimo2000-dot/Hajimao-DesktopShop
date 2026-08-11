using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;
using HajimaoDesktopShop.Application.Diagnostics.Export;

namespace HajimaoDesktopShop.Infrastructure.Diagnostics.Export;

public static class SanitizedDiagnosticLogReader
{
    private static readonly Regex HeaderLinePattern = new(
        @"^(?<timestamp>\d{4}-\d{2}-\d{2}T\S+)\s+\[(?<level>INF|WRN|ERR)\]\s+(?<name>[a-z]+(?:\.[a-z]+)+)\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static IReadOnlyList<SanitizedDiagnosticEvent> Read(
        string logDirectory,
        int maximumEvents = 200)
    {
        if (maximumEvents <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumEvents), maximumEvents, "Maximum events must be positive.");
        }

        if (logDirectory is null)
        {
            throw new ArgumentNullException(nameof(logDirectory));
        }

        if (string.IsNullOrWhiteSpace(logDirectory))
        {
            throw new ArgumentException("Log directory is required.", nameof(logDirectory));
        }

        if (!Directory.Exists(logDirectory))
        {
            return Array.AsReadOnly(Array.Empty<SanitizedDiagnosticEvent>());
        }

        var events = new Queue<SanitizedDiagnosticEvent>(maximumEvents);
        foreach (var logPath in Directory
            .EnumerateFiles(logDirectory, "hajimao-*.log")
            .OrderBy(Path.GetFileName, StringComparer.Ordinal))
        {
            foreach (var line in File.ReadLines(logPath))
            {
                var diagnosticEvent = TryReadHeader(line);
                if (diagnosticEvent is null)
                {
                    continue;
                }

                if (events.Count == maximumEvents)
                {
                    events.Dequeue();
                }

                events.Enqueue(diagnosticEvent);
            }
        }

        return new ReadOnlyCollection<SanitizedDiagnosticEvent>(events.ToArray());
    }

    private static SanitizedDiagnosticEvent? TryReadHeader(string line)
    {
        var match = HeaderLinePattern.Match(line);
        if (!match.Success)
        {
            return null;
        }

        if (!DateTimeOffset.TryParse(
            match.Groups["timestamp"].Value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out var timestamp))
        {
            return null;
        }

        return new SanitizedDiagnosticEvent(
            timestamp.ToUniversalTime(),
            match.Groups["level"].Value,
            match.Groups["name"].Value);
    }
}
