using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using HajimaoDesktopShop.Application.Business.Combat;

namespace HajimaoDesktopShop.Desktop.ViewModels.Market;

public sealed class IdleSessionFeedbackViewModel : ObservableObject
{
    private static readonly TimeSpan RecentActivityHoldDuration = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan NewProductUnlockHoldDuration = TimeSpan.FromSeconds(5);
    private readonly Func<DateTimeOffset> _now;
    private readonly long _startingRevenueCents;
    private readonly int _startingServedCustomers;
    private readonly int _startingEscapedCustomers;
    private readonly int _startingDroppedProducts;
    private int _previousServedCustomers;
    private int _previousDroppedProducts;
    private long _previousRevenueCents;
    private string _sessionProfitText = "本次挂机尚未产生收入";
    private string _sessionProfitShortText = "本次 +¥0.00";
    private string _sessionCustomerText = "本次招待 0 位顾客";
    private string _sessionDropText = "本次掉落 0 件商品";
    private string _escapedCustomerText = "漏掉 0 位";
    private string _sessionSummaryShortText = "招待 0 · 掉落 0 · 漏掉 0";
    private string _streetSummaryText = "毛毛正在自动招待顾客";
    private string _goalText = "毛毛正在自动招待顾客";
    private string _recentActivityText = "等待顾客进入店铺";
    private string _storeStatusText = "挂机中 · 顾客会自动进入店铺";
    private DateTimeOffset _recentActivityExpiresAt;
    private string _profitTone = "Neutral";

    public IdleSessionFeedbackViewModel(
        BusinessCombatSnapshot startingSnapshot,
        Func<DateTimeOffset>? now = null)
    {
        ArgumentNullException.ThrowIfNull(startingSnapshot);
        _now = now ?? (() => DateTimeOffset.UtcNow);
        _startingRevenueCents = TotalRevenue(startingSnapshot);
        _startingServedCustomers = TotalServed(startingSnapshot);
        _startingEscapedCustomers = TotalEscaped(startingSnapshot);
        _startingDroppedProducts = TotalDrops(startingSnapshot);
        _previousServedCustomers = _startingServedCustomers;
        _previousDroppedProducts = _startingDroppedProducts;
        _previousRevenueCents = _startingRevenueCents;
    }

    public string SessionProfitText
    {
        get => _sessionProfitText;
        private set => SetProperty(ref _sessionProfitText, value);
    }

    public string SessionProfitShortText
    {
        get => _sessionProfitShortText;
        private set => SetProperty(ref _sessionProfitShortText, value);
    }

    public string SessionCustomerText
    {
        get => _sessionCustomerText;
        private set => SetProperty(ref _sessionCustomerText, value);
    }

    public string SessionDropText
    {
        get => _sessionDropText;
        private set => SetProperty(ref _sessionDropText, value);
    }

    public string EscapedCustomerText
    {
        get => _escapedCustomerText;
        private set => SetProperty(ref _escapedCustomerText, value);
    }

    public string SessionSummaryShortText
    {
        get => _sessionSummaryShortText;
        private set => SetProperty(ref _sessionSummaryShortText, value);
    }

    public string StreetSummaryText
    {
        get => _streetSummaryText;
        private set => SetProperty(ref _streetSummaryText, value);
    }

    public string GoalText
    {
        get => _goalText;
        private set => SetProperty(ref _goalText, value);
    }

    public string RecentActivityText
    {
        get => _recentActivityText;
        private set => SetProperty(ref _recentActivityText, value);
    }

    public string StoreStatusText
    {
        get => _storeStatusText;
        private set => SetProperty(ref _storeStatusText, value);
    }

    public string ProfitTone
    {
        get => _profitTone;
        private set => SetProperty(ref _profitTone, value);
    }

