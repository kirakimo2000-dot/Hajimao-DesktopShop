using System.Windows;
using System.Windows.Input;
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
    private int _cameraOffset;

    public CommercialStreetSceneControl() => InitializeComponent();

    public CommercialStreetSceneFrame? Frame
    {
        get => (CommercialStreetSceneFrame?)GetValue(FrameProperty);
        set => SetValue(FrameProperty, value);
    }

    public bool UsesLogicalPixelScaling => SceneSurface.IgnorePixelScaling;

    public int CameraOffset => _cameraOffset;

    public event EventHandler<CommercialStreetStorefrontClickedEventArgs>? StorefrontClicked;

    private static void OnFrameChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is CommercialStreetSceneControl control)
        {
            control.ClampCameraOffset();
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

        _renderer.Render(
            e.Surface.Canvas,
            e.Info,
            Frame with { CameraOffset = _cameraOffset });
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Frame is null)
        {
            return;
        }

        _cameraOffset += e.Delta < 0 ? 96 : -96;
        ClampCameraOffset();
        SceneSurface.InvalidateVisual();
        e.Handled = true;
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (Frame is null)
        {
            return;
        }

        var point = e.GetPosition(this);
        var storefronts = CommercialStreetLayout.CreateStorefronts(Frame.Snapshot.Stores);
        var storeId = CommercialStreetLayout.HitTest(
            storefronts,
            (int)Math.Floor(point.X) + _cameraOffset,
            (int)Math.Floor(point.Y));
        if (storeId is null)
        {
            return;
        }

        StorefrontClicked?.Invoke(this, new CommercialStreetStorefrontClickedEventArgs(storeId));
        e.Handled = true;
    }

    private void ClampCameraOffset()
    {
        if (Frame is null)
        {
            _cameraOffset = 0;
            return;
        }

        var contentWidth = CommercialStreetLayout.GetContentWidth(Frame.Snapshot.Stores.Count);
        var viewportWidth = Math.Min(contentWidth, Math.Max(1, (int)Math.Floor(ActualWidth)));
        _cameraOffset = CommercialStreetLayout.ClampCameraOffset(
            contentWidth,
            viewportWidth,
            _cameraOffset);
    }
}

public sealed class CommercialStreetStorefrontClickedEventArgs(string storeId) : EventArgs
{
    public string StoreId { get; } = string.IsNullOrWhiteSpace(storeId)
        ? throw new ArgumentException("Store id is required.", nameof(storeId))
        : storeId;
}
