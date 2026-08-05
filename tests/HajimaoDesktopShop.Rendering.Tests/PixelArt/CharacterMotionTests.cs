using HajimaoDesktopShop.Rendering.PixelArt;

namespace HajimaoDesktopShop.Rendering.Tests.PixelArt;

public sealed class CharacterMotionTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(7, 7)]
    [InlineData(8, 0)]
    [InlineData(17, 1)]
    public void FrameIndex_WrapsEightFrameLoop(long presentationTick, int expected)
    {
        Assert.Equal(expected, CharacterMotion.FrameIndex(presentationTick, 8, reduceMotion: false));
    }

    [Fact]
    public void FrameIndex_ReducedMotionAlwaysUsesFrameZero()
    {
        Assert.Equal(0, CharacterMotion.FrameIndex(7, 8, reduceMotion: true));
    }

    [Theory]
    [InlineData(0, 0, 40)]
    [InlineData(1, 0, 80)]
    [InlineData(5, 0, 240)]
    [InlineData(6, 0, 40)]
    [InlineData(0, 2, 120)]
    public void HorizontalLoop_MovesAndOffsetsActors(long tick, int actorSeed, int expected)
    {
        Assert.Equal(
            expected,
            CharacterMotion.HorizontalLoop(tick, actorSeed, 40, 240, 40, reduceMotion: false));
    }

    [Fact]
    public void HorizontalLoop_ReducedMotionFreezesAtSeededStart()
    {
        Assert.Equal(120, CharacterMotion.HorizontalLoop(99, 2, 40, 240, 40, reduceMotion: true));
    }

    [Theory]
    [InlineData(0, 40)]
    [InlineData(1, 80)]
    [InlineData(5, 240)]
    [InlineData(6, 200)]
    [InlineData(10, 40)]
    public void PingPong_ReversesAtTrackEnds(long tick, int expected)
    {
        Assert.Equal(
            expected,
            CharacterMotion.PingPong(tick, actorSeed: 0, 40, 240, 40, reduceMotion: false));
    }
}
