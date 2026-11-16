using SiteInspectionTracker.Application.Dashboard;
using SiteInspectionTracker.Domain.Enums;
using SiteInspectionTracker.Infrastructure.Persistence;
using SiteInspectionTracker.UnitTests.TestSupport;

namespace SiteInspectionTracker.UnitTests.Services;

public sealed class DashboardServiceTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 5, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly SqliteTestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Empty_database_reports_zero_for_every_status_and_severity()
    {
        await using var db = _database.CreateContext();

        var summary = await new DashboardService(db, new FixedTimeProvider(Now)).GetSummaryAsync();

        Assert.Equal(0, summary.TotalSites);
        Assert.Equal(Enum.GetValues<InspectionStatus>().Length, summary.InspectionsByStatus.Count);
        Assert.Equal(Enum.GetValues<FindingSeverity>().Length, summary.OpenFindingsBySeverity.Count);
        Assert.All(summary.InspectionsByStatus.Values, v => Assert.Equal(0, v));
        Assert.Empty(summary.UpcomingInspections);
    }

    [Fact]
    public async Task Summary_counts_inspections_per_status_and_open_findings_per_severity()
    {
        await using var db = _database.CreateContext();
        var site = await TestData.AddSiteAsync(db);
        await TestData.AddInspectionAsync(db, site.Id, InspectionStatus.Scheduled, Now.AddDays(2));
        await TestData.AddInspectionAsync(db, site.Id, InspectionStatus.Scheduled, Now.AddDays(-1)); // overdue, not upcoming
        var active = await TestData.AddInspectionAsync(db, site.Id, InspectionStatus.InProgress);
        await TestData.AddInspectionAsync(db, site.Id, InspectionStatus.Failed);
        active.AddFinding("c1", FindingSeverity.Critical);
        active.AddFinding("c2", FindingSeverity.Critical);
        active.AddFinding("m1", FindingSeverity.Medium);
        active.AddFinding("resolved", FindingSeverity.High).MarkResolved();
        await db.SaveChangesAsync();

        var summary = await new DashboardService(db, new FixedTimeProvider(Now)).GetSummaryAsync();

        Assert.Equal(1, summary.TotalSites);
        Assert.Equal(4, summary.TotalInspections);
        Assert.Equal(2, summary.InspectionsByStatus[InspectionStatus.Scheduled]);
        Assert.Equal(1, summary.InspectionsByStatus[InspectionStatus.InProgress]);
        Assert.Equal(0, summary.InspectionsByStatus[InspectionStatus.Completed]);
        Assert.Equal(1, summary.InspectionsByStatus[InspectionStatus.Failed]);

        Assert.Equal(4, summary.TotalFindings);
        Assert.Equal(3, summary.OpenFindings);
        Assert.Equal(2, summary.OpenFindingsBySeverity[FindingSeverity.Critical]);
        Assert.Equal(0, summary.OpenFindingsBySeverity[FindingSeverity.High]);
        Assert.Equal(1, summary.OpenFindingsBySeverity[FindingSeverity.Medium]);

        var upcoming = Assert.Single(summary.UpcomingInspections);
        Assert.Equal(Now.AddDays(2), upcoming.ScheduledForUtc);
    }
}
