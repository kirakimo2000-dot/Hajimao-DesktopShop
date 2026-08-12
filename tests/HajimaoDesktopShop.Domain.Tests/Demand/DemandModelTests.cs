using HajimaoDesktopShop.Domain.Demand;

namespace HajimaoDesktopShop.Domain.Tests.Demand;

public sealed class DemandModelTests
{
    [Fact]
    public void Arrival_NeutralStoreAtMorningRush_ExplainsTimeBonus()
    {
        var result = DemandModel.CalculateArrival(CreateContext(minuteOfDay: 9 * 60));

        Assert.Equal(3_800, result.FinalBasisPoints);
        Assert.Equal(800, result.TimeAdjustmentBasisPoints);
        Assert.Equal(0, result.PriceAdjustmentBasisPoints);
        Assert.Equal(0, result.ServiceAdjustmentBasisPoints);
        Assert.Equal(0, result.QueueAdjustmentBasisPoints);
        Assert.Equal(0, result.CleanlinessAdjustmentBasisPoints);
    }

    [Fact]
    public void Arrival_EachOperatingFactorChangesTheExplainableScore()
    {
        var expensive = DemandModel.CalculateArrival(CreateContext(priceIndexBasisPoints: 12_000));
        var goodService = DemandModel.CalculateArrival(CreateContext(servicePermille: 1_300));
        var queue = DemandModel.CalculateArrival(CreateContext(queueLength: 3));
        var dirty = DemandModel.CalculateArrival(CreateContext(cleanlinessPermille: 600));

        Assert.Equal(-1_000, expensive.PriceAdjustmentBasisPoints);
        Assert.Equal(600, goodService.ServiceAdjustmentBasisPoints);
        Assert.Equal(-1_050, queue.QueueAdjustmentBasisPoints);
        Assert.Equal(-800, dirty.CleanlinessAdjustmentBasisPoints);
    }

    [Fact]
    public void GrowthAndPromotion_AddSeparateExplainableDemandAdjustments()
    {
        var result = DemandModel.CalculateArrival(new DemandContext(
            3_000,
            10_000,
            1_000,
            0,
            1_000,
            900,
            attractionBasisPoints: 650,
            promotionBasisPoints: 1_200));

        Assert.Equal(650, result.AttractionAdjustmentBasisPoints);
        Assert.Equal(1_200, result.PromotionAdjustmentBasisPoints);
        Assert.Equal(4_850, result.FinalBasisPoints);
    }

    [Theory]
    [InlineData(120, -1_500)]
    [InlineData(480, 800)]
    [InlineData(720, 700)]
    [InlineData(1_080, 1_000)]
    [InlineData(900, 0)]
    public void Arrival_TimeOfDayUsesStableWindows(int minuteOfDay, int expectedAdjustment)
    {
        var result = DemandModel.CalculateArrival(CreateContext(minuteOfDay: minuteOfDay));

        Assert.Equal(expectedAdjustment, result.TimeAdjustmentBasisPoints);
    }

    [Fact]
    public void Purchase_HigherPriceHurtsWhileServiceAndCleanlinessHelp()
    {
        var neutral = DemandModel.CalculatePurchase(CreateContext());
        var expensive = DemandModel.CalculatePurchase(CreateContext(priceIndexBasisPoints: 12_000));
        var caredFor = DemandModel.CalculatePurchase(CreateContext(
            servicePermille: 1_300,
            cleanlinessPermille: 1_300));

        Assert.True(expensive.FinalBasisPoints < neutral.FinalBasisPoints);
        Assert.True(caredFor.FinalBasisPoints > neutral.FinalBasisPoints);
        Assert.Equal(-4_000, expensive.PriceAdjustmentBasisPoints);
    }

