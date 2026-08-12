using HajimaoDesktopShop.Application.Business.Events;

namespace HajimaoDesktopShop.Application.Tests.Business.Events;

public sealed class MarketEventSchedulerTests
{
    [Fact]
    public void AdvanceMinutes_ActivatesOneEligibleEventAndExpiresAtExactMinute()
    {
        var scheduler = new MarketEventScheduler(
            [Event("traffic-rise", 120, 360)],
            seed: 711UL,
            activationIntervalMinutes: 120);

        scheduler.AdvanceMinutes(119);
        Assert.Empty(scheduler.GetSnapshot().ActiveEvents);

        scheduler.AdvanceMinutes(1);
        var active = Assert.Single(scheduler.GetSnapshot().ActiveEvents);
        scheduler.AdvanceMinutes(active.RemainingMinutes - 1);
        Assert.Single(scheduler.GetSnapshot().ActiveEvents);

        scheduler.AdvanceMinutes(1);
        Assert.Empty(scheduler.GetSnapshot().ActiveEvents);
    }

    [Fact]
    public void SameSeedAndInputs_ReplayTheSameTimeline()
    {
        var first = new MarketEventScheduler(CreateDefinitions(), 7411UL, 60);
        var second = new MarketEventScheduler(CreateDefinitions(), 7411UL, 60);

        first.AdvanceMinutes(720);
        second.AdvanceMinutes(720);

        Assert.Equivalent(first.GetSnapshot(), second.GetSnapshot(), strict: true);
    }

    [Fact]
    public void StrategicEvent_NeverExposesMoreThanTwoChoicesOrStacksWithAnotherDecision()
    {
        var definitions = new[]
        {
            Event("strategy-a", 120, 240, choices: [Choice("safe"), Choice("bold")]),
            Event("strategy-b", 120, 240, choices: [Choice("wait"), Choice("act")])
        };
        var scheduler = new MarketEventScheduler(definitions, 7UL, 30);

        scheduler.AdvanceMinutes(240);

        var decisions = scheduler.GetSnapshot().ActiveEvents.Where(active => active.Choices.Count > 0);
        Assert.InRange(decisions.Count(), 0, 1);
        Assert.All(decisions, decision => Assert.Equal(2, decision.Choices.Count));
    }

    [Fact]
    public void Restore_FromSnapshot_ContinuesTheExactTimeline()
    {
        var definitions = CreateDefinitions();
        var original = new MarketEventScheduler(definitions, 7411UL, 60);
        original.AdvanceMinutes(197);
        var restored = new MarketEventScheduler(definitions, original.GetSnapshot(), 60);

        original.AdvanceMinutes(600);
        restored.AdvanceMinutes(600);

        Assert.Equivalent(original.GetSnapshot(), restored.GetSnapshot(), strict: true);
    }

    [Fact]
    public void ActiveEvents_AggregateIntegerModifiersWithoutChangingCash()
    {
        var scheduler = new MarketEventScheduler(
            [
                new MarketEventDefinition(
                    "mixed",
                    MarketEventScope.Global,
                    [],
                    120,
                    240,
                    "混合变化",
                    "需求结构改变。",
                    [
                        new MarketEventEffect(MarketEventEffectKind.Traffic, 150),
                        new MarketEventEffect(MarketEventEffectKind.PurchaseChance, -80),
                        new MarketEventEffect(MarketEventEffectKind.CategoryWeight, 220, "chilled")
                    ],
                    [])
            ],
            711UL,
            60);
        scheduler.AdvanceMinutes(60);

        var modifiers = scheduler.GetModifiers();

        Assert.Equal(1_150, modifiers.TrafficPermille);
        Assert.Equal(920, modifiers.PurchaseChancePermille);
        Assert.Equal(1_220, modifiers.CategoryWeightPermille["chilled"]);
    }

    private static MarketEventDefinition[] CreateDefinitions() =>
        [
            Event("traffic-rise", 180, 360),
            Event("purchase-rise", 120, 300, MarketEventEffectKind.PurchaseChance)
        ];

    private static MarketEventDefinition Event(
        string id,
        int duration,
        int cooldown,
        MarketEventEffectKind kind = MarketEventEffectKind.Traffic,
        IReadOnlyList<MarketEventChoice>? choices = null) =>
        new(
            id,
            MarketEventScope.Global,
            [],
            duration,
            cooldown,
            $"事件 {id}",
            "经营环境发生变化。",
            [new MarketEventEffect(kind, 100)],
            choices ?? []);

    private static MarketEventChoice Choice(string id) =>
        new(id, id, [new MarketEventEffect(MarketEventEffectKind.Traffic, 50)]);
}
