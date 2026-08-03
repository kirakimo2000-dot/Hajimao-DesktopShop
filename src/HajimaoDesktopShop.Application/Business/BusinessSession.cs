using HajimaoDesktopShop.Application.Business.Simulation;
using HajimaoDesktopShop.Application.Catalog;
using HajimaoDesktopShop.Application.Persistence;
using HajimaoDesktopShop.Application.Simulation;
using HajimaoDesktopShop.Domain.Employees;
using HajimaoDesktopShop.Domain.Players;
using HajimaoDesktopShop.Domain.Shops;

namespace HajimaoDesktopShop.Application.Business;

public sealed class BusinessSession
{
    private BusinessSession(BusinessGameService game, BusinessSimulation simulation)
    {
        Game = game;
        Simulation = simulation;
    }

    public BusinessGameService Game { get; }

    public BusinessSimulation Simulation { get; }

    public static BusinessSession Create(
        IEnumerable<ProductDefinition> productDefinitions,
        IEnumerable<ShopDefinition> shopDefinitions,
        LevelCurve levelCurve,
        string starterShopId,
        long openingCashCents,
        IEnumerable<StoreEmployeeAssignment> assignments,
        IStatefulRandomSource random,
        BusinessSimulationOptions? options = null,
        int experiencePerItemSold = 1)
    {
        var products = productDefinitions?.ToArray()
            ?? throw new ArgumentNullException(nameof(productDefinitions));
        var stores = shopDefinitions?.ToArray()
            ?? throw new ArgumentNullException(nameof(shopDefinitions));
        var staff = assignments?.ToArray()
            ?? throw new ArgumentNullException(nameof(assignments));
        var game = new BusinessGameService(
            products,
            stores,
            levelCurve,
            starterShopId,
            openingCashCents,
            experiencePerItemSold);
        return new BusinessSession(
            game,
            new BusinessSimulation(game, staff, random, options));
    }

    public static BusinessSession RestoreOrUpgrade(
        IEnumerable<ProductDefinition> productDefinitions,
        IEnumerable<ShopDefinition> shopDefinitions,
        LevelCurve levelCurve,
        string starterShopId,
        GameSaveData save,
        IEnumerable<StoreEmployeeAssignment> legacyAssignments,
        IStatefulRandomSource random,
        BusinessSimulationOptions? options = null,
        int experiencePerItemSold = 1)
    {
        ArgumentNullException.ThrowIfNull(save);
        ArgumentNullException.ThrowIfNull(random);
        if (save.SchemaVersion != GameSaveSchema.CurrentVersion)
        {
            throw new InvalidDataException(
                $"Business session requires schema {GameSaveSchema.CurrentVersion}, found {save.SchemaVersion}.");
        }

        var products = productDefinitions?.ToArray()
            ?? throw new ArgumentNullException(nameof(productDefinitions));
        var stores = shopDefinitions?.ToArray()
            ?? throw new ArgumentNullException(nameof(shopDefinitions));
        var fallbackStaff = legacyAssignments?.ToArray()
            ?? throw new ArgumentNullException(nameof(legacyAssignments));

        if ((save.Business is null) != (save.BusinessSimulation is null))
        {
            throw new InvalidDataException("Complete business and simulation state must be present together.");
        }

        if (save.Business is not null && save.BusinessSimulation is not null)
        {
            var restoredGame = new BusinessGameService(
                products,
                stores,
                levelCurve,
                save.Business,
                experiencePerItemSold);
            return new BusinessSession(
                restoredGame,
                new BusinessSimulation(restoredGame, save.BusinessSimulation, random, options));
        }

        var upgradedBusiness = UpgradeLegacyBusiness(save, starterShopId);
        var game = new BusinessGameService(
            products,
            stores,
            levelCurve,
            upgradedBusiness,
            experiencePerItemSold);
        var simulationState = UpgradeLegacySimulation(
            game,
            save.Simulation.GameMinute,
            starterShopId,
            fallbackStaff,
            random.State,
            options ?? new BusinessSimulationOptions());
        return new BusinessSession(
            game,
            new BusinessSimulation(game, simulationState, random, options));
    }

