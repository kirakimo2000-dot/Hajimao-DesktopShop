using HajimaoDesktopShop.Domain.Employees;
using HajimaoDesktopShop.Infrastructure.Configuration;

namespace HajimaoDesktopShop.Infrastructure.Tests.Configuration;

public sealed class JsonPeopleMarketCatalogTests
{
    [Fact]
    public async Task LoadAsync_ShippedCatalog_HasRichValidatedEmployeeAndEventResources()
    {
        var employeesPath = Path.Combine(AppContext.BaseDirectory, "TestData", "employee-profiles.json");
        var eventsPath = Path.Combine(AppContext.BaseDirectory, "TestData", "market-events.json");
        var catalog = new JsonPeopleMarketCatalog(employeesPath, eventsPath);

        var content = await catalog.LoadAsync();

        Assert.Equal(32, content.EmployeeProfiles.Count);
        Assert.Equal(32, content.MarketEvents.Count);
        Assert.Equal(
            Enum.GetValues<EmployeeRole>().Order(),
            content.EmployeeProfiles.SelectMany(profile => profile.AllowedRoles).Distinct().Order());
        Assert.True(content.EmployeeProfiles.Select(profile => profile.RegionTag).Distinct().Count() >= 8);
        Assert.All(content.EmployeeProfiles, profile =>
        {
            Assert.NotEmpty(profile.AllowedRoles);
            Assert.InRange(profile.EfficiencyBiasPermille, 850, 1150);
            Assert.InRange(profile.WageBiasPermille, 850, 1150);
            Assert.StartsWith("employee-", profile.AppearanceKey, StringComparison.Ordinal);
        });
        Assert.All(content.MarketEvents, marketEvent =>
        {
            Assert.NotEmpty(marketEvent.Effects);
            Assert.InRange(marketEvent.Choices.Count, 0, 2);
            Assert.True(marketEvent.DurationMinutes > 0);
            Assert.True(marketEvent.CooldownMinutes >= marketEvent.DurationMinutes);
        });
    }
}
