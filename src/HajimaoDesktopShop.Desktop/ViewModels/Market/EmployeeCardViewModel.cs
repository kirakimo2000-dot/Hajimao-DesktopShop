using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HajimaoDesktopShop.Application.Business.Employees;
using HajimaoDesktopShop.Domain.Employees;

namespace HajimaoDesktopShop.Desktop.ViewModels.Market;

public sealed class EmployeeCardViewModel : ObservableObject
{
    private int _effectiveEfficiencyPermille;
    private int _trainingLevel;
    private int _energyPermille;
    private int _satisfactionPermille;
    private string _storeId = string.Empty;
    private string _shiftText = string.Empty;
    private string _taskText = string.Empty;
    private string _priorityText = string.Empty;

    internal EmployeeCardViewModel(
        EmployeeOperationsEmployeeSnapshot snapshot,
        Action<EmployeeCardViewModel> train,
        Action<EmployeeCardViewModel> assignToSelectedStore,
        Action<EmployeeCardViewModel, int, int> setShift)
    {
        EmployeeId = snapshot.EmployeeId;
        Name = snapshot.Name;
        Role = snapshot.Role;
        HourlyWageCents = snapshot.HourlyWageCents;
        TrainCommand = new RelayCommand(() => train(this));
        AssignToSelectedStoreCommand = new RelayCommand(() => assignToSelectedStore(this));
        SetDayShiftCommand = new RelayCommand(() => setShift(this, 480, 960));
        SetNightShiftCommand = new RelayCommand(() => setShift(this, 1_020, 60));
        Update(snapshot);
    }

    public string EmployeeId { get; }

    public string Name { get; }

    public EmployeeRole Role { get; }

    public long HourlyWageCents { get; }

    public IRelayCommand TrainCommand { get; }

    public IRelayCommand AssignToSelectedStoreCommand { get; }

    public IRelayCommand SetDayShiftCommand { get; }

    public IRelayCommand SetNightShiftCommand { get; }

    public int TrainingLevel
    {
        get => _trainingLevel;
        private set => SetProperty(ref _trainingLevel, value);
    }

    public string StoreId
    {
        get => _storeId;
        private set => SetProperty(ref _storeId, value);
    }

    public string ShiftText
    {
        get => _shiftText;
        private set => SetProperty(ref _shiftText, value);
    }

    public string TaskText
    {
        get => _taskText;
        private set => SetProperty(ref _taskText, value);
    }

    public string PriorityText
    {
        get => _priorityText;
        private set => SetProperty(ref _priorityText, value);
    }

    public string EfficiencyText =>
        string.Format(CultureInfo.InvariantCulture, "{0:0}%", _effectiveEfficiencyPermille / 10m);

    public string EnergyText =>
        string.Format(CultureInfo.InvariantCulture, "{0:0}%", _energyPermille / 10m);

    public string SatisfactionText =>
        string.Format(CultureInfo.InvariantCulture, "{0:0}%", _satisfactionPermille / 10m);

    internal void Update(EmployeeOperationsEmployeeSnapshot snapshot)
    {
        TrainingLevel = snapshot.TrainingLevel;
        StoreId = snapshot.StoreId;
        ShiftText = snapshot.IsAlwaysOn
            ? "全天（兼容）"
            : $"{FormatMinute(snapshot.ShiftStartMinute)}–{FormatMinute(snapshot.ShiftEndMinute)}";
        TaskText = EmployeeTaskTextFormatter.FormatTask(snapshot.CurrentTask);
        PriorityText = EmployeeTaskTextFormatter.FormatPriorities(snapshot.TaskPriorities);
        SetMetric(ref _effectiveEfficiencyPermille, snapshot.EffectiveEfficiencyPermille, nameof(EfficiencyText));
        SetMetric(ref _energyPermille, snapshot.EnergyPermille, nameof(EnergyText));
        SetMetric(ref _satisfactionPermille, snapshot.SatisfactionPermille, nameof(SatisfactionText));
    }

    private void SetMetric(ref int field, int value, string propertyName)
    {
        if (field == value)
        {
            return;
        }

        field = value;
        OnPropertyChanged(propertyName);
    }

    private static string FormatMinute(int minute) => $"{minute / 60:00}:{minute % 60:00}";
}
