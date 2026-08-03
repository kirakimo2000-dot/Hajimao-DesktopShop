using HajimaoDesktopShop.Domain.Players;

namespace HajimaoDesktopShop.Domain.Tests.Players;

public sealed class PlayerProfileTests
{
    [Fact]
    public void NewPlayer_StartsAtLevelOne()
    {
        var player = new PlayerProfile(new LevelCurve([0, 100, 300, 600]));

        Assert.Equal(0, player.TotalExperience);
        Assert.Equal(1, player.Level);
    }

    [Fact]
    public void GainExperience_CrossesMultipleConfiguredLevels()
    {
        var player = new PlayerProfile(new LevelCurve([0, 100, 300, 600]));

        player.GainExperience(350);

        Assert.Equal(350, player.TotalExperience);
        Assert.Equal(3, player.Level);
        Assert.Equal(50, player.ExperienceIntoLevel);
        Assert.Equal(300, player.ExperienceRequiredForNextLevel);
    }

    [Fact]
    public void Restore_DerivesLevelFromTotalExperience()
    {
        var player = new PlayerProfile(new LevelCurve([0, 100, 300]), totalExperience: 450);

        Assert.Equal(3, player.Level);
        Assert.Null(player.ExperienceRequiredForNextLevel);
    }

    [Fact]
    public void GainExperience_RejectsNegativeAndOverflowWithoutMutation()
    {
        var player = new PlayerProfile(new LevelCurve([0, 100]), long.MaxValue - 5);

        Assert.Throws<ArgumentOutOfRangeException>(() => player.GainExperience(-1));
        Assert.Throws<OverflowException>(() => player.GainExperience(6));
        Assert.Equal(long.MaxValue - 5, player.TotalExperience);
    }

    [Theory]
    [InlineData(new long[0])]
    [InlineData(new long[] { 10 })]
    [InlineData(new long[] { 0, 100, 100 })]
    [InlineData(new long[] { 0, 200, 100 })]
    public void LevelCurve_RejectsInvalidThresholds(long[] thresholds)
    {
        Assert.Throws<ArgumentException>(() => new LevelCurve(thresholds));
    }
}
