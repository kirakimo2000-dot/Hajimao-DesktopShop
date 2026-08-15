using System.Windows;
using HajimaoDesktopShop.Desktop.ViewModels.Market;

namespace HajimaoDesktopShop.Desktop.Windows;

public partial class ManagementWindow : Window
{
    private readonly MarketViewModel _viewModel;

    public ManagementWindow(MarketViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    private void OnReturnToStreetClick(object sender, RoutedEventArgs e) => Close();
}
