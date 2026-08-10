using HajimaoDesktopShop.Application.Business.Analysis;
using HajimaoDesktopShop.Application.Business.Strategy;

namespace HajimaoDesktopShop.Application.Business.Progression;

public static class StoreRecoveryAdvisor
{
    private const int CriticalCashRunwayTenthsOfDay = 10;

    public static StoreRecoveryRecommendation? Create(StoreEconomyAnalysis analysis)
    {
        ArgumentNullException.ThrowIfNull(analysis);

        var evidenceCode = analysis.NetProfitCents < 0
            ? "negative-profit"
            : analysis.NecessaryOutflowCents > 0
                && analysis.CashRunwayTenthsOfDay <= CriticalCashRunwayTenthsOfDay
                    ? "critical-cash-runway"
                    : null;
        if (evidenceCode is null)
        {
            return null;
        }

        var pricing = analysis.Bottleneck == StoreBottleneck.Cost
            ? StorePricingPreset.Balanced
            : StorePricingPreset.HighTurnover;
        return new StoreRecoveryRecommendation(
            analysis.StoreId,
            analysis.Bottleneck,
            pricing,
            StoreStockingPreset.Lean,
            evidenceCode);
    }
}
