using System.IO;
using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Business.Combat;
using HajimaoDesktopShop.Desktop.ViewModels;
using HajimaoDesktopShop.Domain.Combat;
using HajimaoDesktopShop.Rendering.Combat;

namespace HajimaoDesktopShop.Desktop.ViewModels.Market;

public sealed class MarketViewModel : ObservableObject
{
    private readonly BusinessSession _session;
    private readonly Func<bool> _reduceMotion;
    private ManagementSection _selectedSection = ManagementSection.Overview;
    private string _selectedStoreId = string.Empty;
    private string _selectedStoreName = string.Empty;
    private string _cashText = "¥0.00";
    private string _playerLevelText = "Lv.1";
    private string _stockWarningText = "累计收益 ¥0.00";
    private string _customerCountText = "顾客 0";
    private bool _isLocked;
    private bool _isClickThrough;
    private int _animationFrame;
    private CombatDesktopShopFrame? _combatDesktopFrame;
    private string? _combatAnimationCue;
    private long _combatActionFrame;

    public MarketViewModel(
        BusinessSession session,
        Func<bool>? reduceMotion = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
        _reduceMotion = reduceMotion ?? (() => false);
        IdleFeedback = new IdleSessionFeedbackViewModel(
            session.Combat?.GetSnapshot()
            ?? throw new InvalidOperationException("Combat content is required by the 0.2 desktop experience."));
        NavigateCommand = new RelayCommand<ManagementSection>(Navigate);
        GoToNextActionCommand = new RelayCommand(GoToNextAction);
        SelectStoreCommand = new RelayCommand<StoreNavigationItemViewModel>(SelectStore);
        ToggleLockCommand = new RelayCommand(ToggleLock);
        ToggleClickThroughCommand = new RelayCommand(ToggleClickThrough);
        DesktopNavigation = new DesktopNavigationViewModel(SelectStoreById);
        Economy = new StoreEconomyViewModel();
        NextAction = new NextActionViewModel();
        Loadout = new StoreLoadoutViewModel(session, () => SelectedStoreId);
        Collection = new ProductCollectionViewModel(session, () => SelectedStoreId, Loadout.Equip);
        Investment = new InvestmentPortfolioViewModel(session, () => SelectedStoreId, Refresh);
        CommercialStreet = new CommercialStreetViewModel();
        EventTicker = new MarketEventTickerViewModel();
        Refresh();
    }

    public ObservableCollection<StoreNavigationItemViewModel> Stores { get; } = [];

    public StoreEconomyViewModel Economy { get; }

    public IdleSessionFeedbackViewModel IdleFeedback { get; }

    public NextActionViewModel NextAction { get; }

    public StoreLoadoutViewModel Loadout { get; }

    public ProductCollectionViewModel Collection { get; }

    public InvestmentPortfolioViewModel Investment { get; }

    public CommercialStreetViewModel CommercialStreet { get; }

    public MarketEventTickerViewModel EventTicker { get; }

    public DesktopNavigationViewModel DesktopNavigation { get; }

    public IRelayCommand<ManagementSection> NavigateCommand { get; }

    public IRelayCommand GoToNextActionCommand { get; }

    public IRelayCommand<StoreNavigationItemViewModel> SelectStoreCommand { get; }

    public IRelayCommand ToggleLockCommand { get; }

    public IRelayCommand ToggleClickThroughCommand { get; }

    public ManagementSection SelectedSection
    {
        get => _selectedSection;
        private set
        {
            if (!SetProperty(ref _selectedSection, value))
            {
                return;
            }

            OnPropertyChanged(nameof(IsOverviewSection));
            OnPropertyChanged(nameof(IsStrategySection));
            OnPropertyChanged(nameof(IsInvestmentSection));
        }
    }

    public bool IsOverviewSection => SelectedSection == ManagementSection.Overview;
    public bool IsStrategySection => SelectedSection == ManagementSection.Strategy;
    public bool IsInvestmentSection => SelectedSection == ManagementSection.Investment;

    public string SelectedStoreId
    {
        get => _selectedStoreId;
        private set => SetProperty(ref _selectedStoreId, value);
    }

    public string SelectedStoreName
    {
        get => _selectedStoreName;
        private set => SetProperty(ref _selectedStoreName, value);
    }

    public string CashText
    {
        get => _cashText;
        private set => SetProperty(ref _cashText, value);
    }

    public string PlayerLevelText
    {
        get => _playerLevelText;
        private set => SetProperty(ref _playerLevelText, value);
    }

    public string StockWarningText
    {
        get => _stockWarningText;
        private set => SetProperty(ref _stockWarningText, value);
    }

    public string CustomerCountText
    {
        get => _customerCountText;
        private set => SetProperty(ref _customerCountText, value);
    }

    public bool IsLocked
    {
        get => _isLocked;
        private set => SetProperty(ref _isLocked, value);
    }

    public bool IsClickThrough
    {
        get => _isClickThrough;
        private set => SetProperty(ref _isClickThrough, value);
    }

    public CombatDesktopShopFrame? CombatDesktopFrame
    {
        get => _combatDesktopFrame;
        private set => SetProperty(ref _combatDesktopFrame, value);
    }

