using HajimaoDesktopShop.Application.Business.Analysis;

namespace HajimaoDesktopShop.Application.Business.Offline;

public sealed record ReturnBriefingSnapshot(
    bool IsVisible,
    int AppliedSeconds,
    long CashDeltaCents,
    int CompletedSalesDelta,
    long NetProfitDeltaCents,
    string? AttentionStoreId,
    StoreBottleneck Bottleneck,
    ReturnBriefingPriority Priority);
