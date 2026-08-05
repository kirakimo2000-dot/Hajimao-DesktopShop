using System.IO;
using System.Xml.Linq;

namespace HajimaoDesktopShop.Desktop.Tests.Services;

public sealed class DesktopPlatformContractTests
{
    private static readonly string TestDataDirectory =
        Path.Combine(AppContext.BaseDirectory, "TestData");

    [Fact]
    public void Manifest_UsesPerMonitorV2WithoutElevationOrUiAccess()
    {
        var manifest = XDocument.Load(Path.Combine(TestDataDirectory, "app.manifest"));

        var executionLevel = Assert.Single(
            manifest.Descendants(),
            element => element.Name.LocalName == "requestedExecutionLevel");
        Assert.Equal("asInvoker", executionLevel.Attribute("level")?.Value);
        Assert.Equal("false", executionLevel.Attribute("uiAccess")?.Value);

        var dpiAware = Assert.Single(
            manifest.Descendants(),
            element => element.Name.LocalName == "dpiAware");
        Assert.Contains("true/pm", dpiAware.Value, StringComparison.OrdinalIgnoreCase);

        var dpiAwareness = Assert.Single(
            manifest.Descendants(),
            element => element.Name.LocalName == "dpiAwareness");
        Assert.StartsWith("PerMonitorV2", dpiAwareness.Value.Trim(), StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            manifest.Descendants().Where(element => element.Name.LocalName == "supportedOS"),
            element => string.Equals(
                element.Attribute("Id")?.Value,
                "{8e0f7a12-bfb3-4fe8-b9a5-48fd50a15a9a}",
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DesktopProject_EmbedsApplicationManifest()
    {
        var project = XDocument.Load(Path.Combine(TestDataDirectory, "HajimaoDesktopShop.Desktop.csproj"));

        var applicationManifest = Assert.Single(
            project.Descendants(),
            element => element.Name.LocalName == "ApplicationManifest");
        Assert.Equal("app.manifest", applicationManifest.Value.Trim());

        Assert.DoesNotContain(
            project.Descendants(),
            element => element.Name.LocalName == "ApplicationHighDpiMode");

        var suppressedWarnings = Assert.Single(
            project.Descendants(),
            element => element.Name.LocalName == "NoWarn");
        Assert.Contains("WFO0003", suppressedWarnings.Value, StringComparison.Ordinal);
    }
}