    public void Refresh()
    {
        var business = _session.Game.GetSnapshot();
        var storeCatalog = _session.Game.GetStoreCatalogSnapshot();
        SynchronizeStores(storeCatalog);

        if (string.IsNullOrEmpty(SelectedStoreId))
        {
            var firstOpenStore = Stores.First(store => store.IsOpen);
            SelectedStoreId = firstOpenStore.Id;
            SelectedStoreName = firstOpenStore.Name;
        }
        else
        {
            SelectedStoreName = Stores.Single(store => store.Id == SelectedStoreId).Name;
        }

        DesktopNavigation.Synchronize(Stores, SelectedStoreId);
        var combatService = _session.Combat
            ?? throw new InvalidOperationException("Combat content is required by the 0.2 desktop experience.");
        var combat = combatService.GetSnapshot();

        CashText = FormatMoney(combat.CashCents);
        PlayerLevelText = $"Lv.{business.PlayerLevel}";

        StockWarningText = "尚未开店";
        CustomerCountText = "顾客 —";
        var reduceMotion = _reduceMotion();
        CommercialStreet.Refresh(
            CombatStreetSnapshotFactory.Create(business, combat),
            reduceMotion ? 0 : _animationFrame,
            reduceMotion);
        if (!reduceMotion)
        {
            _animationFrame = _animationFrame == int.MaxValue ? 0 : _animationFrame + 1;
        }
        var combatStore = combat.Stores.SingleOrDefault(item => item.StoreId == SelectedStoreId);
        if (combatStore is null)
        {
            CombatDesktopFrame = null;
        }
        else
        {
            StockWarningText = $"累计收益 {FormatMoney(combatStore.RevenueCents)}";
            CustomerCountText = $"顾客 {combatStore.State.Customers.Count}";
            var relativeBackground = combatService.GetInteriorBackgroundAssetPath(SelectedStoreId)
                .Replace('/', Path.DirectorySeparatorChar);
            var backgroundPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, relativeBackground));
            var combatScene = new CombatShopSceneFrame(
                combatStore,
                backgroundPath,
                combatService.GetProductIconKeys(SelectedStoreId),
                ResolveCombatAnimationFrame(combatStore, reduceMotion),
                reduceMotion);
            CombatDesktopFrame = new CombatDesktopShopFrame(
                combatScene,
                CashText,
                PlayerLevelText,
                StockWarningText,
                CustomerCountText,
                IsLocked,
                IsClickThrough);
        }
        var loadout = combat.Loadouts.SingleOrDefault(item => item.StoreId == SelectedStoreId);
        if (combatStore is not null && loadout is not null)
        {
            Economy.Update(combatStore, loadout, combat.Collection.Count);
        }

        IdleFeedback.Update(combat);

        Loadout.Refresh();
        Collection.Refresh();
        Investment.Refresh();
        EventTicker.Update(combat.ActiveEventTags ?? []);
        NextAction.Update(
            combat,
            SelectedStoreId,
            Investment.Candidates.Any(candidate => candidate.InvestCommand.CanExecute(null)));
    }

    public void RestoreDesktopState(bool isLocked)
    {
        IsLocked = isLocked;
        IsClickThrough = false;
        Refresh();
    }

    private void Navigate(ManagementSection section) => SelectedSection = section;

    private void GoToNextAction() => Navigate(NextAction.SuggestedSection);

    private void ToggleLock()
    {
        IsLocked = !IsLocked;
        Refresh();
    }

    private void ToggleClickThrough()
    {
        IsClickThrough = !IsClickThrough;
        Refresh();
    }

    private void SelectStore(StoreNavigationItemViewModel? store)
    {
        if (store is null)
        {
            return;
        }

        SelectedStoreId = store.Id;
        SelectedStoreName = store.Name;
        Refresh();
    }

    private void SelectStoreById(string storeId) =>
        SelectStore(Stores.Single(store => store.Id == storeId));

    private long ResolveCombatAnimationFrame(
        StoreCombatSnapshot store,
        bool reduceMotion)
    {
        if (reduceMotion)
        {
            return 0;
        }

        var cue = store.Events.LastOrDefault() switch
        {
            ProductThrownEvent thrown => $"throw:{thrown.ProjectileEntityId}",
            ProductHitEvent hit => $"hit:{hit.ProjectileEntityId}:{hit.CustomerEntityId}",
            CustomerServedEvent served => $"served:{served.CustomerEntityId}",
            CustomerSpawnedEvent spawned => $"spawn:{spawned.CustomerEntityId}",
            _ => null
        };
        if (cue is null)
        {
            _combatAnimationCue = null;
            _combatActionFrame = 0;
            return _animationFrame;
        }

        if (!string.Equals(cue, _combatAnimationCue, StringComparison.Ordinal))
        {
            _combatAnimationCue = cue;
            _combatActionFrame = 0;
        }
        else
        {
            _combatActionFrame = Math.Min(23, _combatActionFrame + 1);
        }

        return _combatActionFrame;
    }

    private void SynchronizeStores(IReadOnlyList<StoreCatalogItemSnapshot> snapshots)
    {
        foreach (var snapshot in snapshots)
        {
            var existing = Stores.SingleOrDefault(store => store.Id == snapshot.Id);
            if (existing is null)
            {
                Stores.Add(new StoreNavigationItemViewModel(snapshot));
            }
            else
            {
                existing.Update(snapshot);
            }
        }
    }

    private static string FormatMoney(long cents) =>
        string.Format(CultureInfo.InvariantCulture, "¥{0:N2}", cents / 100m);

}
