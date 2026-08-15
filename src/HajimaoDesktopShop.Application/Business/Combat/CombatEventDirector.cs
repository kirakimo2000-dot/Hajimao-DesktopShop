namespace HajimaoDesktopShop.Application.Business.Combat;

public sealed class CombatEventDirector
{
    private readonly string[] _eventTags;
    private readonly int _activeDurationSeconds;
    private readonly int _cooldownSeconds;
    private int _secondsUntilNextEvent;
    private int _activeSecondsRemaining;
    private int _nextEventIndex;
    private string? _activeTag;

    public CombatEventDirector(
        IEnumerable<string> eventTags,
        int initialDelaySeconds = 20,
        int activeDurationSeconds = 90,
        int cooldownSeconds = 120)
    {
        ArgumentNullException.ThrowIfNull(eventTags);
        if (initialDelaySeconds <= 0 || activeDurationSeconds <= 0 || cooldownSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialDelaySeconds));
        }

        _eventTags = eventTags
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(tag => tag, StringComparer.Ordinal)
            .ToArray();
        _secondsUntilNextEvent = initialDelaySeconds;
        _activeDurationSeconds = activeDurationSeconds;
        _cooldownSeconds = cooldownSeconds;
    }

    public IReadOnlyList<string> CurrentTags =>
        _activeTag is null ? [] : [_activeTag];

    public IReadOnlyList<string> Tick(int localHour)
    {
        if (localHour is < 0 or > 23)
        {
            throw new ArgumentOutOfRangeException(nameof(localHour));
        }

        if (_activeTag is not null)
        {
            _activeSecondsRemaining--;
            if (_activeSecondsRemaining <= 0)
            {
                _activeTag = null;
                _secondsUntilNextEvent = _cooldownSeconds;
            }

            return CurrentTags;
        }

        _secondsUntilNextEvent--;
        if (_secondsUntilNextEvent > 0 || _eventTags.Length == 0)
        {
            return [];
        }

        _activeTag = SelectApplicable(localHour);
        _activeSecondsRemaining = _activeDurationSeconds;
        return CurrentTags;
    }

    private string SelectApplicable(int localHour)
    {
        for (var offset = 0; offset < _eventTags.Length; offset++)
        {
            var index = (_nextEventIndex + offset) % _eventTags.Length;
            var candidate = _eventTags[index];
            if (!AppliesAtHour(candidate, localHour))
            {
                continue;
            }

            _nextEventIndex = (index + 1) % _eventTags.Length;
            return candidate;
        }

        var fallback = _eventTags[_nextEventIndex];
        _nextEventIndex = (_nextEventIndex + 1) % _eventTags.Length;
        return fallback;
    }

    private static bool AppliesAtHour(string tag, int hour) => tag switch
    {
        "morning-commute" => hour is >= 5 and < 9,
        "rainy-evening" => hour is >= 17 and < 22,
        "night-owls" => hour is >= 22 or < 5,
        "school-holiday" or "senior-club-visit" => hour is >= 9 and < 17,
        _ => true
    };
}
