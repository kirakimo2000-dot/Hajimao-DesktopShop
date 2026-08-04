using HajimaoDesktopShop.Application.Diagnostics;
using HajimaoDesktopShop.Infrastructure.Logging;

namespace HajimaoDesktopShop.Infrastructure.Tests.Logging;

public sealed class SerilogGameDiagnosticSinkTests
{
    [Fact]
    public void Write_PersistsStructuredEventsAndExceptionDetails()
    {
        using var directory = new TemporaryDirectory();
        using (var sink = new SerilogGameDiagnosticSink(directory.Path))
        {
            sink.Write(new GameDiagnosticEvent(
                "application.started",
                GameDiagnosticLevel.Information,
                "Application started.",
                new Dictionary<string, string> { ["Mode"] = "restored" }));
            sink.Write(new GameDiagnosticEvent(
                "offline.settlement.capped",
                GameDiagnosticLevel.Warning,
                "Offline settlement was capped.",
                new Dictionary<string, string> { ["AppliedSeconds"] = "28800" }));
            sink.Write(new GameDiagnosticEvent(
                "simulation.failed",
                GameDiagnosticLevel.Error,
                "Simulation loop failed.",
                exception: new InvalidOperationException("simulated failure")));
        }

        var logPath = Assert.Single(Directory.GetFiles(directory.Path, "hajimao-*.log"));
        var log = File.ReadAllText(logPath);
        Assert.Contains("application.started", log, StringComparison.Ordinal);
        Assert.Contains("Application started.", log, StringComparison.Ordinal);
        Assert.Contains("Mode", log, StringComparison.Ordinal);
        Assert.Contains("restored", log, StringComparison.Ordinal);
        Assert.Contains("offline.settlement.capped", log, StringComparison.Ordinal);
        Assert.Contains("28800", log, StringComparison.Ordinal);
        Assert.Contains("simulation.failed", log, StringComparison.Ordinal);
        Assert.Contains("InvalidOperationException", log, StringComparison.Ordinal);
        Assert.Contains("simulated failure", log, StringComparison.Ordinal);
    }

    [Fact]
    public void Dispose_IsIdempotentAndWriteAfterDisposeFails()
    {
        using var directory = new TemporaryDirectory();
        var sink = new SerilogGameDiagnosticSink(directory.Path);

        sink.Dispose();
        sink.Dispose();

        Assert.Throws<ObjectDisposedException>(() => sink.Write(new GameDiagnosticEvent(
            "application.started",
            GameDiagnosticLevel.Information,
            "Application started.")));
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"hajimao-logging-tests-{Guid.NewGuid():N}");
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
