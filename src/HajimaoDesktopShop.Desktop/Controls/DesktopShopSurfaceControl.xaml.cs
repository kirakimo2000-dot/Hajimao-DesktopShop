using System.Windows;
using System.Windows.Controls;
using HajimaoDesktopShop.Rendering;
using SkiaSharp.Views.Desktop;

namespace HajimaoDesktopShop.Desktop.Controls;

public partial class DesktopShopSurfaceControl : UserControl
{
    public static readonly DependencyProperty FrameProperty = DependencyProperty.Register(
        nameof(Frame),
        typeof(DesktopShopFrame),
        typeof(DesktopShopSurfaceControl),
        new FrameworkPropertyMetadata(null, OnFrameChanged));

    private DesktopShopRenderer? _renderer;

    public DesktopShopSurfaceControl()
    {
        InitializeComponent();
    }

    public DesktopShopFrame? Frame
    {
        get => (DesktopShopFrame?)GetValue(FrameProperty);
        set => SetValue(FrameProperty, value);
    }

    public bool UsesLogicalPixelScaling => Surface.IgnorePixelScaling;

    private static void OnFrameChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is DesktopShopSurfaceControl control)
        {
            control.Surface?.InvalidateVisual();
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _renderer ??= new DesktopShopRenderer();
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
