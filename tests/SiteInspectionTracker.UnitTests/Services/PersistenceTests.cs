using Microsoft.EntityFrameworkCore;
using SiteInspectionTracker.Domain.Enums;
using SiteInspectionTracker.UnitTests.TestSupport;

namespace SiteInspectionTracker.UnitTests.Services;

public sealed class PersistenceTests : IDisposable
{
    private readonly SqliteTestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Dates_read_back_from_the_database_are_marked_as_utc()
    {
        int inspectionId;
        await using (var db = _database.CreateContext())
        {
            var site = await TestData.AddSiteAsync(db);
            inspectionId = (await TestData.AddInspectionAsync(db, site.Id, InspectionStatus.Completed)).Id;
        }

        await using var fresh = _database.CreateContext();
        var inspection = await fresh.Inspections.AsNoTracking().SingleAsync(i => i.Id == inspectionId);

        Assert.Equal(DateTimeKind.Utc, inspection.ScheduledForUtc.Kind);
        Assert.Equal(DateTimeKind.Utc, inspection.CreatedAtUtc.Kind);
        Assert.Equal(DateTimeKind.Utc, inspection.CompletedAtUtc!.Value.Kind);
    }
}
