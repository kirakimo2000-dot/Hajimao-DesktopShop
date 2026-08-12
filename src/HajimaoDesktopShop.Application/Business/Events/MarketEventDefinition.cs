namespace HajimaoDesktopShop.Application.Business.Events;

public enum MarketEventScope
{
    Global,
    Street,
    Store,
    Category,
    Employee
}

public enum MarketEventEffectKind
{
    Traffic,
    PurchaseChance,
    CategoryWeight,
    ProcurementCost,
    EmployeeEfficiency
}

public sealed record MarketEventEffect(
    MarketEventEffectKind Kind,
    int ModifierPermille,
    string? TargetTag = null);

public sealed record MarketEventChoice(
    string Id,
    string Text,
    IReadOnlyList<MarketEventEffect> Effects);

public sealed record MarketEventDefinition
{
    public MarketEventDefinition(
        string id,
        MarketEventScope scope,
        IReadOnlyList<string> eligibilityTags,
        int durationMinutes,
        int cooldownMinutes,
        string headline,
        string effectSummary,
        IReadOnlyList<MarketEventEffect> effects,
        IReadOnlyList<MarketEventChoice> choices)
    {
        if (string.IsNullOrWhiteSpace(id)
            || string.IsNullOrWhiteSpace(headline)
            || string.IsNullOrWhiteSpace(effectSummary))
        {
            throw new ArgumentException("Market event text fields are required.", nameof(id));
        }

        if (durationMinutes <= 0 || cooldownMinutes < durationMinutes)
        {
            throw new ArgumentOutOfRangeException(nameof(durationMinutes));
        }

        if (effects is not { Count: > 0 })
        {
            throw new ArgumentException("At least one market effect is required.", nameof(effects));
        }

        if (choices is null || choices.Count > 2)
        {
            throw new ArgumentException("A market event supports at most two choices.", nameof(choices));
        }

        Id = id.Trim();
        Scope = scope;
        EligibilityTags = Array.AsReadOnly((eligibilityTags ?? []).Select(tag => tag.Trim()).Where(tag => tag.Length > 0).Distinct(StringComparer.Ordinal).ToArray());
        DurationMinutes = durationMinutes;
        CooldownMinutes = cooldownMinutes;
        Headline = headline.Trim();
        EffectSummary = effectSummary.Trim();
        Effects = Array.AsReadOnly(effects.ToArray());
        Choices = Array.AsReadOnly(choices.ToArray());
    }

    public string Id { get; }
    public MarketEventScope Scope { get; }
    public IReadOnlyList<string> EligibilityTags { get; }
    public int DurationMinutes { get; }
    public int CooldownMinutes { get; }
    public string Headline { get; }
    public string EffectSummary { get; }
    public IReadOnlyList<MarketEventEffect> Effects { get; }
    public IReadOnlyList<MarketEventChoice> Choices { get; }
}
