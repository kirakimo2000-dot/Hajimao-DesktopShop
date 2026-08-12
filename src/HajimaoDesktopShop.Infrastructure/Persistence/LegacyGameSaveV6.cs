using HajimaoDesktopShop.Application.Persistence;

namespace HajimaoDesktopShop.Infrastructure.Persistence;

internal sealed record LegacyGameSaveV6(
    int SchemaVersion,
    DateTimeOffset SavedAtUtc,
    ShopSaveData Shop,
    SimulationSaveData Simulation,
    BusinessSaveData? Business,
    BusinessSimulationSaveData? BusinessSimulation,
    InvestmentTrackingSaveData? InvestmentTracking)
{
    public GameSaveData UpgradeToV7()
    {
        var upgradedBusiness = Business is null
            ? null
            : Business with
            {
                Stores = Business.Stores
                    .Select((store, index) => UpgradeStore(store, index))
                    .ToArray()
            };
        return new GameSaveData(
            7,
            SavedAtUtc,
            Shop,
            Simulation,
            upgradedBusiness,
            BusinessSimulation,
            InvestmentTracking);
    }

    private static BusinessStoreSaveData UpgradeStore(BusinessStoreSaveData store, int index)
    {
        var identity = store.StoreId switch
        {
            "corner-store" => ("7-Eleven", "seven-eleven", "convenience"),
            "station-store" => ("FamilyMart", "familymart", "convenience"),
            "community-store" => ("Lawson", "lawson", "convenience"),
            _ => (store.StoreId, "legacy", "legacy")
        };
        return store with
        {
            StoreName = identity.Item1,
            StoreBrandId = identity.Item2,
            StoreFormatId = identity.Item3,
            StreetOrdinal = index + 1
        };
    }
}
