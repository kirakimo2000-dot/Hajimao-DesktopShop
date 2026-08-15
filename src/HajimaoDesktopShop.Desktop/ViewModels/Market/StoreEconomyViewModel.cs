using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using HajimaoDesktopShop.Application.Business.Combat;
using HajimaoDesktopShop.Domain.Collections;

namespace HajimaoDesktopShop.Desktop.ViewModels.Market;

public sealed class StoreEconomyViewModel : ObservableObject
{
    private string _periodText = "开店以来";
    private string _performanceHeadlineText = "毛毛正在等待顾客";
    private string _performanceDetailText = "挂机运行后，这里会累计招待、收入与掉落。";
    private string _reasonHeadlineText = "当前状态：自动战斗已开始";
    private string _reasonDetailText = "商品不会消耗；毛毛会循环使用已装备组合。";

    public string PeriodText { get => _periodText; private set => SetProperty(ref _periodText, value); }
    public string PerformanceHeadlineText { get => _performanceHeadlineText; private set => SetProperty(ref _performanceHeadlineText, value); }
    public string PerformanceDetailText { get => _performanceDetailText; private set => SetProperty(ref _performanceDetailText, value); }
    public string ReasonHeadlineText { get => _reasonHeadlineText; private set => SetProperty(ref _reasonHeadlineText, value); }
    public string ReasonDetailText { get => _reasonDetailText; private set => SetProperty(ref _reasonDetailText, value); }

    public void Update(
        StoreCombatSnapshot store,
        StoreProductLoadout loadout,
        int unlockedProducts)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(loadout);
        PeriodText = "开店以来";
        PerformanceHeadlineText = store.ServedCustomers == 0
            ? "毛毛正在等待首位顾客"
            : $"累计招待 {store.ServedCustomers} 位 · 收入 {FormatMoney(store.RevenueCents)}";
        PerformanceDetailText =
            $"遇见 {store.EncounteredCustomers} 位 · 造成 {store.TotalDamage} 点招待力 · 漏掉 {store.EscapedCustomers} 位 · 掉落 {store.DroppedProducts} 件";
        var currentCustomer = store.State.Customers
            .OrderBy(customer => customer.PositionPermille)
            .FirstOrDefault();
        ReasonHeadlineText = currentCustomer is not null
            ? $"当前顾客：{CustomerName(currentCustomer.ArchetypeId)} · 需求 {currentCustomer.DemandHp}/{Math.Max(currentCustomer.DemandHp, currentCustomer.MaximumDemandHp)}"
            : store.EscapedCustomers > 0
                ? "当前提示：有顾客未被及时招待"
                : "当前状态：自动等待下一位顾客";
        var profile = store.Profile ?? StoreCombatProfilePolicy.Resolve(store.StoreFormatId);
        var customerTrait = currentCustomer is null ? null : CustomerTrait(currentCustomer);
        ReasonDetailText = string.Join(
            " · ",
            new[]
            {
                customerTrait,
                profile.ProfitStyleText,
                profile.RiskText,
                $"装备 {loadout.ProductIds.Count}/{loadout.UnlockedSlots}",
                $"已发现商品 {unlockedProducts}"
            }.Where(text => !string.IsNullOrWhiteSpace(text)));
    }

    private static string CustomerName(string archetypeId) => archetypeId switch
    {
        "early-commuter" => "早班通勤客",
        "student" => "学生",
        "office-worker" => "上班族",
        "tourist" => "游客",
        "senior-neighbor" => "社区长者",
        "delivery-rider" => "外卖骑手",
        "family-shopper" => "家庭顾客",
        "foodie" => "美食爱好者",
        "bargain-hunter" => "折扣猎手",
        "night-owl" => "夜猫子",
        "late-shift-worker" => "夜班员工",
        "collector" => "稀有收藏家",
        "regular" => "普通顾客",
        _ => "顾客"
    };

    private static string CustomerTrait(HajimaoDesktopShop.Domain.Combat.ActiveCustomerState customer)
    {
        var parts = new List<string>();
        if (customer.MovementPermillePerTick >= 85)
        {
            parts.Add("移动较快");
        }
        else if (customer.MovementPermillePerTick <= 48)
        {
            parts.Add("移动缓慢但需求更厚");
        }

        var strongestResistance = customer.ResistancePermille
            .OrderByDescending(pair => pair.Value)
            .FirstOrDefault();
        if (strongestResistance.Value > 0)
        {
            parts.Add($"{TagName(strongestResistance.Key)}类效果较弱");
        }

        return parts.Count == 0 ? "无明显特性" : string.Join("，", parts);
    }

    private static string TagName(string tag) => tag switch
    {
        "liquid" => "液体",
        "sweet" => "甜味",
        "aromatic" => "香气",
        "fruit" => "水果",
        "tea" => "茶饮",
        "splash" => "范围",
        "meal" => "餐食",
        "basic" => "基础",
        "cold" => "冷饮",
        "protein" => "蛋白",
        "dry" => "干货",
        _ => "对应"
    };

    private static string FormatMoney(long cents) =>
        string.Format(CultureInfo.InvariantCulture, "¥{0:N2}", cents / 100m);
}
