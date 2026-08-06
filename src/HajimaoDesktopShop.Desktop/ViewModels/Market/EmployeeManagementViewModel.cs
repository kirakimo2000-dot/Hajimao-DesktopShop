using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HajimaoDesktopShop.Application.Business;
using HajimaoDesktopShop.Application.Business.Employees;
using HajimaoDesktopShop.Desktop.ViewModels;

namespace HajimaoDesktopShop.Desktop.ViewModels.Market;

public sealed class EmployeeManagementViewModel : ObservableObject
{
    private readonly BusinessSession _session;
    private readonly Func<string> _selectedStoreId;
    private readonly Dictionary<string, EmployeeCardViewModel> _employeesById =
        new(StringComparer.Ordinal);
    private string _statusMessage = "员工排班已就绪";

    public EmployeeManagementViewModel(BusinessSession session, Func<string> selectedStoreId)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(selectedStoreId);
        _session = session;
        _selectedStoreId = selectedStoreId;
        RefreshCandidatesCommand = new RelayCommand(RefreshCandidates);
    }

    public event EventHandler<GameFeedbackEventArgs>? FeedbackRaised;

    public ObservableCollection<EmployeeCardViewModel> Employees { get; } = [];

    public ObservableCollection<EmployeeCandidateCardViewModel> Candidates { get; } = [];

    public IRelayCommand RefreshCandidatesCommand { get; }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public void Refresh()
    {
        var snapshot = _session.Simulation.GetSnapshot().Employees;
        Employees.Clear();
        foreach (var employee in snapshot.Employees)
        {
            if (!_employeesById.TryGetValue(employee.EmployeeId, out var card))
            {
                card = new EmployeeCardViewModel(employee, Train, AssignToSelectedStore, SetShift);
                _employeesById.Add(employee.EmployeeId, card);
            }

            card.Update(employee);
            Employees.Add(card);
        }

        Candidates.Clear();
        foreach (var candidate in snapshot.Candidates)
        {
            Candidates.Add(new EmployeeCandidateCardViewModel(candidate, Hire));
        }
    }

    private void Hire(EmployeeCandidateCardViewModel candidate)
    {
        var result = _session.Simulation.Employees.Hire(candidate.CandidateId, _selectedStoreId());
        CompleteCommand("招聘", result);
    }

    private void Train(EmployeeCardViewModel employee)
    {
        var result = _session.Simulation.Employees.Train(employee.EmployeeId);
        CompleteCommand("培训", result);
    }

    private void AssignToSelectedStore(EmployeeCardViewModel employee)
    {
        var result = _session.Simulation.Employees.AssignStore(
            employee.EmployeeId,
            _selectedStoreId());
        CompleteCommand("调店", result);
    }

    private void SetShift(EmployeeCardViewModel employee, int startMinute, int endMinute)
    {
        var result = _session.Simulation.Employees.SetShift(employee.EmployeeId, startMinute, endMinute);
        CompleteCommand("排班", result);
    }

    private void RefreshCandidates()
    {
        _session.Simulation.Employees.RefreshCandidates();
        StatusMessage = "候选人名单已刷新";
        Refresh();
    }

    private void CompleteCommand(string operation, EmployeeCommandResult result)
    {
        StatusMessage = result.Status == EmployeeCommandStatus.Success
            ? $"{operation}成功"
            : $"{operation}失败：{result.Status}";
        if (result.Status == EmployeeCommandStatus.Success)
        {
            FeedbackRaised?.Invoke(this, new GameFeedbackEventArgs(GameFeedbackKind.EmployeeChanged));
        }

        Refresh();
    }
}