    public GameSaveData CaptureSaveData(DateTimeOffset? savedAtUtc = null)
    {
        var business = Game.CaptureSaveData();
        var simulation = Simulation.CaptureSaveData();
        var firstStore = business.Stores.OrderBy(store => store.StoreId, StringComparer.Ordinal).First();
        var completedSales = checked(simulation.Stores.Sum(store => store.CompletedSales));
        return new GameSaveData(
            GameSaveSchema.CurrentVersion,
            savedAtUtc ?? DateTimeOffset.UtcNow,
            new ShopSaveData(
                business.CashCents,
                firstStore.RevenueCents,
                firstStore.StockPurchaseCostCents,
                firstStore.GrossProfitCents,
                Array.AsReadOnly(firstStore.Products
                    .OrderBy(product => product.ProductId, StringComparer.Ordinal)
                    .Select(product => new ProductSaveData(
                        product.ProductId,
                        product.SalePriceCents,
                        product.Quantity))
                    .ToArray())),
            new SimulationSaveData(
                simulation.GameMinute,
                simulation.GameMinute,
                1,
                completedSales,
                [],
                [],
                null,
                [],
                null,
                null),
            business,
            simulation);
    }

    private static BusinessSaveData UpgradeLegacyBusiness(GameSaveData save, string starterShopId)
    {
        if (string.IsNullOrWhiteSpace(starterShopId))
        {
            throw new ArgumentException("Starter shop ID is required.", nameof(starterShopId));
        }

        return new BusinessSaveData(
            0,
            save.Shop.CashCents,
            [
                new BusinessStoreSaveData(
                    starterShopId.Trim(),
                    save.Shop.TotalRevenueCents,
                    save.Shop.TotalStockPurchaseCostCents,
                    save.Shop.TotalGrossProfitCents,
                    0,
                    Array.AsReadOnly(save.Shop.Products
                        .Select(product => new BusinessProductSaveData(
                            product.ProductId,
                            product.SalePriceCents,
                            product.Quantity))
                        .ToArray()))
            ]);
    }

    private static BusinessSimulationSaveData UpgradeLegacySimulation(
        BusinessGameService game,
        long gameMinute,
        string starterShopId,
        IReadOnlyList<StoreEmployeeAssignment> assignments,
        ulong randomState,
        BusinessSimulationOptions options)
    {
        var staff = assignments
            .Where(assignment => string.Equals(
                assignment.StoreId,
                starterShopId,
                StringComparison.Ordinal))
            .OrderBy(assignment => assignment.Employee.Id.Value, StringComparer.Ordinal)
            .Select(assignment =>
            {
                var employee = assignment.Employee;
                var work = employee.CaptureWorkState();
                return new EmployeeAssignmentSaveData(
                    starterShopId,
                    employee.Id.Value,
                    employee.Name,
                    employee.Role,
                    employee.EfficiencyPermille,
                    employee.HourlyWage.Cents,
                    work.WorkedMinutes,
                    work.TotalWagesAccrued.Cents,
                    work.WageRemainderCents);
            })
            .ToArray();
        var store = game.GetSnapshot().Stores.Single(snapshot => snapshot.Id == starterShopId);
        return new BusinessSimulationSaveData(
            gameMinute,
            randomState,
            Array.AsReadOnly(staff),
            [
                new StoreRuntimeSaveData(
                    starterShopId,
                    0,
                    0,
                    0,
                    0,
                    [],
                    null,
                    options.InitialCleanlinessPermille,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    store.RevenueCents,
                    store.GrossProfitCents,
                    store.WageCostCents,
                    0,
                    0)
            ],
            null);
    }
}
