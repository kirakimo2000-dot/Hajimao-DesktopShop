using System.Windows;
using HajimaoDesktopShop.Desktop.ViewModels.Market;

namespace HajimaoDesktopShop.Desktop.Windows;

public partial class StarterStoreChoiceWindow : Window
{
    private readonly StarterStoreChoiceViewModel _viewModel;

    public StarterStoreChoiceWindow(StarterStoreChoiceViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = viewModel;
        _viewModel.SelectionCompleted += OnSelectionCompleted;
        Closed += OnClosed;
    }

    private void OnSelectionCompleted(object? sender, EventArgs e)
    {
        DialogResult = true;
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _viewModel.SelectionCompleted -= OnSelectionCompleted;
        Closed -= OnClosed;
    }
}
