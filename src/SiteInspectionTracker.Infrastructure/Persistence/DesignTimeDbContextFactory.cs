using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SiteInspectionTracker.Infrastructure.Persistence;

/// <summary>
/// Lets <c>dotnet ef</c> create the context without booting the web host.
/// The connection string is only used when commands actually touch a database
/// (e.g. <c>database update</c>); override it with the <c>SIT_DESIGN_CONNECTION</c> variable.
/// </summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string FallbackConnection =
        "Server=localhost,1433;Database=SiteInspectionTracker;Integrated Security=false;TrustServerCertificate=True";

    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("SIT_DESIGN_CONNECTION") ?? FallbackConnection;

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new AppDbContext(options);
    }
}
