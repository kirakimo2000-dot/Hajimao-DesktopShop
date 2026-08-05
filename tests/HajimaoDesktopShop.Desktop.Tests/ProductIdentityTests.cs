using System.Reflection;

namespace HajimaoDesktopShop.Desktop.Tests;

public sealed class ProductIdentityTests
{
    [Fact]
    public void ProductIdentity_UsesOfficialDesktopShopBrandAcrossVisibleSurfaces()
    {
        Assert.Equal("Hajimao DesktopShop", ProductIdentity.DisplayName);
        Assert.Equal("HAJIMAO DESKTOPSHOP", ProductIdentity.BrandHeader);
        Assert.Equal("Hajimao DesktopShop · 桌面小店", ProductIdentity.DesktopWindowTitle);
        Assert.Equal("Hajimao DesktopShop · 经营管理", ProductIdentity.ManagementWindowTitle);
        Assert.Equal("Hajimao DesktopShop · 持续经营中", ProductIdentity.TrayTooltip);
        Assert.Equal("退出 Hajimao DesktopShop", ProductIdentity.ExitMenuText);
        Assert.Equal("Hajimao DesktopShop 启动错误", ProductIdentity.StartupErrorTitle);
        Assert.Equal(
            "你已经掌握 Hajimao DesktopShop 的核心经营循环。",
            ProductIdentity.OnboardingCompletionGuidance);
    }

    [Fact]
    public void DesktopAssemblyMetadata_UsesOfficialDisplayName()
    {
        var assembly = typeof(ProductIdentity).Assembly;

        Assert.Equal(
            ProductIdentity.DisplayName,
            assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title);
        Assert.Equal(
            ProductIdentity.DisplayName,
            assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product);
    }
}
