using System.Globalization;
using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Business.Analysis;
using HajimaoDesktopShop.Application.Business.Offline;

namespace HajimaoDesktopShop.Desktop.ViewModels.Market;

public sealed class ReturnBriefingViewModel
{
    public ReturnBriefingViewModel(
        ReturnBriefingSnapshot snapshot,
        IReadOnlyList<StoreCatalogItemSnapshot> stores)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(stores);

        IsVisible = snapshot.IsVisible;
        DurationText = FormatDuration(snapshot.AppliedSeconds);
        ResultText = string.Format(
            CultureInfo.InvariantCulture,
            "现金 {0} · 成交 {1:+#;-#;0} · 净利润 {2}",
            SignedMoney(snapshot.CashDeltaCents),
            snapshot.CompletedSalesDelta,
            SignedMoney(snapshot.NetProfitDeltaCents));
        GuidanceText = FormatGuidance(snapshot, stores);
    }

    public bool IsVisible { get; }

    public string DurationText { get; }

    public string ResultText { get; }

    public string GuidanceText { get; }

    private static string FormatDuration(int appliedSeconds)
    {
        var days = appliedSeconds / 1_440;
        var remainingMinutes = appliedSeconds % 1_440;
        if (days > 0 && remainingMinutes == 0)
        {
            return $"离线 {days} 个经营日";
        }

        var hours = remainingMinutes / 60;
        var minutes = remainingMinutes % 60;
        var duration = hours > 0
            ? $"{hours} 小时 {minutes} 分"
            : $"{minutes} 分";
        return days > 0
            ? $"离线 {days} 个经营日 + {duration}"
            : $"离线推进 {duration}";
    }

    private static string FormatGuidance(
        ReturnBriefingSnapshot snapshot,
        IReadOnlyList<StoreCatalogItemSnapshot> stores)
    {
        var storeName = stores.FirstOrDefault(store => string.Equals(
            store.Id,
            snapshot.AttentionStoreId,
            StringComparison.Ordinal))?.Name;
        return snapshot.Priority switch
        {
            ReturnBriefingPriority.Recovery when storeName is not null =>
                $"优先关注{storeName}：{BottleneckText(snapshot.Bottleneck)}，先恢复盈利再扩张。",
            ReturnBriefingPriority.Reinvest =>
                "现金与组合利润同步增长，可以比较下一笔投资的回报。",
            _ when storeName is not null =>
                $"继续关注{storeName}：{BottleneckText(snapshot.Bottleneck)}，等待下一份完整日结。",
            _ => "经营证据仍不足，先观察下一份完整日结。"
        };
    }

    private static string BottleneckText(StoreBottleneck bottleneck) => bottleneck switch
    {
        StoreBottleneck.Stock => "库存承接不足",
        StoreBottleneck.Checkout => "收银排队",
        StoreBottleneck.Service => "服务效率偏低",
        StoreBottleneck.Cost => "成本压力",
        StoreBottleneck.Demand => "成交转化不足",
        StoreBottleneck.None => "暂无明显瓶颈",
        _ => "数据仍不足"
    };

    private static string SignedMoney(long cents)
    {
        var sign = cents > 0 ? "+" : cents < 0 ? "-" : string.Empty;
        var absoluteCents = cents == long.MinValue ? (decimal)long.MaxValue + 1 : Math.Abs(cents);
        return string.Format(
            CultureInfo.InvariantCulture,
            "{0}¥{1:N2}",
            sign,
            absoluteCents / 100m);
    }
}
