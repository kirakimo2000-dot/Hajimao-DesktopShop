using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using HajimaoDesktopShop.Desktop.Controls;
using HajimaoDesktopShop.Desktop.Services;
using HajimaoDesktopShop.Desktop.ViewModels.Market;
using HajimaoDesktopShop.Rendering.Interactions;

namespace HajimaoDesktopShop.Desktop.Windows;

public partial class DesktopShopWindow : Window
{
    private const double TaskbarSnapDistance = 48d;
    private readonly MarketViewModel _viewModel;
    private int _lastStreetStoreCount;

    public DesktopShopWindow(MarketViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        StreetPage.GetBindingExpression(VisibilityProperty)?.UpdateTarget();
        StorePage.GetBindingExpression(VisibilityProperty)?.UpdateTarget();
        StreetPage.SetCurrentValue(
            VisibilityProperty,
            viewModel.DesktopNavigation.IsStreet ? Visibility.Visible : Visibility.Collapsed);
        StorePage.SetCurrentValue(
            VisibilityProperty,
            viewModel.DesktopNavigation.IsStore ? Visibility.Visible : Visibility.Collapsed);
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        _viewModel.DesktopNavigation.PropertyChanged += OnDesktopNavigationPropertyChanged;
        _viewModel.CommercialStreet.PropertyChanged += OnCommercialStreetPropertyChanged;
        _lastStreetStoreCount = GetOpenedStoreCount();
        ApplySurfaceLayout(reposition: true);
        Closed += OnClosed;
    }

    public event EventHandler? OpenManagementRequested;

    public void SelectShopObject(BusinessShopInteractionTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);
        _viewModel.SelectShopObjectCommand.Execute(target);
        OpenManagementRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnSurfaceMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var position = e.GetPosition(this);
        if (_viewModel.DesktopNavigation.IsStore
            && e.ClickCount == 2
            && position.Y is >= 42d and < 238d)
        {
            OpenManagementRequested?.Invoke(this, EventArgs.Empty);
            return;
        }

        var dragHeight = _viewModel.DesktopNavigation.IsStreet ? 28d : 42d;
        if (position.Y >= dragHeight
            || (_viewModel.DesktopNavigation.IsStore && position.X >= 300d)
            || e.LeftButton != MouseButtonState.Pressed
            || _viewModel.IsLocked)
        {
            return;
        }

