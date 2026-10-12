using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using HajimaoDesktopShop.Infrastructure.Configuration;
using HajimaoDesktopShop.Rendering;
using HajimaoDesktopShop.Rendering.Animation;
using HajimaoDesktopShop.Rendering.PixelArt;
using SkiaSharp;
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
    private SKBitmap? _parts;
    private CancellationTokenSource? _loadCancellation;
    private bool _loadFailed;
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

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_renderer is not null)
        {
            SceneSurface.InvalidateVisual();
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
                ?? throw new InvalidDataException("Shared human character parts image is unreadable.");
            if (token.IsCancellationRequested || !IsLoaded)
            {
                parts.Dispose();
                return;
            }

            _parts = parts;
            IReadOnlyDictionary<FacadeGeometryKind, SKBitmap>? storefronts = null;
            try
            {
                storefronts = StorefrontTextureSet.Load(Path.Combine(
                    AppContext.BaseDirectory,
                    "Assets",
                    "Content",
                    "facades"));
            }
            catch (Exception exception)
            {
                Trace.TraceError($"Storefront textures failed to load; using fallback facades: {exception}");
            }

            try
            {
                _renderer = new CommercialStreetSceneRenderer(
                    SkeletalAnimationCatalog.Create(content),
                    parts,
                    storefronts);
            }
            finally
            {
                if (storefronts is not null)
                {
                    foreach (var storefront in storefronts.Values)
                    {
                        storefront.Dispose();
                    }
                }
            }
            SceneSurface.InvalidateVisual();
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _loadFailed = true;
            Trace.TraceError($"Street character resources failed to load: {exception}");
            SceneSurface.InvalidateVisual();
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
            e.Surface.Canvas.Clear(SkiaSharp.SKColor.Parse(_loadFailed ? "#4A1F24" : "#17191D"));
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
