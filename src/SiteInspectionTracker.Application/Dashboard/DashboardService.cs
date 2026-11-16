using Microsoft.EntityFrameworkCore;
using SiteInspectionTracker.Application.Common.Interfaces;
using SiteInspectionTracker.Application.Inspections;
using SiteInspectionTracker.Domain.Enums;

namespace SiteInspectionTracker.Application.Dashboard;

public sealed class DashboardService : IDashboardService
{
    public const int UpcomingInspectionCount = 5;

    private readonly IApplicationDbContext _db;
    private readonly TimeProvider _clock;

    public DashboardService(IApplicationDbContext db, TimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var totalSites = await _db.Sites.CountAsync(cancellationToken);

        var statusCounts = await _db.Inspections
            .GroupBy(i => i.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var findingCounts = await _db.Findings
            .GroupBy(f => new { f.Severity, f.IsResolved })
            .Select(g => new { g.Key.Severity, g.Key.IsResolved, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var now = _clock.GetUtcNow().UtcDateTime;
        var upcoming = await _db.Inspections.AsNoTracking()
            .Where(i => i.Status == InspectionStatus.Scheduled && i.ScheduledForUtc >= now)
            .OrderBy(i => i.ScheduledForUtc)
            .Take(UpcomingInspectionCount)
            .Select(InspectionService.ToDto)
            .ToListAsync(cancellationToken);

        // Always return every enum member so clients can render a stable set of cards/bars.
        var byStatus = Enum.GetValues<InspectionStatus>().ToDictionary(
            s => s,
            s => statusCounts.Where(c => c.Status == s).Sum(c => c.Count));

        var openBySeverity = Enum.GetValues<FindingSeverity>().ToDictionary(
            s => s,
            s => findingCounts.Where(c => c.Severity == s && !c.IsResolved).Sum(c => c.Count));

        return new DashboardSummaryDto(
            TotalSites: totalSites,
            TotalInspections: byStatus.Values.Sum(),
            TotalFindings: findingCounts.Sum(c => c.Count),
            OpenFindings: openBySeverity.Values.Sum(),
            InspectionsByStatus: byStatus,
            OpenFindingsBySeverity: openBySeverity,
            UpcomingInspections: upcoming);
    }
}
