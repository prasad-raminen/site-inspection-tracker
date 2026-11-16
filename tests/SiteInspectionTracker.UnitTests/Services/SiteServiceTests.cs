using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SiteInspectionTracker.Application.Common.Exceptions;
using SiteInspectionTracker.Application.Sites;
using SiteInspectionTracker.Domain.Enums;
using SiteInspectionTracker.Infrastructure.Persistence;
using SiteInspectionTracker.UnitTests.TestSupport;

namespace SiteInspectionTracker.UnitTests.Services;

public sealed class SiteServiceTests : IDisposable
{
    private readonly SqliteTestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private static SiteService CreateService(AppDbContext db) =>
        new(db, new SaveSiteRequestValidator(), NullLogger<SiteService>.Instance);

    [Fact]
    public async Task CreateAsync_persists_site_and_returns_dto()
    {
        await using var db = _database.CreateContext();

        var dto = await CreateService(db).CreateAsync(new SaveSiteRequest(" Pier 9 ", "9 Wharf Rd", "Port Co", -33.86, 151.21));

        Assert.True(dto.Id > 0);
        Assert.Equal("Pier 9", dto.Name);
        Assert.Equal(0, dto.InspectionCount);

        await using var verify = _database.CreateContext();
        Assert.True(await verify.Sites.AnyAsync(s => s.Id == dto.Id && s.ClientName == "Port Co"));
    }

    [Fact]
    public async Task CreateAsync_with_invalid_request_throws_validation_exception_with_field_errors()
    {
        await using var db = _database.CreateContext();

        var ex = await Assert.ThrowsAsync<RequestValidationException>(
            () => CreateService(db).CreateAsync(new SaveSiteRequest("", "", null, null, null)));

        Assert.Contains(nameof(SaveSiteRequest.Name), ex.Errors.Keys);
        Assert.Contains(nameof(SaveSiteRequest.Address), ex.Errors.Keys);
    }

    [Fact]
    public async Task CreateAsync_with_duplicate_name_throws_conflict()
    {
        await using var db = _database.CreateContext();
        await TestData.AddSiteAsync(db, "Pier 9");

        await Assert.ThrowsAsync<ConflictException>(
            () => CreateService(db).CreateAsync(new SaveSiteRequest("Pier 9", "Elsewhere", null, null, null)));
    }

    [Fact]
    public async Task UpdateAsync_allows_keeping_the_same_name()
    {
        await using var db = _database.CreateContext();
        var site = await TestData.AddSiteAsync(db, "Pier 9");

        var dto = await CreateService(db).UpdateAsync(site.Id, new SaveSiteRequest("Pier 9", "New address", null, null, null));

        Assert.Equal("New address", dto.Address);
        Assert.NotNull(dto.UpdatedAtUtc);
    }

    [Fact]
    public async Task GetAsync_for_unknown_id_throws_not_found()
    {
        await using var db = _database.CreateContext();

        await Assert.ThrowsAsync<NotFoundException>(() => CreateService(db).GetAsync(999));
    }

    [Fact]
    public async Task ListAsync_filters_by_search_term_and_pages_results_in_name_order()
    {
        await using var db = _database.CreateContext();
        foreach (var name in new[] { "Bridge C", "Tower A", "Bridge A", "Bridge B" })
        {
            await TestData.AddSiteAsync(db, name);
        }

        var page = await CreateService(db).ListAsync(new SiteQuery { Search = "bridge", Page = 1, PageSize = 2 });

        Assert.Equal(3, page.TotalCount);
        Assert.Equal(2, page.TotalPages);
        Assert.True(page.HasNextPage);
        Assert.Equal(new[] { "Bridge A", "Bridge B" }, page.Items.Select(s => s.Name));
    }

    [Fact]
    public async Task Dto_counts_inspections_and_only_open_findings()
    {
        await using var db = _database.CreateContext();
        var site = await TestData.AddSiteAsync(db);
        var inspection = await TestData.AddInspectionAsync(db, site.Id, InspectionStatus.InProgress);
        inspection.AddFinding("Open issue", FindingSeverity.High);
        inspection.AddFinding("Fixed issue", FindingSeverity.Low).MarkResolved();
        await TestData.AddInspectionAsync(db, site.Id);
        await db.SaveChangesAsync();

        var dto = await CreateService(db).GetAsync(site.Id);

        Assert.Equal(2, dto.InspectionCount);
        Assert.Equal(1, dto.OpenFindingCount);
    }

    [Fact]
    public async Task DeleteAsync_cascades_to_inspections_and_findings()
    {
        await using (var db = _database.CreateContext())
        {
            var site = await TestData.AddSiteAsync(db);
            var inspection = await TestData.AddInspectionAsync(db, site.Id, InspectionStatus.InProgress);
            inspection.AddFinding("Issue", FindingSeverity.Medium);
            await db.SaveChangesAsync();

            await CreateService(db).DeleteAsync(site.Id);
        }

        await using var verify = _database.CreateContext();
        Assert.Equal(0, await verify.Inspections.CountAsync());
        Assert.Equal(0, await verify.Findings.CountAsync());
    }
}