    public void Update(BusinessCombatSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var revenue = Math.Max(0, checked(TotalRevenue(snapshot) - _startingRevenueCents));
        var served = Math.Max(0, checked(TotalServed(snapshot) - _startingServedCustomers));
        var escaped = Math.Max(0, checked(TotalEscaped(snapshot) - _startingEscapedCustomers));
        var drops = Math.Max(0, checked(TotalDrops(snapshot) - _startingDroppedProducts));
        var newlyServed = Math.Max(0, checked(TotalServed(snapshot) - _previousServedCustomers));
        var newlyDropped = Math.Max(0, checked(TotalDrops(snapshot) - _previousDroppedProducts));
        var newlyEarned = Math.Max(0, checked(TotalRevenue(snapshot) - _previousRevenueCents));
        var hasNewProductUnlock = newlyDropped > 0
            && snapshot.Stores
                .SelectMany(store => store.DropRolls)
                .Any(roll => roll.IsNewUnlock);
        _previousServedCustomers = TotalServed(snapshot);
        _previousDroppedProducts = TotalDrops(snapshot);
        _previousRevenueCents = TotalRevenue(snapshot);
        var active = snapshot.Stores.Sum(store => store.State.Customers.Count);

        SessionProfitText = revenue > 0
            ? $"本次挂机收入 +{FormatMoney(revenue)}"
            : "本次挂机尚未产生收入";
        SessionProfitShortText = revenue > 0 ? $"本次 +{FormatMoney(revenue)}" : "本次 +¥0.00";
        SessionCustomerText = $"本次招待 {served} 位顾客";
        SessionDropText = $"本次掉落 {drops} 件商品";
        EscapedCustomerText = $"漏掉 {escaped} 位";
        SessionSummaryShortText = $"招待 {served} · 掉落 {drops} · 漏掉 {escaped}";
        ProfitTone = revenue > 0 ? "Positive" : "Neutral";
        StreetSummaryText = served == 0
            ? "毛毛正在自动招待顾客"
            : $"已招待 {served} 位 · 收入 +{FormatMoney(revenue)} · 掉落 {drops} 件";
        if (newlyServed > 0 || newlyDropped > 0)
        {
            if (hasNewProductUnlock)
            {
                RecentActivityText = $"解锁新商品！招待 +{FormatMoney(newlyEarned)}";
                StoreStatusText = "解锁新商品！打开经营装备";
                _recentActivityExpiresAt = _now().Add(NewProductUnlockHoldDuration);
            }
            else
            {
                RecentActivityText = newlyServed > 0
                    ? $"招待成功 +{FormatMoney(newlyEarned)}" + (newlyDropped > 0 ? $" · 商品 +{newlyDropped}" : string.Empty)
                    : $"商品 +{newlyDropped}";
                StoreStatusText = RecentActivityText;
                _recentActivityExpiresAt = _now().Add(RecentActivityHoldDuration);
            }
        }
        else if (_now() >= _recentActivityExpiresAt)
        {
            RecentActivityText = active > 0
                ? $"毛毛攻击中 · 店内顾客 {active}"
                : "等待顾客进入店铺";
            StoreStatusText = drops > 0
                ? "有商品可用 · 打开经营，装备后提高威力和收益"
                : active > 0
                    ? "自动招待中 · 击败顾客获得现金与商品"
                    : served > 0
                        ? "挂机中 · 击败顾客会随机掉落商品"
                        : "挂机中 · 顾客会自动进入店铺";
        }
        GoalText = served == 0 ? "毛毛正在自动招待顾客" : "去战斗策略调整商品组合";
    }

    private static long TotalRevenue(BusinessCombatSnapshot snapshot) =>
        snapshot.Stores.Sum(store => store.RevenueCents);

    private static int TotalServed(BusinessCombatSnapshot snapshot) =>
        snapshot.Stores.Sum(store => store.ServedCustomers);

    private static int TotalEscaped(BusinessCombatSnapshot snapshot) =>
        snapshot.Stores.Sum(store => store.EscapedCustomers);

    private static int TotalDrops(BusinessCombatSnapshot snapshot) =>
        snapshot.Stores.Sum(store => store.DroppedProducts);

    private static string FormatMoney(long cents) => string.Format(
        CultureInfo.InvariantCulture,
        "¥{0:N2}",
        cents / 100m);
}
