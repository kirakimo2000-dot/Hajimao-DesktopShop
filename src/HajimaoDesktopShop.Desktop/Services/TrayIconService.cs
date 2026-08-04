using System.Drawing;
using Forms = System.Windows.Forms;

namespace HajimaoDesktopShop.Desktop.Services;

public sealed class TrayIconService : IDisposable
{
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly Forms.ContextMenuStrip _contextMenu;
    private readonly Icon _icon;
    private bool _disposed;

    public TrayIconService(Icon? icon = null)
    {
        _icon = icon ?? TrayIconFactory.Create();
        _contextMenu = new Forms.ContextMenuStrip();
        _contextMenu.Items.Add("显示桌面小店", null, (_, _) => OpenShopRequested?.Invoke(this, EventArgs.Empty));
        _contextMenu.Items.Add("打开经营管理", null, (_, _) => OpenManagementRequested?.Invoke(this, EventArgs.Empty));
        _contextMenu.Items.Add(new Forms.ToolStripSeparator());
        _contextMenu.Items.Add("退出 Hajimao Market", null, (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty));

        _notifyIcon = new Forms.NotifyIcon
        {
            Text = "Hajimao Market · 持续经营中",
            Icon = _icon,
            ContextMenuStrip = _contextMenu,
            Visible = true
        };
        _notifyIcon.DoubleClick += OnDoubleClick;
    }

    public event EventHandler? OpenShopRequested;

    public event EventHandler? OpenManagementRequested;

    public event EventHandler? ExitRequested;

    public bool IsVisible => !_disposed && _notifyIcon.Visible;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _notifyIcon.DoubleClick -= OnDoubleClick;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _contextMenu.Dispose();
        _icon.Dispose();
    }

    private void OnDoubleClick(object? sender, EventArgs e) =>
        OpenShopRequested?.Invoke(this, EventArgs.Empty);
}
