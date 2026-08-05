using System.Windows;
using System.Windows.Input;
using HajimaoDesktopShop.Rendering;
using HajimaoDesktopShop.Rendering.Interactions;
using SkiaSharp.Views.Desktop;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace HajimaoDesktopShop.Desktop.Controls;

public partial class BusinessShopSceneControl : WpfUserControl
{
    public static readonly DependencyProperty FrameProperty = DependencyProperty.Register(
        nameof(Frame),
        typeof(BusinessShopSceneFrame),
        typeof(BusinessShopSceneControl),
        new FrameworkPropertyMetadata(null, OnFrameChanged));

    private BusinessShopSceneRenderer? _renderer;

    public BusinessShopSceneControl() => InitializeComponent();

    public BusinessShopSceneFrame? Frame
    {
        get => (BusinessShopSceneFrame?)GetValue(FrameProperty);
        set => SetValue(FrameProperty, value);
    }

    public bool UsesLogicalPixelScaling => SceneSurface.IgnorePixelScaling;

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
                BusinessShopSceneRenderer.LogicalWidth,
                BusinessShopSceneRenderer.LogicalHeight,
                viewportX,
                viewportY,
                out var point))
        {
            return null;
        }

        return BusinessShopInteractionMap.HitTest(Frame, point.X, point.Y);
    }

    private static void OnFrameChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is BusinessShopSceneControl control)
        {
            control.SceneSurface?.InvalidateVisual();
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _renderer ??= new BusinessShopSceneRenderer();
        SceneSurface.InvalidateVisual();
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
        var point = e.GetPosition(SceneSurface);
        var target = HitTestObject(
            point.X,
            point.Y,
            Math.Max(0, (int)Math.Floor(SceneSurface.ActualWidth)),
            Math.Max(0, (int)Math.Floor(SceneSurface.ActualHeight)));
        if (target is null)
        {
            return;
        }

        ObjectClicked?.Invoke(this, new BusinessShopObjectClickedEventArgs(target));
        e.Handled = true;
    }
}
