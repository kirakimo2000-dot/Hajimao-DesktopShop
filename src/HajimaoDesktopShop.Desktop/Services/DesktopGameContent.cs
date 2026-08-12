using HajimaoDesktopShop.Application.Business.Simulation;
using HajimaoDesktopShop.Application.Business.StorePortfolio;
using HajimaoDesktopShop.Domain.Economy;
using HajimaoDesktopShop.Domain.Employees;
using HajimaoDesktopShop.Domain.Players;
using HajimaoDesktopShop.Domain.Shops;

namespace HajimaoDesktopShop.Desktop.Services;

public static class DesktopGameContent
{
    public const string StarterStoreId = "corner-store";
    public const long OpeningCashCents = 120_000;
    public const int ExperiencePerItemSold = 1;
    public const int BaseArrivalBasisPoints = 6_000;
    public const int StarterShiftStartMinute = 480;
    public const int StarterShiftEndMinute = 960;

    public static BusinessSimulationOptions SimulationOptions { get; } =
        new(baseArrivalBasisPoints: BaseArrivalBasisPoints);

    public static IReadOnlyList<long> ShopOpeningCostsCents { get; } =
        Array.AsReadOnly(new long[] { 0, 80_000, 120_000 });

    public static IReadOnlyList<long> LevelThresholds { get; } =
        Array.AsReadOnly(new long[] { 0, 40, 120, 300, 650, 1_200, 2_000, 3_200, 5_000, 7_500 });

    public static IReadOnlyList<ShopDefinition> Shops { get; } = Array.AsReadOnly(
    new[]
    {
        new ShopDefinition(
            new ShopId(StarterStoreId),
            new StoreBrandId("seven-eleven"),
            new StoreFormatId("convenience"),
            "7-Eleven",
            1,
            new Money(ShopOpeningCostsCents[0])),
        new ShopDefinition(
            new ShopId("station-store"),
            new StoreBrandId("familymart"),
            new StoreFormatId("convenience"),
            "FamilyMart",
            2,
            new Money(ShopOpeningCostsCents[1])),
        new ShopDefinition(
            new ShopId("community-store"),
            new StoreBrandId("lawson"),
            new StoreFormatId("convenience"),
            "Lawson",
            3,
            new Money(ShopOpeningCostsCents[2]))
    });

    public static LevelCurve LevelCurve { get; } =
        new(LevelThresholds);

    public static IReadOnlyList<ShopDefinition> CreateStarterShops(
        StoreOpeningProposal starterStoreProposal)
    {
        ArgumentNullException.ThrowIfNull(starterStoreProposal);
        return Array.AsReadOnly(
        [
            new ShopDefinition(
                new ShopId(StarterStoreId),
                new StoreBrandId(starterStoreProposal.BrandId),
                new StoreFormatId(starterStoreProposal.FormatId),
                starterStoreProposal.BrandName,
                streetOrdinal: 1,
                Money.Zero),
            .. Shops.Skip(1)
        ]);
    }

    public static IReadOnlyList<StoreEmployeeAssignment> CreateStarterAssignments() =>
        Array.AsReadOnly(new[]
        {
            new StoreEmployeeAssignment(
                StarterStoreId,
                new Employee(
                    new EmployeeId("starter-cashier"),
                    "小葵",
                    EmployeeRole.Cashier,
                    1_000,
                    new Money(400))),
            new StoreEmployeeAssignment(
                StarterStoreId,
                new Employee(
                    new EmployeeId("starter-restocker"),
                    "阿澄",
                    EmployeeRole.Restocker,
                    950,
                    new Money(350)))
        });
}
