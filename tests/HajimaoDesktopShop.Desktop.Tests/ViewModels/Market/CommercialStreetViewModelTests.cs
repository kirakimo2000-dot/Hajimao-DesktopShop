using HajimaoDesktopShop.Application.Business.Street;
using HajimaoDesktopShop.Desktop.ViewModels.Market;
using HajimaoDesktopShop.Domain.Streets;
using System.Globalization;

namespace HajimaoDesktopShop.Desktop.Tests.ViewModels.Market;

public sealed class CommercialStreetViewModelTests
{
    [Fact]
    public void Refresh_FormatsStreetSnapshotWithoutOwningTrafficRules()
    {
        var viewModel = new CommercialStreetViewModel();
        var snapshot = CreateSnapshot();

        viewModel.Refresh(snapshot, animationFrame: 2, reduceMotion: false);

        Assert.Equal("雨天", viewModel.WeatherText);
        Assert.Equal("一段商业街", viewModel.TierText);
        Assert.Equal("共享客流 64.40%", viewModel.TrafficText);
        Assert.Equal("已开店 2 · 路人 4 · 车辆 1", viewModel.ActivityText);
        Assert.Equal(2, viewModel.SceneFrame!.AnimationFrame);
        Assert.Same(snapshot, viewModel.SceneFrame.Snapshot);
    }

    [Fact]
    public void Refresh_ReducedMotionLocksStreetFrameZero()
    {
        var viewModel = new CommercialStreetViewModel();

        viewModel.Refresh(CreateSnapshot(), animationFrame: 3, reduceMotion: true);

        Assert.Equal(0, viewModel.SceneFrame!.AnimationFrame);
        Assert.True(viewModel.SceneFrame.ReduceMotion);
    }

    [Fact]
    public void Refresh_FormatsStorePercentagesIndependentlyOfWindowsCulture()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            var viewModel = new CommercialStreetViewModel();

            viewModel.Refresh(CreateSnapshot(), animationFrame: 0, reduceMotion: false);

            Assert.Equal("吸引力 80.00%", viewModel.Stores[0].AttractionText);
            Assert.Equal("客流份额 50.00%", viewModel.Stores[0].TrafficShareText);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    private static CommercialStreetSnapshot CreateSnapshot() =>
        new(
            CommercialStreetTier.Street,
            StreetWeather.Rain,
            6_440,
            4,
            1,
            [
                new CommercialStreetStoreSnapshot("corner", "街角店", 8_000, 5_000),
                new CommercialStreetStoreSnapshot("station", "车站店", 8_000, 5_000)
            ]);
}
