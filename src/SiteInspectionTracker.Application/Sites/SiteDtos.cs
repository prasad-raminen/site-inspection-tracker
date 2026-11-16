using SiteInspectionTracker.Application.Common.Models;

namespace SiteInspectionTracker.Application.Sites;

public sealed record SiteDto(
    int Id,
    string Name,
    string Address,
    string? ClientName,
    double? Latitude,
    double? Longitude,
    int InspectionCount,
    int OpenFindingCount,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

/// <summary>Request body for creating or updating a site.</summary>
public sealed record SaveSiteRequest(
    string Name,
    string Address,
    string? ClientName,
    double? Latitude,
    double? Longitude);

public sealed record SiteQuery : PageRequest
{
    /// <summary>Case-insensitive match against name, address or client.</summary>
    public string? Search { get; init; }
}
