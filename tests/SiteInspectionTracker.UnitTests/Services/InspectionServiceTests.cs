using Microsoft.Extensions.Logging.Abstractions;
using SiteInspectionTracker.Application.Common.Exceptions;
using SiteInspectionTracker.Application.Inspections;
using SiteInspectionTracker.Domain.Enums;
using SiteInspectionTracker.Infrastructure.Persistence;
using SiteInspectionTracker.UnitTests.TestSupport;

namespace SiteInspectionTracker.UnitTests.Services;

public sealed class InspectionServiceTests : IDisposable
{
    private readonly SqliteTestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private static InspectionService CreateService(AppDbContext db) => new(
        db,
        new CreateInspectionRequestValidator(),
        new UpdateInspectionRequestValidator(),
        new InspectionQueryValidator(),
        NullLogger<InspectionService>.Instance);

    [Fact]
    public async Task CreateAsync_schedules_inspection_for_existing_site()
    {
        await using var db = _database.CreateContext();
        var site = await TestData.AddSiteAsync(db, "Harbour Tower");
        var when = new DateTime(2026, 6, 1, 9, 30, 0, DateTimeKind.Utc);

        var dto = await CreateService(db).CreateAsync(new CreateInspectionRequest(site.Id, "Scaffold walk", "Sam Lee", when, "Bring harness"));

        Assert.Equal(InspectionStatus.Scheduled, dto.Status);
        Assert.Equal("Harbour Tower", dto.SiteName);
        Assert.Equal(when, dto.ScheduledForUtc);
        Assert.Equal(0, dto.FindingCount);
    }

    [Fact]
    public async Task CreateAsync_for_unknown_site_reports_a_site_id_validation_error()
    {
        await using var db = _database.CreateContext();

        var ex = await Assert.ThrowsAsync<RequestValidationException>(() =>
            CreateService(db).CreateAsync(new CreateInspectionRequest(404, "Title", "Inspector", DateTime.UtcNow, null)));

        Assert.Contains("SiteId", ex.Errors.Keys);
    }

    [Fact]
    public async Task ListAsync_filters_by_site_and_status_newest_first()
    {
        await using var db = _database.CreateContext();
        var siteA = await TestData.AddSiteAsync(db, "A");
        var siteB = await TestData.AddSiteAsync(db, "B");
        var older = await TestData.AddInspectionAsync(db, siteA.Id, InspectionStatus.Completed, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var newer = await TestData.AddInspectionAsync(db, siteA.Id, InspectionStatus.Completed, new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc));
        await TestData.AddInspectionAsync(db, siteA.Id, InspectionStatus.Scheduled);
        await TestData.AddInspectionAsync(db, siteB.Id, InspectionStatus.Completed);

        var result = await CreateService(db).ListAsync(new InspectionQuery { SiteId = siteA.Id, Status = InspectionStatus.Completed });

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(new[] { newer.Id, older.Id }, result.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task ListAsync_filters_by_inspector_case_insensitively()
    {
        await using var db = _database.CreateContext();
        var site = await TestData.AddSiteAsync(db);
        await TestData.AddInspectionAsync(db, site.Id, inspector: "Priya Raman");
        await TestData.AddInspectionAsync(db, site.Id, inspector: "Liam Chen");

        var result = await CreateService(db).ListAsync(new InspectionQuery { Inspector = "priya" });

        var only = Assert.Single(result.Items);
        Assert.Equal("Priya Raman", only.InspectorName);
    }

    [Fact]
    public async Task UpdateAsync_applies_a_legal_status_transition()
    {
        await using var db = _database.CreateContext();
        var site = await TestData.AddSiteAsync(db);
        var inspection = await TestData.AddInspectionAsync(db, site.Id);

        var dto = await CreateService(db).UpdateAsync(
            inspection.Id,
            new UpdateInspectionRequest("Renamed", "New Inspector", inspection.ScheduledForUtc, InspectionStatus.InProgress, null));

        Assert.Equal(InspectionStatus.InProgress, dto.Status);
        Assert.Equal("Renamed", dto.Title);
    }

    [Fact]
    public async Task UpdateAsync_with_illegal_status_transition_throws_conflict_and_does_not_persist()
    {
        int inspectionId;
        await using (var db = _database.CreateContext())
        {
            var site = await TestData.AddSiteAsync(db);
            var inspection = await TestData.AddInspectionAsync(db, site.Id, InspectionStatus.Completed);
            inspectionId = inspection.Id;

            await Assert.ThrowsAsync<ConflictException>(() => CreateService(db).UpdateAsync(
                inspectionId,
                new UpdateInspectionRequest("Changed", "Inspector", DateTime.UtcNow, InspectionStatus.Scheduled, null)));
        }

        await using var verify = _database.CreateContext();
        var reloaded = await CreateService(verify).GetAsync(inspectionId);
        Assert.Equal(InspectionStatus.Completed, reloaded.Inspection.Status);
        Assert.Equal("Routine inspection", reloaded.Inspection.Title);
    }

    [Fact]
    public async Task GetAsync_returns_findings_most_severe_first()
    {
        await using var db = _database.CreateContext();
        var site = await TestData.AddSiteAsync(db);
        var inspection = await TestData.AddInspectionAsync(db, site.Id, InspectionStatus.InProgress);
        inspection.AddFinding("Low", FindingSeverity.Low);
        inspection.AddFinding("Critical", FindingSeverity.Critical);
        inspection.AddFinding("Medium", FindingSeverity.Medium);
        await db.SaveChangesAsync();

        var detail = await CreateService(db).GetAsync(inspection.Id);

        Assert.Equal(
            new[] { FindingSeverity.Critical, FindingSeverity.Medium, FindingSeverity.Low },
            detail.Findings.Select(f => f.Severity));
        Assert.Equal(3, detail.Inspection.OpenFindingCount);
    }

    [Fact]
    public async Task DeleteAsync_for_unknown_id_throws_not_found()
    {
        await using var db = _database.CreateContext();

        await Assert.ThrowsAsync<NotFoundException>(() => CreateService(db).DeleteAsync(12345));
    }
}
