using Microsoft.EntityFrameworkCore;
using SiteInspectionTracker.Infrastructure.Persistence;
using SiteInspectionTracker.UnitTests.TestSupport;

namespace SiteInspectionTracker.UnitTests.Services;

public sealed class SampleDataSeederTests : IDisposable
{
    private readonly SqliteTestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Seeds_an_empty_database_once()
    {
        await using var db = _database.CreateContext();

        var first = await SampleDataSeeder.SeedAsync(db, DateTime.UtcNow);
        var second = await SampleDataSeeder.SeedAsync(db, DateTime.UtcNow);

        Assert.True(first);
        Assert.False(second);
        Assert.Equal(4, await db.Sites.CountAsync());
        Assert.True(await db.Findings.AnyAsync());
    }
}
