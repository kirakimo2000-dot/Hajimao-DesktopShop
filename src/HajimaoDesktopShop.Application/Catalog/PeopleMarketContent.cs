using HajimaoDesktopShop.Application.Business.Events;

namespace HajimaoDesktopShop.Application.Catalog;

public sealed record PeopleMarketContent(
    IReadOnlyList<EmployeeProfileDefinition> EmployeeProfiles,
    IReadOnlyList<MarketEventDefinition> MarketEvents);
