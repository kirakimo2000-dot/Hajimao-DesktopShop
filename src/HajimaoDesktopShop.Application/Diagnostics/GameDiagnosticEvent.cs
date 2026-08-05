using System.Collections.ObjectModel;

namespace HajimaoDesktopShop.Application.Diagnostics;

public sealed record GameDiagnosticEvent
{
    public GameDiagnosticEvent(
        string name,
        GameDiagnosticLevel level,
        string message,
        IReadOnlyDictionary<string, string>? properties = null,
        Exception? exception = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Diagnostic event name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Diagnostic event message is required.", nameof(message));
        }

        Name = name.Trim();
        Level = level;
        Message = message.Trim();
        Properties = new ReadOnlyDictionary<string, string>(
            properties is null
                ? new Dictionary<string, string>(StringComparer.Ordinal)
                : new Dictionary<string, string>(properties, StringComparer.Ordinal));
        Exception = exception;
    }

    public string Name { get; }

    public GameDiagnosticLevel Level { get; }

    public string Message { get; }

    public IReadOnlyDictionary<string, string> Properties { get; }

    public Exception? Exception { get; }
}
