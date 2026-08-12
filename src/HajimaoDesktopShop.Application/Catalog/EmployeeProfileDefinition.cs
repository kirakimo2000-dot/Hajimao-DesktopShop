using HajimaoDesktopShop.Domain.Employees;

namespace HajimaoDesktopShop.Application.Catalog;

public sealed record EmployeeProfileDefinition
{
    public EmployeeProfileDefinition(
        string id,
        string displayName,
        string regionTag,
        string appearanceKey,
        IReadOnlyList<EmployeeRole> allowedRoles,
        int efficiencyBiasPermille,
        int wageBiasPermille,
        string backgroundText)
    {
        if (string.IsNullOrWhiteSpace(id)
            || string.IsNullOrWhiteSpace(displayName)
            || string.IsNullOrWhiteSpace(regionTag)
            || string.IsNullOrWhiteSpace(appearanceKey)
            || string.IsNullOrWhiteSpace(backgroundText))
        {
            throw new ArgumentException("Employee profile fields are required.", nameof(id));
        }

        if (allowedRoles is not { Count: > 0 })
        {
            throw new ArgumentException("At least one employee role is required.", nameof(allowedRoles));
        }

        if (efficiencyBiasPermille is < 850 or > 1150)
        {
            throw new ArgumentOutOfRangeException(nameof(efficiencyBiasPermille));
        }

        if (wageBiasPermille is < 850 or > 1150)
        {
            throw new ArgumentOutOfRangeException(nameof(wageBiasPermille));
        }

        Id = id.Trim();
        DisplayName = displayName.Trim();
        RegionTag = regionTag.Trim();
        AppearanceKey = appearanceKey.Trim();
        AllowedRoles = Array.AsReadOnly(allowedRoles.Distinct().ToArray());
        EfficiencyBiasPermille = efficiencyBiasPermille;
        WageBiasPermille = wageBiasPermille;
        BackgroundText = backgroundText.Trim();
    }

    public string Id { get; }
    public string DisplayName { get; }
    public string RegionTag { get; }
    public string AppearanceKey { get; }
    public IReadOnlyList<EmployeeRole> AllowedRoles { get; }
    public int EfficiencyBiasPermille { get; }
    public int WageBiasPermille { get; }
    public string BackgroundText { get; }
}
