using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SiteInspectionTracker.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    /// <summary>
    /// Applies migrations and/or seeds sample data according to <see cref="DatabaseOptions"/>.
    /// Both steps are off by default so production deployments run migrations explicitly.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DatabaseInitializer));
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (options.ApplyMigrationsOnStartup)
        {
            logger.LogInformation("Applying database migrations");
            await db.Database.MigrateAsync(cancellationToken);
        }

        if (options.SeedSampleData)
        {
            var seeded = await SampleDataSeeder.SeedAsync(db, DateTime.UtcNow, cancellationToken);
            logger.LogInformation(seeded ? "Seeded sample data" : "Database already contains data; skipping seed");
        }
    }
}