    [Fact]
    public void Scores_AreClampedToProbabilityBasisPointRange()
    {
        var minimum = DemandModel.CalculatePurchase(CreateContext(
            baseBasisPoints: 0,
            priceIndexBasisPoints: 30_000,
            servicePermille: 0,
            queueLength: int.MaxValue,
            cleanlinessPermille: 0,
            minuteOfDay: 60));
        var maximum = DemandModel.CalculateArrival(CreateContext(
            baseBasisPoints: 10_000,
            priceIndexBasisPoints: 1,
            servicePermille: 2_000,
            cleanlinessPermille: 2_000,
            minuteOfDay: 1_080));

        Assert.Equal(0, minimum.FinalBasisPoints);
        Assert.Equal(10_000, maximum.FinalBasisPoints);
    }

    [Fact]
    public void Context_RejectsValuesOutsideDocumentedRanges()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateContext(baseBasisPoints: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateContext(baseBasisPoints: 10_001));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateContext(priceIndexBasisPoints: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateContext(priceIndexBasisPoints: 30_001));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateContext(servicePermille: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateContext(cleanlinessPermille: 2_001));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateContext(queueLength: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateContext(minuteOfDay: 1_440));
    }

    [Fact]
    public void Arrival_DiscountFormatGetsMoreBaseDemandButLargerHighPricePenalty()
    {
        var neutral = DemandModel.CalculateArrival(CreateContext(priceIndexBasisPoints: 12_000));
        var discount = DemandModel.CalculateArrival(new DemandContext(
            3_000,
            12_000,
            1_000,
            0,
            1_000,
            900,
            sensitivity: new DemandSensitivity(
                BaseDemandPermille: 1_220,
                PricePermille: 1_450,
                ServicePermille: 800,
                QueuePermille: 1_250,
                CleanlinessPermille: 800),
            timeCurve: DemandTimeCurve.AllDayVolume));

        Assert.Equal(3_660, discount.BaseBasisPoints);
        Assert.True(discount.PriceAdjustmentBasisPoints < neutral.PriceAdjustmentBasisPoints);
    }

    [Fact]
    public void Arrival_PremiumFormatAmplifiesServiceAndCleanlinessEffects()
    {
        var neutral = DemandModel.CalculateArrival(CreateContext(
            servicePermille: 800,
            cleanlinessPermille: 800));
        var premium = DemandModel.CalculateArrival(new DemandContext(
            3_000,
            10_000,
            800,
            0,
            800,
            900,
            sensitivity: new DemandSensitivity(
                BaseDemandPermille: 780,
                PricePermille: 600,
                ServicePermille: 1_500,
                QueuePermille: 900,
                CleanlinessPermille: 1_500),
            timeCurve: DemandTimeCurve.AfternoonSelect));

        Assert.True(premium.ServiceAdjustmentBasisPoints < neutral.ServiceAdjustmentBasisPoints);
        Assert.True(premium.CleanlinessAdjustmentBasisPoints < neutral.CleanlinessAdjustmentBasisPoints);
    }

    [Fact]
    public void Arrival_CommuterCurveConcentratesDemandAtMorningAndEveningPeaks()
    {
        var morning = DemandModel.CalculateArrival(new DemandContext(
            3_000, 10_000, 1_000, 0, 1_000, 480,
            timeCurve: DemandTimeCurve.CommuterPeaks));
        var midday = DemandModel.CalculateArrival(new DemandContext(
            3_000, 10_000, 1_000, 0, 1_000, 780,
            timeCurve: DemandTimeCurve.CommuterPeaks));
        var evening = DemandModel.CalculateArrival(new DemandContext(
            3_000, 10_000, 1_000, 0, 1_000, 1_080,
            timeCurve: DemandTimeCurve.CommuterPeaks));

        Assert.True(morning.TimeAdjustmentBasisPoints > midday.TimeAdjustmentBasisPoints);
        Assert.True(evening.TimeAdjustmentBasisPoints > midday.TimeAdjustmentBasisPoints);
    }

    private static DemandContext CreateContext(
        int baseBasisPoints = 3_000,
        int priceIndexBasisPoints = 10_000,
        int servicePermille = 1_000,
        int queueLength = 0,
        int cleanlinessPermille = 1_000,
        int minuteOfDay = 900) =>
        new(
            baseBasisPoints,
            priceIndexBasisPoints,
            servicePermille,
            queueLength,
            cleanlinessPermille,
            minuteOfDay);
}
