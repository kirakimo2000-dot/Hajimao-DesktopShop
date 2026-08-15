using HajimaoDesktopShop.Application.Business.StorePortfolio;
using HajimaoDesktopShop.Application.Catalog;
using HajimaoDesktopShop.Domain.Economy;
using HajimaoDesktopShop.Domain.Shops;

namespace HajimaoDesktopShop.Application.Business.Combat;

public enum CombatStoreExpansionStatus
{
    Success,
    InsufficientFunds,
    NotAvailable,
    UnknownChoice
}

public sealed record CombatStoreExpansionResult(
    CombatStoreExpansionStatus Status,
    StoreOpeningProposal? Proposal = null);

public sealed class CombatStoreExpansionService
{
    private readonly BusinessGameService _game;
    private readonly StoreOpeningProposalService _proposals;

    public CombatStoreExpansionService(BusinessGameService game, StoreContentCatalog catalog)
    {
        _game = game ?? throw new ArgumentNullException(nameof(game));
        _proposals = new StoreOpeningProposalService(
            catalog ?? throw new ArgumentNullException(nameof(catalog)));
    }

    public IReadOnlyList<StoreOpeningProposal> GetProposals()
    {
        var snapshot = _game.GetSnapshot();
        return _proposals.CreateExpansionProposals(
            snapshot.Stores.Count,
            snapshot.CashCents,
            seed: checked(711 + snapshot.Stores.Count * 101),
            snapshot.Stores.Select(store => store.StoreBrandId).ToArray());
    }

    public CombatStoreExpansionResult Open(string brandId)
    {
        if (string.IsNullOrWhiteSpace(brandId))
        {
            return new CombatStoreExpansionResult(CombatStoreExpansionStatus.UnknownChoice);
        }

        var proposal = GetProposals().SingleOrDefault(item =>
            string.Equals(item.BrandId, brandId.Trim(), StringComparison.Ordinal));
        if (proposal is null)
        {
            return new CombatStoreExpansionResult(CombatStoreExpansionStatus.UnknownChoice);
        }

        var result = _game.OpenStore(new ShopDefinition(
            new ShopId(proposal.ProspectiveStoreId),
            new StoreBrandId(proposal.BrandId),
            new StoreFormatId(proposal.FormatId),
            proposal.BrandName,
            proposal.StreetOrdinal,
            new Money(proposal.OpeningCostCents)));
        var status = result.Status switch
        {
            OpenShopStatus.Success => CombatStoreExpansionStatus.Success,
            OpenShopStatus.InsufficientFunds => CombatStoreExpansionStatus.InsufficientFunds,
            OpenShopStatus.AlreadyOpen or OpenShopStatus.LevelLocked =>
                CombatStoreExpansionStatus.NotAvailable,
            _ => CombatStoreExpansionStatus.UnknownChoice
        };
        return new CombatStoreExpansionResult(status, proposal);
    }
}
