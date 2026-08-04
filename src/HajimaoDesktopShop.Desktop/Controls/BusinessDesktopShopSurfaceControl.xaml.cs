using System.Windows;
using HajimaoDesktopShop.Rendering;
using SkiaSharp.Views.Desktop;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace HajimaoDesktopShop.Desktop.Controls;

public partial class BusinessDesktopShopSurfaceControl : WpfUserControl
{
    public static readonly DependencyProperty FrameProperty = DependencyProperty.Register(
        nameof(Frame),
        typeof(BusinessShopFrame),
        typeof(BusinessDesktopShopSurfaceControl),
        new FrameworkPropertyMetadata(null, OnFrameChanged));

    private BusinessDesktopShopRenderer? _renderer;

    public BusinessDesktopShopSurfaceControl() => InitializeComponent();

    public BusinessShopFrame? Frame
    {
        get => (BusinessShopFrame?)GetValue(FrameProperty);
        set => SetValue(FrameProperty, value);
    }

    public bool UsesLogicalPixelScaling => Surface.IgnorePixelScaling;

    private static void OnFrameChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is BusinessDesktopShopSurfaceControl control)
        {
            control.Surface?.InvalidateVisual();
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _renderer ??= new BusinessDesktopShopRenderer();
        Surface.InvalidateVisual();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _renderer?.Dispose();
        _renderer = null;
    }

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        if (_renderer is null || Frame is null)
        {
            e.Surface.Canvas.Clear(SkiaSharp.SKColor.Parse("#17191D"));
            return;
        }

        _renderer.Render(e.Surface.Canvas, e.Info, Frame);
    }
}
