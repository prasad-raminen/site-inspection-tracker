namespace SiteInspectionTracker.Infrastructure.Persistence;

/// <summary>Startup behaviour for the database, bound from the <c>Database</c> configuration section.</summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>Apply pending EF Core migrations when the app starts (convenient for dev/containers).</summary>
    public bool ApplyMigrationsOnStartup { get; set; }

    /// <summary>Insert demo sites/inspections/findings when the database is empty.</summary>
    public bool SeedSampleData { get; set; }
}
