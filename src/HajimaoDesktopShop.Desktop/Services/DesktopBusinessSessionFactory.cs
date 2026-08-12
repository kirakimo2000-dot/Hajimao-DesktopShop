using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Business.Employees;
using HajimaoDesktopShop.Application.Catalog;
using HajimaoDesktopShop.Application.Persistence;
using HajimaoDesktopShop.Application.Business.StorePortfolio;
using HajimaoDesktopShop.Infrastructure.Simulation;

namespace HajimaoDesktopShop.Desktop.Services;

public static class DesktopBusinessSessionFactory
{
    public static DesktopBusinessSessionStartResult Create(
        IReadOnlyList<ProductDefinition> products,
        GameSaveData? save,
        int seed,
        DateTimeOffset nowUtc,
        StoreContentCatalog? storeContent = null,
        PeopleMarketContent? peopleMarketContent = null,
        StoreOpeningProposal? starterStoreProposal = null)
    {
        ArgumentNullException.ThrowIfNull(products);
        if (products.Count == 0)
        {
            throw new ArgumentException("At least one product is required.", nameof(products));
        }

        var random = new DeterministicRandomSource(seed);
        if (save is null)
        {
            var starterShops = starterStoreProposal is null
                ? DesktopGameContent.Shops
                : CreateSelectedStarterShops(starterStoreProposal, storeContent);
            var newSession = BusinessSession.Create(
                products,
                starterShops,
                DesktopGameContent.LevelCurve,
                DesktopGameContent.StarterStoreId,
                DesktopGameContent.OpeningCashCents,
                DesktopGameContent.CreateStarterAssignments(),
                random,
                DesktopGameContent.SimulationOptions,
                experiencePerItemSold: DesktopGameContent.ExperiencePerItemSold,
                storeContent,
                peopleMarketContent?.EmployeeProfiles,
                peopleMarketContent?.MarketEvents);
            ConfigureStarterShifts(newSession);
            return new DesktopBusinessSessionStartResult(
                newSession,
                IsNewGame: true);
        }

        var restoredSession = BusinessSession.RestoreOrUpgrade(
                products,
                DesktopGameContent.Shops,
                DesktopGameContent.LevelCurve,
                DesktopGameContent.StarterStoreId,
                save,
                DesktopGameContent.CreateStarterAssignments(),
                random,
                DesktopGameContent.SimulationOptions,
                experiencePerItemSold: DesktopGameContent.ExperiencePerItemSold,
                storeContent,
                peopleMarketContent?.EmployeeProfiles,
                peopleMarketContent?.MarketEvents);
        return new DesktopBusinessSessionStartResult(
            restoredSession,
            IsNewGame: false);
    }

    private static IReadOnlyList<Domain.Shops.ShopDefinition> CreateSelectedStarterShops(
        StoreOpeningProposal proposal,
        StoreContentCatalog? storeContent)
    {
        if (storeContent is null)
        {
            throw new ArgumentException(
                "A starter store selection requires loaded store content.",
                nameof(storeContent));
        }

        var brand = storeContent.Brands.SingleOrDefault(item => item.Id == proposal.BrandId);
        var format = storeContent.Formats.SingleOrDefault(item => item.Id == proposal.FormatId);
        if (brand is null
            || format is null
            || brand.FormatId != format.Id
            || proposal.StreetOrdinal != 1
            || proposal.OpeningCostCents != 0)
        {
            throw new ArgumentException(
                "The starter store proposal does not match the loaded content catalog.",
                nameof(proposal));
        }

        var normalized = proposal with
        {
            BrandName = brand.DisplayName,
            FormatName = format.DisplayName
        };
        return DesktopGameContent.CreateStarterShops(normalized);
    }

    private static void ConfigureStarterShifts(BusinessSession session)
    {
        foreach (var employee in session.Simulation.GetSnapshot().Employees.Employees)
        {
            var result = session.Simulation.Employees.SetShift(
                employee.EmployeeId,
                DesktopGameContent.StarterShiftStartMinute,
                DesktopGameContent.StarterShiftEndMinute);
            if (result.Status != EmployeeCommandStatus.Success)
            {
                throw new InvalidOperationException(
                    $"Starter shift configuration failed for '{employee.EmployeeId}': {result.Status}.");
            }
        }
    }
}
