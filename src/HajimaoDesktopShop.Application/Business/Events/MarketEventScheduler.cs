namespace HajimaoDesktopShop.Application.Business.Events;

public sealed record ActiveMarketEventSnapshot(
    string DefinitionId,
    string Headline,
    string EffectSummary,
    int RemainingMinutes,
    IReadOnlyList<MarketEventEffect> Effects,
    IReadOnlyList<MarketEventChoice> Choices);

public sealed record MarketEventSchedulerSnapshot(
    long GameMinute,
    ulong RandomState,
    int MinutesUntilNextActivation,
    IReadOnlyList<ActiveMarketEventSnapshot> ActiveEvents,
    IReadOnlyDictionary<string, int> CooldownMinutes);

public sealed record MarketEventModifiers(
    int TrafficPermille,
    int PurchaseChancePermille,
    int ProcurementCostPermille,
    int EmployeeEfficiencyPermille,
    IReadOnlyDictionary<string, int> CategoryWeightPermille)
{
    public static MarketEventModifiers Neutral { get; } = new(
        1_000,
        1_000,
        1_000,
        1_000,
        new System.Collections.ObjectModel.ReadOnlyDictionary<string, int>(
            new Dictionary<string, int>(StringComparer.Ordinal)));
}

public sealed class MarketEventScheduler
{
    private const ulong SplitMixIncrement = 0x9E3779B97F4A7C15UL;
    private readonly IReadOnlyList<MarketEventDefinition> _definitions;
    private readonly int _activationIntervalMinutes;
    private readonly List<ActiveEvent> _active = [];
    private readonly Dictionary<string, int> _cooldowns = new(StringComparer.Ordinal);
    private ulong _randomState;
    private long _gameMinute;
    private int _minutesUntilNextActivation;

