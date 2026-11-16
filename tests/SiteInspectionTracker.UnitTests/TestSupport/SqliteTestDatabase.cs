using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SiteInspectionTracker.Infrastructure.Persistence;

namespace SiteInspectionTracker.UnitTests.TestSupport;

/// <summary>
/// A throw-away SQLite in-memory database with the real EF Core model. Using a relational
/// provider (rather than EF InMemory) means constraints, cascades and LINQ translation are exercised.
/// </summary>
public sealed class SqliteTestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public SqliteTestDatabase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    /// <summary>Creates a new context over the same database (use a fresh one to avoid tracking side effects).</summary>
    public AppDbContext CreateContext() => new(_options);

    public void Dispose() => _connection.Dispose();
}
