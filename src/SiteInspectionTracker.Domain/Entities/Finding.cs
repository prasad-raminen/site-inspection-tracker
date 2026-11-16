using SiteInspectionTracker.Domain.Common;
using SiteInspectionTracker.Domain.Enums;

namespace SiteInspectionTracker.Domain.Entities;

/// <summary>
/// An issue observed during an <see cref="Inspection"/> (e.g. missing guard rail, cracked weld).
/// </summary>
public class Finding : Entity
{
    public const int DescriptionMaxLength = 2000;

    // Required by EF Core.
    private Finding()
    {
        Description = string.Empty;
    }

    internal Finding(int inspectionId, string description, FindingSeverity severity)
    {
        InspectionId = inspectionId;
        Description = Guard.NotBlank(description, nameof(Description), DescriptionMaxLength);
        Severity = severity;
    }

    public int InspectionId { get; private set; }

    public Inspection? Inspection { get; private set; }

    public string Description { get; private set; }

    public FindingSeverity Severity { get; private set; }

    public bool IsResolved { get; private set; }

    public DateTime? ResolvedAtUtc { get; private set; }

    public void Update(string description, FindingSeverity severity)
    {
        Description = Guard.NotBlank(description, nameof(Description), DescriptionMaxLength);
        Severity = severity;
        Touch();
    }

    public void MarkResolved()
    {
        if (IsResolved)
        {
            return;
        }

        IsResolved = true;
        ResolvedAtUtc = DateTime.UtcNow;
        Touch();
    }

    public void Reopen()
    {
        if (!IsResolved)
        {
            return;
        }

        IsResolved = false;
        ResolvedAtUtc = null;
        Touch();
    }
}
