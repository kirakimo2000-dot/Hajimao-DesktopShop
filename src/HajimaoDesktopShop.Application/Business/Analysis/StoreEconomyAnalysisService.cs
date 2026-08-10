using System.Numerics;
using HajimaoDesktopShop.Application.Business.Simulation;

namespace HajimaoDesktopShop.Application.Business.Analysis;

public static class StoreEconomyAnalysisService
{
    public static StoreEconomyAnalysis? Calculate(
        BusinessSimulationSnapshot snapshot,
        string storeId)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (string.IsNullOrWhiteSpace(storeId))
        {
            throw new ArgumentException("Store ID is required.", nameof(storeId));
        }

        var normalizedStoreId = storeId.Trim();
        var store = snapshot.Business.Stores.SingleOrDefault(item =>
            string.Equals(item.Id, normalizedStoreId, StringComparison.Ordinal));
        var operations = snapshot.Stores.SingleOrDefault(item =>
            string.Equals(item.StoreId, normalizedStoreId, StringComparison.Ordinal));
        if (store is null || operations is null)
        {
            return null;
        }

        var report = snapshot.LastCompletedDay?.Stores.SingleOrDefault(item =>
            string.Equals(item.StoreId, normalizedStoreId, StringComparison.Ordinal));
        return Calculate(new StoreEconomyAnalysisInput(
            normalizedStoreId,
            snapshot.Business.CashCents,
            report?.RevenueCents ?? store.RevenueCents,
            report?.GrossProfitCents ?? store.GrossProfitCents,
            report?.WageCostCents ?? store.WageCostCents,
            report?.OperatingCostCents ?? store.OperatingCostCents,
            report?.NetProfitCents ?? store.NetProfitCents,
            report?.Visitors ?? operations.Visitors,
            report?.CompletedSales ?? operations.CompletedSales,
            report?.LostSales ?? operations.LostSales,
            store.Products.Count(product => product.Quantity == 0),
            operations.CheckoutQueueLength,
            operations.ServicePermille,
            IsCompletedDay: report is not null));
    }

    public static StoreEconomyAnalysis Calculate(StoreEconomyAnalysisInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        Validate(input);

        var costOfGoodsCents = checked(input.RevenueCents - input.GrossProfitCents);
        var necessaryOutflowCents = Math.Max(
            0,
            checked(costOfGoodsCents + input.WageCostCents + input.OperatingCostCents));

        return new StoreEconomyAnalysis(
            input.StoreId.Trim(),
            input.IsCompletedDay ? "CompletedDay" : "SinceOpening",
            input.RevenueCents,
            input.GrossProfitCents,
            input.WageCostCents,
            input.OperatingCostCents,
            input.NetProfitCents,
            necessaryOutflowCents,
            RatioBasisPoints(input.GrossProfitCents, input.RevenueCents),
            RatioBasisPoints(input.NetProfitCents, input.RevenueCents),
            CashRunwayTenths(input.SharedCashCents, necessaryOutflowCents),
            input.Visitors,
            input.CompletedSales,
            input.LostSales,
            FindBottleneck(input));
    }

    private static void Validate(StoreEconomyAnalysisInput input)
    {
        if (string.IsNullOrWhiteSpace(input.StoreId))
        {
            throw new ArgumentException("Store ID is required.", nameof(input));
        }

        if (input.SharedCashCents < 0
            || input.RevenueCents < 0
            || input.WageCostCents < 0
            || input.OperatingCostCents < 0
            || input.Visitors < 0
            || input.CompletedSales < 0
            || input.LostSales < 0
            || input.OutOfStockProductCount < 0
            || input.CheckoutQueueLength < 0
            || input.ServicePermille is < 0 or > 2_000)
        {
            throw new ArgumentOutOfRangeException(nameof(input));
        }
    }

    private static int RatioBasisPoints(long numerator, long denominator)
    {
        if (denominator == 0)
        {
            return 0;
        }

        var ratio = (new BigInteger(numerator) * 10_000) / denominator;
        return SaturateToInt(ratio);
    }

    private static int CashRunwayTenths(long cashCents, long outflowCents)
    {
        if (cashCents == 0 || outflowCents <= 0)
        {
            return 0;
        }

        var runway = (new BigInteger(cashCents) * 10) / outflowCents;
        return Math.Max(0, SaturateToInt(runway));
    }

    private static int SaturateToInt(BigInteger value)
    {
        if (value > int.MaxValue)
        {
            return int.MaxValue;
        }

        if (value < int.MinValue)
        {
            return int.MinValue;
        }

        return (int)value;
    }

    private static StoreBottleneck FindBottleneck(StoreEconomyAnalysisInput input)
    {
        if (input.RevenueCents == 0
            && input.Visitors == 0
            && input.CompletedSales == 0
            && input.LostSales == 0)
        {
            return StoreBottleneck.InsufficientData;
        }

        if (input.OutOfStockProductCount > 0 && input.LostSales > 0)
        {
            return StoreBottleneck.Stock;
        }

        if (input.CheckoutQueueLength > 0 && input.LostSales > 0)
        {
            return StoreBottleneck.Checkout;
        }

        if (input.ServicePermille < 700)
        {
            return StoreBottleneck.Service;
        }

        if (input.NetProfitCents < 0
            && checked(input.WageCostCents + input.OperatingCostCents) > 0)
        {
            return StoreBottleneck.Cost;
        }

        if (input.Visitors > 0 && input.CompletedSales == 0)
        {
            return StoreBottleneck.Demand;
        }

        return StoreBottleneck.None;
    }
}
