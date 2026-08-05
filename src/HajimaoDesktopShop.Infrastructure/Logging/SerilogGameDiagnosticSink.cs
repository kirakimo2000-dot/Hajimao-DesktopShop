using HajimaoDesktopShop.Application.Diagnostics;
using Serilog;
using Serilog.Events;

namespace HajimaoDesktopShop.Infrastructure.Logging;

public sealed class SerilogGameDiagnosticSink : IGameDiagnosticSink, IDisposable
{
    private readonly ILogger _logger;
    private int _disposed;

    public SerilogGameDiagnosticSink(string logDirectory)
    {
        if (string.IsNullOrWhiteSpace(logDirectory))
        {
            throw new ArgumentException("Log directory is required.", nameof(logDirectory));
        }

        Directory.CreateDirectory(logDirectory);
        var logPath = Path.Combine(logDirectory, "hajimao-.log");
        _logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                logPath,
                rollingInterval: RollingInterval.Day,
                fileSizeLimitBytes: 5 * 1024 * 1024,
                rollOnFileSizeLimit: true,
                retainedFileCountLimit: 14,
                buffered: false,
                shared: false,
                outputTemplate:
                    "{Timestamp:O} [{Level:u3}] {EventName} {Message:lj} {Properties:j}{NewLine}{Exception}")
            .CreateLogger();
    }

    public void Write(GameDiagnosticEvent diagnosticEvent)
    {
        ObjectDisposedException.ThrowIf(
            Volatile.Read(ref _disposed) != 0,
            this);
        ArgumentNullException.ThrowIfNull(diagnosticEvent);

        var contextualLogger = _logger;
        foreach (var property in diagnosticEvent.Properties)
        {
            contextualLogger = contextualLogger.ForContext(property.Key, property.Value);
        }

        contextualLogger
            .ForContext("EventName", diagnosticEvent.Name)
            .Write(
                MapLevel(diagnosticEvent.Level),
                diagnosticEvent.Exception,
                "{DiagnosticMessage}",
                diagnosticEvent.Message);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0
            && _logger is IDisposable disposableLogger)
        {
            disposableLogger.Dispose();
        }
    }

    private static LogEventLevel MapLevel(GameDiagnosticLevel level) => level switch
    {
        GameDiagnosticLevel.Information => LogEventLevel.Information,
        GameDiagnosticLevel.Warning => LogEventLevel.Warning,
        GameDiagnosticLevel.Error => LogEventLevel.Error,
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
    };
}
