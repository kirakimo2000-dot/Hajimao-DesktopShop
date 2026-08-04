namespace HajimaoDesktopShop.Application.Diagnostics;

public interface IGameDiagnosticSink
{
    void Write(GameDiagnosticEvent diagnosticEvent);
}
