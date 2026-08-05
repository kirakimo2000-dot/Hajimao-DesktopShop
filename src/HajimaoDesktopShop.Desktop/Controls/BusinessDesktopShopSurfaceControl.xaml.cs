using System.Windows;
using System.Windows.Input;
using HajimaoDesktopShop.Rendering;
using HajimaoDesktopShop.Rendering.Interactions;
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

    public event EventHandler<BusinessShopObjectClickedEventArgs>? ObjectClicked;

    public BusinessShopInteractionTarget? HitTestObject(
        double viewportX,
        double viewportY,
        int viewportWidth,
        int viewportHeight)
    {
        if (Frame is null
            || !LogicalPixelViewport.TryMapPoint(
                viewportWidth,
                viewportHeight,
                BusinessDesktopShopRenderer.LogicalWidth,
                BusinessDesktopShopRenderer.LogicalHeight,
                viewportX,
                viewportY,
                out var point))
        {
            return null;
        }

        const int sceneOffsetY = 50;
        var sceneY = point.Y - sceneOffsetY;
        if (sceneY < 0 || sceneY >= BusinessShopSceneRenderer.LogicalHeight)
        {
            return null;
        }

        return BusinessShopInteractionMap.HitTest(Frame.Scene, point.X, sceneY);
    }

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

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        var point = e.GetPosition(Surface);
        var target = HitTestObject(
            point.X,
            point.Y,
            Math.Max(0, (int)Math.Floor(Surface.ActualWidth)),
            Math.Max(0, (int)Math.Floor(Surface.ActualHeight)));
        if (target is null)
        {
            return;
        }

        ObjectClicked?.Invoke(this, new BusinessShopObjectClickedEventArgs(target));
        e.Handled = true;
    }
}
