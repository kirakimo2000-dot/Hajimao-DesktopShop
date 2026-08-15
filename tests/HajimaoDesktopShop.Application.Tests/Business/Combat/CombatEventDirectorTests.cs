using HajimaoDesktopShop.Application.Business.Combat;

namespace HajimaoDesktopShop.Application.Tests.Business.Combat;

public sealed class CombatEventDirectorTests
{
    [Fact]
    public void EventsDependOnActiveRuntimeAndNeverAdvanceWhileClosed()
    {
        var director = new CombatEventDirector(
            ["morning-commute", "office-payday", "night-owls"],
            initialDelaySeconds: 2,
            activeDurationSeconds: 3,
            cooldownSeconds: 2);

        Assert.Empty(director.CurrentTags);
        Assert.Empty(director.Tick(localHour: 8));
        Assert.Equal(["morning-commute"], director.Tick(localHour: 8));
        Assert.Equal(["morning-commute"], director.CurrentTags);

        // Merely reading state represents a closed/not-running period and changes nothing.
        Assert.Equal(["morning-commute"], director.CurrentTags);
    }

    [Fact]
    public void TimeSpecificEventsAreSkippedOutsideTheirRealHourWindow()
    {
        var morning = new CombatEventDirector(
            ["morning-commute", "office-payday"], 1, 2, 1);
        var night = new CombatEventDirector(
            ["night-owls", "office-payday"], 1, 2, 1);

        Assert.Equal(["office-payday"], morning.Tick(localHour: 18));
        Assert.Equal(["night-owls"], night.Tick(localHour: 23));
    }
}
