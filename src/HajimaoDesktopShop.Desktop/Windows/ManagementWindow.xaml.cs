using System.Windows;
using HajimaoDesktopShop.Desktop.ViewModels;

namespace HajimaoDesktopShop.Desktop.Windows;

public partial class ManagementWindow : Window
{
    public ManagementWindow(GameViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        InitializeComponent();
        DataContext = viewModel;
    }
}
