namespace HajimaoDesktopShop.Domain.Players;

public sealed class LevelCurve
{
    private readonly long[] _totalExperienceThresholds;

    public LevelCurve(IEnumerable<long> totalExperienceThresholds)
    {
        ArgumentNullException.ThrowIfNull(totalExperienceThresholds);
        _totalExperienceThresholds = totalExperienceThresholds.ToArray();
        if (_totalExperienceThresholds.Length == 0 || _totalExperienceThresholds[0] != 0)
        {
            throw new ArgumentException(
                "A level curve must start with a zero threshold for level one.",
                nameof(totalExperienceThresholds));
        }

        for (var index = 1; index < _totalExperienceThresholds.Length; index++)
        {
            if (_totalExperienceThresholds[index] <= _totalExperienceThresholds[index - 1])
            {
                throw new ArgumentException(
                    "Level thresholds must be strictly increasing.",
                    nameof(totalExperienceThresholds));
            }
        }
    }

    public int MaximumLevel => _totalExperienceThresholds.Length;

    public int GetLevel(long totalExperience)
    {
        if (totalExperience < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalExperience));
        }

        var level = 1;
        while (level < MaximumLevel && totalExperience >= _totalExperienceThresholds[level])
        {
            level++;
        }

        return level;
    }

    public long GetTotalExperienceThreshold(int level)
    {
        if (level < 1 || level > MaximumLevel)
        {
            throw new ArgumentOutOfRangeException(nameof(level));
        }

        return _totalExperienceThresholds[level - 1];
    }
}
