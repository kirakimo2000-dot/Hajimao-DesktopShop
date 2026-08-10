using System.Numerics;

namespace HajimaoDesktopShop.Application.Business.Investments;

public static class InvestmentReturnCalculator
{
    public static InvestmentReturnEstimate Calculate(
        long costCents,
        long expectedDailyNetBenefitCents,
        long currentCashCents,
        long necessaryOutflowCents)
    {
        if (costCents < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(costCents));
        }

        if (currentCashCents < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(currentCashCents));
        }

        if (necessaryOutflowCents < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(necessaryOutflowCents));
        }

        var cashAfterInvestment = currentCashCents - costCents;
        var isAffordable = cashAfterInvestment >= 0;
        long? payback = expectedDailyNetBenefitCents > 0
            ? SaturatingCeilingDivide(
                new BigInteger(costCents) * 10,
                expectedDailyNetBenefitCents)
            : null;

        return new InvestmentReturnEstimate(
            costCents,
            expectedDailyNetBenefitCents,
            payback,
            cashAfterInvestment,
            ClassifyPressure(isAffordable, cashAfterInvestment, necessaryOutflowCents),
            isAffordable);
    }

    private static InvestmentCashPressure ClassifyPressure(
        bool isAffordable,
        long cashAfterInvestmentCents,
        long necessaryOutflowCents)
    {
        if (!isAffordable)
        {
            return InvestmentCashPressure.CannotAfford;
        }

        if (necessaryOutflowCents == 0)
        {
            return InvestmentCashPressure.Unproven;
        }

        if (cashAfterInvestmentCents < necessaryOutflowCents)
        {
            return InvestmentCashPressure.Critical;
        }

        return new BigInteger(cashAfterInvestmentCents) < new BigInteger(necessaryOutflowCents) * 2
            ? InvestmentCashPressure.Tight
            : InvestmentCashPressure.Healthy;
    }

    private static long SaturatingCeilingDivide(BigInteger numerator, long denominator)
    {
        var result = (numerator + denominator - 1) / denominator;
        return result > long.MaxValue ? long.MaxValue : (long)result;
    }
}
