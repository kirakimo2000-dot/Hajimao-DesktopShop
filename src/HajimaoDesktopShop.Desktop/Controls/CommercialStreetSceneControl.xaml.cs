using System.Windows;
using HajimaoDesktopShop.Rendering;
using SkiaSharp.Views.Desktop;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace HajimaoDesktopShop.Desktop.Controls;

public partial class CommercialStreetSceneControl : WpfUserControl
{
    public static readonly DependencyProperty FrameProperty = DependencyProperty.Register(
        nameof(Frame),
        typeof(CommercialStreetSceneFrame),
        typeof(CommercialStreetSceneControl),
        new FrameworkPropertyMetadata(null, OnFrameChanged));

    private CommercialStreetSceneRenderer? _renderer;

    public CommercialStreetSceneControl() => InitializeComponent();

    public CommercialStreetSceneFrame? Frame
    {
        get => (CommercialStreetSceneFrame?)GetValue(FrameProperty);
        set => SetValue(FrameProperty, value);
    }

    public bool UsesLogicalPixelScaling => SceneSurface.IgnorePixelScaling;

    private static void OnFrameChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is CommercialStreetSceneControl control)
        {
            control.SceneSurface?.InvalidateVisual();
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _renderer ??= new CommercialStreetSceneRenderer();
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
}
