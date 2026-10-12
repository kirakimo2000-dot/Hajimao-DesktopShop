using System.IO;
using System.Diagnostics;
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
    private CancellationTokenSource? _loadCancellation;
    private bool _loadFailed;

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

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_renderer is not null)
        {
            Surface.InvalidateVisual();
            return;
        }

        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
        _loadFailed = false;
        var cancellation = new CancellationTokenSource();
        _loadCancellation = cancellation;
        var token = cancellation.Token;

        try
        {
            var root = Path.Combine(AppContext.BaseDirectory, "Assets", "Content", "characters");
            var content = await new JsonCharacterAnimationCatalog(
                    Path.Combine(root, "rigs", "humanoid.json"),
                    Path.Combine(root, "animations", "humanoid-clips.json"),
                    Path.Combine(root, "skins.json"))
                .LoadAsync(token);
            token.ThrowIfCancellationRequested();
            var parts = SKBitmap.Decode(Path.Combine(root, "maomao", "parts.png"))
                ?? throw new InvalidDataException("Maomao modular parts image is unreadable.");
            if (token.IsCancellationRequested || !IsLoaded)
            {
                parts.Dispose();
                return;
            }

            _parts = parts;
            _renderer = new CombatShopSceneRenderer(SkeletalAnimationCatalog.Create(content), parts);
            Surface.InvalidateVisual();
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _loadFailed = true;
            Trace.TraceError($"Management combat resources failed to load: {exception}");
            Surface.InvalidateVisual();
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
        _parts?.Dispose();
        _parts = null;
    }

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        if (_renderer is null || Frame is null)
        {
            e.Surface.Canvas.Clear(SKColor.Parse(_loadFailed ? "#4A1F24" : "#17191D"));
            return;
        }

        _renderer.Render(e.Surface.Canvas, e.Info, Frame);
    }
}
