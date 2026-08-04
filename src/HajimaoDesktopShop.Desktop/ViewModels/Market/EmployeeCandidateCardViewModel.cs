using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using HajimaoDesktopShop.Application.Business.Employees;
using HajimaoDesktopShop.Domain.Employees;

namespace HajimaoDesktopShop.Desktop.ViewModels.Market;

public sealed class EmployeeCandidateCardViewModel
{
    internal EmployeeCandidateCardViewModel(
        EmployeeCandidate candidate,
        Action<EmployeeCandidateCardViewModel> hire)
    {
        CandidateId = candidate.CandidateId;
        Name = candidate.Name;
        Role = candidate.Role;
        EfficiencyPermille = candidate.EfficiencyPermille;
        HourlyWageCents = candidate.HourlyWage.Cents;
        HireCostCents = candidate.HireCost.Cents;
        HireCommand = new RelayCommand(() => hire(this));
    }

    public string CandidateId { get; }

    public string Name { get; }

    public EmployeeRole Role { get; }

    public int EfficiencyPermille { get; }

    public long HourlyWageCents { get; }

    public long HireCostCents { get; }

    public string EfficiencyText =>
        string.Format(CultureInfo.InvariantCulture, "{0:0}%", EfficiencyPermille / 10m);

    public IRelayCommand HireCommand { get; }
}
