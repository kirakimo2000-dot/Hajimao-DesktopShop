using HajimaoDesktopShop.Domain.Streets;

namespace HajimaoDesktopShop.Application.Business.Street;

public sealed record CommercialStreetSnapshot(
    CommercialStreetTier Tier,
    StreetWeather Weather,
    int SharedTrafficBasisPoints,
    int VisiblePedestrians,
    int VisibleVehicles,
    IReadOnlyList<CommercialStreetStoreSnapshot> Stores,
    int VisitorOpportunities = 1);

public sealed record CommercialStreetStoreSnapshot(
    string StoreId,
    string StoreName,
    int AttractionBasisPoints,
    int TrafficShareBasisPoints,
    string FacadeStyleKey = "facade-convenience-a");

public sealed record StreetStoreDemand(
    string StoreId,
    string StoreName,
    int AttractionBasisPoints,
    string FacadeStyleKey = "facade-convenience-a");
