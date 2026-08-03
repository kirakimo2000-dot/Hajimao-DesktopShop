using HajimaoDesktopShop.Application.Catalog;

namespace HajimaoDesktopShop.Application.Tests.Catalog;

public sealed class ProductDefinitionTests
{
    [Fact]
    public void LegacyConstructor_DefaultsUnlockToLevelOne()
    {
        var definition = new ProductDefinition("water", "矿泉水", 100, 180, 20, "ambient");

        Assert.Equal(1, definition.RequiredPlayerLevel);
    }

    [Fact]
    public void Constructor_RejectsUnlockLevelBelowOne()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ProductDefinition("water", "矿泉水", 100, 180, 20, "ambient", 0));
    }
}
