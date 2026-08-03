namespace HajimaoDesktopShop.Domain.Players;

public sealed class PlayerProfile
{
    private readonly LevelCurve _levelCurve;

    public PlayerProfile(LevelCurve levelCurve, long totalExperience = 0)
    {
        ArgumentNullException.ThrowIfNull(levelCurve);
        if (totalExperience < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalExperience));
        }

        _levelCurve = levelCurve;
        TotalExperience = totalExperience;
    }

    public long TotalExperience { get; private set; }

    public int Level => _levelCurve.GetLevel(TotalExperience);

    public long ExperienceIntoLevel =>
        TotalExperience - _levelCurve.GetTotalExperienceThreshold(Level);

    public long? ExperienceRequiredForNextLevel =>
        Level == _levelCurve.MaximumLevel
            ? null
            : _levelCurve.GetTotalExperienceThreshold(Level + 1)
                - _levelCurve.GetTotalExperienceThreshold(Level);

    public void GainExperience(long experience)
    {
        if (experience < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(experience));
        }

        var updated = checked(TotalExperience + experience);
        TotalExperience = updated;
    }
}
