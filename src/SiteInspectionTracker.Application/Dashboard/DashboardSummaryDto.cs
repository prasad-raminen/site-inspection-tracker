using SiteInspectionTracker.Application.Inspections;
using SiteInspectionTracker.Domain.Enums;

namespace SiteInspectionTracker.Application.Dashboard;

public sealed record DashboardSummaryDto(
    int TotalSites,
    int TotalInspections,
    int TotalFindings,
    int OpenFindings,
    IReadOnlyDictionary<InspectionStatus, int> InspectionsByStatus,
    IReadOnlyDictionary<FindingSeverity, int> OpenFindingsBySeverity,
    IReadOnlyList<InspectionDto> UpcomingInspections);
