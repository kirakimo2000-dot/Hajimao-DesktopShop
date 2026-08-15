using HajimaoDesktopShop.Desktop.Services;
using System.IO;

namespace HajimaoDesktopShop.Desktop.Tests.Services;

public sealed class DesktopGameContentTests
{
    [Fact]
    public void LevelCurve_ReachesTheLevelTenCommercialBlockUnlock()
    {
        Assert.Equal(10, DesktopGameContent.LevelCurve.MaximumLevel);
        Assert.Equal(10, DesktopGameContent.LevelCurve.GetLevel(7_500));
    }

    [Fact]
    public void EconomyConstants_AreNamedAtTheProductionCompositionRoot()
    {
        Assert.True(DesktopGameContent.OpeningCashCents > 0);
        Assert.True(DesktopGameContent.ExperiencePerItemSold > 0);
        Assert.InRange(DesktopGameContent.BaseArrivalBasisPoints, 1, 10_000);
        Assert.Equal([0, 80_000, 120_000], DesktopGameContent.ShopOpeningCostsCents);
        Assert.Equal(10, DesktopGameContent.LevelThresholds.Count);
    }

    [Fact]
    public void CharacterAnimationAssets_AreCopiedWithTheDesktopBuild()
    {
        var characterRoot = Path.Combine(
            AppContext.BaseDirectory,
            "Assets",
            "Content",
            "characters");

        Assert.True(File.Exists(Path.Combine(characterRoot, "rigs", "humanoid.json")));
        Assert.True(File.Exists(Path.Combine(characterRoot, "animations", "humanoid-clips.json")));
        Assert.True(File.Exists(Path.Combine(characterRoot, "skins.json")));
        Assert.True(File.Exists(Path.Combine(characterRoot, "maomao", "parts.png")));
    }

    [Fact]
    public void CombatContentAssets_AreCopiedWithTheDesktopBuild()
    {
        var assets = Path.Combine(AppContext.BaseDirectory, "Assets");

        Assert.True(File.Exists(Path.Combine(assets, "Config", "product-combat.json")));
        Assert.True(File.Exists(Path.Combine(assets, "Content", "customers", "customer-archetypes.json")));
        Assert.True(File.Exists(Path.Combine(assets, "Content", "customers", "customer-spawn-pools.json")));
        Assert.True(File.Exists(Path.Combine(assets, "Content", "characters", "characters.json")));
        Assert.True(File.Exists(Path.Combine(assets, "Content", "interiors", "interiors.json")));
        Assert.True(File.Exists(Path.Combine(assets, "Content", "interiors", "placeholders", "default-shop.png")));
    }
}