    public MarketEventScheduler(
        IEnumerable<MarketEventDefinition> definitions,
        ulong seed,
        int activationIntervalMinutes = 240)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        if (seed == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(seed));
        }

        if (activationIntervalMinutes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(activationIntervalMinutes));
        }

        _definitions = Array.AsReadOnly(definitions.OrderBy(definition => definition.Id, StringComparer.Ordinal).ToArray());
        if (_definitions.Count == 0)
        {
            throw new ArgumentException("At least one market event is required.", nameof(definitions));
        }

        _randomState = seed;
        _activationIntervalMinutes = activationIntervalMinutes;
        _minutesUntilNextActivation = activationIntervalMinutes;
    }

    public MarketEventScheduler(
        IEnumerable<MarketEventDefinition> definitions,
        MarketEventSchedulerSnapshot restoredState,
        int activationIntervalMinutes = 240)
        : this(definitions, GetRestoreSeed(restoredState), activationIntervalMinutes)
    {
        if (restoredState.GameMinute < 0
            || restoredState.MinutesUntilNextActivation is <= 0
            || restoredState.MinutesUntilNextActivation > activationIntervalMinutes)
        {
            throw new ArgumentOutOfRangeException(nameof(restoredState));
        }

        var byId = _definitions.ToDictionary(definition => definition.Id, StringComparer.Ordinal);
        foreach (var active in restoredState.ActiveEvents)
        {
            if (!byId.TryGetValue(active.DefinitionId, out var definition)
                || active.RemainingMinutes is <= 0
                || active.RemainingMinutes > definition.DurationMinutes)
            {
                throw new InvalidDataException($"Cannot restore market event '{active.DefinitionId}'.");
            }

            _active.Add(new ActiveEvent(definition) { RemainingMinutes = active.RemainingMinutes });
        }

        foreach (var cooldown in restoredState.CooldownMinutes)
        {
            if (!byId.ContainsKey(cooldown.Key) || cooldown.Value <= 0)
            {
                throw new InvalidDataException($"Cannot restore market event cooldown '{cooldown.Key}'.");
            }

            _cooldowns.Add(cooldown.Key, cooldown.Value);
        }

        _gameMinute = restoredState.GameMinute;
        _minutesUntilNextActivation = restoredState.MinutesUntilNextActivation;
    }

    private static ulong GetRestoreSeed(MarketEventSchedulerSnapshot restoredState)
    {
        ArgumentNullException.ThrowIfNull(restoredState);
        return restoredState.RandomState;
    }

    public void AdvanceMinutes(int minutes)
    {
        if (minutes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minutes));
        }

        for (var minute = 0; minute < minutes; minute++)
        {
            _gameMinute = checked(_gameMinute + 1);
            AdvanceDurations();
            _minutesUntilNextActivation--;
            if (_minutesUntilNextActivation == 0)
            {
                TryActivate();
                _minutesUntilNextActivation = _activationIntervalMinutes;
            }
        }
    }

    public MarketEventSchedulerSnapshot GetSnapshot()
    {
        var active = _active
            .OrderBy(item => item.Definition.Id, StringComparer.Ordinal)
            .Select(item => new ActiveMarketEventSnapshot(
                item.Definition.Id,
                item.Definition.Headline,
                item.Definition.EffectSummary,
                item.RemainingMinutes,
                item.Definition.Effects,
                item.Definition.Choices))
            .ToArray();
        return new MarketEventSchedulerSnapshot(
            _gameMinute,
            _randomState,
            _minutesUntilNextActivation,
            Array.AsReadOnly(active),
            new System.Collections.ObjectModel.ReadOnlyDictionary<string, int>(
                new Dictionary<string, int>(_cooldowns, StringComparer.Ordinal)));
    }

    public MarketEventModifiers GetModifiers()
    {
        var traffic = 1_000;
        var purchase = 1_000;
        var procurement = 1_000;
        var employee = 1_000;
        var categories = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var effect in _active.SelectMany(active => active.Definition.Effects))
        {
            switch (effect.Kind)
            {
                case MarketEventEffectKind.Traffic:
                    traffic = checked(traffic + effect.ModifierPermille);
                    break;
                case MarketEventEffectKind.PurchaseChance:
                    purchase = checked(purchase + effect.ModifierPermille);
                    break;
                case MarketEventEffectKind.ProcurementCost:
                    procurement = checked(procurement + effect.ModifierPermille);
                    break;
                case MarketEventEffectKind.EmployeeEfficiency:
                    employee = checked(employee + effect.ModifierPermille);
                    break;
                case MarketEventEffectKind.CategoryWeight:
                    var target = effect.TargetTag
                        ?? throw new InvalidOperationException("Category event effects require a target.");
                    categories[target] = checked(categories.GetValueOrDefault(target, 1_000) + effect.ModifierPermille);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(effect.Kind));
            }
        }

        return new MarketEventModifiers(
            ClampModifier(traffic),
            ClampModifier(purchase),
            ClampModifier(procurement),
            ClampModifier(employee),
            new System.Collections.ObjectModel.ReadOnlyDictionary<string, int>(
                categories.ToDictionary(pair => pair.Key, pair => ClampModifier(pair.Value), StringComparer.Ordinal)));
    }

    private static int ClampModifier(int value) => Math.Clamp(value, 100, 3_000);

    private void AdvanceDurations()
    {
        for (var index = _active.Count - 1; index >= 0; index--)
        {
            var active = _active[index];
            active.RemainingMinutes--;
            if (active.RemainingMinutes <= 0)
            {
                _active.RemoveAt(index);
            }
        }

        foreach (var id in _cooldowns.Keys.ToArray())
        {
            var remaining = _cooldowns[id] - 1;
            if (remaining <= 0)
            {
                _cooldowns.Remove(id);
            }
            else
            {
                _cooldowns[id] = remaining;
            }
        }
    }

    private void TryActivate()
    {
        var hasDecision = _active.Any(active => active.Definition.Choices.Count > 0);
        var eligible = _definitions
            .Where(definition => !_cooldowns.ContainsKey(definition.Id))
            .Where(definition => !_active.Any(active => active.Definition.Id == definition.Id))
            .Where(definition => !hasDecision || definition.Choices.Count == 0)
            .ToArray();
        if (eligible.Length == 0)
        {
            return;
        }

        var definition = eligible[NextInt(eligible.Length)];
        _active.Add(new ActiveEvent(definition));
        _cooldowns[definition.Id] = definition.CooldownMinutes;
    }

    private int NextInt(int exclusiveMaximum)
    {
        _randomState = unchecked(_randomState + SplitMixIncrement);
        var mixed = _randomState;
        mixed = (mixed ^ (mixed >> 30)) * 0xBF58476D1CE4E5B9UL;
        mixed = (mixed ^ (mixed >> 27)) * 0x94D049BB133111EBUL;
        mixed ^= mixed >> 31;
        return checked((int)(mixed % (uint)exclusiveMaximum));
    }

    private sealed class ActiveEvent(MarketEventDefinition definition)
    {
        public MarketEventDefinition Definition { get; } = definition;
        public int RemainingMinutes { get; set; } = definition.DurationMinutes;
    }
}
