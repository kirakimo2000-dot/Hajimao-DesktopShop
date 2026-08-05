using CommunityToolkit.Mvvm.Input;

namespace HajimaoDesktopShop.Desktop.ViewModels.Market;

public sealed class ShopObjectActionsViewModel
{
    private readonly Func<ShopObjectDetailViewModel?> _selectedObject;
    private readonly Func<string> _selectedStoreId;
    private readonly ProductManagementViewModel _products;
    private readonly EmployeeManagementViewModel _employees;
    private readonly Action _refreshMarket;
    private readonly RelayCommand _quickRestockCommand;
    private readonly RelayCommand _toggleAutoRestockCommand;
    private readonly RelayCommand _trainEmployeeCommand;
    private readonly RelayCommand _setDayShiftCommand;
    private readonly RelayCommand _setNightShiftCommand;

    public ShopObjectActionsViewModel(
        Func<ShopObjectDetailViewModel?> selectedObject,
        Func<string> selectedStoreId,
        ProductManagementViewModel products,
        EmployeeManagementViewModel employees,
        Action refreshMarket)
    {
        ArgumentNullException.ThrowIfNull(selectedObject);
        ArgumentNullException.ThrowIfNull(selectedStoreId);
        ArgumentNullException.ThrowIfNull(products);
        ArgumentNullException.ThrowIfNull(employees);
        ArgumentNullException.ThrowIfNull(refreshMarket);
        _selectedObject = selectedObject;
        _selectedStoreId = selectedStoreId;
        _products = products;
        _employees = employees;
        _refreshMarket = refreshMarket;
        _quickRestockCommand = new RelayCommand(QuickRestock, CanOperateShelf);
        _toggleAutoRestockCommand = new RelayCommand(ToggleAutoRestock, CanOperateShelf);
        _trainEmployeeCommand = new RelayCommand(TrainEmployee, CanOperateEmployee);
        _setDayShiftCommand = new RelayCommand(SetDayShift, CanOperateEmployee);
        _setNightShiftCommand = new RelayCommand(SetNightShift, CanOperateEmployee);
    }

    public IRelayCommand QuickRestockCommand => _quickRestockCommand;

    public IRelayCommand ToggleAutoRestockCommand => _toggleAutoRestockCommand;

    public IRelayCommand TrainEmployeeCommand => _trainEmployeeCommand;

    public IRelayCommand SetDayShiftCommand => _setDayShiftCommand;

    public IRelayCommand SetNightShiftCommand => _setNightShiftCommand;

    public string AutoRestockActionText =>
        _selectedObject()?.IsAutoRestockEnabled == true
            ? "关闭自动补货"
            : "开启自动补货";

    public void NotifySelectionChanged()
    {
        _quickRestockCommand.NotifyCanExecuteChanged();
        _toggleAutoRestockCommand.NotifyCanExecuteChanged();
        _trainEmployeeCommand.NotifyCanExecuteChanged();
        _setDayShiftCommand.NotifyCanExecuteChanged();
        _setNightShiftCommand.NotifyCanExecuteChanged();
    }

    private bool CanOperateShelf() => FindShelfProduct() is not null;

    private bool CanOperateEmployee() => FindEmployee() is not null;

    private void QuickRestock() => Execute(FindShelfProduct(), product => product.OrderLocalCommand);

    private void ToggleAutoRestock() => Execute(
        FindShelfProduct(),
        product => product.ToggleAutoRestockCommand);

    private void TrainEmployee() => Execute(FindEmployee(), employee => employee.TrainCommand);

    private void SetDayShift() => Execute(FindEmployee(), employee => employee.SetDayShiftCommand);

    private void SetNightShift() => Execute(FindEmployee(), employee => employee.SetNightShiftCommand);

    private ProductManagementItemViewModel? FindShelfProduct() =>
        _selectedObject() is { IsShelf: true } detail
            ? _products.Products.SingleOrDefault(product => product.Id == detail.ActionTargetKey)
            : null;

    private EmployeeCardViewModel? FindEmployee() =>
        _selectedObject() is { IsEmployee: true } detail
            ? _employees.Employees.SingleOrDefault(employee =>
                employee.EmployeeId == detail.ActionTargetKey
                && employee.StoreId == _selectedStoreId())
            : null;

    private void Execute<T>(T? target, Func<T, IRelayCommand> command)
        where T : class
    {
        if (target is null)
        {
            return;
        }

        command(target).Execute(null);
        _refreshMarket();
    }
}
