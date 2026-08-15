using HajimaoDesktopShop.Rendering.PixelArt;

namespace HajimaoDesktopShop.Rendering.Tests.PixelArt;

public sealed class CharacterAnimationTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(7, 7)]
    [InlineData(8, 0)]
    [InlineData(16, 0)]
    [InlineData(23, 7)]
    [InlineData(24, 0)]
    [InlineData(-1, 7)]
    public void CelIndex_PlaysEveryStoredPoseWithinTheTwentyFourFrameTimeline(
        long presentationFrame,
        int expected)
    {
        Assert.Equal(expected, CharacterAnimation.CelIndex(presentationFrame, reduceMotion: false));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    [InlineData(23)]
    [InlineData(24)]
    public void CelIndex_ReducedMotionAlwaysUsesSeedCel(long presentationFrame)
    {
        Assert.Equal(0, CharacterAnimation.CelIndex(presentationFrame, reduceMotion: true));
    }
}
