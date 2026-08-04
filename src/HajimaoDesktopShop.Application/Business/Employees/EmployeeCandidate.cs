using HajimaoDesktopShop.Domain.Economy;
using HajimaoDesktopShop.Domain.Employees;

namespace HajimaoDesktopShop.Application.Business.Employees;

public sealed record EmployeeCandidate
{
    public EmployeeCandidate(
        string candidateId,
        string name,
        EmployeeRole role,
        int efficiencyPermille,
        Money hourlyWage)
    {
        if (string.IsNullOrWhiteSpace(candidateId))
        {
            throw new ArgumentException("Candidate ID is required.", nameof(candidateId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Candidate name is required.", nameof(name));
        }

        if (efficiencyPermille <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(efficiencyPermille));
        }

        if (!hourlyWage.IsPositive || hourlyWage.Cents > long.MaxValue / 40L)
        {
            throw new ArgumentOutOfRangeException(nameof(hourlyWage));
        }

        CandidateId = candidateId.Trim();
        Name = name.Trim();
        Role = role;
        EfficiencyPermille = efficiencyPermille;
        HourlyWage = hourlyWage;
    }

    public string CandidateId { get; }

    public string Name { get; }

    public EmployeeRole Role { get; }

    public int EfficiencyPermille { get; }

    public Money HourlyWage { get; }

    public Money HireCost => HourlyWage * 40;
}
