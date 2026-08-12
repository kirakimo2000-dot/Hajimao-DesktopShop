namespace HajimaoDesktopShop.Application.Catalog;

public sealed record StoreBrandDefinition
{
    public StoreBrandDefinition(
        string id,
        string displayName,
        string region,
        string formatId,
        string facadeStyleKey,
        string referenceNote,
        string distributionStatus)
    {
        if (string.IsNullOrWhiteSpace(id)
            || string.IsNullOrWhiteSpace(displayName)
            || string.IsNullOrWhiteSpace(region)
            || string.IsNullOrWhiteSpace(formatId)
            || string.IsNullOrWhiteSpace(facadeStyleKey)
            || string.IsNullOrWhiteSpace(referenceNote)
            || string.IsNullOrWhiteSpace(distributionStatus))
        {
            throw new ArgumentException("Store brand fields are required.", nameof(id));
        }

        Id = id.Trim();
        DisplayName = displayName.Trim();
        Region = region.Trim();
        FormatId = formatId.Trim();
        FacadeStyleKey = facadeStyleKey.Trim();
        ReferenceNote = referenceNote.Trim();
        DistributionStatus = distributionStatus.Trim();
    }

    public string Id { get; }
    public string DisplayName { get; }
    public string Region { get; }
    public string FormatId { get; }
    public string FacadeStyleKey { get; }
    public string ReferenceNote { get; }
    public string DistributionStatus { get; }
}
