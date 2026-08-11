using System.Runtime.ExceptionServices;
using System.Drawing;
using HajimaoDesktopShop.Desktop.Services;
using Forms = System.Windows.Forms;

namespace HajimaoDesktopShop.Desktop.Tests.Services;

public sealed class TrayIconServiceTests
{
    [Fact]
    public void Factory_CreatesDistinctThirtyTwoPixelMarketIcon()
    {
        using var icon = TrayIconFactory.Create();

        Assert.Equal(new Size(32, 32), icon.Size);
        Assert.NotEqual(SystemIcons.Application.Handle, icon.Handle);
    }

    [Fact]
    public void Lifecycle_ShowsOneIconUntilDisposed()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var service = new TrayIconService();
                Assert.True(service.IsVisible);

                service.Dispose();

                Assert.False(service.IsVisible);
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(15)), "Tray verification thread timed out.");
        if (failure is not null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }

    [Fact]
    public void ContextMenu_ExportFeedbackClickRaisesRequestOnceBeforeExit()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                using var service = new TrayIconService();
                var requestCount = 0;
                service.ExportFeedbackRequested += (_, _) => requestCount++;

                var contextMenu = typeof(TrayIconService)
                    .GetField("_contextMenu", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    ?.GetValue(service) as Forms.ContextMenuStrip;

                Assert.NotNull(contextMenu);
                var exportItem = Assert.IsType<Forms.ToolStripMenuItem>(
                    contextMenu.Items.Cast<Forms.ToolStripItem>().Single(item => item.Text == "生成测试反馈包"));
                var exportIndex = contextMenu.Items.IndexOf(exportItem);
                var separatorIndex = contextMenu.Items
                    .Cast<Forms.ToolStripItem>()
                    .Select((item, index) => (item, index))
                    .Single(pair => pair.item is Forms.ToolStripSeparator)
                    .index;
                var exitIndex = contextMenu.Items
                    .Cast<Forms.ToolStripItem>()
                    .Select((item, index) => (item, index))
                    .Single(pair => pair.item.Text == ProductIdentity.ExitMenuText)
                    .index;

                exportItem.PerformClick();

                Assert.Equal(1, requestCount);
                Assert.True(exportIndex < separatorIndex);
                Assert.True(separatorIndex < exitIndex);
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(15)), "Tray verification thread timed out.");
        if (failure is not null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }
}
