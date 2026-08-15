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

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_renderer is null)
        {
            var characterRoot = Path.Combine(AppContext.BaseDirectory, "Assets", "Content", "characters");
            var content = new JsonCharacterAnimationCatalog(
                    Path.Combine(characterRoot, "rigs", "humanoid.json"),
                    Path.Combine(characterRoot, "animations", "humanoid-clips.json"),
                    Path.Combine(characterRoot, "skins.json"))
                .LoadAsync()
                .GetAwaiter()
                .GetResult();
            var parts = SKBitmap.Decode(Path.Combine(characterRoot, "maomao", "parts.png"))
                ?? throw new InvalidDataException("Maomao modular parts image is unreadable.");
            _renderer = new CombatDesktopShopRenderer(
                SkeletalAnimationCatalog.Create(content),
                parts);
        }

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
            e.Surface.Canvas.Clear(SKColor.Parse("#17191D"));
            return;
        }

        _renderer.Render(e.Surface.Canvas, e.Info, Frame);
    }
}
