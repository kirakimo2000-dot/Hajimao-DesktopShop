using HajimaoDesktopShop.Application.Business.Street;
using HajimaoDesktopShop.Desktop.ViewModels.Market;
using HajimaoDesktopShop.Domain.Streets;

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
