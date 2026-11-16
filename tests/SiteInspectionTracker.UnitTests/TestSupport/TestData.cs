using SiteInspectionTracker.Domain.Entities;
using SiteInspectionTracker.Domain.Enums;
using SiteInspectionTracker.Infrastructure.Persistence;

namespace SiteInspectionTracker.UnitTests.TestSupport;

internal static class TestData
{
    public static async Task<Site> AddSiteAsync(AppDbContext db, string name = "Test Site")
    {
        var site = new Site(name, "1 Example Street");
        db.Sites.Add(site);
        await db.SaveChangesAsync();
        return site;
    }

    public static async Task<Inspection> AddInspectionAsync(
        AppDbContext db,
        int siteId,
        InspectionStatus status = InspectionStatus.Scheduled,
        DateTime? scheduledForUtc = null,
        string inspector = "Jane Doe")
    {
        var inspection = new Inspection(siteId, "Routine inspection", inspector, scheduledForUtc ?? new DateTime(2026, 1, 15, 9, 0, 0, DateTimeKind.Utc));
        db.Inspections.Add(inspection);
        await db.SaveChangesAsync();

        foreach (var step in PathTo(status))
        {
            inspection.ChangeStatus(step);
        }

        await db.SaveChangesAsync();
        return inspection;
    }

    private static IEnumerable<InspectionStatus> PathTo(InspectionStatus target) => target switch
    {
        InspectionStatus.Scheduled => Array.Empty<InspectionStatus>(),
        InspectionStatus.InProgress => new[] { InspectionStatus.InProgress },
        _ => new[] { InspectionStatus.InProgress, target },
    };
}
