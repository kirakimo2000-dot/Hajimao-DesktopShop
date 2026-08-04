using System.Text;
using HajimaoDesktopShop.Desktop.Services;
using HajimaoDesktopShop.Desktop.ViewModels;
using HajimaoDesktopShop.Rendering.PixelArt;

namespace HajimaoDesktopShop.Desktop.Tests.Services;

public sealed class PixelGameSoundBankTests
{
    [Theory]
    [InlineData(GameFeedbackKind.RestockQueued)]
    [InlineData(GameFeedbackKind.PriceChanged)]
    [InlineData(GameFeedbackKind.SaleCompleted)]
    [InlineData(GameFeedbackKind.ProcurementOrdered)]
    [InlineData(GameFeedbackKind.AutoRestockChanged)]
    [InlineData(GameFeedbackKind.EmployeeChanged)]
    [InlineData(GameFeedbackKind.StoreGrowthChanged)]
    [InlineData(GameFeedbackKind.PromotionStarted)]
    public void CreateWave_ProducesShortBudgetedPcmCue(GameFeedbackKind kind)
    {
        var bytes = PixelGameSoundBank.CreateWave(kind);

        Assert.Equal("RIFF", Encoding.ASCII.GetString(bytes, 0, 4));
        Assert.Equal("WAVE", Encoding.ASCII.GetString(bytes, 8, 4));
        Assert.InRange(bytes.Length, 45, PixelArtBudget.MaximumSoundBytes);
    }

    [Fact]
    public void CreateWave_IsDeterministicAndDistinctByFeedbackKind()
    {
        var sale = PixelGameSoundBank.CreateWave(GameFeedbackKind.SaleCompleted);

        Assert.Equal(sale, PixelGameSoundBank.CreateWave(GameFeedbackKind.SaleCompleted));
        Assert.NotEqual(sale, PixelGameSoundBank.CreateWave(GameFeedbackKind.EmployeeChanged));
    }
}
