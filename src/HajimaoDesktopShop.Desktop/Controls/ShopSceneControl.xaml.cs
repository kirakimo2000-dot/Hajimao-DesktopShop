using System.Windows;
using System.Windows.Controls;
using HajimaoDesktopShop.Application.Simulation;
using HajimaoDesktopShop.Rendering;
using SkiaSharp.Views.Desktop;

namespace HajimaoDesktopShop.Desktop.Controls;

public partial class ShopSceneControl : UserControl
{
    public static readonly DependencyProperty SnapshotProperty = DependencyProperty.Register(
        nameof(Snapshot),
        typeof(SimulationSnapshot),
        typeof(ShopSceneControl),
        new FrameworkPropertyMetadata(null, OnSnapshotChanged));

    private ShopSceneRenderer? _renderer;

    public ShopSceneControl()
    {
        InitializeComponent();
    }

    public SimulationSnapshot? Snapshot
    {
        get => (SimulationSnapshot?)GetValue(SnapshotProperty);
        set => SetValue(SnapshotProperty, value);
    }

    private static void OnSnapshotChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is ShopSceneControl control)
        {
            control.SceneSurface?.InvalidateVisual();
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _renderer ??= new ShopSceneRenderer();
        SceneSurface.InvalidateVisual();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _renderer?.Dispose();
        _renderer = null;
    }

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        if (_renderer is null || Snapshot is null)
        {
            e.Surface.Canvas.Clear(SkiaSharp.SKColor.Parse("#17191D"));
            return;
        }

        _renderer.Render(e.Surface.Canvas, e.Info, Snapshot);
    }
}
