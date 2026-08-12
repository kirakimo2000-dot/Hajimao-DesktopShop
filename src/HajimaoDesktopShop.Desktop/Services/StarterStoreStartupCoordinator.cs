using HajimaoDesktopShop.Application.Business.StorePortfolio;
using HajimaoDesktopShop.Application.Catalog;
using HajimaoDesktopShop.Application.Persistence;
using HajimaoDesktopShop.Desktop.ViewModels.Market;

namespace HajimaoDesktopShop.Desktop.Services;

public static class StarterStoreStartupCoordinator
{
    public static StarterStoreStartupResult SelectForStartup(
        GameSaveData? save,
        StoreContentCatalog storeContent,
        int seed,
        Func<StarterStoreChoiceViewModel, bool?> presentChoice)
    {
        ArgumentNullException.ThrowIfNull(storeContent);
        ArgumentNullException.ThrowIfNull(presentChoice);
        if (save is not null)
        {
            return new StarterStoreStartupResult(true, null);
        }

        var proposals = new StoreOpeningProposalService(storeContent)
            .CreateStarterProposals(seed, DesktopGameContent.OpeningCashCents);
        var viewModel = new StarterStoreChoiceViewModel(proposals);
        var accepted = presentChoice(viewModel) == true;
        return accepted && viewModel.SelectedProposal is not null
            ? new StarterStoreStartupResult(true, viewModel.SelectedProposal)
            : new StarterStoreStartupResult(false, null);
    }
}

public sealed record StarterStoreStartupResult(
    bool ShouldContinue,
    StoreOpeningProposal? Proposal);
