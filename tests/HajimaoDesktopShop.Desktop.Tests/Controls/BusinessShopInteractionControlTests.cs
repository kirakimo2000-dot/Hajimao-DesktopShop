using System.Runtime.ExceptionServices;
using HajimaoDesktopShop.Desktop.Controls;
using HajimaoDesktopShop.Desktop.Tests.ViewModels.Market;
using HajimaoDesktopShop.Desktop.ViewModels.Market;
using HajimaoDesktopShop.Rendering.Interactions;

namespace HajimaoDesktopShop.Desktop.Tests.Controls;

public sealed class BusinessShopInteractionControlTests
{
    [Fact]
    public void SceneControl_MapsViewportPointToShelfTarget()
    {
        RunOnSta(() =>
        {
            var viewModel = new MarketViewModel(MarketTestSession.Create());
            var control = new BusinessShopSceneControl { Frame = viewModel.SceneFrame };

            var target = control.HitTestObject(110, 80, 420, 180);

            Assert.NotNull(target);
            Assert.Equal(BusinessShopInteractionKind.Shelf, target.Kind);
            Assert.Equal("ambient", target.Key);
        });
    }

    [Fact]
    public void DesktopSurface_SubtractsSceneOffsetAndRejectsHeader()
    {
        RunOnSta(() =>
        {
            var viewModel = new MarketViewModel(MarketTestSession.Create());
            var control = new BusinessDesktopShopSurfaceControl { Frame = viewModel.DesktopFrame };

            var target = control.HitTestObject(110, 130, 420, 280);

            Assert.NotNull(target);
            Assert.Equal("ambient", target.Key);
            Assert.Null(control.HitTestObject(110, 20, 420, 280));
        });
    }

    [Fact]
    public void ClickEventArgs_RejectsNullTarget()
    {
        Assert.Throws<ArgumentNullException>(() => new BusinessShopObjectClickedEventArgs(null!));
    }

    private static void RunOnSta(Action action)
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                error = exception;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(10)), "STA control test timed out.");
        if (error is not null)
        {
            ExceptionDispatchInfo.Capture(error).Throw();
        }
    }
}
