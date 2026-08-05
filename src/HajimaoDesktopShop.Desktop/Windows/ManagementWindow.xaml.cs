using System.Windows;
using HajimaoDesktopShop.Desktop.Controls;
using HajimaoDesktopShop.Desktop.ViewModels.Market;
using HajimaoDesktopShop.Rendering.Interactions;

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

    public void SelectShopObject(BusinessShopInteractionTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);
        _viewModel.SelectShopObjectCommand.Execute(target);
    }

    private void OnShopObjectClicked(
        object sender,
        BusinessShopObjectClickedEventArgs e) =>
        SelectShopObject(e.Target);
}
