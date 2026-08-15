using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using HajimaoDesktopShop.Application.Business.Combat;

namespace HajimaoDesktopShop.Desktop.ViewModels.Market;

public sealed class IdleSessionFeedbackViewModel : ObservableObject
{
    private readonly long _startingRevenueCents;
    private readonly int _startingServedCustomers;
    private readonly int _startingEscapedCustomers;
    private readonly int _startingDroppedProducts;
    private int _previousServedCustomers;
    private int _previousDroppedProducts;
    private string _sessionProfitText = "本次挂机尚未产生收入";
    private string _sessionProfitShortText = "本次 +¥0.00";
    private string _sessionCustomerText = "本次招待 0 位顾客";
    private string _sessionDropText = "本次掉落 0 件商品";
    private string _escapedCustomerText = "漏掉 0 位";
    private string _streetSummaryText = "毛毛正在自动招待顾客";
    private string _goalText = "毛毛正在自动招待顾客";
    private string _recentActivityText = "等待顾客进入店铺";
    private string _profitTone = "Neutral";

    public IdleSessionFeedbackViewModel(BusinessCombatSnapshot startingSnapshot)
    {
        ArgumentNullException.ThrowIfNull(startingSnapshot);
        _startingRevenueCents = TotalRevenue(startingSnapshot);
        _startingServedCustomers = TotalServed(startingSnapshot);
        _startingEscapedCustomers = TotalEscaped(startingSnapshot);
        _startingDroppedProducts = TotalDrops(startingSnapshot);
        _previousServedCustomers = _startingServedCustomers;
        _previousDroppedProducts = _startingDroppedProducts;
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
        _previousServedCustomers = TotalServed(snapshot);
        _previousDroppedProducts = TotalDrops(snapshot);
        var active = snapshot.Stores.Sum(store => store.State.Customers.Count);

        SessionProfitText = revenue > 0
            ? $"本次挂机收入 +{FormatMoney(revenue)}"
            : "本次挂机尚未产生收入";
        SessionProfitShortText = revenue > 0 ? $"本次 +{FormatMoney(revenue)}" : "本次 +¥0.00";
        SessionCustomerText = $"本次招待 {served} 位顾客";
        SessionDropText = $"本次掉落 {drops} 件商品";
        EscapedCustomerText = $"漏掉 {escaped} 位";
        ProfitTone = revenue > 0 ? "Positive" : "Neutral";
        StreetSummaryText = served == 0
            ? "毛毛正在自动招待顾客"
            : $"已招待 {served} 位 · 收入 +{FormatMoney(revenue)} · 掉落 {drops} 件";
        RecentActivityText = newlyServed > 0
            ? $"刚刚完成 {newlyServed} 次招待" + (newlyDropped > 0 ? $" · 获得 {newlyDropped} 件商品" : string.Empty)
            : active > 0
                ? $"毛毛攻击中 · 店内顾客 {active}"
                : "等待顾客进入店铺";
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
