using System.IO;
using System.Windows;
using HajimaoDesktopShop.Infrastructure.Configuration;
using HajimaoDesktopShop.Rendering.Animation;
using HajimaoDesktopShop.Rendering.Combat;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using WpfUserControl = System.Windows.Controls.UserControl;

namespace HajimaoDesktopShop.Desktop.Controls;

public partial class CombatShopSceneControl : WpfUserControl
{
    public static readonly DependencyProperty FrameProperty = DependencyProperty.Register(
        nameof(Frame),
        typeof(CombatShopSceneFrame),
        typeof(CombatShopSceneControl),
        new FrameworkPropertyMetadata(null, OnFrameChanged));

    private CombatShopSceneRenderer? _renderer;
    private SKBitmap? _parts;

    public CombatShopSceneControl() => InitializeComponent();

    public CombatShopSceneFrame? Frame
    {
        get => (CombatShopSceneFrame?)GetValue(FrameProperty);
        set => SetValue(FrameProperty, value);
    }

    private static void OnFrameChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is CombatShopSceneControl control)
        {
            control.Surface?.InvalidateVisual();
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_renderer is null)
        {
            var root = Path.Combine(AppContext.BaseDirectory, "Assets", "Content", "characters");
            var content = new JsonCharacterAnimationCatalog(
                    Path.Combine(root, "rigs", "humanoid.json"),
                    Path.Combine(root, "animations", "humanoid-clips.json"),
                    Path.Combine(root, "skins.json"))
                .LoadAsync().GetAwaiter().GetResult();
            _parts = SKBitmap.Decode(Path.Combine(root, "maomao", "parts.png"))
                ?? throw new InvalidDataException("Maomao modular parts image is unreadable.");
            _renderer = new CombatShopSceneRenderer(SkeletalAnimationCatalog.Create(content), _parts);
        }

        Surface.InvalidateVisual();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _renderer?.Dispose();
        _renderer = null;
        _parts?.Dispose();
        _parts = null;
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
