using System.IO;
using System.Windows;
using HajimaoDesktopShop.Infrastructure.Configuration;
using HajimaoDesktopShop.Rendering.Animation;
using HajimaoDesktopShop.Rendering.Combat;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace HajimaoDesktopShop.Desktop.Controls;

public partial class CombatDesktopShopSurfaceControl : WpfUserControl
{
    public static readonly DependencyProperty FrameProperty = DependencyProperty.Register(
        nameof(Frame),
        typeof(CombatDesktopShopFrame),
        typeof(CombatDesktopShopSurfaceControl),
        new FrameworkPropertyMetadata(null, OnFrameChanged));

    private CombatDesktopShopRenderer? _renderer;
    private CancellationTokenSource? _loadCancellation;

    public CombatDesktopShopSurfaceControl() => InitializeComponent();

    public CombatDesktopShopFrame? Frame
    {
        get => (CombatDesktopShopFrame?)GetValue(FrameProperty);
        set => SetValue(FrameProperty, value);
    }

    public bool UsesLogicalPixelScaling => Surface.IgnorePixelScaling;

    private static void OnFrameChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is CombatDesktopShopSurfaceControl control)
        {
            control.Surface?.InvalidateVisual();
        }
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_renderer is not null)
        {
            Surface.InvalidateVisual();
            return;
        }

        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
        var cancellation = new CancellationTokenSource();
        _loadCancellation = cancellation;
        var token = cancellation.Token;

        try
        {
            var characterRoot = Path.Combine(AppContext.BaseDirectory, "Assets", "Content", "characters");
            var content = await new JsonCharacterAnimationCatalog(
                    Path.Combine(characterRoot, "rigs", "humanoid.json"),
                    Path.Combine(characterRoot, "animations", "humanoid-clips.json"),
                    Path.Combine(characterRoot, "skins.json"))
                .LoadAsync(token);
            token.ThrowIfCancellationRequested();
            var parts = SKBitmap.Decode(Path.Combine(characterRoot, "maomao", "parts.png"))
                ?? throw new InvalidDataException("Maomao modular parts image is unreadable.");
            if (token.IsCancellationRequested || !IsLoaded)
            {
                parts.Dispose();
                return;
            }

            _renderer = new CombatDesktopShopRenderer(
                SkeletalAnimationCatalog.Create(content),
                parts);
            Surface.InvalidateVisual();
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        finally
        {
            if (ReferenceEquals(_loadCancellation, cancellation))
            {
                _loadCancellation = null;
                cancellation.Dispose();
            }
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
        _loadCancellation = null;
        _renderer?.Dispose();
        _renderer = null;
    }

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        if (_renderer is null || Frame is null)
        {
            e.Surface.Canvas.Clear(SKColor.Parse("#17191D"));
            return;
        }

        _renderer.Render(e.Surface.Canvas, e.Info, Frame);
    }
}
