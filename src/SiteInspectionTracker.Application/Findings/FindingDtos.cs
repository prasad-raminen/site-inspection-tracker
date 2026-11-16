using SiteInspectionTracker.Application.Common.Models;
using SiteInspectionTracker.Domain.Enums;

namespace SiteInspectionTracker.Application.Findings;

public sealed record FindingDto(
    int Id,
    int InspectionId,
    string InspectionTitle,
    int SiteId,
    string SiteName,
    string Description,
    FindingSeverity Severity,
    bool IsResolved,
    DateTime CreatedAtUtc,
    DateTime? ResolvedAtUtc);

public sealed record CreateFindingRequest(string Description, FindingSeverity Severity);

public sealed record UpdateFindingRequest(string Description, FindingSeverity Severity, bool IsResolved);

public sealed record FindingQuery : PageRequest
{
    public int? SiteId { get; init; }

    public int? InspectionId { get; init; }

    /// <summary>Only return findings of exactly this severity.</summary>
    public FindingSeverity? Severity { get; init; }

    /// <summary>Only return findings at or above this severity (e.g. High returns High and Critical).</summary>
    public FindingSeverity? MinSeverity { get; init; }

    public bool? IsResolved { get; init; }
}
