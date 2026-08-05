namespace HajimaoDesktopShop.Application.Diagnostics;

public sealed class NullGameDiagnosticSink : IGameDiagnosticSink
{
    private NullGameDiagnosticSink()
    {
    }

    public static NullGameDiagnosticSink Instance { get; } = new();

    public void Write(GameDiagnosticEvent diagnosticEvent) =>
        ArgumentNullException.ThrowIfNull(diagnosticEvent);
}
