using HajimaoDesktopShop.Application.Business.Analysis;
using HajimaoDesktopShop.Domain.Employees;

namespace HajimaoDesktopShop.Application.Business.Investments;

public sealed record InvestmentObservableEffect(
    int ShelfSlotChange = 0,
    int QueueComfortChange = 0,
    int InventoryCapacityChangePermille = 0,
    int AttractionChangeBasisPoints = 0,
    EmployeeRole? AddedRole = null,
    int AddedEfficiencyPermille = 0,
    int StoreCountChange = 0);

public sealed record InvestmentCandidate(
    string Id,
    string StoreId,
    InvestmentKind Kind,
    string TargetId,
    string TargetName,
    InvestmentReturnEstimate Return,
    InvestmentObservableEffect Effect,
    StoreBottleneck AddressedBottleneck,
    InvestmentEstimateCondition EstimateCondition,
    InvestmentAvailability Availability,
    int RequiredPlayerLevel = 0,
    string StoreBrandId = "",
    string StoreFormatId = "",
    int StreetOrdinal = 0,
    long RecommendedReserveCents = 0)
{
    public bool IsExecutable => Availability == InvestmentAvailability.Available;
}
