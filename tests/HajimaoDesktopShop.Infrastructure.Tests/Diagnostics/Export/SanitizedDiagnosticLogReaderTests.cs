using System.Text.Json;
using HajimaoDesktopShop.Application.Diagnostics;
using HajimaoDesktopShop.Infrastructure.Diagnostics.Export;
using HajimaoDesktopShop.Infrastructure.Logging;

namespace HajimaoDesktopShop.Infrastructure.Tests.Diagnostics.Export;

public sealed class SanitizedDiagnosticLogReaderTests
{
    [Fact]
    public void Read_CanReadEventsFromActiveSerilogSink()
    {
        using var directory = new TemporaryDirectory();
        using var sink = new SerilogGameDiagnosticSink(directory.Path);
        sink.Write(new GameDiagnosticEvent(
            "application.started",
            GameDiagnosticLevel.Information,
            "Application started."));

        var events = SanitizedDiagnosticLogReader.Read(directory.Path);

        var diagnosticEvent = Assert.Single(events);
        Assert.Equal("INF", diagnosticEvent.Level);
        Assert.Equal("application.started", diagnosticEvent.Name);
    }

    [Fact]
    public void Read_ReturnsOnlySanitizedHeadersAndDropsContinuationDetails()
    {
        using var directory = new TemporaryDirectory();
        File.WriteAllText(
            Path.Combine(directory.Path, "hajimao-20260811.log"),
            string.Join(
                Environment.NewLine,
                "2026-08-11T07:30:00.0000000+00:00 [INF] application.started Application started. {\"UserPath\":\"C:\\Users\\tester\\secret\"}",
                "System.InvalidOperationException: C:\\Users\\tester\\secret",
                "   at HajimaoDesktopShop.SecretThing.Run() in C:\\Users\\tester\\secret\\File.cs:line 12",
                "2026-08-11T07:31:00.0000000+00:00 [ERR] simulation.failed Simulation loop failed. {\"Path\":\"C:\\Users\\tester\\secret\"}"));

        var events = SanitizedDiagnosticLogReader.Read(directory.Path);

        Assert.Collection(
            events,
            first =>
            {
                Assert.Equal(DateTimeOffset.Parse("2026-08-11T07:30:00.0000000+00:00").UtcDateTime, first.TimestampUtc.UtcDateTime);
                Assert.Equal("INF", first.Level);
                Assert.Equal("application.started", first.Name);
            },
            second =>
            {
                Assert.Equal(DateTimeOffset.Parse("2026-08-11T07:31:00.0000000+00:00").UtcDateTime, second.TimestampUtc.UtcDateTime);
                Assert.Equal("ERR", second.Level);
                Assert.Equal("simulation.failed", second.Name);
            });

        var serialized = JsonSerializer.Serialize(events);
        Assert.DoesNotContain("tester", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Application started", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("Simulation loop failed", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("C:\\Users", serialized, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Read_MissingDirectoryReturnsEmptyReadOnlyResult()
    {
        var missingDirectory = Path.Combine(Path.GetTempPath(), $"hajimao-missing-{Guid.NewGuid():N}");

        var events = SanitizedDiagnosticLogReader.Read(missingDirectory);

        Assert.Empty(events);
        var collection = Assert.IsAssignableFrom<System.Collections.IList>(events);
        Assert.True(collection.IsReadOnly);
    }

    [Fact]
    public void Read_NonPositiveMaximumEventsThrows()
    {
        using var directory = new TemporaryDirectory();

        Assert.Throws<ArgumentOutOfRangeException>(() => SanitizedDiagnosticLogReader.Read(directory.Path, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => SanitizedDiagnosticLogReader.Read(directory.Path, -1));
    }

    [Fact]
    public void Read_SkipsInaccessibleLogFilesAndContinues()
    {
        using var directory = new TemporaryDirectory();
        var inaccessiblePath = Path.Combine(directory.Path, "hajimao-a.log");
        using var inaccessibleStream = new FileStream(
            inaccessiblePath,
            FileMode.Create,
            FileAccess.ReadWrite,
            FileShare.None);
        using (var writer = new StreamWriter(inaccessibleStream, leaveOpen: true))
        {
            writer.WriteLine("2026-08-11T07:30:00.0000000+00:00 [INF] application.locked Locked file");
        }

        File.WriteAllText(
            Path.Combine(directory.Path, "hajimao-b.log"),
            "2026-08-11T07:31:00.0000000+00:00 [INF] application.readable Readable file");

        var events = SanitizedDiagnosticLogReader.Read(directory.Path);

        var diagnosticEvent = Assert.Single(events);
        Assert.Equal("application.readable", diagnosticEvent.Name);
    }

    [Fact]
    public void Read_ReadsLogFilesInOrdinalOrderAndReturnsLastMaximumEvents()
    {
        using var directory = new TemporaryDirectory();
        File.WriteAllText(
            Path.Combine(directory.Path, "hajimao-b.log"),
            "2026-08-11T07:32:00.0000000+00:00 [INF] application.b Second ordinal file");
        File.WriteAllText(
            Path.Combine(directory.Path, "hajimao-a.log"),
            "2026-08-11T07:30:00.0000000+00:00 [INF] application.a First ordinal file");
        File.WriteAllText(
            Path.Combine(directory.Path, "other.log"),
            "2026-08-11T07:31:00.0000000+00:00 [INF] application.other Ignored file");

        var events = SanitizedDiagnosticLogReader.Read(directory.Path, maximumEvents: 1);

        var diagnosticEvent = Assert.Single(events);
        Assert.Equal("application.b", diagnosticEvent.Name);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"hajimao-sanitized-log-reader-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
