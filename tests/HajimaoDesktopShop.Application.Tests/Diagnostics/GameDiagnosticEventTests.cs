using HajimaoDesktopShop.Application.Diagnostics;

namespace HajimaoDesktopShop.Application.Tests.Diagnostics;

public sealed class GameDiagnosticEventTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RequiresEventName(string? name)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new GameDiagnosticEvent(name!, GameDiagnosticLevel.Information, "message"));

        Assert.Equal("name", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RequiresMessage(string? message)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new GameDiagnosticEvent("application.started", GameDiagnosticLevel.Information, message!));

        Assert.Equal("message", exception.ParamName);
    }

    [Fact]
    public void Constructor_CopiesPropertiesAndPreservesException()
    {
        var properties = new Dictionary<string, string>
        {
            ["StoreCount"] = "2"
        };
        var expectedException = new InvalidOperationException("boom");

        var diagnosticEvent = new GameDiagnosticEvent(
            "simulation.checkpoint.completed",
            GameDiagnosticLevel.Warning,
            "Simulation checkpoint completed.",
            properties,
            expectedException);
        properties["StoreCount"] = "99";
        properties["Added"] = "later";

        Assert.Equal("2", diagnosticEvent.Properties["StoreCount"]);
        Assert.False(diagnosticEvent.Properties.ContainsKey("Added"));
        Assert.Same(expectedException, diagnosticEvent.Exception);
        Assert.Equal(GameDiagnosticLevel.Warning, diagnosticEvent.Level);
    }

    [Fact]
    public void NullSink_AcceptsEventsWithoutSideEffects()
    {
        var diagnosticEvent = new GameDiagnosticEvent(
            "application.started",
            GameDiagnosticLevel.Information,
            "Application started.");

        NullGameDiagnosticSink.Instance.Write(diagnosticEvent);
    }
}
