using SiteInspectionTracker.Application.Common.Models;
using SiteInspectionTracker.Application.Findings;
using SiteInspectionTracker.Domain.Enums;

namespace SiteInspectionTracker.Application.Inspections;

public record InspectionDto(
    int Id,
    int SiteId,
    string SiteName,
    string Title,
    string InspectorName,
    DateTime ScheduledForUtc,
    DateTime? CompletedAtUtc,
    InspectionStatus Status,
    string? Notes,
    int FindingCount,
    int OpenFindingCount,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record InspectionDetailDto(InspectionDto Inspection, IReadOnlyList<FindingDto> Findings);

public sealed record CreateInspectionRequest(
    int SiteId,
    string Title,
    string InspectorName,
    DateTime ScheduledForUtc,
    string? Notes);

public sealed record UpdateInspectionRequest(
    string Title,
    string InspectorName,
    DateTime ScheduledForUtc,
    InspectionStatus Status,
    string? Notes);

public sealed record InspectionQuery : PageRequest
{
    public int? SiteId { get; init; }

    public InspectionStatus? Status { get; init; }

    /// <summary>Case-insensitive partial match on the inspector's name.</summary>
    public string? Inspector { get; init; }

    public DateTime? From { get; init; }

    public DateTime? To { get; init; }
}
