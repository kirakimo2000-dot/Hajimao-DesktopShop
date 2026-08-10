using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Business.Progression;

namespace HajimaoDesktopShop.Desktop.ViewModels.Market;

public sealed class LongTermProgressionViewModel : ObservableObject
{
    private string _titleText = "建立第一家盈利店铺";
    private string _progressText = "等待第一份完整日结";
    private string _guidanceText = "先观察收入、毛利与工资能否形成正向现金流。";

    public string TitleText
    {
        get => _titleText;
        private set => SetProperty(ref _titleText, value);
    }

    public string ProgressText
    {
        get => _progressText;
        private set => SetProperty(ref _progressText, value);
    }

    public string GuidanceText
    {
        get => _guidanceText;
        private set => SetProperty(ref _guidanceText, value);
    }

    public void Update(
        LongTermProgressionSnapshot snapshot,
        IReadOnlyList<StoreCatalogItemSnapshot> storeCatalog)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(storeCatalog);

        var goal = snapshot.CurrentGoal;
        var storeName = StoreName(storeCatalog, goal.TargetStoreId);
        (TitleText, ProgressText, GuidanceText) = goal.Id switch
        {
            ProgressionGoalId.ReachProfitableDay => (
                "建立第一家盈利店铺",
                goal.CurrentValue > 0
                    ? "最近完整日结已盈利"
                    : $"最近完整日结 {FormatMoney(goal.CurrentValue)}",
                "调整整店策略，让收入与毛利稳定覆盖进货、工资和经营成本。"),
            ProgressionGoalId.MakeFirstInvestment => (
                "完成第一笔投资",
                "投资进度 0/1",
                "比较回报、现金压力与经营瓶颈，再选择第一项长期投入。"),
            ProgressionGoalId.PrepareSecondStore => (
                "为第二家店准备资本",
                CapitalProgress(snapshot, goal, storeName),
                "比较当前店铺投资与开店储备，保留足够经营现金。"),
            ProgressionGoalId.OpenSecondStore => (
                "开设第二家店",
                $"{storeName} · 等级与资金已满足",
                "在投资列表执行开店，并为新店留下员工与周转资金。"),
            ProgressionGoalId.StrengthenPortfolio => (
                "强化最弱店铺",
                $"{storeName} · 成长 {goal.CurrentValue}/{goal.TargetValue}",
                "先补齐最弱店铺，再把共享现金投入下一次扩张。"),
            ProgressionGoalId.PrepareThirdStore => (
                "为第三家店准备资本",
                CapitalProgress(snapshot, goal, storeName),
                "让现有店铺稳定盈利，同时积累下一家店的开业储备。"),
            ProgressionGoalId.OpenThirdStore => (
                "开设第三家店",
                $"{storeName} · 等级与资金已满足",
                "在投资列表完成扩张，逐步形成完整街区。"),
            ProgressionGoalId.UnlockCommercialBlock => (
                "解锁完整街区",
                $"完整街区 Lv.{goal.CurrentValue}/{goal.TargetValue}",
                "继续经营多店组合；等级来自真实成交，不依赖倍速或点击收入。"),
            ProgressionGoalId.ImproveWeakestStore => (
                "持续强化店铺组合",
                $"{storeName} · 成长 {goal.CurrentValue}/{goal.TargetValue}",
                "长期比较各店回报，把下一笔资本投向最弱环节。"),
            _ => throw new ArgumentOutOfRangeException(nameof(snapshot))
        };
    }

    private static string CapitalProgress(
        LongTermProgressionSnapshot snapshot,
        ProgressionGoalSnapshot goal,
        string storeName) =>
        $"{storeName} · 现金 {FormatMoney(snapshot.SharedCashCents)}/{FormatMoney(goal.RequiredCashCents)} · Lv.{snapshot.PlayerLevel}/{goal.RequiredPlayerLevel}";

    private static string StoreName(
        IReadOnlyList<StoreCatalogItemSnapshot> storeCatalog,
        string storeId) =>
        storeCatalog.SingleOrDefault(store => string.Equals(
            store.Id,
            storeId,
            StringComparison.Ordinal))?.Name ?? storeId;

    private static string FormatMoney(long cents) =>
        string.Format(CultureInfo.InvariantCulture, "¥{0:N2}", cents / 100m);
}
