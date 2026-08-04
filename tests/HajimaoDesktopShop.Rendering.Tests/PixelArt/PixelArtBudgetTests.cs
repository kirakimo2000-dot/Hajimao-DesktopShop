using HajimaoDesktopShop.Rendering.PixelArt;

namespace HajimaoDesktopShop.Rendering.Tests.PixelArt;

public sealed class PixelArtBudgetTests
{
    [Fact]
    public void Budget_FixesAtlasFrameAndActorLimits()
    {
        Assert.Equal(256, PixelArtBudget.AtlasWidth);
        Assert.Equal(256, PixelArtBudget.AtlasHeight);
        Assert.Equal(4, PixelArtBudget.CharacterFrameCount);
        Assert.Equal(5, PixelArtBudget.MaximumVisibleCustomers);
        Assert.Equal(256 * 1024, PixelArtBudget.MaximumAtlasBytes);
        Assert.Equal(16 * 1024, PixelArtBudget.MaximumSoundBytes);
    }
}
