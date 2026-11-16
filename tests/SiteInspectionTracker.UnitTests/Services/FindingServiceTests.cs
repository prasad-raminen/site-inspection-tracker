using Microsoft.Extensions.Logging.Abstractions;
using SiteInspectionTracker.Application.Common.Exceptions;
using SiteInspectionTracker.Application.Findings;
using SiteInspectionTracker.Domain.Enums;
using SiteInspectionTracker.Infrastructure.Persistence;
using SiteInspectionTracker.UnitTests.TestSupport;

namespace SiteInspectionTracker.UnitTests.Services;

public sealed class FindingServiceTests : IDisposable
{
    private readonly SqliteTestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private static FindingService CreateService(AppDbContext db) => new(
        db,
        new CreateFindingRequestValidator(),
        new UpdateFindingRequestValidator(),
        NullLogger<FindingService>.Instance);

    [Fact]
    public async Task CreateAsync_on_started_inspection_returns_finding_with_site_context()
    {
        await using var db = _database.CreateContext();
        var site = await TestData.AddSiteAsync(db, "Rail Bridge");
        var inspection = await TestData.AddInspectionAsync(db, site.Id, InspectionStatus.InProgress);

        var dto = await CreateService(db).CreateAsync(inspection.Id, new CreateFindingRequest("Cracked weld", FindingSeverity.Critical));

        Assert.Equal(inspection.Id, dto.InspectionId);
        Assert.Equal(site.Id, dto.SiteId);
        Assert.Equal("Rail Bridge", dto.SiteName);
        Assert.Equal(FindingSeverity.Critical, dto.Severity);
        Assert.False(dto.IsResolved);
    }

    [Fact]
    public async Task CreateAsync_on_scheduled_inspection_throws_conflict()
    {
        await using var db = _database.CreateContext();
        var site = await TestData.AddSiteAsync(db);
        var inspection = await TestData.AddInspectionAsync(db, site.Id);

        await Assert.ThrowsAsync<ConflictException>(() =>
            CreateService(db).CreateAsync(inspection.Id, new CreateFindingRequest("Too early", FindingSeverity.Low)));
    }

    [Fact]
    public async Task CreateAsync_on_unknown_inspection_throws_not_found()
    {
        await using var db = _database.CreateContext();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateService(db).CreateAsync(77, new CreateFindingRequest("Nothing", FindingSeverity.Low)));
    }

    [Fact]
    public async Task UpdateAsync_resolves_and_reopens_finding()
    {
        await using var db = _database.CreateContext();
        var site = await TestData.AddSiteAsync(db);
        var inspection = await TestData.AddInspectionAsync(db, site.Id, InspectionStatus.InProgress);
        var service = CreateService(db);
        var created = await service.CreateAsync(inspection.Id, new CreateFindingRequest("Loose bolt", FindingSeverity.Medium));

        var resolved = await service.UpdateAsync(created.Id, new UpdateFindingRequest("Loose bolt", FindingSeverity.Medium, IsResolved: true));
        Assert.True(resolved.IsResolved);
        Assert.NotNull(resolved.ResolvedAtUtc);

        var reopened = await service.UpdateAsync(created.Id, new UpdateFindingRequest("Loose bolt (again)", FindingSeverity.High, IsResolved: false));
        Assert.False(reopened.IsResolved);
        Assert.Null(reopened.ResolvedAtUtc);
        Assert.Equal(FindingSeverity.High, reopened.Severity);
    }

    [Fact]
    public async Task ListAsync_with_min_severity_returns_open_high_and_critical_findings_most_severe_first()
    {
        await using var db = _database.CreateContext();
        var site = await TestData.AddSiteAsync(db);
        var inspection = await TestData.AddInspectionAsync(db, site.Id, InspectionStatus.InProgress);
        inspection.AddFinding("High", FindingSeverity.High);
        inspection.AddFinding("Low", FindingSeverity.Low);
        inspection.AddFinding("Critical", FindingSeverity.Critical);
        inspection.AddFinding("Resolved critical", FindingSeverity.Critical).MarkResolved();
        await db.SaveChangesAsync();

        var result = await CreateService(db).ListAsync(new FindingQuery { MinSeverity = FindingSeverity.High, IsResolved = false });

        Assert.Equal(new[] { "Critical", "High" }, result.Items.Select(f => f.Description));
    }

    [Fact]
    public async Task ListAsync_filters_by_site()
    {
        await using var db = _database.CreateContext();
        var siteA = await TestData.AddSiteAsync(db, "A");
        var siteB = await TestData.AddSiteAsync(db, "B");
        var inA = await TestData.AddInspectionAsync(db, siteA.Id, InspectionStatus.InProgress);
        var inB = await TestData.AddInspectionAsync(db, siteB.Id, InspectionStatus.InProgress);
        inA.AddFinding("In A", FindingSeverity.Low);
        inB.AddFinding("In B", FindingSeverity.Low);
        await db.SaveChangesAsync();

        var result = await CreateService(db).ListAsync(new FindingQuery { SiteId = siteB.Id });

        Assert.Equal("In B", Assert.Single(result.Items).Description);
    }
}
