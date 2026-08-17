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
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var workArea = SystemParameters.WorkArea;
        MinWidth = Math.Min(MinWidth, workArea.Width);
        MinHeight = Math.Min(MinHeight, workArea.Height);
        MaxWidth = workArea.Width;
        MaxHeight = workArea.Height;
        Width = Math.Min(ActualWidth > 0 ? ActualWidth : Width, workArea.Width);
        Height = Math.Min(ActualHeight > 0 ? ActualHeight : Height, workArea.Height);
        Left = Math.Clamp(Left, workArea.Left, Math.Max(workArea.Left, workArea.Right - Width));
        Top = Math.Clamp(Top, workArea.Top, Math.Max(workArea.Top, workArea.Bottom - Height));
    }

    private void OnReturnToStreetClick(object sender, RoutedEventArgs e) => Close();
}
