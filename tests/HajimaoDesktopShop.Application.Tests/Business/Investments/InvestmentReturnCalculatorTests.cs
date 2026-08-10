using HajimaoDesktopShop.Application.Business.Investments;

namespace HajimaoDesktopShop.Application.Tests.Business.Investments;

public sealed class InvestmentReturnCalculatorTests
{
    [Fact]
    public void Calculate_UsesCeilingPaybackAndPreservesExpectedBenefit()
    {
        var estimate = InvestmentReturnCalculator.Calculate(
            costCents: 10_000,
            expectedDailyNetBenefitCents: 3_000,
            currentCashCents: 50_000,
            necessaryOutflowCents: 15_000);

        Assert.Equal(3_000, estimate.ExpectedDailyNetBenefitCents);
        Assert.Equal(34, estimate.PaybackDaysTenths);
        Assert.Equal(40_000, estimate.CashAfterInvestmentCents);
        Assert.True(estimate.IsAffordable);
        Assert.Equal(InvestmentCashPressure.Healthy, estimate.CashPressure);
    }

    [Theory]
    [InlineData(35_000, 10_000, 15_000, InvestmentCashPressure.Tight)]
    [InlineData(20_000, 10_000, 15_000, InvestmentCashPressure.Critical)]
    [InlineData(5_000, 10_000, 15_000, InvestmentCashPressure.CannotAfford)]
    [InlineData(50_000, 10_000, 0, InvestmentCashPressure.Unproven)]
    public void Calculate_ClassifiesCashAfterInvestment(
        long currentCashCents,
        long costCents,
        long necessaryOutflowCents,
        InvestmentCashPressure expectedPressure)
    {
        var estimate = InvestmentReturnCalculator.Calculate(
            costCents,
            expectedDailyNetBenefitCents: 1,
            currentCashCents,
            necessaryOutflowCents);

        Assert.Equal(expectedPressure, estimate.CashPressure);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Calculate_DoesNotInventPaybackForNonPositiveBenefit(long benefitCents)
    {
        var estimate = InvestmentReturnCalculator.Calculate(
            costCents: 10_000,
            expectedDailyNetBenefitCents: benefitCents,
            currentCashCents: 50_000,
            necessaryOutflowCents: 10_000);

        Assert.Null(estimate.PaybackDaysTenths);
    }

    [Fact]
    public void Calculate_SaturatesPaybackInsteadOfOverflowing()
    {
        var estimate = InvestmentReturnCalculator.Calculate(
            costCents: long.MaxValue,
            expectedDailyNetBenefitCents: 1,
            currentCashCents: long.MaxValue,
            necessaryOutflowCents: 1);

        Assert.Equal(long.MaxValue, estimate.PaybackDaysTenths);
    }
}