        DragMove();
        SnapAboveTaskbarIfNear();
    }

    private void OnOpenManagementClick(object sender, RoutedEventArgs e) =>
        OpenManagementRequested?.Invoke(this, EventArgs.Empty);

    private void OnClickThroughClick(object sender, RoutedEventArgs e)
    {
        if (!_viewModel.IsClickThrough)
        {
            OpenManagementRequested?.Invoke(this, EventArgs.Empty);
        }

        _viewModel.ToggleClickThroughCommand.Execute(null);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MarketViewModel.IsClickThrough))
        {
            WindowInteractionService.SetClickThrough(this, _viewModel.IsClickThrough);
        }
    }

    private void OnDesktopNavigationPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DesktopNavigationViewModel.Mode))
        {
            StreetPage.SetCurrentValue(
                VisibilityProperty,
                _viewModel.DesktopNavigation.IsStreet ? Visibility.Visible : Visibility.Collapsed);
            StorePage.SetCurrentValue(
                VisibilityProperty,
                _viewModel.DesktopNavigation.IsStore ? Visibility.Visible : Visibility.Collapsed);
            ApplySurfaceLayout(reposition: false);
        }
    }

    private void OnCommercialStreetPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(CommercialStreetViewModel.SceneFrame))
        {
            return;
        }

        var storeCount = GetOpenedStoreCount();
        if (storeCount == _lastStreetStoreCount)
        {
            return;
        }

        _lastStreetStoreCount = storeCount;
        if (_viewModel.DesktopNavigation.IsStreet)
        {
            ApplySurfaceLayout(reposition: false);
        }
    }

    private void OnStorefrontClicked(
        object sender,
        CommercialStreetStorefrontClickedEventArgs e) =>
        _viewModel.DesktopNavigation.OpenStoreCommand.Execute(e.StoreId);

    private void OnShopObjectClicked(
        object sender,
        BusinessShopObjectClickedEventArgs e) =>
        SelectShopObject(e.Target);

    private int GetOpenedStoreCount() =>
        _viewModel.CommercialStreet.SceneFrame?.Snapshot.Stores.Count ?? 1;

    private void ApplySurfaceLayout(bool reposition)
    {
        var workAreas = MonitorWorkAreaProvider.GetLogicalWorkAreas();
        if (workAreas.Count == 0)
        {
            return;
        }

        var workArea = FindNearestWorkArea(workAreas);
        var layout = DesktopSurfaceWindowLayoutPolicy.Create(
            _viewModel.DesktopNavigation.Mode,
            GetOpenedStoreCount(),
            workArea);
        var currentWidth = ActualWidth > 0d ? ActualWidth : Width;
        var currentHeight = ActualHeight > 0d ? ActualHeight : Height;
        var wasTaskbarDocked = !reposition
            && double.IsFinite(Left)
            && double.IsFinite(Top)
            && double.IsFinite(currentWidth)
            && currentWidth > 0d
            && double.IsFinite(currentHeight)
            && currentHeight > 0d
            && DesktopWindowPlacementPolicy.TrySnapAboveWorkAreaBottom(
                new DesktopRect(Left, Top, currentWidth, currentHeight),
                workArea,
                TaskbarSnapDistance,
                DesktopSurfaceWindowLayoutPolicy.WorkAreaMargin,
                out _);
        Width = layout.Size.Width;
        Height = layout.Size.Height;
        if (reposition)
        {
            Left = layout.Position.X;
            Top = layout.Position.Y;
        }
        else if (wasTaskbarDocked)
        {
            Top = layout.Position.Y;
        }
    }

    private DesktopRect FindNearestWorkArea(IReadOnlyList<DesktopRect> workAreas)
    {
        if (!double.IsFinite(Left) || !double.IsFinite(Top))
        {
            return workAreas[0];
        }

        var center = new DesktopPoint(Left + (Width / 2d), Top + (Height / 2d));
        return workAreas.MinBy(area => SquaredDistance(center, area))!;
    }

    private static double SquaredDistance(DesktopPoint point, DesktopRect area)
    {
        var horizontal = point.X < area.X
            ? area.X - point.X
            : point.X > area.Right ? point.X - area.Right : 0d;
        var vertical = point.Y < area.Y
            ? area.Y - point.Y
            : point.Y > area.Bottom ? point.Y - area.Bottom : 0d;
        return (horizontal * horizontal) + (vertical * vertical);
    }

    private void SnapAboveTaskbarIfNear()
    {
        var workAreas = MonitorWorkAreaProvider.GetLogicalWorkAreas();
        var width = ActualWidth > 0d ? ActualWidth : Width;
        var height = ActualHeight > 0d ? ActualHeight : Height;
        if (workAreas.Count == 0
            || !double.IsFinite(Left)
            || !double.IsFinite(Top)
            || !double.IsFinite(width)
            || width <= 0d
            || !double.IsFinite(height)
            || height <= 0d)
        {
            return;
        }

        var workArea = FindNearestWorkArea(workAreas);
        if (DesktopWindowPlacementPolicy.TrySnapAboveWorkAreaBottom(
                new DesktopRect(Left, Top, width, height),
                workArea,
                TaskbarSnapDistance,
                DesktopSurfaceWindowLayoutPolicy.WorkAreaMargin,
                out var snapped))
        {
            Left = snapped.X;
            Top = snapped.Y;
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel.DesktopNavigation.PropertyChanged -= OnDesktopNavigationPropertyChanged;
        _viewModel.CommercialStreet.PropertyChanged -= OnCommercialStreetPropertyChanged;
        Closed -= OnClosed;
    }
}
