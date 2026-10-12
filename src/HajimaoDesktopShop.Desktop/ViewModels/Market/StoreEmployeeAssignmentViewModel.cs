using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HajimaoDesktopShop.Application.Business;

namespace HajimaoDesktopShop.Desktop.ViewModels.Market;

public sealed class StoreEmployeeAssignmentViewModel : ObservableObject
{
    private readonly BusinessSession _session;
    private readonly Func<string> _selectedStoreId;
    private readonly Action _refreshMarket;
    private string _statusText = "店员属于全局仓库；调到本店后，原店会暂停挂机。";

    public StoreEmployeeAssignmentViewModel(
        BusinessSession session,
        Func<string> selectedStoreId,
        Action refreshMarket)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _selectedStoreId = selectedStoreId ?? throw new ArgumentNullException(nameof(selectedStoreId));
        _refreshMarket = refreshMarket ?? throw new ArgumentNullException(nameof(refreshMarket));
        AssignToStoreCommand = new RelayCommand<string>(AssignToStore);
    }

    public ObservableCollection<CombatEmployeeItemViewModel> Employees { get; } = [];

    public IRelayCommand<string> AssignToStoreCommand { get; }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public void Refresh()
    {
        Employees.Clear();
        var combat = _session.Combat;
        var selectedStoreId = _selectedStoreId();
        if (combat is null || string.IsNullOrWhiteSpace(selectedStoreId))
        {
            return;
        }

        var snapshot = combat.GetSnapshot();
        var storeNames = _session.Game.GetSnapshot().Stores
            .ToDictionary(item => item.Id, item => item.Name, StringComparer.Ordinal);
        foreach (var employee in snapshot.Employees ?? [])
        {
            var isHere = string.Equals(employee.AssignedStoreId, selectedStoreId, StringComparison.Ordinal);
            var location = employee.AssignedStoreId is null
                ? "仓库待命"
                : isHere
                    ? "已在本店"
                    : $"由 {storeNames.GetValueOrDefault(employee.AssignedStoreId, employee.AssignedStoreId)} 占用";
            Employees.Add(new CombatEmployeeItemViewModel(
                employee.InstanceId,
                employee.DisplayName,
                location,
                isHere ? "已安排" : "调到本店",
                isHere));
        }

        StatusText = Employees.Any(item => item.IsAssignedHere)
            ? "本店正在挂机营业。店员和装备均可在店铺之间调动。"
            : "本店无人，挂机暂停。请从全局店员仓库调入一名店员。";
    }

    private void AssignToStore(string? instanceId)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            return;
        }

        var combat = _session.Combat
            ?? throw new InvalidOperationException("Combat session is unavailable.");
        combat.AssignEmployee(instanceId, _selectedStoreId());
        _refreshMarket();
    }
}

public sealed record CombatEmployeeItemViewModel(
    string InstanceId,
    string Name,
    string LocationText,
    string ActionText,
    bool IsAssignedHere);
