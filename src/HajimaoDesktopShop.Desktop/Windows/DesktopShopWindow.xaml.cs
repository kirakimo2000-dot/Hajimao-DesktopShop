using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using HajimaoDesktopShop.Desktop.Services;
using HajimaoDesktopShop.Desktop.ViewModels;

namespace HajimaoDesktopShop.Desktop.Windows;

public partial class DesktopShopWindow : Window
{
    private readonly GameViewModel _viewModel;

    public DesktopShopWindow(GameViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        Closed += OnClosed;
    }

    public event EventHandler? OpenManagementRequested;

    private void OnSurfaceMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var position = e.GetPosition(this);
        if (e.ClickCount == 2 && position.Y is >= 42d and < 238d)
        {
            OpenManagementRequested?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (position.Y >= 42d
            || position.X >= 300d
            || e.LeftButton != MouseButtonState.Pressed
            || _viewModel.IsLocked)
        {
            return;
        }

        DragMove();
        WindowInteractionService.SnapToNearestWorkAreaCorner(this);
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
        if (e.PropertyName == nameof(GameViewModel.IsClickThrough))
        {
            WindowInteractionService.SetClickThrough(this, _viewModel.IsClickThrough);
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        Closed -= OnClosed;
    }
}
